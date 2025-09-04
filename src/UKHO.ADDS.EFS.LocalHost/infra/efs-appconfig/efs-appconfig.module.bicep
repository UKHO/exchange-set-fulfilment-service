@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efsAppConfigurationName string

resource efs_appconfig 'Microsoft.AppConfiguration/configurationStores@2024-06-01' existing = {
  name: efsAppConfigurationName
}

output appConfigEndpoint string = efs_appconfig.properties.endpoint

output name string = efsAppConfigurationName