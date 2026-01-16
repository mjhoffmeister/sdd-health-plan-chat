# Azure Managed Redis (Redis Enterprise) for Health Plan Chat

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
    }
  }

  response_export_values = ["properties"]
}

# Note: Azure Managed Redis (Enterprise) uses access keys.
# The connection string will be retrieved at runtime using managed identity
# via Azure Resource Manager, or passed as a secret via App Settings.
