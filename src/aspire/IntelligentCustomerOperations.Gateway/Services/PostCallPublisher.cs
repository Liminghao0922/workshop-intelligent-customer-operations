using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using IntelligentCustomerOperations.Gateway.Models;

namespace IntelligentCustomerOperations.Gateway.Services;

public sealed class PostCallPublisher(AppConfig config)
{
    public async Task<JsonNode> PublishAsync(CallRecord call, CancellationToken ct = default)
    {
        var hasConnectionString = !string.IsNullOrEmpty(config.PostCall.ConnectionString);
        var hasManagedIdentityConfig = !string.IsNullOrEmpty(config.PostCall.FullyQualifiedNamespace);

        if (!hasConnectionString && !hasManagedIdentityConfig)
        {
            return new JsonObject
            {
                ["mode"] = "mock",
                ["submitted"] = true,
                ["callId"] = call.Id,
                ["note"] = "Set POST_CALL_EVENT_HUB_CONNECTION_STRING or POST_CALL_EVENT_HUB_FULLY_QUALIFIED_NAMESPACE to publish call-ended events."
            };
        }

        await using var producer = hasConnectionString
            ? new EventHubProducerClient(config.PostCall.ConnectionString, config.PostCall.EventHubName)
            : new EventHubProducerClient(
                config.PostCall.FullyQualifiedNamespace,
                config.PostCall.EventHubName,
                new DefaultAzureCredential());

        var eventId = $"{call.Id}:ended";
        var envelope = new JsonObject
        {
            ["schemaVersion"] = "1.0",
            ["eventId"] = eventId,
            ["eventType"] = "customer.call.ended",
            ["occurredAt"] = call.CompletedAt ?? DateTimeOffset.UtcNow.ToString("o"),
            ["callId"] = call.Id,
            ["language"] = call.Language,
            ["transcript"] = JsonSerializer.SerializeToNode(call.Transcript),
            ["artifactReferences"] = JsonSerializer.SerializeToNode(call.Artifacts)
        };
        var eventData = new EventData(BinaryData.FromObjectAsJson(envelope))
        {
            ContentType = "application/json",
            MessageId = eventId
        };
        await producer.SendAsync(
            [eventData],
            new SendEventOptions { PartitionKey = call.Id },
            ct);

        return new JsonObject
        {
            ["mode"] = hasConnectionString ? "local" : "azure",
            ["submitted"] = true,
            ["callId"] = call.Id,
            ["eventId"] = eventId,
            ["eventHub"] = config.PostCall.EventHubName
        };
    }
}

