@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efs_cae_outputs_azure_container_apps_environment_id string

param efs_cae_outputs_azure_container_registry_endpoint string

param efs_cae_outputs_azure_container_registry_managed_identity_id string

param adds_mocks_efs_containerimage string

param adds_mocks_efs_containerport string

resource adds_mocks_efs 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'adds-mocks-efs'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: int(adds_mocks_efs_containerport)
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
          image: adds_mocks_efs_containerimage
          name: 'adds-mocks-efs'
          env: [
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES'
              value: 'true'
            }
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES'
              value: 'true'
            }
            {
              name: 'OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY'
              value: 'in_memory'
            }
            {
              name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
              value: 'true'
            }
            {
              name: 'HTTP_PORTS'
              value: adds_mocks_efs_containerport
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
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
