@description('Name of the storage account')
param storageAccountName string

@description('Event Hub namespace authorization rule resource ID')
param eventHubAuthorizationRuleId string

@description('Name of the Event Hub')
param eventHubName string

@description('Base name for diagnostic settings')
param diagnosticSettingsName string = 'efs-storage-diagnostic-logs-to-event-hub'

resource storageAccount 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
  name: storageAccountName
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' existing = {
  name: 'default'
  parent: storageAccount
}

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2024-01-01' existing = {
  name: 'default'
  parent: storageAccount
}

resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2024-01-01' existing = {
  name: 'default'
  parent: storageAccount
}

var targetNames = [
  'storage'
  'blob'
  'queue'
  'table'
]

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = [for name in targetNames: {
  name: '${diagnosticSettingsName}-${name}'
  scope: (
    name == 'storage' ? storageAccount :
    name == 'blob' ? blobService :
    name == 'queue' ? queueService :
    tableService
  )
  properties: {
    eventHubAuthorizationRuleId: eventHubAuthorizationRuleId
    eventHubName: eventHubName
    logs: name == 'storage' ? [] : [
      {
        categoryGroup: 'audit'
        enabled: true
      }
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'Transaction'
        enabled: true
      }
    ]
  }
}]
