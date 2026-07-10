namespace IntelligentCustomerOperations.Gateway;

public sealed class AppConfig
{
    public string Mode { get; init; } = "mock";
    public string PublicBaseUrl { get; init; } = "";
    public AcsConfig Acs { get; init; } = new();
    public FoundryConfig Foundry { get; init; } = new();
    public SearchConfig Search { get; init; } = new();
    public StorageConfig Storage { get; init; } = new();
    public PostCallConfig PostCall { get; init; } = new();
    public CallbackConfig Callback { get; init; } = new();
    public RecordingConfig Recording { get; init; } = new();
    public TwilioConfig Twilio { get; init; } = new();
    public DynamicsConfig Dynamics { get; init; } = new();
    public CosmosConfig Cosmos { get; init; } = new();

    public bool IsAzureMode => string.Equals(Mode, "azure", StringComparison.OrdinalIgnoreCase);

    public static AppConfig Load() => new()
    {
        Mode = Env("APP_MODE", "mock"),
        PublicBaseUrl = Env("PUBLIC_BASE_URL", ""),
        Acs = new AcsConfig
        {
            ConnectionString = Env("ACS_CONNECTION_STRING", ""),
            CallbackSecret = Env("ACS_CALLBACK_SECRET", ""),
            HumanAgentPhoneNumber = Env("ACS_HUMAN_AGENT_PHONE_NUMBER", ""),
            TransferSourcePhoneNumber = Env("ACS_TRANSFER_SOURCE_PHONE_NUMBER", ""),
            VoiceName = Env("ACS_VOICE_NAME", "en-US-AriaNeural")
        },
        Foundry = new FoundryConfig
        {
            ProjectEndpoint = Env("AZURE_AI_PROJECT_ENDPOINT", ""),
            AgentId = Env("FOUNDRY_AGENT_ID", ""),
            AnalyticsAgentId = Env("FOUNDRY_ANALYTICS_AGENT_ID", Env("FOUNDRY_AGENT_ID", "")),
            VoiceLiveEndpoint = Env("VOICE_LIVE_ENDPOINT", ""),
            VoiceLiveModel = Env("VOICE_LIVE_MODEL", ""),
            ApiVersion = Env("FOUNDRY_API_VERSION", "v1"),
            UseAgentInvoke = bool.TryParse(Env("FOUNDRY_USE_AGENT_INVOKE", "false"), out var useAgentInvoke) && useAgentInvoke
        },
        Search = new SearchConfig
        {
            Endpoint = Env("AZURE_SEARCH_ENDPOINT", ""),
            IndexName = Env("AZURE_SEARCH_INDEX_NAME", "customer-operations-knowledge"),
            ApiKey = Env("AZURE_SEARCH_API_KEY", "")
        },
        Storage = new StorageConfig
        {
            AccountName = Env("AZURE_STORAGE_ACCOUNT_NAME", ""),
            ArtifactContainer = Env("CALL_ARTIFACT_CONTAINER", "call-artifacts"),
            ConnectionString = Env("AZURE_STORAGE_CONNECTION_STRING", Env("AzureWebJobsStorage", ""))
        },
        PostCall = new PostCallConfig
        {
            ConnectionString = Env("POST_CALL_EVENT_HUB_CONNECTION_STRING", ""),
            FullyQualifiedNamespace = Env("POST_CALL_EVENT_HUB_FULLY_QUALIFIED_NAMESPACE", ""),
            EventHubName = Env("POST_CALL_EVENT_HUB_NAME", "call-ended")
        },
        Callback = new CallbackConfig
        {
            QueueName = Env("CALLBACK_QUEUE_NAME", "callback-jobs"),
            PromisedWaitMinutes = int.TryParse(Env("CALLBACK_PROMISED_WAIT_MINUTES", "15"), out var minutes) ? minutes : 15
        },
        Recording = new RecordingConfig
        {
            Enabled = bool.TryParse(Env("ACS_RECORDING_ENABLED", "false"), out var enabled) && enabled,
            BlobContainerUri = Env(
                "ACS_RECORDING_BLOB_CONTAINER_URI",
                !string.IsNullOrWhiteSpace(Env("AZURE_STORAGE_ACCOUNT_NAME", ""))
                    ? $"https://{Env("AZURE_STORAGE_ACCOUNT_NAME", "")}.blob.core.windows.net/{Env("CALL_ARTIFACT_CONTAINER", "call-artifacts")}" 
                    : ""
            )
        },
        Twilio = new TwilioConfig
        {
            AccountSid = Env("TWILIO_ACCOUNT_SID", ""),
            AuthToken = Env("TWILIO_AUTH_TOKEN", ""),
            PhoneNumber = Env("TWILIO_PHONE_NUMBER", "unknown")
        },
        Dynamics = new DynamicsConfig
        {
            OrganizationUrl = Env("DYNAMICS_ORGANIZATION_URL", ""),
            ConnectionString = Env("DYNAMICS_CONNECTION_STRING", ""),
            TenantId = Env("DYNAMICS_TENANT_ID", ""),
            ClientId = Env("DYNAMICS_CLIENT_ID", ""),
            ClientSecret = Env("DYNAMICS_CLIENT_SECRET", ""),
            ManagedIdentityClientId = Env("DYNAMICS_MANAGED_IDENTITY_CLIENT_ID", "")
        },
        Cosmos = new CosmosConfig
        {
            Endpoint = Env("COSMOS_ENDPOINT", ""),
            ConnectionString = Env("COSMOS_CONNECTION_STRING", ""),
            DatabaseName = Env("COSMOS_DATABASE_NAME", "smart-call-center"),
            ContainerName = Env("COSMOS_CONTAINER_NAME", "call-sessions"),
            ManagedIdentityClientId = Env("COSMOS_MANAGED_IDENTITY_CLIENT_ID", ""),
            PreferredRegion = Env("COSMOS_PREFERRED_REGION", Env("AZURE_LOCATION", ""))
        }
    };

    private static string Env(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;
}

public sealed class AcsConfig
{
    public string ConnectionString { get; init; } = "";
    public string CallbackSecret { get; init; } = "";
    public string HumanAgentPhoneNumber { get; init; } = "";
    public string TransferSourcePhoneNumber { get; init; } = "";
    public string VoiceName { get; init; } = "en-US-AriaNeural";
}

public sealed class FoundryConfig
{
    public string ProjectEndpoint { get; init; } = "";
    public string AgentId { get; init; } = "";
    public string AnalyticsAgentId { get; init; } = "";
    public string VoiceLiveEndpoint { get; init; } = "";
    public string VoiceLiveModel { get; init; } = "";
    public string ApiVersion { get; init; } = "v1";
    public bool UseAgentInvoke { get; init; }
}

public sealed class SearchConfig
{
    public string Endpoint { get; init; } = "";
    public string IndexName { get; init; } = "customer-operations-knowledge";
    public string ApiKey { get; init; } = "";
}

public sealed class StorageConfig
{
    public string AccountName { get; init; } = "";
    public string ArtifactContainer { get; init; } = "call-artifacts";
    public string ConnectionString { get; init; } = "";
}

public sealed class PostCallConfig
{
    public string ConnectionString { get; init; } = "";
    public string FullyQualifiedNamespace { get; init; } = "";
    public string EventHubName { get; init; } = "call-ended";
}

public sealed class CallbackConfig
{
    public string QueueName { get; init; } = "callback-jobs";
    public int PromisedWaitMinutes { get; init; } = 15;
}

public sealed class RecordingConfig
{
    public bool Enabled { get; init; }
    public string BlobContainerUri { get; init; } = "";
}

public sealed class TwilioConfig
{
    public string AccountSid { get; init; } = "";
    public string AuthToken { get; init; } = "";
    public string PhoneNumber { get; init; } = "unknown";
}

public sealed class DynamicsConfig
{
    public string OrganizationUrl { get; init; } = "";
    public string ConnectionString { get; init; } = "";
    public string TenantId { get; init; } = "";
    public string ClientId { get; init; } = "";
    public string ClientSecret { get; init; } = "";
    public string ManagedIdentityClientId { get; init; } = "";
}

public sealed class CosmosConfig
{
    public string Endpoint { get; init; } = "";
    public string ConnectionString { get; init; } = "";
    public string DatabaseName { get; init; } = "smart-call-center";
    public string ContainerName { get; init; } = "call-sessions";
    public string ManagedIdentityClientId { get; init; } = "";
    public string PreferredRegion { get; init; } = "";
}

