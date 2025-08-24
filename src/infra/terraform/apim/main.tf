# Look up the resource group where APIM is deployed
# This allows us to reference an existing resource group in Azure
# Variable: apim_rg is set in the tfvars file for each environment
data "azurerm_resource_group" "rg" {
  name = var.apim_rg
}

# Look up the existing APIM instance by name and resource group
# Variable: apim_name is set in the tfvars file for each environment
data "azurerm_api_management" "apim_instance" {
  name                = var.apim_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

# Create a new APIM Product for the Exchange Set Service
# This product will be used to group and manage access to the API
resource "azurerm_api_management_product" "efs_product" {
  resource_group_name   = data.azurerm_resource_group.rg.name
  api_management_name   = data.azurerm_api_management.apim_instance.name
  product_id            = "exchange-set-service"
  display_name          = "Exchange Set Service"
  description           = "Product for Exchange Set Service APIs"
  subscription_required = true
  approval_required     = true
  published             = true
  subscriptions_limit   = 1
}

# Create a new API in APIM using the OpenAPI specification
# The API exposes 3 endpoints as defined in openapispec.yml
# The backend URL is set via the efs_api_backend_url variable
resource "azurerm_api_management_api" "efs_api" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  name                = "exchange-set-service-api"
  display_name        = "Exchange Set Service API"
  description         = "API for Exchange Set Service"
  revision            = "1"
  path                = "v2/exchangeSet/s100"
  protocols           = ["https"]
  service_url         = var.efs_api_backend_url

  import {
    content_format = "openapi"
    content_value  = file("${path.module}/openapispec.yml")
  }
}

# Map the new API to the product so users can subscribe and access the API
resource "azurerm_api_management_product_api" "efs_product_api_mapping" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  api_name            = azurerm_api_management_api.efs_api.name
  product_id          = azurerm_api_management_product.efs_product.product_id
}

# Apply APIM policies to the product
# - JWT authentication: requires a valid token for access
# - Rate limiting: restricts calls per minute
# - Quota: restricts total calls per day
resource "azurerm_api_management_product_policy" "efs_product_policy" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  product_id          = azurerm_api_management_product.efs_product.product_id

  xml_content = <<XML
    <policies>
      <inbound>
        <!-- Conditional Authentication Policy for B2C (external) and Entra ID (internal) -->
        <choose>
          <!-- If the JWT issuer claim matches B2C, validate with B2C settings -->
          <when condition="@(context.Request.Headers.GetValueOrDefault('Authorization','').Contains('b2c'))">
            <validate-jwt header-name="Authorization" failed-validation-error-message="Authorization token is missing or invalid" require-scheme="Bearer">
              <openid-config url="${var.b2c_jwt_issuer}" />
              <audiences>
                <audience>${var.b2c_jwt_audience}</audience>
              </audiences>
            </validate-jwt>
          </when>
          <!-- Otherwise, validate with Entra ID settings -->
          <otherwise>
            <validate-jwt header-name="Authorization" failed-validation-error-message="Authorization token is missing or invalid" require-scheme="Bearer">
              <openid-config url="${var.entra_jwt_issuer}" />
              <audiences>
                <audience>${var.entra_jwt_audience}</audience>
              </audiences>
            </validate-jwt>
          </otherwise>
        </choose>
        <base />
      </inbound>
      <outbound>
        <!-- Throttling and Quota Policies -->
        <rate-limit calls="10" renewal-period="60" />
        <quota calls="1000" renewal-period="86400" />
        <base />
      </outbound>
    </policies>
  XML
}
