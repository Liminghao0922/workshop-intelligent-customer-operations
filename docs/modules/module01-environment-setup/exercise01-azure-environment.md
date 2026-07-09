# Exercise 1 - Prepare Azure Environment

## Objective

Create the Azure resource group and confirm access to the subscription and services used in this workshop.

## Tasks

### 1. Confirm Azure Subscription Access

- Sign in to the [Azure Portal](https://portal.azure.com)
- Confirm you have **Contributor** or **Owner** role on the target subscription
- Note your subscription ID

### 2. Define a Naming Suffix

Open Azure Cloud Shell (`>_` icon) from the portal. In Azure Cloud Shell, make sure the shell is set to **PowerShell** first, then define one suffix and reuse it for workshop resource names:

```powershell
# If Cloud Shell opens in Bash, switch to PowerShell in the Cloud Shell toolbar first.
$postfix = "workshop"   # replace with your team or customer suffix
$resourceGroup = "rg-customer-operations-$postfix"
```

### 3. Create a Resource Group

Create a dedicated resource group for this workshop in Cloud Shell:

```powershell
az group create --name $resourceGroup --location japaneast
```

Recommended naming: `rg-customer-operations-$postfix`

### 4. Confirm Required Resource Providers

The **Contributor** or **Owner** role confirmed earlier normally gives you permission to create the workshop resources. Before continuing, also confirm the required Azure resource providers are registered in the subscription. This avoids deployment failures later.

Run the following commands in Azure Cloud Shell (PowerShell):

```powershell
az provider show --namespace Microsoft.Search --query "registrationState" --output tsv
az provider show --namespace Microsoft.CognitiveServices --query "registrationState" --output tsv
az provider show --namespace Microsoft.App --query "registrationState" --output tsv
az provider show --namespace Microsoft.Storage --query "registrationState" --output tsv
```

Each command should return `Registered`.

If any provider returns `NotRegistered`, register it:

```powershell
az provider register --namespace Microsoft.Search
az provider register --namespace Microsoft.CognitiveServices
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.Storage
```

Wait a few minutes, then run the `az provider show` commands again until each provider returns `Registered`.

### 5. Record Environment Values

After the resource group is ready, continue in Azure Cloud Shell (PowerShell). If you have not cloned this workshop repository in Cloud Shell yet, clone it first and move into the repository root:

```powershell
git clone https://github.com/Liminghao0922/workshop-intelligent-customer-operations.git
Set-Location workshop-intelligent-customer-operations
```

The environment template is included in the repository at `config/workshop.env.example`. Copy it to `.env` in the repository root, then fill in the values you already know:

```powershell
Copy-Item config/workshop.env.example .env
```

Keep `.env` in the repository root. Do not place it under `config/`, because later workshop commands read `.env` from the repository root. Do not commit `.env`, because it contains environment-specific values and may contain secrets.

```env
AZURE_SUBSCRIPTION_ID=<your subscription id>
AZURE_RESOURCE_GROUP=rg-customer-operations-$postfix
AZURE_LOCATION=japaneast
```

## Recommended Region

Use **Japan East** (`japaneast`) for this workshop. Ensure the region supports Microsoft Foundry model deployments before proceeding.

## Validation

- [ ] Signed in to Azure Portal
- [ ] Resource group created and visible in the portal
- [ ] Required resource providers return `Registered`
- [ ] Subscription ID and resource group name recorded in `.env`
- [ ] `config/workshop.env.example` copied to `.env`
- [ ] `.env` saved in the repository root in Azure Cloud Shell
- [ ] `$postfix` defined and reused for naming
