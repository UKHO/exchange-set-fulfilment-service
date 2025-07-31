@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource efs_orchestrator_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: take('efs_orchestrator_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
  tags: {
    'hidden-title': 'EFS'
  }
}

output id string = efs_orchestrator_identity.id

output clientId string = efs_orchestrator_identity.properties.clientId

output principalId string = efs_orchestrator_identity.properties.principalId

output principalName string = efs_orchestrator_identity.name

output name string = efs_orchestrator_identity.name
