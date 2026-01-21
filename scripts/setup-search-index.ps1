<#
.SYNOPSIS
    Sets up the Azure AI Search index for Health Plan Chat.

.DESCRIPTION
    Creates the 'plan-materials' index with:
    - Vector search configuration (HNSW algorithm)
    - Semantic search configuration
    - Fields for plan document content and embeddings

    Also creates the data source, skillset (with embedding), and indexer.

.PARAMETER SearchServiceName
    Name of the Azure AI Search service.

.PARAMETER ResourceGroupName
    Name of the resource group containing the search service.

.PARAMETER StorageAccountName
    Name of the storage account containing plan materials.

.PARAMETER FoundryEndpoint
    Azure AI Foundry endpoint for embedding model.

.EXAMPLE
    ./setup-search-index.ps1 -SearchServiceName "srch-healthplanchat-demo-abc123" -ResourceGroupName "rg-healthplanchat-demo" -StorageAccountName "sthpcdemoabc123" -FoundryEndpoint "https://aif-healthplanchat-demo-abc123.cognitiveservices.azure.com"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$SearchServiceName,

    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$StorageAccountName,

    [Parameter(Mandatory = $true)]
    [string]$FoundryEndpoint
)

$ErrorActionPreference = "Stop"

Write-Host "Setting up Azure AI Search index for Health Plan Chat..." -ForegroundColor Cyan

# Get access token for Search management
$searchEndpoint = "https://$SearchServiceName.search.windows.net"
$token = az account get-access-token --resource https://search.azure.com --query accessToken -o tsv

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type"  = "application/json"
    "api-key"       = ""  # Using AAD auth
}

# Index definition with vector search
$indexDefinition = @{
    name = "plan-materials"
    fields = @(
        @{ name = "id"; type = "Edm.String"; key = $true; searchable = $false; filterable = $true }
        @{ name = "content"; type = "Edm.String"; searchable = $true; analyzer = "en.microsoft" }
        @{ name = "title"; type = "Edm.String"; searchable = $true; analyzer = "en.microsoft" }
        @{ name = "planId"; type = "Edm.String"; searchable = $false; filterable = $true; facetable = $true }
        @{ name = "planType"; type = "Edm.String"; searchable = $false; filterable = $true; facetable = $true }
        @{ name = "section"; type = "Edm.String"; searchable = $true; filterable = $true }
        @{ name = "metadata_storage_path"; type = "Edm.String"; searchable = $false; filterable = $false }
        @{ name = "contentVector"; type = "Collection(Edm.Single)"; searchable = $true; dimensions = 1536; vectorSearchProfile = "vector-profile" }
    )
    vectorSearch = @{
        algorithms = @(
            @{
                name = "hnsw-algorithm"
                kind = "hnsw"
                hnswParameters = @{
                    metric = "cosine"
                    m = 4
                    efConstruction = 400
                    efSearch = 500
                }
            }
        )
        profiles = @(
            @{
                name = "vector-profile"
                algorithm = "hnsw-algorithm"
            }
        )
    }
    semantic = @{
        configurations = @(
            @{
                name = "plan-semantic-config"
                prioritizedFields = @{
                    titleField = @{ fieldName = "title" }
                    contentFields = @(
                        @{ fieldName = "content" }
                    )
                    keywordsFields = @(
                        @{ fieldName = "section" }
                        @{ fieldName = "planType" }
                    )
                }
            }
        )
    }
} | ConvertTo-Json -Depth 10

Write-Host "Creating index 'plan-materials'..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$searchEndpoint/indexes/plan-materials?api-version=2024-05-01-preview" `
        -Method Put `
        -Headers $headers `
        -Body $indexDefinition
    Write-Host "Index created successfully." -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 'Conflict') {
        Write-Host "Index already exists, updating..." -ForegroundColor Yellow
        $response = Invoke-RestMethod -Uri "$searchEndpoint/indexes/plan-materials?api-version=2024-05-01-preview" `
            -Method Put `
            -Headers $headers `
            -Body $indexDefinition
        Write-Host "Index updated successfully." -ForegroundColor Green
    } else {
        throw
    }
}

# Get storage connection string for data source
Write-Host "Getting storage account key..." -ForegroundColor Yellow
$storageKey = az storage account keys list --account-name $StorageAccountName --resource-group $ResourceGroupName --query "[0].value" -o tsv
$storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=$StorageAccountName;AccountKey=$storageKey;EndpointSuffix=core.windows.net"

# Data source definition
$dataSourceDefinition = @{
    name = "plan-materials-blob"
    type = "azureblob"
    credentials = @{
        connectionString = $storageConnectionString
    }
    container = @{
        name = "plan-materials"
    }
} | ConvertTo-Json -Depth 5

Write-Host "Creating data source 'plan-materials-blob'..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$searchEndpoint/datasources/plan-materials-blob?api-version=2024-05-01-preview" `
        -Method Put `
        -Headers $headers `
        -Body $dataSourceDefinition
    Write-Host "Data source created successfully." -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode -eq 'Conflict') {
        Write-Host "Data source already exists, updating..." -ForegroundColor Yellow
        $response = Invoke-RestMethod -Uri "$searchEndpoint/datasources/plan-materials-blob?api-version=2024-05-01-preview" `
            -Method Put `
            -Headers $headers `
            -Body $dataSourceDefinition
        Write-Host "Data source updated successfully." -ForegroundColor Green
    } else {
        throw
    }
}

# Skillset definition with Azure OpenAI embedding
$skillsetDefinition = @{
    name = "plan-materials-skillset"
    description = "Skillset for plan materials with text splitting and embedding"
    skills = @(
        @{
            "@odata.type" = "#Microsoft.Skills.Text.SplitSkill"
            name = "split-skill"
            description = "Split content into chunks"
            context = "/document"
            inputs = @(
                @{ name = "text"; source = "/document/content" }
            )
            outputs = @(
                @{ name = "textItems"; targetName = "chunks" }
            )
            textSplitMode = "pages"
            maximumPageLength = 2000
            pageOverlapLength = 200
        }
        @{
            "@odata.type" = "#Microsoft.Skills.Text.AzureOpenAIEmbeddingSkill"
            name = "embedding-skill"
            description = "Generate embeddings for content chunks"
            context = "/document/chunks/*"
            resourceUri = $FoundryEndpoint
            deploymentId = "text-embedding-3-small"
            modelName = "text-embedding-3-small"
            inputs = @(
                @{ name = "text"; source = "/document/chunks/*" }
            )
            outputs = @(
                @{ name = "embedding"; targetName = "vector" }
            )
        }
    )
    indexProjections = @{
        selectors = @(
            @{
                targetIndexName = "plan-materials"
                parentKeyFieldName = "id"
                sourceContext = "/document/chunks/*"
                mappings = @(
                    @{ name = "content"; source = "/document/chunks/*" }
                    @{ name = "contentVector"; source = "/document/chunks/*/vector" }
                    @{ name = "title"; source = "/document/metadata_storage_name" }
                    @{ name = "metadata_storage_path"; source = "/document/metadata_storage_path" }
                )
            }
        )
        parameters = @{
            projectionMode = "generatedKeyAsId"
        }
    }
} | ConvertTo-Json -Depth 10

Write-Host "Creating skillset 'plan-materials-skillset'..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$searchEndpoint/skillsets/plan-materials-skillset?api-version=2024-05-01-preview" `
        -Method Put `
        -Headers $headers `
        -Body $skillsetDefinition
    Write-Host "Skillset created successfully." -ForegroundColor Green
} catch {
    Write-Host "Warning: Skillset creation failed. Error: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "You may need to configure the skillset manually or check Foundry endpoint permissions." -ForegroundColor Yellow
}

# Indexer definition
$indexerDefinition = @{
    name = "plan-materials-indexer"
    dataSourceName = "plan-materials-blob"
    targetIndexName = "plan-materials"
    skillsetName = "plan-materials-skillset"
    schedule = @{
        interval = "PT5M"  # Every 5 minutes
    }
    parameters = @{
        configuration = @{
            parsingMode = "json"
            dataToExtract = "contentAndMetadata"
        }
    }
    fieldMappings = @(
        @{ sourceFieldName = "metadata_storage_path"; targetFieldName = "metadata_storage_path" }
    )
    outputFieldMappings = @()
} | ConvertTo-Json -Depth 5

Write-Host "Creating indexer 'plan-materials-indexer'..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$searchEndpoint/indexers/plan-materials-indexer?api-version=2024-05-01-preview" `
        -Method Put `
        -Headers $headers `
        -Body $indexerDefinition
    Write-Host "Indexer created successfully." -ForegroundColor Green
} catch {
    Write-Host "Warning: Indexer creation failed. Error: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "The index was created but you may need to populate it manually." -ForegroundColor Yellow
}

# Run the indexer immediately
Write-Host "Running indexer..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "$searchEndpoint/indexers/plan-materials-indexer/run?api-version=2024-05-01-preview" `
        -Method Post `
        -Headers $headers
    Write-Host "Indexer started." -ForegroundColor Green
} catch {
    Write-Host "Warning: Could not start indexer. Error: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Search index setup complete!" -ForegroundColor Cyan
Write-Host "Index: plan-materials" -ForegroundColor White
Write-Host "Data source: plan-materials-blob" -ForegroundColor White
Write-Host "Skillset: plan-materials-skillset" -ForegroundColor White
Write-Host "Indexer: plan-materials-indexer" -ForegroundColor White
