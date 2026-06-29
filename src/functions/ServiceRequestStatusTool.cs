using System.Text.Json;

namespace CustomerOperations.Functions;

public sealed class ServiceRequestStatusTool : IServiceRequestStatusTool
{
    private readonly Dictionary<string, ServiceRequestRecord> _records;

    public ServiceRequestStatusTool()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "data", "business-records", "service-requests.json");
        var json = File.Exists(filePath) ? File.ReadAllText(filePath) : "[]";
        var items = JsonSerializer.Deserialize<List<ServiceRequestRecord>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<ServiceRequestRecord>();

        _records = items
            .Where(i => !string.IsNullOrWhiteSpace(i.RequestId))
            .ToDictionary(i => i.RequestId.Trim().ToUpperInvariant(), i => i);
    }

    public ServiceRequestStatusResult GetStatus(string requestId)
    {
        var normalized = requestId.Trim().ToUpperInvariant();
        if (_records.TryGetValue(normalized, out var record))
        {
            return new ServiceRequestStatusResult(record.RequestId, record.Status, record.NextStep);
        }

        return new ServiceRequestStatusResult(normalized, "Not Found", "Confirm request ID and try again.");
    }
}
