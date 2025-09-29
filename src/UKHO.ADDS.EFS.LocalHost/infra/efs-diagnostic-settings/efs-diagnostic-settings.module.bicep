@description('Name of the storage account')
param storageAccountName string

@description('Event hub namespace authorization rule resource ID')
param eventHubAuthorizationRuleId string

@description('Name of the event hub')
param eventHubName string

@description('Name for diagnostic settings')
param diagnosticSettingsName string = 'efs-storage-diagnostic-logs-to-event-hub'

resource storageAccount 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
  name: storageAccountName
}

resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: diagnosticSettingsName
  scope: storageAccount
  properties: {
    eventHubAuthorizationRuleId: eventHubAuthorizationRuleId
    eventHubName: eventHubName
    logs: [
      /* Storage Account level logs */
      {
        category: 'StorageRead'
        enabled: true
      }
      {
        category: 'StorageWrite'
        enabled: true
      }
      {
        category: 'StorageDelete'
        enabled: true
      }
      {
        category: 'Transaction'
        enabled: true
      }
      /* Service-specific logs */
      {
        category: 'BlobStorage'    // Blob operations
        enabled: true
      }
      {
        category: 'QueueStorage'   // Queue operations
        enabled: true
      }
      {
        category: 'TableStorage'   // Table operations
        enabled: true
      }
      {
        category: 'FileStorage'    // File share operations
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'Transaction'
        enabled: true
      }
      {
        category: 'Capacity'
        enabled: true
      }
    ]
  }
  tags: {
    'hidden-title': 'EFS'
  }
}

output diagnosticSettingsId string = diagnosticSettings.id