using System.Net;
using System.Text.Json.Nodes;
using Azure.Identity;
using Microsoft.Azure.Cosmos;

namespace IntelligentCustomerOperations.PostCallWorker;

public sealed class EventProcessingStore
{
    private readonly Container? _container;

    public EventProcessingStore()
    {
        var endpoint = Environment.GetEnvironmentVariable("COSMOS_ENDPOINT");
        var databaseName = Environment.GetEnvironmentVariable("COSMOS_DATABASE_NAME");
        var containerName = Environment.GetEnvironmentVariable("COSMOS_CONTAINER_NAME");
        if (string.IsNullOrWhiteSpace(endpoint)
            || string.IsNullOrWhiteSpace(databaseName)
            || string.IsNullOrWhiteSpace(containerName))
        {
            return;
        }

        var client = new CosmosClient(endpoint, new DefaultAzureCredential());
        _container = client.GetDatabase(databaseName).GetContainer(containerName);
    }

    public async Task<bool> IsCompletedAsync(string eventId, CancellationToken ct)
    {
        if (_container is null)
        {
            return false;
        }

        var id = GetDocumentId(eventId);
        try
        {
            var response = await _container.ReadItemAsync<JsonObject>(
                id,
                new PartitionKey(id),
                cancellationToken: ct);
            return response.Resource["status"]?.GetValue<string>() == "completed";
        }
        catch (CosmosException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task MarkCompletedAsync(
        string eventId,
        string callId,
        JsonNode analytics,
        CancellationToken ct)
    {
        if (_container is null)
        {
            return;
        }

        var id = GetDocumentId(eventId);
        var document = new JsonObject
        {
            ["id"] = id,
            ["kind"] = "postCallProcessingResult",
            ["eventId"] = eventId,
            ["callId"] = callId,
            ["status"] = "completed",
            ["completedAt"] = DateTimeOffset.UtcNow.ToString("o"),
            ["analytics"] = analytics.DeepClone()
        };
        await _container.UpsertItemAsync(
            document,
            new PartitionKey(id),
            cancellationToken: ct);
    }

    private static string GetDocumentId(string eventId) => $"post-call:{eventId}";
}