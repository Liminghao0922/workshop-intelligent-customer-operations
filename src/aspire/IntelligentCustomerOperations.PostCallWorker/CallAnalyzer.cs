using System.Text.Json.Nodes;
using System.Net.Http.Json;
using Azure.Core;
using Azure.Identity;

namespace IntelligentCustomerOperations.PostCallWorker;

public sealed class CallAnalyzer(IHttpClientFactory httpClientFactory)
{
    public async Task<JsonNode> AnalyzeCallAsync(JsonNode call, CancellationToken ct = default)
    {
        var redaction = await PiiMasker.MaskTranscriptAsync(call["transcript"], call["language"]?.GetValue<string>(), ct);
        var mode = Environment.GetEnvironmentVariable("APP_MODE") ?? "mock";
        if (!string.Equals(mode, "azure", StringComparison.OrdinalIgnoreCase))
        {
            return MockAnalyze(call, redaction);
        }

        var endpoint = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT");
        var agentId = Environment.GetEnvironmentVariable("FOUNDRY_ANALYTICS_AGENT_ID")
            ?? Environment.GetEnvironmentVariable("FOUNDRY_AGENT_ID");
        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(agentId))
        {
            throw new InvalidOperationException("AZURE_AI_PROJECT_ENDPOINT and FOUNDRY_ANALYTICS_AGENT_ID are required.");
        }

        TokenCredential credential = Environment.GetEnvironmentVariable("MSI_ENDPOINT") is { Length: > 0 }
            ? new ManagedIdentityCredential()
            : new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(new TokenRequestContext(["https://ai.azure.com/.default"]), ct);

        var apiVersion = Environment.GetEnvironmentVariable("FOUNDRY_API_VERSION") ?? "2025-05-01";
        var http = httpClientFactory.CreateClient("foundry");
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{endpoint.TrimEnd('/')}/agents/{agentId}/invoke?api-version={apiVersion}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
        request.Content = JsonContent.Create(new
        {
            input = redaction.MaskedText,
            metadata = new
            {
                callId = call["id"]?.GetValue<string>(),
                language = call["language"]?.GetValue<string>(),
                purpose = "post_call_analytics",
                redactionStatus = redaction.Applied ? "applied" : "not_needed",
                redactionSignals = redaction.Signals
            }
        });

        var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Foundry analytics invocation failed: {(int)response.StatusCode} {body}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        return AttachRedactionMetadata(JsonNode.Parse(json) ?? new JsonObject(), redaction);
    }

    private static JsonObject MockAnalyze(JsonNode call, PiiRedactionResult redaction) => new()
    {
        ["summary"] = "Customer reported a subscription billing issue and requested billing support follow-up.",
        ["intent"] = "billing_dispute",
        ["sentiment"] = "concerned",
        ["entities"] = new JsonArray("Contoso Care", "subscription", "duplicate charge"),
        ["actionItems"] = new JsonArray("Create billing ticket", "Verify transactions", "Follow up with refund decision"),
        ["resolutionStatus"] = "unresolved",
        ["followUpRequired"] = true,
        ["followUpReason"] = "Billing specialist must review the duplicate charge.",
        ["priority"] = "medium",
        ["confidence"] = 0.92,
        ["redactionStatus"] = redaction.Applied ? "applied" : "not_needed",
        ["redactionSignals"] = new JsonArray(
            redaction.Signals.Select(signal => (JsonNode?)JsonValue.Create(signal)).ToArray()
        ),
        ["maskedTranscript"] = redaction.MaskedTranscript
    };

    private static JsonNode AttachRedactionMetadata(JsonNode analysis, PiiRedactionResult redaction)
    {
        var output = analysis as JsonObject ?? new JsonObject { ["analysis"] = analysis.DeepClone() };
        output["redactionStatus"] = redaction.Applied ? "applied" : "not_needed";
        output["redactionSignals"] = new JsonArray(
            redaction.Signals.Select(signal => (JsonNode?)JsonValue.Create(signal)).ToArray()
        );
        output["maskedTranscript"] = redaction.MaskedTranscript;
        return output;
    }
}

