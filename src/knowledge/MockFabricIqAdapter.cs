namespace CustomerOperations.Knowledge;

public sealed class MockFabricIqAdapter : IKnowledgeAdapter
{
    private readonly string _productFaq;
    private readonly string _supportPolicy;

    public MockFabricIqAdapter()
    {
        // TODO(v4): replace file reads with real Fabric IQ retrieval API.
        _productFaq = TryReadDataFile("data", "knowledge-base", "product-faq.md");
        _supportPolicy = TryReadDataFile("data", "knowledge-base", "support-policy.md");
    }

    public KnowledgeResult GetAnswer(string message)
    {
        var lowered = message.ToLowerInvariant();

        if (lowered.Contains("warranty") || lowered.Contains("product"))
        {
            return new KnowledgeResult(
                true,
                "To check product warranty, provide your product serial number and purchase date.",
                "product-faq.md");
        }

        if (lowered.Contains("escalat") || lowered.Contains("policy"))
        {
            return new KnowledgeResult(
                true,
                "Requests involving safety, legal, financial impact, or repeated unresolved issues should be escalated to a human specialist.",
                "support-policy.md");
        }

        if (!string.IsNullOrWhiteSpace(_productFaq) && lowered.Contains("repair"))
        {
            return new KnowledgeResult(
                true,
                "Customers can check repair status by providing a valid service request ID.",
                "product-faq.md");
        }

        return new KnowledgeResult(false, string.Empty, null);
    }

    private static string TryReadDataFile(params string[] pathParts)
    {
        var baseDir = AppContext.BaseDirectory;
        var fullPath = Path.Combine(new[] { baseDir }.Concat(pathParts).ToArray());
        return File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty;
    }
}
