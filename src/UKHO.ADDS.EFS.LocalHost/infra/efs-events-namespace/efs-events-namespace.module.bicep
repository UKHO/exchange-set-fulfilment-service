@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efsEventHubsNamespaceName string

resource efs_events_namespace 'Microsoft.EventHub/namespaces@2024-01-01' existing = {
  name: efsEventHubsNamespaceName
}

output eventHubsEndpoint string = efs_events_namespace.properties.serviceBusEndpoint

output name string = efsEventHubsNamespaceName
output eventHubAuthorizationRuleId string = '${efs_events_namespace.id}/AuthorizationRules/RootManageSharedAccessKey'
output eventHubName string = efs_events_hub.name