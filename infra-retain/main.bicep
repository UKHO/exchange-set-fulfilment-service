targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the resource group for fixed resources.')
param resourceGroupName string

@minLength(1)
@maxLength(64)
@description('Name of the resource group for applications.')
param appResourceGroupName string

@minLength(1)
@description('The partial name (from the start) of the service identity resource.')
param efsServiceIdentityPartialName string

@minLength(1)
@description('The partial name (from the start) of the log analytics workspace resource.')
param efsLogAnalyticsWorkspacePartialName string

@minLength(1)
@description('The partial name (from the start) of the application insights resource.')
param efsApplicationInsightsPartialName string

@minLength(1)
@description('The partial name (from the start) of the app configuration resource.')
param efsAppConfigurationPartialName string

@minLength(1)
@description('The partial name (from the start) of the event hub namespace resource.')
param efsEventHubsNamespacePartialName string

@minLength(1)
@description('The partial name (from the start) of the container registry resource.')
param efsContainerRegistryPartialName string

@minLength(1)
@description('The partial name (from the start) of the container apps environment resource.')
param efsContainerAppsEnvironmentPartialName string

@minLength(1)
@description('The partial name (from the start) of the storage account resource.')
param efsStorageAccountPartialName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@minLength(1)
@description('The name of the deployment for pipeline roles')
param pipelineDeploymentName string

@minLength(1)
@description('The id of the pipeline service principal')
param pipelineClientObjectId string

@minLength(1)
@description('Id of the container app subnet')
param subnetResourceId string

@description('Enable zone redundancy during deployment')
param zoneRedundant bool

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: resourceGroupName
  location: location
}

resource app_rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: appResourceGroupName
  location: location
}

module efs_service_identity 'efs-service-identity/efs-service-identity.module.bicep' = {
  name: 'efs-service-identity'
  scope: rg
  params: {
    location: location
    efsServiceIdentityPartialName: efsServiceIdentityPartialName
  }
}

module efs_law 'efs-law/efs-law.module.bicep' = {
  name: 'efs-law'
  scope: rg
  params: {
    location: location
    efsLogAnalyticsWorkspacePartialName: efsLogAnalyticsWorkspacePartialName
  }
}

module efs_app_insights 'efs-app-insights/efs-app-insights.module.bicep' = {
  name: 'efs-app-insights'
  scope: app_rg
  params: {
    efs_law_outputs_loganalyticsworkspaceid: efs_law.outputs.logAnalyticsWorkspaceId
    location: location
    efsApplicationInsightsPartialName: efsApplicationInsightsPartialName
  }
}

module efs_appconfig 'efs-appconfig/efs-appconfig.module.bicep' = {
  name: 'efs-appconfig'
  scope: app_rg
  params: {
    location: location
    principalId: efs_service_identity.outputs.principalId
    efsAppConfigurationPartialName: efsAppConfigurationPartialName
  }
}

module efs_events_namespace 'efs-events-namespace/efs-events-namespace.module.bicep' = {
  name: 'efs-events-namespace'
  scope: rg
  params: {
    location: location
    principalId: efs_service_identity.outputs.principalId
    efsEventHubsNamespacePartialName: efsEventHubsNamespacePartialName
  }
}

module efs_cae_acr 'efs-cae-acr/efs-cae-acr.module.bicep' = {
  name: 'efs-cae-acr'
  scope: app_rg
  params: {
    location: location
    principalId: efs_service_identity.outputs.principalId
    efsContainerRegistryPartialName: efsContainerRegistryPartialName
  }
}

module efs_cae 'efs-cae/efs-cae.module.bicep' = {
  name: 'efs-cae'
  scope: rg
  params: {
    efs_law_outputs_name: efs_law.outputs.name
    location: location
    subnetResourceId: subnetResourceId
    zoneRedundant: zoneRedundant
    efsContainerAppsEnvironmentPartialName: efsContainerAppsEnvironmentPartialName
  }
}

module efs_storage 'efs-storage/efs-storage.module.bicep' = {
  name: 'efs-storage'
  scope: app_rg
  params: {
    location: location
    principalId: efs_service_identity.outputs.principalId
    efsStorageAccountPartialName: efsStorageAccountPartialName
  }
}

module efs_diagnostic_settings 'efs-diagnostic-settings/efs-diagnostic-settings.module.bicep' = {
  name: 'efs-diagnostic-settings'
  scope: app_rg
  params: {
    storageAccountName: efs_storage.outputs.name
    eventHubAuthorizationRuleId: efs_events_namespace.outputs.eventHubAuthorizationRuleId
    eventHubName: efs_events_namespace.outputs.eventHubName
  }
}

module pipeline_roles 'pipeline-roles/pipeline-roles.module.bicep' = {
  name: pipelineDeploymentName
  params: {
    principalId: pipelineClientObjectId
  }
}

output EFS_RETAIN_RESOURCE_GROUP string = rg.name
output EFS_RESOURCE_GROUP string = app_rg.name
output EFS_SERVICE_IDENTITY_NAME string = efs_service_identity.outputs.name
output EFS_LAW_NAME string = efs_law.outputs.name
output EFS_APP_INSIGHTS_NAME string = efs_app_insights.outputs.name
output EFS_APPCONFIG_NAME string = efs_appconfig.outputs.name
output EFS_EVENTS_NAMESPACE_NAME string = efs_events_namespace.outputs.name
output EFS_CAE_NAME string = efs_cae.outputs.name
output EFS_CAE_DEFAULT_DOMAIN string = efs_cae.outputs.defaultDomain
