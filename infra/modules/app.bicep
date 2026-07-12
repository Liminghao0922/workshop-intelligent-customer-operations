targetScope = 'resourceGroup'

param environmentName string
param location string = resourceGroup().location
param tags object = {}
param searchServiceName string
param foundryAccountName string
param foundryProjectName string
param foundryModelDeploymentName string = 'gpt-5'
param foundryModelVersion string = '2025-08-07'
param foundryModelCapacity int = 10
param voiceLiveModelDeploymentName string = ''
param voiceLiveModelName string = ''
param voiceLiveModelVersion string = ''
param voiceLiveModelCapacity int = 50
param acsConnectionString string = ''
@secure()
param acsCallbackSecret string = ''
param publicBaseUrl string = ''
param foundryAgentId string = ''
param foundryAnalyticsAgentId string = ''

var suffix = take(uniqueString(subscription().id, resourceGroup().name, environmentName), 6)
var clean = replace(toLower(environmentName), '-', '')
var name = take(clean, 12)
var storageName = take('st${name}${suffix}', 24)
var eventHubsNamespaceName = take('evhns-${name}-${suffix}', 50)
var postCallEventHubName = 'call-ended'
var postCallConsumerGroupName = 'post-call-worker'
var acsName = take('acs-${name}-${suffix}', 63)
var kvName = take('kv-${name}-${suffix}', 24)
var acrName = take('acr${name}${suffix}', 50)
var logName = 'log-${environmentName}-${suffix}'
var appiName = 'appi-${environmentName}-${suffix}'
var envName = 'cae-${environmentName}-${suffix}'
var webName = take('ca-web-${name}-${suffix}', 32)
var workerName = take('ca-worker-${name}-${suffix}', 32)
var apiName = take('ca-api-${name}-${suffix}', 32)
var portalName = take('ca-portal-${name}-${suffix}', 32)
var cosmosName = take('cos${name}${suffix}', 44)
var cosmosDatabaseName = 'smart-call-center'
var cosmosContainerName = 'call-sessions'
var foundryProjectEndpoint = '${foundryAccount.properties.endpoints['AI Foundry API']}api/projects/${foundryProject.name}'

resource log 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appi 'Microsoft.Insights/components@2020-02-02' = {
  name: appiName
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: log.id
  }
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  tags: tags
  sku: {
    name: 'Standard_ZRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}

resource blob 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  name: 'default'
  parent: storage
}

resource artifacts 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: 'call-artifacts'
  parent: blob
  properties: {
    publicAccess: 'None'
  }
}

resource eventHubsNamespace 'Microsoft.EventHub/namespaces@2024-01-01' = {
  name: eventHubsNamespaceName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
    minimumTlsVersion: '1.2'
  }
}

resource postCallEventHub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  parent: eventHubsNamespace
  name: postCallEventHubName
  properties: {
    partitionCount: 2
    messageRetentionInDays: 1
  }
}

resource postCallConsumerGroup 'Microsoft.EventHub/namespaces/eventhubs/consumergroups@2024-01-01' = {
  parent: postCallEventHub
  name: postCallConsumerGroupName
}

resource deploymentPackages 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: 'deploymentpackage'
  parent: blob
  properties: {
    publicAccess: 'None'
  }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmosName
  location: location
  kind: 'GlobalDocumentDB'
  tags: tags
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    publicNetworkAccess: 'Enabled'
    enableAutomaticFailover: false
    disableLocalAuth: true
    disableKeyBasedMetadataWriteAccess: false
  }
}

resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  parent: cosmos
  name: cosmosDatabaseName
  properties: {
    resource: {
      id: cosmosDatabaseName
    }
  }
}

resource cosmosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: cosmosDatabase
  name: cosmosContainerName
  properties: {
    resource: {
      id: cosmosContainerName
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
      }
    }
    options: {}
  }
}

resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: kvName
  location: location
  tags: tags
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
  }
}

resource search 'Microsoft.Search/searchServices@2024-06-01-preview' = {
  name: searchServiceName
  location: location
  tags: tags
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
    disableLocalAuth: false
  }
}

resource foundryAccount 'Microsoft.CognitiveServices/accounts@2025-10-01-preview' = {
  name: foundryAccountName
  location: location
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  tags: tags
  properties: {
    allowProjectManagement: true
    customSubDomainName: foundryAccountName
    publicNetworkAccess: 'Enabled'
  }
}

resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2025-10-01-preview' = {
  parent: foundryAccount
  name: foundryProjectName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  tags: tags
  properties: {
    displayName: foundryProjectName
    description: 'Intelligent customer operations workshop'
  }
}

resource foundryModelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: foundryAccount
  name: foundryModelDeploymentName
  sku: {
    name: 'GlobalStandard'
    capacity: foundryModelCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-5'
      version: foundryModelVersion
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
  }
}

// Deploy model only when name/version are provided.
resource voiceLiveModelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = if (!empty(voiceLiveModelDeploymentName) && !empty(voiceLiveModelName) && !empty(voiceLiveModelVersion)) {
  parent: foundryAccount
  name: voiceLiveModelDeploymentName
  sku: {
    name: 'GlobalStandard'
    capacity: voiceLiveModelCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: voiceLiveModelName
      version: voiceLiveModelVersion
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
    raiPolicyName: 'Microsoft.DefaultV2'
  }
}

resource acs 'Microsoft.Communication/communicationServices@2023-04-01-preview' = {
  name: acsName
  location: 'global'
  tags: tags
  properties: {
    dataLocation: 'Japan'
  }
}

resource env 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: envName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: log.properties.customerId
        sharedKey: log.listKeys().primarySharedKey
      }
    }
  }
}

resource web 'Microsoft.App/containerApps@2024-03-01' = {
  name: webName
  location: location
  tags: union(tags, { 'azd-service-name': 'gateway' })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
      }
    }
    template: {
      containers: [
        {
          name: 'gateway'
          image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          env: [
            { name: 'APP_MODE', value: 'azure' }
            { name: 'AZURE_LOCATION', value: location }
            { name: 'AZURE_AI_PROJECT_ENDPOINT', value: foundryProjectEndpoint }
            { name: 'FOUNDRY_AGENT_ID', value: foundryAgentId }
            { name: 'FOUNDRY_ANALYTICS_AGENT_ID', value: foundryAnalyticsAgentId }
            { name: 'VOICE_LIVE_ENDPOINT', value: foundryAccount.properties.endpoint }
            { name: 'VOICE_LIVE_MODEL', value: voiceLiveModelDeploymentName }
            { name: 'AZURE_SEARCH_ENDPOINT', value: 'https://${search.name}.search.windows.net' }
            { name: 'AZURE_SEARCH_INDEX_NAME', value: 'customer-operations-knowledge' }
            { name: 'AZURE_STORAGE_ACCOUNT_NAME', value: storage.name }
            { name: 'CALL_ARTIFACT_CONTAINER', value: artifacts.name }
            { name: 'POST_CALL_EVENT_HUB_FULLY_QUALIFIED_NAMESPACE', value: '${eventHubsNamespace.name}.servicebus.windows.net' }
            { name: 'POST_CALL_EVENT_HUB_NAME', value: postCallEventHub.name }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appi.properties.ConnectionString }
            { name: 'ACS_CONNECTION_STRING', value: acsConnectionString }
            { name: 'ACS_CALLBACK_SECRET', value: acsCallbackSecret }
            { name: 'PUBLIC_BASE_URL', value: empty(publicBaseUrl) ? 'https://placeholder.example.com' : publicBaseUrl }
            { name: 'COSMOS_ENDPOINT', value: cosmos.properties.documentEndpoint }
            { name: 'COSMOS_DATABASE_NAME', value: cosmosDatabaseName }
            { name: 'COSMOS_CONTAINER_NAME', value: cosmosContainerName }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

resource worker 'Microsoft.App/containerApps@2024-03-01' = {
  name: workerName
  location: location
  tags: union(tags, { 'azd-service-name': 'postcall-worker' })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 80
      }
    }
    template: {
      containers: [
        {
          name: 'postcall-worker'
          image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          env: [
            { name: 'APP_MODE', value: 'azure' }
            { name: 'AzureWebJobsStorage__accountName', value: storage.name }
            { name: 'AzureWebJobsStorage__credential', value: 'managedidentity' }
            { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
            { name: 'AZURE_AI_PROJECT_ENDPOINT', value: foundryProjectEndpoint }
            { name: 'FOUNDRY_AGENT_ID', value: '' }
            { name: 'FOUNDRY_ANALYTICS_AGENT_ID', value: '' }
            { name: 'PostCallEventHub__fullyQualifiedNamespace', value: '${eventHubsNamespace.name}.servicebus.windows.net' }
            { name: 'POST_CALL_EVENT_HUB_NAME', value: postCallEventHub.name }
            { name: 'POST_CALL_EVENT_HUB_CONSUMER_GROUP', value: postCallConsumerGroup.name }
            { name: 'AZURE_STORAGE_ACCOUNT_NAME', value: storage.name }
            { name: 'COSMOS_ENDPOINT', value: cosmos.properties.documentEndpoint }
            { name: 'COSMOS_DATABASE_NAME', value: cosmosDatabaseName }
            { name: 'COSMOS_CONTAINER_NAME', value: cosmosContainerName }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appi.properties.ConnectionString }
          ]
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiName
  location: location
  tags: union(tags, { 'azd-service-name': 'api' })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 8080
      }
    }
    template: {
      containers: [
        {
          name: 'api'
          image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          env: [
            { name: 'GATEWAY_API_BASE_URL', value: 'https://${web.properties.configuration.ingress.fqdn}' }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appi.properties.ConnectionString }
          ]
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

resource portal 'Microsoft.App/containerApps@2024-03-01' = {
  name: portalName
  location: location
  tags: union(tags, { 'azd-service-name': 'portal' })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
      }
    }
    template: {
      containers: [
        {
          name: 'portal'
          image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          env: [
            { name: 'BACKEND_API_BASE_URL', value: 'https://${api.properties.configuration.ingress.fqdn}' }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appi.properties.ConnectionString }
          ]
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

resource webAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, web.id, 'acrpull')
  scope: acr
  properties: {
    principalId: web.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
}

resource webStorageContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, web.id, 'blob-contributor')
  scope: storage
  properties: {
    principalId: web.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalType: 'ServicePrincipal'
  }
}

resource workerAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, worker.id, 'acrpull')
  scope: acr
  properties: {
    principalId: worker.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
}

resource apiAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, api.id, 'acrpull')
  scope: acr
  properties: {
    principalId: api.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
}

resource portalAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, portal.id, 'acrpull')
  scope: acr
  properties: {
    principalId: portal.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
}

resource workerStorageContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, worker.id, 'blob-contributor')
  scope: storage
  properties: {
    principalId: worker.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalType: 'ServicePrincipal'
  }
}

resource webEventHubsSender 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(postCallEventHub.id, web.id, 'event-hubs-data-sender')
  scope: postCallEventHub
  properties: {
    principalId: web.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '2b629674-e913-4c01-ae53-ef4638d8f975')
    principalType: 'ServicePrincipal'
  }
}

resource workerEventHubsReceiver 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(postCallEventHub.id, worker.id, 'event-hubs-data-receiver')
  scope: postCallEventHub
  properties: {
    principalId: worker.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a638d3c7-ab3a-418d-83e6-5f17a39d4fde')
    principalType: 'ServicePrincipal'
  }
}

resource webSearchContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(search.id, web.id, 'search-index-contributor')
  scope: search
  properties: {
    principalId: web.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
    principalType: 'ServicePrincipal'
  }
}

resource webFoundryUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundryProject.id, web.id, 'foundry-user')
  scope: foundryProject
  properties: {
    principalId: web.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '53ca6127-db72-4b80-b1b0-d745d6d5456d')
    principalType: 'ServicePrincipal'
  }
}

resource workerFoundryUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundryProject.id, worker.id, 'foundry-user')
  scope: foundryProject
  properties: {
    principalId: worker.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '53ca6127-db72-4b80-b1b0-d745d6d5456d')
    principalType: 'ServicePrincipal'
  }
}

resource webCognitiveServicesUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundryAccount.id, web.id, 'cognitiveservices-user')
  scope: foundryAccount
  properties: {
    principalId: web.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
    principalType: 'ServicePrincipal'
  }
}

resource workerCognitiveServicesUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundryAccount.id, worker.id, 'cognitiveservices-user')
  scope: foundryAccount
  properties: {
    principalId: worker.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
    principalType: 'ServicePrincipal'
  }
}

resource webCosmosDataContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(cosmos.id, web.id, 'cosmos-data-contributor')
  parent: cosmos
  properties: {
    principalId: web.identity.principalId
    roleDefinitionId: '${cosmos.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    scope: cosmos.id
  }
}

resource workerCosmosDataContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(cosmos.id, worker.id, 'cosmos-data-contributor')
  parent: cosmos
  properties: {
    principalId: worker.identity.principalId
    roleDefinitionId: '${cosmos.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    scope: cosmos.id
  }
}

output gatewayUrl string = 'https://${web.properties.configuration.ingress.fqdn}'
output apiUrl string = 'https://${api.properties.configuration.ingress.fqdn}'
output portalUrl string = 'https://${portal.properties.configuration.ingress.fqdn}'
output acrLoginServer string = acr.properties.loginServer
output storageAccountName string = storage.name
output searchEndpoint string = 'https://${search.name}.search.windows.net'
output keyVaultName string = kv.name
output eventHubsNamespace string = '${eventHubsNamespace.name}.servicebus.windows.net'
output postCallEventHubName string = postCallEventHub.name
output postCallConsumerGroupName string = postCallConsumerGroup.name
output foundryProjectEndpoint string = foundryProjectEndpoint
output voiceLiveEndpoint string = foundryAccount.properties.endpoint
output foundryAccountName string = foundryAccount.name
output foundryProjectName string = foundryProject.name
output foundryModelDeploymentName string = foundryModelDeployment.name
output voiceLiveModelDeploymentName string = voiceLiveModelDeploymentName
output cosmosAccountName string = cosmos.name
output cosmosEndpoint string = cosmos.properties.documentEndpoint
output cosmosDatabaseName string = cosmosDatabaseName
output cosmosContainerName string = cosmosContainerName
