# Troubleshooting

## Common Issues

| Issue | Possible Cause | Suggested Check |
| --- | --- | --- |
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

## Cosmos DB SQL Role Assignment Rejects the Scope

Provisioning can fail with a payload parsing error similar to:

```text
Could not parse property [scope] with value [/] as FullyQualifiedResourcePath
```

For `Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments`, the `scope`
property must be a fully qualified Cosmos DB resource path. To grant access at
the account level, use the symbolic account ID rather than `/`:

```bicep
resource cosmosDataContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  parent: cosmos
  properties: {
    principalId: workload.identity.principalId
    roleDefinitionId: '${cosmos.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    scope: cosmos.id
  }
}
```

Compile and preview before retrying the deployment:

```powershell
az bicep build --file ./infra/main.bicep --outfile ./infra/main.json
azd provision --preview
azd provision
```
