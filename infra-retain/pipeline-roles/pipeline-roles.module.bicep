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
