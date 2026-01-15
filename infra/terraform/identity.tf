# GitHub Actions Workload Identity Federation (WIF/OIDC) for Health Plan Chat

# Create Azure AD application for GitHub Actions
resource "azuread_application" "github_actions" {
  display_name = "GitHub Actions - ${var.project_name}"

  owners = [data.azuread_client_config.current.object_id]
}

# Create service principal for the application
resource "azuread_service_principal" "github_actions" {
  client_id = azuread_application.github_actions.client_id

  owners = [data.azuread_client_config.current.object_id]
}

# Federated identity credential for main branch
resource "azuread_application_federated_identity_credential" "github_main" {
  application_id = azuread_application.github_actions.id
  display_name   = "github-main-branch"
  description    = "GitHub Actions federated credential for main branch"
  audiences      = ["api://AzureADTokenExchange"]
  issuer         = "https://token.actions.githubusercontent.com"
  subject        = "repo:${var.github_repository}:ref:refs/heads/main"
}

# Federated identity credential for GitHub environment
resource "azuread_application_federated_identity_credential" "github_environment" {
  application_id = azuread_application.github_actions.id
  display_name   = "github-${var.github_environment}-environment"
  description    = "GitHub Actions federated credential for ${var.github_environment} environment"
  audiences      = ["api://AzureADTokenExchange"]
  issuer         = "https://token.actions.githubusercontent.com"
  subject        = "repo:${var.github_repository}:environment:${var.github_environment}"
}

# Federated identity credential for pull requests (optional, for PR validation)
resource "azuread_application_federated_identity_credential" "github_pr" {
  application_id = azuread_application.github_actions.id
  display_name   = "github-pull-request"
  description    = "GitHub Actions federated credential for pull requests"
  audiences      = ["api://AzureADTokenExchange"]
  issuer         = "https://token.actions.githubusercontent.com"
  subject        = "repo:${var.github_repository}:pull_request"
}

# Assign Contributor role at subscription level for infrastructure deployment
resource "azurerm_role_assignment" "github_contributor" {
  scope                = data.azurerm_subscription.current.id
  role_definition_name = "Contributor"
  principal_id         = azuread_service_principal.github_actions.object_id
}

# Assign User Access Administrator for role assignments
resource "azurerm_role_assignment" "github_user_access_admin" {
  scope                = data.azurerm_subscription.current.id
  role_definition_name = "User Access Administrator"
  principal_id         = azuread_service_principal.github_actions.object_id
}

# Storage Blob Data Contributor for state bootstrap and plan materials
resource "azurerm_role_assignment" "github_storage_contributor" {
  scope                = data.azurerm_subscription.current.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azuread_service_principal.github_actions.object_id
}
