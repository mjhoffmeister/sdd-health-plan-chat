# Azure AI Foundry / Azure AI Services for Health Plan Chat

resource "azapi_resource" "ai_services" {
  type      = "Microsoft.CognitiveServices/accounts@2025-09-01"
  name      = "aif-${local.resource_prefix}-${random_string.suffix.result}"
  location  = var.location
  parent_id = azapi_resource.resource_group.id

  schema_validation_enabled = false  # allowProjectManagement not in schema yet

  identity {
    type = "SystemAssigned"
  }

  body = {
    kind = "AIServices"
    sku = {
      name = "S0"
    }
    properties = {
      customSubDomainName    = "aif-${local.resource_prefix}-${random_string.suffix.result}"
      publicNetworkAccess    = "Enabled"
      disableLocalAuth       = true  # Match current Azure state
      allowProjectManagement = true  # Required for Foundry projects
      apiProperties          = {}
    }
    tags = local.tags
  }

  response_export_values = ["properties.endpoint"]
}

# Foundry Project - required for model deployments, agents, playground
resource "azapi_resource" "foundry_project" {
  type      = "Microsoft.CognitiveServices/accounts/projects@2025-04-01-preview"
  name      = "healthplanchat"
  location  = var.location
  parent_id = azapi_resource.ai_services.id

  identity {
    type = "SystemAssigned"
  }

  body = {
    properties = {
      description = "Health Plan Chat AI Project"
    }
    tags = local.tags
  }

  response_export_values = ["properties"]
}
