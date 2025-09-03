@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efs_cae_outputs_azure_container_apps_environment_id string

param efs_cae_outputs_azure_container_registry_endpoint string

param efs_cae_outputs_azure_container_registry_managed_identity_id string

param efs_service_identity_id string

param efs_service_identity_outputs_clientid string

param efs_builder_s100_containerimage string

param efs_storage_name string

@secure()
param efs_storage_connection_string string

param fss_endpoint string

param fss_endpoint_health string

param fss_client_id string

param azure_env_name string

param max_retry_attempts string

param retry_delay_ms string

resource efsbuilders100 'Microsoft.App/jobs@2025-01-01' = {
  name: 'efs-builder-s100'
  location: location
  tags: {
    'hidden-title': 'EFS'
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${efs_service_identity_id}': {}
    }
  }
  properties: {
    environmentId: efs_cae_outputs_azure_container_apps_environment_id
    workloadProfileName: 'consumption'
    configuration: {
      secrets: [
        {
          name: 'connection-string-secret'
          value: efs_storage_connection_string
        }
      ]
      triggerType: 'Event'
      replicaTimeout: 1800
      replicaRetryLimit: 0
      eventTriggerConfig: {
        replicaCompletionCount: 1
        parallelism: 1
        scale: {
          minExecutions: 0
          maxExecutions: 10
          pollingInterval: 10
          rules: [
            {
              name: 'request-queue'
              type: 'azure-queue'
              metadata: {
                accountName: efs_storage_name
                queueLength: '1'
                queueName: 's100buildrequest'
              }
              auth: []
              identity: efs_service_identity_id
            }
          ]
        }
      }
      registries: [
        {
          server: efs_cae_outputs_azure_container_registry_endpoint
          identity: efs_cae_outputs_azure_container_registry_managed_identity_id
        }
      ]
      identitySettings: []
    }
    template: {
      containers: [
        {
          image: efs_builder_s100_containerimage
          name: 'efs-builder-s100'
          env: [
            {
              name: 'FSS_ENDPOINT'
              value: fss_endpoint
            }
            {
              name: 'FSS_ENDPOINT_HEALTH'
              value: fss_endpoint_health
            }
            {
              name: 'FSS_CLIENT_ID'
              value: fss_client_id
            }
            {
              name: 'REQUEST_QUEUE_NAME'
              value: 's100buildrequest'
            }
            {
              name: 'RESPONSE_QUEUE_NAME'
              value: 's100buildresponse'
            }
            {
              name: 'QUEUE_CONNECTION_STRING'
              secretRef: 'connection-string-secret'
            }
            {
              name: 'BLOB_CONTAINER_NAME'
              value: 's100build'
            }
            {
              name: 'BLOB_CONNECTION_STRING'
              secretRef: 'connection-string-secret'
            }
            {
              name: 'ADDS_ENVIRONMENT'
              value: azure_env_name
            }
            {
              name: 'MAX_RETRY_ATTEMPTS'
              value: max_retry_attempts
            }
            {
              name: 'RETRY_DELAY_MS'
              value: retry_delay_ms
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: efs_service_identity_outputs_clientid
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
    }
  }
}
