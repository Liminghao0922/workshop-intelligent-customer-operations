using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Azure;
using Azure.Communication;
using Azure.Communication.CallAutomation;
using Microsoft.Extensions.Logging;
using IntelligentCustomerOperations.Gateway.Models;

namespace IntelligentCustomerOperations.Gateway.Services;

public sealed partial class AcsAdapter : ICallChannelAdapter
{
    private static readonly Dictionary<string, string> SampleUtterances = new()
    {
        ["en"] =
            "Hi, I was charged twice for my Contoso Care subscription. Please connect me to billing support if this cannot be resolved today.",
        ["ja"] =
            "Contoso Care のサブスクリプションで二重請求されました。今日解決できない場合は請求サポートにつないでください。",
        ["zh"] = "你好，我的 Contoso Care 订阅被扣了两次费。如果今天不能解决，请帮我转到账单支持。",
    };

    private static readonly Dictionary<string, string> SpeechLanguages = new()
    {
        ["en"] = "en-US",
        ["ja"] = "ja-JP",
        ["zh"] = "zh-CN",
    };

    private static readonly Dictionary<string, string> WelcomePrompts = new()
    {
        ["en"] =
            "Welcome to Contoso support. I am your AI assistant. Please tell me how I can help you today.",
        ["ja"] = "Contoso サポートへようこそ。AIアシスタントが対応します。ご用件をお話しください。",
        ["zh"] = "欢迎致电 Contoso 客服。我是 AI 助手，请告诉我您需要什么帮助。",
    };

    private static readonly Dictionary<string, string> InterruptAcknowledgementPrompts = new()
    {
        ["en"] = "Okay, I paused. Please continue and tell me what you need.",
        ["ja"] = "承知しました。いったん止めます。続けてご要件をお話しください。",
        ["zh"] = "好的，我先暂停。请继续告诉我您的需求。",
    };

    private static readonly Dictionary<string, string> EndCallPrompts = new()
    {
        ["en"] = "Thanks for calling Contoso support. Have a great day. Goodbye.",
        ["ja"] = "Contoso サポートにお電話いただきありがとうございました。失礼いたします。",
        ["zh"] = "感谢您致电 Contoso 客服。祝您今天愉快，再见。",
    };

    private static readonly Dictionary<string, string> DefaultVoiceNames = new()
    {
        ["en"] = "en-US-AriaNeural",
        ["ja"] = "ja-JP-NanamiNeural",
        ["zh"] = "zh-CN-XiaoxiaoNeural",
    };

    private readonly FoundryClient _foundry;
    private readonly CallStore _store;
    private readonly TicketService _ticketService;
    private readonly StorageRepository _storage;
    private readonly PostCallPublisher _postCallPublisher;
    private readonly CallbackQueuePublisher _callbackQueue;
    private readonly AppConfig _config;
    private readonly ILogger<AcsAdapter> _logger;
    private readonly CallAutomationClient? _callClient;
    private readonly CallRecording? _callRecording;

    public AcsAdapter(
        FoundryClient foundry,
        CallStore store,
        TicketService ticketService,
        StorageRepository storage,
        PostCallPublisher postCallPublisher,
        CallbackQueuePublisher callbackQueue,
        AppConfig config,
        ILogger<AcsAdapter> logger
    )
    {
        _foundry = foundry;
        _store = store;
        _ticketService = ticketService;
        _storage = storage;
        _postCallPublisher = postCallPublisher;
        _callbackQueue = callbackQueue;
        _config = config;
        _logger = logger;
        _callClient = string.IsNullOrWhiteSpace(config.Acs.ConnectionString)
            ? null
            : new CallAutomationClient(config.Acs.ConnectionString);
        _callRecording = _callClient?.GetCallRecording();
    }

    [GeneratedRegex("connect|support|人工|サポート|つない|转")]
    private static partial Regex HandoffRequestPattern();

    [GeneratedRegex("wait|hold on|stop|等一下|等等|先别|ちょっと待|待って")]
    private static partial Regex InterruptPhrasePattern();

    [GeneratedRegex(
        "(bye|goodbye|end (the )?(call|conversation)|hang up|that'?s all|结束|挂断|不用了|再见|おしまい|終わり|切って|さようなら)",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex EndConversationPattern();

    [GeneratedRegex(
        "(speak|use|switch|中文|汉语|日语|日本語|english|chinese|japanese|中国語|英語)",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex LanguageSwitchHintPattern();

    public async Task<object> HandleIncomingEventsAsync(
        JsonNode? events,
        CancellationToken ct = default
    )
    {
        var eventList = events is JsonArray array ? array.ToList() : [events];
        var created = new List<CallRecord>();
        string? validationResponse = null;

        foreach (var entry in eventList)
        {
            var eventType =
                entry?["eventType"]?.GetValue<string>() ?? entry?["type"]?.GetValue<string>() ?? "";
            if (eventType.Contains("SubscriptionValidation", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Received EventGrid subscription validation event");
                validationResponse =
                    entry?["data"]?["validationCode"]?.GetValue<string>()
                    ?? entry?["data"]?["validationResponse"]?.GetValue<string>();
                continue;
            }

            var data = entry?["data"] ?? entry;
            var from = data?["from"];
            var phone =
                from?["phoneNumber"]?["value"]?.GetValue<string>()
                ?? (from is JsonValue value ? value.GetValue<string>() : null)
                ?? "unknown";

            var call = _store.CreateCall(
                new CallRecord
                {
                    // Keep callback route stable: ACS identifiers may include '/' and break path-based routing.
                    Id = Guid.NewGuid().ToString("N"),
                    AcsCallConnectionId = data?["callConnectionId"]?.GetValue<string>() ?? "",
                    CustomerPhoneNumber = phone,
                    Language = data?["language"]?.GetValue<string>() ?? "en",
                    Status = "incoming",
                    Artifacts =
                    [
                        new JsonObject
                        {
                            ["type"] = "incoming_call_received",
                            ["serverCallId"] = data?["serverCallId"]?.GetValue<string>(),
                            ["incomingCallConnectionId"] = data?[
                                "callConnectionId"
                            ]?.GetValue<string>(),
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        },
                    ],
                }
            );

            if (
                _callClient is not null
                && !string.IsNullOrWhiteSpace(data?["incomingCallContext"]?.GetValue<string>())
                && !string.IsNullOrWhiteSpace(_config.PublicBaseUrl)
            )
            {
                _logger.LogInformation(
                    "Incoming ACS call received. callId={CallId} phone={Phone}",
                    call.Id,
                    phone
                );
                await AutoAnswerAndStartConversationAsync(
                    call,
                    data!["incomingCallContext"]!.GetValue<string>(),
                    ct
                );
            }

            created.Add(call);
        }

        return validationResponse is not null
            ? new { validationResponse }
            : new { accepted = created.Count, calls = created };
    }

    public async Task<object> HandleCallCallbackAsync(
        string callId,
        JsonNode? payload,
        CancellationToken ct = default
    )
    {
        if (payload is JsonArray callbackEvents)
        {
            object? lastReply = null;

            foreach (var evt in callbackEvents)
            {
                var eventType = evt?["type"]?.GetValue<string>() ?? "";
                var data = evt?["data"];

                _store.UpdateCall(
                    callId,
                    c =>
                    {
                        c.Artifacts.Add(
                            new JsonObject
                            {
                                ["type"] = "callback_event",
                                ["eventType"] = eventType,
                                ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                            }
                        );
                    }
                );

                if (eventType.EndsWith("CallConnected", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "Call connected callback received. callId={CallId}",
                        callId
                    );
                    var call =
                        _store.GetCall(callId)
                        ?? _store.CreateCall(new CallRecord { Id = callId, Status = "active" });
                    call.AcsCallConnectionId =
                        data?["callConnectionId"]?.GetValue<string>() ?? call.AcsCallConnectionId;
                    await StartRecordingIfEnabledAsync(call, ct);
                    await EnsureWelcomeStartedAsync(call.Id, "callback", ct);
                    continue;
                }

                if (eventType.EndsWith("RecordingStateChanged", StringComparison.OrdinalIgnoreCase))
                {
                    var recordingId = data?["recordingId"]?.GetValue<string>();
                    var state = data?["state"]?.GetValue<string>() ?? "unknown";
                    var call = _store.GetCall(callId);
                    if (call is not null)
                    {
                        _store.UpdateCall(
                            callId,
                            c =>
                            {
                                if (!string.IsNullOrWhiteSpace(recordingId))
                                {
                                    c.RecordingId = recordingId;
                                }
                                c.RecordingState = state;
                                c.Artifacts.Add(
                                    new JsonObject
                                    {
                                        ["type"] = "recording_state_changed",
                                        ["recordingId"] = recordingId,
                                        ["state"] = state,
                                        ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                                    }
                                );
                            }
                        );
                    }
                    continue;
                }

                if (eventType.EndsWith("PlayCompleted", StringComparison.OrdinalIgnoreCase))
                {
                    if (await EnsurePendingCallDisconnectAsync(callId, ct))
                    {
                        continue;
                    }

                    await EnsureWelcomeRecognitionStartedAsync(callId, ct);
                    await EnsurePendingFollowUpRecognitionStartedAsync(callId, ct);
                    continue;
                }

                if (eventType.EndsWith("PlayFailed", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("PlayFailed callback received. callId={CallId}", callId);
                    _store.UpdateCall(
                        callId,
                        c =>
                        {
                            c.Artifacts.Add(
                                new JsonObject
                                {
                                    ["type"] = "play_failed",
                                    ["code"] = data?["resultInformation"]?[
                                        "code"
                                    ]?.GetValue<int?>(),
                                    ["subCode"] = data?["resultInformation"]?[
                                        "subCode"
                                    ]?.GetValue<int?>(),
                                    ["message"] = data?["resultInformation"]?[
                                        "message"
                                    ]?.GetValue<string>(),
                                    ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                                }
                            );
                        }
                    );

                    if (await EnsurePendingCallDisconnectAsync(callId, ct))
                    {
                        continue;
                    }

                    // Do not let play failures deadlock the call flow.
                    await EnsureWelcomeRecognitionStartedAsync(callId, ct);
                    await EnsurePendingFollowUpRecognitionStartedAsync(callId, ct);
                    continue;
                }

                if (eventType.EndsWith("RecognizeCompleted", StringComparison.OrdinalIgnoreCase))
                {
                    var text =
                        data?["speechResult"]?["speech"]?.GetValue<string>()
                        ?? data?["speechResult"]?.GetValue<string>()
                        ?? data?["text"]?.GetValue<string>();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        _logger.LogInformation(
                            "Recognize completed. callId={CallId} text={Text}",
                            callId,
                            text
                        );
                        try
                        {
                            lastReply = await ContinueConversationAsync(callId, text, ct);
                        }
                        catch (Exception ex)
                        {
                            var call =
                                _store.GetCall(callId)
                                ?? _store.CreateCall(
                                    new CallRecord { Id = callId, Status = "active" }
                                );
                            var fallbackReply = GetAssistantFallbackPrompt(call.Language);
                            _logger.LogError(
                                ex,
                                "ContinueConversation failed. callId={CallId}",
                                callId
                            );
                            _store.UpdateCall(
                                callId,
                                c =>
                                {
                                    c.Artifacts.Add(
                                        new JsonObject
                                        {
                                            ["type"] = "assistant_reply_failed",
                                            ["message"] = ex.Message,
                                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                                        }
                                    );
                                }
                            );
                            _store.AppendTurn(callId, "assistant", fallbackReply);
                            await QueueAssistantReplyPlaybackAsync(call, fallbackReply, ct);
                            lastReply = new { fallback = true, text = fallbackReply };
                        }
                    }

                    continue;
                }

                if (eventType.EndsWith("RecognizeFailed", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Recognize failed callback received. callId={CallId}",
                        callId
                    );
                    var call = _store.GetCall(callId);
                    if (call is not null)
                    {
                        await StartSpeechRecognitionAsync(
                            call,
                            "Sorry, I did not catch that. Could you repeat that?",
                            ct
                        );
                    }
                    continue;
                }

                if (eventType.EndsWith("CallDisconnected", StringComparison.OrdinalIgnoreCase))
                {
                    var call = _store.GetCall(callId);
                    if (call is not null)
                    {
                        var completedAt = DateTimeOffset.UtcNow.ToString("o");
                        var completedCall = _store.UpdateCall(
                            callId,
                            c =>
                            {
                                c.Status = "completed";
                                c.CompletedAt = completedAt;
                            }
                        );
                        await _storage.SaveCallArtifactAsync(completedCall, ct);
                        var publishResult = await _postCallPublisher.PublishAsync(completedCall, ct);
                        _store.UpdateCall(
                            callId,
                            c =>
                            {
                                c.AnalyticsStatus = "submitted";
                                c.PostCallResult = publishResult.DeepClone();
                            }
                        );
                        _logger.LogInformation(
                            "Published call-ended event. callId={CallId} eventId={EventId}",
                            callId,
                            publishResult["eventId"]?.GetValue<string>() ?? "mock"
                        );
                    }
                }
            }

            return new
            {
                accepted = callbackEvents.Count,
                callId,
                reply = lastReply,
            };
        }

        var userText =
            payload?["speechResult"]?.GetValue<string>()
            ?? payload?["text"]?.GetValue<string>()
            ?? SampleUtterances.GetValueOrDefault(
                (_store.GetCall(callId)?.Language) ?? "en",
                SampleUtterances["en"]
            );

        var reply = await ContinueConversationAsync(callId, userText, ct);
        return new { callId, reply };
    }

    public async Task<CallRecord> SimulateCallAsync(string language, CancellationToken ct = default)
    {
        var call = _store.CreateCall(
            new CallRecord
            {
                Status = "active",
                Language = language,
                CustomerPhoneNumber = "+10000000000",
            }
        );

        var utterance = SampleUtterances.GetValueOrDefault(language, SampleUtterances["en"]);
        await HandleCallCallbackAsync(call.Id, new JsonObject { ["text"] = utterance }, ct);

        var completed = _store.UpdateCall(
            call.Id,
            c =>
            {
                c.Status = "completed";
                c.CompletedAt = DateTimeOffset.UtcNow.ToString("o");
            }
        );

        await _storage.SaveCallArtifactAsync(completed, ct);
        return completed;
    }

    private async Task<object> ContinueConversationAsync(
        string callId,
        string userText,
        CancellationToken ct
    )
    {
        var call =
            _store.GetCall(callId)
            ?? _store.CreateCall(new CallRecord { Id = callId, Status = "active" });
        _store.AppendTurn(call.Id, "customer", userText);

        var switchedLanguage = DetectLanguageSwitch(userText);
        if (
            !string.IsNullOrWhiteSpace(switchedLanguage)
            && LanguageSwitchHintPattern().IsMatch(userText)
        )
        {
            _store.UpdateCall(
                call.Id,
                c =>
                {
                    c.Language = switchedLanguage;
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "language_switched",
                            ["language"] = switchedLanguage,
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                }
            );

            call = _store.GetCall(call.Id) ?? call;
            var switchReply = GetLanguageSwitchReply(switchedLanguage);
            _store.AppendTurn(call.Id, "assistant", switchReply);

            if (!string.IsNullOrWhiteSpace(call.AcsCallConnectionId) && _callClient is not null)
            {
                await QueueAssistantReplyPlaybackAsync(call, switchReply, ct);
            }

            return new JsonObject
            {
                ["text"] = switchReply,
                ["language"] = switchedLanguage,
                ["switched"] = true,
            };
        }

        if (
            InterruptPhrasePattern().IsMatch(userText) && !HandoffRequestPattern().IsMatch(userText)
        )
        {
            _logger.LogInformation("User interruption detected. callId={CallId}", callId);
            await StartSpeechRecognitionAsync(
                call,
                GetInterruptAcknowledgementPrompt(call.Language),
                ct
            );
            return new { interrupted = true };
        }

        if (EndConversationPattern().IsMatch(userText))
        {
            var goodbye = GetEndCallPrompt(call.Language);
            _logger.LogInformation(
                "Customer requested call end. callId={CallId}",
                call.Id
            );

            _store.AppendTurn(call.Id, "assistant", goodbye);
            _store.UpdateCall(
                call.Id,
                c =>
                {
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "call_end_requested",
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "pending_call_disconnect",
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                }
            );

            if (!string.IsNullOrWhiteSpace(call.AcsCallConnectionId) && _callClient is not null)
            {
                await PlayPromptToAllAsync(call.AcsCallConnectionId, goodbye, call.Language, ct);
            }
            else
            {
                _store.UpdateCall(
                    call.Id,
                    c =>
                    {
                        c.Status = "completed";
                        c.CompletedAt = DateTimeOffset.UtcNow.ToString("o");
                    }
                );
            }

            return new JsonObject { ["text"] = goodbye, ["ending"] = true };
        }

        if (HandoffRequestPattern().IsMatch(userText))
        {
            var ticket = await _ticketService.CreateTicketAsync(
                call.Id,
                "customer_requested_handoff",
                userText,
                ct: ct
            );
            var transfer = await TryTransferToHumanAsync(call, ct);

            if (!transfer.Transferred)
            {
                var queueResult = await _callbackQueue.PublishAsync(
                    call,
                    ticket,
                    transfer.Reason ?? "transfer_failed",
                    ct
                );

                _store.UpdateCall(
                    call.Id,
                    c =>
                    {
                        c.Status = "callback_queued";
                        c.Artifacts.Add(queueResult.DeepClone());
                    }
                );

                var waitMinutes = _config.Callback.PromisedWaitMinutes;
                await StartSpeechRecognitionAsync(
                    call,
                    $"All our agents are busy right now. We queued your callback request. We will call you back within about {waitMinutes} minutes. Your ticket number is {ticket.Id}.",
                    ct
                );
            }

            return new
            {
                handoff = transfer.Transferred,
                ticket,
                transferReason = transfer.Reason,
            };
        }

        JsonNode? reply;
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(8));
            reply = await _foundry.GenerateVoiceAgentReplyAsync(call, userText, linkedCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            var timeoutReply = GetAssistantTimeoutPrompt(call.Language);
            _logger.LogWarning("Foundry reply timed out. callId={CallId}", call.Id);
            _store.UpdateCall(
                call.Id,
                c =>
                {
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "assistant_reply_timeout",
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                }
            );
            _store.AppendTurn(call.Id, "assistant", timeoutReply);

            if (!string.IsNullOrWhiteSpace(call.AcsCallConnectionId) && _callClient is not null)
            {
                await QueueAssistantReplyPlaybackAsync(call, timeoutReply, ct);
            }

            return new JsonObject { ["text"] = timeoutReply, ["fallback"] = true };
        }
        catch (Exception ex)
        {
            var fallbackReply = GetAssistantFallbackPrompt(call.Language);
            _logger.LogError(ex, "Foundry reply generation failed. callId={CallId}", call.Id);
            _store.UpdateCall(
                call.Id,
                c =>
                {
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "assistant_reply_error",
                            ["message"] = ex.Message,
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                }
            );
            _store.AppendTurn(call.Id, "assistant", fallbackReply);

            if (!string.IsNullOrWhiteSpace(call.AcsCallConnectionId) && _callClient is not null)
            {
                await QueueAssistantReplyPlaybackAsync(call, fallbackReply, ct);
            }

            return new JsonObject { ["text"] = fallbackReply, ["fallback"] = true };
        }

        var replyText =
            reply?["text"]?.GetValue<string>()
            ?? reply?["output"]?.GetValue<string>()
            ?? reply?.ToJsonString()
            ?? GetAssistantFallbackPrompt(call.Language);
        if (string.IsNullOrWhiteSpace(replyText))
        {
            replyText = GetAssistantFallbackPrompt(call.Language);
            _store.UpdateCall(
                call.Id,
                c =>
                {
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "assistant_empty_reply",
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                }
            );
        }

        _store.AppendTurn(call.Id, "assistant", replyText);

        if (!string.IsNullOrWhiteSpace(call.AcsCallConnectionId) && _callClient is not null)
        {
            await QueueAssistantReplyPlaybackAsync(call, replyText, ct);
        }

        return reply ?? new JsonObject { ["text"] = replyText };
    }

    private async Task AutoAnswerAndStartConversationAsync(
        CallRecord call,
        string incomingCallContext,
        CancellationToken ct
    )
    {
        if (_callClient is null)
        {
            return;
        }

        var callbackUri = new Uri(
            new Uri(_config.PublicBaseUrl.TrimEnd('/')),
            $"/api/acs/callbacks/{call.Id}"
        );
        var options = new AnswerCallOptions(incomingCallContext, callbackUri);

        if (
            Uri.TryCreate(
                _config.Foundry.VoiceLiveEndpoint,
                UriKind.Absolute,
                out var cognitiveEndpoint
            )
        )
        {
            options.CallIntelligenceOptions = new CallIntelligenceOptions
            {
                CognitiveServicesEndpoint = cognitiveEndpoint,
            };
        }

        Response<AnswerCallResult>? answerResponse = null;
        try
        {
            answerResponse = await _callClient.AnswerCallAsync(options, ct);
            _logger.LogInformation(
                "AnswerCall succeeded. callId={CallId} acsConnectionId={AcsCallConnectionId}",
                call.Id,
                answerResponse.Value.CallConnection.CallConnectionId
            );
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "AnswerCall failed. callId={CallId}", call.Id);
            _store.UpdateCall(
                call.Id,
                c =>
                {
                    c.Status = "answer_failed";
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "auto_answer_failed",
                            ["code"] = ex.ErrorCode,
                            ["message"] = ex.Message,
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                }
            );
            return;
        }

        _store.UpdateCall(
            call.Id,
            c =>
            {
                c.Status = "connected";
                c.AcsCallConnectionId = answerResponse!.Value.CallConnection.CallConnectionId;
            }
        );

        // Welcome media is started after CallConnected callback to avoid media race.
    }

    private async Task EnsureWelcomeStartedAsync(string callId, string source, CancellationToken ct)
    {
        var call =
            _store.GetCall(callId)
            ?? _store.CreateCall(new CallRecord { Id = callId, Status = "active" });
        var alreadyStarted = call
            .Artifacts.OfType<JsonObject>()
            .Any(a =>
                string.Equals(
                    a["type"]?.GetValue<string>(),
                    "welcome_started",
                    StringComparison.OrdinalIgnoreCase
                )
            );

        if (alreadyStarted)
        {
            return;
        }

        _store.UpdateCall(
            callId,
            c =>
            {
                c.Artifacts.Add(
                    new JsonObject
                    {
                        ["type"] = "welcome_starting",
                        ["source"] = source,
                        ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                    }
                );
            }
        );

        call = _store.GetCall(callId) ?? call;
        try
        {
            await PlayPromptToAllAsync(
                call.AcsCallConnectionId,
                GetWelcomePrompt(call.Language),
                call.Language,
                ct
            );
            _logger.LogInformation(
                "Welcome prompt played. callId={CallId} source={Source}",
                callId,
                source
            );
            _store.UpdateCall(
                callId,
                c =>
                {
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "welcome_started",
                            ["source"] = source,
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                }
            );
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Welcome prompt failed to play. callId={CallId}", callId);
            _store.UpdateCall(
                callId,
                c =>
                {
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "welcome_start_failed",
                            ["code"] = ex.ErrorCode,
                            ["message"] = ex.Message,
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                }
            );
        }
    }

    private async Task EnsureWelcomeRecognitionStartedAsync(string callId, CancellationToken ct)
    {
        var call =
            _store.GetCall(callId)
            ?? _store.CreateCall(new CallRecord { Id = callId, Status = "active" });
        var recognizeStarted = call
            .Artifacts.OfType<JsonObject>()
            .Any(a =>
                string.Equals(
                    a["type"]?.GetValue<string>(),
                    "welcome_recognition_started",
                    StringComparison.OrdinalIgnoreCase
                )
            );

        if (recognizeStarted)
        {
            return;
        }

        _store.UpdateCall(
            callId,
            c =>
            {
                c.Artifacts.Add(
                    new JsonObject
                    {
                        ["type"] = "welcome_recognition_started",
                        ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                    }
                );
            }
        );

        call = _store.GetCall(callId) ?? call;
        _logger.LogInformation("Starting post-welcome recognition. callId={CallId}", callId);
        await StartSpeechRecognitionAsync(call, string.Empty, ct);
    }

    private async Task EnsurePendingFollowUpRecognitionStartedAsync(
        string callId,
        CancellationToken ct
    )
    {
        var call = _store.GetCall(callId);
        if (call is null)
        {
            return;
        }

        var pendingCount = call
            .Artifacts.OfType<JsonObject>()
            .Count(a =>
                string.Equals(
                    a["type"]?.GetValue<string>(),
                    "pending_followup_recognition",
                    StringComparison.OrdinalIgnoreCase
                )
            );

        var startedCount = call
            .Artifacts.OfType<JsonObject>()
            .Count(a =>
                string.Equals(
                    a["type"]?.GetValue<string>(),
                    "followup_recognition_started",
                    StringComparison.OrdinalIgnoreCase
                )
            );

        if (pendingCount <= startedCount)
        {
            return;
        }

        _store.UpdateCall(
            callId,
            c =>
            {
                c.Artifacts.Add(
                    new JsonObject
                    {
                        ["type"] = "followup_recognition_started",
                        ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                    }
                );
            }
        );

        _logger.LogInformation(
            "Starting follow-up recognition after assistant playback. callId={CallId}",
            callId
        );
        await StartSpeechRecognitionAsync(call, string.Empty, ct);
    }

    private async Task<bool> EnsurePendingCallDisconnectAsync(string callId, CancellationToken ct)
    {
        var call = _store.GetCall(callId);
        if (call is null)
        {
            return false;
        }

        var pendingCount = call
            .Artifacts.OfType<JsonObject>()
            .Count(a =>
                string.Equals(
                    a["type"]?.GetValue<string>(),
                    "pending_call_disconnect",
                    StringComparison.OrdinalIgnoreCase
                )
            );

        var startedCount = call
            .Artifacts.OfType<JsonObject>()
            .Count(a =>
                string.Equals(
                    a["type"]?.GetValue<string>(),
                    "call_disconnect_started",
                    StringComparison.OrdinalIgnoreCase
                )
            );

        if (pendingCount <= startedCount)
        {
            return false;
        }

        _store.UpdateCall(
            callId,
            c =>
            {
                c.Artifacts.Add(
                    new JsonObject
                    {
                        ["type"] = "call_disconnect_started",
                        ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                    }
                );
            }
        );

        _logger.LogInformation("Disconnecting call after goodbye playback. callId={CallId}", callId);
        await DisconnectCallAsync(call, ct);
        return true;
    }

    private async Task QueueAssistantReplyPlaybackAsync(
        CallRecord call,
        string replyText,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(replyText))
        {
            replyText = GetAssistantFallbackPrompt(call.Language);
            _store.UpdateCall(
                call.Id,
                c =>
                {
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "assistant_empty_reply_before_play",
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                }
            );
        }

        _store.UpdateCall(
            call.Id,
            c =>
            {
                c.Artifacts.Add(
                    new JsonObject
                    {
                        ["type"] = "pending_followup_recognition",
                        ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                    }
                );
            }
        );

        _logger.LogInformation(
            "Playing assistant reply. callId={CallId} text={ReplyText}",
            call.Id,
            replyText
        );
        await PlayPromptToAllAsync(call.AcsCallConnectionId, replyText, call.Language, ct);
    }

    private async Task StartSpeechRecognitionAsync(
        CallRecord call,
        string promptText,
        CancellationToken ct
    )
    {
        if (_callClient is null || string.IsNullOrWhiteSpace(call.AcsCallConnectionId))
        {
            return;
        }

        var callConnection = _callClient.GetCallConnection(call.AcsCallConnectionId);
        var callMedia = callConnection.GetCallMedia();
        var targetParticipant = new PhoneNumberIdentifier(call.CustomerPhoneNumber);

        var recognize = new CallMediaRecognizeSpeechOptions(targetParticipant)
        {
            SpeechLanguage = SpeechLanguages.GetValueOrDefault(call.Language, "en-US"),
            InterruptPrompt = true,
            InterruptCallMediaOperation = true,
            EndSilenceTimeout = TimeSpan.FromSeconds(1),
            InitialSilenceTimeout = TimeSpan.FromSeconds(10),
            OperationContext =
                $"recognize-{call.Id}-{call.Transcript.Count}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
        };

        if (!string.IsNullOrWhiteSpace(promptText))
        {
            recognize.PlayPrompts.Add(CreatePromptSource(promptText, call.Language));
        }

        try
        {
            _logger.LogInformation(
                "Starting speech recognition. callId={CallId} hasPrompt={HasPrompt}",
                call.Id,
                !string.IsNullOrWhiteSpace(promptText)
            );
            await callMedia.StartRecognizingAsync(recognize, ct);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "StartRecognizing failed. callId={CallId}", call.Id);
            _store.UpdateCall(
                call.Id,
                c =>
                {
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "recognize_start_failed",
                            ["code"] = ex.ErrorCode,
                            ["message"] = ex.Message,
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                }
            );

            // Some caller identities (for example Teams identities) may not be resolvable as
            // PhoneNumberIdentifier. Fall back to plain TTS playback so callers still hear prompts.
            if (!string.IsNullOrWhiteSpace(promptText))
            {
                await PlayPromptToAllAsync(call.AcsCallConnectionId, promptText, call.Language, ct);
            }
        }
    }

    private async Task PlayPromptToAllAsync(
        string callConnectionId,
        string promptText,
        string language,
        CancellationToken ct
    )
    {
        if (_callClient is null || string.IsNullOrWhiteSpace(callConnectionId))
        {
            return;
        }

        var callConnection = _callClient.GetCallConnection(callConnectionId);
        var callMedia = callConnection.GetCallMedia();

        _logger.LogInformation(
            "Playing prompt to all. callConnectionId={CallConnectionId}",
            callConnectionId
        );
        await callMedia.PlayToAllAsync(
            CreatePromptSource(promptText, language),
            cancellationToken: ct
        );
    }

    private async Task DisconnectCallAsync(CallRecord call, CancellationToken ct)
    {
        if (_callClient is null || string.IsNullOrWhiteSpace(call.AcsCallConnectionId))
        {
            _store.UpdateCall(
                call.Id,
                c =>
                {
                    c.Status = "completed";
                    c.CompletedAt = DateTimeOffset.UtcNow.ToString("o");
                }
            );
            return;
        }

        try
        {
            var callConnection = _callClient.GetCallConnection(call.AcsCallConnectionId);
            await callConnection.HangUpAsync(true, ct);
            _store.UpdateCall(
                call.Id,
                c =>
                {
                    c.Status = "completed";
                    c.CompletedAt = DateTimeOffset.UtcNow.ToString("o");
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "call_disconnect_requested",
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                }
            );
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to disconnect call. callId={CallId}", call.Id);
            _store.UpdateCall(
                call.Id,
                c =>
                {
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "call_disconnect_failed",
                            ["code"] = ex.ErrorCode,
                            ["message"] = ex.Message,
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                }
            );
        }
    }

    private TextSource CreatePromptSource(string promptText, string language)
    {
        var source = new TextSource(promptText)
        {
            SourceLocale = SpeechLanguages.GetValueOrDefault(language, "en-US"),
        };

        var configuredVoice = _config.Acs.VoiceName;
        if (
            string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(configuredVoice)
        )
        {
            source.VoiceName = configuredVoice;
            return source;
        }

        source.VoiceName = DefaultVoiceNames.GetValueOrDefault(language, DefaultVoiceNames["en"]);

        return source;
    }

    private async Task StartRecordingIfEnabledAsync(CallRecord call, CancellationToken ct)
    {
        if (
            !_config.Recording.Enabled
            || _callRecording is null
            || string.IsNullOrWhiteSpace(call.AcsCallConnectionId)
        )
        {
            return;
        }

        try
        {
            var options = new StartRecordingOptions(call.AcsCallConnectionId)
            {
                RecordingContent = RecordingContent.Audio,
                RecordingChannel = RecordingChannel.Mixed,
                RecordingFormat = RecordingFormat.Mp3,
                RecordingStateCallbackUri = !string.IsNullOrWhiteSpace(_config.PublicBaseUrl)
                    ? new Uri(
                        new Uri(_config.PublicBaseUrl.TrimEnd('/')),
                        $"/api/acs/callbacks/{call.Id}"
                    )
                    : null,
            };

            if (
                Uri.TryCreate(
                    _config.Recording.BlobContainerUri,
                    UriKind.Absolute,
                    out var recordingContainerUri
                )
            )
            {
                // ACS exports recording chunks directly to customer-owned Blob storage (BYOS).
                options.RecordingStorage = RecordingStorage.CreateAzureBlobContainerRecordingStorage(
                    recordingContainerUri
                );
            }
            else
            {
                _logger.LogWarning(
                    "ACS recording BYOS is not configured. Set ACS_RECORDING_BLOB_CONTAINER_URI to export recordings directly to Blob. callId={CallId}",
                    call.Id
                );
            }

            var startResponse = await _callRecording.StartAsync(options, ct);
            var payload = startResponse.Value;

            _store.UpdateCall(
                call.Id,
                c =>
                {
                    c.RecordingState = "started";
                    c.RecordingId = payload?.RecordingId;
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "recording_started",
                            ["recordingId"] = payload?.RecordingId,
                            ["recordingState"] = payload?.RecordingState.ToString(),
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                }
            );
        }
        catch (RequestFailedException ex)
        {
            _store.UpdateCall(
                call.Id,
                c =>
                {
                    c.RecordingState = "start_failed";
                    c.Artifacts.Add(
                        new JsonObject
                        {
                            ["type"] = "recording_start_failed",
                            ["status"] = ex.Status,
                            ["errorCode"] = ex.ErrorCode,
                            ["message"] = ex.Message,
                            ["at"] = DateTimeOffset.UtcNow.ToString("o"),
                        }
                    );
                }
            );
        }
    }

    private static string GetWelcomePrompt(string language) =>
        WelcomePrompts.GetValueOrDefault(language, WelcomePrompts["en"]);

    private static string GetInterruptAcknowledgementPrompt(string language) =>
        InterruptAcknowledgementPrompts.GetValueOrDefault(
            language,
            InterruptAcknowledgementPrompts["en"]
        );

    private static string GetEndCallPrompt(string language) =>
        EndCallPrompts.GetValueOrDefault(language, EndCallPrompts["en"]);

    private static string GetAssistantTimeoutPrompt(string language) =>
        language switch
        {
            "ja" =>
                "少々お待ちください。回答を準備しています。もう一度ご用件を簡単にお話しいただけますか。",
            "zh" => "请稍等，我正在准备回答。可以请您再简短说一遍问题吗？",
            _ =>
                "Please hold on a moment while I prepare the answer. Could you briefly repeat your question?",
        };

    private static string GetAssistantFallbackPrompt(string language) =>
        language switch
        {
            "ja" =>
                "申し訳ありません。現在回答の生成で問題が発生しています。請求に関するご質問としてお手伝いできます。要点をもう一度教えてください。",
            "zh" =>
                "抱歉，我现在生成回答时遇到问题。我可以继续帮您处理账单问题，请再简要说明一次。",
            _ =>
                "Sorry, I had trouble generating the answer just now. I can still help with billing questions, so please briefly tell me again.",
        };

    private static string? DetectLanguageSwitch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var lower = text.ToLowerInvariant();
        if (
            lower.Contains("chinese")
            || lower.Contains("中文")
            || lower.Contains("汉语")
            || lower.Contains("中国語")
        )
        {
            return "zh";
        }

        if (
            lower.Contains("japanese")
            || lower.Contains("日本語")
            || lower.Contains("日语")
            || lower.Contains("日文")
            || lower.Contains("日本语")
        )
        {
            return "ja";
        }

        if (
            lower.Contains("english")
            || lower.Contains("英文")
            || lower.Contains("英語")
            || lower.Contains("英语")
        )
        {
            return "en";
        }

        return null;
    }

    private static string GetLanguageSwitchReply(string language) =>
        language switch
        {
            "ja" => "承知しました。これから日本語でご案内します。ご質問をどうぞ。",
            "zh" => "好的，我现在切换为中文。请告诉我您的问题。",
            _ => "Sure. I will continue in English from now on. Please tell me your question.",
        };

    private async Task<(bool Transferred, string? Reason)> TryTransferToHumanAsync(
        CallRecord call,
        CancellationToken ct
    )
    {
        if (
            _callClient is null
            || string.IsNullOrWhiteSpace(call.AcsCallConnectionId)
            || string.IsNullOrWhiteSpace(_config.Acs.HumanAgentPhoneNumber)
        )
        {
            return (false, "human_agent_not_configured");
        }

        try
        {
            var callConnection = _callClient.GetCallConnection(call.AcsCallConnectionId);
            await callConnection.TransferCallToParticipantAsync(
                new PhoneNumberIdentifier(_config.Acs.HumanAgentPhoneNumber),
                ct
            );
            _store.UpdateCall(call.Id, c => c.Status = "transferred_to_human");
            return (true, null);
        }
        catch (RequestFailedException ex)
        {
            return (false, $"acs_transfer_failed:{ex.Status}:{ex.ErrorCode}");
        }
    }
}

