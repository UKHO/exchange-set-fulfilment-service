@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efsLogAnalyticsWorkspaceName string

resource efs_law 'Microsoft.OperationalInsights/workspaces@2025-02-01' existing = {
  name: efsLogAnalyticsWorkspaceName
}

output logAnalyticsWorkspaceId string = efs_law.id

output name string = efsLogAnalyticsWorkspaceName