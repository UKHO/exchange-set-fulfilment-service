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

resource "azurerm_api_management_api_policy" "efs_api_policy" {
  api_name            = azurerm_api_management_api.efs_api.name
  api_management_name = data.azurerm_api_management.apim_instance.name
  resource_group_name = data.azurerm_resource_group.rg.name

  xml_content = <<XML
<policies>
  <outbound>
    <set-header name="X-Error-Origin-Service" exists-action="delete" />
    <set-header name="X-Error-Origin-Status" exists-action="delete" />
    <base />
  </outbound>
</policies>
XML
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
		 <rate-limit calls="${var.product_rate_limit.calls}" renewal-period="${var.product_rate_limit.renewal-period}" retry-after-header-name="retry-after" remaining-calls-header-name="remaining-calls" />
		 <quota calls="${var.product_quota.calls}" renewal-period="${var.product_quota.renewal-period}" />

         <!-- Validate b2c token -->
         <validate-jwt header-name="Authorization" failed-validation-error-message="Authorization token is missing or invalid" require-scheme="Bearer" output-token-variable-name="jwt">
            <openid-config url="${var.efs_b2c_token_issuer}" />
            <audiences>
                <audience>${var.efs_b2c_client_id}</audience>
            </audiences>
          </validate-jwt>

		 <base />
	  </inbound>
	</policies>
	XML
}

# Create policy for generating distributor access token
resource "azurerm_api_management_api_operation_policy" "client_credentials_token_operation_policy" {
    resource_group_name = data.azurerm_resource_group.rg.name
    api_management_name = data.azurerm_api_management.apim_instance.name
    api_name            = azurerm_api_management_api.efs_api.name  
    operation_id        = var.client_credentials_operation_id

    xml_content = <<XML
     <policies>
        <inbound>    
            <base/>
            <!-- Retrieve values from request body -->
            <set-variable name="ClientId" value="@(context.Request.Body?.As<JObject>(preserveContent: true)["client_id"]?.ToString())" />
            <set-variable name="ClientSecret" value="@(context.Request.Body?.As<JObject>(preserveContent: true)["client_secret"]?.ToString())" />
            <set-header name="X-Correlation-ID" exists-action="skip">
                <value>@(Guid.NewGuid().ToString())</value>
            </set-header>
            <!-- Validate the required fields -->
            <choose>
                <when condition="@(string.IsNullOrWhiteSpace(context.Variables.GetValueOrDefault<string>("ClientId")) ||
                                    string.IsNullOrWhiteSpace(context.Variables.GetValueOrDefault<string>("ClientSecret")))">
                    <return-response>
                        <set-status code="400" reason="Bad Request" />
                        <set-header name="Content-Type" exists-action="override">
                            <value>application/json</value>
                        </set-header>
                        <set-header name="X-Correlation-ID">
                            <value>@(context.Request.Headers["X-Correlation-ID"][0])</value>
                        </set-header>
                        <set-body template="liquid">{
                                "correlationId": "{{context.Request.Headers["X-Correlation-ID"]}}",
                                "errors": [
                                            {
                                                "source": "Request",
                                                "description": "Request missing client_id and/or client_secret"
                                            }
                                        ]
                                    }
                            </set-body>
                    </return-response>
                </when>
            </choose>
            <!-- Send request to generate-token url with required values -->
            <send-request mode="new" response-variable-name="tokenResponse" timeout="60" ignore-error="true">            
                <set-url>https://login.microsoftonline.com/${var.client_credentials_tenant_id}/oauth2/v2.0/token</set-url>
                <set-method>POST</set-method>
                <set-header name="Content-Type" exists-action="override">
                    <value>application/x-www-form-urlencoded</value>
                </set-header>
                <set-body>@{
                    return $"client_id={context.Variables.GetValueOrDefault<string>("ClientId")}&client_secret={context.Variables.GetValueOrDefault<string>("ClientSecret")}&grant_type=client_credentials&scope=${var.client_credentials_scope}";
                   }
                </set-body>
            </send-request>
            <choose>
                <when condition="@(((IResponse)context.Variables["tokenResponse"]).StatusCode == 200)">
                    <return-response>
                        <set-status code="@(((IResponse)context.Variables.GetValueOrDefault<IResponse>("tokenResponse")).StatusCode)" 
                        reason="@(((IResponse)context.Variables.GetValueOrDefault<IResponse>("tokenResponse")).StatusReason)" />
                        <set-header name="Content-Type" exists-action="override">
                            <value>application/json</value>
                        </set-header>
                        <set-header name="X-Correlation-ID">
                            <value>@(context.Request.Headers["X-Correlation-ID"][0])</value>
                        </set-header>
                        <set-body template="none">@{
                            var body = ((IResponse)context.Variables["tokenResponse"]).Body.As<JObject>();
                            return body.ToString();
                        }</set-body>
                    </return-response>                    
                </when>
                <otherwise>
                    <set-variable name="source" value="@{ 
                        return ((IResponse)context.Variables["tokenResponse"]).Body?.As<JObject>(true)["error"]?.ToString();
                        }" />
                    <set-variable name="errorMessage" value="@{ 
                        return ((IResponse)context.Variables["tokenResponse"]).Body?.As<JObject>()["error_description"]?.ToString();
                    }" />
                    <!-- Retrieve only the error description and exclude trace id, time stamp, etc. -->
                    <set-variable name="errorDescription" value="@{ 
                        return ((string)context.Variables["errorMessage"])?.Substring(0, ((string)context.Variables["errorMessage"]).IndexOf("\r"));
                    }" />
                    <return-response>
                        <set-status code="@(((IResponse)context.Variables.GetValueOrDefault<IResponse>("tokenResponse")).StatusCode)" 
                        reason="@(((IResponse)context.Variables.GetValueOrDefault<IResponse>("tokenResponse")).StatusReason)" />
                        
                        <set-header name="Content-Type" exists-action="override">
                            <value>application/json</value>
                        </set-header>
                        
                        <set-header name="X-Correlation-ID">
                            <value>@(context.Request.Headers["X-Correlation-ID"][0])</value>
                        </set-header>
                        
                        <set-body template="liquid">{
                                "correlationId": "{{context.Request.Headers["X-Correlation-ID"]}}",
                                "errors": [
                                            {
                                                "source": "{{context.Variables["source"]}}",
											    "description": "{{context.Variables["errorDescription"]}}"
                                            }
                                        ]
                                    }
                            </set-body>
                    </return-response>                    
                </otherwise>
            </choose>
        </inbound>        
    </policies>
    XML
}                      
