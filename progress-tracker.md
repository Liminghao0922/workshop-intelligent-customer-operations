# Progress Tracker - Intelligent Customer Operations

Last updated: 2026-07-06 11:57 (Asia/Tokyo)

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
|---|---|---|
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
  - `docs/modules/module04-business-actions/exercise01-business-api.md`
  - `docs/modules/module06-end-to-end-validation/index.md`
- Physically removed legacy folders from repository:
  - `src/agent`
  - `src/api`
  - `src/frontend`
  - `src/functions`
  - `src/knowledge`

## What Is Already Built

| Area | Status | Notes |
|---|---|---|
| Workshop docs structure | Done | `docs/modules/module00` to `module07` present |
| Local runnable demo path | Done | setup/run scripts in `scripts/`, demo guide in docs |
| API + agent/knowledge adapters | Done (Aspire consolidated) | `Gateway/Services/FoundryClient.cs` + `SearchKnowledgeClient.cs` |
| Aspire-based gateway flow | Done (baseline) | call events, callbacks, ticket/escalation endpoints |
| Infra template baseline | Done (starter) | `infra/main.bicep` + params/json present |
| Automated test coverage | Partial | only prompt test asset found under `tests/e2e-prompts` |

## Key Gaps To Complete Next

| Priority | Gap | Evidence |
|---|---|---|
| P0 | Azure mode end-to-end validation incomplete | ensure `foundry-agent-sdk` + search retrieval path validated with real resources |
| P0 | Knowledge grounding quality tuning pending | ranking/filter strategy and multilingual relevance tests pending |
| P1 | Production integration path mostly mock mode | docs + gateway/services show `APP_MODE=mock` defaults |
| P1 | Validation/tests not complete | no broad automated test suite found |
| P2 | Delivery hardening pending | deployment/security/observability runbook needs final pass |

## Action Plan (Track Here)

| ID | Task | Owner | Status | Target Date | Notes |
|---|---|---|---|---|---|
| A1 | Confirm target architecture split: workshop demo vs production reference | Team | Done | 2026-06-30 | Locked in `README.md` + `docs/deployment.md` |
| A2 | Validate Azure Foundry Agent flow in non-mock mode | Team | In Progress | 2026-07-03 | Added `scripts/foundry/validate-azure-mode.ps1` |
| A3 | Tune Search/Fabric grounding quality and multilingual ranking | Team | Todo | 2026-07-03 | Define retrieval contract + eval set |
| A4 | Add end-to-end automated validation (API + call flow + post-call) | Team | Todo | 2026-07-05 | CI gate before demo/workshop publish |
| A5 | Finalize infra + deployment runbook for repeatable environment bring-up | Team | Todo | 2026-07-06 | azd/bicep path + config checklist |

## Module Refinement Track (One by One)

| ID | Module | Status | Notes |
|---|---|---|---|
| M00 | Workshop Overview | Pending review | Cloud Shell wording added; customer-facing framing retained |
| M01 | Prerequisites & Environment Setup | In progress | Exercise01 reviewed; exercise02 renamed to Azure AI Search service and converted to Cloud Shell-only flow |
| M02 | Knowledge Foundation | Pending review | Switched to Azure AI Search (from Fabric IQ); module now focuses on indexing docs, low-latency retrieval, and grounded answer checks |
| M03 | Build Agent | Pending review | Microsoft Foundry agent module now maps to current Gateway integration path and Foundry knowledge/tool flow |
| M04 | Business Actions | Pending review | Tool-calling module now aligned to current business API endpoints and predictable workshop actions |
| M05 | Deploy Application | Pending review | Deployment module now matches current app/runtime assumptions and config flow |
| M06 | End-to-End Validation | Pending review | Scenario module now emphasizes request-to-resolution checks and call metadata validation |
| M07 | Multi-Agent Extension | Pending review | Extension module now frames multi-agent design as future expansion, not workshop core |

## Review Queue

| ID | Module | Review State | What Changed |
|---|---|---|---|
| M00 | Workshop Overview | Pending review | Cloud Shell wording added; customer-facing framing retained |
| M01 | Prerequisites & Environment Setup | In progress | Exercise01 reviewed; exercise02 rename + Cloud Shell-only Azure AI Search setup underway |
| M02 | Knowledge Foundation | Not reviewed | AI Search knowledge foundation and validation flow |
| M03 | Build Agent | Not reviewed | Microsoft Foundry agent creation and connection flow |
| M04 | Business Actions | Not reviewed | API/tool-calling alignment |
| M05 | Deploy Application | Not reviewed | Deployment and configuration flow |
| M06 | End-to-End Validation | Not reviewed | Scenario validation and call metadata |
| M07 | Multi-Agent Extension | Not reviewed | Future expansion into multi-agent design |

## Decision Log

| Date | Decision | Source | Status |
|---|---|---|---|
| 2026-07-06 | Cloud Shell becomes command baseline for workshop setup/deploy docs; remove portal UI operations when a CLI/PowerShell path is available | team | Active |
| 2026-07-02 | Knowledge layer switched from Fabric IQ to Azure AI Search across all docs. Reason: Fabric IQ cold-start latency 30–50s unsuitable for real-time agent calls. Fabric IQ retained in Future Expansion section only. | team | Active |
| 2026-06-29 | Weekly automatic import removed; run manual confirmed one-time execution first | memory | Needs reconfirm |
| 2026-06-29 | Milestone source via weekly Saturday Excel snapshot; prioritize accuracy over full automation for Dynamics activity logging | memory | Needs reconfirm |

## Update Rules

1. Update this file at each major milestone (design lock, integration done, test pass, deployment ready).
2. Only mark `Done` with concrete artifact/link/commit.
3. Add blocker directly into Notes column with owner + date.
