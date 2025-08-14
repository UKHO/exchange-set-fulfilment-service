@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param userPrincipalId string

param efs_law_outputs_name string

param subnetResourceId string

param zoneRedundant bool

resource efs_cae_mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('efs_cae_mi-${uniqueString(resourceGroup().id)}', 128)
  location: location
  tags: {
    'aspire-resource-name': 'efs-cae-mi'
    'hidden-title': 'EFS'
  }
}

resource efs_cae_acr 'Microsoft.ContainerRegistry/registries@2025-04-01' = {
  name: take('efscaeacr${uniqueString(resourceGroup().id)}', 50)
  location: location
  sku: {
    name: 'Basic'
  }
  tags: {
    'aspire-resource-name': 'efs-cae-acr'
    'hidden-title': 'EFS'
  }
}

resource efs_cae_acr_efs_cae_mi_AcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(efs_cae_acr.id, efs_cae_mi.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d'))
  properties: {
    principalId: efs_cae_mi.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
  }
  scope: efs_cae_acr
}

resource efs_law 'Microsoft.OperationalInsights/workspaces@2025-02-01' existing = {
  name: efs_law_outputs_name
}

resource efs_cae 'Microsoft.App/managedEnvironments@2025-01-01' = {
  name: take('efscae${uniqueString(resourceGroup().id)}', 24)
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: efs_law.properties.customerId
        sharedKey: efs_law.listKeys().primarySharedKey
      }
    }
    zoneRedundant: zoneRedundant
    vnetConfiguration: {
      internal: false
      infrastructureSubnetId: subnetResourceId
    }
    workloadProfiles: [
      {
        name: 'consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
  tags: {
    'aspire-resource-name': 'efs-cae'
    'hidden-title': 'EFS'
  }
}

resource aspireDashboard 'Microsoft.App/managedEnvironments/dotNetComponents@2024-10-02-preview' = {
  name: 'aspire-dashboard'
  properties: {
    componentType: 'AspireDashboard'
  }
  parent: efs_cae
}

output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = efs_law_outputs_name

output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = efs_law.id

output AZURE_CONTAINER_REGISTRY_NAME string = efs_cae_acr.name

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = efs_cae_acr.properties.loginServer

output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = efs_cae_mi.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = efs_cae.name

output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = efs_cae.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = efs_cae.properties.defaultDomain
