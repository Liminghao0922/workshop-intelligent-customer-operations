using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

var app = builder.Build();

app.MapGet("/healthz", () => Results.Json(new { status = "ok", service = "api" }));

app.MapGet("/api/sessions", async (IHttpClientFactory clientFactory, CancellationToken ct) =>
{
	var sessions = await LoadSessionsAsync(clientFactory, ct);
	return Results.Json(sessions);
});

app.MapGet(
	"/api/sessions/{id}",
	async (string id, IHttpClientFactory clientFactory, CancellationToken ct) =>
	{
		var sessions = await LoadSessionsAsync(clientFactory, ct);
		var session = sessions.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.Ordinal));
		return session is null
			? Results.NotFound(new { error = "Session not found" })
			: Results.Json(session);
	}
);

app.MapGet(
	"/api/metrics/daily",
	async (int? days, IHttpClientFactory clientFactory, CancellationToken ct) =>
	{
		var horizon = Math.Clamp(days ?? 14, 1, 90);
		var sessions = await LoadSessionsAsync(clientFactory, ct);
		var since = DateTime.UtcNow.Date.AddDays(-(horizon - 1));

		var daily = sessions
			.Select(
				s =>
					DateTimeOffset.TryParse(s.StartedAt, out var started)
						? new { Day = started.UtcDateTime.Date, Session = s }
						: null
			)
			.Where(x => x is not null && x.Day >= since)
			.GroupBy(x => x!.Day)
			.Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), count = g.Count() })
			.OrderBy(x => x.date)
			.ToList();

		return Results.Json(
			new
			{
				days = horizon,
				total = daily.Sum(d => d.count),
				points = daily,
			}
		);
	}
);

app.MapGet(
	"/api/metrics/fcr",
	async (int? days, IHttpClientFactory clientFactory, CancellationToken ct) =>
	{
		var horizon = Math.Clamp(days ?? 30, 1, 180);
		var sessions = await LoadSessionsAsync(clientFactory, ct);
		var since = DateTimeOffset.UtcNow.AddDays(-horizon);

		var filtered = sessions
			.Where(s => DateTimeOffset.TryParse(s.StartedAt, out var started) && started >= since)
			.ToList();

		var resolved = filtered.Count(s =>
			string.Equals(s.Status, "completed", StringComparison.OrdinalIgnoreCase)
			&& s.Ticket is null
		);

		var rate = filtered.Count == 0 ? 0 : Math.Round((double)resolved / filtered.Count, 4);

		return Results.Json(
			new
			{
				days = horizon,
				totalCalls = filtered.Count,
				resolvedOnFirstCall = resolved,
				fcr = rate,
			}
		);
	}
);

app.Run();

static async Task<List<CallSessionDto>> LoadSessionsAsync(
	IHttpClientFactory clientFactory,
	CancellationToken ct
)
{
	var gatewayBaseUrl =
		Environment.GetEnvironmentVariable("GATEWAY_API_BASE_URL")
		?? "http://localhost:8080";

	var client = clientFactory.CreateClient();
	using var response = await client.GetAsync(
		$"{gatewayBaseUrl.TrimEnd('/')}/api/calls",
		ct
	);
	if (!response.IsSuccessStatusCode)
	{
		return [];
	}

	await using var stream = await response.Content.ReadAsStreamAsync(ct);
	var sessions = await JsonSerializer.DeserializeAsync<List<CallSessionDto>>(
		stream,
		new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true,
		},
		ct
	);

	return sessions ?? [];
}

sealed class CallSessionDto
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("status")]
	public string Status { get; set; } = string.Empty;

	[JsonPropertyName("startedAt")]
	public string StartedAt { get; set; } = string.Empty;

	[JsonPropertyName("language")]
	public string Language { get; set; } = "en";

	[JsonPropertyName("ticket")]
	public TicketDto? Ticket { get; set; }
}

sealed class TicketDto
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;
}
