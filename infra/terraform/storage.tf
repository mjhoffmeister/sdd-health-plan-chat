# Azure Storage Account for Health Plan Chat

resource "azapi_resource" "storage_account" {
  type      = "Microsoft.Storage/storageAccounts@2023-05-01"
  name      = "sthpc${var.environment}${random_string.suffix.result}"
  location  = var.location
  parent_id = azapi_resource.resource_group.id

  identity {
    type = "SystemAssigned"
  }

  body = {
    kind = "StorageV2"
    sku = {
      name = "Standard_LRS"
    }
    properties = {
      accessTier                   = "Hot"
      allowBlobPublicAccess        = false
      allowSharedKeyAccess         = false
      supportsHttpsTrafficOnly     = true
      minimumTlsVersion            = "TLS1_2"
      defaultToOAuthAuthentication = true
    }
    tags = local.tags
  }

  response_export_values = ["properties.primaryEndpoints"]
}

# Blob container for plan materials
resource "azapi_resource" "storage_container" {
  type      = "Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01"
  name      = "plan-materials"
  parent_id = "${azapi_resource.storage_account.id}/blobServices/default"

  body = {
    properties = {
      publicAccess = "None"
    }
  }
}

# Allow Search Service to read from Storage (for indexer)
resource "azapi_resource" "search_storage_role" {
  type      = "Microsoft.Authorization/roleAssignments@2022-04-01"
  name      = uuidv5("dns", "${azapi_resource.search_service.id}-storage-reader")
  parent_id = azapi_resource.storage_account.id

  body = {
    properties = {
      roleDefinitionId = "/subscriptions/${data.azurerm_subscription.current.subscription_id}/providers/Microsoft.Authorization/roleDefinitions/2a2b9908-6ea1-4ae2-8e65-a410df84e7d1" # Storage Blob Data Reader
      principalId      = azapi_resource.search_service.identity[0].principal_id
      principalType    = "ServicePrincipal"
    }
  }
}

# Allow developer to manage blobs in Storage
resource "azapi_resource" "developer_storage_role" {
  count     = var.developer_principal_id != "" ? 1 : 0
  type      = "Microsoft.Authorization/roleAssignments@2022-04-01"
  name      = uuidv5("dns", "${var.developer_principal_id}-storage-blob-contributor")
  parent_id = azapi_resource.storage_account.id

  body = {
    properties = {
      roleDefinitionId = "/subscriptions/${data.azurerm_subscription.current.subscription_id}/providers/Microsoft.Authorization/roleDefinitions/ba92f5b4-2d11-453d-a403-e96b0029c9fe" # Storage Blob Data Contributor
      principalId      = var.developer_principal_id
      principalType    = "User"
    }
  }
}
