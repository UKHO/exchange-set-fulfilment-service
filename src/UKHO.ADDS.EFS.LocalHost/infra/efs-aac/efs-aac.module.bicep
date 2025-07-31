@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource efs_aac 'Microsoft.AppConfiguration/configurationStores@2024-06-01' = {
  name: take('efsaac-${uniqueString(resourceGroup().id)}', 50)
  location: location
  properties: {
    disableLocalAuth: true
  }
  sku: {
    name: 'standard'
  }
  tags: {
    'aspire-resource-name': 'efs-aac'
    'hidden-title': 'EFS'
  }
}

output appConfigEndpoint string = efs_aac.properties.endpoint

output name string = efs_aac.name
