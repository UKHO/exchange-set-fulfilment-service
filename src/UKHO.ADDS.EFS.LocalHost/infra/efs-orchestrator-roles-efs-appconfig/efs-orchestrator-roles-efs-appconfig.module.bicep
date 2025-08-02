@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efs_appconfig_outputs_name string

param principalId string

resource efs_appconfig 'Microsoft.AppConfiguration/configurationStores@2024-06-01' existing = {
  name: efs_appconfig_outputs_name
}

resource efs_appconfig_AppConfigurationDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(efs_appconfig.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b'))
  properties: {
    principalId: principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5ae67dd6-50cb-40e7-96ff-dc2bfa4b606b')
    principalType: 'ServicePrincipal'
  }
  scope: efs_appconfig
}