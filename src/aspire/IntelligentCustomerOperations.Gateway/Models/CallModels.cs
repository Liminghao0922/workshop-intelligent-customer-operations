using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace IntelligentCustomerOperations.Gateway.Models;

public sealed class CallTurn
{
    [JsonPropertyName("speaker")]
    public string Speaker { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonPropertyName("at")]
    public string At { get; set; } = "";
}

public sealed class Ticket
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("callId")]
    public string? CallId { get; set; }

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "medium";

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "human_handoff";

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = "";

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "open";
}

public sealed class CallRecord
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";

    [JsonPropertyName("customerPhoneNumber")]
    public string CustomerPhoneNumber { get; set; } = "unknown";

    [JsonPropertyName("acsCallConnectionId")]
    public string AcsCallConnectionId { get; set; } = "";

    [JsonPropertyName("recordingId")]
    public string? RecordingId { get; set; }

    [JsonPropertyName("recordingState")]
    public string? RecordingState { get; set; }

    [JsonPropertyName("startedAt")]
    public string StartedAt { get; set; } = "";

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = "";

    [JsonPropertyName("completedAt")]
    public string? CompletedAt { get; set; }

    [JsonPropertyName("transcript")]
    public List<CallTurn> Transcript { get; set; } = [];

    [JsonPropertyName("artifacts")]
    public List<JsonNode?> Artifacts { get; set; } = [];

    [JsonPropertyName("analyticsStatus")]
    public string AnalyticsStatus { get; set; } = "not_started";

    [JsonPropertyName("ticket")]
    public Ticket? Ticket { get; set; }

    [JsonPropertyName("postCallResult")]
    public JsonNode? PostCallResult { get; set; }

    [JsonPropertyName("foundryConversationId")]
    public string? FoundryConversationId { get; set; }
}

public sealed class KnowledgeDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("language")]
    public string Language { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("sourceUrl")]
    public string? SourceUrl { get; set; }
}

