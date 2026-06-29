targetScope = 'subscription'

@minLength(1)
param environmentName string
param location string = 'japaneast'
param voiceLiveModelDeploymentName string = ''
param voiceLiveModelName string = ''
param voiceLiveModelVersion string = ''
param voiceLiveModelCapacity int = 50
param acsConnectionString string = ''
param acsCallbackSecret string = ''
param publicBaseUrl string = ''
param foundryAgentId string = ''
param foundryAnalyticsAgentId string = ''

var tags = {
  'azd-env-name': environmentName
  workload: 'smart-call-center'
}

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module app './modules/app.bicep' = {
  name: 'smart-call-center'
  scope: rg
  params: {
    environmentName: environmentName
    location: location
    tags: tags
    voiceLiveModelDeploymentName: voiceLiveModelDeploymentName
    voiceLiveModelName: voiceLiveModelName
    voiceLiveModelVersion: voiceLiveModelVersion
    voiceLiveModelCapacity: voiceLiveModelCapacity
    acsConnectionString: acsConnectionString
    acsCallbackSecret: acsCallbackSecret
    publicBaseUrl: publicBaseUrl
    foundryAgentId: foundryAgentId
    foundryAnalyticsAgentId: foundryAnalyticsAgentId
  }
}

output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_LOCATION string = location
output WEB_URL string = app.outputs.webUrl
output API_URL string = app.outputs.webUrl
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = app.outputs.acrLoginServer
output AZURE_STORAGE_ACCOUNT_NAME string = app.outputs.storageAccountName
output AZURE_SEARCH_ENDPOINT string = app.outputs.searchEndpoint
output AZURE_SEARCH_INDEX_NAME string = 'call-center-knowledge'
output AZURE_KEY_VAULT_NAME string = app.outputs.keyVaultName
output POST_CALL_QUEUE_NAME string = app.outputs.postCallAnalyticsEndpoint
output AZURE_AI_PROJECT_ENDPOINT string = app.outputs.foundryProjectEndpoint
output VOICE_LIVE_ENDPOINT string = app.outputs.voiceLiveEndpoint
output VOICE_LIVE_MODEL string = app.outputs.voiceLiveModelDeploymentName
output FOUNDRY_ACCOUNT_NAME string = app.outputs.foundryAccountName
output FOUNDRY_PROJECT_NAME string = app.outputs.foundryProjectName
output COSMOS_ACCOUNT_NAME string = app.outputs.cosmosAccountName
output COSMOS_ENDPOINT string = app.outputs.cosmosEndpoint
output COSMOS_DATABASE_NAME string = app.outputs.cosmosDatabaseName
output COSMOS_CONTAINER_NAME string = app.outputs.cosmosContainerName
