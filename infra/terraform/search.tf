# Azure AI Search for Health Plan Chat

resource "azapi_resource" "search_service" {
  type      = "Microsoft.Search/searchServices@2024-06-01-preview"
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
      publicNetworkAccess = "enabled"
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

# Search index for plan materials
resource "azapi_resource" "search_index" {
  type      = "Microsoft.Search/searchServices/indexes@2024-06-01-preview"
  name      = "plan-materials"
  parent_id = azapi_resource.search_service.id

  # Data-plane resource - no ARM schema available
  schema_validation_enabled = false

  body = {
    fields = [
      {
        name       = "id"
        type       = "Edm.String"
        key        = true
        searchable = false
        filterable = false
        sortable   = false
        facetable  = false
      },
      {
        name       = "chunkId"
        type       = "Edm.String"
        searchable = false
        filterable = true
        sortable   = false
        facetable  = false
      },
      {
        name       = "planDocumentId"
        type       = "Edm.String"
        searchable = false
        filterable = true
        sortable   = false
        facetable  = true
      },
      {
        name       = "planName"
        type       = "Edm.String"
        searchable = true
        filterable = true
        sortable   = true
        facetable  = true
      },
      {
        name       = "planType"
        type       = "Edm.String"
        searchable = false
        filterable = true
        sortable   = false
        facetable  = true
      },
      {
        name       = "section"
        type       = "Edm.String"
        searchable = true
        filterable = true
        sortable   = false
        facetable  = true
      },
      {
        name       = "text"
        type       = "Edm.String"
        searchable = true
        filterable = false
        sortable   = false
        facetable  = false
        analyzer   = "en.microsoft"
      },
      {
        name       = "pageOrAnchor"
        type       = "Edm.String"
        searchable = false
        filterable = false
        sortable   = false
        facetable  = false
      },
      {
        name       = "vector"
        type       = "Collection(Edm.Single)"
        searchable = true
        dimensions = 1536
        vectorSearchProfile = "default-vector-profile"
      }
    ]
    vectorSearch = {
      algorithms = [
        {
          name = "default-hnsw"
          kind = "hnsw"
          hnswParameters = {
            m              = 4
            efConstruction = 400
            efSearch       = 500
            metric         = "cosine"
          }
        }
      ]
      profiles = [
        {
          name          = "default-vector-profile"
          algorithmConfigurationName = "default-hnsw"
        }
      ]
    }
    semantic = {
      configurations = [
        {
          name = "default-semantic"
          prioritizedFields = {
            contentFields = [
              {
                fieldName = "text"
              }
            ]
            titleField = {
              fieldName = "section"
            }
          }
        }
      ]
    }
  }
}

# Data source connecting Search to Blob Storage
resource "azapi_resource" "search_datasource" {
  type      = "Microsoft.Search/searchServices/dataSources@2024-06-01-preview"
  name      = "plan-materials-blob"
  parent_id = azapi_resource.search_service.id

  # Data-plane resource - no ARM schema available
  schema_validation_enabled = false

  body = {
    type = "azureblob"
    credentials = {
      connectionString = "ResourceId=/subscriptions/${data.azurerm_subscription.current.subscription_id}/resourceGroups/${azapi_resource.resource_group.name}/providers/Microsoft.Storage/storageAccounts/${azapi_resource.storage_account.name};"
    }
    container = {
      name = "plan-materials"
    }
    dataChangeDetectionPolicy = {
      "@odata.type" = "#Microsoft.Azure.Search.HighWaterMarkChangeDetectionPolicy"
      highWaterMarkColumnName = "metadata_storage_last_modified"
    }
  }

  # Depends on storage role defined in storage.tf
  depends_on = [azapi_resource.storage_container]
}

# Skillset with Azure OpenAI embedding skill
resource "azapi_resource" "search_skillset" {
  type      = "Microsoft.Search/searchServices/skillsets@2024-06-01-preview"
  name      = "plan-materials-skillset"
  parent_id = azapi_resource.search_service.id

  # Data-plane resource - no ARM schema available
  schema_validation_enabled = false

  body = {
    description = "Skillset for processing plan materials with embeddings"
    skills = [
      {
        "@odata.type" = "#Microsoft.Skills.Text.SplitSkill"
        name          = "split-sections"
        description   = "Split JSON content into sections"
        context       = "/document"
        inputs = [
          {
            name   = "text"
            source = "/document/content"
          }
        ]
        outputs = [
          {
            name       = "textItems"
            targetName = "chunks"
          }
        ]
        textSplitMode     = "pages"
        maximumPageLength = 2000
        pageOverlapLength = 200
      },
      {
        "@odata.type" = "#Microsoft.Skills.Text.AzureOpenAIEmbeddingSkill"
        name          = "generate-embeddings"
        description   = "Generate embeddings using text-embedding-3-small"
        context       = "/document/chunks/*"
        resourceUri   = jsondecode(azapi_resource.ai_services.output).properties.endpoint
        deploymentId  = "text-embedding-3-small"
        modelName     = "text-embedding-3-small"
        inputs = [
          {
            name   = "text"
            source = "/document/chunks/*"
          }
        ]
        outputs = [
          {
            name       = "embedding"
            targetName = "vector"
          }
        ]
      }
    ]
    indexProjections = {
      selectors = [
        {
          targetIndexName = "plan-materials"
          parentKeyFieldName = "planDocumentId"
          sourceContext = "/document/chunks/*"
          mappings = [
            {
              name   = "text"
              source = "/document/chunks/*"
            },
            {
              name   = "vector"
              source = "/document/chunks/*/vector"
            },
            {
              name   = "planDocumentId"
              source = "/document/metadata_storage_name"
            },
            {
              name   = "chunkId"
              source = "/document/chunks/*"
              sourceContext = "/document/chunks/*"
            }
          ]
        }
      ]
      parameters = {
        projectionMode = "generatedKeyAsKeyField"
      }
    }
  }

  depends_on = [azapi_resource.search_foundry_role, azapi_resource.search_index]
}

# Indexer to orchestrate blob -> skillset -> index
resource "azapi_resource" "search_indexer" {
  type      = "Microsoft.Search/searchServices/indexers@2024-06-01-preview"
  name      = "plan-materials-indexer"
  parent_id = azapi_resource.search_service.id

  # Data-plane resource - no ARM schema available
  schema_validation_enabled = false

  body = {
    description    = "Indexer for plan materials from blob storage"
    dataSourceName = azapi_resource.search_datasource.name
    targetIndexName = azapi_resource.search_index.name
    skillsetName   = azapi_resource.search_skillset.name
    schedule = {
      interval = "PT5M" # Run every 5 minutes to detect changes
    }
    parameters = {
      configuration = {
        parsingMode = "json"
        dataToExtract = "contentAndMetadata"
      }
    }
    fieldMappings = [
      {
        sourceFieldName = "metadata_storage_name"
        targetFieldName = "planDocumentId"
      }
    ]
  }

  depends_on = [azapi_resource.search_datasource, azapi_resource.search_skillset]
}
