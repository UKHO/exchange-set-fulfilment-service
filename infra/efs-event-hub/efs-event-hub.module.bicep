@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Standard'

resource efs_event_hub 'Microsoft.EventHub/namespaces@2024-01-01' = {
  name: take('efs_event_hub-${uniqueString(resourceGroup().id)}', 256)
  location: location
  sku: {
    name: sku
  }
  tags: {
    'aspire-resource-name': 'efs-event-hub'
    'hidden-title': 'EFS'
  }
}

resource efs_ingestion_hub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  name: 'efs-ingestion-hub'
  parent: efs_event_hub
}

output eventHubsEndpoint string = efs_event_hub.properties.serviceBusEndpoint

output name string = efs_event_hub.name