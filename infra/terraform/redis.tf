# Azure Managed Redis (Redis Enterprise) for Health Plan Chat
# Uses Microsoft Entra Authentication (managed identity) - no access keys required.

resource "azapi_resource" "redis_enterprise" {
  type      = "Microsoft.Cache/redisEnterprise@2025-04-01"
  name      = "amr-${local.resource_prefix}-${random_string.suffix.result}"
  location  = var.location
  parent_id = azapi_resource.resource_group.id

  body = {
    sku = {
      name = "Balanced_B0"
    }
    properties = {
      minimumTlsVersion = "1.2"
    }
    tags = local.tags
  }

  response_export_values = ["properties.hostName"]
}

# Redis Enterprise database (default)
resource "azapi_resource" "redis_database" {
  type      = "Microsoft.Cache/redisEnterprise/databases@2025-04-01"
  name      = "default"
  parent_id = azapi_resource.redis_enterprise.id

  body = {
    properties = {
      clientProtocol   = "Encrypted"
      clusteringPolicy = "EnterpriseCluster"
      evictionPolicy   = "VolatileLRU"
      port             = 10000
      modules          = []
      persistence = {
        aofEnabled = false
        rdbEnabled = false
      }
      accessKeysAuthentication = "Disabled"
    }
  }

  response_export_values = ["properties"]
}

# Redis access assignment for App Service managed identity
# Uses Microsoft Entra Authentication - Data Owner role grants full data access
resource "azapi_resource" "redis_app_access" {
  type      = "Microsoft.Cache/redisEnterprise/databases/accessPolicyAssignments@2025-07-01"
  name      = "app-service-access"
  parent_id = azapi_resource.redis_database.id

  body = {
    properties = {
      accessPolicyName = "Data Owner"
      user = {
        objectId = azapi_resource.app_service.identity[0].principal_id
      }
    }
  }

  depends_on = [azapi_resource.app_service]
}
