output "apim_product_id" {
  value = azurerm_api_management_product.efs_product.product_id
}

output "apim_api_id" {
  value = azurerm_api_management_api.efs_api.id
}
