# Reference Architecture

## Recommended design

The reference implementation is centered on Azure AI Foundry. ACS handles phone integration, Voice Live API handles real-time speech interaction, a Foundry Agent owns the conversation policy, Azure AI Search grounds answers, and an event-driven Azure Function runs post-call analytics asynchronously.

```mermaid
flowchart LR
  Caller[Customer caller] --> ACS[Azure Communication Services<br/>phone number / direct routing]
  ACS --> Gateway[Call gateway<br/>Azure Container Apps]
  Gateway <--> VoiceLive[Voice Live API]
  VoiceLive <--> Agent[Azure AI Foundry Agent]
  Agent <--> Models[Foundry Models<br/>realtime + analytics models]
  Agent --> Tools[Function tools<br/>ticket and escalation actions]
  Agent --> SearchTool[Azure AI Search tool<br/>Foundry project connection]
  SearchTool --> Search[(Azure AI Search<br/>multilingual knowledge index)]
  Gateway --> Blob[(Azure Storage<br/>call artifacts)]
  Blob --> Queue[(Azure Storage Queue<br/>post-call-jobs)]
  Queue --> Functions[Azure Functions (queue trigger)<br/>Azure Container Apps]
  Functions --> AnalyticsAgent[Foundry analytics agent/model]
  AnalyticsAgent --> Blob
  Functions --> CRM[(Dynamics 365 Customer Service<br/>ticketing)]
  Blob --> Dashboard[Instructor console / Power BI]
```

## Real-time call flow

1. The customer calls an ACS number or a number routed to ACS.
2. ACS sends call events to the call gateway.
3. The call gateway creates a Voice Live session and connects the customer conversation to the Foundry Agent.
4. The Foundry Agent uses Foundry Models, Azure AI Search grounding, and function tools to answer, decide escalation, or create a ticket.
5. The gateway stores call artifacts in Azure Storage.

## Post-call analytics flow

1. Completed call artifacts are published as events to an Azure Storage Queue.
2. Functions invoke a Foundry analytics agent/model for redaction-aware summary, intent, sentiment, entities, resolution status, and action items.
3. Structured analytics are saved back to Storage and surfaced in the instructor console or Power BI.

## Service mapping

| Requirement | Azure service |
| --- | --- |
| Phone integration | Azure Communication Services |
| Real-time speech-to-speech | Voice Live API |
| Conversation orchestration | Azure AI Foundry Agent |
| Model access | Foundry Models |
| Knowledge grounding | Azure AI Search tool connected to Foundry |
| App hosting | Azure Container Apps |
| Post-call workflow | Azure Functions (queue trigger) on Azure Container Apps |
| Ticket/case management | Dynamics 365 Customer Service |
| Artifact storage | Azure Storage |
| Monitoring | Application Insights and Log Analytics |
| Identity and access | Managed identity, RBAC, Key Vault |

## Language support

- Configure voice and model instructions for English, Japanese, and Chinese.
- Store the language code in call metadata.
- Index multilingual knowledge content in Azure AI Search.
- Evaluate voice-agent behavior and post-call analytics separately for each language.
