param(
    [switch] $Force
)

$ErrorActionPreference = "Stop"

$ProjectDir = Resolve-Path (Join-Path $PSScriptRoot "..")
$LibDir = Join-Path $ProjectDir "libs"
$TargetDir = Join-Path $LibDir "concentus-v1.2-csharp"
$ExpectedSourceDir = Join-Path $TargetDir "CSharp\Concentus"
$CacheDir = Join-Path $LibDir ".cache"
$ZipPath = Join-Path $CacheDir "concentus-v1.2-csharp.zip"
$ExtractDir = Join-Path $CacheDir "extract"
$ConcentusUrl = "https://codeload.github.com/lostromb/concentus/zip/refs/tags/v1.2-c%23"

if ((Test-Path -LiteralPath $ExpectedSourceDir) -and !$Force) {
    Write-Host "Concentus source already installed at $TargetDir"
    exit 0
}

New-Item -ItemType Directory -Force -Path $LibDir, $CacheDir | Out-Null

if ($Force) {
    Remove-Item -LiteralPath $TargetDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $ZipPath -Force -ErrorAction SilentlyContinue
}

if (!(Test-Path -LiteralPath $ZipPath)) {
    Write-Host "Downloading Concentus source..."
    Invoke-WebRequest -Uri $ConcentusUrl -OutFile $ZipPath -UseBasicParsing
}

Remove-Item -LiteralPath $ExtractDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $ExtractDir | Out-Null

Write-Host "Extracting Concentus source..."
Expand-Archive -LiteralPath $ZipPath -DestinationPath $ExtractDir -Force

$ExtractedRoot = Get-ChildItem -LiteralPath $ExtractDir -Directory |
    Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "CSharp\Concentus") } |
    Select-Object -First 1

if ($null -eq $ExtractedRoot) {
    throw "Downloaded Concentus archive did not contain CSharp\Concentus."
}

Remove-Item -LiteralPath $TargetDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $TargetDir | Out-Null

Get-ChildItem -LiteralPath $ExtractedRoot.FullName -Force | ForEach-Object {
    Move-Item -LiteralPath $_.FullName -Destination $TargetDir -Force
}

if (!(Test-Path -LiteralPath $ExpectedSourceDir)) {
    throw "Concentus install failed; expected source directory was not created: $ExpectedSourceDir"
}

Write-Host "Installed Concentus source to $TargetDir"
