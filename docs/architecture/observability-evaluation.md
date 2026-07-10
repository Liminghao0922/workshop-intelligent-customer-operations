# Observability & Evaluation (Aspire)

## Trace Key

Use `callId` as primary trace key. Track it across:

- `POST /api/dev/simulate-call`
- `GET /api/calls/{id}`
- call-ended `eventId`
- post-call processing result
- Dynamics Case source call ID

## Minimum Logging

- provider event type and callback result
- Knowledge Agent invocation result without prompt content
- queue submission and delivery status
- Call Analysis schema-validation outcome
- post-call outcome (`no_case`, `manual_review_required`, or Dynamics case number)
- call ID

## Response Evaluation Checklist

- [ ] Call record created and updated correctly
- [ ] Transcript grows with each turn
- [ ] Grounded answer does not invent policy
- [ ] Follow-up policy creates a Case only when needed
- [ ] Duplicate delivery does not create a duplicate Case
- [ ] Post-call result is stored durably

## Demo Validation Table

| Scenario | Expected Call State | Expected Outcome |
| --- | --- | --- |
| Simulated inbound call | `status=active` then callback updates | transcript entries visible |
| Resolved call | `status=completed` | no Dynamics Case |
| Unresolved call | validated follow-up decision | one Dynamics Case |
| Duplicate event | existing event ID | no second agent call or Case |
| Azure mode path | `source=foundry-agent-sdk` | real Foundry response |
