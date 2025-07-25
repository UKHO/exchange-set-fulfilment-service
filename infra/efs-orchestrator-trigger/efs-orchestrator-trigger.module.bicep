@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efs_cae_outputs_azure_container_apps_environment_default_domain string

param efs_cae_outputs_azure_container_apps_environment_id string

param efs_cae_outputs_azure_container_registry_endpoint string

param efs_cae_outputs_azure_container_registry_managed_identity_id string

param efs_orchestrator_trigger_containerimage string

param efs_orchestrator_trigger_containerport string

param efs_orchestrator_trigger_identity_outputs_id string

param efs_storage_outputs_blobendpoint string

param efs_storage_outputs_queueendpoint string

param efs_storage_outputs_tableendpoint string

param efs_orchestrator_trigger_identity_outputs_clientid string

resource efs_orchestrator_trigger 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'efs-orchestrator-trigger'
  location: location
  properties: {
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: int(efs_orchestrator_trigger_containerport)
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
          image: efs_orchestrator_trigger_containerimage
          name: 'efs-orchestrator-trigger'
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
              name: 'FUNCTIONS_WORKER_RUNTIME'
              value: 'dotnet-isolated'
            }
            {
              name: 'AzureFunctionsJobHost__telemetryMode'
              value: 'OpenTelemetry'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'AzureWebJobsStorage__blobServiceUri'
              value: efs_storage_outputs_blobendpoint
            }
            {
              name: 'AzureWebJobsStorage__queueServiceUri'
              value: efs_storage_outputs_queueendpoint
            }
            {
              name: 'AzureWebJobsStorage__tableServiceUri'
              value: efs_storage_outputs_tableendpoint
            }
            {
              name: 'Aspire__Azure__Storage__Blobs__AzureWebJobsStorage__ServiceUri'
              value: efs_storage_outputs_blobendpoint
            }
            {
              name: 'Aspire__Azure__Storage__Queues__AzureWebJobsStorage__ServiceUri'
              value: efs_storage_outputs_queueendpoint
            }
            {
              name: 'Aspire__Azure__Data__Tables__AzureWebJobsStorage__ServiceUri'
              value: efs_storage_outputs_tableendpoint
            }
            {
              name: 'services__efs-orchestrator__http__0'
              value: 'http://efs-orchestrator.${efs_cae_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'services__efs-orchestrator__https__0'
              value: 'https://efs-orchestrator.${efs_cae_outputs_azure_container_apps_environment_default_domain}'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: efs_orchestrator_trigger_identity_outputs_clientid
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
      '${efs_orchestrator_trigger_identity_outputs_id}': { }
      '${efs_cae_outputs_azure_container_registry_managed_identity_id}': { }
    }
  }
}
