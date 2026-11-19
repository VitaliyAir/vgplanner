# Increments the version of the API and/or the UI. Each component has its own version.txt:
#   VGoalsPlanner.API/version.txt        backend (API/Application/Domain/Infrastructure)
#   VGoalsPlanner.UIReact/version.txt    frontend build
#
#   .\scripts\bump-version.ps1                 # both, patch: 1.0.0 -> 1.0.1
#   .\scripts\bump-version.ps1 -Target ui      # only the UI
#   .\scripts\bump-version.ps1 -Target api -Part minor
#   .\scripts\bump-version.ps1 -DryRun         # show the result, change nothing
[CmdletBinding()]
param(
    [ValidateSet('api', 'ui', 'both')]
    [string]$Target = 'both',

    [ValidateSet('patch', 'minor', 'major')]
    [string]$Part = 'patch',

    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

$versionFiles = @{
    api = Join-Path $root 'VGoalsPlanner.API\version.txt'
    ui  = Join-Path $root 'VGoalsPlanner.UIReact\version.txt'
}

$targets = switch ($Target) {
    'api' { @('api') }
    'ui' { @('ui') }
    default { @('api', 'ui') }
}

# No BOM: MSBuild reads the whole file into <Version>, and Vite reads it as UTF-8 text.
$utf8 = New-Object System.Text.UTF8Encoding($false)

foreach ($name in $targets) {
    $file = $versionFiles[$name]

    if (-not (Test-Path -LiteralPath $file)) {
        throw "Version file not found: $file"
    }

    $current = (Get-Content -LiteralPath $file -Raw).Trim()

    if ($current -notmatch '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$') {
        throw "$file must contain a single MAJOR.MINOR.PATCH line, got: '$current'"
    }

    $major = [int]$Matches.major
    $minor = [int]$Matches.minor
    $patch = [int]$Matches.patch

    switch ($Part) {
        'major' { $major++; $minor = 0; $patch = 0 }
        'minor' { $minor++; $patch = 0 }
        default { $patch++ }
    }

    $next = "$major.$minor.$patch"

    if ($DryRun) {
        Write-Output "$($name): $current -> $next (DryRun, file not changed)"
        continue
    }

    [System.IO.File]::WriteAllText($file, $next + "`n", $utf8)
    Write-Output "$($name): $current -> $next"
}
