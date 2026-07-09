# Exercise 2 - Configure Azure AI Search

## Objective

Configure Azure AI Search as the knowledge layer for the customer operations agent.

## Tasks

1. Create an Azure AI Search service in your resource group.
2. Create an index with the recommended schema for knowledge documents.
3. Upload support documents (FAQ, policy, troubleshooting guides) to the index.
4. Test at least one search query to confirm documents are retrievable.
5. Record the search endpoint, index name, and API key for agent configuration.

## Recommended Index Schema

| Field | Type | Purpose |
|---|---|---|
| `id` | string (key) | Unique document identifier |
| `title` | string | Document title |
| `content` | string | Full document text (searchable) |
| `category` | string | Document category (FAQ / policy / guide) |
| `content_vector` | Collection(Single) | Vector embedding for semantic search |

## Design Note

In this workshop, the knowledge layer is described as **Knowledge Foundation** because the business value is grounded customer operations, not just retrieval. Azure AI Search is the delivery mechanism — the goal is that the agent answers customer questions from real enterprise content.

## Validation

The search index should be able to return relevant content for customer support questions such as:

- "What is the warranty period for product X?"
- "How do I register a support request?"
- "What is the return policy?"
