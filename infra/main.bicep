// Resource-group scoped: create the resource group first, then deploy this
// template into it (see deploy.sh). Provisions everything needed to run
// the API and client as two Azure Container Apps, with the API reading
// files from Azure Blob Storage via a user-assigned managed identity.

@description('Short, lowercase, alphanumeric name used to derive resource names (e.g. filebrowser)')
@minLength(3)
@maxLength(11)
@pattern('^[a-z0-9]+$')
param appName string = 'filebrowser'

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Full image reference for the API container (e.g. myacr.azurecr.io/filebrowser-api:latest). Left as a public placeholder until the real image is built and pushed.')
param apiImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('Full image reference for the web/client container. Left as a public placeholder until the real image is built and pushed.')
param webImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

var resourceToken = substring(uniqueString(resourceGroup().id), 0, 8)
var acrName = '${appName}acr${resourceToken}'
var storageAccountName = '${take(appName, 6)}st${resourceToken}'
var logAnalyticsName = '${appName}-logs-${resourceToken}'
var containerAppsEnvName = '${appName}-env-${resourceToken}'
var apiIdentityName = '${appName}-api-id-${resourceToken}'
var webIdentityName = '${appName}-web-id-${resourceToken}'
var apiAppName = '${appName}-api'
var webAppName = '${appName}-web'
var blobContainerName = 'files'

var acrPullRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
var storageBlobDataContributorRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: { name: 'Basic' }
  properties: {
    adminUserEnabled: false
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource filesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: blobContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource containerAppsEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppsEnvName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: apiIdentityName
  location: location
}

resource webIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: webIdentityName
  location: location
}

resource apiAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, apiIdentity.id, 'AcrPull')
  scope: containerRegistry
  properties: {
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleId
  }
}

resource webAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(containerRegistry.id, webIdentity.id, 'AcrPull')
  scope: containerRegistry
  properties: {
    principalId: webIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleId
  }
}

resource apiBlobAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, apiIdentity.id, 'StorageBlobDataContributor')
  scope: storageAccount
  properties: {
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: storageBlobDataContributorRoleId
  }
}

var apiFqdn = '${apiAppName}.${containerAppsEnv.properties.defaultDomain}'
var webFqdn = '${webAppName}.${containerAppsEnv.properties.defaultDomain}'

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${apiIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: apiIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: apiImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'FileBrowser__Provider', value: 'Azure' }
            { name: 'FileBrowser__AzureBlob__AccountUrl', value: storageAccount.properties.primaryEndpoints.blob }
            { name: 'FileBrowser__AzureBlob__ContainerName', value: blobContainerName }
            { name: 'Cors__AllowedOrigins__0', value: 'https://${webFqdn}' }
            { name: 'AZURE_CLIENT_ID', value: apiIdentity.properties.clientId }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
  dependsOn: [
    apiAcrPull
    apiBlobAccess
  ]
}

resource webApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${webIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnv.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: webIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'web'
          image: webImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            { name: 'API_BASE_URL', value: 'https://${apiFqdn}' }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
  dependsOn: [
    webAcrPull
  ]
}

output containerRegistryName string = containerRegistry.name
output containerRegistryLoginServer string = containerRegistry.properties.loginServer
output apiAppName string = apiApp.name
output webAppName string = webApp.name
output apiUrl string = 'https://${apiFqdn}'
output webUrl string = 'https://${webFqdn}'
