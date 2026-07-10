# Progress Tracker - Intelligent Customer Operations

Last updated: 2026-07-06 (Asia/Tokyo)

## Session Recovery Result

- Prior chat session context in session history is mostly missing.
- Recoverable record found: one unfinished design-review kickoff prompt (session id: `c661d5fb-805d-4f76-9ee2-c4bd475a30dc`).
- Practical baseline now uses repo current state as source of truth.

## Current Repository State (Recovered)

- Branch: `main`
- Working tree: clean
- Recent commits:
  - `436062a` update code
  - `9654572` init code
  - `9d7b925` Initial commit

## Aspire Migration Mapping (Merged)

| Legacy Path | Aspire Target | Merge Decision |
| --- | --- | --- |
| `src/api/CustomerOperations.Api` | `src/aspire/IntelligentCustomerOperations.Gateway` + `src/aspire/IntelligentCustomerOperations.ApiService` | Legacy removed |
| `src/agent/*MockFoundry*` | `src/aspire/IntelligentCustomerOperations.Gateway/Services/FoundryClient.cs` | Legacy removed, real Azure mode + local fallback in one place |
| `src/knowledge/*MockFabricIq*` | `src/aspire/IntelligentCustomerOperations.Gateway/Services/SearchKnowledgeClient.cs` | Legacy removed, retrieval path consolidated |
| `src/functions/*` | `src/aspire/IntelligentCustomerOperations.Gateway/Services/TicketService.cs` (+ tool endpoints in `Program.cs`) | Legacy removed |
| `src/frontend` | `src/aspire/IntelligentCustomerOperations.Portal` | Legacy removed |

### Merge Actions Completed (2026-06-29)

- Deleted obsolete legacy folders/files under `src/api`, `src/agent`, `src/knowledge`, `src/functions`, `src/frontend`.
- Updated local scripts to Aspire runtime:
  - `scripts/setup-local.ps1` -> restore Aspire solution
  - `scripts/run-api.ps1` -> start AppHost
  - `scripts/run-frontend.ps1` -> start Portal project
  - `scripts/run-demo.ps1` -> use Gateway health + simulate-call + call list
- Updated docs pointers:
  - `README.md`
  - `scripts/README.md`
  - `src/README.md`
- Synced docs/contracts to Aspire endpoints:
  - `contracts/openapi/customer-operations-api.yaml`
  - `docs/architecture/sequence-flow.md`
  - `docs/architecture/observability-evaluation.md`
  - `docs/demos/technical-demo.md`
  - `docs/modules/module01-environment-setup/index.md`
  - Legacy validation module was replaced by validation inside Parts 1-3.
- Physically removed legacy folders from repository:
  - `src/agent`
  - `src/api`
  - `src/frontend`
  - `src/functions`
  - `src/knowledge`

## What Is Already Built

| Area | Status | Notes |
| --- | --- | --- |
| Workshop docs structure | Done | Shared Setup plus three lifecycle Parts present |
| Local runnable demo path | Done | setup/run scripts in `scripts/`, demo guide in docs |
| API + agent/knowledge adapters | Done (Aspire consolidated) | `Gateway/Services/FoundryClient.cs` + `SearchKnowledgeClient.cs` |
| Aspire-based gateway flow | Done (baseline) | call events, callbacks, ticket/escalation endpoints |
| Infra template baseline | Done | Shared Setup provisions Foundry, `gpt-5`, Search, Event Hubs, ACS, Storage, hosting, and RBAC |
| Automated test coverage | Partial | only prompt test asset found under `tests/e2e-prompts` |

## Key Gaps To Complete Next

| Priority | Gap | Evidence |
| --- | --- | --- |
| P0 | Knowledge grounding quality tuning pending | ranking/filter strategy and multilingual relevance tests pending |
| P1 | Production integration validation pending | Worker policy and Dynamics upsert are implemented; live Dataverse credentials and alternate key still require environment validation |
| P1 | Validation/tests not complete | no broad automated test suite found |
| P2 | Delivery hardening pending | deployment/security/observability runbook needs final pass |

Completed in the current architecture pass:

- Gateway publishes the versioned `customer.call.ended` envelope from `CallDisconnected` to Event Hubs.
- Worker consumes `call-ended` through the `post-call-worker` consumer group.
- Cosmos-backed completion records provide durable `eventId` idempotency for successful processing.
- Worker deterministically applies the case policy and upserts Dynamics `incident` by `ico_callid`.
- The separate `callback-jobs` Storage Queue remains unchanged.

## Action Plan (Track Here)

| ID | Task | Owner | Status | Target Date | Notes |
| --- | --- | --- | --- | --- | --- |
| A1 | Confirm target architecture split: workshop demo vs production reference | Team | Done | 2026-06-30 | Locked in `README.md` + `docs/deployment.md` |
| A2 | Validate Azure Foundry Agent flow in non-mock mode | Team | In Progress | 2026-07-03 | Added `scripts/foundry/validate-azure-mode.ps1` |
| A3 | Tune Search/Fabric grounding quality and multilingual ranking | Team | Todo | 2026-07-03 | Define retrieval contract + eval set |
| A4 | Add end-to-end automated validation (API + call flow + post-call) | Team | Todo | 2026-07-05 | CI gate before demo/workshop publish |
| A5 | Finalize infra + deployment runbook for repeatable environment bring-up | Team | Todo | 2026-07-06 | azd/bicep path + config checklist |
| A6 | Implement deterministic Dynamics policy validation and idempotent incident upsert | Team | Done | 2026-07-06 | Agent recommends; Worker code authorizes and writes |

## Curriculum Restructure

| ID | Module | Status | Notes |
| --- | --- | --- | --- |
| M00 | Workshop Overview | Done | Reframed around the customer call lifecycle |
| M01 | Shared Environment Setup | Done | Common Search, Foundry, ACS, Storage, and hosting prerequisites |
| P1 | Knowledge Agent | Done | Search preparation, agent creation, and multilingual RAG validation |
| P2 | Voice Channel | Done | Gateway deployment, ACS events, and live-call validation |
| P3 | Post-call Operations | Done | Analysis Agent, call-ended event, Function, and Dynamics exercises |

## Review Queue

| ID | Module | Review State | What Changed |
| --- | --- | --- | --- |
| M00 | Workshop Overview | Updated | Call lifecycle and two-agent boundary |
| M01 | Shared Setup | Updated | Common prerequisites for all Parts |
| P1 | Knowledge Agent | New | Search-grounded live-answer path |
| P2 | Voice Channel | New | ACS-to-Knowledge-Agent path |
| P3 | Post-call Operations | New | Asynchronous analysis and conditional Case path |

## Decision Log

| Date | Decision | Source | Status |
| --- | --- | --- | --- |
| 2026-07-06 | Core curriculum uses exactly two agents: Knowledge Agent and Call Analysis Agent; Voice is a channel, not an agent | team | Active |
| 2026-07-06 | Shared Setup owns all control-plane resources through one `azd provision`; Parts create data-plane assets and the two Agents | team | Active |
| 2026-07-06 | Post-call transport uses Event Hubs (`call-ended`); `callback-jobs` remains a separate Storage Queue path | team | Active |
| 2026-07-06 | Deployment and validation steps live inside each Part; standalone Deploy, Validation, and Multi-Agent modules removed | team | Active |
| 2026-07-06 | Cloud Shell becomes command baseline for workshop setup/deploy docs; remove portal UI operations when a CLI/PowerShell path is available | team | Active |
| 2026-07-02 | Knowledge layer switched from Fabric IQ to Azure AI Search across all docs. Reason: Fabric IQ cold-start latency 30–50s unsuitable for real-time agent calls. Fabric IQ retained in Future Expansion section only. | team | Active |
| 2026-06-29 | Weekly automatic import removed; run manual confirmed one-time execution first | memory | Needs reconfirm |
| 2026-06-29 | Milestone source via weekly Saturday Excel snapshot; prioritize accuracy over full automation for Dynamics activity logging | memory | Needs reconfirm |

## Update Rules

1. Update this file at each major milestone (design lock, integration done, test pass, deployment ready).
2. Only mark `Done` with concrete artifact/link/commit.
3. Add blocker directly into Notes column with owner + date.
