@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efsContainerAppsEnvironmentName string

resource efs_cae 'Microsoft.App/managedEnvironments@2025-01-01' existing = {
  name: efsContainerAppsEnvironmentName
}

output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = efs_cae.name

output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = efs_cae.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = efs_cae.properties.defaultDomain

var caeFull = reference(efs_cae.id, '2025-01-01', 'full')

output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = contains(caeFull, 'identity') && contains(caeFull.identity, 'userAssignedIdentities') && length(objectKeys(caeFull.identity.userAssignedIdentities)) > 0
  ? objectKeys(caeFull.identity.userAssignedIdentities)[0]
  : ''
