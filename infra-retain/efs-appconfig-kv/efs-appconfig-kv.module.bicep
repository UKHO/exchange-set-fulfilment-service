@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

@minLength(1)
@maxLength(24)
@description('The name of the app config key vault resource.')
param efsAppConfigKeyVaultName string

resource efs_dev_appconfig_kv 'Microsoft.KeyVault/vaults@2025-05-01' = {
  name: efsAppConfigKeyVaultName
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
  }
  tags: {
    'hidden-title': 'EFS'
  }
}

var roleDefinitionId_KeyVaultSecretsUser = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')

resource roleAssignment_KeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(efs_dev_appconfig_kv.id, principalId, roleDefinitionId_KeyVaultSecretsUser)
  properties: {
    roleDefinitionId: roleDefinitionId_KeyVaultSecretsUser
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
  scope: efs_dev_appconfig_kv
}

output vaultUri string = efs_dev_appconfig_kv.properties.vaultUri
