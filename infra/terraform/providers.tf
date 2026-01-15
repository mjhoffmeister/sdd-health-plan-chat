# Terraform Providers Configuration for Health Plan Chat
# Uses AzAPI provider (pinned to 2.8.0) for Azure resources

terraform {
  required_version = ">= 1.9.0"

  required_providers {
    azapi = {
      source  = "Azure/azapi"
      version = "2.8.0"
    }
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # Remote state backend - concrete settings passed via -backend-config from workflow
  backend "azurerm" {}
}

provider "azapi" {}

provider "azurerm" {
  features {}
}

provider "azuread" {}

provider "random" {}
