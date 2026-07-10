# Module 01 - Shared Environment Setup

## Goal

Provision all shared Azure resources once, then validate the services used by Parts 1-3.

By the end of this module, every participant should have working access to:

- An Azure resource group with required services provisioned
- An Azure AI Search service ready for the Part 1 knowledge index
- A Microsoft Foundry project with a `gpt-5` deployment ready for Parts 1 and 3
- A cloned repository in Cloud Shell storage

## Topics

- One-time `azd provision`
- Azure AI Search validation
- Microsoft Foundry project and model validation
- Cloud Shell environment and repository setup

## Required Azure Services

| Service | Used In | Notes |
| --- | --- | --- |
| Azure Resource Group | All modules | Container for all workshop resources |
| Azure AI Search | Part 1 and Part 2 | Knowledge grounding for live answers |
| Microsoft Foundry | Part 1 and Part 3 | Knowledge and Call Analysis agents |
| Azure Communication Services | Part 2 | Inbound PSTN voice channel |
| Azure Container Apps | Part 2 and Part 3 | Voice Gateway and post-call Worker hosting |
| Azure Storage | Part 2 and Part 3 | Call artifacts and Functions host storage |
| Azure Event Hubs | Part 3 | Durable call-ended event transport |
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

- Shared infrastructure created by one `azd provision`
- Azure AI Search endpoint recorded
- Microsoft Foundry project endpoint and model deployment name recorded
- Cloud Shell access confirmed and repository cloned
- `config/workshop.env.example` values filled in for their environment

Resource names in this module are derived from one Cloud Shell variable:

```powershell
$postfix = "workshop"   # replace with your team or customer suffix
```

## Exit Criteria

- [ ] `azd provision` completed once
- [ ] Azure AI Search service validated
- [ ] Microsoft Foundry project and model deployment validated
- [ ] Azure Cloud Shell available and ready
- [ ] Repository cloned and `setup-local.ps1` runs without errors in Cloud Shell
- [ ] Resource names are derived from `$postfix`
- [ ] Ready to proceed to Part 1

## Module Checklist

- [ ] Complete Exercise 1 - Azure Environment
- [ ] Complete Exercise 2 - Azure AI Search Service
- [ ] Complete Exercise 3 - Microsoft Foundry Project
- [ ] Save `azd env get-values` to the repository-root `.env`
- [ ] Run Cloud Shell setup and confirm no errors
- [ ] Proceed to Part 1
