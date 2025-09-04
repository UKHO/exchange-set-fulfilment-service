@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

resource efs_appconfig 'Microsoft.AppConfiguration/configurationStores@2024-06-01' = {
  name: take('efsappconfig-${uniqueString(resourceGroup().id)}', 50)
  location: location
  properties: {
    disableLocalAuth: true
  }
  sku: {
    name: 'standard'
  }
  tags: {
    'hidden-title': 'EFS'
  }
}

var roleDefinitionId_AppConfigurationDataOwner = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b')

resource efs_events_namespace_AppConfigurationDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(efs_appconfig.id, principalId, roleDefinitionId_AppConfigurationDataOwner)
  properties: {
    principalId: principalId
    roleDefinitionId: roleDefinitionId_AppConfigurationDataOwner
    principalType: 'ServicePrincipal'
  }
  scope: efs_appconfig
}

output appConfigEndpoint string = efs_appconfig.properties.endpoint

output name string = efs_appconfig.name
