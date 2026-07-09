# Exercise 3 - Prepare Microsoft Foundry Project

## Objective

Create a Microsoft Foundry project and confirm a language model deployment is available for the customer operations agent.

## Tasks

### 1. Open Microsoft Foundry Portal

Navigate to [https://ai.azure.com](https://ai.azure.com) and sign in with your Azure account.

### 2. Create a Project

- Select or create a **Hub** connected to your workshop resource group
- Create a new **Project** within that hub
- Recommended project name: `customer-operations-$postfix`

### 3. Deploy a Language Model

In your project, navigate to **Deployments** → **Deploy model**:

- Recommended model: `gpt-4o` or `gpt-4o-mini`
- Deployment name: `gpt-4o` (use a simple name — this will be referenced in config)
- Confirm the deployment reaches **Succeeded** status

### 4. Get Project Endpoint

Navigate to **Project overview** → copy the **Azure AI Foundry endpoint** (format: `https://<hub>.services.ai.azure.com/...`)

### 5. Record Values

Add to your `.env`:

```env
FOUNDRY_PROJECT_ENDPOINT=https://<your-hub>.services.ai.azure.com/api/projects/<your-project>
FOUNDRY_MODEL_DEPLOYMENT=gpt-4o
FOUNDRY_AGENT_ID=
```

> `FOUNDRY_AGENT_ID` will be filled in Module 03 after the agent is created.

## Validation

- [ ] Microsoft Foundry project created and accessible in the portal
- [ ] Language model deployment confirmed as **Succeeded**
- [ ] Project endpoint and model deployment name recorded in `.env`
- [ ] Any supporting CLI commands were run from Azure Cloud Shell
- [ ] Project name uses the same `$postfix`
- [ ] Ready to proceed to Module 02 (agent creation is in Module 03)
