@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

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
    'aspire-resource-name': 'efs-events-namespace'
    'hidden-title': 'EFS'
  }
}

resource efs_events_hub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  name: 'efs-events-hub'
  parent: efs_events_namespace
}

output eventHubsEndpoint string = efs_events_namespace.properties.serviceBusEndpoint

output name string = efs_events_namespace.name
