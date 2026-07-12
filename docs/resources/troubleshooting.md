# Troubleshooting

## Common Issues

| Issue | Possible Cause | Suggested Check |
|---|---|---|
| Agent does not answer from knowledge | Search connection or index not configured | Recheck the Azure AI Search connection and index data |
| Tool call not triggered | Tool schema or instruction issue | Validate tool description and required parameters |
| App cannot call backend | App setting or CORS issue | Check frontend environment variables |
| Backend cannot call agent | Endpoint or identity issue | Check Foundry project settings and credentials |

## Foundry Account Is Soft-Deleted

`azd provision --preview` can fail with `FlagMustBeSetForRestore` when a previous
environment used the same Foundry Account name. Cognitive Services retains a
deleted account for a recovery period, so ARM cannot create another account with
that name until the old account is restored or permanently purged.

Inspect the deleted account:

```powershell
az cognitiveservices account list-deleted `
	--query "[?name=='foundry-customer-operations-workshop']" `
	--output table
```

For a clean workshop reset, permanently purge the old account. This operation is
irreversible and deletes its projects, model deployments, and agents:

```powershell
az cognitiveservices account purge `
	--name foundry-customer-operations-workshop `
	--resource-group rg-customer-operations-workshop `
	--location japaneast

azd provision --preview
```

If the old Foundry content must be retained, restore the account instead of
purging it:

```powershell
$properties = '{"restore":true}'
az resource create `
	--resource-group rg-customer-operations-workshop `
	--name foundry-customer-operations-workshop `
	--location japaneast `
	--namespace Microsoft.CognitiveServices `
	--resource-type accounts `
	--properties $properties
```
