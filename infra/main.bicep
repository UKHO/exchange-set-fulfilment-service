targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
param environmentName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''

param subnetName string
param subnetResourceGroup string
param subnetSubscription string
param subnetVnet string

var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module adds_configuration_identity 'adds-configuration-identity/adds-configuration-identity.module.bicep' = {
  name: 'adds-configuration-identity'
  scope: rg
  params: {
    location: location
  }
}
module adds_configuration_kv 'adds-configuration-kv/adds-configuration-kv.module.bicep' = {
  name: 'adds-configuration-kv'
  scope: rg
  params: {
    location: location
  }
}
module adds_configuration_roles_adds_configuration_kv 'adds-configuration-roles-adds-configuration-kv/adds-configuration-roles-adds-configuration-kv.module.bicep' = {
  name: 'adds-configuration-roles-adds-configuration-kv'
  scope: rg
  params: {
    adds_configuration_kv_outputs_name: adds_configuration_kv.outputs.name
    location: location
    principalId: adds_configuration_identity.outputs.principalId
  }
}
module adds_configuration_roles_adds_configuration_was 'adds-configuration-roles-adds-configuration-was/adds-configuration-roles-adds-configuration-was.module.bicep' = {
  name: 'adds-configuration-roles-adds-configuration-was'
  scope: rg
  params: {
    adds_configuration_was_outputs_name: adds_configuration_was.outputs.name
    location: location
    principalId: adds_configuration_identity.outputs.principalId
  }
}
module adds_configuration_was 'adds-configuration-was/adds-configuration-was.module.bicep' = {
  name: 'adds-configuration-was'
  scope: rg
  params: {
    location: location
  }
}
module efs_cae 'efs-cae/efs-cae.module.bicep' = {
  name: 'efs-cae'
  scope: rg
  params: {
    location: location
    subnetName: subnetName
    subnetResourceGroup: subnetResourceGroup
    subnetSubscription: subnetSubscription
    subnetVnet: subnetVnet
    userPrincipalId: principalId
  }
}
module efs_orchestrator_identity 'efs-orchestrator-identity/efs-orchestrator-identity.module.bicep' = {
  name: 'efs-orchestrator-identity'
  scope: rg
  params: {
    location: location
  }
}
module efs_orchestrator_roles_storage 'efs-orchestrator-roles-storage/efs-orchestrator-roles-storage.module.bicep' = {
  name: 'efs-orchestrator-roles-storage'
  scope: rg
  params: {
    location: location
    principalId: efs_orchestrator_identity.outputs.principalId
    storage_outputs_name: storage.outputs.name
  }
}
module storage 'storage/storage.module.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    location: location
  }
}
output ADDS_CONFIGURATION_IDENTITY_CLIENTID string = adds_configuration_identity.outputs.clientId
output ADDS_CONFIGURATION_IDENTITY_ID string = adds_configuration_identity.outputs.id
output ADDS_CONFIGURATION_KV_VAULTURI string = adds_configuration_kv.outputs.vaultUri
output ADDS_CONFIGURATION_WAS_TABLEENDPOINT string = adds_configuration_was.outputs.tableEndpoint
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = efs_cae.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output EFS_CAE_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output EFS_CAE_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output EFS_CAE_AZURE_CONTAINER_REGISTRY_ENDPOINT string = efs_cae.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output EFS_CAE_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = efs_cae.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output EFS_ORCHESTRATOR_IDENTITY_CLIENTID string = efs_orchestrator_identity.outputs.clientId
output EFS_ORCHESTRATOR_IDENTITY_ID string = efs_orchestrator_identity.outputs.id
output STORAGE_BLOBENDPOINT string = storage.outputs.blobEndpoint
output STORAGE_QUEUEENDPOINT string = storage.outputs.queueEndpoint
output STORAGE_TABLEENDPOINT string = storage.outputs.tableEndpoint
output STORAGE_NAME string = storage.outputs.name
