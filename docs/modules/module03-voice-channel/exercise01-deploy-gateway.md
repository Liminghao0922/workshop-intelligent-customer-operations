# Exercise 1 - Deploy the Voice Gateway

## Objective

Provision the shared application hosting, deploy Gateway, and connect it to the Part 1 Knowledge Agent.

## 1. Select the Workshop Environment

Run from the repository root in Azure Cloud Shell PowerShell:

```powershell
$postfix = "workshop"   # use your shared setup value
$environmentName = "customer-operations-$postfix"
$resourceGroup = "rg-$environmentName"
$location = "japaneast"

azd auth login
azd env select $environmentName
azd env set AZURE_LOCATION $location
```

If the environment does not exist, create it with `azd env new $environmentName`.

## 2. Preview and Provision

```powershell
azd provision --preview
```

Review the proposed resources, then provision:

```powershell
azd provision
```

The template creates the ACS resource, Container Apps environment, Gateway host, Storage queues, monitoring, and the resources used in Part 3.

## 3. Deploy Gateway

```powershell
azd deploy gateway
```

Discover its public URL:

```powershell
$gatewayApp = az resource list `
  --resource-group $resourceGroup `
  --tag azd-service-name=gateway `
  --query "[0].name" `
  --output tsv

$gatewayFqdn = az containerapp show `
  --name $gatewayApp `
  --resource-group $resourceGroup `
  --query properties.configuration.ingress.fqdn `
  --output tsv

$gatewayUrl = "https://$gatewayFqdn"
```

## 4. Configure the Knowledge Agent

Load the endpoint and agent ID recorded in Part 1, then configure Gateway:

```powershell
$foundryEndpoint = "<your-foundry-project-endpoint>"
$knowledgeAgentId = "<your-knowledge-agent-id>"

az containerapp update `
  --name $gatewayApp `
  --resource-group $resourceGroup `
  --set-env-vars `
    "APP_MODE=azure" `
    "PUBLIC_BASE_URL=$gatewayUrl" `
    "AZURE_AI_PROJECT_ENDPOINT=$foundryEndpoint" `
    "FOUNDRY_AGENT_ID=$knowledgeAgentId" `
    "AZURE_SEARCH_INDEX_NAME=customer-operations-knowledge"
```

`PUBLIC_BASE_URL` is required because Gateway uses it to construct the per-call HTTPS callback URI passed to Call Automation.

## 5. Grant Foundry Access

Use the Gateway system-assigned identity and the same Foundry role assignments established in the deployment setup:

```powershell
$gatewayPrincipalId = az containerapp show `
  --name $gatewayApp `
  --resource-group $resourceGroup `
  --query identity.principalId `
  --output tsv

$foundryAccount = ([uri]$foundryEndpoint).Host.Split('.')[0]
$foundryProject = ($foundryEndpoint.TrimEnd('/') -split '/')[-1]
$foundryAccountId = az cognitiveservices account show `
  --name $foundryAccount --resource-group $resourceGroup --query id --output tsv
$foundryProjectId = az resource show `
  --name "$foundryAccount/$foundryProject" `
  --resource-group $resourceGroup `
  --resource-type "Microsoft.CognitiveServices/accounts/projects" `
  --query id --output tsv

az role assignment create `
  --assignee-object-id $gatewayPrincipalId `
  --assignee-principal-type ServicePrincipal `
  --role "53ca6127-db72-4b80-b1b0-d745d6d5456d" `
  --scope $foundryProjectId

az role assignment create `
  --assignee-object-id $gatewayPrincipalId `
  --assignee-principal-type ServicePrincipal `
  --role "a97b65f3-24c7-4388-baec-2e87135dc908" `
  --scope $foundryAccountId
```

Role assignments can take several minutes to propagate.

## 6. Run Pre-call Checks

```powershell
Invoke-RestMethod "$gatewayUrl/healthz"
Invoke-RestMethod "$gatewayUrl/api/config"

Invoke-RestMethod `
  -Method Post `
  -Uri "$gatewayUrl/api/dev/simulate-call" `
  -ContentType "application/json" `
  -Body '{"language":"en"}'
```

The configuration response must show `mode: azure` and `foundryConfigured: true`.

## Validation

- [ ] Provisioning preview was reviewed
- [ ] Gateway image was deployed
- [ ] Public Gateway URL uses HTTPS
- [ ] `PUBLIC_BASE_URL` points to Gateway
- [ ] Part 1 Knowledge Agent ID is configured
- [ ] Gateway identity has Foundry access
- [ ] Simulated call returns a grounded assistant response
