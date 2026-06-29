namespace CustomerOperations.Knowledge;

public record KnowledgeResult(
    bool Found,
    string Answer,
    string? Source);
