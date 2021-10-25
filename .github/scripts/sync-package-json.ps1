#!/usr/bin/env pwsh

param(
  [Parameter(Mandatory)]
  [ValidateScript({ ($_.EndsWith("manifest.json")) })]
  [string]$manifest,

  [Parameter(Mandatory)]
  [ValidateScript({ ($_.EndsWith("package.json")) })]
  [string]$package,

  [string]$version,

  $dryRun
)

$semVerRegex = "^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$"
$versionTemplate = '${ ci.manifest_value }'

$manifestJson = $null
$packageJson = $null

try {
  $manifestJson = Get-Content $manifest -ErrorAction Stop | ConvertFrom-Json -AsHashtable -ErrorAction Stop
  $packageJson = Get-Content $package -ErrorAction Stop | ConvertFrom-Json -AsHashtable -ErrorAction Stop
} catch {
  Write-Host "[Sync]: Error: $_" -ForegroundColor Red
  exit 1
}

$manifestDeps = $manifestJson.dependencies
$packageDeps = $packageJson.dependencies

foreach ($key in $manifestDeps.keys) {
  if ($packageDeps.ContainsKey($key)) {
    $manifestVersion = $manifestDeps[$key];
    $packageVersion = $packageDeps[$key];

    Write-Host "[Sync]: $key ..."
    Write-Host "[Sync]: Manifest: $manifestVersion, Package: $packageVersion"

    if (-not($manifestVersion -match $semVerRegex)) {
      Write-Host "[Sync]: Not compatible." -ForegroundColor Red
      continue
    }

    if (($manifestVersion -ne $packageVersion) -or ($packageVersion -eq $versionTemplate)) {
      Write-Host "[Sync]: Synchronizing with project ($packageVersion -> $manifestVersion)" -ForegroundColor Yellow
      $packageJson.dependencies[$key] = $manifestVersion
    } else {
      Write-Host "[Sync]: Ok." -ForegroundColor Green
    }
  }
}

if ($version) {
  if (-not($version -match $semVerRegex)) {
    Write-Host "[Sync]: $version is not SemVer" -ForegroundColor Red
    exit 1
  }

  $packageJson.version = $version
}

Write-Host "[Sync]: Result package.json:"
$packageJson | ConvertTo-Json | Write-Host

if (-not $dryRun -or ($dryRun -ieq "false")) {
  $packageJson | ConvertTo-Json | Out-File -FilePath $package -NoNewline -Encoding ASCII
  Write-Host "[Sync]: Done." -ForegroundColor Green
} else {
  Write-Host "[Sync]: Done. (DryRun)" -ForegroundColor Yellow
}
