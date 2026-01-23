<#
.SYNOPSIS
    Sets up a demo environment for spec-kit staged demos using git worktrees.

.DESCRIPTION
    Creates a worktree-based demo environment with:
    - A "live" worktree for editing during demos
    - Checkpoint worktrees for each phase tag (for offline resilience)

.PARAMETER Name
    Name for the demo folder. Creates <repo>-<name> (e.g., -Name mcaps creates sdd-health-plan-chat-mcaps).
    If not provided, auto-numbers as <repo>-demo-01, demo-02, etc.

.PARAMETER WorktreeRoot
    Override the full path for the demo folder. If set, -Name is ignored.

.PARAMETER SkipCheckpoints
    If set, skips creating worktrees for each phase tag.

.PARAMETER Reset
    If set, removes existing demo worktrees and recreates them.

.EXAMPLE
    .\.github\skills\demo\setup-demo.ps1
    # Creates sdd-health-plan-chat-demo-01 (auto-numbered)

.EXAMPLE
    .\.github\skills\demo\setup-demo.ps1 -Name mcaps
    # Creates sdd-health-plan-chat-mcaps

.EXAMPLE
    .\.github\skills\demo\setup-demo.ps1 -SkipCheckpoints
    # Creates only the live worktree

.EXAMPLE
    .\.github\skills\demo\setup-demo.ps1 -Reset
    # Removes and recreates demo worktrees
#>

param(
    [string]$Name,
    [string]$WorktreeRoot,
    [switch]$SkipCheckpoints,
    [switch]$Reset
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

if (-not $WorktreeRoot) {
    if ($Name) {
        # Use provided name
        $WorktreeRoot = Join-Path $parentDir "$repoName-$Name"
    }
    else {
        # Auto-number: find next available demo-NN
        $nextNum = 1
        while (Test-Path (Join-Path $parentDir "$repoName-demo-$($nextNum.ToString('00'))")) {
            $nextNum++
        }
        $WorktreeRoot = Join-Path $parentDir "$repoName-demo-$($nextNum.ToString('00'))"
    }
}

# Phase tags in order
$phaseTags = @(
    "phase/00-init",
    "phase/01-constitution",
    "phase/02-spec",
    "phase/03-plan",
    "phase/04-tasks",
    "phase/05-implement/01-setup",
    "phase/05-implement/02-foundational",
    "phase/05-implement/03-ask-plan-questions",
    "phase/05-implement/04-handle-missing-answers",
    "phase/05-implement/05-ui",
    "phase/05-implement/06-documentation"
)

# Derive demo name from folder for unique branch
$demoFolderName = Split-Path $WorktreeRoot -Leaf
$demoName = $demoFolderName -replace "^$([regex]::Escape($repoName))-", ""
$liveBranch = "demo/$demoName"
$liveWorktree = Join-Path $WorktreeRoot "live"

function Remove-DemoWorktrees {
    Write-Host "Removing existing demo worktrees..." -ForegroundColor Yellow

    # Remove live worktree
    if (Test-Path $liveWorktree) {
        Write-Host "  Removing $liveWorktree"
        git worktree remove $liveWorktree --force 2>$null
    }

    # Remove checkpoint worktrees
    foreach ($tag in $phaseTags) {
        $safeName = $tag -replace "/", "-"
        $worktreePath = Join-Path $WorktreeRoot $safeName
        if (Test-Path $worktreePath) {
            Write-Host "  Removing $worktreePath"
            git worktree remove $worktreePath --force 2>$null
        }
    }

    # Remove demo branch if it exists
    $branches = git branch --list $liveBranch 2>$null
    if ($branches) {
        Write-Host "  Removing branch $liveBranch"
        git branch -D $liveBranch 2>$null
    }

    # Clear demo stashes for this specific demo
    $stashes = git stash list 2>$null | Where-Object { $_ -match "demo\[$demoName\]:" }
    if ($stashes) {
        Write-Host "  Clearing stashes for demo '$demoName'"
        # Process in reverse order to maintain correct indices
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

    git worktree prune

    # Remove demo folder if empty
    if ((Test-Path $WorktreeRoot) -and -not (Get-ChildItem $WorktreeRoot)) {
        Write-Host "  Removing empty demo folder"
        Remove-Item $WorktreeRoot
    }
}

function Test-TagExists {
    param([string]$Tag)
    $result = git tag -l $Tag 2>$null
    return [bool]$result
}

function New-LiveWorktree {
    Write-Host "`nCreating live demo worktree..." -ForegroundColor Cyan

    # Ensure demo folder exists
    if (-not (Test-Path $WorktreeRoot)) {
        New-Item -ItemType Directory -Path $WorktreeRoot | Out-Null
    }

    # Verify init tag exists
    $initTag = $phaseTags[0]
    if (-not (Test-TagExists $initTag)) {
        Write-Error "Tag '$initTag' not found. Run 'git fetch --tags' first."
        exit 1
    }

    # Create demo/live branch from init tag
    $existingBranch = git branch --list $liveBranch 2>$null
    if (-not $existingBranch) {
        Write-Host "  Creating branch $liveBranch from $initTag"
        git branch $liveBranch $initTag
    }

    # Create worktree
    Write-Host "  Creating worktree at $liveWorktree"
    git worktree add $liveWorktree $liveBranch

    Write-Host "  Live worktree ready!" -ForegroundColor Green
}

function New-CheckpointWorktrees {
    Write-Host "`nCreating checkpoint worktrees..." -ForegroundColor Cyan

    foreach ($tag in $phaseTags) {
        if (-not (Test-TagExists $tag)) {
            Write-Host "  Skipping $tag (not found)" -ForegroundColor Yellow
            continue
        }

        $safeName = $tag -replace "/", "-"
        $worktreePath = Join-Path $WorktreeRoot $safeName

        if (Test-Path $worktreePath) {
            Write-Host "  Skipping $tag (already exists)"
            continue
        }

        Write-Host "  Creating $safeName"
        git worktree add --detach $worktreePath $tag
    }

    Write-Host "  Checkpoint worktrees ready!" -ForegroundColor Green
}

# Main execution
Write-Host "=== Spec Kit Demo Setup ===" -ForegroundColor Magenta
Write-Host "Repository: $repoName"
Write-Host "Worktree root: $WorktreeRoot"

# Fetch latest tags
Write-Host "`nFetching tags..." -ForegroundColor Cyan
git fetch --tags

if ($Reset) {
    Remove-DemoWorktrees
}

New-LiveWorktree

if (-not $SkipCheckpoints) {
    New-CheckpointWorktrees
}

# Summary
Write-Host "`n=== Setup Complete ===" -ForegroundColor Magenta
Write-Host "`nLive worktree: $liveWorktree" -ForegroundColor Green
Write-Host "  - Open this folder in VS Code to run your demo"
Write-Host "  - Edit freely without affecting the main repo"

Write-Host "`nUseful commands:" -ForegroundColor Yellow
Write-Host "  # Jump to a specific phase (preserves work):"
Write-Host "  cd $liveWorktree"
Write-Host "  .\.github\skills\demo\jump-to-phase.ps1 -Phase plan"
Write-Host ""
Write-Host "  # Jump and discard current changes:"
Write-Host "  .\.github\skills\demo\jump-to-phase.ps1 -Phase plan -Reset"
Write-Host ""
Write-Host "  # Open in VS Code:"
Write-Host "  code $liveWorktree"
