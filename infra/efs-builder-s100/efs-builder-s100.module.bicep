@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efs_cae_outputs_azure_container_apps_environment_id string

param efs_cae_outputs_azure_container_registry_endpoint string

param efs_cae_outputs_azure_container_registry_managed_identity_id string

param efs_builder_s100_containerimage string

resource efsbuilders100 'Microsoft.App/jobs@2025-01-01' = {
  name: 'efs-builder-s100'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '/subscriptions/ac1ac25d-6c09-4d38-bbae-ffc1cd3c5ebf/resourcegroups/hb-job-test-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/hb-efs-job-test-mi2': {}
    }
  }
  properties: {
    environmentId: efs_cae_outputs_azure_container_apps_environment_id
    workloadProfileName: 'Consumption'
    configuration: {
      secrets: [
        {
          name: 'connection-string-secret'
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
          pollingInterval: 30
          rules: [
            {
              name: 'queue'
              type: 'azure-queue'
              metadata: {
                accountName: 'hbefsjobtestsa'
                queueLength: '1'
                queueName: 'myqueue'
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
              value: 'myqueue'
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
