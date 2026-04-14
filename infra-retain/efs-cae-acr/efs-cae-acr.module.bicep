@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

@minLength(5)
@maxLength(50)
@description('The name of the container registry resource.')
param efsContainerRegistryName string

@description('Optional list of allowed IPv4 addresses or CIDR ranges.')
param ipRules array = []

resource efs_cae_acr 'Microsoft.ContainerRegistry/registries@2025-11-01' = {
  name: efsContainerRegistryName
  location: location
  sku: {
    name: 'Basic'
  }
  tags: {
    'hidden-title': 'EFS'
  }
  properties: {
    networkRuleBypassAllowedForTasks: false
    networkRuleBypassOptions: 'AzureServices'
    networkRuleSet: {
      defaultAction: 'Deny'
      ipRules: ipRules
    }
  }
}

var roleDefinitionId_AcrPull = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')

resource roleAssignment_AcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(efs_cae_acr.id, principalId, roleDefinitionId_AcrPull)
  properties: {
    roleDefinitionId: roleDefinitionId_AcrPull
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
  scope: efs_cae_acr
}

output name string = efs_cae_acr.name
