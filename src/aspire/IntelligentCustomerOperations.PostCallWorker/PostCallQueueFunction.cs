using System.Text.Json.Nodes;
using System.Text.Json;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace IntelligentCustomerOperations.PostCallWorker;

public sealed class PostCallQueueFunction(CallAnalyzer analyzer, ILogger<PostCallQueueFunction> logger)
{
    [Function("postCallQueue")]
    public async Task RunAsync(
        [QueueTrigger("%POST_CALL_QUEUE_NAME%", Connection = "AzureWebJobsStorage")]
        BinaryData message,
        CancellationToken ct)
    {
        var failOnError = string.Equals(
            Environment.GetEnvironmentVariable("POST_CALL_FAIL_ON_ERROR"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        try
        {
            var payload = message.ToString();
            var call = TryParseCall(payload);
            if (call is null)
            {
                logger.LogError("Queue payload is not valid JSON: {Payload}", payload);
                return;
            }

            var callId = call["id"]?.GetValue<string>() ?? "unknown";
            logger.LogInformation("Processing post-call analytics job for call {CallId}", callId);
            var analytics = await analyzer.AnalyzeCallAsync(call, ct);

            logger.LogInformation(
                "Post-call analytics completed for call {CallId}, resolution={Resolution}, redaction={Redaction}",
                callId,
                analytics["resolutionStatus"]?.GetValue<string>() ?? "unknown",
                analytics["redactionStatus"]?.GetValue<string>() ?? "unknown");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Post-call analytics job failed.");
            if (failOnError)
            {
                throw;
            }
        }
    }

    private static JsonNode? TryParseCall(string payload)
    {
        try
        {
            return JsonNode.Parse(payload);
        }
        catch (JsonException)
        {
            try
            {
                var unwrapped = JsonSerializer.Deserialize<string>(payload);
                return string.IsNullOrEmpty(unwrapped) ? null : JsonNode.Parse(unwrapped);
            }
            catch
            {
                return null;
            }
        }
    }
}

