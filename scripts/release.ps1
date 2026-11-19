# One-command release for VGoalsPlanner: bump versions -> build server images -> archive for scp.
#
# API and UI are versioned separately (VGoalsPlanner.API/version.txt and
# VGoalsPlanner.UIReact/version.txt), but normally move together: -Target both by default.
#
#   .\scripts\release.ps1                    # both: 1.0.0 -> 1.0.1, build, DevOps\vapps-1.0.1.tar
#   .\scripts\release.ps1 -Target ui         # only the UI: UI 1.0.0 -> 1.0.1, API untouched
#   .\scripts\release.ps1 -Part minor
#   .\scripts\release.ps1 -SkipBump          # pack current versions without a bump
#   .\scripts\release.ps1 -SkipBuild         # only pack already built images
#   .\scripts\release.ps1 -DryRun            # print the plan, change nothing
[CmdletBinding()]
param(
    [ValidateSet('api', 'ui', 'both')]
    [string]$Target = 'both',

    [ValidateSet('patch', 'minor', 'major')]
    [string]$Part = 'patch',

    [switch]$SkipBump,
    [switch]$SkipBuild,
    [switch]$SkipSave,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$dockerRoot = Join-Path (Split-Path -Parent $repoRoot) 'VDocker'

$apiVersionFile = Join-Path $repoRoot 'VGoalsPlanner.API\version.txt'
$uiVersionFile = Join-Path $repoRoot 'VGoalsPlanner.UIReact\version.txt'

$images = @{
    api = 'vapps-vds/vgplanner-api'
    ui  = 'vapps-vds/vgplanner-ui'
}

$services = switch ($Target) {
    'api' { @('vgplanner-api') }
    'ui' { @('vgplanner-ui') }
    default { @('vgplanner-api', 'vgplanner-ui') }
}

$targets = switch ($Target) {
    'api' { @('api') }
    'ui' { @('ui') }
    default { @('api', 'ui') }
}

function Invoke-Step {
    param([string]$Description, [scriptblock]$Action)

    Write-Host "==> $Description" -ForegroundColor Cyan
    if ($DryRun) {
        return
    }

    $global:LASTEXITCODE = 0
    & $Action
    if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) {
        throw "Step failed (exit $LASTEXITCODE): $Description"
    }
}

# --- 1. Versions -----------------------------------------------------------------------------

$bumpScript = Join-Path $PSScriptRoot 'bump-version.ps1'
$bumpPreview = @()

if (-not $SkipBump) {
    if ($DryRun) {
        $bumpPreview = @(& $bumpScript -Target $Target -Part $Part -DryRun)
        Write-Host "==> Bump version (-Target $Target -Part $Part): $($bumpPreview -join '; ')" -ForegroundColor Cyan
    }
    else {
        Invoke-Step "Bump version (-Target $Target -Part $Part)" { & $bumpScript -Target $Target -Part $Part }
    }
}

foreach ($file in @($apiVersionFile, $uiVersionFile)) {
    if (-not (Test-Path -LiteralPath $file)) {
        throw "Version file not found: $file"
    }
}

$apiVersion = (Get-Content -LiteralPath $apiVersionFile -Raw).Trim()
$uiVersion = (Get-Content -LiteralPath $uiVersionFile -Raw).Trim()

# DryRun does not touch the files, so take the versions from the preview.
foreach ($line in $bumpPreview) {
    if ($line -match '^api:\s*\S+\s*->\s*(\d+\.\d+\.\d+)') { $apiVersion = $Matches[1] }
    if ($line -match '^ui:\s*\S+\s*->\s*(\d+\.\d+\.\d+)') { $uiVersion = $Matches[1] }
}

foreach ($pair in @(@{ Name = 'api'; Value = $apiVersion }, @{ Name = 'ui'; Value = $uiVersion })) {
    if ($pair.Value -notmatch '^\d+\.\d+\.\d+$') {
        throw "$($pair.Name) version must be MAJOR.MINOR.PATCH, got: '$($pair.Value)'"
    }
}

function Get-Version([string]$Name) {
    if ($Name -eq 'api') { return $apiVersion }
    return $uiVersion
}

# --- 2. Commit and build environment ---------------------------------------------------------

$commit = (& git -C $repoRoot rev-parse --short HEAD 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($commit)) {
    $commit = ''
    Write-Host 'git: commit not detected (image will have no revision)' -ForegroundColor Yellow
}
else {
    $commit = $commit.Trim()
    $dirty = (& git -C $repoRoot status --porcelain 2>$null)
    if (-not [string]::IsNullOrWhiteSpace($dirty)) {
        Write-Host 'git: working tree has uncommitted changes - the image is built from them' -ForegroundColor Yellow
    }
}

# Compose picks these up as build.args (see VDocker/docker-compose.yml).
$env:VGPLANNER_API_VERSION = $apiVersion
$env:VGPLANNER_UI_VERSION = $uiVersion
$env:VGPLANNER_COMMIT = $commit

Write-Host "API: $apiVersion  UI: $uiVersion  commit: $(if ($commit) { $commit } else { '(none)' })" -ForegroundColor Green

if (-not (Test-Path -LiteralPath $dockerRoot)) {
    throw "Docker stack folder not found: $dockerRoot"
}

# --- 3. Build images -------------------------------------------------------------------------

if (-not $SkipBuild) {
    Invoke-Step ("Build " + (($targets | ForEach-Object { $images[$_] }) -join ' + ')) {
        Push-Location $dockerRoot
        try {
            docker compose -f docker-compose.yml -f docker-compose.server.yml --profile vgplanner build $services
        }
        finally {
            Pop-Location
        }
    }
}

# --- 4. Version tags -------------------------------------------------------------------------

if (-not $SkipBuild) {
    Invoke-Step 'Tag images with their versions' {
        foreach ($name in $targets) {
            docker tag "$($images[$name]):latest" "$($images[$name]):$(Get-Version $name)"
        }
    }
}

# --- 5. Archive for the server ---------------------------------------------------------------

$tarName = if ($apiVersion -eq $uiVersion) { "vapps-$apiVersion.tar" } else { "vapps-api$apiVersion-ui$uiVersion.tar" }
$tarPath = Join-Path $dockerRoot (Join-Path 'DevOps' $tarName)

$saveRefs = @()
foreach ($name in $targets) {
    $saveRefs += "$($images[$name]):$(Get-Version $name)"
    $saveRefs += "$($images[$name]):latest"
}

if (-not $SkipSave) {
    Invoke-Step "docker save -> $tarPath" { docker save -o $tarPath $saveRefs }
}

# --- 6. Summary ------------------------------------------------------------------------------

if ($DryRun) {
    Write-Host ''
    Write-Host 'DryRun: nothing was changed.' -ForegroundColor Yellow
    return
}

if (-not (Test-Path -LiteralPath $tarPath)) {
    throw "Archive was not created: $tarPath"
}

$size = (Get-Item -LiteralPath $tarPath).Length
$hash = (Get-FileHash -LiteralPath $tarPath -Algorithm SHA256).Hash

Write-Host ''
Write-Host "Archive: $tarPath" -ForegroundColor Green
Write-Host ("Size: {0:N0} bytes" -f $size)
Write-Host "SHA256: $hash"
Write-Host ''
Write-Host '--- Next ---' -ForegroundColor Cyan
Write-Host "scp `"$tarPath`" root@155.212.168.28:/root/"
Write-Host ''
Write-Host 'On the server:'
Write-Host "sha256sum /root/$tarName        # expected $hash"
Write-Host "docker load -i /root/$tarName"
Write-Host 'cd /home/devops/vapps/deploy'
Write-Host ("docker compose up -d --force-recreate --pull never --no-build " + ($services -join ' '))
Write-Host 'docker compose logs --since 3m vgplanner-api | grep -iE "migrat|fail"'
Write-Host ''
Write-Host "Version check: https://<stand>/api/version -> API $apiVersion"
Write-Host "               UI label in the header -> UI $uiVersion · API $apiVersion"
