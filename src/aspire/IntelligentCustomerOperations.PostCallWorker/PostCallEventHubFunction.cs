using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace IntelligentCustomerOperations.PostCallWorker;

public sealed class PostCallEventHubFunction(
    CallAnalyzer analyzer,
    CaseDecisionService caseDecisionService,
    EventProcessingStore processingStore,
    ILogger<PostCallEventHubFunction> logger)
{
    [Function("postCallEventHub")]
    public async Task RunAsync(
        [EventHubTrigger(
            "%POST_CALL_EVENT_HUB_NAME%",
            Connection = "PostCallEventHub",
            ConsumerGroup = "%POST_CALL_EVENT_HUB_CONSUMER_GROUP%")]
        string[] messages,
        CancellationToken ct)
    {
        foreach (var message in messages)
        {
            var envelope = TryParseEvent(message)
                ?? throw new InvalidOperationException("Call-ended event payload is not valid JSON.");
            ValidateEnvelope(envelope);

            var callId = envelope["callId"]!.GetValue<string>();
            var eventId = envelope["eventId"]!.GetValue<string>();
            if (await processingStore.IsCompletedAsync(eventId, ct))
            {
                logger.LogInformation(
                    "Skipping completed duplicate event {EventId} for call {CallId}",
                    eventId,
                    callId);
                continue;
            }

            logger.LogInformation(
                "Processing call-ended event {EventId} for call {CallId}",
                eventId,
                callId);

            var call = new JsonObject
            {
                ["id"] = callId,
                ["language"] = envelope["language"]?.DeepClone(),
                ["transcript"] = envelope["transcript"]?.DeepClone(),
                ["artifacts"] = envelope["artifactReferences"]?.DeepClone()
            };
            var analytics = await analyzer.AnalyzeCallAsync(call, ct);
            var result = await caseDecisionService.ApplyAsync(callId, analytics, ct);
            await processingStore.MarkCompletedAsync(eventId, callId, result, ct);

            logger.LogInformation(
                "Post-call analytics completed for call {CallId}, resolution={Resolution}, redaction={Redaction}",
                callId,
                analytics["resolutionStatus"]?.GetValue<string>() ?? "unknown",
                analytics["redactionStatus"]?.GetValue<string>() ?? "unknown");
        }
    }

    private static JsonNode? TryParseEvent(string payload)
    {
        try
        {
            return JsonNode.Parse(payload);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void ValidateEnvelope(JsonNode envelope)
    {
        if (envelope["schemaVersion"]?.GetValue<string>() != "1.0"
            || envelope["eventType"]?.GetValue<string>() != "customer.call.ended"
            || string.IsNullOrWhiteSpace(envelope["eventId"]?.GetValue<string>())
            || string.IsNullOrWhiteSpace(envelope["callId"]?.GetValue<string>())
            || envelope["transcript"] is not JsonArray)
        {
            throw new InvalidOperationException("Call-ended event does not match schema version 1.0.");
        }
    }
}