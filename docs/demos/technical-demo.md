# Technical Demo

## Objective

Show a minimum runnable architecture with clear extension points for real Foundry and Fabric IQ.

## Flow

1. Call `/health`.
2. Call `/api/chat` with four scenarios.
3. Inspect `intent`, `toolCalled`, `toolName`, and `correlationId`.
4. Call `/api/tools/service-request-status` directly.

## Expected Outcome

Audience understands how mock adapters can be swapped with real Azure integrations in v4.
