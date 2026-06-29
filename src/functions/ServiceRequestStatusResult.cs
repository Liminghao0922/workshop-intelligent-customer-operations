namespace CustomerOperations.Functions;

public record ServiceRequestStatusResult(
    string RequestId,
    string Status,
    string NextStep);
