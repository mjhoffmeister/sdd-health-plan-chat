# Main resource group for Health Plan Chat

# Get current subscription
data "azurerm_subscription" "current" {}

# Get current Azure AD config
data "azuread_client_config" "current" {}

# Resource Group
resource "azapi_resource" "resource_group" {
  type     = "Microsoft.Resources/resourceGroups@2024-03-01"
  name     = "rg-${local.resource_prefix}"
  location = var.location

  body = {
    tags = local.tags
  }
}

# Random suffix for globally unique names
resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}
