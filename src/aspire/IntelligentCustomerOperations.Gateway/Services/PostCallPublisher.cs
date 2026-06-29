using System.Text.Json.Nodes;
using Azure.Identity;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using IntelligentCustomerOperations.Gateway.Models;

namespace IntelligentCustomerOperations.Gateway.Services;

public sealed class PostCallPublisher(AppConfig config)
{
    public async Task<JsonNode> PublishAsync(CallRecord call, CancellationToken ct = default)
    {
        var hasConnectionString = !string.IsNullOrEmpty(config.Storage.ConnectionString);
        var hasManagedIdentityQueueConfig = !string.IsNullOrEmpty(config.Storage.AccountName);

        if (!hasConnectionString && !hasManagedIdentityQueueConfig)
        {
            return new JsonObject
            {
                ["mode"] = "mock",
                ["submitted"] = true,
                ["callId"] = call.Id,
                ["note"] = "Set AZURE_STORAGE_CONNECTION_STRING or AZURE_STORAGE_ACCOUNT_NAME to enqueue post-call analytics jobs."
            };
        }

        QueueClient queueClient;
        if (hasConnectionString)
        {
            queueClient = new QueueClient(
                config.Storage.ConnectionString,
                config.PostCall.QueueName,
                new QueueClientOptions
                {
                    MessageEncoding = QueueMessageEncoding.Base64
                });
        }
        else
        {
            queueClient = new QueueClient(
                new Uri($"https://{config.Storage.AccountName}.queue.core.windows.net/{config.PostCall.QueueName}"),
                new DefaultAzureCredential(),
                new QueueClientOptions
                {
                    MessageEncoding = QueueMessageEncoding.Base64
                });
        }

        await queueClient.CreateIfNotExistsAsync(cancellationToken: ct);
        await queueClient.SendMessageAsync(BinaryData.FromObjectAsJson(call).ToString(), cancellationToken: ct);

        return new JsonObject
        {
            ["mode"] = hasConnectionString ? "local" : "azure",
            ["submitted"] = true,
            ["callId"] = call.Id,
            ["queue"] = config.PostCall.QueueName
        };
    }
}

