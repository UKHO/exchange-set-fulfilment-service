@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efsStorageAccountName string

resource efs_storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
  name: efsStorageAccountName
}

output blobEndpoint string = efs_storage.properties.primaryEndpoints.blob

output queueEndpoint string = efs_storage.properties.primaryEndpoints.queue

output tableEndpoint string = efs_storage.properties.primaryEndpoints.table

output name string = efsStorageAccountName