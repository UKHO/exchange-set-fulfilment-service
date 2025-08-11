@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param applicationType string = 'web'

param kind string = 'web'

resource law_efs_app_insights 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: take('lawefsappinsights-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: {
    'aspire-resource-name': 'law_efs_app_insights'
    'hidden-title': 'EFS'
  }
}

resource efs_app_insights 'Microsoft.Insights/components@2020-02-02' = {
  name: take('efs_app_insights-${uniqueString(resourceGroup().id)}', 260)
  kind: kind
  location: location
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: law_efs_app_insights.id
  }
  tags: {
    'aspire-resource-name': 'efs-app-insights'
    'hidden-title': 'EFS'
  }
}

output appInsightsConnectionString string = efs_app_insights.properties.ConnectionString

output name string = efs_app_insights.name