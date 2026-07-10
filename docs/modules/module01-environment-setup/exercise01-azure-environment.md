# Exercise 1 - Provision the Shared Azure Environment

## Objective

Run one Azure Developer CLI deployment that creates every control-plane resource required by Parts 1-3. Later Parts add knowledge, agents, channel configuration, and business integration without provisioning a second environment.

## 1. Confirm Access

Sign in to the [Azure portal](https://portal.azure.com) and confirm that you have **Contributor** or **Owner** access to the target subscription. Open Azure Cloud Shell and select **PowerShell**.

Register the providers used by the workshop if they are not already registered:

```powershell
$providers = @(
  "Microsoft.App",
  "Microsoft.CognitiveServices",
  "Microsoft.Communication",
  "Microsoft.ContainerRegistry",
  "Microsoft.DocumentDB",
  "Microsoft.EventHub",
  "Microsoft.Search",
  "Microsoft.Storage"
)

foreach ($provider in $providers) {
  az provider register --namespace $provider
}
```

## 2. Clone the Workshop

```powershell
git clone https://github.com/Liminghao0922/workshop-intelligent-customer-operations.git
Set-Location workshop-intelligent-customer-operations
```

## 3. Create the azd Environment

Choose one suffix and reuse it throughout the workshop:

```powershell
$postfix = "workshop"   # replace with your team or customer suffix
$environmentName = "customer-operations-$postfix"
$location = "japaneast"

azd auth login
azd env new $environmentName
azd env set AZURE_LOCATION $location
```

If the environment already exists, use `azd env select $environmentName` instead of `azd env new`.

## 4. Preview and Provision Once

```powershell
azd provision --preview
azd provision
```

This deployment creates the shared resource group, Microsoft Foundry account and project, `gpt-5` deployment, Azure AI Search, Azure Communication Services, Container Apps, Container Registry, Storage, Cosmos DB, Event Hubs, Key Vault, and monitoring resources.

Agents, the Search index, phone number configuration, Event Grid subscription, and Dynamics configuration are intentionally created in the relevant Part.

## 5. Record Outputs

```powershell
azd env get-values
azd env get-values > .env
```

Keep `.env` in the repository root and do not commit it.

## Validation

- [ ] All required providers are registered
- [ ] The azd environment is named `customer-operations-$postfix`
- [ ] `azd provision --preview` was reviewed
- [ ] `azd provision` completed successfully once
- [ ] `azd env get-values` includes Foundry, Search, Event Hubs, Storage, and application outputs
- [ ] No Agent or Search index was created during provisioning
