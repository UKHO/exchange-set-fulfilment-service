@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efs_cae_outputs_azure_container_apps_environment_default_domain string

param efs_cae_outputs_azure_container_apps_environment_id string

param efs_cae_outputs_azure_container_registry_endpoint string

param efs_cae_outputs_azure_container_registry_managed_identity_id string

param efs_orchestrator_containerimage string

param efs_orchestrator_identity_outputs_id string

param efs_orchestrator_containerport string

param efssa_outputs_queueendpoint string

param efssa_outputs_tableendpoint string

param efssa_outputs_blobendpoint string

param efs_orchestrator_identity_outputs_clientid string

resource efs_orchestrator 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'efs-orchestrator'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: int(efs_orchestrator_containerport)
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
          image: efs_orchestrator_containerimage
          name: 'efs-orchestrator'
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
              value: efs_orchestrator_containerport
            }
            {
              name: 'ConnectionStrings__queues'
              value: efssa_outputs_queueendpoint
            }
            {
              name: 'ConnectionStrings__tables'
              value: efssa_outputs_tableendpoint
            }
            {
              name: 'ConnectionStrings__blobs'
              value: efssa_outputs_blobendpoint
            }
            {
              name: 'services__adds-mocks-efs__http__0'
              value: 'http://adds-mocks-efs.${efs_cae_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'services__adds-mocks-efs__https__0'
              value: 'https://adds-mocks-efs.${efs_cae_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'services__adds-configuration__http__0'
              value: 'http://adds-configuration.${efs_cae_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'services__adds-configuration__https__0'
              value: 'https://adds-configuration.${efs_cae_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'adds-environment'
              value: 'local'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: efs_orchestrator_identity_outputs_clientid
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
      '${efs_orchestrator_identity_outputs_id}': { }
      '${efs_cae_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}