#!/usr/bin/env pwsh
# Builds a Windows setup .exe (Inno Setup) for one desktop app.
#
#   installers/windows/build-windows-installer.ps1 -App ReportDesigner -Rid win-x64
#
# Needs: dotnet SDK and Inno Setup 6 (iscc.exe). Output: Release-Builds/build-output/installers/
param(
    [Parameter(Mandatory)][ValidateSet("ReportDesigner", "RdlReader", "RdlCmd")][string]$App,
    [ValidateSet("win-x64", "win-arm64")][string]$Rid = "win-x64"
)
$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$root = (Resolve-Path (Join-Path $PSScriptRoot ".." "..")).Path
$xml = [xml](Get-Content (Join-Path $root "Directory.Build.props"))
$version = @($xml.Project.PropertyGroup.Version | Where-Object { $_ }) | Select-Object -First 1

# AppIds are permanent: changing one makes an upgrade install side by side with the old version.
$apps = @{
    ReportDesigner = @{ Title = "Majorsilence Report Designer"; Guid = "976347b5-3dc4-4e08-a7bd-ef3cc09c1ace" }
    RdlCmd         = @{ Title = "Majorsilence RdlCmd"; Guid = "c1f0a6de-5b0e-4a7c-9d51-3e8a2f6b7c14"; Cli = 1 }
    RdlReader      = @{ Title = "Majorsilence Report Reader";   Guid = "2d834aa4-110e-4195-9776-bf5c79a746c5" }
}[$App]

$publishDir = Join-Path $root "Release-Builds" "installer-work" "$App-$Rid"
$outDir = Join-Path $root "Release-Builds" "build-output" "installers"
Remove-Item $publishDir -Recurse -Force -ErrorAction Ignore
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

dotnet publish (Join-Path $root $App) -c Release -r $Rid -f net10.0 --self-contained true `
    -p:GeneratePackageOnBuild=false -o $publishDir

$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if ($iscc) { $iscc = $iscc.Source } else { $iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" }

$isccArgs = @()
if ($apps.Cli -eq 1) { $isccArgs += "/DAppIsCli=1" }
& $iscc @isccArgs "/DAppName=$App" "/DAppTitle=$($apps.Title)" "/DAppGuid=$($apps.Guid)" "/DAppVersion=$version" `
    "/DRid=$Rid" "/DPublishDir=$publishDir" "/DOutDir=$outDir" (Join-Path $PSScriptRoot "app.iss")
Get-ChildItem $outDir
