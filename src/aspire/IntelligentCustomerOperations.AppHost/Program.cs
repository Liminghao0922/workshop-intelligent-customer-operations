if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1:18888");
}

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"))
    && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL")))
{
    Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", "http://127.0.0.1:18889");
}

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT")))
{
    Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
}

var builder = DistributedApplication.CreateBuilder(args);

var storageAccountName = Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME") ?? "";
var callArtifactContainer =
    Environment.GetEnvironmentVariable("CALL_ARTIFACT_CONTAINER") ?? "call-artifacts";
var recordingContainerUri = Environment.GetEnvironmentVariable("ACS_RECORDING_BLOB_CONTAINER_URI");

if (string.IsNullOrWhiteSpace(recordingContainerUri) && !string.IsNullOrWhiteSpace(storageAccountName))
{
    recordingContainerUri =
        $"https://{storageAccountName}.blob.core.windows.net/{callArtifactContainer}";
}

var gateway = builder
    .AddProject<Projects.IntelligentCustomerOperations_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithEnvironment("POST_CALL_QUEUE_NAME", "post-call-jobs")
    .WithEnvironment(
        "ACS_RECORDING_ENABLED",
        Environment.GetEnvironmentVariable("ACS_RECORDING_ENABLED") ?? "true"
    )
    .WithEnvironment("ACS_RECORDING_BLOB_CONTAINER_URI", recordingContainerUri ?? "")
    .WithEnvironment("CALL_ARTIFACT_CONTAINER", callArtifactContainer)
    .WithEnvironment("AZURE_STORAGE_ACCOUNT_NAME", storageAccountName);

var api = builder
    .AddProject<Projects.IntelligentCustomerOperations_ApiService>("api")
    .WithExternalHttpEndpoints()
    .WithEnvironment("GATEWAY_API_BASE_URL", gateway.GetEndpoint("http"));

builder
    .AddProject<Projects.IntelligentCustomerOperations_Portal>("portal")
    .WithExternalHttpEndpoints()
    .WithEnvironment("BACKEND_API_BASE_URL", api.GetEndpoint("http"));

builder.AddProject<Projects.IntelligentCustomerOperations_PostCallWorker>("postcall-worker");

builder.Build().Run();

