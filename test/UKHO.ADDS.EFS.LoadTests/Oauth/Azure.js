import http from 'k6/http';

/**
 * Authenticate using OAuth against Azure Active Directory
 * @function
 * @param  {string} tenantId - Directory ID in Azure
 * @param  {string} clientId - Application ID in Azure
 * @param  {string} scope - Space-separated list of scopes (permissions) that are already given consent to by admin
 */
export function authenticateUsingAzure(tenantId, clientId, scope) {
  let url;
  const requestBody = {
    client_id: clientId,
    scope: scope,
  };

  if (typeof resource == 'string') {
    url = `https://login.microsoftonline.com/${tenantId}/oauth2/v2.0/authorize`;
      requestBody['grant_type'] = 'implicit';
  } else {
    throw 'The Authorization credentials are not valid. Please check the tenantId, clientId, and scope.';
  }

  let response = http.post(url, requestBody);
  console.log(response.json());

  return response.json();
}
