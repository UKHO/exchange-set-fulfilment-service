@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efs_keyvault_outputs_name string

param principalId string

resource efs_keyvault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: efs_keyvault_outputs_name
}

resource efs_keyvault_KeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(efs_keyvault.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalType: 'ServicePrincipal'
  }
  scope: efs_keyvault
}