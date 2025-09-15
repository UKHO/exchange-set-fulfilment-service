@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efsContainerRegistryName string

resource efs_cae_acr 'Microsoft.ContainerRegistry/registries@2025-04-01' existing = {
  name: efsContainerRegistryName
}

output name string = efsContainerRegistryName

output loginServer string = efs_cae_acr.properties.loginServer