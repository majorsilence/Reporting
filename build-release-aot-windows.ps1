#!/usr/bin/env pwsh
# AOT builds for Windows — run this on a Windows runner/machine.
$ErrorActionPreference = "Stop"
# Correct name is $PSNativeCommandUseErrorActionPreference; the 'Stop'-valued misspelling below
# was a no-op, so a failing dotnet build let the script carry on and fail later somewhere odd.
$PSNativeCommandUseErrorActionPreference = $true
$CURRENTPATH=$pwd.Path

$pConfiguration="Release"
$pTargetFrameworkGeneric="net10.0"

function GetVersions([ref]$theVersion)
{
	$csprojPath = Join-Path $CURRENTPATH ".\Directory.Build.props"
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

$solutionPath = Join-Path $CURRENTPATH "MajorsilenceReporting.slnx"
dotnet restore $solutionPath
dotnet build $solutionPath --configuration $pConfiguration --verbosity minimal -p:GeneratePackageOnBuild=false

dotnet publish RdlCmd -c Release -r win-x64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd\bin\$pConfiguration\$pTargetFrameworkGeneric\win-x64-aot\publish"
dotnet publish RdlCmd -c Release -r win-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd\bin\$pConfiguration\$pTargetFrameworkGeneric\win-arm64-aot\publish"

dotnet publish RdlNative -c Release -r win-x64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative\bin\$pConfiguration\$pTargetFrameworkGeneric\win-x64-aot\publish"
dotnet publish RdlNative -c Release -r win-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative\bin\$pConfiguration\$pTargetFrameworkGeneric\win-arm64-aot\publish"

dotnet publish PdfNative -c Release -r win-x64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "PdfNative\bin\$pConfiguration\$pTargetFrameworkGeneric\win-x64-aot\publish"
dotnet publish PdfNative -c Release -r win-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "PdfNative\bin\$pConfiguration\$pTargetFrameworkGeneric\win-arm64-aot\publish"

# Native AOT verification: publish Majorsilence.Pdf's smoke test as a self-contained AOT
# binary and actually run it (win-x64 only -- arm64 can't run on this runner), not just
# check that it compiled without trim/AOT warnings. Fails the build if any exercised code
# path (text, tables, PdfLayout, AES encryption, PKCS#7 signing, merge) doesn't work under
# real ahead-of-time compilation.
$smokeTestOutDir = "Examples\PdfAotSmokeTest\bin\$pConfiguration\$pTargetFrameworkGeneric\win-x64\publish"
dotnet publish Examples\PdfAotSmokeTest -c Release -r win-x64 -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o $smokeTestOutDir
$smokeTestBinary = Join-Path $CURRENTPATH $smokeTestOutDir "PdfAotSmokeTest.exe"
Write-Host "Running Native AOT smoke test (win-x64)..."
& $smokeTestBinary
if ($LASTEXITCODE -ne 0) {
    throw "Native AOT smoke test failed (exit code $LASTEXITCODE) -- see Examples\PdfAotSmokeTest\Program.cs"
}

$buildoutputpath_rdlcmd_aot = "$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-rdlcmd-aot"
$buildoutputpath_rdlnative  = "$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-rdlnative"
$buildoutputpath_pdfnative  = "$CURRENTPATH\Release-Builds\build-output\majorsilence-pdfnative"

Remove-Item "$buildoutputpath_rdlcmd_aot" -Recurse -ErrorAction Ignore
mkdir "$buildoutputpath_rdlcmd_aot"

$rdlcmd_win_aot       = "$buildoutputpath_rdlcmd_aot\win-x64"
$rdlcmd_win_arm64_aot = "$buildoutputpath_rdlcmd_aot\win-arm64"
mkdir "$rdlcmd_win_aot"
mkdir "$rdlcmd_win_arm64_aot"

Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfiguration $pTargetFrameworkGeneric "win-x64-aot" "publish")   -Destination "$rdlcmd_win_aot"       -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfiguration $pTargetFrameworkGeneric "win-arm64-aot" "publish") -Destination "$rdlcmd_win_arm64_aot"  -Recurse

Remove-Item "$buildoutputpath_rdlnative" -Recurse -ErrorAction Ignore
mkdir "$buildoutputpath_rdlnative"

$rdlnative_win_x64   = "$buildoutputpath_rdlnative\win-x64"
$rdlnative_win_arm64 = "$buildoutputpath_rdlnative\win-arm64"
mkdir "$rdlnative_win_x64"
mkdir "$rdlnative_win_arm64"

Copy-Item (Join-Path $CURRENTPATH "RdlNative" "bin" $pConfiguration $pTargetFrameworkGeneric "win-x64-aot" "publish")   -Destination "$rdlnative_win_x64"   -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlNative" "bin" $pConfiguration $pTargetFrameworkGeneric "win-arm64-aot" "publish")  -Destination "$rdlnative_win_arm64"  -Recurse

Copy-Item ".\RdlNative\rdlnative.h" "$buildoutputpath_rdlnative\rdlnative.h"

Remove-Item "$buildoutputpath_pdfnative" -Recurse -ErrorAction Ignore
mkdir "$buildoutputpath_pdfnative"

$pdfnative_win_x64   = "$buildoutputpath_pdfnative\win-x64"
$pdfnative_win_arm64 = "$buildoutputpath_pdfnative\win-arm64"
mkdir "$pdfnative_win_x64"
mkdir "$pdfnative_win_arm64"

Copy-Item (Join-Path $CURRENTPATH "PdfNative" "bin" $pConfiguration $pTargetFrameworkGeneric "win-x64-aot" "publish")   -Destination "$pdfnative_win_x64"   -Recurse
Copy-Item (Join-Path $CURRENTPATH "PdfNative" "bin" $pConfiguration $pTargetFrameworkGeneric "win-arm64-aot" "publish")  -Destination "$pdfnative_win_arm64"  -Recurse

Copy-Item ".\PdfNative\pdfnative.h" "$buildoutputpath_pdfnative\pdfnative.h"

$7zaExclude = "-xr!*.pdb", "-xr!*.dbg"

$buildOutputDir = Join-Path $CURRENTPATH "Release-Builds" "build-output"
Set-Location $buildOutputDir

foreach ($arch in @("x64", "arm64")) {
    ..\7za.exe a -tzip "$Version-majorsilence-reporting-rdlcmd-aot-windows-$arch.zip"  @7zaExclude "majorsilence-reporting-rdlcmd-aot\win-$arch\"
    ..\7za.exe a -tzip "$Version-majorsilence-reporting-rdlnative-windows-$arch.zip"   @7zaExclude "majorsilence-reporting-rdlnative\win-$arch\" "majorsilence-reporting-rdlnative\rdlnative.h"
    ..\7za.exe a -tzip "$Version-majorsilence-pdfnative-windows-$arch.zip"             @7zaExclude "majorsilence-pdfnative\win-$arch\" "majorsilence-pdfnative\pdfnative.h"
}

Set-Location $CURRENTPATH
