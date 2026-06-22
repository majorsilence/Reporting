#!/usr/bin/env pwsh
# AOT builds for Windows — run this on a Windows runner/machine.
$ErrorActionPreference = "Stop"
$PSNativeCommandErrorActionPreference = 'Stop'
$CURRENTPATH=$pwd.Path

$pConfiguration="Release"
$pTargetFrameworkGeneric="net10.0"

function GetVersions([ref]$theVersion)
{
	$csprojPath = Join-Path $CURRENTPATH ".\Directory.Build.props"
	$xml = [xml](Get-Content $csprojPath)
	$theVersion.Value = $xml.Project.PropertyGroup.Version
}

$Version=""
GetVersions([ref]$Version)
Write-Host $Version

if ($env:GITHUB_OUTPUT) {
    "version=$Version" | Out-File -Append -FilePath $env:GITHUB_OUTPUT
}

$solutionPath = Join-Path $CURRENTPATH "MajorsilenceReporting.sln"
dotnet restore $solutionPath
dotnet build $solutionPath --configuration $pConfiguration --verbosity minimal -p:GeneratePackageOnBuild=false

dotnet publish RdlCmd -c Release -r win-x64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd\bin\$pConfiguration\$pTargetFrameworkGeneric\win-x64-aot\publish"
dotnet publish RdlCmd -c Release -r win-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd\bin\$pConfiguration\$pTargetFrameworkGeneric\win-arm64-aot\publish"

dotnet publish RdlNative -c Release -r win-x64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative\bin\$pConfiguration\$pTargetFrameworkGeneric\win-x64-aot\publish"
dotnet publish RdlNative -c Release -r win-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative\bin\$pConfiguration\$pTargetFrameworkGeneric\win-arm64-aot\publish"

$buildoutputpath_rdlcmd_aot = "$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-rdlcmd-aot"
$buildoutputpath_rdlnative  = "$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-rdlnative"

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

Copy-Item ".\RdlNative\rdlnative.h"                          "$buildoutputpath_rdlnative\rdlnative.h"
Copy-Item ".\LanguageWrappers\python\report_native.py"        "$buildoutputpath_rdlnative\report_native.py"
Copy-Item ".\LanguageWrappers\php\report_native.php"          "$buildoutputpath_rdlnative\report_native.php"
Copy-Item ".\LanguageWrappers\ruby\report_native.rb"          "$buildoutputpath_rdlnative\report_native.rb"

$7zaExclude = "-xr!*.pdb", "-xr!*.dbg"

cd Release-Builds
cd build-output
..\7za.exe a -tzip "$Version-majorsilence-reporting-rdlcmd-aot-windows.zip"   @7zaExclude majorsilence-reporting-rdlcmd-aot\
..\7za.exe a -tzip "$Version-majorsilence-reporting-rdlnative-windows.zip"    @7zaExclude majorsilence-reporting-rdlnative\
cd "$CURRENTPATH"
