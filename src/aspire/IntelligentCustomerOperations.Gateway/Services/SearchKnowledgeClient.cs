using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using IntelligentCustomerOperations.Gateway.Models;

namespace IntelligentCustomerOperations.Gateway.Services;

public sealed class SearchKnowledgeClient(AppConfig config)
{
    private static readonly KnowledgeDocument[] MockKnowledge =
    [
        new() { Id = "en-billing-1", Language = "en", Title = "Duplicate billing policy", Content = "Duplicate charges should be verified and escalated to billing support when refund approval is required." },
        new() { Id = "ja-billing-1", Language = "ja", Title = "二重請求ポリシー", Content = "二重請求は取引を確認し、返金承認が必要な場合は請求サポートへ引き継ぎます。" },
        new() { Id = "zh-billing-1", Language = "zh", Title = "重复扣费政策", Content = "重复扣费需要核对交易，如需退款审批则转给账单支持团队。" }
    ];

    public static readonly KnowledgeDocument[] SeedDocuments =
    [
        new()
        {
            Id = "en-billing-duplicate-charge",
            Language = "en",
            Title = "Duplicate billing policy",
            Content = "When a customer reports duplicate subscription billing, verify both transactions, explain the expected reversal window, and escalate to billing support when refund approval or account-specific review is required.",
            SourceUrl = "https://contoso.example/policies/billing"
        },
        new()
        {
            Id = "ja-billing-duplicate-charge",
            Language = "ja",
            Title = "二重請求ポリシー",
            Content = "サブスクリプションの二重請求が報告された場合は、両方の取引を確認し、返金予定期間を説明します。返金承認や個別確認が必要な場合は請求サポートへ引き継ぎます。",
            SourceUrl = "https://contoso.example/policies/billing-ja"
        },
        new()
        {
            Id = "zh-billing-duplicate-charge",
            Language = "zh",
            Title = "重复扣费政策",
            Content = "当客户反馈订阅重复扣费时，需要核对两笔交易，说明预计冲正时间。如果需要退款审批或账户级核查，应转给账单支持团队。",
            SourceUrl = "https://contoso.example/policies/billing-zh"
        }
    ];

    private SearchClient? CreateClient()
    {
        if (string.IsNullOrEmpty(config.Search.Endpoint))
        {
            return null;
        }

        var endpoint = new Uri(config.Search.Endpoint);
        return string.IsNullOrEmpty(config.Search.ApiKey)
            ? new SearchClient(endpoint, config.Search.IndexName, new DefaultAzureCredential())
            : new SearchClient(endpoint, config.Search.IndexName, new AzureKeyCredential(config.Search.ApiKey));
    }

    public async Task<IReadOnlyList<KnowledgeDocument>> RetrieveAsync(string query, string language, CancellationToken ct = default)
    {
        var client = CreateClient();
        if (client is null || !config.IsAzureMode)
        {
            return MockKnowledge.Where(d => d.Language == language).Take(3).ToList();
        }

        var options = new SearchOptions
        {
            Size = 3,
            Filter = $"language eq '{language.Replace("'", "''")}'"
        };

        var documents = new List<KnowledgeDocument>();
        var response = await client.SearchAsync<KnowledgeDocument>(query, options, ct);
        await foreach (var result in response.Value.GetResultsAsync())
        {
            documents.Add(result.Document);
        }
        return documents;
    }

    public async Task<object> SeedAsync(IReadOnlyList<KnowledgeDocument> documents, CancellationToken ct = default)
    {
        var client = CreateClient();
        if (client is null || !config.IsAzureMode)
        {
            return new { mode = "mock", uploaded = documents.Count };
        }

        var result = await client.UploadDocumentsAsync(documents, cancellationToken: ct);
        return new
        {
            mode = "azure",
            uploaded = result.Value.Results.Count(r => r.Succeeded),
            failed = result.Value.Results.Count(r => !r.Succeeded)
        };
    }
}

