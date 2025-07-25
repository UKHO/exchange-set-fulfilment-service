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

module adds_con_kv 'adds-con-kv/adds-con-kv.module.bicep' = {
  name: 'adds-con-kv'
  scope: rg
  params: {
    location: location
  }
}
module adds_con_was 'adds-con-was/adds-con-was.module.bicep' = {
  name: 'adds-con-was'
  scope: rg
  params: {
    location: location
  }
}
module adds_configuration_identity 'adds-configuration-identity/adds-configuration-identity.module.bicep' = {
  name: 'adds-configuration-identity'
  scope: rg
  params: {
    location: location
  }
}
module adds_configuration_roles_adds_con_kv 'adds-configuration-roles-adds-con-kv/adds-configuration-roles-adds-con-kv.module.bicep' = {
  name: 'adds-configuration-roles-adds-con-kv'
  scope: rg
  params: {
    adds_con_kv_outputs_name: adds_con_kv.outputs.name
    location: location
    principalId: adds_configuration_identity.outputs.principalId
  }
}
module adds_configuration_roles_adds_con_was 'adds-configuration-roles-adds-con-was/adds-configuration-roles-adds-con-was.module.bicep' = {
  name: 'adds-configuration-roles-adds-con-was'
  scope: rg
  params: {
    adds_con_was_outputs_name: adds_con_was.outputs.name
    location: location
    principalId: adds_configuration_identity.outputs.principalId
  }
}
module efs_app_insights 'efs-app-insights/efs-app-insights.module.bicep' = {
  name: 'efs-app-insights'
  scope: rg
  params: {
    location: location
    logAnalyticsWorkspaceId: efs_cae.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
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
module efs_orchestrator_roles_efs_storage 'efs-orchestrator-roles-efs-storage/efs-orchestrator-roles-efs-storage.module.bicep' = {
  name: 'efs-orchestrator-roles-efs-storage'
  scope: rg
  params: {
    efs_storage_outputs_name: efs_storage.outputs.name
    location: location
    principalId: efs_orchestrator_identity.outputs.principalId
  }
}
module efs_orchestrator_roles_efseventhub 'efs-orchestrator-roles-efseventhub/efs-orchestrator-roles-efseventhub.module.bicep' = {
  name: 'efs-orchestrator-roles-efseventhub'
  scope: rg
  params: {
    efseventhub_outputs_name: efseventhub.outputs.name
    location: location
    principalId: efs_orchestrator_identity.outputs.principalId
  }
}
module efs_storage 'efs-storage/efs-storage.module.bicep' = {
  name: 'efs-storage'
  scope: rg
  params: {
    location: location
  }
}
module efseventhub 'efseventhub/efseventhub.module.bicep' = {
  name: 'efseventhub'
  scope: rg
  params: {
    location: location
  }
}
output ADDS_CON_KV_VAULTURI string = adds_con_kv.outputs.vaultUri
output ADDS_CON_WAS_TABLEENDPOINT string = adds_con_was.outputs.tableEndpoint
output ADDS_CONFIGURATION_IDENTITY_CLIENTID string = adds_configuration_identity.outputs.clientId
output ADDS_CONFIGURATION_IDENTITY_ID string = adds_configuration_identity.outputs.id
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = efs_cae.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output EFS_APP_INSIGHTS_APPINSIGHTSCONNECTIONSTRING string = efs_app_insights.outputs.appInsightsConnectionString
output EFS_CAE_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output EFS_CAE_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output EFS_CAE_AZURE_CONTAINER_REGISTRY_ENDPOINT string = efs_cae.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output EFS_CAE_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = efs_cae.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output EFSEVENTHUB_EVENTHUBSENDPOINT string = efseventhub.outputs.eventHubsEndpoint
output EFSEVENTHUB_EVENTHUBSENDPOINT_CONNECTION_STRING string = efseventhub.outputs.efseventhubConnectionString
output EFS_ORCHESTRATOR_IDENTITY_CLIENTID string = efs_orchestrator_identity.outputs.clientId
output EFS_ORCHESTRATOR_IDENTITY_ID string = efs_orchestrator_identity.outputs.id
output EFS_STORAGE_BLOBENDPOINT string = efs_storage.outputs.blobEndpoint
output EFS_STORAGE_QUEUEENDPOINT string = efs_storage.outputs.queueEndpoint
output EFS_STORAGE_TABLEENDPOINT string = efs_storage.outputs.tableEndpoint
output EFS_STORAGE_NAME string = efs_storage.outputs.name
