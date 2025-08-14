@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param applicationType string = 'web'

param kind string = 'web'

param efs_law_outputs_loganalyticsworkspaceid string

resource efs_app_insights 'Microsoft.Insights/components@2020-02-02' = {
  name: take('efs_app_insights-${uniqueString(resourceGroup().id)}', 260)
  kind: kind
  location: location
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: efs_law_outputs_loganalyticsworkspaceid
  }
  tags: {
    'aspire-resource-name': 'efs-app-insights'
    'hidden-title': 'EFS'
  }
}

output appInsightsConnectionString string = efs_app_insights.properties.ConnectionString

output name string = efs_app_insights.name