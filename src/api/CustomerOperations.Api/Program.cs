using CustomerOperations.Agent;
using CustomerOperations.Api.Models;
using CustomerOperations.Functions;
using CustomerOperations.Knowledge;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddSingleton<IKnowledgeAdapter, MockFabricIqAdapter>();
builder.Services.AddSingleton<IServiceRequestStatusTool, ServiceRequestStatusTool>();
builder.Services.AddSingleton<IFoundryAgentAdapter, MockFoundryAgentAdapter>();

var app = builder.Build();

app.UseCors();
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        service = "customer-operations-api",
        timestampUtc = DateTime.UtcNow
    });
});

app.MapPost("/api/tools/service-request-status", (ServiceRequestToolRequest req, IServiceRequestStatusTool tool) =>
{
    if (string.IsNullOrWhiteSpace(req.RequestId))
    {
        return Results.BadRequest(new { error = "requestId is required." });
    }

    var result = tool.GetStatus(req.RequestId.Trim());
    return Results.Ok(result);
});

app.MapPost("/api/chat", async (
    ChatRequest req,
    IFoundryAgentAdapter agent,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(req.Message))
    {
        return Results.BadRequest(new { error = "message is required." });
    }

    var logger = loggerFactory.CreateLogger("ChatEndpoint");
    var correlationId = Guid.NewGuid().ToString("N");
    var result = await agent.ProcessAsync(req.Message.Trim(), correlationId, ct);

    logger.LogInformation(
        "correlationId={CorrelationId} intent={Intent} toolCalled={ToolCalled} toolName={ToolName}",
        correlationId, result.Intent, result.ToolCalled, result.ToolName ?? "none");

    var response = new ChatResponse(
        result.Response,
        result.Intent,
        result.ToolCalled,
        result.ToolName,
        correlationId);

    return Results.Ok(response);
});

app.Run();
