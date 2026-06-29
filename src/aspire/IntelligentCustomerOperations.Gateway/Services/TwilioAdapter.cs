using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using IntelligentCustomerOperations.Gateway.Models;

namespace IntelligentCustomerOperations.Gateway.Services;

public sealed class TwilioAdapter : ICallChannelAdapter
{
    private readonly CallStore _store;
    private readonly AppConfig _config;
    private readonly ILogger<TwilioAdapter> _logger;

    public TwilioAdapter(CallStore store, AppConfig config, ILogger<TwilioAdapter> logger)
    {
        _store = store;
        _config = config;
        _logger = logger;
    }

    public async Task<object> HandleIncomingEventsAsync(JsonNode? events, CancellationToken ct = default)
    {
        var eventList = events is JsonArray array ? array.ToList() : [events];
        var created = new List<CallRecord>();

        foreach (var entry in eventList)
        {
            var data = entry ?? new JsonObject();
            var callSid =
                data?["CallSid"]?.GetValue<string>()
                ?? data?["callSid"]?.GetValue<string>()
                ?? Guid.NewGuid().ToString("N");
            var from =
                data?["From"]?.GetValue<string>()
                ?? data?["from"]?.GetValue<string>()
                ?? "unknown";
            var to = data?["To"]?.GetValue<string>() ?? data?["to"]?.GetValue<string>();
            var callStatus =
                data?["CallStatus"]?.GetValue<string>()
                ?? data?["callStatus"]?.GetValue<string>()
                ?? "incoming";

            // Use Twilio CallSid as call id so status callbacks can address the same record.
            var existing = await _store.GetCallAsync(callSid, ct);
            if (existing is not null)
            {
                await _store.UpdateCallAsync(
                    callSid,
                    c =>
                    {
                        c.Status = NormalizeStatus(callStatus);
                        c.Artifacts.Add(
                            new JsonObject
                            {
                                ["type"] = "twilio_incoming_event",
                                ["callSid"] = callSid,
                                ["from"] = from,
                                ["to"] = to,
                                ["status"] = callStatus,
                                ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                            }
                        );
                    },
                    ct
                );
                continue;
            }

            var createdCall = await _store.CreateCallAsync(
                new CallRecord
                {
                    Id = callSid,
                    AcsCallConnectionId = callSid,
                    CustomerPhoneNumber = from,
                    Language = "en",
                    Status = NormalizeStatus(callStatus),
                    Artifacts =
                    [
                        new JsonObject
                        {
                            ["type"] = "twilio_incoming_event",
                            ["callSid"] = callSid,
                            ["from"] = from,
                            ["to"] = to,
                            ["status"] = callStatus,
                            ["provider"] = "twilio",
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        },
                    ],
                },
                ct
            );

            _logger.LogInformation(
                "Twilio incoming call accepted. callId={CallId} from={From} status={Status}",
                createdCall.Id,
                from,
                callStatus
            );
            created.Add(createdCall);
        }

        return new { accepted = created.Count, calls = created, provider = "twilio" };
    }

    public async Task<object> HandleCallCallbackAsync(
        string callId,
        JsonNode? payload,
        CancellationToken ct = default
    )
    {
        var callSid =
            payload?["CallSid"]?.GetValue<string>()
            ?? payload?["callSid"]?.GetValue<string>()
            ?? callId;
        var callStatus =
            payload?["CallStatus"]?.GetValue<string>()
            ?? payload?["callStatus"]?.GetValue<string>()
            ?? payload?["CallEvent"]?.GetValue<string>()
            ?? payload?["callEvent"]?.GetValue<string>()
            ?? "unknown";

        var call = await _store.GetCallAsync(callSid, ct);
        if (call is null)
        {
            return new
            {
                updated = false,
                reason = "call_not_found",
                callId = callSid,
                provider = "twilio",
            };
        }

        await _store.UpdateCallAsync(
            call.Id,
            c =>
            {
                c.Status = NormalizeStatus(callStatus);
                if (IsTerminalStatus(callStatus))
                {
                    c.CompletedAt = DateTimeOffset.UtcNow.ToString("o");
                }

                c.Artifacts.Add(
                    new JsonObject
                    {
                        ["type"] = "twilio_status_callback",
                        ["callSid"] = callSid,
                        ["status"] = callStatus,
                        ["payload"] = payload?.DeepClone(),
                        ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                    }
                );
            },
            ct
        );

        return new { updated = true, callId = call.Id, status = callStatus, provider = "twilio" };
    }

    public async Task<CallRecord> SimulateCallAsync(string language, CancellationToken ct = default)
    {
        var syntheticSid = $"SIM-{Guid.NewGuid():N}";
        var call = await _store.CreateCallAsync(
            new CallRecord
            {
                Id = syntheticSid,
                AcsCallConnectionId = syntheticSid,
                CustomerPhoneNumber = _config.Twilio.PhoneNumber,
                Language = string.IsNullOrWhiteSpace(language) ? "en" : language,
                Status = "active",
                Artifacts =
                [
                    new JsonObject
                    {
                        ["type"] = "twilio_simulated_call",
                        ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                    },
                ],
            },
            ct
        );

        return call;
    }

    private static string NormalizeStatus(string rawStatus)
    {
        return rawStatus.ToLowerInvariant() switch
        {
            "queued" => "queued",
            "ringing" => "ringing",
            "in-progress" => "active",
            "in_progress" => "active",
            "answered" => "active",
            "completed" => "completed",
            "busy" => "failed",
            "failed" => "failed",
            "no-answer" => "missed",
            "no_answer" => "missed",
            "canceled" => "cancelled",
            _ => rawStatus,
        };
    }

    private static bool IsTerminalStatus(string rawStatus)
    {
        var normalized = rawStatus.ToLowerInvariant();
        return normalized is "completed" or "busy" or "failed" or "no-answer" or "no_answer" or "canceled";
    }
}
