# Data Flow

## Real-time Call Flow

```text
1. Customer calls the ACS phone number.
2. Event Grid delivers IncomingCall to Voice Gateway.
3. Gateway answers and receives speech-recognition callbacks.
4. Gateway sends the utterance to the Knowledge Agent.
5. Knowledge Agent retrieves approved content from Azure AI Search.
6. Gateway plays the grounded answer through ACS.
```

## Post-call Flow

```text
1. Call Automation sends CallDisconnected to Gateway.
2. Gateway persists the completed record and publishes customer.call.ended.
3. Storage Queue delivers the event to the Azure Function Worker.
4. Worker masks PII before invoking the Call Analysis Agent.
5. Worker validates the structured recommendation and applies deterministic policy.
6. Worker upserts a Dynamics Case only when follow-up is required.
7. Worker stores a durable result keyed by the stable event ID.
```

## Data Categories

| Data | Example | Used By |
| --- | --- | --- |
| Knowledge documents | FAQ, policy, troubleshooting guide | Azure AI Search / Agent |
| Conversation data | customer and assistant turns | Gateway / post-call Worker |
| Call-ended event | call ID, language, artifact references | Queue / Worker |
| Masked analysis | summary, resolution, follow-up recommendation | Call Analysis Agent / Worker |
| Case data | summary, priority, source call ID | Dynamics 365 |

Queue events carry transcript data only for workshop simplicity. Production implementations should prefer encrypted artifact references with access scoped to Worker.

