# Azure AI Foundry Model Deployments for Health Plan Chat

# GPT-4o deployment for chat completions (supports azure_ai_search tool)
resource "azapi_resource" "gpt4o_deployment" {
  type      = "Microsoft.CognitiveServices/accounts/deployments@2024-10-01"
  name      = "gpt-4o"
  parent_id = azapi_resource.ai_services.id

  body = {
    sku = {
      name     = "GlobalStandard"
      capacity = 10
    }
    properties = {
      model = {
        format  = "OpenAI"
        name    = "gpt-4o"
        version = "2024-11-20"
      }
      raiPolicyName = "Microsoft.DefaultV2"
    }
  }

  # Deployments can take time
  timeouts {
    create = "30m"
    delete = "30m"
  }
}

# Text Embedding 3 Small deployment for embeddings
resource "azapi_resource" "embedding_deployment" {
  type      = "Microsoft.CognitiveServices/accounts/deployments@2024-10-01"
  name      = "text-embedding-3-small"
  parent_id = azapi_resource.ai_services.id

  body = {
    sku = {
      name     = "GlobalStandard"
      capacity = 10
    }
    properties = {
      model = {
        format  = "OpenAI"
        name    = "text-embedding-3-small"
        version = "1"
      }
    }
  }

  # Ensure deployments are created sequentially to avoid conflicts
  depends_on = [azapi_resource.gpt4o_deployment]

  timeouts {
    create = "30m"
    delete = "30m"
  }
}
