@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource efs_cae_acr 'Microsoft.ContainerRegistry/registries@2025-04-01' = {
  name: take('efscaeacr${uniqueString(resourceGroup().id)}', 50)
  location: location
  sku: {
    name: 'Basic'
  }
  tags: {
    'hidden-title': 'EFS'
  }
}
