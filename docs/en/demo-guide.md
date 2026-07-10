# Demo Guide

## Local demo

The local app runs in mock mode so the instructor can test the console and API flow without Azure credentials.

```powershell
cd smart-call-center-quiz
dotnet run --project src/aspire/IntelligentCustomerOperations.AppHost
```

Open the Aspire dashboard URL shown in the terminal and browse to the `gateway` endpoint.

## What to demonstrate

1. Select English, Japanese, or Chinese.
2. Click **Start simulated call**.
3. Show the customer and AI voice-agent conversation.
4. Show the AI handoff decision and CRM ticket.
5. Show post-call analytics: PII redaction, summary, intent, sentiment, entities, action items, and dashboard metrics.
6. Connect each demo panel back to the architecture diagram.

## How this maps to a real Azure implementation

| Demo panel | Real implementation |
| --- | --- |
| Live call transcript | Azure Communication Services plus Voice Live API |
| Knowledge answer | Foundry Agent with Azure AI Search tool |
| Handoff card | Foundry function tool calling CRM/contact-center APIs |
| Stored call artifact | Azure Storage |
| Post-call workflow | Azure Functions (Event Hubs trigger) on Azure Container Apps |
| Summary and sentiment | Foundry analytics agent/model |
| Dashboard cards | Instructor console or Power BI |

## Instructor note

The local app is intentionally deterministic in mock mode. The deployed path uses ACS, Voice Live API, Foundry Agent/Models, Azure AI Search, Azure Storage, queue-driven Azure Functions on Azure Container Apps, and CRM/tool APIs. See `docs/deployment.md`.

