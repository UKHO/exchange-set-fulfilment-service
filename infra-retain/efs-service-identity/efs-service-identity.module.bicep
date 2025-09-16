@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

@minLength(1)
@description('The partial name (from the start) of the service identity resource.')
param efsServiceIdentityPartialName string

resource efs_service_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('${efsServiceIdentityPartialName}-${uniqueString(resourceGroup().id)}', 128)
  location: location
  tags: {
    'hidden-title': 'EFS'
  }
}

output id string = efs_service_identity.id

output clientId string = efs_service_identity.properties.clientId

output principalId string = efs_service_identity.properties.principalId

output name string = efs_service_identity.name
