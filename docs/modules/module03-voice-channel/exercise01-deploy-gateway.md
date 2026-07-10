# Exercise 1 - Deploy the Voice Gateway

## Objective

Deploy Gateway into the environment created during Shared Setup and connect it to the Part 1 Knowledge Agent. Do not run `azd provision` again.

## 1. Select the Existing Environment

```powershell
$postfix = "workshop"   # use your Shared Setup value
$environmentName = "customer-operations-$postfix"
azd env select $environmentName

$values = azd env get-values --output json | ConvertFrom-Json
$resourceGroup = $values.AZURE_RESOURCE_GROUP
$foundryEndpoint = $values.AZURE_AI_PROJECT_ENDPOINT
```

## 2. Deploy Gateway

```powershell
azd deploy gateway

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

## 3. Connect the Knowledge Agent

```powershell
$knowledgeAgentId = "<your-Part-1-Knowledge-Agent-ID>"

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

Shared Setup already granted the Gateway managed identity access to Foundry, Search, Storage, Cosmos DB, and Event Hubs. Role assignments can take several minutes to propagate.

## 4. Run Pre-call Checks

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

- [ ] Existing `azd` environment selected
- [ ] `azd provision` was not run in Part 2
- [ ] Gateway image deployed to the pre-provisioned Container App
- [ ] `PUBLIC_BASE_URL` uses the public HTTPS Gateway URL
- [ ] Part 1 Knowledge Agent ID configured
- [ ] Simulated call returns a grounded response
