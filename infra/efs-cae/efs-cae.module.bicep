@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param userPrincipalId string

param tags object = { }

param subnetSubscription string

param subnetResourceGroup string

param subnetVnet string

param subnetName string

resource efs_cae_mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: take('efs_cae_mi-${uniqueString(resourceGroup().id)}', 128)
  location: location
  tags: tags
}

resource efs_cae_acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: take('efscaeacr${uniqueString(resourceGroup().id)}', 50)
  location: location
  sku: {
    name: 'Basic'
  }
  tags: tags
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

resource efs_cae_law 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: take('efscaelaw-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: tags
}

resource efs_cae 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: take('efscae${uniqueString(resourceGroup().id)}', 24)
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: efs_cae_law.properties.customerId
        sharedKey: efs_cae_law.listKeys().primarySharedKey
      }
    }
    vnetConfiguration: {
      internal: true
      infrastructureSubnetId: '/subscriptions/${subnetSubscription}/resourceGroups/${subnetResourceGroup}/providers/Microsoft.Network/virtualNetworks/${subnetVnet}/subnets/${subnetName}'
    }
    workloadProfiles: [
      {
        name: 'consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
  tags: tags
}

resource aspireDashboard 'Microsoft.App/managedEnvironments/dotNetComponents@2024-10-02-preview' = {
  name: 'aspire-dashboard'
  properties: {
    componentType: 'AspireDashboard'
  }
  parent: efs_cae
}

resource efs_cae_Contributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(efs_cae.id, userPrincipalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c'))
  properties: {
    principalId: userPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
  }
  scope: efs_cae
}

output MANAGED_IDENTITY_NAME string = efs_cae_mi.name

output MANAGED_IDENTITY_PRINCIPAL_ID string = efs_cae_mi.properties.principalId

output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = efs_cae_law.name

output AZURE_LOG_ANALYTICS_WORKSPACE_ID string = efs_cae_law.id

output AZURE_CONTAINER_REGISTRY_NAME string = efs_cae_acr.name

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = efs_cae_acr.properties.loginServer

output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = efs_cae_mi.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = efs_cae.name

output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = efs_cae.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = efs_cae.properties.defaultDomain