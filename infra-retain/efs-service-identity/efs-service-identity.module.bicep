@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource efs_service_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: take('efs_service_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
  tags: {
    'hidden-title': 'EFS'
  }
}

output id string = efs_service_identity.id

output clientId string = efs_service_identity.properties.clientId

output name string = efs_service_identity.name
