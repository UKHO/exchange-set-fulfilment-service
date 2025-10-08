import http from 'k6/http';
import { check } from 'k6';

// Track token expiration (most Azure tokens expire in 1 hour)
const TOKEN_LIFETIME_MS = 50 * 60 * 1000; // 50 minutes to be safe
let tokenInfo = {
  token: null,
  expiresAt: 0
};

/**
* Gets an authentication token for API requests with automatic refresh
* @param {Object} config - Authentication configuration
* @param {boolean} forceRefresh - Force token refresh regardless of expiration
* @returns {string} - The auth token
*/
export function authenticateUsingAzure(config, forceRefresh = false) {
  const now = Date.now();

  if (!forceRefresh && tokenInfo.token && now < tokenInfo.expiresAt) {
    return tokenInfo.token;
  }

  const url = `https://login.microsoftonline.com/${config.EFS_TENANT_ID}/oauth2/v2.0/token`;

  const payload = {
    client_id: config.EFS_CLIENT_ID,
    client_secret: config.EFS_CLIENT_SECRET,
    grant_type: 'client_credentials',
    scope: config.EFS_SCOPES,
  };

  const response = http.post(url, payload, params);
  const success = check(response, {
    'Authentication successful': (r) => r.status === 200,
    'Token received': (r) => r.json().access_token !== undefined
  });

  if (success) {
    tokenInfo.token = response.json().access_token;
    tokenInfo.expiresAt = now + TOKEN_LIFETIME_MS;
    return tokenInfo.token;
  } else {
    return null;
  }
}