namespace CustomerOperations.Agent;

public interface IFoundryAgentAdapter
{
    Task<AgentResult> ProcessAsync(string message, string correlationId, CancellationToken cancellationToken = default);
}
