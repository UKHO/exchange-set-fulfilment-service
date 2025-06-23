using './efs-builder-s100.module.bicep'

param efs_builder_s100_containerimage = '{{ .Image }}'
param efs_cae_outputs_azure_container_apps_environment_default_domain = '{{ .Env.EFS_CAE_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN }}'
param efs_cae_outputs_azure_container_apps_environment_id = '{{ .Env.EFS_CAE_AZURE_CONTAINER_APPS_ENVIRONMENT_ID }}'
param efs_cae_outputs_azure_container_registry_endpoint = '{{ .Env.EFS_CAE_AZURE_CONTAINER_REGISTRY_ENDPOINT }}'
param efs_cae_outputs_azure_container_registry_managed_identity_id = '{{ .Env.EFS_CAE_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID }}'
