# Exercise 3 - Prepare Microsoft Foundry Project

## Objective

Create a Microsoft Foundry project and confirm a `gpt-5` deployment is available for the Knowledge and Call Analysis agents.

## Tasks

### 1. Open Azure Portal

Navigate to [https://portal.azure.com](https://portal.azure.com) and sign in with your Azure account.

In the Azure portal search box, search for `Foundry`, then open **Microsoft Foundry**.

### 2. Create a Foundry Project

Create a new Foundry project in resource group:

- Select **Foundry** from the left menu
- Select **+ Create**
- Subscription: use your workshop subscription
- **Resource group**: `rg-customer-operations-$postfix`
- **Name**: `foundry-customer-operations-$postfix`
- **Region**: use the same location from Exercise 1, for example `japaneast`
- Project name: `proj-customer-operations-$postfix`
- Review and create the project

Wait until deployment completes successfully.

### 3. Deploy a Language Model

After deployment completes, select **Go to resource** and open your project.
Select **Go to Foundry portal**. In the Foundry portal, make sure **New Foundry** is enabled.
Navigate to **Build** → **Models** → **Deployments**, then select **Deploy** → **Deploy a base model**.
Select `gpt-5`, then select **Deploy** → **Default settings**.
Confirm that the deployment reaches **Succeeded** status.

### 4. Get Project Endpoint

Navigate to **Home** and copy the **Project endpoint**.

Expected format:

```text
https://<your-foundry-resource>.services.ai.azure.com/api/projects/<your-project>
```

### 5. Record Values

Add to your `.env`:

```env
FOUNDRY_PROJECT_ENDPOINT=https://<your-foundry-resource>.services.ai.azure.com/api/projects/<your-project>
FOUNDRY_MODEL_DEPLOYMENT=gpt-5
FOUNDRY_AGENT_ID=
```

> `FOUNDRY_AGENT_ID` is filled in Part 1. `FOUNDRY_ANALYTICS_AGENT_ID` is filled in Part 3.

## Validation

- [ ] Microsoft Foundry project created and accessible in the portal
- [ ] Language model deployment confirmed as **Succeeded**
- [ ] Project endpoint and model deployment name recorded in `.env`
- [ ] Foundry project created from Azure portal
- [ ] Project name uses the same `$postfix`
- [ ] Ready to proceed to Part 1
