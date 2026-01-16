<#
.SYNOPSIS
    Creates a GitHub Environment and populates variables from Terraform outputs.

.DESCRIPTION
    This script reads Terraform outputs and uses the GitHub CLI to:
    1. Create a GitHub Environment (if it doesn't exist)
    2. Set environment variables required by the app.yml workflow

    This is a one-time bootstrap step after the first `terraform apply`.

.PARAMETER Environment
    The name of the GitHub Environment to create/update. Default: "demo"

.PARAMETER TerraformDir
    Path to the Terraform directory. Default: "infra/terraform"

.PARAMETER Repository
    GitHub repository in "owner/repo" format. Default: auto-detected from git remote.

.EXAMPLE
    # Set your PAT with admin:repo scope
    $env:GH_TOKEN = "ghp_your_pat_here"

    # Run the bootstrap script
    ./scripts/setup-github-env.ps1 -Environment demo

.NOTES
    Requires:
    - GitHub CLI (gh) installed and in PATH
    - GitHub PAT with admin:repo scope (set as GH_TOKEN environment variable)
    - Terraform state initialized (terraform init already run)
    - Terraform apply completed (resources exist)
#>

param(
    [Parameter()]
    [string]$Environment = "demo",

    [Parameter()]
    [string]$TerraformDir = "infra/terraform",

    [Parameter()]
    [string]$Repository
)

$ErrorActionPreference = "Stop"

# Resolve paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$tfDir = Join-Path $repoRoot $TerraformDir

Write-Host "GitHub Environment Bootstrap Script" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check GitHub CLI
if (-not (Get-Command "gh" -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) is not installed. Install from https://cli.github.com/"
}
Write-Host "  ✓ GitHub CLI found" -ForegroundColor Green

# Check gh authentication (prefer GH_TOKEN, fall back to gh auth status)
if ($env:GH_TOKEN) {
    Write-Host "  ✓ GH_TOKEN is set" -ForegroundColor Green
} else {
    $authStatus = gh auth status 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Not authenticated with GitHub CLI. Run 'gh auth login' or set GH_TOKEN environment variable."
    }
    Write-Host "  ✓ GitHub CLI authenticated" -ForegroundColor Green
}

# Check Terraform
if (-not (Get-Command "terraform" -ErrorAction SilentlyContinue)) {
    Write-Error "Terraform is not installed."
}
Write-Host "  ✓ Terraform found" -ForegroundColor Green

# Check Terraform directory
if (-not (Test-Path $tfDir)) {
    Write-Error "Terraform directory not found: $tfDir"
}
Write-Host "  ✓ Terraform directory exists" -ForegroundColor Green

# Auto-detect repository if not provided
if (-not $Repository) {
    Push-Location $repoRoot
    try {
        $remoteUrl = git remote get-url origin 2>$null
        if ($remoteUrl -match "github\.com[:/]([^/]+/[^/.]+)(\.git)?$") {
            $Repository = $Matches[1]
        } else {
            Write-Error "Could not detect repository from git remote. Provide -Repository parameter."
        }
    } finally {
        Pop-Location
    }
}
Write-Host "  ✓ Repository: $Repository" -ForegroundColor Green

Write-Host ""
Write-Host "Reading Terraform outputs..." -ForegroundColor Yellow

# Get Terraform outputs
Push-Location $tfDir
try {
    $outputJson = terraform output -json 2>$null
    if (-not $outputJson) {
        Write-Error "Failed to read Terraform outputs. Ensure 'terraform apply' has been run."
    }
    $outputs = $outputJson | ConvertFrom-Json
} finally {
    Pop-Location
}

# Extract required values
$variables = @{
    "AZURE_CLIENT_ID"          = $outputs.azure_client_id.value
    "AZURE_TENANT_ID"          = $outputs.azure_tenant_id.value
    "AZURE_SUBSCRIPTION_ID"    = $outputs.azure_subscription_id.value
    "AZURE_APP_SERVICE_NAME"   = $outputs.app_service_name.value
    "AZURE_STORAGE_ACCOUNT_NAME" = $outputs.storage_account_name.value
    "AZURE_SWA_NAME"           = $outputs.static_web_app_name.value
    "AZURE_SWA_HOSTNAME"       = $outputs.static_web_app_hostname.value
    "AZURE_RESOURCE_GROUP_NAME" = $outputs.resource_group_name.value
}

# Validate all outputs exist
$missing = @()
foreach ($key in $variables.Keys) {
    if (-not $variables[$key]) {
        $missing += $key
    }
}
if ($missing.Count -gt 0) {
    Write-Error "Missing Terraform outputs: $($missing -join ', ')"
}

Write-Host "  ✓ All Terraform outputs retrieved" -ForegroundColor Green
Write-Host ""

# Create environment
Write-Host "Creating GitHub Environment '$Environment'..." -ForegroundColor Yellow

try {
    gh api --method PUT "repos/$Repository/environments/$Environment" 2>$null | Out-Null
    Write-Host "  ✓ Environment created/verified" -ForegroundColor Green
} catch {
    Write-Error "Failed to create environment. Ensure your PAT has admin:repo scope."
}

Write-Host ""
Write-Host "Setting environment variables..." -ForegroundColor Yellow

# Set each variable
foreach ($key in $variables.Keys) {
    $value = $variables[$key]
    Write-Host "  Setting $key..." -NoNewline

    try {
        gh variable set $key --env $Environment --body $value 2>$null
        Write-Host " ✓" -ForegroundColor Green
    } catch {
        Write-Host " ✗" -ForegroundColor Red
        Write-Error "Failed to set variable $key"
    }
}

Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Bootstrap complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Environment '$Environment' is now configured with:" -ForegroundColor White
foreach ($key in $variables.Keys) {
    Write-Host "  - $key" -ForegroundColor Gray
}
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Verify in GitHub: Settings → Environments → $Environment"
Write-Host "  2. Run the app.yml workflow to deploy the application"
Write-Host ""
