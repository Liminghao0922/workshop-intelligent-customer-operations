namespace CustomerOperations.Api.Models;

public record ChatResponse(
    string Response,
    string Intent,
    bool ToolCalled,
    string? ToolName,
    string CorrelationId);
