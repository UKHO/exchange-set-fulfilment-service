@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param applicationType string = 'web'

param kind string = 'web'

param efs_law_outputs_loganalyticsworkspaceid string

@minLength(1)
@description('The partial name (from the start) of the application insights resource.')
param efsApplicationInsightsPartialName string

resource efs_app_insights 'Microsoft.Insights/components@2020-02-02' = {
  name: take('${efsApplicationInsightsPartialName}-${uniqueString(resourceGroup().id)}', 260)
  kind: kind
  location: location
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: efs_law_outputs_loganalyticsworkspaceid
  }
  tags: {
    'hidden-title': 'EFS'
  }
}

output appInsightsConnectionString string = efs_app_insights.properties.ConnectionString

output name string = efs_app_insights.name
