@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efs_cae_acr_outputs_name string

param efs_law_outputs_name string

param subnetResourceId string

param zoneRedundant bool

resource efs_cae_mi 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('efs_cae_mi-${uniqueString(resourceGroup().id)}', 128)
  location: location
  tags: {
    'hidden-title': 'EFS'
  }
}

resource efs_cae_acr 'Microsoft.ContainerRegistry/registries@2025-04-01' existing = {
  name: efs_cae_acr_outputs_name
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
