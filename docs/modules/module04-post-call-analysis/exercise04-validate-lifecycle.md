# Exercise 4 - Validate the Complete Lifecycle

## Objective

Prove that resolved and unresolved calls produce different post-call outcomes and that retries do not create duplicate cases.

## Scenario A - Resolved, No Case

1. Call the ACS number.
2. Ask what information is required for a warranty check.
3. Confirm the answer resolves the question.
4. End the call.

Expected result:

- One `customer.call.ended` event is processed.
- Analysis has `resolutionStatus: resolved`.
- `followUpRequired` is `false`.
- No Dynamics case is created.

## Scenario B - Unresolved, Create Case

1. Call again.
2. Report a duplicate charge that requires billing review.
3. Ask for human follow-up.
4. End the call.

Expected result:

- Analysis identifies an unresolved or follow-up state.
- Confidence satisfies the application policy.
- One Dynamics Case is upserted with the source `callId`.
- The result store contains the Case ID and case number.

## Scenario C - Duplicate Delivery

Resubmit the exact Scenario B event with the same `eventId` and `callId`.

Expected result:

- Worker identifies it as already completed.
- Agent is not invoked again.
- No second Dynamics Case is created.

## Observe the Pipeline

```powershell
az containerapp logs show `
  --name $workerApp `
  --resource-group $resourceGroup `
  --type console `
  --tail 150
```

Correlate logs by `callId` and `eventId`. Confirm the sequence:

```text
call ended -> queued -> analysis completed -> decision applied -> result stored
```

Review Application Insights for failures and queue retry behavior. Shared evidence must exclude transcripts, phone numbers, tokens, and secrets.

## Final Validation

- [ ] Resolved call creates no case
- [ ] Unresolved call creates one case
- [ ] Case contains summary, reason, priority, and source call ID
- [ ] Duplicate event creates no duplicate case
- [ ] PII masking occurs before Foundry invocation
- [ ] Logs correlate the full pipeline without sensitive content
- [ ] Exactly two Foundry agents are used across the workshop
