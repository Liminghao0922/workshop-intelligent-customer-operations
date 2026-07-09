# Observability & Evaluation (Aspire)

## Trace Key

Use `callId` as primary trace key. Track it across:

- `POST /api/dev/simulate-call`
- `GET /api/calls/{id}`
- `POST /api/admin/analyze/{callId}`
- ticket creation and escalation artifacts

## Minimum Logging

- provider event type and callback result
- Foundry invocation result (source + conversation mapping)
- `analyticsStatus` transition
- ticket creation outcome (`ticket.id` or failure artifact)
- call ID

## Response Evaluation Checklist

- [ ] Call record created and updated correctly
- [ ] Transcript grows with each turn
- [ ] Escalation creates ticket only when needed
- [ ] Post-call analysis written to `postCallResult`

## Demo Validation Table

| Scenario | Expected Call State | Expected Outcome |
|---|---|---|
| Simulated inbound call | `status=active` then callback updates | transcript entries visible |
| Escalation request | escalation decision true | ticket created |
| Post-call analysis | `analyticsStatus=submitted` | `postCallResult` present |
| Azure mode path | `source=foundry-agent-sdk` | real Foundry response |
