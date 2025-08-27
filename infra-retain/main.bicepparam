using './main.bicep'

param resourceGroupName = readEnvironmentVariable('EFS_RETAIN_RESOURCE_GROUP')
param efsServiceIdentityPartialName = readEnvironmentVariable('EFS_SERVICE_IDENTITY_PARTIAL_NAME')
param location = readEnvironmentVariable('AZURE_LOCATION')
param pipelineDeploymentName = readEnvironmentVariable('PIPELINE_DEPLOYMENT_NAME')
param pipelineClientObjectId = readEnvironmentVariable('PIPELINE-CLIENT-OBJECT-ID')
param principalId = readEnvironmentVariable('AZURE_PRINCIPAL_ID')
param subnetResourceId = readEnvironmentVariable('AZURE_SUBNET_RESOURCE_ID')
param zoneRedundant = bool(readEnvironmentVariable('AZURE_ZONE_REDUNDANT'))
