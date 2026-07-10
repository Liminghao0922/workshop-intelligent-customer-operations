using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace IntelligentCustomerOperations.PostCallWorker;

public sealed record DynamicsCaseResult(string CaseId, string? CaseNumber, bool Created);

public sealed class DynamicsCaseClient(ILogger<DynamicsCaseClient> logger)
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DYNAMICS_ORGANIZATION_URL"))
        && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DYNAMICS_CLIENT_ID"))
        && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DYNAMICS_CLIENT_SECRET"));

    public async Task<DynamicsCaseResult> UpsertCaseAsync(
        string callId,
        string summary,
        string followUpReason,
        string? priority,
        CancellationToken ct)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Dynamics Worker credentials are not configured.");
        }

        return await Task.Run(() =>
        {
            using var client = new ServiceClient(BuildConnectionString());
            if (!client.IsReady)
            {
                throw new InvalidOperationException($"Dynamics connection failed: {client.LastError}");
            }

            var incident = new Entity("incident", "ico_callid", callId)
            {
                ["title"] = string.IsNullOrWhiteSpace(summary) ? $"Call follow-up - {callId}" : summary,
                ["description"] = followUpReason,
                ["prioritycode"] = new OptionSetValue(MapPriority(priority))
            };
            var response = (UpsertResponse)client.Execute(new UpsertRequest { Target = incident });
            var created = client.Retrieve("incident", response.Target.Id, new ColumnSet("ticketnumber"));
            var caseNumber = created.GetAttributeValue<string>("ticketnumber");

            logger.LogInformation(
                "Dynamics case upserted. caseId={CaseId} caseNumber={CaseNumber} created={Created}",
                response.Target.Id,
                caseNumber,
                response.RecordCreated);
            return new DynamicsCaseResult(response.Target.Id.ToString(), caseNumber, response.RecordCreated);
        }, ct);
    }

    private static string BuildConnectionString() => string.Join(';',
        "AuthType=ClientSecret",
        $"Url={Environment.GetEnvironmentVariable("DYNAMICS_ORGANIZATION_URL")}",
        $"ClientId={Environment.GetEnvironmentVariable("DYNAMICS_CLIENT_ID")}",
        $"ClientSecret={Environment.GetEnvironmentVariable("DYNAMICS_CLIENT_SECRET")}");

    private static int MapPriority(string? priority) => priority?.Trim().ToLowerInvariant() switch
    {
        "high" => 1,
        "low" => 3,
        _ => 2
    };
}