# Exercise 2 - Publish and Process the Call-ended Event

## Objective

Complete the event path from `CallDisconnected` to the queue-trigger Function.

## Event Contract

Publish a versioned envelope rather than an unlabelled transcript:

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

Use `callId + ":ended"` as the stable event ID. Queue delivery is at least once, so consumers must expect duplicates.

## 1. Complete Gateway Publishing

Open `AcsAdapter.cs` and locate the `CallDisconnected` branch in `HandleCallCallbackAsync`.

After the call record is marked completed:

1. Load the updated call record.
2. Save its artifact through `StorageRepository`.
3. Publish the versioned call-ended envelope through `PostCallPublisher`.
4. Set `AnalyticsStatus` to `submitted` only after the queue send succeeds.
5. Log `callId` and `eventId`, but not transcript content or phone number.

Keep this operation outside the in-memory update callback because publishing is asynchronous.

## 2. Update the Publisher Contract

Open `PostCallPublisher.cs`. Replace the raw `CallRecord` queue payload with the versioned envelope above.

The publisher must:

- Use managed identity in Azure mode.
- Keep Base64 queue-message encoding.
- Avoid embedding storage credentials.
- Return the queue name, `callId`, and `eventId` for diagnostics.

## 3. Make the Function Idempotent

Open `PostCallQueueFunction.cs` and validate these fields before analysis:

- `schemaVersion == "1.0"`
- `eventType == "customer.call.ended"`
- `eventId` and `callId` are nonempty
- transcript exists

Use `eventId` as the processing key. Before invoking the agent, check the durable result store. If a completed result already exists, log a duplicate and return success.

Do not swallow processing exceptions in Azure mode. Throwing allows Azure Functions queue retries and eventual poison-queue handling.

## 4. Configure and Deploy Worker

```powershell
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
    "POST_CALL_QUEUE_NAME=post-call-jobs" `
    "POST_CALL_FAIL_ON_ERROR=true"

azd deploy postcall-worker
azd deploy gateway
```

## 5. Grant Queue Access

Confirm Gateway can send and Worker can receive queue messages. Use least-privilege data-plane roles scoped to the storage account:

- Gateway: **Storage Queue Data Message Sender**
- Worker: **Storage Queue Data Message Processor**

The infrastructure template provides the base managed identities. Verify assignments rather than using storage account keys.

## Validation

- [ ] `CallDisconnected` publishes automatically
- [ ] Queue payload uses the versioned envelope
- [ ] Event ID is stable across retries
- [ ] Worker rejects invalid contracts
- [ ] Duplicate completed events are skipped
- [ ] Worker exceptions participate in queue retry behavior
- [ ] No transcript or credential appears in logs
