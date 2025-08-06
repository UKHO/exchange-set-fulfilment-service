targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention. The name of the resource group for your application will include this name.')
param environmentName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''

@metadata({azd: {
  type: 'generate'
  config: {length:22,noSpecial:true}
  }
})
@secure()
param efs_redis_password string
param efsServiceIdentityName string
@metadata({azd: {
  type: 'resourceGroup'
  config: {}
  }
})
param efsServiceIdentityResourceGroup string
param subnetResourceId string
param zoneRedundant bool

var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'efs-${environmentName}-rg'
  location: location
  tags: tags
}

module efs_appconfig 'efs-appconfig/efs-appconfig.module.bicep' = {
  name: 'efs-appconfig'
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
    subnetResourceId: subnetResourceId
    userPrincipalId: principalId
    zoneRedundant: zoneRedundant
  }
}
module efs_orchestrator_identity 'efs-orchestrator-identity/efs-orchestrator-identity.module.bicep' = {
  name: 'efs-orchestrator-identity'
  scope: rg
  params: {
    location: location
  }
}
module efs_orchestrator_roles_efs_appconfig 'efs-orchestrator-roles-efs-appconfig/efs-orchestrator-roles-efs-appconfig.module.bicep' = {
  name: 'efs-orchestrator-roles-efs-appconfig'
  scope: rg
  params: {
    efs_appconfig_outputs_name: efs_appconfig.outputs.name
    location: location
    principalId: efs_orchestrator_identity.outputs.principalId
  }
}
module efs_orchestrator_roles_efs_storage 'efs-orchestrator-roles-efs-storage/efs-orchestrator-roles-efs-storage.module.bicep' = {
  name: 'efs-orchestrator-roles-efs-storage'
  scope: rg
  params: {
    efs_storage_outputs_name: efs_storage.outputs.name
    location: location
    principalId: efs_orchestrator_identity.outputs.principalId
  }
}
module efs_service_identity 'efs-service-identity/efs-service-identity.module.bicep' = {
  name: 'efs-service-identity'
  scope: resourceGroup(efsServiceIdentityResourceGroup)
  params: {
    efsServiceIdentityName: efsServiceIdentityName
    location: location
  }
}
module efs_storage 'efs-storage/efs-storage.module.bicep' = {
  name: 'efs-storage'
  scope: rg
  params: {
    location: location
  }
}
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = efs_cae.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output EFS_APPCONFIG_APPCONFIGENDPOINT string = efs_appconfig.outputs.appConfigEndpoint
output EFS_CAE_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output EFS_CAE_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output EFS_CAE_AZURE_CONTAINER_REGISTRY_ENDPOINT string = efs_cae.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output EFS_CAE_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = efs_cae.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output EFS_ORCHESTRATOR_IDENTITY_CLIENTID string = efs_orchestrator_identity.outputs.clientId
output EFS_ORCHESTRATOR_IDENTITY_ID string = efs_orchestrator_identity.outputs.id
output EFS_STORAGE_BLOBENDPOINT string = efs_storage.outputs.blobEndpoint
output EFS_STORAGE_QUEUEENDPOINT string = efs_storage.outputs.queueEndpoint
output EFS_STORAGE_TABLEENDPOINT string = efs_storage.outputs.tableEndpoint
output EFS_STORAGE_NAME string = efs_storage.outputs.name
output EFS_SERVICE_IDENTITY_ID string = efs_service_identity.outputs.id
