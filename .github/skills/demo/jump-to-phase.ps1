<#
.SYNOPSIS
    Jump to a different phase in the demo, preserving work-in-progress via stash.

.DESCRIPTION
    Navigates between demo phases while preserving uncommitted changes:
    - Stashes current changes with a phase-specific message
    - Resets to the target phase tag
    - Restores any previously stashed work for that phase

    Use -Reset to discard changes instead of preserving them.

.PARAMETER Phase
    The phase to jump to. Accepts:
    - Phase number: 0, 1, 2, 3, 4, 5.1, 5.2, etc.
    - Phase name: init, constitution, spec, plan, tasks, setup, foundational, etc.
    - Full tag: phase/03-plan, phase/05-implement/01-setup

.PARAMETER Reset
    If set, discards current changes and skips restoring stashed work.

.EXAMPLE
    .\jump-to-phase.ps1 -Phase 3
    # Jump to phase/03-plan, preserving current work

.EXAMPLE
    .\jump-to-phase.ps1 -Phase plan
    # Jump to phase/03-plan by name

.EXAMPLE
    .\jump-to-phase.ps1 -Phase 5.3 -Reset
    # Jump to phase/05-implement/03-ask-plan-questions, discarding changes
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Phase,
    [switch]$Reset
)

$ErrorActionPreference = "Stop"

# Capture current branch (may be empty when detached)
$currentBranch = git branch --show-current 2>$null

# Phase tags in order with metadata for lookup
$phases = @(
    @{ Tag = "phase/00-init"; Number = "0"; Names = @("init", "00") },
    @{ Tag = "phase/01-constitution"; Number = "1"; Names = @("constitution", "01") },
    @{ Tag = "phase/02-spec"; Number = "2"; Names = @("spec", "02") },
    @{ Tag = "phase/03-plan"; Number = "3"; Names = @("plan", "03") },
    @{ Tag = "phase/04-tasks"; Number = "4"; Names = @("tasks", "04") },
    @{ Tag = "phase/05-implement/01-setup"; Number = "5.1"; Names = @("setup", "05.1", "5.01") },
    @{ Tag = "phase/05-implement/02-foundational"; Number = "5.2"; Names = @("foundational", "05.2", "5.02") },
    @{ Tag = "phase/05-implement/03-ask-plan-questions"; Number = "5.3"; Names = @("ask-plan-questions", "us1", "05.3", "5.03") },
    @{ Tag = "phase/05-implement/04-handle-missing-answers"; Number = "5.4"; Names = @("handle-missing-answers", "us2", "05.4", "5.04") },
    @{ Tag = "phase/05-implement/05-ui"; Number = "5.5"; Names = @("ui", "us3", "05.5", "5.05") },
    @{ Tag = "phase/05-implement/06-documentation"; Number = "5.6"; Names = @("documentation", "docs", "05.6", "5.06") }
)

function Get-CurrentPhaseTag {
    $tag = git describe --tags --match "phase/*" --abbrev=0 2>$null
    if ($tag) {
        return $tag.Trim()
    }

    return "phase/unknown"
}

function Test-IsWorktree {
    $repoRoot = git rev-parse --show-toplevel 2>$null
    if (-not $repoRoot) {
        return $false
    }

    $gitPath = Join-Path $repoRoot ".git"
    if (Test-Path $gitPath) {
        $item = Get-Item $gitPath -ErrorAction SilentlyContinue
        if ($item -and -not $item.PSIsContainer) {
            return $true
        }
    }

    $gitDir = git rev-parse --git-dir 2>$null
    if ($gitDir) {
        $normalized = $gitDir.Trim() -replace "\\", "/"
        return $normalized -match "/worktrees/"
    }

    return $false
}

function Test-IsMainBranch {
    $branch = git branch --show-current 2>$null
    if (-not $branch) {
        return $false
    }

    $branch = $branch.Trim()
    return $branch -eq "main" -or $branch -eq "master"
}

function Get-DemoName {
    $branch = git branch --show-current 2>$null
    if ($branch -match "^demo/(.+)$") {
        return $Matches[1]
    }

    if (-not (Test-IsWorktree)) {
        return "local"
    }

    $repoRoot = git rev-parse --show-toplevel 2>$null
    $gitCommonDir = git rev-parse --git-common-dir 2>$null
    if (-not $repoRoot -or -not $gitCommonDir) {
        return "unknown"
    }

    $repoRoot = $repoRoot.Trim() -replace "/", "\\"
    $gitCommonDir = $gitCommonDir.Trim() -replace "/", "\\"

    $repoName = Split-Path (Split-Path $gitCommonDir -Parent) -Leaf
    $demoFolder = Split-Path $repoRoot -Parent
    $demoFolderName = Split-Path $demoFolder -Leaf

    if ($repoName -and $demoFolderName -like "$repoName-*") {
        return $demoFolderName.Substring($repoName.Length + 1)
    }

    return $demoFolderName
}

function Resolve-PhaseTag {
    param([string]$PhaseInput)

    # Check if it's already a full tag
    if ($PhaseInput -match "^phase/") {
        return $PhaseInput
    }

    # Search by number or name
    $inputLower = $PhaseInput.ToLower()
    foreach ($p in $phases) {
        if ($p.Number -eq $PhaseInput) {
            return $p.Tag
        }
        if ($p.Names -contains $inputLower) {
            return $p.Tag
        }
    }

    return $null
}

function Get-DemoStashIndex {
    param([string]$Tag)

    $stashList = git stash list 2>$null
    $index = 0
    $pattern = "demo\[$demoName\]:$([regex]::Escape($Tag))$"
    foreach ($line in $stashList) {
        if ($line -match $pattern) {
            return $index
        }
        $index++
    }
    return -1
}

$demoName = Get-DemoName
if (-not $demoName) {
    $demoName = "unknown"
}

# Resolve target phase
$targetTag = Resolve-PhaseTag $Phase
if (-not $targetTag) {
    Write-Error "Unknown phase: $Phase`n`nValid phases:`n  Numbers: 0, 1, 2, 3, 4, 5.1, 5.2, 5.3, 5.4, 5.5, 5.6`n  Names: init, constitution, spec, plan, tasks, setup, foundational, us1, us2, us3, docs"
    exit 1
}

# Verify tag exists
$tagExists = git tag -l $targetTag 2>$null
if (-not $tagExists) {
    Write-Error "Tag '$targetTag' not found locally. Run 'git fetch --tags' first."
    exit 1
}

# Warn if not in a demo worktree
if (Test-IsMainBranch) {
    Write-Error "Refusing to run on 'main'/'master'. Use a demo worktree or a non-main branch."
    exit 1
}

if (-not (Test-IsWorktree)) {
    Write-Warning "Not in a demo worktree. Changes here may affect the main repository."
}

$currentTag = Get-CurrentPhaseTag
Write-Host "=== Jump to Phase ===" -ForegroundColor Magenta
Write-Host "From: $currentTag"
Write-Host "To:   $targetTag"

# Check for uncommitted changes
$status = git status --porcelain 2>$null
$hasChanges = [bool]$status

if ($hasChanges) {
    if ($Reset) {
        Write-Host "`nDiscarding changes..." -ForegroundColor Yellow
        git reset --hard HEAD
        git clean -fd
    }
    else {
        Write-Host "`nStashing changes as 'demo[$demoName]:$currentTag'..." -ForegroundColor Cyan
        git stash push -m "demo[$demoName]:$currentTag" --include-untracked
    }
}

# Reset to target phase
Write-Host "`nResetting to $targetTag..." -ForegroundColor Cyan
git reset --hard $targetTag
git clean -fd

# Restore stashed work for target phase (unless reset mode)
if (-not $Reset) {
    $stashIndex = Get-DemoStashIndex $targetTag
    if ($stashIndex -ge 0) {
        Write-Host "`nRestoring stashed work for $targetTag..." -ForegroundColor Cyan
        git stash pop stash@{$stashIndex}
    }
}

Write-Host "`n=== Now at: $targetTag ===" -ForegroundColor Green
