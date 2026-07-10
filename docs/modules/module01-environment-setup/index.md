# Module 01 - Shared Environment Setup

## Goal

Prepare the Azure services, source code, and Cloud Shell access needed to run all workshop modules.

By the end of this module, every participant should have working access to:

- An Azure resource group with required services provisioned
- An Azure AI Search service and knowledge index ready for agent grounding
- A Microsoft Foundry project with a `gpt-5` deployment ready for Parts 1 and 3
- A cloned repository in Cloud Shell storage

## Topics

- Azure subscription and resource group setup
- Azure AI Search service provisioning
- Microsoft Foundry project and model deployment
- Cloud Shell environment and repository setup

## Required Azure Services

| Service | Used In | Notes |
| --- | --- | --- |
| Azure Resource Group | All modules | Container for all workshop resources |
| Azure AI Search | Part 1 and Part 2 | Knowledge grounding for live answers |
| Microsoft Foundry | Part 1 and Part 3 | Knowledge and Call Analysis agents |
| Azure Communication Services | Part 2 | Inbound PSTN voice channel |
| Azure Container Apps | Part 2 and Part 3 | Voice Gateway and post-call Worker hosting |
| Azure Storage | Part 3 | Call artifacts and post-call queue |
| Dynamics 365 | Part 3 | Cases requiring human follow-up |

## Cloud Shell Setup Path

After completing environment provisioning, run the verification commands in **Azure Cloud Shell (PowerShell)**:

```powershell
.\scripts\setup-local.ps1
.\scripts\run-api.ps1
```

In a second terminal:

```powershell
.\scripts\run-demo.ps1 -GatewayBaseUrl http://localhost:61989
```

## Expected Output

By the end of this module, participants should have:

- Azure resource group confirmed with required services accessible
- Azure AI Search endpoint and API key recorded
- Microsoft Foundry project endpoint and model deployment name recorded
- Cloud Shell access confirmed and repository cloned
- `config/workshop.env.example` values filled in for their environment

Resource names in this module are derived from one Cloud Shell variable:

```powershell
$postfix = "workshop"   # replace with your team or customer suffix
```

## Exit Criteria

- [ ] Azure subscription and resource group confirmed
- [ ] Azure AI Search service created and endpoint recorded
- [ ] Microsoft Foundry project created and model deployment confirmed
- [ ] Azure Cloud Shell available and ready
- [ ] Repository cloned and `setup-local.ps1` runs without errors in Cloud Shell
- [ ] Resource names are derived from `$postfix`
- [ ] Ready to proceed to Part 1

## Module Checklist

- [ ] Complete Exercise 1 - Azure Environment
- [ ] Complete Exercise 2 - Azure AI Search Service
- [ ] Complete Exercise 3 - Microsoft Foundry Project
- [ ] Fill in `config/workshop.env.example`
- [ ] Run Cloud Shell setup and confirm no errors
- [ ] Proceed to Part 1
