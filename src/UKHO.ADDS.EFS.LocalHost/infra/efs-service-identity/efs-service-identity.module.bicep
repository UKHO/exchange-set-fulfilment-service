@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efsServiceIdentityName string

resource efs_service_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: efsServiceIdentityName
}

output id string = efs_service_identity.id

output clientId string = efs_service_identity.properties.clientId

output principalId string = efs_service_identity.properties.principalId

output principalName string = efsServiceIdentityName