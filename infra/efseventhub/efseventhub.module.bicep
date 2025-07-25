@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param sku string = 'Standard'

resource efseventhub 'Microsoft.EventHub/namespaces@2024-01-01' = {
  name: take('efseventhub-${uniqueString(resourceGroup().id)}', 256)
  location: location
  sku: {
    name: sku
  }
  tags: {
    'aspire-resource-name': 'efseventhub'
    'hidden-title': 'EFS'
  }
}

resource efsingestionhub 'Microsoft.EventHub/namespaces/eventhubs@2024-01-01' = {
  name: 'efsingestionhub'
  parent: efseventhub
}

resource efseventhubAuthRule 'Microsoft.EventHub/namespaces/AuthorizationRules@2024-01-01' = {
  name: 'RootAccessPolicy'
  parent: efseventhub
  properties: {
    rights: [
      'Listen'
      'Send'
      'Manage'
    ]
  }
}

output name string = efseventhub.name

output efseventhubConnectionString string = listKeys(efseventhubAuthRule.id, efseventhubAuthRule.apiVersion).primaryConnectionString
