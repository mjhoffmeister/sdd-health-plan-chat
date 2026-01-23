<#
.SYNOPSIS
    Cleans up a demo environment by removing worktrees, branches, and stashes.

.DESCRIPTION
    Removes all artifacts created by setup-demo.ps1:
    - Worktree folders (live and checkpoints)
    - Demo branch (demo/<name>)
    - Demo-specific stashes

.PARAMETER Name
    Name of the demo to clean up (e.g., "mcaps" or "demo-01").
    Required unless -All is specified.

.PARAMETER All
    If set, cleans up ALL demo environments.

.PARAMETER KeepStashes
    If set, preserves stashes (only removes folders and branches).

.EXAMPLE
    .\.github\skills\demo\cleanup-demo.ps1 -Name mcaps
    # Removes sdd-health-plan-chat-mcaps folder and demo/mcaps branch

.EXAMPLE
    .\.github\skills\demo\cleanup-demo.ps1 -All
    # Removes all demo folders and branches
#>

param(
    [string]$Name,
    [switch]$All,
    [switch]$KeepStashes
)

$ErrorActionPreference = "Stop"

# Get repo root and name
$repoRoot = git rev-parse --show-toplevel 2>$null
if (-not $repoRoot) {
    Write-Error "Not in a git repository"
    exit 1
}

$repoName = Split-Path $repoRoot -Leaf
$parentDir = Split-Path $repoRoot -Parent

# Validate parameters
if (-not $Name -and -not $All) {
    Write-Error "Specify -Name <demo-name> or -All to clean up all demos."
    exit 1
}

function Remove-Demo {
    param([string]$DemoName)

    $demoFolder = Join-Path $parentDir "$repoName-$DemoName"
    $demoBranch = "demo/$DemoName"

    Write-Host "`nCleaning up demo: $DemoName" -ForegroundColor Cyan

    # Remove worktrees
    if (Test-Path $demoFolder) {
        $worktrees = Get-ChildItem $demoFolder -Directory
        foreach ($wt in $worktrees) {
            Write-Host "  Removing worktree: $($wt.Name)"
            git worktree remove $wt.FullName --force 2>$null
        }

        # Remove the demo folder
        if (Test-Path $demoFolder) {
            Write-Host "  Removing folder: $demoFolder"
            Remove-Item $demoFolder -Recurse -Force
        }
    }
    else {
        Write-Host "  Folder not found: $demoFolder" -ForegroundColor Yellow
    }

    # Remove branch
    $branchExists = git branch --list $demoBranch 2>$null
    if ($branchExists) {
        Write-Host "  Removing branch: $demoBranch"
        git branch -D $demoBranch 2>$null
    }
    else {
        Write-Host "  Branch not found: $demoBranch" -ForegroundColor Yellow
    }

    # Remove stashes
    if (-not $KeepStashes) {
        $stashes = git stash list 2>$null | Where-Object { $_ -match "demo\[$DemoName\]:" }
        if ($stashes) {
            Write-Host "  Clearing stashes for demo '$DemoName'"
            $indices = @()
            $stashes | ForEach-Object {
                if ($_ -match "^stash@\{(\d+)\}") {
                    $indices += [int]$Matches[1]
                }
            }
            $indices | Sort-Object -Descending | ForEach-Object {
                git stash drop stash@{$_} 2>$null
            }
        }
    }

    Write-Host "  Done!" -ForegroundColor Green
}

Write-Host "=== Demo Cleanup ===" -ForegroundColor Magenta

# Prune any stale worktree references
git worktree prune

if ($All) {
    # Find all demo folders
    $demoFolders = Get-ChildItem $parentDir -Directory | Where-Object {
        $_.Name -match "^$([regex]::Escape($repoName))-(demo-\d+|[a-zA-Z][\w-]*)$" -and
        $_.Name -ne $repoName
    }

    if ($demoFolders.Count -eq 0) {
        Write-Host "`nNo demo folders found." -ForegroundColor Yellow
    }
    else {
        foreach ($folder in $demoFolders) {
            $demoName = $folder.Name -replace "^$([regex]::Escape($repoName))-", ""
            Remove-Demo $demoName
        }
    }

    # Also check for orphaned demo branches
    $demoBranches = git branch --list "demo/*" 2>$null
    if ($demoBranches) {
        foreach ($branch in $demoBranches) {
            $branchName = $branch.Trim() -replace "^\*?\s*", ""
            $demoName = $branchName -replace "^demo/", ""
            $demoFolder = Join-Path $parentDir "$repoName-$demoName"

            if (-not (Test-Path $demoFolder)) {
                Write-Host "`nRemoving orphaned branch: $branchName" -ForegroundColor Yellow
                git branch -D $branchName 2>$null
            }
        }
    }
}
else {
    Remove-Demo $Name
}

Write-Host "`n=== Cleanup Complete ===" -ForegroundColor Magenta
