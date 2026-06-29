using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using IntelligentCustomerOperations.Gateway.Models;

namespace IntelligentCustomerOperations.Gateway.Services;

public sealed class StorageRepository(AppConfig config)
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public async Task<object> SaveCallArtifactAsync(CallRecord call, CancellationToken ct = default)
    {
        if (!config.IsAzureMode)
        {
            return new { mode = "mock", path = $"mock://{call.Id}.json" };
        }

        if (string.IsNullOrEmpty(config.Storage.AccountName))
        {
            throw new InvalidOperationException("AZURE_STORAGE_ACCOUNT_NAME is required in azure mode.");
        }

        var serviceClient = new BlobServiceClient(
            new Uri($"https://{config.Storage.AccountName}.blob.core.windows.net"),
            new DefaultAzureCredential());
        var container = serviceClient.GetBlobContainerClient(config.Storage.ArtifactContainer);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);

        var blob = container.GetBlobClient($"{call.Id}/call.json");
        var payload = JsonSerializer.Serialize(call, Indented);
        await blob.UploadAsync(
            new BinaryData(Encoding.UTF8.GetBytes(payload)),
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" }
            },
            ct);
        return new { mode = "azure", path = blob.Uri.ToString() };
    }
}

