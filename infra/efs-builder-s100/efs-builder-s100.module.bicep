@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource efsbuilders100 'Microsoft.App/jobs@2024-03-01' = {
  name: take('efsbuilders${uniqueString(resourceGroup().id)}', 24)
  location: location
}

output AZURE_BUILDER_CONTAINER_APP_JOB_NAME string = efsbuilders100.name