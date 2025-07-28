@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Standard'

resource efs_events_namespace 'Microsoft.EventHub/namespaces@2024-01-01' = {
  name: take('efs-events-namespace-${uniqueString(resourceGroup().id)}', 256)
  location: location
  sku: {
    name: sku
  }
  tags: {
    'aspire-resource-name': 'efs-events-namespace'
    'hidden-title': 'EFS'
  }
}

resource efs_events_hub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  name: 'efs-events-hub'
  parent: efs_events_namespace
}

resource efseventsnamespaceAuthRule 'Microsoft.EventHub/namespaces/AuthorizationRules@2024-01-01' = {
  name: 'efseventsnamespace_ManageSharedAccessKeyOnly'
  parent: efs_events_namespace
  properties: {
    rights: [
      'Send'
      'Listen'
      'Manage'
    ]
  }
}

var keys = listKeys(efseventsnamespaceAuthRule.id, efseventsnamespaceAuthRule.apiVersion)
var connectionStringWithEntityPath = '${keys.primaryConnectionString};EntityPath=${efs_events_hub.name}'
output eventHubsEndpoint string = connectionStringWithEntityPath
output name string = efs_events_namespace.name
output eventhubname = efs_events_hub.name