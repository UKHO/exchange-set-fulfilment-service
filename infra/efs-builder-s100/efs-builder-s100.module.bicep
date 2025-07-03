@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efs_cae_outputs_azure_container_apps_environment_id string

param efs_cae_outputs_azure_container_registry_endpoint string

param efs_cae_outputs_azure_container_registry_managed_identity_id string

param efs_builder_s100_containerimage string

param storage_outputs_name string

@secure()
param storage_connection_string string

resource efsbuilders100 'Microsoft.App/jobs@2025-01-01' = {
  name: 'efs-builder-s100'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${efs_cae_outputs_azure_container_registry_managed_identity_id}': {}
    }
  }
  properties: {
    environmentId: efs_cae_outputs_azure_container_apps_environment_id
    workloadProfileName: 'Consumption'
    configuration: {
      secrets: [
        {
          name: 'connection-string-secret'
          value: storage_connection_string
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
              name: 'queue'
              type: 'azure-queue'
              metadata: {
                accountName: storage_outputs_name
                queueLength: '1'
                queueName: 'queues'
              }
              auth: [
                {
                  secretRef: 'connection-string-secret'
                  triggerParameter: 'connection'
                }
              ]
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
              name: 'AZURE_STORAGE_QUEUE_NAME'
              value: 'queues'
            }
            {
              name: 'AZURE_STORAGE_CONNECTION_STRING'
              secretRef: 'connection-string-secret'
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
