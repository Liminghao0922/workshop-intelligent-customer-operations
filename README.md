# Workshop - Intelligent Customer Operations

Build an intelligent voice support lifecycle using **Microsoft Foundry Agents**, **Azure AI Search**, **Azure Communication Services**, **Azure Functions**, and **Dynamics 365**.

This repository is designed as both:

1. A learner portal published with GitHub Pages / MkDocs.
2. A hands-on workshop source repository containing sample application code, data, prompts, architecture assets, instructor materials, and deployment placeholders.

## Workshop Story

Participants build an end-to-end customer call experience:

```text
Customer Call → ACS Voice Channel → Knowledge Agent → Azure AI Search
                                           ↓
CallDisconnected → Storage Queue → Azure Function → Call Analysis Agent
                                           ↓
                              Conditional Dynamics 365 Case
```

## Recommended Repository Name

```text
workshop-intelligent-customer-operations
```

## High-Level Modules

| Module | Name | Outcome |
| --- | --- | --- |
| 00 | Workshop Overview | Understand business scenario and target architecture |
| 01 | Shared Environment Setup | Prepare Azure, Search, Foundry, ACS, Storage, and hosting prerequisites |
| Part 1 | Build the Knowledge Agent | Create and validate Azure AI Search-grounded answers |
| Part 2 | Build the Voice Channel | Connect inbound ACS calls to the Knowledge Agent |
| Part 3 | Analyze Calls and Create Tickets | Process call-ended events and conditionally create Dynamics cases |

## Local Preview

```bash
pip install -r requirements.txt
mkdocs serve
```

Then open the local MkDocs URL shown in your terminal.

## Cloud Shell Runnable Demo (Aspire)

```powershell
.\scripts\setup-local.ps1
.\scripts\run-api.ps1
```

Run both commands in **Azure Cloud Shell (PowerShell)**. In a second tab or after the API is ready:

```powershell
.\scripts\run-demo.ps1 -GatewayBaseUrl http://localhost:61989
```

Optional minimal web UI:

```powershell
.\scripts\run-frontend.ps1
```

## Execution Lanes

To avoid mixed priorities, this repo now follows two explicit lanes:

1. **Local Simulation Lane**
   - Purpose: deterministic classroom/demo execution in Cloud Shell.
   - Entry: `scripts/setup-local.ps1`, `scripts/run-api.ps1`, `scripts/run-demo.ps1`.
   - Runtime: Aspire AppHost with workshop fallback where needed.
   - Success criteria: reproducible call simulation and observable call artifacts.

2. **Azure-integrated Workshop Lane**
    - Purpose: customer-facing reference implementation for real cloud integration.
    - Entry: `docs/deployment.md` + `azd` provisioning/deploy flow.
    - Runtime: Microsoft Foundry/ACS/Search/Storage/Container Apps integrations.
    - Success criteria: a real grounded voice call and an idempotent post-call Dynamics workflow.

The Portal and API remain an optional Operations Dashboard. They are not required to understand the three-Part call lifecycle.

## Publish to GitHub Pages

This repo includes a GitHub Actions workflow under `.github/workflows/publish-docs.yml`.

Recommended GitHub Pages setting:

```text
Source: GitHub Actions
```

## Repo Structure

```text
workshop-intelligent-customer-operations-v2/
├── docs/                 # Learner portal content
├── src/                  # .NET Aspire solution (AppHost + services)
├── data/                 # Sample customer operations data
├── infra/                # Infrastructure-as-code placeholders
├── prompts/              # Agent instructions and prompt assets
├── assets/               # Images, diagrams, icons
├── slides/               # Deck placeholders
├── instructor/           # Delivery guide, timing, speaker notes
├── scripts/              # Setup and utility scripts
├── .github/workflows/    # GitHub Pages publishing workflow
├── mkdocs.yml
└── requirements.txt
```
