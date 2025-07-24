@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efsventhub_outputs_name string

param principalId string

resource efsventhub 'Microsoft.EventHub/namespaces@2024-01-01' existing = {
  name: efsventhub_outputs_name
}

resource efsventhub_AzureEventHubsDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(efsventhub.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec')
    principalType: 'ServicePrincipal'
  }
  scope: efsventhub
}