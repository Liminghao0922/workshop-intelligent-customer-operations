# Exercise 2 - Validate Azure AI Search

## Objective

Confirm that the one-time Shared Setup created Azure AI Search. The `customer-operations-knowledge` index and its documents are created in Part 1.

## 1. Load Provisioning Outputs

```powershell
$values = azd env get-values --output json | ConvertFrom-Json
$resourceGroup = $values.AZURE_RESOURCE_GROUP
$searchEndpoint = $values.AZURE_SEARCH_ENDPOINT
$searchService = ([uri]$searchEndpoint).Host.Split('.')[0]
```

## 2. Validate the Service

```powershell
az search service show `
  --name $searchService `
  --resource-group $resourceGroup `
  --query "{name:name, sku:sku.name, location:location, status:status}" `
  --output table
```

Expected result: the service exists in `japaneast` on the `basic` SKU.

Do not create the index or upload documents here. Those are the learning tasks in Part 1.

## Validation

- [ ] `AZURE_SEARCH_ENDPOINT` is present in the `azd` outputs
- [ ] Azure AI Search reports a healthy status
- [ ] No workshop knowledge index has been created yet
- [ ] Ready to build the knowledge foundation in Part 1
