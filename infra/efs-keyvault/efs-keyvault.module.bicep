@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource efs_keyvault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('efskeyvault-${uniqueString(resourceGroup().id)}', 24)
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
    'aspire-resource-name': 'efs-keyvault'
  }
}

output vaultUri string = efs_keyvault.properties.vaultUri

output name string = efs_keyvault.name