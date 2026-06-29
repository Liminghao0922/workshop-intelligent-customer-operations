using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace IntelligentCustomerOperations.Portal.Services;

public sealed class ApiClient(HttpClient httpClient)
{
    public async Task<List<CallSessionView>> GetSessionsAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<CallSessionView>>("/api/sessions", ct) ?? [];
    }

    public async Task<DailyMetricsView?> GetDailyMetricsAsync(int days = 14, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<DailyMetricsView>($"/api/metrics/daily?days={days}", ct);
    }

    public async Task<FcrMetricsView?> GetFcrMetricsAsync(int days = 30, CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<FcrMetricsView>($"/api/metrics/fcr?days={days}", ct);
    }
}

public sealed class CallSessionView
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("startedAt")]
    public string StartedAt { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("ticket")]
    public TicketView? Ticket { get; set; }
}

public sealed class TicketView
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public sealed class DailyMetricsView
{
    [JsonPropertyName("days")]
    public int Days { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("points")]
    public List<DailyPoint> Points { get; set; } = [];
}

public sealed class DailyPoint
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public sealed class FcrMetricsView
{
    [JsonPropertyName("days")]
    public int Days { get; set; }

    [JsonPropertyName("totalCalls")]
    public int TotalCalls { get; set; }

    [JsonPropertyName("resolvedOnFirstCall")]
    public int ResolvedOnFirstCall { get; set; }

    [JsonPropertyName("fcr")]
    public double Fcr { get; set; }
}

