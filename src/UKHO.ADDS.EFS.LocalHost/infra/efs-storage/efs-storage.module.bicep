@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource efs_storage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: take('efsstorage${uniqueString(resourceGroup().id)}', 24)
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
    'aspire-resource-name': 'efs-storage'
    'hidden-title': 'EFS'
  }
}

output blobEndpoint string = efs_storage.properties.primaryEndpoints.blob

output queueEndpoint string = efs_storage.properties.primaryEndpoints.queue

output tableEndpoint string = efs_storage.properties.primaryEndpoints.table

output name string = efs_storage.name