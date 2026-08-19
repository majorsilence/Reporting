#!/usr/bin/env pwsh
# AOT builds for macOS — run this on a macOS runner/machine.
$ErrorActionPreference = "Stop"
# Correct name is $PSNativeCommandUseErrorActionPreference; the 'Stop'-valued misspelling below
# was a no-op, so a failing dotnet build let the script carry on and fail later somewhere odd.
$PSNativeCommandUseErrorActionPreference = $true
$CURRENTPATH=$pwd.Path

$pConfigurationCompat="Release-DrawingCompat"
$pTargetFrameworkGeneric="net10.0"

function GetVersions([ref]$theVersion)
{
	$csprojPath = Join-Path $CURRENTPATH "Directory.Build.props"
	$xml = [xml](Get-Content $csprojPath)
	# Directory.Build.props has several PropertyGroups; only one carries Version, so the rest
	# contribute empty entries that would stringify into a leading-whitespace version.
	$theVersion.Value = @($xml.Project.PropertyGroup.Version | Where-Object { $_ }) | Select-Object -First 1
}

$Version=""
GetVersions([ref]$Version)
Write-Host $Version

if ($env:GITHUB_OUTPUT) {
    "version=$Version" | Out-File -Append -FilePath $env:GITHUB_OUTPUT
}

dotnet publish RdlCmd -c Release-DrawingCompat -r osx-x64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd/bin/$pConfigurationCompat/$pTargetFrameworkGeneric/osx-x64-aot/publish"
dotnet publish RdlCmd -c Release-DrawingCompat -r osx-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd/bin/$pConfigurationCompat/$pTargetFrameworkGeneric/osx-arm64-aot/publish"

dotnet publish RdlNative -c Release-DrawingCompat -r osx-x64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative/bin/$pConfigurationCompat/$pTargetFrameworkGeneric/osx-x64-aot/publish"
dotnet publish RdlNative -c Release-DrawingCompat -r osx-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative/bin/$pConfigurationCompat/$pTargetFrameworkGeneric/osx-arm64-aot/publish"

dotnet publish PdfNative -c Release-DrawingCompat -r osx-x64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "PdfNative/bin/$pConfigurationCompat/$pTargetFrameworkGeneric/osx-x64-aot/publish"
dotnet publish PdfNative -c Release-DrawingCompat -r osx-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "PdfNative/bin/$pConfigurationCompat/$pTargetFrameworkGeneric/osx-arm64-aot/publish"

$buildoutputpath_rdlcmd_aot = Join-Path $CURRENTPATH "Release-Builds" "build-output" "majorsilence-reporting-rdlcmd-aot"
$buildoutputpath_rdlnative  = Join-Path $CURRENTPATH "Release-Builds" "build-output" "majorsilence-reporting-rdlnative"
$buildoutputpath_pdfnative  = Join-Path $CURRENTPATH "Release-Builds" "build-output" "majorsilence-pdfnative"

Remove-Item $buildoutputpath_rdlcmd_aot -Recurse -ErrorAction Ignore
mkdir $buildoutputpath_rdlcmd_aot

$rdlcmd_osx_aot       = Join-Path $buildoutputpath_rdlcmd_aot "osx-x64"
$rdlcmd_osx_arm64_aot = Join-Path $buildoutputpath_rdlcmd_aot "osx-arm64"
mkdir $rdlcmd_osx_aot
mkdir $rdlcmd_osx_arm64_aot

Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "osx-x64-aot" "publish")   -Destination $rdlcmd_osx_aot       -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "osx-arm64-aot" "publish") -Destination $rdlcmd_osx_arm64_aot  -Recurse

Remove-Item $buildoutputpath_rdlnative -Recurse -ErrorAction Ignore
mkdir $buildoutputpath_rdlnative

$rdlnative_osx_x64   = Join-Path $buildoutputpath_rdlnative "osx-x64"
$rdlnative_osx_arm64 = Join-Path $buildoutputpath_rdlnative "osx-arm64"
mkdir $rdlnative_osx_x64
mkdir $rdlnative_osx_arm64

Copy-Item (Join-Path $CURRENTPATH "RdlNative" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "osx-x64-aot" "publish")   -Destination $rdlnative_osx_x64   -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlNative" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "osx-arm64-aot" "publish")  -Destination $rdlnative_osx_arm64  -Recurse

Copy-Item (Join-Path $CURRENTPATH "RdlNative" "rdlnative.h") (Join-Path $buildoutputpath_rdlnative "rdlnative.h")

Remove-Item $buildoutputpath_pdfnative -Recurse -ErrorAction Ignore
mkdir $buildoutputpath_pdfnative

$pdfnative_osx_x64   = Join-Path $buildoutputpath_pdfnative "osx-x64"
$pdfnative_osx_arm64 = Join-Path $buildoutputpath_pdfnative "osx-arm64"
mkdir $pdfnative_osx_x64
mkdir $pdfnative_osx_arm64

Copy-Item (Join-Path $CURRENTPATH "PdfNative" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "osx-x64-aot" "publish")   -Destination $pdfnative_osx_x64   -Recurse
Copy-Item (Join-Path $CURRENTPATH "PdfNative" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "osx-arm64-aot" "publish")  -Destination $pdfnative_osx_arm64  -Recurse

Copy-Item (Join-Path $CURRENTPATH "PdfNative" "pdfnative.h") (Join-Path $buildoutputpath_pdfnative "pdfnative.h")

$7zaExclude = "-xr!*.pdb", "-xr!*.dbg"
$buildOutputDir = Join-Path $CURRENTPATH "Release-Builds" "build-output"

Set-Location $buildOutputDir
7z a -tzip "$Version-majorsilence-reporting-rdlcmd-aot-osx.zip"    @7zaExclude "majorsilence-reporting-rdlcmd-aot/"
7z a -tzip "$Version-majorsilence-reporting-rdlnative-osx.zip"     @7zaExclude "majorsilence-reporting-rdlnative/"
7z a -tzip "$Version-majorsilence-pdfnative-osx.zip"               @7zaExclude "majorsilence-pdfnative/"
Set-Location $CURRENTPATH
