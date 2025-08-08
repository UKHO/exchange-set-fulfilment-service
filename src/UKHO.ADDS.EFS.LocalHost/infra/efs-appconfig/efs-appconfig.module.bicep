@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource efs_appconfig 'Microsoft.AppConfiguration/configurationStores@2024-06-01' = {
  name: take('efsappconfig-${uniqueString(resourceGroup().id)}', 50)
  location: location
  properties: {
    disableLocalAuth: true
  }
  sku: {
    name: 'standard'
  }
  tags: {
    'aspire-resource-name': 'efs-appconfig'
    'hidden-title': 'EFS'
  }
}

output appConfigEndpoint string = efs_appconfig.properties.endpoint

output name string = efs_appconfig.name