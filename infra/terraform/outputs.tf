# Outputs for Health Plan Chat Infrastructure

output "resource_group_name" {
  description = "Name of the resource group"
  value       = azapi_resource.resource_group.name
}

output "app_service_url" {
  description = "URL of the backend App Service"
  value       = "https://${azapi_resource.app_service.name}.azurewebsites.net"
}

output "static_web_app_url" {
  description = "URL of the frontend Static Web App"
  value       = jsondecode(azapi_resource.static_web_app.output).properties.defaultHostname
}

output "search_endpoint" {
  description = "Azure AI Search endpoint"
  value       = "https://${azapi_resource.search_service.name}.search.windows.net"
}

output "storage_blob_endpoint" {
  description = "Storage account blob endpoint"
  value       = "https://${azapi_resource.storage_account.name}.blob.core.windows.net"
}

output "redis_hostname" {
  description = "Azure Managed Redis hostname"
  value       = jsondecode(azapi_resource.redis_enterprise.output).properties.hostName
  sensitive   = true
}

output "foundry_endpoint" {
  description = "Azure AI Foundry endpoint"
  value       = jsondecode(azapi_resource.ai_services.output).properties.endpoint
}

# GitHub Actions WIF outputs
output "azure_tenant_id" {
  description = "Azure tenant ID for GitHub Actions WIF"
  value       = data.azuread_client_config.current.tenant_id
}

output "azure_subscription_id" {
  description = "Azure subscription ID for GitHub Actions WIF"
  value       = data.azurerm_subscription.current.subscription_id
}

output "azure_client_id" {
  description = "Azure client ID for GitHub Actions WIF"
  value       = azuread_application.github_actions.client_id
}

# Resource names for GitHub Environment variables
output "app_service_name" {
  description = "App Service name for deployment"
  value       = azapi_resource.app_service.name
}

output "storage_account_name" {
  description = "Storage account name for blob sync"
  value       = azapi_resource.storage_account.name
}

output "static_web_app_name" {
  description = "Static Web App name for deployment"
  value       = azapi_resource.static_web_app.name
}

output "static_web_app_hostname" {
  description = "Static Web App default hostname"
  value       = jsondecode(azapi_resource.static_web_app.output).properties.defaultHostname
}
