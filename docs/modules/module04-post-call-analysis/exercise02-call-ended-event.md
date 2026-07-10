# Exercise 2 - Publish and Process the Call-ended Event

## Objective

Complete the asynchronous path from `CallDisconnected` through Azure Event Hubs to the post-call Azure Function Worker.

## Event Contract

Gateway publishes this versioned envelope to the `call-ended` Event Hub:

```json
{
  "schemaVersion": "1.0",
  "eventId": "<stable-call-id>:ended",
  "eventType": "customer.call.ended",
  "occurredAt": "2026-01-01T00:00:00Z",
  "callId": "<stable-call-id>",
  "language": "en",
  "transcript": [],
  "artifactReferences": []
}
```

Use `callId + ":ended"` as the stable event ID and `callId` as the partition key. Event Hubs delivery is at least once, so the Worker must tolerate duplicate events.

## 1. Review Gateway Publishing

In `AcsAdapter.cs`, locate the `CallDisconnected` branch. It must:

1. Mark the call completed.
2. Save the call artifact.
3. Publish through `PostCallPublisher`.
4. Set `AnalyticsStatus` to `submitted` only after a successful send.
5. Log `callId` and `eventId`, never transcript text or phone number.

In `PostCallPublisher.cs`, confirm that Azure mode uses `EventHubProducerClient` with managed identity and sends the versioned envelope with `callId` as its partition key.

## 2. Review the Event Hub Trigger

Open `PostCallEventHubFunction.cs`. The trigger uses:

- Event Hub: `%POST_CALL_EVENT_HUB_NAME%`
- Connection prefix: `PostCallEventHub`
- Consumer group: `%POST_CALL_EVENT_HUB_CONSUMER_GROUP%`

Validate `schemaVersion`, `eventType`, `eventId`, `callId`, and `transcript` before analysis. Use `eventId` as the durable idempotency key before invoking the Call Analysis Agent.

Do not swallow processing exceptions in Azure mode. The Event Hubs extension retries failed batches and advances checkpoints only after successful processing.

## 3. Configure and Deploy the Worker

```powershell
$values = azd env get-values --output json | ConvertFrom-Json
$resourceGroup = $values.AZURE_RESOURCE_GROUP
$foundryEndpoint = $values.AZURE_AI_PROJECT_ENDPOINT
$eventHubNamespace = $values.POST_CALL_EVENT_HUB_FULLY_QUALIFIED_NAMESPACE
$eventHubName = $values.POST_CALL_EVENT_HUB_NAME
$consumerGroup = $values.POST_CALL_EVENT_HUB_CONSUMER_GROUP

$workerApp = az resource list `
  --resource-group $resourceGroup `
  --tag azd-service-name=postcall-worker `
  --query "[0].name" `
  --output tsv

az containerapp update `
  --name $workerApp `
  --resource-group $resourceGroup `
  --set-env-vars `
    "APP_MODE=azure" `
    "AZURE_AI_PROJECT_ENDPOINT=$foundryEndpoint" `
    "FOUNDRY_ANALYTICS_AGENT_ID=<call-analysis-agent-id>" `
    "PostCallEventHub__fullyQualifiedNamespace=$eventHubNamespace" `
    "POST_CALL_EVENT_HUB_NAME=$eventHubName" `
    "POST_CALL_EVENT_HUB_CONSUMER_GROUP=$consumerGroup"

azd deploy postcall-worker
azd deploy gateway
```

## 4. Verify Least-privilege Access

Shared Setup assigns these data-plane roles at Event Hub scope:

- Gateway: **Azure Event Hubs Data Sender**
- Worker: **Azure Event Hubs Data Receiver**

The Worker retains `AzureWebJobsStorage` for the Functions host and Event Hubs checkpoint storage. No Event Hubs access key is stored in application settings.

## Validation

- [ ] `CallDisconnected` publishes automatically
- [ ] Event body uses the versioned envelope
- [ ] `eventId` is stable and `callId` is the partition key
- [ ] Worker uses its dedicated `post-call-worker` consumer group
- [ ] Invalid contracts fail before agent invocation
- [ ] Duplicate completed events are skipped by the durable idempotency check
- [ ] No transcript, phone number, credential, or connection string appears in logs
