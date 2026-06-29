using System.Text.RegularExpressions;
using CustomerOperations.Functions;
using CustomerOperations.Knowledge;

namespace CustomerOperations.Agent;

public sealed class MockFoundryAgentAdapter : IFoundryAgentAdapter
{
    private static readonly Regex ServiceRequestIdRegex = new(@"SR-\d{4,}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly IKnowledgeAdapter _knowledgeAdapter;
    private readonly IServiceRequestStatusTool _serviceRequestStatusTool;

    public MockFoundryAgentAdapter(IKnowledgeAdapter knowledgeAdapter, IServiceRequestStatusTool serviceRequestStatusTool)
    {
        _knowledgeAdapter = knowledgeAdapter;
        _serviceRequestStatusTool = serviceRequestStatusTool;
    }

    public Task<AgentResult> ProcessAsync(string message, string correlationId, CancellationToken cancellationToken = default)
    {
        var normalized = message.Trim();
        var lowered = normalized.ToLowerInvariant();

        // TODO(v4): replace with real Foundry Agent orchestration + tool calls.
        if (IsEscalationRequest(lowered))
        {
            return Task.FromResult(new AgentResult(
                "I understand this has happened repeatedly. I recommend escalation to a human specialist now. Please share your service request ID if available, and we will prioritize follow-up.",
                "escalation_request",
                false,
                null));
        }

        if (IsStatusIntent(lowered))
        {
            var requestId = ExtractServiceRequestId(normalized);
            if (string.IsNullOrWhiteSpace(requestId))
            {
                return Task.FromResult(new AgentResult(
                    "I can help with that. Please provide your service request ID in the format SR-1001.",
                    "missing_information",
                    false,
                    null));
            }

            var status = _serviceRequestStatusTool.GetStatus(requestId);
            return Task.FromResult(new AgentResult(
                $"Service request {requestId.ToUpperInvariant()} is currently {status.Status}. Next step: {status.NextStep}.",
                "service_request_status",
                true,
                "getServiceRequestStatus"));
        }

        var knowledge = _knowledgeAdapter.GetAnswer(normalized);
        if (knowledge.Found)
        {
            return Task.FromResult(new AgentResult(
                knowledge.Answer,
                "faq_request",
                false,
                null));
        }

        return Task.FromResult(new AgentResult(
            "I can help with product, warranty, support, service status, or escalation requests. Please share your question with details.",
            "missing_information",
            false,
            null));
    }

    private static bool IsStatusIntent(string lowered) =>
        lowered.Contains("status") || lowered.Contains("repair") || lowered.Contains("service request");

    private static bool IsEscalationRequest(string lowered) =>
        lowered.Contains("escalation") || lowered.Contains("third time") || lowered.Contains("urgent");

    private static string? ExtractServiceRequestId(string text)
    {
        var match = ServiceRequestIdRegex.Match(text);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }
}
