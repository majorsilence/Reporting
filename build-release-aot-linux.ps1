#!/usr/bin/env pwsh
# AOT builds for Linux — run on a Linux runner.
# Pass -Arch x64 (default) or -Arch arm64.
param(
    [ValidateSet("x64","arm64")]
    [string]$Arch = "x64"
)
$ErrorActionPreference = "Stop"
$PSNativeCommandErrorActionPreference = 'Stop'
$CURRENTPATH=$pwd.Path

$pConfigurationCompat="Release-DrawingCompat"
$pTargetFrameworkGeneric="net10.0"
$rid = "linux-$Arch"

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

dotnet publish RdlCmd    -c Release-DrawingCompat -r $rid -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd/bin/$pConfigurationCompat/$pTargetFrameworkGeneric/$rid-aot/publish"
dotnet publish RdlNative -c Release-DrawingCompat -r $rid -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative/bin/$pConfigurationCompat/$pTargetFrameworkGeneric/$rid-aot/publish"
dotnet publish PdfNative -c Release-DrawingCompat -r $rid -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "PdfNative/bin/$pConfigurationCompat/$pTargetFrameworkGeneric/$rid-aot/publish"

# Native AOT verification: publish Majorsilence.Pdf's smoke test as a self-contained AOT
# binary and actually run it, not just check that it compiled without trim/AOT warnings.
# Fails the build if any exercised code path (text, tables, PdfLayout, AES encryption,
# PKCS#7 signing, merge) doesn't work under real ahead-of-time compilation.
$smokeTestOutDir = "Examples/PdfAotSmokeTest/bin/Release/$pTargetFrameworkGeneric/$rid/publish"
dotnet publish Examples/PdfAotSmokeTest -c Release -r $rid -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o $smokeTestOutDir
$smokeTestBinary = Join-Path $CURRENTPATH $smokeTestOutDir "PdfAotSmokeTest"
Write-Host "Running Native AOT smoke test ($rid)..."
& $smokeTestBinary
if ($LASTEXITCODE -ne 0) {
    throw "Native AOT smoke test failed (exit code $LASTEXITCODE) -- see Examples/PdfAotSmokeTest/Program.cs"
}

$buildoutputpath_rdlcmd_aot = Join-Path $CURRENTPATH "Release-Builds" "build-output" "majorsilence-reporting-rdlcmd-aot"
$buildoutputpath_rdlnative  = Join-Path $CURRENTPATH "Release-Builds" "build-output" "majorsilence-reporting-rdlnative"
$buildoutputpath_pdfnative  = Join-Path $CURRENTPATH "Release-Builds" "build-output" "majorsilence-pdfnative"

Remove-Item $buildoutputpath_rdlcmd_aot -Recurse -ErrorAction Ignore
mkdir $buildoutputpath_rdlcmd_aot
$rdlcmd_arch = Join-Path $buildoutputpath_rdlcmd_aot $rid
mkdir $rdlcmd_arch
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "$rid-aot" "publish") -Destination $rdlcmd_arch -Recurse

Remove-Item $buildoutputpath_rdlnative -Recurse -ErrorAction Ignore
mkdir $buildoutputpath_rdlnative
$rdlnative_arch = Join-Path $buildoutputpath_rdlnative $rid
mkdir $rdlnative_arch
Copy-Item (Join-Path $CURRENTPATH "RdlNative" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "$rid-aot" "publish") -Destination $rdlnative_arch -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlNative" "rdlnative.h") (Join-Path $buildoutputpath_rdlnative "rdlnative.h")

Remove-Item $buildoutputpath_pdfnative -Recurse -ErrorAction Ignore
mkdir $buildoutputpath_pdfnative
$pdfnative_arch = Join-Path $buildoutputpath_pdfnative $rid
mkdir $pdfnative_arch
Copy-Item (Join-Path $CURRENTPATH "PdfNative" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "$rid-aot" "publish") -Destination $pdfnative_arch -Recurse
Copy-Item (Join-Path $CURRENTPATH "PdfNative" "pdfnative.h") (Join-Path $buildoutputpath_pdfnative "pdfnative.h")

$7zaExclude = "-xr!*.pdb", "-xr!*.dbg"
$buildOutputDir = Join-Path $CURRENTPATH "Release-Builds" "build-output"

Set-Location $buildOutputDir
7z a -tzip "$Version-majorsilence-reporting-rdlcmd-aot-linux-$Arch.zip"  @7zaExclude "majorsilence-reporting-rdlcmd-aot/"
7z a -tzip "$Version-majorsilence-reporting-rdlnative-linux-$Arch.zip"   @7zaExclude "majorsilence-reporting-rdlnative/"
7z a -tzip "$Version-majorsilence-pdfnative-linux-$Arch.zip"             @7zaExclude "majorsilence-pdfnative/"
Set-Location $CURRENTPATH
