# Module 02 - Knowledge Foundation with Azure AI Search

## Goal

Build a fast, grounded knowledge layer so the customer operations agent can answer support questions with real enterprise content.

## Why Azure AI Search

Fabric-style query engines can be powerful, but first-query latency and cold-start behavior make them a poor fit for real-time agent calls. Azure AI Search gives the workshop a simpler path:

- Fast vector and hybrid retrieval
- No cold-start delay for the first customer question
- Direct integration with Microsoft Foundry
- Easy upload of FAQ, policy, and support documents

## Topics

- Prepare knowledge documents for indexing
- Create and configure an Azure AI Search index
- Connect the index to the agent as a knowledge source
- Validate grounded answers with customer-style questions

## What to Index

Recommended starter content:

- Product FAQ
- Support policy docs
- Troubleshooting guide
- Warranty and service terms
- Service request examples

## Implementation Note

The knowledge path in this workshop uses `SearchKnowledgeClient`:

```text
src/aspire/IntelligentCustomerOperations.Gateway/Services/SearchKnowledgeClient.cs
```

In azure mode, it queries Azure AI Search. In local/mock mode, it falls back to static workshop content.

## Expected Output

By the end of this module, participants should have:

- An Azure AI Search index populated with support content
- A set of sample questions that return grounded answers
- Search endpoint, index name, and key recorded for Module 03

## Exit Criteria

- [ ] Knowledge documents prepared and uploaded
- [ ] Azure AI Search index configured
- [ ] At least one FAQ-style query returns a grounded answer
- [ ] Search connection values recorded for the agent module
- [ ] Ready to proceed to Module 03

## Module Checklist

- [ ] Read module overview
- [ ] Complete Exercise 1 - Prepare Enterprise Knowledge Data
- [ ] Complete Exercise 2 - Configure Azure AI Search
- [ ] Complete Exercise 3 - Validate Knowledge Answers
- [ ] Record any missing content or gaps
- [ ] Proceed to Module 03
