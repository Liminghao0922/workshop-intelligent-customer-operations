# Deploy the Foundry-based smart call center (.NET Aspire + event-driven Functions)

This app is designed for a production-like POC using Microsoft Foundry, Azure Communication Services, Voice Live API, Azure AI Search, and event-driven Azure Functions hosted with Azure Container Apps.

## Architecture split policy

This repository uses a dual-lane model:

- **Workshop Demo Lane:** local deterministic run path for enablement/workshop delivery.
- **Production Reference Lane:** Azure-integrated deployment path for customer-facing reference implementation.

For this deployment document, treat all steps as **Production Reference Lane** scope.
Do not reintroduce separate legacy runtime folders for demo vs production; use Aspire services plus environment/profile-based behavior.

## Prerequisites

- Azure subscription with permission to create resources and assign RBAC roles.
- Azure Cloud Shell (PowerShell) with Azure CLI and Azure Developer CLI available.
- ACS phone number or direct routing configuration.
- Foundry/Voice Live model availability in the target region.

## Provision and deploy

Run the commands below in **Azure Cloud Shell (PowerShell)**:

```powershell
azd auth login
azd env new customer-operations-workshop
azd env set AZURE_LOCATION japaneast
azd env set VOICE_LIVE_MODEL "<deployment-name>"
azd env set VOICE_LIVE_MODEL_NAME "<model-name>"
azd env set VOICE_LIVE_MODEL_VERSION "<model-version>"
azd provision
azd deploy
```

Infrastructure behavior (Bicep):

- Foundry Account, Foundry Project, `gpt-5`, Azure AI Search, Event Hubs, and application infrastructure are created together.
- Application identities receive least-privilege role assignments on the resources created by this deployment.
- Voice Live model deployment is created when all three variables are set: `VOICE_LIVE_MODEL`, `VOICE_LIVE_MODEL_NAME`, and `VOICE_LIVE_MODEL_VERSION`.
- Agents are not provisioned automatically. Create the Knowledge Agent in Part 1 and the Call Analysis Agent in Part 3.

After deployment, configure these values if they were not provisioned automatically:

- `FOUNDRY_AGENT_ID` after Part 1
- `FOUNDRY_ANALYTICS_AGENT_ID` after Part 3
- `ACS_CONNECTION_STRING` or managed identity-based ACS settings
- `ACS_CALLBACK_SECRET`

## Post-deployment setup

1. Open the Portal endpoint from `FRONTEND_URL` and verify the Gateway health endpoint at `WEB_URL/healthz`.
2. Seed knowledge from the console.
3. In ACS, configure callbacks to `WEB_URL/api/acs/events` and `WEB_URL/api/acs/callbacks/{callId}` according to the ACS Call Automation flow you use.
4. Configure the Foundry Agent with:
   - Azure AI Search tool connected to the `customer-operations-knowledge` index.
   - Function tools pointing to `/api/foundry/tools/create-ticket` and `/api/foundry/tools/escalation-decision`.
   - Instructions for English, Japanese, and Chinese support.
5. If model deployment was skipped, set model variables and rerun `azd provision`.
6. Place a test call and verify live call state, transcript, handoff, persisted artifacts, and post-call analytics.
7. Run azure-mode validation script after gateway is reachable:

```powershell
.\scripts\foundry\validate-azure-mode.ps1 -GatewayBaseUrl "<WEB_URL>"
```

## Local development

Optional, if you want to run the Aspire app from the same Cloud Shell session:

```powershell
dotnet run --project src/aspire/IntelligentCustomerOperations.AppHost
```

The Aspire AppHost orchestrates `gateway`, `api`, `portal`, and `postcall-worker` locally. In Azure, the Portal calls API, API calls Gateway, and Gateway publishes call-ended events to Event Hubs for the Worker to consume asynchronously.

