@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efs_law_outputs_name string

param subnetResourceId string

param zoneRedundant bool

@minLength(1)
@description('The partial name (from the start) of the container apps environment resource.')
param efsContainerAppsEnvironmentPartialName string

resource efs_law 'Microsoft.OperationalInsights/workspaces@2025-02-01' existing = {
  name: efs_law_outputs_name
}

resource efs_cae 'Microsoft.App/managedEnvironments@2025-01-01' = {
  name: take('${efsContainerAppsEnvironmentPartialName}${uniqueString(resourceGroup().id)}', 24)
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

resource aspireDashboard 'Microsoft.App/managedEnvironments/dotNetComponents@2025-02-02-preview' = {
  name: 'aspire-dashboard'
  properties: {
    componentType: 'AspireDashboard'
  }
  parent: efs_cae
}

output name string = efs_cae.name
output defaultDomain string = efs_cae.properties.defaultDomain
