# Technical Demo

## Objective

Show Aspire runnable architecture with Gateway, Portal, and post-call worker.

## Flow

1. Call `/healthz`.
2. Call `/api/dev/simulate-call` for `en`, `ja`, `zh`.
3. Query `/api/calls` and inspect transcript + state.
4. Trigger `/api/admin/analyze/{callId}` and inspect post-call artifact.
5. Optionally call `/api/foundry/tools/create-ticket`.

## Expected Outcome

Audience understands single Aspire codepath supports:

- local fallback mode for workshop/demo
- Azure-integrated mode for real Foundry/Search/Dynamics
