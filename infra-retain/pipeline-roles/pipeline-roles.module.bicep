targetScope = 'subscription'

param principalId string

var roleDefinitionId_AppConfigurationDataOwner = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b')

resource roleAssignment_AppConfigurationDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, principalId, roleDefinitionId_AppConfigurationDataOwner)
  properties: {
    roleDefinitionId: roleDefinitionId_AppConfigurationDataOwner
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

var roleDefinitionId_KeyVaultSecretsOfficer = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7')

resource roleAssignment_KeyVaultSecretsOfficer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, principalId, roleDefinitionId_KeyVaultSecretsOfficer)
  properties: {
    roleDefinitionId: roleDefinitionId_KeyVaultSecretsOfficer
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
