@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efs_cae_outputs_azure_container_apps_environment_default_domain string

param efs_cae_outputs_azure_container_apps_environment_id string

param efs_cae_outputs_azure_container_registry_endpoint string

param efs_cae_outputs_azure_container_registry_managed_identity_id string

param adds_configuration_containerimage string

param adds_configuration_identity_outputs_id string

param adds_configuration_containerport string

param adds_configuration_was_outputs_tableendpoint string

param adds_configuration_kv_outputs_vaulturi string

param adds_configuration_identity_outputs_clientid string

resource adds_configuration 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'adds-configuration'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: int(adds_configuration_containerport)
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
          image: adds_configuration_containerimage
          name: 'adds-configuration'
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
              value: adds_configuration_containerport
            }
            {
              name: 'ConnectionStrings__adds-configuration-was-ts'
              value: adds_configuration_was_outputs_tableendpoint
            }
            {
              name: 'ConnectionStrings__adds-configuration-kv'
              value: adds_configuration_kv_outputs_vaulturi
            }
            {
              name: 'adds-environment'
              value: 'local'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: adds_configuration_identity_outputs_clientid
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
      '${adds_configuration_identity_outputs_id}': { }
      '${efs_cae_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}