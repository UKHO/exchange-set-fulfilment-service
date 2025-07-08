@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param adds_configuration_kv_outputs_name string

param principalId string

resource adds_configuration_kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: adds_configuration_kv_outputs_name
}

resource adds_configuration_kv_KeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(adds_configuration_kv.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalType: 'ServicePrincipal'
  }
  scope: adds_configuration_kv
}