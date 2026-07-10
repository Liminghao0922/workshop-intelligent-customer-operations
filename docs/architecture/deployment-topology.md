# Deployment Topology

## Suggested Deployment Units

```text
Resource Group
├── Azure Communication Services
├── Voice Gateway Container App
├── Storage Account and Queues
├── Post-call Function Container App
├── Optional Operations API and Portal
├── Microsoft Foundry Project
│   ├── Knowledge Agent
│   └── Call Analysis Agent
├── Azure AI Search
├── Cosmos DB / Processing Results
└── Application Insights / Log Analytics

External SaaS boundary
└── Dynamics 365 Customer Service
```

## Environments

| Environment | Purpose |
| --- | --- |
| Local | Development and module testing |
| Dev | Workshop shared environment |
| Demo | Customer-facing demonstration |
| Production | Future production reference only |
