<#
.SYNOPSIS
    Bootstrap Terraform remote state storage for Health Plan Chat.

.DESCRIPTION
    Creates the Azure resources needed for Terraform remote state:
    - Resource Group
    - Storage Account
    - Blob Container
    - RBAC assignment for GitHub Actions WIF identity

    This script is idempotent and can be run multiple times safely.
    Designed to run from GitHub Actions with WIF authentication.

.PARAMETER ResourceGroupName
    Name of the resource group for state storage.

.PARAMETER StorageAccountName
    Name of the storage account for state storage.

.PARAMETER ContainerName
    Name of the blob container for state files.

.PARAMETER Location
    Azure region for resources.

.PARAMETER GitHubActionsSpnObjectId
    Object ID of the GitHub Actions service principal for RBAC assignment.
    If not provided, RBAC assignment is skipped.

.EXAMPLE
    ./state-bootstrap.ps1 -ResourceGroupName "rg-terraform-state" -StorageAccountName "stterraformstate123" -ContainerName "tfstate" -Location "eastus2"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$StorageAccountName,

    [Parameter(Mandatory = $false)]
    [string]$ContainerName = "tfstate",

    [Parameter(Mandatory = $false)]
    [string]$Location = "eastus2",

    [Parameter(Mandatory = $false)]
    [string]$GitHubActionsSpnObjectId
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Terraform State Bootstrap" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verify Azure CLI is available
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI (az) is not installed or not in PATH."
    exit 1
}

# Verify we're logged in
$account = az account show 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Not logged into Azure. Please run 'az login' first."
    exit 1
}

$subscriptionId = (az account show --query id -o tsv)
Write-Host "Subscription: $subscriptionId" -ForegroundColor Gray

# Create Resource Group
Write-Host ""
Write-Host "Creating Resource Group: $ResourceGroupName..." -ForegroundColor Yellow
$rgExists = az group exists --name $ResourceGroupName
if ($rgExists -eq "true") {
    Write-Host "  Resource Group already exists." -ForegroundColor Green
} else {
    az group create --name $ResourceGroupName --location $Location --output none
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create Resource Group."
        exit 1
    }
    Write-Host "  Resource Group created." -ForegroundColor Green
}

# Create Storage Account
Write-Host ""
Write-Host "Creating Storage Account: $StorageAccountName..." -ForegroundColor Yellow
$saExists = az storage account show --name $StorageAccountName --resource-group $ResourceGroupName 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "  Storage Account already exists." -ForegroundColor Green

    # Ensure network rules allow GitHub-hosted runners to reach the account.
    # Some environments enforce restrictive defaults via Azure Policy.
    Write-Host "  Ensuring public network access is enabled for state bootstrap..." -ForegroundColor Yellow
    az storage account update `
        --name $StorageAccountName `
        --resource-group $ResourceGroupName `
        --set publicNetworkAccess=Enabled networkRuleSet.defaultAction=Allow `
        --output none
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to update Storage Account network rules for state bootstrap."
        exit 1
    }
} else {
    az storage account create `
        --name $StorageAccountName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --sku Standard_LRS `
        --kind StorageV2 `
        --https-only true `
        --min-tls-version TLS1_2 `
        --allow-blob-public-access false `
        --allow-shared-key-access false `
        --default-action Allow `
        --output none
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create Storage Account."
        exit 1
    }
    Write-Host "  Storage Account created." -ForegroundColor Green
}

# Create Blob Container
Write-Host ""
Write-Host "Creating Blob Container: $ContainerName..." -ForegroundColor Yellow

# Use OAuth for container operations (since shared key is disabled)
$containerExists = az storage container exists `
    --name $ContainerName `
    --account-name $StorageAccountName `
    --auth-mode login `
    --query exists -o tsv 2>&1

if ($containerExists -eq "true") {
    Write-Host "  Blob Container already exists." -ForegroundColor Green
} else {
    az storage container create `
        --name $ContainerName `
        --account-name $StorageAccountName `
        --auth-mode login `
        --output none
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create Blob Container."
        exit 1
    }
    Write-Host "  Blob Container created." -ForegroundColor Green
}

# Assign RBAC if SPN Object ID provided
if ($GitHubActionsSpnObjectId) {
    Write-Host ""
    Write-Host "Assigning Storage Blob Data Contributor role to GitHub Actions SPN..." -ForegroundColor Yellow

    $scope = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Storage/storageAccounts/$StorageAccountName"

    # Check if assignment exists
    $existing = az role assignment list `
        --assignee $GitHubActionsSpnObjectId `
        --scope $scope `
        --role "Storage Blob Data Contributor" `
        --query "[].id" -o tsv 2>&1

    if ($existing) {
        Write-Host "  Role assignment already exists." -ForegroundColor Green
    } else {
        az role assignment create `
            --assignee-object-id $GitHubActionsSpnObjectId `
            --assignee-principal-type ServicePrincipal `
            --role "Storage Blob Data Contributor" `
            --scope $scope `
            --output none
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Failed to create role assignment. You may need to assign it manually."
        } else {
            Write-Host "  Role assignment created." -ForegroundColor Green
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Bootstrap Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Use these backend-config values for terraform init:" -ForegroundColor Yellow
Write-Host "  -backend-config=`"resource_group_name=$ResourceGroupName`"" -ForegroundColor White
Write-Host "  -backend-config=`"storage_account_name=$StorageAccountName`"" -ForegroundColor White
Write-Host "  -backend-config=`"container_name=$ContainerName`"" -ForegroundColor White
Write-Host "  -backend-config=`"key=healthplanchat.tfstate`"" -ForegroundColor White
Write-Host ""
