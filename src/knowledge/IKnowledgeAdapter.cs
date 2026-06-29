namespace CustomerOperations.Knowledge;

public interface IKnowledgeAdapter
{
    KnowledgeResult GetAnswer(string message);
}
