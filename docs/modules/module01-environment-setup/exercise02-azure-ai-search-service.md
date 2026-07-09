# Exercise 2 - Prepare Azure AI Search Service

## Objective

Create and configure an Azure AI Search service from Azure Cloud Shell. This service will serve as the knowledge foundation for the customer operations agent.

## Tasks

### 1. Confirm Cloud Shell Variables

Continue in Azure Cloud Shell (PowerShell), from the repository root cloned in Exercise 1.

Reuse the same naming suffix and resource group from Exercise 1:

```powershell
$postfix = "workshop"   # use the same value from Exercise 1
$resourceGroup = "rg-customer-operations-$postfix"
$location = "japaneast"
$searchService = "srch-customer-ops-$postfix"
$searchIndexName = "customer-operations-knowledge"
```

### 2. Create Azure AI Search Service

Create the Azure AI Search service:

```powershell
az search service create `
  --name $searchService `
  --resource-group $resourceGroup `
  --sku Basic `
  --location $location
```

The `Basic` tier is sufficient for this workshop.

### 3. Get Endpoint and API Key

Set the Azure AI Search endpoint and retrieve the primary admin key:

```powershell
$searchEndpoint = "https://$searchService.search.windows.net"
$searchKey = az search admin-key show `
  --resource-group $resourceGroup `
  --service-name $searchService `
  --query primaryKey `
  --output tsv
```

### 4. Create a Search Index

Create an index named `customer-operations-knowledge`:

```powershell
$indexDefinition = @"
{
  "name": "$searchIndexName",
  "fields": [
    {
      "name": "id",
      "type": "Edm.String",
      "key": true,
      "retrievable": true
    },
    {
      "name": "title",
      "type": "Edm.String",
      "searchable": true,
      "retrievable": true
    },
    {
      "name": "content",
      "type": "Edm.String",
      "searchable": true,
      "retrievable": true
    },
    {
      "name": "language",
      "type": "Edm.String",
      "filterable": true,
      "retrievable": true
    },
    {
      "name": "category",
      "type": "Edm.String",
      "filterable": true,
      "retrievable": true
    },
    {
      "name": "sourceUrl",
      "type": "Edm.String",
      "retrievable": true
    }
  ]
}
"@

$headers = @{
  "api-key" = $searchKey
}

Invoke-RestMethod `
  -Method Put `
  -Uri "$searchEndpoint/indexes/${searchIndexName}?api-version=2024-07-01" `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $indexDefinition
```

### 5. Import Knowledge Data

Import the sample knowledge files from `data/knowledge-base` into the search index:

```powershell
$documents = @{
  value = @(
    @{
      "@search.action" = "mergeOrUpload"
      id = "support-policy"
      title = "Support Policy"
      content = Get-Content .\data\knowledge-base\support-policy.md -Raw
      language = "en"
      category = "policy"
      sourceUrl = "data/knowledge-base/support-policy.md"
    },
    @{
      "@search.action" = "mergeOrUpload"
      id = "product-faq"
      title = "Product FAQ"
      content = Get-Content .\data\knowledge-base\product-faq.md -Raw
      language = "en"
      category = "faq"
      sourceUrl = "data/knowledge-base/product-faq.md"
    }
  )
} | ConvertTo-Json -Depth 10

Invoke-RestMethod `
  -Method Post `
  -Uri "$searchEndpoint/indexes/${searchIndexName}/docs/index?api-version=2024-07-01" `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $documents
```

### 6. Record Values

Update `.env` in the repository root:

```powershell
$envContent = Get-Content .env
$envContent = $envContent `
  -replace '^AZURE_SEARCH_ENDPOINT=.*', "AZURE_SEARCH_ENDPOINT=$searchEndpoint" `
  -replace '^AZURE_SEARCH_INDEX=.*', "AZURE_SEARCH_INDEX=$searchIndexName" `
  -replace '^AZURE_SEARCH_API_KEY=.*', "AZURE_SEARCH_API_KEY=$searchKey"

if ($envContent -match '^AZURE_SEARCH_INDEX_NAME=') {
  $envContent = $envContent -replace '^AZURE_SEARCH_INDEX_NAME=.*', "AZURE_SEARCH_INDEX_NAME=$searchIndexName"
} else {
  $envContent += "AZURE_SEARCH_INDEX_NAME=$searchIndexName"
}

$envContent | Set-Content .env
```

Confirm the values were saved:

```powershell
Get-Content .env | Select-String "AZURE_SEARCH_"
```

## Validation

Confirm the Azure AI Search service exists:

```powershell
az search service show `
  --name $searchService `
  --resource-group $resourceGroup `
  --query "{name:name, sku:sku.name, location:location}" `
  --output table
```

Confirm the index exists:

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri "$searchEndpoint/indexes/${searchIndexName}?api-version=2024-07-01" `
  -Headers $headers |
  Select-Object name
```

Confirm data was imported:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "$searchEndpoint/indexes/${searchIndexName}/docs/search?api-version=2024-07-01" `
  -Headers $headers `
  -ContentType "application/json" `
  -Body '{ "search": "*", "select": "id,title,category", "top": 5 }'
```

- [ ] Azure AI Search service created from Azure Cloud Shell
- [ ] Endpoint URL and API key recorded in `.env`
- [ ] Index `customer-operations-knowledge` created
- [ ] Sample knowledge data imported into the index
- [ ] `$postfix` reused for search service naming
