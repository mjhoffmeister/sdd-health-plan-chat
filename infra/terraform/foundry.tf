# Azure AI Foundry / Azure AI Services for Health Plan Chat

resource "azapi_resource" "ai_services" {
  type      = "Microsoft.CognitiveServices/accounts@2024-10-01"
  name      = "aif-${local.resource_prefix}-${random_string.suffix.result}"
  location  = var.location
  parent_id = azapi_resource.resource_group.id

  identity {
    type = "SystemAssigned"
  }

  body = {
    kind = "AIServices"
    sku = {
      name = "S0"
    }
    properties = {
      customSubDomainName = "aif-${local.resource_prefix}-${random_string.suffix.result}"
      publicNetworkAccess = "Enabled"
      disableLocalAuth    = false
      apiProperties       = {}
    }
    tags = local.tags
  }

  response_export_values = ["properties.endpoint"]
}
