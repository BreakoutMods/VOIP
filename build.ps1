param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Debug",
    [switch] $Deploy
)

$ErrorActionPreference = "Stop"

$ProjectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ServerDir = Resolve-Path (Join-Path $ProjectDir "..\..")
$ManagedDir = Join-Path $ServerDir "valheim_server_Data\Managed"
$BepInExDir = Join-Path $ServerDir "BepInEx"
$Csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$BreakoutNetDir = Resolve-Path (Join-Path $ProjectDir "..\BreakoutNet")
$BreakoutNetBuild = Join-Path $BreakoutNetDir "build.ps1"
$BreakoutNetDll = Join-Path $BreakoutNetDir "src\BreakoutNet\bin\$Configuration\net462\BreakoutNet.dll"

if (!(Test-Path -LiteralPath $Csc)) {
    throw "Could not find csc.exe at $Csc"
}

if (!(Test-Path -LiteralPath $BreakoutNetBuild)) {
    throw "Could not find BreakoutNet build script at $BreakoutNetBuild"
}

& $BreakoutNetBuild -Configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (!(Test-Path -LiteralPath $BreakoutNetDll)) {
    throw "BreakoutNet.dll was not built at $BreakoutNetDll"
}

$OutDir = Join-Path $ProjectDir "bin\$Configuration\net462"
$DeployDir = Join-Path $BepInExDir "plugins\VOIP"
$BreakoutNetDeployDir = Join-Path $BepInExDir "plugins\BreakoutNet"
New-Item -ItemType Directory -Force -Path $OutDir, $DeployDir, $BreakoutNetDeployDir | Out-Null

$Out = Join-Path $OutDir "VOIP.dll"
if (Test-Path -LiteralPath $Out) {
    Remove-Item -LiteralPath $Out -Force
}

$ManagedReferences = @(
    "mscorlib.dll",
    "System.dll",
    "System.Core.dll",
    "assembly_valheim.dll",
    "assembly_utils.dll",
    "netstandard.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.AudioModule.dll",
    "UnityEngine.InputLegacyModule.dll",
    "UnityEngine.IMGUIModule.dll"
) | ForEach-Object {
    "/reference:" + (Resolve-Path (Join-Path $ManagedDir $_)).Path
}

$References = @()
$References += "/reference:" + (Resolve-Path (Join-Path $BepInExDir "core\BepInEx.dll")).Path
$References += "/reference:" + (Resolve-Path $BreakoutNetDll).Path
$References += $ManagedReferences

$ConcentusSourceDir = Join-Path $ProjectDir "libs\concentus-v1.2-csharp\CSharp\Concentus"
if (!(Test-Path -LiteralPath $ConcentusSourceDir)) {
    throw "Missing Concentus source at $ConcentusSourceDir. Run .\scripts\install-deps.ps1 first."
}

$Sources = @()
$Sources += Get-ChildItem -LiteralPath (Join-Path $ProjectDir "src") -Recurse -Filter "*.cs" | ForEach-Object { $_.FullName }
$Sources += Get-ChildItem -LiteralPath $ConcentusSourceDir -Recurse -Filter "*.cs" |
    Where-Object { $_.Name -ne "AssemblyInfo.cs" } |
    ForEach-Object { $_.FullName }

& $Csc /nologo /noconfig /nostdlib /define:TRACE,PARITY /target:library "/out:$Out" $References $Sources
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Built $Out"

if (!$Deploy) {
    Write-Host "Deployment skipped. Pass -Deploy to copy VOIP.dll to BepInEx/plugins/VOIP."
    exit 0
}

try {
    Copy-Item -LiteralPath $BreakoutNetDll -Destination $BreakoutNetDeployDir -Force
    Copy-Item -LiteralPath $Out -Destination $DeployDir -Force
    Remove-Item -LiteralPath (Join-Path $DeployDir "Concentus.dll") -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath (Join-Path $DeployDir "Concentus.dll.pending") -Force -ErrorAction SilentlyContinue
    Write-Host "Deployed to $DeployDir"
} catch {
    $Pending = Join-Path $DeployDir "VOIP.dll.pending"
    $BreakoutNetPending = Join-Path $BreakoutNetDeployDir "BreakoutNet.dll.pending"
    Copy-Item -LiteralPath $BreakoutNetDll -Destination $BreakoutNetPending -Force
    Copy-Item -LiteralPath $Out -Destination $Pending -Force
    Remove-Item -LiteralPath (Join-Path $DeployDir "Concentus.dll.pending") -Force -ErrorAction SilentlyContinue
    Write-Warning "Could not overwrite the deployed DLL. It is probably loaded by Valheim or the dedicated server."
    Write-Warning "Stop the game/server, then copy VOIP.dll.pending over VOIP.dll."
    Write-Warning "Also delete the old Concentus.dll from this plugin folder; Opus is now embedded in VOIP.dll."
}
