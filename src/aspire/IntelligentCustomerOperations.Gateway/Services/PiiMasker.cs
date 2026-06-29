using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using IntelligentCustomerOperations.Gateway.Models;

namespace IntelligentCustomerOperations.Gateway.Services;

public sealed record PiiRedactionResult(JsonArray MaskedTranscript, string MaskedText, IReadOnlyList<string> Signals)
{
    public bool Applied => Signals.Count > 0;
}

public static class PiiMasker
{
    private static readonly HttpClient Http = new();

    private static readonly Regex EmailRegex = new(
        @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled
    );

    private static readonly Regex CreditCardRegex = new(
        @"\b(?:\d[ -]*?){13,19}\b",
        RegexOptions.CultureInvariant | RegexOptions.Compiled
    );

    private static readonly Regex PhoneRegex = new(
        @"(?<!\d)(?:\+\d{1,3}[\s.-]?)?(?:\(?\d{2,4}\)?[\s.-]?)?\d{2,4}[\s.-]?\d{3,4}[\s.-]?\d{3,4}(?!\d)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled
    );

    private static readonly Regex PostalCodeRegex = new(
        @"\b\d{3}-\d{4}\b",
        RegexOptions.CultureInvariant | RegexOptions.Compiled
    );

    public static async Task<PiiRedactionResult> MaskTranscriptAsync(
        IEnumerable<CallTurn> transcript,
        string? language,
        CancellationToken ct = default
    )
    {
        var turns = transcript.ToList();
        var redaction = await TryAzureLanguageRedactionAsync(turns, language, ct);
        if (redaction is not null)
        {
            return redaction;
        }

        return MaskTranscriptLocally(turns);
    }

    private static async Task<PiiRedactionResult?> TryAzureLanguageRedactionAsync(
        IReadOnlyList<CallTurn> turns,
        string? language,
        CancellationToken ct
    )
    {
        var endpoint = GetEnvironmentVariable("AZURE_LANGUAGE_ENDPOINT", "LANGUAGE_ENDPOINT");
        var key = GetEnvironmentVariable(
            "AZURE_LANGUAGE_KEY",
            "AZURE_LANGUAGE_API_KEY",
            "LANGUAGE_API_KEY"
        );

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var apiVersion =
            Environment.GetEnvironmentVariable("AZURE_LANGUAGE_CONVERSATION_PII_API_VERSION")
            ?? "2024-05-01";
        var modelVersion =
            Environment.GetEnvironmentVariable("AZURE_LANGUAGE_CONVERSATION_PII_MODEL_VERSION")
            ?? "2026-04-15-preview";

        var requestBody = new
        {
            displayName = "Smart Call Center PII redaction",
            analysisInput = new
            {
                conversations = new[]
                {
                    new
                    {
                        id = Guid.NewGuid().ToString("N"),
                        language = NormalizeLanguage(language),
                        modality = "text",
                        conversationItems = turns.Select((turn, index) => new
                        {
                            participantId = NormalizeParticipantId(turn.Speaker),
                            id = (index + 1).ToString(),
                            text = turn.Text,
                        }),
                    },
                },
            },
            tasks = new[]
            {
                new
                {
                    taskName = "conversational-pii",
                    kind = "ConversationalPIITask",
                    parameters = new
                    {
                        modelVersion,
                        piiCategories = new[] { "all" },
                    },
                },
            },
        };

        using var submitRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{endpoint.TrimEnd('/')}/language/analyze-conversations/jobs?api-version={apiVersion}"
        );
        submitRequest.Headers.Add("Ocp-Apim-Subscription-Key", key);
        submitRequest.Content = JsonContent.Create(requestBody);

        using var submitResponse = await Http.SendAsync(submitRequest, ct);
        if (!submitResponse.IsSuccessStatusCode)
        {
            return null;
        }

        if (!submitResponse.Headers.TryGetValues("operation-location", out var locations))
        {
            return null;
        }

        var operationLocation = locations.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(operationLocation))
        {
            return null;
        }

        var result = await PollResultAsync(operationLocation, key, ct);
        if (result is null)
        {
            return null;
        }

        var maskedTranscript = TryExtractMaskedTranscript(result) ?? MaskTranscriptLocally(turns).MaskedTranscript;
        var maskedText = TryGetFirstString(result, "redactedText") ?? string.Join("\n", turns.Select(t => $"{t.Speaker}: {t.Text}"));
        var signals = TryCollectSignals(result);

        return new PiiRedactionResult(maskedTranscript, maskedText, signals);
    }

    private static async Task<JsonNode?> PollResultAsync(
        string operationLocation,
        string key,
        CancellationToken ct
    )
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, operationLocation);
            request.Headers.Add("Ocp-Apim-Subscription-Key", key);

            using var response = await Http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var root = JsonNode.Parse(body);
            if (root is null)
            {
                return null;
            }

            var status = TryGetFirstString(root, "status") ?? TryGetFirstString(root, "state");
            if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            if (
                string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase)
            )
            {
                return null;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(400 + (attempt * 100)), ct);
        }

        return null;
    }

    private static PiiRedactionResult MaskTranscriptLocally(IEnumerable<CallTurn> transcript)
    {
        var signals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var maskedTranscript = new JsonArray();
        var lines = new List<string>();

        foreach (var turn in transcript)
        {
            var maskedText = MaskText(turn.Text, signals);
            maskedTranscript.Add(
                new JsonObject
                {
                    ["speaker"] = turn.Speaker,
                    ["text"] = maskedText,
                    ["at"] = turn.At,
                }
            );
            lines.Add($"{turn.Speaker}: {maskedText}");
        }

        return new PiiRedactionResult(maskedTranscript, string.Join("\n", lines), signals.ToArray());
    }

    private static JsonArray? TryExtractMaskedTranscript(JsonNode root)
    {
        if (root is JsonObject obj)
        {
            if (obj["conversationItems"] is JsonArray conversationItems)
            {
                var masked = new JsonArray();
                foreach (var entry in conversationItems)
                {
                    if (entry is JsonObject item)
                    {
                        var text = item["redactedText"]?.GetValue<string>() ?? item["text"]?.GetValue<string>() ?? string.Empty;
                        masked.Add(
                            new JsonObject
                            {
                                ["speaker"] = item["participantId"]?.GetValue<string>() ?? "unknown",
                                ["text"] = text,
                                ["at"] = item["at"]?.GetValue<string>() ?? string.Empty,
                            }
                        );
                    }
                }

                if (masked.Count > 0)
                {
                    return masked;
                }
            }

            foreach (var property in obj)
            {
                var nested = TryExtractMaskedTranscript(property.Value);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }
        else if (root is JsonArray array)
        {
            foreach (var item in array)
            {
                var nested = TryExtractMaskedTranscript(item);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static string? TryGetFirstString(JsonNode? node, string propertyName)
    {
        if (node is JsonObject obj)
        {
            if (obj[propertyName] is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            foreach (var property in obj)
            {
                var nested = TryGetFirstString(property.Value, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                var nested = TryGetFirstString(item, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<string> TryCollectSignals(JsonNode root)
    {
        var signals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectSignals(root, signals);
        return signals.ToArray();
    }

    private static void CollectSignals(JsonNode? node, ISet<string> signals)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in new[] { "category", "entityCategory", "subcategory" })
            {
                if (obj[key] is JsonValue value && value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text))
                {
                    signals.Add(text);
                }
            }

            foreach (var property in obj)
            {
                CollectSignals(property.Value, signals);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                CollectSignals(item, signals);
            }
        }
    }

    private static string? GetEnvironmentVariable(params string[] names)
    {
        foreach (var name in names)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string NormalizeLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ? "en" : language;

    private static string NormalizeParticipantId(string speaker) =>
        string.IsNullOrWhiteSpace(speaker) ? "participant" : speaker.ToLowerInvariant();

    private static string MaskText(string input, ISet<string> signals)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var masked = input;
        masked = Replace(masked, EmailRegex, "[EMAIL REDACTED]", signals, "email");
        masked = Replace(masked, PhoneRegex, "[PHONE REDACTED]", signals, "phone");
        masked = Replace(masked, CreditCardRegex, "[CARD REDACTED]", signals, "card");
        masked = Replace(masked, PostalCodeRegex, "[POSTAL CODE REDACTED]", signals, "postal_code");
        return masked;
    }

    private static string Replace(
        string input,
        Regex regex,
        string replacement,
        ISet<string> signals,
        string signalName
    )
    {
        var hit = false;
        var output = regex.Replace(
            input,
            _ =>
            {
                hit = true;
                return replacement;
            }
        );

        if (hit)
        {
            signals.Add(signalName);
        }

        return output;
    }
}
