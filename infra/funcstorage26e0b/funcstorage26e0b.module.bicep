@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource funcstorage26e0b 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: take('funcstorage26e0b${uniqueString(resourceGroup().id)}', 24)
  kind: 'StorageV2'
  location: location
  sku: {
    name: 'Standard_GRS'
  }
  properties: {
    accessTier: 'Hot'
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
  tags: {
    'aspire-resource-name': 'funcstorage26e0b'
  }
}

resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  name: 'default'
  parent: funcstorage26e0b
}

output blobEndpoint string = funcstorage26e0b.properties.primaryEndpoints.blob

output queueEndpoint string = funcstorage26e0b.properties.primaryEndpoints.queue

output tableEndpoint string = funcstorage26e0b.properties.primaryEndpoints.table

output name string = funcstorage26e0b.name