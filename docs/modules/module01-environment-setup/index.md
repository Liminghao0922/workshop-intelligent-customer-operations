# Module 01 - Prerequisites & Environment Setup

## Goal

Prepare the Azure services, source code, and Cloud Shell access needed to run all workshop modules.

By the end of this module, every participant should have working access to:

- An Azure resource group with required services provisioned
- An Azure AI Search service ready for knowledge indexing (Module 02)
- A Microsoft Foundry project with a model deployment ready (Module 03)
- A cloned repository in Cloud Shell storage

## Topics

- Azure subscription and resource group setup
- Azure AI Search service provisioning
- Microsoft Foundry project and model deployment
- Cloud Shell environment and repository setup

## Required Azure Services

| Service | Used In | Notes |
|---|---|---|
| Azure Resource Group | All modules | Container for all workshop resources |
| Azure AI Search | Module 02 | Knowledge grounding for the agent |
| Microsoft Foundry | Module 03 | Agent creation and model deployment |
| Azure Container Apps (or App Service) | Module 05 | Hosting the customer operations app |
| Azure Storage | Module 05 | Supporting assets |

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
- [ ] Ready to proceed to Module 02

## Module Checklist

- [ ] Complete Exercise 1 - Azure Environment
- [ ] Complete Exercise 2 - Azure AI Search Service
- [ ] Complete Exercise 3 - Microsoft Foundry Project
- [ ] Fill in `config/workshop.env.example`
- [ ] Run Cloud Shell setup and confirm no errors
- [ ] Proceed to Module 02
