# Exercise 3 - Validate Microsoft Foundry

## Objective

Confirm that Shared Setup created the Microsoft Foundry account, project, and `gpt-5` deployment used by the two workshop agents.

## 1. Load the Outputs

```powershell
$values = azd env get-values --output json | ConvertFrom-Json
$resourceGroup = $values.AZURE_RESOURCE_GROUP
$foundryAccount = $values.FOUNDRY_ACCOUNT_NAME
$foundryProject = $values.FOUNDRY_PROJECT_NAME
$foundryEndpoint = $values.AZURE_AI_PROJECT_ENDPOINT
$modelDeployment = $values.FOUNDRY_MODEL_DEPLOYMENT
```

## 2. Validate the Resources

```powershell
az cognitiveservices account show `
  --name $foundryAccount `
  --resource-group $resourceGroup `
  --query "{name:name, kind:kind, location:location}" `
  --output table

az cognitiveservices account deployment show `
  --name $foundryAccount `
  --resource-group $resourceGroup `
  --deployment-name $modelDeployment `
  --query "{deployment:name, model:properties.model.name, version:properties.model.version, state:properties.provisioningState}" `
  --output table
```

Open the project in New Microsoft Foundry and confirm the deployment status is **Succeeded**.

Do not create an Agent here. The Knowledge Agent is created in Part 1 and the Call Analysis Agent is created in Part 3.

## Validation

- [ ] Foundry account and project are accessible
- [ ] Project endpoint matches the `AZURE_AI_PROJECT_ENDPOINT` output
- [ ] `gpt-5` deployment is **Succeeded**
- [ ] No workshop Agent has been created yet
- [ ] Ready to proceed to Part 1
