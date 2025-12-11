@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param efs_cae_outputs_azure_container_apps_environment_default_domain string

param efs_cae_outputs_azure_container_apps_environment_id string

param efs_cae_outputs_azure_container_registry_endpoint string

param efs_cae_outputs_azure_container_registry_managed_identity_id string

param efs_orchestrator_containerimage string

param efs_service_identity_outputs_id string

param efs_orchestrator_containerport string

param efs_storage_outputs_queueendpoint string

param efs_storage_outputs_tableendpoint string

param efs_storage_outputs_blobendpoint string

@secure()
param efs_redis_password_value string

param efs_app_insights_outputs_appinsightsconnectionstring string

param efs_events_namespace_outputs_eventhubsendpoint string

@secure()
param elasticapmapikey_value string

param elasticapmserverurl_value string

param elasticapmservicename_value string

param elasticapmenvironment_value string

param efsappregclientid_value string

param efsappregtenantid_value string

param efsb2cappclientid_value string

param efsb2capptenantid_value string

param efsb2cappdomain_value string

param efsb2cappinstance_value string

param efsb2cappsigninpolicy_value string

param efs_appconfig_outputs_appconfigendpoint string

param addsenvironment_value string

param efs_service_identity_outputs_clientid string

param orchestratorCpu string

param orchestratorMemory string

resource efs_orchestrator 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'efs-orchestrator'
  location: location
  properties: {
    configuration: {
      secrets: [
        {
          name: 'connectionstrings--efs-redis'
          value: 'efs-redis:6379,password=${efs_redis_password_value}'
        }
        {
          name: 'elasticapm--apikey'
          value: elasticapmapikey_value
        }
      ]
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
      runtime: {
        dotnet: {
          autoConfigureDataProtection: true
        }
      }
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
              name: 'ConnectionStrings__efs-queues'
              value: efs_storage_outputs_queueendpoint
            }
            {
              name: 'ConnectionStrings__efs-tables'
              value: efs_storage_outputs_tableendpoint
            }
            {
              name: 'ConnectionStrings__efs-blobs'
              value: efs_storage_outputs_blobendpoint
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
              name: 'ConnectionStrings__efs-redis'
              secretRef: 'connectionstrings--efs-redis'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: efs_app_insights_outputs_appinsightsconnectionstring
            }
            {
              name: 'ConnectionStrings__efs-events-namespace'
              value: efs_events_namespace_outputs_eventhubsendpoint
            }
            {
              name: 'ElasticAPM__ApiKey'
              secretRef: 'elasticapm--apikey'
            }
            {
              name: 'ElasticAPM__ServerURL'
              value: elasticapmserverurl_value
            }
            {
              name: 'ElasticAPM__ServiceName'
              value: elasticapmservicename_value
            }
            {
              name: 'ElasticAPM__Environment'
              value: elasticapmenvironment_value
            }
            {
              name: 'EFS_APP_REG_CLIENT_ID'
              value: efsappregclientid_value
            }
            {
              name: 'EFS_APP_REG_TENANT_ID'
              value: efsappregtenantid_value
            }
            {
              name: 'EFS_B2C_APP_CLIENT_ID'
              value: efsb2cappclientid_value
            }
            {
              name: 'EFS_B2C_APP_TENANT_ID'
              value: efsb2capptenantid_value
            }
            {
              name: 'EFS_B2C_APP_DOMAIN'
              value: efsb2cappdomain_value
            }
            {
              name: 'EFS_B2C_APP_INSTANCE'
              value: efsb2cappinstance_value
            }
            {
              name: 'EFS_B2C_APP_SIGNIN_POLICY'
              value: efsb2cappsigninpolicy_value
            }
            {
              name: 'ConnectionStrings__efs-appconfig'
              value: efs_appconfig_outputs_appconfigendpoint
            }
            {
              name: 'adds-environment'
              value: addsenvironment_value
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: efs_service_identity_outputs_clientid
            }
          ]
          resources: {
            cpu: json(orchestratorCpu)
            memory: orchestratorMemory
          }
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
      '${efs_service_identity_outputs_id}': { }
    }
  }
  tags: {
    'hidden-title': 'EFS'
  }
}
