# Technical Demo

## Objective

Show the Aspire Gateway and post-call Worker implementing separate real-time and asynchronous paths.

## Flow

1. Call `/healthz`.
2. Call `/api/dev/simulate-call` for `en`, `ja`, `zh`.
3. Query `/api/calls` and inspect transcript + state.
4. In the Azure path, end a call and observe the automatic queue event.
5. Inspect structured analysis and the conditional Dynamics result.

`POST /api/admin/analyze/{callId}` remains a diagnostic fallback for isolating queue problems; it is not the target customer flow.

## Expected Outcome

Audience understands single Aspire codepath supports:

- local fallback mode for workshop/demo
- Azure-integrated mode for ACS, two Foundry agents, Search, Queue, and Dynamics
