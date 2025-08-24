variable "apim_name" {
  type = string
}

variable "env_name" {
  type = string
}

variable "apim_api_path" {
  type = string
}

variable "apim_ui_api_path" {
  type = string
}

variable "apim_api_backend_url" {
  type        = string
  description = "The URL of the backend service serving the API."
}

variable "apim_group_name" {
  type = string
}

variable "apim_group_description" {
  type = string
}

variable "apim_ess_product_name" {
  type = string
}

variable "apim_ess_product_description" {
  type = string
}

variable "product_rate_limit" {
  type = map(any)
}

variable "product_quota" {
  type = map(any)
}

variable "apim_api_name" {
  type = string
}

variable "apim_api_description" {
  type = string
}

variable "apim_ui_api_name" {
  type = string
}

variable "apim_ui_api_description" {
  type = string
}

variable "apim_api_openapi" {
  type = string
}

variable "apim_ui_openapi" {
  type = string
}

variable "client_credentials_operation_id" {
  type    = string  
}

variable "client_credentials_tenant_id" {
  type    = string  
}

variable "client_credentials_scope" {
  type    = string  
}

variable "apim_ess_ui_product_name" {
    type = string
    default = "Exchange Set Service UI"
}

variable "ess_b2c_token_issuer" {
  type  = string
}

variable "ess_b2c_client_id" {
  type  = string
}

variable "cors_origins" {
  type = list(string)
}

variable "ess_ui_product_call_limit" {
    type = number
}

variable "ess_ui_product_call_renewal_period" {
    type = number
}

variable "ess_ui_product_daily_quota_limit" {
    type = number
}

variable "apim_ess_monitor_product_name" {
  type  = string
}

variable "apim_ess_monitor_product_description" {
  type  = string
}

variable "apim_monitor_api_name" {
  type  = string
}

variable "apim_monitor_api_description" {
  type  = string
}

variable "apim_monitor_api_openapi" {
  type  = string
}

variable "apim_monitor_api_path" {
  type  = string
}

variable "environment" {
  description = "The deployment environment (dev, vnextiat, vnexte2e, iat)"
  type        = string
}

variable "apim_rg" {}
variable "apim_name" {}
variable "efs_api_backend_url" {}
variable "jwt_issuer" {}
variable "jwt_audience" {}

# JWT issuer and audience for Azure B2C (external users)
variable "b2c_jwt_issuer" {
  type = string
  description = "JWT issuer URL for Azure B2C authentication."
}

variable "b2c_jwt_audience" {
  type = string
  description = "JWT audience for Azure B2C authentication."
}

# JWT issuer and audience for Entra ID (internal users)
variable "entra_jwt_issuer" {
  type = string
  description = "JWT issuer URL for Entra ID authentication."
}

variable "entra_jwt_audience" {
  type = string
  description = "JWT audience for Entra ID authentication."
}
