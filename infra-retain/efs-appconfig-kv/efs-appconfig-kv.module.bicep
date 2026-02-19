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

output vaultUri string = efs_dev_appconfig_kv.properties.vaultUri
