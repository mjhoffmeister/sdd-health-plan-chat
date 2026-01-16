# Azure App Service + Plan for Health Plan Chat Backend

# App Service Plan (Linux)
resource "azapi_resource" "app_service_plan" {
  type      = "Microsoft.Web/serverfarms@2024-04-01"
  name      = "asp-${local.resource_prefix}"
  location  = var.location
  parent_id = azapi_resource.resource_group.id

  body = {
    kind = "linux"
    sku = {
      name = "F1"
      tier = "Free"
    }
    properties = {
      reserved = true # Required for Linux
    }
    tags = local.tags
  }
}

# App Service (Web App)
resource "azapi_resource" "app_service" {
  type      = "Microsoft.Web/sites@2024-04-01"
  name      = "app-${local.resource_prefix}-${random_string.suffix.result}"
  location  = var.location
  parent_id = azapi_resource.resource_group.id

  identity {
    type = "SystemAssigned"
  }

  body = {
    kind = "app,linux"
    properties = {
      serverFarmId = azapi_resource.app_service_plan.id
      httpsOnly    = true
      siteConfig = {
        linuxFxVersion = "DOTNETCORE|10.0"
        alwaysOn       = false # Basic tier doesn't support alwaysOn
        http20Enabled  = true
        minTlsVersion  = "1.2"
        ftpsState      = "Disabled"
        appSettings = [
          {
            name  = "ASPNETCORE_ENVIRONMENT"
            value = var.environment == "demo" ? "Development" : "Production"
          },
          {
            name  = "Search__Endpoint"
            value = "https://${azapi_resource.search_service.name}.search.windows.net"
          },
          {
            name  = "Search__IndexName"
            value = "plan-materials"
          },
          {
            name  = "Storage__BlobServiceUrl"
            value = "https://${azapi_resource.storage_account.name}.blob.core.windows.net"
          },
          {
            name  = "Storage__ContainerName"
            value = "plan-materials"
          },
          {
            name  = "Foundry__Endpoint"
            value = azapi_resource.ai_services.output.properties.endpoint
          },
          {
            name  = "Foundry__ChatModelDeployment"
            value = "gpt-5-mini"
          },
          {
            name  = "Foundry__EmbeddingModelDeployment"
            value = "text-embedding-3-small"
          },
          {
            name  = "Redis__Endpoint"
            value = "${azapi_resource.redis_cluster.output.properties.hostName}:10000"
          }
        ]
      }
    }
    tags = local.tags
  }

  response_export_values = ["properties.defaultHostName"]
}

# Role assignments for App Service managed identity

# Search Index Data Reader
resource "azapi_resource" "app_search_role" {
  type      = "Microsoft.Authorization/roleAssignments@2022-04-01"
  name      = uuidv5("dns", "${azapi_resource.app_service.id}-search-reader")
  parent_id = azapi_resource.search_service.id

  body = {
    properties = {
      roleDefinitionId = "/subscriptions/${data.azurerm_subscription.current.subscription_id}/providers/Microsoft.Authorization/roleDefinitions/1407120a-92aa-4202-b7e9-c0e197c71c8f" # Search Index Data Reader
      principalId      = azapi_resource.app_service.identity[0].principal_id
      principalType    = "ServicePrincipal"
    }
  }
}

# Cognitive Services User (for AI Foundry)
resource "azapi_resource" "app_foundry_role" {
  type      = "Microsoft.Authorization/roleAssignments@2022-04-01"
  name      = uuidv5("dns", "${azapi_resource.app_service.id}-foundry-user")
  parent_id = azapi_resource.ai_services.id

  body = {
    properties = {
      roleDefinitionId = "/subscriptions/${data.azurerm_subscription.current.subscription_id}/providers/Microsoft.Authorization/roleDefinitions/a97b65f3-24c7-4388-baec-2e87135dc908" # Cognitive Services User
      principalId      = azapi_resource.app_service.identity[0].principal_id
      principalType    = "ServicePrincipal"
    }
  }
}
