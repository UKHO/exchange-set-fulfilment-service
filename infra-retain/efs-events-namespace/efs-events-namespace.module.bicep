@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

param sku string = 'Standard'

resource efs_events_namespace 'Microsoft.EventHub/namespaces@2024-01-01' = {
  name: take('efs-events-namespace-${uniqueString(resourceGroup().id)}', 256)
  location: location
  properties: {
    disableLocalAuth: false
  }
  sku: {
    name: sku
  }
  tags: {
    'hidden-title': 'EFS'
  }
}

resource efs_events_hub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  name: 'efs-events-hub'
  parent: efs_events_namespace
}

var roleDefinitionId_AzureEventHubsDataOwner = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'f526a384-b230-433a-b45c-95f59c4a2dec')

resource efs_events_namespace_AzureEventHubsDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(efs_events_namespace.id, principalId, roleDefinitionId_AzureEventHubsDataOwner)
  properties: {
    principalId: principalId
    roleDefinitionId: roleDefinitionId_AzureEventHubsDataOwner
    principalType: 'ServicePrincipal'
  }
  scope: efs_events_namespace
}

output eventHubsEndpoint string = efs_events_namespace.properties.serviceBusEndpoint

output name string = efs_events_namespace.name
