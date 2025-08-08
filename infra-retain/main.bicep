targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention. The name of the resource group for your application will include this name.')
param environmentName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'efs-${environmentName}-retain-rg'
  location: location
}

module efs_service_identity 'efs-service-identity/efs-service-identity.module.bicep' = {
  name: 'efs-service-identity'
  scope: rg
  params: {
    location: location
  }
}

output EFS_SERVICE_IDENTITY_RESOURCE_GROUP string = rg.name
output EFS_SERVICE_IDENTITY_CLIENTID string = efs_service_identity.outputs.clientId
output EFS_SERVICE_IDENTITY_ID string = efs_service_identity.outputs.id
output EFS_SERVICE_IDENTITY_NAME string = efs_service_identity.outputs.name
