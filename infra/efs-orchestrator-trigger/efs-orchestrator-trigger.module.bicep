@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

@description('The name of the Function App.')
param functionAppName string

@description('The name of the hosting plan for the Function App.')
param hostingPlanName string

@description('The SKU for the hosting plan.')
param hostingPlanSku string = 'P1v2' // Consumption plan

@description('The name of the storage account for the Function App.')
param storageAccountName string

@description('The user-assigned managed identity resource id for the Function App.')
param userAssignedIdentityId string

resource storageAccount 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
  name: storageAccountName
}

resource hostingPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: hostingPlanSku
    tier: 'PremiumV2'
  }
  kind: 'functionapp'
}

resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageAccount.properties.primaryEndpoints.blob
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
    httpsOnly: true
  }
}

output functionAppName string = functionApp.name
output functionAppEndpoint string = 'https://${functionApp.properties.defaultHostName}/'
output principalId string = functionApp.identity.principalId
output hostingPlanName string = hostingPlan.name
