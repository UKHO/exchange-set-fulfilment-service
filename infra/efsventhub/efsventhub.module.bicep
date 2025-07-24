@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Standard'

resource efsventhub 'Microsoft.EventHub/namespaces@2024-01-01' = {
  name: take('efsventhub-${uniqueString(resourceGroup().id)}', 256)
  location: location
  sku: {
    name: sku
  }
  tags: {
    'aspire-resource-name': 'efsventhub'
    'hidden-title': 'EFS'
  }
}

resource efsingestionhub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  name: 'efsingestionhub'
  parent: efsventhub
}

output eventHubsEndpoint string = efsventhub.properties.serviceBusEndpoint

output name string = efsventhub.name