@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efsContainerAppsEnvironmentName string

resource efs_cae 'Microsoft.App/managedEnvironments@2025-01-01' existing = {
  name: efsContainerAppsEnvironmentName
}

output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = efs_cae.name

output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = efs_cae.id

output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = efs_cae.properties.defaultDomain
