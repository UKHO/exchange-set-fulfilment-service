variable "apim_name" {
  type = string
}

variable "apim_rg" {
  type = string
}

variable "env_name" {
  type = string
}

variable "apim_api_path" {
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

variable "apim_efs_product_name" {
  type = string
}

variable "apim_efs_product_description" {
  type = string
}

variable "product_quota" {
  type = map(any)
  default = {
    calls = 5000
    renewal_period = 86400
  }
}

variable "product_rate_limit" {
  type = map(any)
  default = {
    calls = 5
    renewal_period = 5
  }
}

variable "apim_api_name" {
  type = string
}

variable "apim_api_description" {
  type = string
}

variable "apim_api_openapi" {
  type = string
}


variable "efs_b2c_token_issuer" {
  type  = string
}

variable "efs_b2c_client_id" {
  type  = string
}
