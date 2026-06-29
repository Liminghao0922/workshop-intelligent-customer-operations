// Azure.AI.Projects SDK – dotnet add package Azure.AI.Projects --version 2.0.0-beta.2
#pragma warning disable OPENAI001

using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
using IntelligentCustomerOperations.Gateway.Models;

namespace IntelligentCustomerOperations.Gateway.Services;

public sealed partial class FoundryClient(
    AppConfig config,
    SearchKnowledgeClient searchClient,
    ILogger<FoundryClient> logger
)
{
    [GeneratedRegex("human|agent|support|人工|サポート|つない")]
    private static partial Regex HandoffPattern();

    public async Task<JsonNode> GenerateVoiceAgentReplyAsync(
        CallRecord call,
        string userText,
        CancellationToken ct = default
    )
    {
        if (!config.IsAzureMode)
        {
            var context = await searchClient.RetrieveAsync(userText, call.Language, ct);
            return new JsonObject
            {
                ["text"] = MockReply(call.Language, userText, context),
                ["groundedContext"] = JsonSerializer.SerializeToNode(context),
                ["source"] = "mock-foundry",
            };
        }

        var conversationId = call.FoundryConversationId;
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            conversationId = Guid.NewGuid().ToString("N");
            call.FoundryConversationId = conversationId;
            logger.LogInformation(
                "Created Foundry conversation mapping. callId={CallId} conversationId={ConversationId}",
                call.Id,
                conversationId
            );
        }

        var result = await InvokeAgentAsync(
            config.Foundry.AgentId,
            call.Language,
            userText,
            conversationId,
            ct
        );

        var resolvedConversationId = result["conversationId"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(resolvedConversationId))
        {
            call.FoundryConversationId = resolvedConversationId;
        }

        return result;
    }

    public async Task<JsonNode> AnalyzeCallAsync(CallRecord call, CancellationToken ct = default)
    {
        var redaction = await PiiMasker.MaskTranscriptAsync(call.Transcript, call.Language, ct);

        if (!config.IsAzureMode)
        {
            var text = redaction.MaskedText;
            return new JsonObject
            {
                ["summary"] =
                    text.Length > 0
                        ? "Customer reported a billing issue and requested follow-up."
                        : "No transcript available.",
                ["intent"] = "billing_dispute",
                ["sentiment"] = "concerned",
                ["entities"] = new JsonArray("subscription", "billing", "duplicate charge"),
                ["actionItems"] = new JsonArray(
                    "Create ticket",
                    "Verify transaction",
                    "Follow up with refund decision"
                ),
                ["resolutionStatus"] = "escalated",
                ["redactionStatus"] = redaction.Applied ? "applied" : "not_needed",
                ["redactionSignals"] = new JsonArray(
                    redaction.Signals.Select(signal => (JsonNode?)JsonValue.Create(signal)).ToArray()
                ),
                ["maskedTranscript"] = redaction.MaskedTranscript,
            };
        }

        var transcript = redaction.MaskedText;
        return await InvokeAgentAsync(
            config.Foundry.AnalyticsAgentId,
            call.Language,
            transcript,
            null,
            ct
        );
    }

    private async Task<JsonNode> InvokeAgentAsync(
        string agentName,
        string language,
        string input,
        string? conversationId,
        CancellationToken ct
    )
    {
        if (
            string.IsNullOrWhiteSpace(config.Foundry.ProjectEndpoint)
            || string.IsNullOrWhiteSpace(agentName)
        )
        {
            throw new InvalidOperationException(
                "AZURE_AI_PROJECT_ENDPOINT and FOUNDRY_AGENT_ID are required in azure mode."
            );
        }

        AIProjectClient projectClient = new(
            endpoint: new Uri(config.Foundry.ProjectEndpoint),
            tokenProvider: new DefaultAzureCredential()
        );

        AgentReference agentReference = new(name: agentName);
        ProjectResponsesClient responseClient =
            projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(agentReference);

        CreateResponseOptions createOptions = new()
        {
            InputItems = { ResponseItem.CreateUserMessageItem(input) },
            MaxOutputTokenCount = 180,
        };

        if (!string.IsNullOrWhiteSpace(conversationId))
        {
            createOptions.ConversationOptions = new ResponseConversationOptions(conversationId);
        }

        logger.LogInformation(
            "Invoking Foundry agent via SDK. agentName={AgentName} language={Language} conversationId={ConversationId}",
            agentName,
            language,
            conversationId
        );

        var sdkResponse = await Task.Run(() => responseClient.CreateResponse(createOptions), ct);

        var text = sdkResponse.Value.GetOutputText();
        var resolvedConversationId = sdkResponse.Value.ConversationOptions?.ConversationId;

        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogWarning(
                "Agent SDK returned empty text. Using localized fallback. agentName={AgentName}",
                agentName
            );
            text = GetLocalizedFallbackText(language);
        }

        return new JsonObject
        {
            ["text"] = text,
            ["source"] = "foundry-agent-sdk",
            ["conversationId"] = resolvedConversationId ?? conversationId,
        };
    }

    /// <summary>Wraps a static bearer token as a <see cref="AuthenticationTokenProvider"/>.</summary>
    private sealed class BearerTokenProvider(string token) : AuthenticationTokenProvider
    {
        private static AuthenticationToken MakeToken(string t) =>
            new(t, "Bearer", DateTimeOffset.UtcNow.AddHours(1), null);

        public override AuthenticationToken GetToken(
            GetTokenOptions options,
            CancellationToken ct
        ) => MakeToken(token);

        public override ValueTask<AuthenticationToken> GetTokenAsync(
            GetTokenOptions options,
            CancellationToken ct
        ) => ValueTask.FromResult(MakeToken(token));

        public override GetTokenOptions? CreateTokenOptions(
            IReadOnlyDictionary<string, object> extraOptions
        ) => null;
    }

    private static string GetLocalizedFallbackText(string language) =>
        language switch
        {
            "ja" => "承知しました。請求に関するご質問ですね。状況をもう少し詳しく教えてください。",
            "zh" => "好的，我可以帮您处理账单问题。请再告诉我一下具体情况。",
            _ =>
                "Understood. I can help with your billing question. Please share a bit more detail.",
        };

    private static string MockReply(
        string language,
        string userText,
        IReadOnlyList<KnowledgeDocument> context
    )
    {
        var mentionsHuman = HandoffPattern().IsMatch(userText);
        return language switch
        {
            "ja" => mentionsHuman
                ? "請求サポートに引き継ぐため、会話内容と推奨アクションをまとめます。"
                : $"確認します。関連ナレッジ: {context.FirstOrDefault()?.Title ?? "請求ポリシー"}。",
            "zh" => mentionsHuman
                ? "我会整理当前上下文，并创建账单支持工单。"
                : $"我来确认。相关知识：{context.FirstOrDefault()?.Title ?? "账单政策"}。",
            _ => mentionsHuman
                ? "I will summarize the context and create a billing support handoff."
                : $"I can help. Relevant knowledge: {context.FirstOrDefault()?.Title ?? "billing policy"}.",
        };
    }
}

