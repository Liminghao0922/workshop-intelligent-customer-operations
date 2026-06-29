using System.Text.Json.Nodes;
using Azure.Identity;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using IntelligentCustomerOperations.Gateway.Models;

namespace IntelligentCustomerOperations.Gateway.Services;

public sealed class CallbackQueuePublisher(AppConfig config)
{
    public async Task<JsonNode> PublishAsync(
        CallRecord call,
        Ticket ticket,
        string reason,
        CancellationToken ct = default
    )
    {
        var payload = new JsonObject
        {
            ["callId"] = call.Id,
            ["ticketId"] = ticket.Id,
            ["customerPhoneNumber"] = call.CustomerPhoneNumber,
            ["language"] = call.Language,
            ["reason"] = reason,
            ["queuedAt"] = DateTimeOffset.UtcNow.ToString("o"),
        };

        var hasConnectionString = !string.IsNullOrEmpty(config.Storage.ConnectionString);
        var hasManagedIdentityQueueConfig = !string.IsNullOrEmpty(config.Storage.AccountName);

        if (!hasConnectionString && !hasManagedIdentityQueueConfig)
        {
            return new JsonObject
            {
                ["mode"] = "mock",
                ["submitted"] = true,
                ["queue"] = config.Callback.QueueName,
                ["payload"] = payload.DeepClone(),
            };
        }

        QueueClient queueClient;
        if (hasConnectionString)
        {
            queueClient = new QueueClient(
                config.Storage.ConnectionString,
                config.Callback.QueueName,
                new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }
            );
        }
        else
        {
            queueClient = new QueueClient(
                new Uri(
                    $"https://{config.Storage.AccountName}.queue.core.windows.net/{config.Callback.QueueName}"
                ),
                new DefaultAzureCredential(),
                new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }
            );
        }

        await queueClient.CreateIfNotExistsAsync(cancellationToken: ct);
        await queueClient.SendMessageAsync(payload.ToJsonString(), cancellationToken: ct);

        return new JsonObject
        {
            ["mode"] = hasConnectionString ? "local" : "azure",
            ["submitted"] = true,
            ["queue"] = config.Callback.QueueName,
            ["callId"] = call.Id,
            ["ticketId"] = ticket.Id,
        };
    }
}

