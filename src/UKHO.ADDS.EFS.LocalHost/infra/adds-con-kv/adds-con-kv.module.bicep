@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource adds_con_kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('addsconkv-${uniqueString(resourceGroup().id)}', 24)
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
    'aspire-resource-name': 'adds-con-kv'
    'hidden-title': 'EFS'
  }
}

output vaultUri string = adds_con_kv.properties.vaultUri

output name string = adds_con_kv.name