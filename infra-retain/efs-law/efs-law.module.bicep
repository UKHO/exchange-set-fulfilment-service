@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

@minLength(1)
@description('The partial name (from the start) of the log analytics workspace resource.')
param efsLogAnalyticsWorkspacePartialName string

resource efs_law 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: take('${efsLogAnalyticsWorkspacePartialName}-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: {
    'hidden-title': 'EFS'
  }
}

output logAnalyticsWorkspaceId string = efs_law.id

output name string = efs_law.name
