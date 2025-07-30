@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource adds_con_was 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: take('addsconwas${uniqueString(resourceGroup().id)}', 24)
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
    'aspire-resource-name': 'adds-con-was'
    'hidden-title': 'EFS'
  }
}

resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  name: 'default'
  parent: adds_con_was
}

output blobEndpoint string = adds_con_was.properties.primaryEndpoints.blob

output queueEndpoint string = adds_con_was.properties.primaryEndpoints.queue

output tableEndpoint string = adds_con_was.properties.primaryEndpoints.table

output name string = adds_con_was.name