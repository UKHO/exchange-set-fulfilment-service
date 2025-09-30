data "azurerm_resource_group" "rg" {
  name = var.apim_rg
}

data "azurerm_api_management" "apim_instance" {
  name                = var.apim_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

# Create apim group
resource "azurerm_api_management_group" "efs_management_group" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  name                = lower(replace(var.apim_group_name, " ", "-"))
  display_name        = title(var.apim_group_name)
  description         = var.apim_group_description
}

# Create EFS Product
resource "azurerm_api_management_product" "efs_product" {
  resource_group_name   = data.azurerm_resource_group.rg.name
  api_management_name   = data.azurerm_api_management.apim_instance.name
  product_id            = lower(replace(var.apim_efs_product_name, " ", "-"))
  display_name          = title(var.apim_efs_product_name)
  description           = var.apim_efs_product_description
  subscription_required = true
  approval_required     = true
  published             = true
  subscriptions_limit   = 1

}

# EFS product-Group mapping
resource "azurerm_api_management_product_group" "product_group_mappping" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  product_id          = azurerm_api_management_product.efs_product.product_id
  group_name          = azurerm_api_management_group.efs_management_group.name
}

# Create EFS API
resource "azurerm_api_management_api" "efs_api" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  name                = lower(replace(var.apim_api_name, " ", "-"))
  display_name        = var.apim_api_name
  description         = var.apim_api_description
  revision            = "1"
  path                = var.apim_api_path
  protocols           = ["https"]
  service_url         = var.apim_api_backend_url

  subscription_key_parameter_names {
    header = "Ocp-Apim-Subscription-Key"
    query  = "subscription-key"
  }

  import {
    content_format = "openapi"
    content_value  = var.apim_api_openapi
  }
}

# Add EFS API to EFS Product
resource "azurerm_api_management_product_api" "efs_product_api_mapping" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  api_name            = azurerm_api_management_api.efs_api.name
  product_id          = azurerm_api_management_product.efs_product.product_id

}

#Product quota and throttle policy
resource "azurerm_api_management_product_policy" "efs_product_policy" {
  resource_group_name = data.azurerm_resource_group.rg.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  product_id          = azurerm_api_management_product.efs_product.product_id
  depends_on          = [azurerm_api_management_product.efs_product, azurerm_api_management_product_api.efs_product_api_mapping]

  xml_content = <<XML
	<policies>
	  <inbound>
       <base />
		 <rate-limit calls="${var.product_rate_limit.calls}" renewal-period="${var.product_rate_limit.renewal-period}" retry-after-header-name="retry-after" remaining-calls-header-name="remaining-calls" />
		 <quota calls="${var.product_quota.calls}" renewal-period="${var.product_quota.renewal-period}" />

         <!-- Validate b2c token -->
         <validate-jwt header-name="Authorization" failed-validation-error-message="Authorization token is missing or invalid" require-scheme="Bearer" output-token-variable-name="jwt">
            <openid-config url="${var.efs_b2c_token_issuer}" />
            <audiences>
                <audience>${var.efs_b2c_client_id}</audience>
            </audiences>
          </validate-jwt>

	  </inbound>
	</policies>
	XML
}

