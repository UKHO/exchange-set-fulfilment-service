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

param addsEnvironment string
param addsMocksCpu string
param addsMocksMemory string
@metadata({azd: {
  type: 'generate'
  config: {length:22,noSpecial:true}
  }
})
@secure()
param efs_redis_password string
param efsAppConfigurationName string
param efsAppRegClientId string
param efsAppRegTenantId string
param efsApplicationInsightsName string
param efsB2cAppClientId string
param efsB2cAppDomain string
param efsB2cAppInstance string
param efsB2cAppSigninPolicy string
param efsB2cAppTenantId string
param efsContainerAppsEnvironmentName string
param efsContainerRegistryName string
param efsEventHubsNamespaceName string
@metadata({azd: {
  type: 'resourceGroup'
  config: {}
  }
})
param efsRetainResourceGroup string
param efsServiceIdentityName string
param efsStorageAccountName string
@secure()
param elasticAPMApiKey string
param elasticAPMEnvironment string
param elasticAPMServerURL string
param elasticAPMServiceName string
param orchestratorCpu string
param orchestratorMemory string

var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'efs-${environmentName}-rg'
  location: location
  tags: tags
}

module efs_app_insights 'efs-app-insights/efs-app-insights.module.bicep' = {
  name: 'efs-app-insights'
  scope: rg
  params: {
    efsApplicationInsightsName: efsApplicationInsightsName
    location: location
  }
}
module efs_appconfig 'efs-appconfig/efs-appconfig.module.bicep' = {
  name: 'efs-appconfig'
  scope: rg
  params: {
    efsAppConfigurationName: efsAppConfigurationName
    location: location
  }
}
module efs_cae 'efs-cae/efs-cae.module.bicep' = {
  name: 'efs-cae'
  scope: resourceGroup(efsRetainResourceGroup)
  params: {
    location: location
    efsContainerAppsEnvironmentName: efsContainerAppsEnvironmentName
  }
}
module efs_cae_acr 'efs-cae-acr/efs-cae-acr.module.bicep' = {
  name: 'efs-cae-acr'
  scope: rg
  params: {
    efsContainerRegistryName: efsContainerRegistryName
    location: location
  }
}
module efs_events_namespace 'efs-events-namespace/efs-events-namespace.module.bicep' = {
  name: 'efs-events-namespace'
  scope: resourceGroup(efsRetainResourceGroup)
  params: {
    efsEventHubsNamespaceName: efsEventHubsNamespaceName
    location: location
  }
}
module efs_service_identity 'efs-service-identity/efs-service-identity.module.bicep' = {
  name: 'efs-service-identity'
  scope: resourceGroup(efsRetainResourceGroup)
  params: {
    efsServiceIdentityName: efsServiceIdentityName
    location: location
  }
}
module efs_storage 'efs-storage/efs-storage.module.bicep' = {
  name: 'efs-storage'
  scope: rg
  params: {
    efsStorageAccountName: efsStorageAccountName
    location: location
  }
}
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = efs_cae_acr.outputs.loginServer
output EFS_APP_INSIGHTS_APPINSIGHTSCONNECTIONSTRING string = efs_app_insights.outputs.appInsightsConnectionString
output EFS_APPCONFIG_APPCONFIGENDPOINT string = efs_appconfig.outputs.appConfigEndpoint
output EFS_CAE_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN
output EFS_CAE_AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = efs_cae.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output EFS_CAE_AZURE_CONTAINER_REGISTRY_ENDPOINT string = efs_cae_acr.outputs.loginServer
output EFS_CAE_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = efs_service_identity.outputs.id
output EFS_EVENTS_NAMESPACE_EVENTHUBSENDPOINT string = efs_events_namespace.outputs.eventHubsEndpoint
output EFS_SERVICE_IDENTITY_CLIENTID string = efs_service_identity.outputs.clientId
output EFS_SERVICE_IDENTITY_ID string = efs_service_identity.outputs.id
output EFS_STORAGE_BLOBENDPOINT string = efs_storage.outputs.blobEndpoint
output EFS_STORAGE_QUEUEENDPOINT string = efs_storage.outputs.queueEndpoint
output EFS_STORAGE_TABLEENDPOINT string = efs_storage.outputs.tableEndpoint
output EFS_STORAGE_NAME string = efs_storage.outputs.name
