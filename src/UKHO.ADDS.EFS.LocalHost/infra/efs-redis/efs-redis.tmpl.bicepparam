using './efs-redis.module.bicep'

param efs_cae_outputs_azure_container_apps_environment_default_domain = '{{ .Env.EFS_CAE_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN }}'
param efs_cae_outputs_azure_container_apps_environment_id = '{{ .Env.EFS_CAE_AZURE_CONTAINER_APPS_ENVIRONMENT_ID }}'
param efs_redis_password_value = '{{ securedParameter "efs_redis_password" }}'
