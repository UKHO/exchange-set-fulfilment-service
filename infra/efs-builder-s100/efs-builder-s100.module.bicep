@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efs_cae_outputs_azure_container_apps_environment_default_domain string

param efs_cae_outputs_azure_container_apps_environment_id string

param efs_cae_outputs_azure_container_registry_endpoint string

param efs_cae_outputs_azure_container_registry_managed_identity_id string

param efs_builder_s100_containerimage string

resource efs_builder_s100 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'efs-builder-s100'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
      }
      registries: [
        {
          server: efs_cae_outputs_azure_container_registry_endpoint
          identity: efs_cae_outputs_azure_container_registry_managed_identity_id
        }
      ]
    }
    environmentId: efs_cae_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: efs_builder_s100_containerimage
          name: 'efs-builder-s100'
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1000
      }
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${efs_cae_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}