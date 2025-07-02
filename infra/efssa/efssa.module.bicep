@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource efssa 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: take('efssa${uniqueString(resourceGroup().id)}', 24)
  kind: 'StorageV2'
  location: location
  sku: {
    name: 'Standard_GRS'
  }
  properties: {
    accessTier: 'Hot'
    allowSharedKeyAccess: true
    minimumTlsVersion: 'TLS1_2'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
  tags: {
    'aspire-resource-name': 'efssa'
    'hidden-title': 'EFS'
  }
}

resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  name: 'default'
  parent: efssa
}

output blobEndpoint string = efssa.properties.primaryEndpoints.blob

output queueEndpoint string = efssa.properties.primaryEndpoints.queue

output tableEndpoint string = efssa.properties.primaryEndpoints.table

output name string = efssa.name