using System.Text.Json.Nodes;
using IntelligentCustomerOperations.Gateway.Models;

namespace IntelligentCustomerOperations.Gateway.Services;

/// <summary>
/// Provider-agnostic abstraction for the telephony channel.
/// Current implementation: ACS (AcsAdapter).
/// Future implementations: Twilio, Teams Phone Extensibility, etc.
/// </summary>
public interface ICallChannelAdapter
{
    /// <summary>
    /// Handle an incoming call event (e.g. EventGrid webhook from ACS, or Twilio StatusCallback).
    /// Returns a provider-specific acknowledgement payload.
    /// </summary>
    Task<object> HandleIncomingEventsAsync(JsonNode? events, CancellationToken ct = default);

    /// <summary>
    /// Handle mid-call or post-call callback events for a specific call.
    /// </summary>
    Task<object> HandleCallCallbackAsync(string callId, JsonNode? payload, CancellationToken ct = default);

    /// <summary>
    /// Trigger a synthetic inbound call for local development / demo without real telephony.
    /// </summary>
    Task<CallRecord> SimulateCallAsync(string language, CancellationToken ct = default);
}

