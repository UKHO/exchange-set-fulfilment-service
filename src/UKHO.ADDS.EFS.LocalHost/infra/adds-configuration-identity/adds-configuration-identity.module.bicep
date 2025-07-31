@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource adds_configuration_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('adds_configuration_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
  tags: {
    'hidden-title': 'EFS'
  }
}

output id string = adds_configuration_identity.id

output clientId string = adds_configuration_identity.properties.clientId

output principalId string = adds_configuration_identity.properties.principalId

output principalName string = adds_configuration_identity.name

output name string = adds_configuration_identity.name
