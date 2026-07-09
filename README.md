# Workshop - Intelligent Customer Operations

Build an intelligent customer request automation solution using **Microsoft Foundry Agents** and **Azure AI Search**.

This repository is designed as both:

1. A learner portal published with GitHub Pages / MkDocs.
2. A hands-on workshop source repository containing sample application code, data, prompts, architecture assets, instructor materials, and deployment placeholders.

## Workshop Story

Participants will build an end-to-end customer request automation experience:

```text
Customer Request
    ↓
Customer Operations Web App
    ↓
Microsoft Foundry Agent
    ↓
Azure AI Search / Enterprise Knowledge
    ↓
Business APIs / Actions
    ↓
Response / Resolution / Escalation
```

## Recommended Repository Name

```text
workshop-intelligent-customer-operations
```

## High-Level Modules

| Module | Name | Outcome |
|---|---|---|
| 00 | Workshop Overview | Understand business scenario and target architecture |
| 01 | Prerequisites & Environment Setup | Prepare Azure, search, Foundry, GitHub, and Cloud Shell access |
| 02 | Knowledge Foundation with Azure AI Search | Prepare enterprise knowledge and search index |
| 03 | Build AI Agent with Microsoft Foundry | Create and test a customer operations agent |
| 04 | Business Actions & Tool Calling | Connect the agent to backend APIs / actions |
| 05 | Deploy Customer Operations App | Deploy frontend, API, and agent integration |
| 06 | End-to-End Validation | Validate request-to-resolution scenarios |
| 07 | Multi-Agent Extension | Extend into supervisor / specialist agent workflow |

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

## Architecture Split (Locked)

To avoid mixed priorities, this repo now follows two explicit lanes:

1. **Workshop Demo Lane (Cloud Shell path)**
   - Purpose: deterministic classroom/demo execution in Cloud Shell.
   - Entry: `scripts/setup-local.ps1`, `scripts/run-api.ps1`, `scripts/run-demo.ps1`.
   - Runtime: Aspire AppHost with workshop fallback where needed.
   - Success criteria: reproducible call simulation and observable call artifacts.

2. **Production Reference Lane (Azure-integrated path)**
    - Purpose: customer-facing reference implementation for real cloud integration.
    - Entry: `docs/deployment.md` + `azd` provisioning/deploy flow.
    - Runtime: Microsoft Foundry/ACS/Search/Storage/Container Apps integrations.
   - Success criteria: real resource-backed behavior, operational readiness, and security/compliance hardening.

**Rule:** keep shared code in Aspire services; keep lane-specific behavior behind configuration and deployment profiles, not separate legacy folders.

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

