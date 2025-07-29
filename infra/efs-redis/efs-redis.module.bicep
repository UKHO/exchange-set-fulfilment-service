@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efs_cae_outputs_azure_container_apps_environment_default_domain string

param efs_cae_outputs_azure_container_apps_environment_id string

@secure()
param efs_redis_password_value string

resource efs_redis 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'efs-redis'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'redis-password'
          value: efs_redis_password_value
        }
      ]
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 6379
        transport: 'tcp'
      }
    }
    environmentId: efs_cae_outputs_azure_container_apps_environment_id
    template: {
      containers: [
        {
          image: 'docker.io/library/redis:7.4'
          name: 'efs-redis'
          command: [
            '/bin/sh'
          ]
          args: [
            '-c'
            'redis-server --requirepass \$REDIS_PASSWORD'
          ]
          env: [
            {
              name: 'REDIS_PASSWORD'
              secretRef: 'redis-password'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
      }
    }
  }
}