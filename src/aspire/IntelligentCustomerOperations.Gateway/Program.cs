using System.Text.Json.Nodes;
using IntelligentCustomerOperations.Gateway;
using IntelligentCustomerOperations.Gateway.Models;
using IntelligentCustomerOperations.Gateway.Services;

static async Task<JsonNode?> ReadWebhookPayloadAsync(HttpRequest request)
{
    if (request.HasFormContentType)
    {
        var form = await request.ReadFormAsync();
        var payload = new JsonObject();
        foreach (var item in form)
        {
            payload[item.Key] = item.Value.ToString();
        }
        return payload;
    }

    return await JsonNode.ParseAsync(request.Body);
}

var builder = WebApplication.CreateBuilder(args);

var config = AppConfig.Load();
builder.Services.AddSingleton(config);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<CallStore>();
builder.Services.AddSingleton<DynamicsCaseClient>();
builder.Services.AddSingleton<TicketService>();
builder.Services.AddSingleton<SearchKnowledgeClient>();
builder.Services.AddSingleton<FoundryClient>();
builder.Services.AddSingleton<StorageRepository>();
// Provider is selected via CALL_CHANNEL_PROVIDER env var (default: acs).
// To add a new provider (e.g. Twilio), implement ICallChannelAdapter and register it here.
var callChannelProvider = Environment.GetEnvironmentVariable("CALL_CHANNEL_PROVIDER") ?? "acs";
if (callChannelProvider.Equals("acs", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<ICallChannelAdapter, AcsAdapter>();
}
else if (callChannelProvider.Equals("twilio", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<ICallChannelAdapter, TwilioAdapter>();
}
else
{
    throw new InvalidOperationException(
        $"Unknown CALL_CHANNEL_PROVIDER '{callChannelProvider}'. Supported values: acs, twilio");
}
builder.Services.AddSingleton<PostCallPublisher>();
builder.Services.AddSingleton<CallbackQueuePublisher>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/healthz", () => Results.Json(new { status = "ok", mode = config.Mode }));

app.MapGet("/api/config", () => Results.Json(new
{
    mode = config.Mode,
    region = Environment.GetEnvironmentVariable("AZURE_LOCATION") ?? "japaneast",
    foundryConfigured = !string.IsNullOrEmpty(config.Foundry.ProjectEndpoint) && !string.IsNullOrEmpty(config.Foundry.AgentId),
    dynamicsConfigured = !string.IsNullOrEmpty(config.Dynamics.OrganizationUrl),
    cosmosConfigured = !string.IsNullOrEmpty(config.Cosmos.Endpoint) || !string.IsNullOrEmpty(config.Cosmos.ConnectionString),
    searchConfigured = !string.IsNullOrEmpty(config.Search.Endpoint),
    storageConfigured = !string.IsNullOrEmpty(config.Storage.AccountName)
}));

app.MapGet("/api/calls", async (CallStore store, CancellationToken ct) => Results.Json(await store.ListCallsAsync(ct)));

app.MapGet("/api/calls/{id}", async (string id, CallStore store, CancellationToken ct) =>
    await store.GetCallAsync(id, ct) is { } call
        ? Results.Json(call)
        : Results.NotFound(new { error = "Call not found" }));

app.MapPost("/api/acs/events", async (HttpRequest request, ICallChannelAdapter channel, CancellationToken ct) =>
{
    var body = await ReadWebhookPayloadAsync(request);
    // /api/acs/events is the Event Grid webhook endpoint.
    // Event Grid performs its own validation and signing; we do NOT apply the
    // ACS_CALLBACK_SECRET header check here — that check is only for the
    // per-call callback URL below, which is invoked directly by ACS.
    return Results.Json(await channel.HandleIncomingEventsAsync(body, ct));
});

app.MapPost("/api/acs/callbacks/{callId}", async (string callId, HttpRequest request, ICallChannelAdapter channel, CancellationToken ct) =>
{
    var body = await ReadWebhookPayloadAsync(request);
    return Results.Json(await channel.HandleCallCallbackAsync(callId, body, ct));
});

app.MapPost("/api/channel/events", async (HttpRequest request, ICallChannelAdapter channel, CancellationToken ct) =>
{
    var body = await ReadWebhookPayloadAsync(request);
    return Results.Json(await channel.HandleIncomingEventsAsync(body, ct));
});

app.MapPost("/api/channel/callbacks/{callId}", async (string callId, HttpRequest request, ICallChannelAdapter channel, CancellationToken ct) =>
{
    var body = await ReadWebhookPayloadAsync(request);
    return Results.Json(await channel.HandleCallCallbackAsync(callId, body, ct));
});

app.MapPost("/api/foundry/tools/create-ticket", async (Ticket? input, TicketService ticketService, CancellationToken ct) =>
{
    var ticket = await ticketService.CreateTicketAsync(
        input?.CallId,
        input?.Reason,
        input?.Summary,
        input?.Priority,
        ct
    );
    return Results.Json(ticket);
});

app.MapPost("/api/foundry/tools/escalation-decision", (JsonNode? body) =>
{
    var confidence = body?["confidence"]?.GetValue<double>() ?? 0;
    var customerRequestedHuman = body?["customerRequestedHuman"]?.GetValue<bool>() ?? false;
    return Results.Json(new
    {
        escalate = customerRequestedHuman || confidence < 0.72,
    });
});

app.MapPost("/api/admin/knowledge/seed", async (SearchKnowledgeClient search, CancellationToken ct) =>
    Results.Json(await search.SeedAsync(SearchKnowledgeClient.SeedDocuments, ct)));

app.MapPost("/api/admin/analyze/{callId}", async (
    string callId,
    CallStore store,
    StorageRepository storage,
    PostCallPublisher postCall,
    CancellationToken ct) =>
{
    var call = await store.GetCallAsync(callId, ct);
    if (call is null)
    {
        return Results.NotFound(new { error = "Call not found" });
    }

    await storage.SaveCallArtifactAsync(call, ct);
    var result = await postCall.PublishAsync(call, ct);
    await store.UpdateCallAsync(call.Id, c =>
    {
        c.AnalyticsStatus = "submitted";
        c.PostCallResult = result.DeepClone();
    }, ct);
    return Results.Json(result);
});

app.MapPost("/api/dev/simulate-call", async (JsonNode? body, ICallChannelAdapter channel, CancellationToken ct) =>
    Results.Json(await channel.SimulateCallAsync(body?["language"]?.GetValue<string>() ?? "en", ct)));

app.Run();

