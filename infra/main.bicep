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

module efs_cae 'efs-cae/efs-cae.module.bicep' = {
  name: 'efs-cae'
  scope: rg
  params: {
    location: location
    userPrincipalId: principalId
  }
}
module efs_keyvault 'efs-keyvault/efs-keyvault.module.bicep' = {
  name: 'efs-keyvault'
  scope: rg
  params: {
    location: location
  }
}
module efs_orchestrator_identity 'efs-orchestrator-identity/efs-orchestrator-identity.module.bicep' = {
  name: 'efs-orchestrator-identity'
  scope: rg
  params: {
    location: location
  }
}
module efs_orchestrator_roles_efs_keyvault 'efs-orchestrator-roles-efs-keyvault/efs-orchestrator-roles-efs-keyvault.module.bicep' = {
  name: 'efs-orchestrator-roles-efs-keyvault'
  scope: rg
  params: {
    efs_keyvault_outputs_name: efs_keyvault.outputs.name
    location: location
    principalId: efs_orchestrator_identity.outputs.principalId
  }
}
module efs_orchestrator_roles_efssa 'efs-orchestrator-roles-efssa/efs-orchestrator-roles-efssa.module.bicep' = {
  name: 'efs-orchestrator-roles-efssa'
  scope: rg
  params: {
    efssa_outputs_name: efssa.outputs.name
    location: location
    principalId: efs_orchestrator_identity.outputs.principalId
  }
}
module efssa 'efssa/efssa.module.bicep' = {
  name: 'efssa'
  scope: rg
  params: {
    location: location
  }
}
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = efs_cae.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output EFS_CAE_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output EFS_CAE_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output EFS_CAE_AZURE_CONTAINER_REGISTRY_ENDPOINT string = efs_cae.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output EFS_CAE_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = efs_cae.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output EFS_KEYVAULT_VAULTURI string = efs_keyvault.outputs.vaultUri
output EFS_ORCHESTRATOR_IDENTITY_CLIENTID string = efs_orchestrator_identity.outputs.clientId
output EFS_ORCHESTRATOR_IDENTITY_ID string = efs_orchestrator_identity.outputs.id
output EFSSA_BLOBENDPOINT string = efssa.outputs.blobEndpoint
output EFSSA_QUEUEENDPOINT string = efssa.outputs.queueEndpoint
output EFSSA_TABLEENDPOINT string = efssa.outputs.tableEndpoint
