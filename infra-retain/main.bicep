targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the resource group for fixed resources.')
param resourceGroupName string

@minLength(1)
@description('The partial name (from the start) of the service identity resource.')
param efsServiceIdentityPartialName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@minLength(1)
@description('The name of the deployment for pipeline roles')
param pipelineDeploymentName string

@minLength(1)
@description('The id of the pipeline service principal')
param pipelineClientObjectId string

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: resourceGroupName
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
  }
}

module pipeline_roles 'pipeline-roles/pipeline-roles.module.bicep' = {
  name: pipelineDeploymentName
  params: {
    principalId: pipelineClientObjectId
  }
}

output EFS_SERVICE_IDENTITY_RESOURCE_GROUP string = rg.name
output EFS_SERVICE_IDENTITY_CLIENTID string = efs_service_identity.outputs.clientId
output EFS_SERVICE_IDENTITY_ID string = efs_service_identity.outputs.id
output EFS_SERVICE_IDENTITY_NAME string = efs_service_identity.outputs.name
output EFS_LAW_ID string = efs_law.outputs.logAnalyticsWorkspaceId
output EFS_LAW_NAME string = efs_law.outputs.name
