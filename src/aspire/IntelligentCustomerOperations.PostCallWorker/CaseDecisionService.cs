using System.Text.Json.Nodes;

namespace IntelligentCustomerOperations.PostCallWorker;

public sealed class CaseDecisionService(DynamicsCaseClient dynamicsCaseClient)
{
    private const double MinimumConfidence = 0.70;

    public async Task<JsonNode> ApplyAsync(string callId, JsonNode analytics, CancellationToken ct)
    {
        var result = analytics as JsonObject
            ?? new JsonObject { ["analysis"] = analytics.DeepClone() };
        var followUpRequired = result["followUpRequired"]?.GetValue<bool>() == true;
        var resolutionStatus = result["resolutionStatus"]?.GetValue<string>();
        var confidence = result["confidence"]?.GetValue<double>() ?? 0;
        var unresolved = string.Equals(resolutionStatus, "unresolved", StringComparison.OrdinalIgnoreCase)
            || string.Equals(resolutionStatus, "follow_up", StringComparison.OrdinalIgnoreCase);

        if (!followUpRequired || !unresolved)
        {
            result["caseAction"] = "not_required";
            return result;
        }

        if (confidence < MinimumConfidence)
        {
            result["caseAction"] = "manual_review_required";
            return result;
        }

        if (!dynamicsCaseClient.IsConfigured
            && !string.Equals(
                Environment.GetEnvironmentVariable("APP_MODE"),
                "azure",
                StringComparison.OrdinalIgnoreCase))
        {
            result["caseAction"] = "simulated";
            return result;
        }

        var caseResult = await dynamicsCaseClient.UpsertCaseAsync(
            callId,
            result["summary"]?.GetValue<string>() ?? string.Empty,
            result["followUpReason"]?.GetValue<string>() ?? "Customer follow-up required.",
            result["priority"]?.GetValue<string>(),
            ct);
        result["caseAction"] = caseResult.Created ? "created" : "updated";
        result["dynamicsCaseId"] = caseResult.CaseId;
        result["dynamicsCaseNumber"] = caseResult.CaseNumber;
        return result;
    }
}