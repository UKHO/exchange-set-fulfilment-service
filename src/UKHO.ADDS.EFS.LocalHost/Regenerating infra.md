# Recreating IaC

The Bicep files used for deployment were initially created by running `azd infra gen`. They were then manually edited.

If we need to regenerate from scratch again then you can run the `azd regenerate.cmd` script in the repo root. Afterwards the following updates need to be done:

1. `efs-cae.module.bicep` Ensure that the `$` symbols are not escaped for the `infrastructureSubnetId` value.
2. `adds-mocks-efs.module.bicep` Remove the `efs_cae_outputs_azure_container_apps_environment_default_domain` parameter as it's not used.
