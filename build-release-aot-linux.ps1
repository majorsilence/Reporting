#!/usr/bin/env pwsh
# AOT builds for Linux — run this on a Linux runner/machine.
$ErrorActionPreference = "Stop"
$PSNativeCommandErrorActionPreference = 'Stop'
$CURRENTPATH=$pwd.Path

$pConfigurationCompat="Release-DrawingCompat"
$pTargetFrameworkGeneric="net10.0"

function GetVersions([ref]$theVersion)
{
	$csprojPath = Join-Path $CURRENTPATH "Directory.Build.props"
	$xml = [xml](Get-Content $csprojPath)
	$theVersion.Value = $xml.Project.PropertyGroup.Version
}

$Version=""
GetVersions([ref]$Version)
Write-Host $Version

if ($env:GITHUB_OUTPUT) {
    "version=$Version" | Out-File -Append -FilePath $env:GITHUB_OUTPUT
}

dotnet publish RdlCmd -c Release-DrawingCompat -r linux-x64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd/bin/$pConfigurationCompat/$pTargetFrameworkGeneric/linux-x64-aot/publish"
dotnet publish RdlCmd -c Release-DrawingCompat -r linux-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd/bin/$pConfigurationCompat/$pTargetFrameworkGeneric/linux-arm64-aot/publish"

dotnet publish RdlNative -c Release-DrawingCompat -r linux-x64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative/bin/$pConfigurationCompat/$pTargetFrameworkGeneric/linux-x64-aot/publish"
dotnet publish RdlNative -c Release-DrawingCompat -r linux-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative/bin/$pConfigurationCompat/$pTargetFrameworkGeneric/linux-arm64-aot/publish"

$buildoutputpath_rdlcmd_aot = Join-Path $CURRENTPATH "Release-Builds" "build-output" "majorsilence-reporting-rdlcmd-aot"
$buildoutputpath_rdlnative  = Join-Path $CURRENTPATH "Release-Builds" "build-output" "majorsilence-reporting-rdlnative"

Remove-Item $buildoutputpath_rdlcmd_aot -Recurse -ErrorAction Ignore
mkdir $buildoutputpath_rdlcmd_aot

$rdlcmd_linux_aot       = Join-Path $buildoutputpath_rdlcmd_aot "linux-x64"
$rdlcmd_linux_arm64_aot = Join-Path $buildoutputpath_rdlcmd_aot "linux-arm64"
mkdir $rdlcmd_linux_aot
mkdir $rdlcmd_linux_arm64_aot

Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "linux-x64-aot" "publish")   -Destination $rdlcmd_linux_aot       -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "linux-arm64-aot" "publish") -Destination $rdlcmd_linux_arm64_aot  -Recurse

Remove-Item $buildoutputpath_rdlnative -Recurse -ErrorAction Ignore
mkdir $buildoutputpath_rdlnative

$rdlnative_linux_x64   = Join-Path $buildoutputpath_rdlnative "linux-x64"
$rdlnative_linux_arm64 = Join-Path $buildoutputpath_rdlnative "linux-arm64"
mkdir $rdlnative_linux_x64
mkdir $rdlnative_linux_arm64

Copy-Item (Join-Path $CURRENTPATH "RdlNative" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "linux-x64-aot" "publish")   -Destination $rdlnative_linux_x64   -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlNative" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "linux-arm64-aot" "publish")  -Destination $rdlnative_linux_arm64  -Recurse

Copy-Item (Join-Path $CURRENTPATH "RdlNative" "rdlnative.h") (Join-Path $buildoutputpath_rdlnative "rdlnative.h")

$7zaExclude = "-xr!*.pdb", "-xr!*.dbg"
$buildOutputDir = Join-Path $CURRENTPATH "Release-Builds" "build-output"

Set-Location $buildOutputDir
7z a -tzip "$Version-majorsilence-reporting-rdlcmd-aot-linux.zip"    @7zaExclude "majorsilence-reporting-rdlcmd-aot/"
7z a -tzip "$Version-majorsilence-reporting-rdlnative-linux.zip"     @7zaExclude "majorsilence-reporting-rdlnative/"
Set-Location $CURRENTPATH
