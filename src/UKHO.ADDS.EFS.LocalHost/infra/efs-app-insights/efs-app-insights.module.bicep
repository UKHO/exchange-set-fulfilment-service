@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efsApplicationInsightsName string

resource efs_app_insights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: efsApplicationInsightsName
}

output appInsightsConnectionString string = efs_app_insights.properties.ConnectionString

output name string = efsApplicationInsightsName