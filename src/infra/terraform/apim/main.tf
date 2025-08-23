data "azurerm_resource_group" "rg" {
  name = var.apim_rg
}

data "azurerm_api_management" "apim_instance" {
  name                = var.apim_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

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

resource "azurerm_api_management_product_api" "efs_product_api_mapping" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  api_name            = azurerm_api_management_api.efs_api.name
  product_id          = azurerm_api_management_product.efs_product.product_id
}

resource "azurerm_api_management_product_policy" "efs_product_policy" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  product_id          = azurerm_api_management_product.efs_product.product_id

  xml_content = <<XML
    <policies>
      <inbound>
        <!-- Authentication Policy -->
        <validate-jwt header-name="Authorization" failed-validation-error-message="Authorization token is missing or invalid" require-scheme="Bearer">
          <openid-config url="${var.jwt_issuer}" />
          <audiences>
            <audience>${var.jwt_audience}</audience>
          </audiences>
        </validate-jwt>
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
