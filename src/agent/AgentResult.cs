namespace CustomerOperations.Agent;

public record AgentResult(
    string Response,
    string Intent,
    bool ToolCalled,
    string? ToolName);
