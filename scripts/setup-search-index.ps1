<#
.SYNOPSIS
    Sets up the Azure AI Search index pipeline for Health Plan Chat.

.DESCRIPTION
    Creates the complete search pipeline:
    - Index with vector search (HNSW) and semantic configuration
    - Data source using managed identity (no storage keys)
    - Skillset with text splitting and Azure OpenAI embedding
    - Indexer to process plan materials from blob storage

.PARAMETER SearchServiceName
    Name of the Azure AI Search service.

.PARAMETER ResourceGroupName
    Name of the resource group containing the search service.

.PARAMETER StorageAccountName
    Name of the storage account containing plan materials.

.PARAMETER FoundryEndpoint
    Azure AI Foundry endpoint for embedding model.

.PARAMETER SubscriptionId
    Azure subscription ID (optional, defaults to current subscription).

.PARAMETER ResetIndexer
    If specified, resets the indexer before running.

.PARAMETER Force
    If specified, deletes and recreates all resources.

.EXAMPLE
    ./setup-search-index.ps1 `
        -SearchServiceName "srch-healthplanchat-demo-abc123" `
        -ResourceGroupName "rg-healthplanchat-demo" `
        -StorageAccountName "sthpcdemoabc123" `
        -FoundryEndpoint "https://aif-healthplanchat-demo-abc123.cognitiveservices.azure.com"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$SearchServiceName,

    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$StorageAccountName,

    [Parameter(Mandatory = $true)]
    [string]$FoundryEndpoint,

    [string]$SubscriptionId,

    [string]$ApiVersion = "2024-05-01-preview",

    [switch]$ResetIndexer,

    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Constants
$IndexName = "plan-materials"
$DataSourceName = "plan-materials-blob"
$SkillsetName = "plan-materials-skillset"
$IndexerName = "plan-materials-indexer"
$ContainerName = "plan-materials"

#region Helper Functions

function Get-SearchEndpoint {
    param([string]$Rg, [string]$Svc)
    $service = az search service show --resource-group $Rg --name $Svc | ConvertFrom-Json
    if (-not $service -or -not $service.name) {
        throw "Failed to resolve Search service endpoint."
    }
    return "https://$($service.name).search.windows.net"
}

function Get-SearchAccessToken {
    $token = az account get-access-token --resource "https://search.azure.com" --query accessToken -o tsv
    if (-not $token) {
        throw "Failed to acquire AAD token for https://search.azure.com."
    }
    return $token
}

function Invoke-SearchRest {
    param(
        [Parameter(Mandatory)][ValidateSet('GET','PUT','POST','DELETE')][string]$Method,
        [Parameter(Mandatory)][string]$Url,
        [object]$BodyObj
    )

    # Get fresh token for each request
    $headers = @{
        "Content-Type"  = "application/json"
        "Authorization" = "Bearer $(Get-SearchAccessToken)"
    }

    $body = $null
    if ($null -ne $BodyObj) {
        $body = $BodyObj | ConvertTo-Json -Depth 100
    }

    try {
        if ($null -ne $body) {
            return Invoke-RestMethod -Method $Method -Uri $Url -Headers $headers -Body $body
        }
        return Invoke-RestMethod -Method $Method -Uri $Url -Headers $headers
    } catch {
        Write-Host "Request failed: $Method $Url" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        if ($_.Exception.Response) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $resp = $reader.ReadToEnd()
                if ($resp) { Write-Host $resp -ForegroundColor DarkRed }
            } catch { }
        }
        throw
    }
}

#endregion

Write-Host "Setting up Azure AI Search pipeline for Health Plan Chat..." -ForegroundColor Cyan
Write-Host ""

# Get subscription ID if not provided
if (-not $SubscriptionId) {
    $SubscriptionId = az account show --query id -o tsv
}

$searchEndpoint = Get-SearchEndpoint -Rg $ResourceGroupName -Svc $SearchServiceName
Write-Host "Search endpoint: $searchEndpoint" -ForegroundColor White
Write-Host "Subscription: $SubscriptionId" -ForegroundColor White
Write-Host ""

# Build storage account resource ID for managed identity connection
$storageAccountId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Storage/storageAccounts/$StorageAccountName"

#region Force cleanup if requested
if ($Force) {
    Write-Host "Force mode: deleting existing resources..." -ForegroundColor Yellow

    # Delete in order: indexer -> skillset -> data source -> index
    @(
        @{ type = "indexers"; name = $IndexerName }
        @{ type = "skillsets"; name = $SkillsetName }
        @{ type = "datasources"; name = $DataSourceName }
        @{ type = "indexes"; name = $IndexName }
    ) | ForEach-Object {
        $url = "{0}/{1}/{2}?api-version={3}" -f $searchEndpoint, $_.type, $_.name, $ApiVersion
        try {
            Invoke-SearchRest -Method DELETE -Url $url | Out-Null
            Write-Host "  Deleted $($_.type)/$($_.name)" -ForegroundColor Gray
        } catch {
            # Ignore not found errors
        }
    }
    Write-Host ""
}
#endregion

#region 1. Create Index
Write-Host "1. Creating index '$IndexName'..." -ForegroundColor Yellow

$indexSchema = @{
    name = $IndexName
    fields = @(
        @{
            name = "id"
            type = "Edm.String"
            key = $true
            searchable = $true
            filterable = $true
            retrievable = $true
            # Keyword analyzer required for index projections
            analyzer = "keyword"
        }
        @{
            name = "parent_id"
            type = "Edm.String"
            searchable = $false
            filterable = $true
            retrievable = $true
        }
        @{
            name = "content"
            type = "Edm.String"
            searchable = $true
            filterable = $false
            retrievable = $true
            analyzer = "en.microsoft"
        }
        @{
            name = "title"
            type = "Edm.String"
            searchable = $true
            filterable = $true
            retrievable = $true
            analyzer = "en.microsoft"
        }
        @{
            name = "planId"
            type = "Edm.String"
            searchable = $false
            filterable = $true
            retrievable = $true
            facetable = $true
        }
        @{
            name = "planType"
            type = "Edm.String"
            searchable = $false
            filterable = $true
            retrievable = $true
            facetable = $true
        }
        @{
            name = "section"
            type = "Edm.String"
            searchable = $true
            filterable = $true
            retrievable = $true
        }
        @{
            name = "metadata_storage_path"
            type = "Edm.String"
            searchable = $false
            filterable = $false
            retrievable = $true
        }
        @{
            name = "contentVector"
            type = "Collection(Edm.Single)"
            searchable = $true
            filterable = $false
            retrievable = $false
            sortable = $false
            facetable = $false
            dimensions = 1536
            vectorSearchProfile = "vector-profile"
        }
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
        vectorizers = @(
            @{
                name = "azure-openai-vectorizer"
                kind = "azureOpenAI"
                azureOpenAIParameters = @{
                    resourceUri = $FoundryEndpoint
                    deploymentId = "text-embedding-3-small"
                    modelName = "text-embedding-3-small"
                }
            }
        )
        profiles = @(
            @{
                name = "vector-profile"
                algorithm = "hnsw-algorithm"
                vectorizer = "azure-openai-vectorizer"
            }
        )
    }
    semantic = @{
        configurations = @(
            @{
                name = "plan-semantic-config"
                prioritizedFields = @{
                    titleField = @{ fieldName = "title" }
                    prioritizedContentFields = @(
                        @{ fieldName = "content" }
                    )
                    prioritizedKeywordsFields = @(
                        @{ fieldName = "section" }
                        @{ fieldName = "planType" }
                    )
                }
            }
        )
    }
}

$indexUrl = "{0}/indexes/{1}?api-version={2}&allowIndexDowntime=true" -f $searchEndpoint, $IndexName, $ApiVersion
Invoke-SearchRest -Method PUT -Url $indexUrl -BodyObj $indexSchema | Out-Null
Write-Host "   Index created." -ForegroundColor Green
#endregion

#region 2. Create Data Source (Managed Identity)
Write-Host "2. Creating data source '$DataSourceName' (managed identity)..." -ForegroundColor Yellow

# For system-assigned managed identity, use DataNoneIdentity with ResourceId connection string
$dataSourceSchema = @{
    name = $DataSourceName
    type = "azureblob"
    credentials = @{
        # ResourceId format enables managed identity authentication
        connectionString = "ResourceId=$storageAccountId;"
    }
    container = @{
        name = $ContainerName
    }
    identity = @{
        "@odata.type" = "#Microsoft.Azure.Search.DataNoneIdentity"
    }
}

$dataSourceUrl = "{0}/datasources/{1}?api-version={2}" -f $searchEndpoint, $DataSourceName, $ApiVersion
Invoke-SearchRest -Method PUT -Url $dataSourceUrl -BodyObj $dataSourceSchema | Out-Null
Write-Host "   Data source created." -ForegroundColor Green
#endregion

#region 3. Create Skillset
Write-Host "3. Creating skillset '$SkillsetName'..." -ForegroundColor Yellow

$skillsetSchema = @{
    name = $SkillsetName
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
                targetIndexName = $IndexName
                parentKeyFieldName = "parent_id"
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
            projectionMode = "skipIndexingParentDocuments"
        }
    }
}

$skillsetUrl = "{0}/skillsets/{1}?api-version={2}" -f $searchEndpoint, $SkillsetName, $ApiVersion
Invoke-SearchRest -Method PUT -Url $skillsetUrl -BodyObj $skillsetSchema | Out-Null
Write-Host "   Skillset created." -ForegroundColor Green
#endregion

#region 4. Create Indexer
Write-Host "4. Creating indexer '$IndexerName'..." -ForegroundColor Yellow

$indexerSchema = @{
    name = $IndexerName
    dataSourceName = $DataSourceName
    targetIndexName = $IndexName
    skillsetName = $SkillsetName
    schedule = @{
        interval = "PT5M"
    }
    parameters = @{
        configuration = @{
            # Parse JSON array at /sections to index each section as a document
            parsingMode = "jsonArray"
            documentRoot = "/sections"
            dataToExtract = "contentAndMetadata"
        }
    }
    fieldMappings = @(
        @{ sourceFieldName = "metadata_storage_path"; targetFieldName = "metadata_storage_path" }
        @{ sourceFieldName = "heading"; targetFieldName = "title" }
        @{ sourceFieldName = "/planType"; targetFieldName = "planType" }
        @{ sourceFieldName = "/planDocumentId"; targetFieldName = "planId" }
        @{ sourceFieldName = "heading"; targetFieldName = "section" }
    )
    outputFieldMappings = @()
}

$indexerUrl = "{0}/indexers/{1}?api-version={2}" -f $searchEndpoint, $IndexerName, $ApiVersion
Invoke-SearchRest -Method PUT -Url $indexerUrl -BodyObj $indexerSchema | Out-Null
Write-Host "   Indexer created." -ForegroundColor Green
#endregion

#region 5. Reset indexer if requested
if ($ResetIndexer) {
    Write-Host "5. Resetting indexer..." -ForegroundColor Yellow
    $resetUrl = "{0}/indexers/{1}/reset?api-version={2}" -f $searchEndpoint, $IndexerName, $ApiVersion
    Invoke-SearchRest -Method POST -Url $resetUrl | Out-Null
    Write-Host "   Indexer reset." -ForegroundColor Green
}
#endregion

#region 6. Run the indexer
Write-Host "6. Running indexer..." -ForegroundColor Yellow
$runUrl = "{0}/indexers/{1}/run?api-version={2}" -f $searchEndpoint, $IndexerName, $ApiVersion
Invoke-SearchRest -Method POST -Url $runUrl | Out-Null
Write-Host "   Indexer started." -ForegroundColor Green
#endregion

Write-Host ""
Write-Host "Search pipeline setup complete!" -ForegroundColor Cyan
Write-Host ""
Write-Host "Pipeline configuration:" -ForegroundColor White
Write-Host "  Index:       $IndexName"
Write-Host "  Data source: $DataSourceName (container: $ContainerName)"
Write-Host "  Skillset:    $SkillsetName"
Write-Host "  Indexer:     $IndexerName"
Write-Host ""
Write-Host "Check indexer status with:" -ForegroundColor Yellow
Write-Host "  az search indexer status show --resource-group $ResourceGroupName --service-name $SearchServiceName --name $IndexerName"
