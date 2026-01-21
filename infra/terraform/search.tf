# Azure AI Search for Health Plan Chat

resource "azapi_resource" "search_service" {
  type      = "Microsoft.Search/searchServices@2025-05-01"
  name      = "srch-${local.resource_prefix}-${random_string.suffix.result}"
  location  = var.location
  parent_id = azapi_resource.resource_group.id

  identity {
    type = "SystemAssigned"
  }

  body = {
    sku = {
      name = "basic"
    }
    properties = {
      replicaCount        = 1
      partitionCount      = 1
      hostingMode         = "default"
      publicNetworkAccess  = "Enabled"
      authOptions = {
        aadOrApiKey = {
          aadAuthFailureMode = "http403"
        }
      }
      semanticSearch = "free"
    }
    tags = local.tags
  }

  response_export_values = ["properties.hostName", "identity.principalId"]
}

# Search Service managed identity role assignments
# Note: Storage Blob Data Reader role is defined in storage.tf

# Cognitive Services User - for calling embedding API via skillset
resource "azapi_resource" "search_foundry_role" {
  type      = "Microsoft.Authorization/roleAssignments@2022-04-01"
  name      = uuidv5("dns", "${azapi_resource.search_service.id}-foundry-user")
  parent_id = azapi_resource.ai_services.id

  body = {
    properties = {
      roleDefinitionId = "/subscriptions/${data.azurerm_subscription.current.subscription_id}/providers/Microsoft.Authorization/roleDefinitions/a97b65f3-24c7-4388-baec-2e87135dc908" # Cognitive Services User
      principalId      = azapi_resource.search_service.identity[0].principal_id
      principalType    = "ServicePrincipal"
    }
  }
}

# Foundry project identity needs Search Index Data Reader for azure_ai_search tool
resource "azapi_resource" "foundry_search_data_reader" {
  type      = "Microsoft.Authorization/roleAssignments@2022-04-01"
  name      = uuidv5("dns", "${azapi_resource.search_service.id}-foundry-project-reader")
  parent_id = azapi_resource.search_service.id

  body = {
    properties = {
      roleDefinitionId = "/subscriptions/${data.azurerm_subscription.current.subscription_id}/providers/Microsoft.Authorization/roleDefinitions/1407120a-92aa-4202-b7e9-c0e197c71c8f" # Search Index Data Reader
      principalId      = azapi_resource.foundry_project.identity[0].principal_id
      principalType    = "ServicePrincipal"
    }
  }

  depends_on = [azapi_resource.foundry_project]
}

# Foundry project identity needs Search Service Contributor for azure_ai_search tool
resource "azapi_resource" "foundry_search_contributor" {
  type      = "Microsoft.Authorization/roleAssignments@2022-04-01"
  name      = uuidv5("dns", "${azapi_resource.search_service.id}-foundry-project-contributor")
  parent_id = azapi_resource.search_service.id

  body = {
    properties = {
      roleDefinitionId = "/subscriptions/${data.azurerm_subscription.current.subscription_id}/providers/Microsoft.Authorization/roleDefinitions/7ca78c08-252a-4471-8644-bb5ff32d4ba0" # Search Service Contributor
      principalId      = azapi_resource.foundry_project.identity[0].principal_id
      principalType    = "ServicePrincipal"
    }
  }

  depends_on = [azapi_resource.foundry_project]
}

# NOTE: Search index, datasource, skillset, and indexer must be created via Azure CLI
# or Search REST API after infrastructure is deployed. ARM doesn't support these
# data-plane resources. Use scripts/setup-search-index.ps1 after terraform apply.
#
# TODO: Create scripts/setup-search-index.ps1 to set up:
# - Index: plan-materials (with vector search + semantic config)
# - Data source: plan-materials-blob (connect to storage)
# - Skillset: plan-materials-skillset (split + embedding)
# - Indexer: plan-materials-indexer (scheduled every 5 min)
