# Exercise 1 - Prepare Enterprise Knowledge

## Objective

Confirm that the workshop knowledge is indexed and retrievable before attaching it to an agent.

## 1. Review the Source Content

The workshop uses these files:

- `data/knowledge-base/product-faq.md`
- `data/knowledge-base/support-policy.md`

Review the content and identify at least one answer that is present and one question the documents do not answer. This distinction is used to test grounding later.

## 2. Restore the Search Variables

Open Azure Cloud Shell in **PowerShell** mode from the repository root:

```powershell
$postfix = "workshop"   # use the same value from shared setup
$resourceGroup = "rg-customer-operations-$postfix"
$searchService = "srch-customer-ops-$postfix"
$searchIndexName = "customer-operations-knowledge"
$searchEndpoint = "https://$searchService.search.windows.net"
$searchKey = az search admin-key show `
  --service-name $searchService `
  --resource-group $resourceGroup `
  --query primaryKey `
  --output tsv
```

Do not print or commit `$searchKey`.

## 3. Confirm the Index

```powershell
$headers = @{ "api-key" = $searchKey }

Invoke-RestMethod `
  -Method Get `
  -Uri "$searchEndpoint/indexes/${searchIndexName}?api-version=2024-07-01" `
  -Headers $headers
```

If the index is missing, return to Shared Setup, Exercise 2 and complete the index creation and document upload steps.

## 4. Test Retrieval

```powershell
$query = @{
  search = "product warranty support policy"
  top = 3
  select = "id,title,content,source"
} | ConvertTo-Json

$results = Invoke-RestMethod `
  -Method Post `
  -Uri "$searchEndpoint/indexes/${searchIndexName}/docs/search?api-version=2024-07-01" `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $query

$results.value | Select-Object title, source, '@search.score'
```

Repeat the query with one Japanese or Chinese support question. Confirm the most relevant source appears near the top.

## Validation

- [ ] Both workshop knowledge files were reviewed
- [ ] Search index exists
- [ ] Search query returns relevant content
- [ ] At least one multilingual query was tested
- [ ] Search key was not written to source control
