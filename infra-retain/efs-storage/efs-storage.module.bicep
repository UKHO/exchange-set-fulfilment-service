@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

@minLength(3)
@maxLength(24)
@description('The name of the storage account resource.')
param efsStorageAccountName string

@description('Optional list of allowed IPv4 addresses or CIDR ranges.')
param ipRules array = []

@minLength(1)
@description('Id of the container app subnet')
param subnetResourceId string

@minLength(1)
@description('Agent subnet')
param azureAgent2204SubnetId string

@minLength(1)
@description('Agent subnet')
param azureAgentPrdSubnetId string

resource efs_storage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: efsStorageAccountName
  kind: 'StorageV2'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    networkAcls: {
      bypass: 'Logging, Metrics, AzureServices'
      virtualNetworkRules: [
        {
          id: subnetResourceId
          action: 'Allow'
          state: 'Succeeded'
        }
        {
          id: azureAgent2204SubnetId
          action: 'Allow'
          state: 'Succeeded'
        }
        {
          id: azureAgentPrdSubnetId
          action: 'Allow'
          state: 'Succeeded'
        }
      ]
      ipRules: ipRules
      defaultAction: 'Deny'
    }
  }
  tags: {
    'hidden-title': 'EFS'
  }
}

var roleDefinitionId_StorageBlobDataContributor = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')

resource efs_storage_StorageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(efs_storage.id, principalId, roleDefinitionId_StorageBlobDataContributor)
  properties: {
    principalId: principalId
    roleDefinitionId: roleDefinitionId_StorageBlobDataContributor
    principalType: 'ServicePrincipal'
  }
  scope: efs_storage
}

var roleDefinitionId_StorageTableDataContributor = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')

resource efs_storage_StorageTableDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(efs_storage.id, principalId, roleDefinitionId_StorageTableDataContributor)
  properties: {
    principalId: principalId
    roleDefinitionId: roleDefinitionId_StorageTableDataContributor
    principalType: 'ServicePrincipal'
  }
  scope: efs_storage
}

var roleDefinitionId_StorageQueueDataContributor = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')

resource efs_storage_StorageQueueDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(efs_storage.id, principalId, roleDefinitionId_StorageQueueDataContributor)
  properties: {
    principalId: principalId
    roleDefinitionId: roleDefinitionId_StorageQueueDataContributor
    principalType: 'ServicePrincipal'
  }
  scope: efs_storage
}

output blobEndpoint string = efs_storage.properties.primaryEndpoints.blob

output queueEndpoint string = efs_storage.properties.primaryEndpoints.queue

output tableEndpoint string = efs_storage.properties.primaryEndpoints.table

output name string = efs_storage.name
