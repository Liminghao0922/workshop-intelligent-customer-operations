using Microsoft.Extensions.Logging;
using IntelligentCustomerOperations.Gateway.Models;

namespace IntelligentCustomerOperations.Gateway.Services;

public sealed class TicketService(
    CallStore store,
    DynamicsCaseClient dynamicsCaseClient,
    ILogger<TicketService> logger
)
{
    public async Task<Ticket> CreateTicketAsync(
        string? callId,
        string? reason,
        string? summary,
        string? priority = null,
        CancellationToken ct = default
    )
    {
        var ticket = await store.CreateTicketAsync(callId, reason, summary, priority, ct);

        if (!dynamicsCaseClient.IsConfigured)
        {
            return ticket;
        }

        try
        {
            var (caseId, caseNumber) = await dynamicsCaseClient.CreateCaseAsync(ticket, ct);
            ticket.Id = !string.IsNullOrWhiteSpace(caseNumber) ? caseNumber! : caseId;

            if (!string.IsNullOrWhiteSpace(callId))
            {
                await store.UpdateCallAsync(
                    callId,
                    c =>
                    {
                        c.Ticket = ticket;
                        c.Artifacts.Add(
                            new System.Text.Json.Nodes.JsonObject
                            {
                                ["type"] = "dynamics_case_created",
                                ["caseId"] = caseId,
                                ["caseNumber"] = caseNumber,
                                ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                            }
                        );
                    }
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dynamics case creation failed. Falling back to local ticket.");
            if (!string.IsNullOrWhiteSpace(callId))
            {
                await store.UpdateCallAsync(
                    callId,
                    c =>
                    {
                        c.Artifacts.Add(
                            new System.Text.Json.Nodes.JsonObject
                            {
                                ["type"] = "dynamics_case_failed",
                                ["message"] = ex.Message,
                                ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                            }
                        );
                    }
                );
            }
        }

        return ticket;
    }
}

