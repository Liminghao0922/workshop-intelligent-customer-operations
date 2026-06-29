using System.Collections.Concurrent;
using System.Net;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using IntelligentCustomerOperations.Gateway.Models;

namespace IntelligentCustomerOperations.Gateway.Services;

public sealed class CallStore
{
    private readonly ConcurrentDictionary<string, CallRecord> _calls = new();
    private readonly ConcurrentDictionary<string, Ticket> _tickets = new();
    private readonly AppConfig _config;
    private readonly ILogger<CallStore> _logger;
    private readonly Lazy<Task<Container?>> _containerLazy;

    public CallStore(AppConfig config, ILogger<CallStore> logger)
    {
        _config = config;
        _logger = logger;
        _containerLazy = new Lazy<Task<Container?>>(InitializeContainerAsync);
    }

    public CallRecord CreateCall(CallRecord input) =>
        CreateCallAsync(input).GetAwaiter().GetResult();

    public async Task<CallRecord> CreateCallAsync(
        CallRecord input,
        CancellationToken ct = default
    )
    {
        var now = DateTimeOffset.UtcNow.ToString("o");
        var call = new CallRecord
        {
            Id = string.IsNullOrEmpty(input.Id) ? Guid.NewGuid().ToString("N") : input.Id,
            Status = string.IsNullOrEmpty(input.Status) ? "active" : input.Status,
            Language = string.IsNullOrEmpty(input.Language) ? "en" : input.Language,
            CustomerPhoneNumber = string.IsNullOrEmpty(input.CustomerPhoneNumber)
                ? "unknown"
                : input.CustomerPhoneNumber,
            AcsCallConnectionId = input.AcsCallConnectionId,
            FoundryConversationId = input.FoundryConversationId,
            StartedAt = string.IsNullOrEmpty(input.StartedAt) ? now : input.StartedAt,
            UpdatedAt = now,
            CompletedAt = input.CompletedAt,
            RecordingId = input.RecordingId,
            RecordingState = input.RecordingState,
            Transcript = input.Transcript ?? [],
            Artifacts = input.Artifacts ?? [],
            AnalyticsStatus = string.IsNullOrEmpty(input.AnalyticsStatus)
                ? "not_started"
                : input.AnalyticsStatus,
            Ticket = input.Ticket,
            PostCallResult = input.PostCallResult,
        };

        if (await TryUpsertToCosmosAsync(call, ct))
        {
            return call;
        }

        _calls[call.Id] = call;
        return call;
    }

    public CallRecord? GetCall(string id) => GetCallAsync(id).GetAwaiter().GetResult();

    public async Task<CallRecord?> GetCallAsync(string id, CancellationToken ct = default)
    {
        if (await TryGetFromCosmosAsync(id, ct) is { } call)
        {
            return call;
        }

        return _calls.TryGetValue(id, out var inMemoryCall) ? inMemoryCall : null;
    }

    public IReadOnlyList<CallRecord> ListCalls() => ListCallsAsync().GetAwaiter().GetResult();

    public async Task<IReadOnlyList<CallRecord>> ListCallsAsync(CancellationToken ct = default)
    {
        var cosmosCalls = await TryReadAllFromCosmosAsync(ct);
        if (cosmosCalls is not null)
        {
            return cosmosCalls
                .OrderByDescending(c => c.UpdatedAt, StringComparer.Ordinal)
                .ToList();
        }

        return _calls.Values
            .OrderByDescending(c => c.UpdatedAt, StringComparer.Ordinal)
            .ToList();
    }

    public CallRecord UpdateCall(string id, Action<CallRecord> patch) =>
        UpdateCallAsync(id, patch).GetAwaiter().GetResult();

    public async Task<CallRecord> UpdateCallAsync(
        string id,
        Action<CallRecord> patch,
        CancellationToken ct = default
    )
    {
        var call = await GetCallAsync(id, ct) ?? throw new InvalidOperationException($"Call not found: {id}");
        patch(call);
        call.UpdatedAt = DateTimeOffset.UtcNow.ToString("o");

        if (await TryUpsertToCosmosAsync(call, ct))
        {
            return call;
        }

        _calls[id] = call;
        return call;
    }

    public CallRecord AppendTurn(string id, string speaker, string text) =>
        AppendTurnAsync(id, speaker, text).GetAwaiter().GetResult();

    public Task<CallRecord> AppendTurnAsync(
        string id,
        string speaker,
        string text,
        CancellationToken ct = default
    ) =>
        UpdateCallAsync(
            id,
            call => call.Transcript.Add(
                new CallTurn
                {
                    Speaker = speaker,
                    Text = text,
                    At = DateTimeOffset.UtcNow.ToString("o"),
                }
            ),
            ct
        );

    public Ticket CreateTicket(string? callId, string? reason, string? summary, string? priority = null) =>
        CreateTicketAsync(callId, reason, summary, priority).GetAwaiter().GetResult();

    public async Task<Ticket> CreateTicketAsync(
        string? callId,
        string? reason,
        string? summary,
        string? priority = null,
        CancellationToken ct = default
    )
    {
        var ticket = new Ticket
        {
            Id = $"TICKET-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            CallId = callId,
            Priority = string.IsNullOrEmpty(priority) ? "medium" : priority,
            Reason = string.IsNullOrEmpty(reason) ? "human_handoff" : reason,
            Summary = summary ?? "",
            CreatedAt = DateTimeOffset.UtcNow.ToString("o"),
            Status = "open",
        };

        var call = !string.IsNullOrWhiteSpace(callId)
            ? await GetCallAsync(callId, ct)
            : null;

        if (call is not null)
        {
            call.Ticket = ticket;
            call.UpdatedAt = DateTimeOffset.UtcNow.ToString("o");

            if (!(await TryUpsertToCosmosAsync(call, ct)))
            {
                _calls[call.Id] = call;
            }
        }
        else
        {
            _tickets[ticket.Id] = ticket;
        }

        return ticket;
    }

    private async Task<Container?> InitializeContainerAsync()
    {
        if (
            string.IsNullOrWhiteSpace(_config.Cosmos.ConnectionString)
            && string.IsNullOrWhiteSpace(_config.Cosmos.Endpoint)
        )
        {
            _logger.LogInformation("Cosmos DB is not configured. Using in-memory call store.");
            return null;
        }

        var options = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Direct,
            ApplicationName = "smart-call-center-gateway",
            MaxRetryAttemptsOnRateLimitedRequests = 9,
            MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
        };

        if (!string.IsNullOrWhiteSpace(_config.Cosmos.PreferredRegion))
        {
            options.ApplicationPreferredRegions = [_config.Cosmos.PreferredRegion];
        }

        CosmosClient client;
        if (!string.IsNullOrWhiteSpace(_config.Cosmos.Endpoint))
        {
            client = new CosmosClient(_config.Cosmos.Endpoint, CreateCredential(), options);
        }
        else
        {
            client = new CosmosClient(_config.Cosmos.ConnectionString, options);
        }

        var db = (await client.CreateDatabaseIfNotExistsAsync(_config.Cosmos.DatabaseName)).Database;
        var container = (
            await db.CreateContainerIfNotExistsAsync(
                new ContainerProperties(_config.Cosmos.ContainerName, "/id")
            )
        ).Container;

        _logger.LogInformation(
            "Cosmos DB initialized. database={Database} container={Container}",
            _config.Cosmos.DatabaseName,
            _config.Cosmos.ContainerName
        );

        return container;
    }

    private TokenCredential CreateCredential()
    {
        if (!string.IsNullOrWhiteSpace(_config.Cosmos.ManagedIdentityClientId))
        {
            return new ManagedIdentityCredential(
                ManagedIdentityId.FromUserAssignedClientId(_config.Cosmos.ManagedIdentityClientId)
            );
        }

        return _config.IsAzureMode
            ? new ManagedIdentityCredential(new ManagedIdentityCredentialOptions())
            : new DefaultAzureCredential();
    }

    private async Task<CallRecord?> TryGetFromCosmosAsync(string id, CancellationToken ct)
    {
        var container = await GetContainerAsync();
        if (container is null)
        {
            return null;
        }

        try
        {
            var response = await container.ReadItemAsync<CallRecord>(id, new PartitionKey(id), cancellationToken: ct);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<List<CallRecord>?> TryReadAllFromCosmosAsync(CancellationToken ct)
    {
        var container = await GetContainerAsync();
        if (container is null)
        {
            return null;
        }

        var results = new List<CallRecord>();
        using var iterator = container.GetItemQueryIterator<CallRecord>(
            new QueryDefinition("SELECT * FROM c"),
            requestOptions: new QueryRequestOptions { MaxItemCount = 100 }
        );

        while (iterator.HasMoreResults)
        {
            var page = await iterator.ReadNextAsync(ct);
            results.AddRange(page.Resource);
        }

        return results;
    }

    private async Task<bool> TryUpsertToCosmosAsync(CallRecord call, CancellationToken ct)
    {
        var container = await GetContainerAsync();
        if (container is null)
        {
            return false;
        }

        await container.UpsertItemAsync(call, new PartitionKey(call.Id), cancellationToken: ct);
        return true;
    }

    private async Task<Container?> GetContainerAsync()
    {
        if (_containerLazy.Value is null)
        {
            return null;
        }

        return await _containerLazy.Value;
    }
}

