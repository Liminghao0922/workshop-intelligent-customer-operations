namespace CustomerOperations.Functions;

public sealed class ServiceRequestRecord
{
    public string RequestId { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string NextStep { get; set; } = string.Empty;
}
