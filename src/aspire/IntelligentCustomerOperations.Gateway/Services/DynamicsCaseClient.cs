using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using IntelligentCustomerOperations.Gateway.Models;

namespace IntelligentCustomerOperations.Gateway.Services;

public sealed class DynamicsCaseClient(AppConfig config, ILogger<DynamicsCaseClient> logger)
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(config.Dynamics.ConnectionString)
        || (
            !string.IsNullOrWhiteSpace(config.Dynamics.OrganizationUrl)
            && !string.IsNullOrWhiteSpace(config.Dynamics.ClientId)
            && !string.IsNullOrWhiteSpace(config.Dynamics.ClientSecret)
        );

    public async Task<(string CaseId, string? CaseNumber)> CreateCaseAsync(
        Ticket ticket,
        CancellationToken ct
    )
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Dynamics is not configured.");
        }

        ct.ThrowIfCancellationRequested();

        return await Task.Run(
            () =>
            {
                using var serviceClient = new ServiceClient(BuildConnectionString());
                if (!serviceClient.IsReady)
                {
                    throw new InvalidOperationException(
                        $"Dynamics connection failed: {serviceClient.LastError}"
                    );
                }

                var caseRecord = new Entity("incident")
                {
                    ["title"] = BuildTitle(ticket),
                    ["description"] = string.IsNullOrWhiteSpace(ticket.Summary)
                        ? "Created from Smart Call Center handoff."
                        : ticket.Summary,
                    ["prioritycode"] = new OptionSetValue(MapPriority(ticket.Priority)),
                };

                var caseId = serviceClient.Create(caseRecord);
                var created = serviceClient.Retrieve(
                    "incident",
                    caseId,
                    new ColumnSet("ticketnumber")
                );
                var caseNumber = created.GetAttributeValue<string>("ticketnumber");

                logger.LogInformation(
                    "Dynamics case created. caseId={CaseId} caseNumber={CaseNumber}",
                    caseId,
                    caseNumber
                );

                return (caseId.ToString(), caseNumber);
            },
            ct
        );
    }

    private string BuildConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(config.Dynamics.ConnectionString))
        {
            return config.Dynamics.ConnectionString;
        }

        return string.Join(
            ';',
            [
                "AuthType=ClientSecret",
                $"Url={config.Dynamics.OrganizationUrl}",
                $"ClientId={config.Dynamics.ClientId}",
                $"ClientSecret={config.Dynamics.ClientSecret}",
            ]
        );
    }

    private static int MapPriority(string? priority) =>
        priority?.Trim().ToLowerInvariant() switch
        {
            "high" => 1,
            "low" => 3,
            _ => 2,
        };

    private static string BuildTitle(Ticket ticket)
    {
        var reason = string.IsNullOrWhiteSpace(ticket.Reason) ? "handoff" : ticket.Reason;
        return $"Smart Call Center - {reason}";
    }
}

