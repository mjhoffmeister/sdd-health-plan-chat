# Azure Static Web Apps for Health Plan Chat Frontend

resource "azapi_resource" "static_web_app" {
  type      = "Microsoft.Web/staticSites@2024-04-01"
  name      = "stapp-${local.resource_prefix}-${random_string.suffix.result}"
  location  = var.location
  parent_id = azapi_resource.resource_group.id

  body = {
    sku = {
      name = "Free"
      tier = "Free"
    }
    properties = {
      stagingEnvironmentPolicy     = "Enabled"
      allowConfigFileUpdates       = true
      buildProperties = {
        appLocation         = "src/frontend/HealthPlanChat.Web"
        apiLocation         = ""
        outputLocation      = "wwwroot"
        skipGithubActionWorkflowGeneration = true
      }
    }
    tags = local.tags
  }

  response_export_values = ["properties.defaultHostname"]
}

# Configure backend API link
resource "azapi_resource" "swa_backend_link" {
  type      = "Microsoft.Web/staticSites/linkedBackends@2024-04-01"
  name      = "backend"
  parent_id = azapi_resource.static_web_app.id

  body = {
    properties = {
      backendResourceId = azapi_resource.app_service.id
      region            = var.location
    }
  }
}
