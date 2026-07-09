# Data Flow

## Request-to-Resolution Flow

```text
1. Customer submits request through web app.
2. Backend sends message to Microsoft Foundry Agent.
3. Agent classifies intent and determines next step.
4. Agent uses Azure AI Search for knowledge-grounded response when appropriate.
5. Agent calls business action tools when operational data or execution is required.
6. Agent returns final response or escalation recommendation.
7. Web app displays response to the customer.
```

## Data Categories

| Data | Example | Used By |
|---|---|---|
| Knowledge documents | FAQ, policy, troubleshooting guide | Azure AI Search / Agent |
| Operational data | request ID, order status | Business APIs |
| Conversation data | user request, agent response | App / Agent |
| Escalation data | reason, priority, next owner | Human workflow |

