@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource adds_configuration_kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('addsconfigurationkv-${uniqueString(resourceGroup().id)}', 24)
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
    'aspire-resource-name': 'adds-configuration-kv'
    'hidden-title': 'EFS'
  }
}

output vaultUri string = adds_configuration_kv.properties.vaultUri

output name string = adds_configuration_kv.name