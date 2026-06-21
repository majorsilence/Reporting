#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"
$PSNativeCommandErrorActionPreference = 'Stop'
$CURRENTPATH=$pwd.Path

# /p:Configuration="Debug", "Debug-DrawingCompat", "Release", "Release-DrawingCompat"
$pConfiguration="Release"
$pConfigurationCompat="Release-DrawingCompat"
$pTargetFramework="net10.0-windows"
$pTargetFrameworkGeneric="net10.0"

function delete_files([string]$path)
{
	If (Test-Path $path){
		Write-Host "Deleting path $path" -ForegroundColor Green
		Remove-Item -recurse -force $path
	}
}

function GetVersions([ref]$theVersion)
{
	$csprojPath = Join-Path $CURRENTPATH ".\Directory.Build.props"
	$xml = [xml](Get-Content $csprojPath)
	$theVersion.Value = $xml.Project.PropertyGroup.Version
}

Get-ChildItem .\ -include bin,obj,build-output -Recurse | foreach ($_) { remove-item $_.fullname -Force -Recurse }

$Version=""
GetVersions([ref]$Version)
Write-Host $Version

if ($env:GITHUB_OUTPUT) {
    "version=$Version" | Out-File -Append -FilePath $env:GITHUB_OUTPUT
}

$solutionPath = Join-Path $CURRENTPATH "MajorsilenceReporting.sln"
dotnet restore $solutionPath
# ************* Begin anycpu *********************************************
dotnet build $solutionPath --configuration Release-DrawingCompat --verbosity minimal -p:GeneratePackageOnBuild=false
dotnet publish RdlCmd -c Release-DrawingCompat -r linux-x64 -f $pTargetFrameworkGeneric --self-contained true -p:GeneratePackageOnBuild=false #-p:PublishSingleFile=true
dotnet publish RdlCmd -c Release-DrawingCompat -r linux-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:GeneratePackageOnBuild=false #-p:PublishSingleFile=true
dotnet publish RdlCmd -c Release-DrawingCompat -r osx-x64 -f $pTargetFrameworkGeneric --self-contained true -p:GeneratePackageOnBuild=false #-p:PublishSingleFile=true
dotnet publish RdlCmd -c Release-DrawingCompat -r osx-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:GeneratePackageOnBuild=false #-p:PublishSingleFile=true

dotnet build $solutionPath --configuration $pConfiguration --verbosity minimal -p:GeneratePackageOnBuild=false
dotnet publish RdlCmd -c Release -r win-x64 -f $pTargetFrameworkGeneric --self-contained true -p:GeneratePackageOnBuild=false #-p:PublishSingleFile=true
dotnet publish RdlCmd -c Release -r win-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:GeneratePackageOnBuild=false #-p:PublishSingleFile=true

# AOT native self-contained builds (output to separate -aot dirs to avoid overwriting the framework-dependent publish above)
# -p:PublishAot=true propagates to all referenced projects; clearing TargetFrameworks forces single-TFM mode across
# the whole build graph so net48 entries in referenced projects don't trigger NETSDK1207.
# Note: cross-platform AOT (Linux/macOS from Windows) requires the respective cross-compilation toolchain.
# In CI, run these on each target platform's runner instead.
dotnet publish RdlCmd -c Release-DrawingCompat -r linux-x64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd\bin\$pConfigurationCompat\$pTargetFrameworkGeneric\linux-x64-aot\publish"
dotnet publish RdlCmd -c Release-DrawingCompat -r linux-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd\bin\$pConfigurationCompat\$pTargetFrameworkGeneric\linux-arm64-aot\publish"
dotnet publish RdlCmd -c Release-DrawingCompat -r osx-x64     -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd\bin\$pConfigurationCompat\$pTargetFrameworkGeneric\osx-x64-aot\publish"
dotnet publish RdlCmd -c Release-DrawingCompat -r osx-arm64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd\bin\$pConfigurationCompat\$pTargetFrameworkGeneric\osx-arm64-aot\publish"
dotnet publish RdlCmd -c Release               -r win-x64     -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd\bin\$pConfiguration\$pTargetFrameworkGeneric\win-x64-aot\publish"
dotnet publish RdlCmd -c Release               -r win-arm64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlCmd\bin\$pConfiguration\$pTargetFrameworkGeneric\win-arm64-aot\publish"

# AOT native shared library builds for FFI use (Python/ctypes, PHP/FFI, Ruby/Fiddle)
dotnet publish RdlNative -c Release-DrawingCompat -r linux-x64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative\bin\$pConfigurationCompat\$pTargetFrameworkGeneric\linux-x64-aot\publish"
dotnet publish RdlNative -c Release-DrawingCompat -r linux-arm64 -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative\bin\$pConfigurationCompat\$pTargetFrameworkGeneric\linux-arm64-aot\publish"
dotnet publish RdlNative -c Release-DrawingCompat -r osx-x64     -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative\bin\$pConfigurationCompat\$pTargetFrameworkGeneric\osx-x64-aot\publish"
dotnet publish RdlNative -c Release-DrawingCompat -r osx-arm64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative\bin\$pConfigurationCompat\$pTargetFrameworkGeneric\osx-arm64-aot\publish"
dotnet publish RdlNative -c Release               -r win-x64     -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative\bin\$pConfiguration\$pTargetFrameworkGeneric\win-x64-aot\publish"
dotnet publish RdlNative -c Release               -r win-arm64   -f $pTargetFrameworkGeneric --self-contained true -p:PublishAot=true -p:GeneratePackageOnBuild=false -o "RdlNative\bin\$pConfiguration\$pTargetFrameworkGeneric\win-arm64-aot\publish"

$buildoutputpath_designer="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-designer-$pTargetFramework-anycpu"
$buildoutputpath_desktop="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-desktop-$pTargetFrameworkGeneric-anycpu"
$buildoutputpath_rdlcmd="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-rdlcmd-$pTargetFrameworkGeneric-anycpu"
$buildoutputpath_rdlcmd_selfcontained="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-rdlcmd-self-contained"
$buildoutputpath_rdlcmd_aot="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-rdlcmd-aot"
$buildoutputpath_rdlnative="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-rdlnative"
$buildoutputpath_reader="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-reader-$pTargetFramework-anycpu"
$buildoutputpath_mapfile="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-mapfile-$pTargetFramework-anycpu"

Remove-Item "$buildoutputpath_designer" -Recurse -ErrorAction Ignore
mkdir "$buildoutputpath_designer"
Remove-Item "$buildoutputpath_desktop" -Recurse -ErrorAction Ignore
mkdir "$buildoutputpath_desktop"
Remove-Item "$buildoutputpath_rdlcmd" -Recurse -ErrorAction Ignore
mkdir "$buildoutputpath_rdlcmd"
Remove-Item "$buildoutputpath_rdlcmd_selfcontained" -Recurse -ErrorAction Ignore
mkdir "$buildoutputpath_rdlcmd_selfcontained"
Remove-Item "$buildoutputpath_rdlcmd_aot" -Recurse -ErrorAction Ignore
mkdir "$buildoutputpath_rdlcmd_aot"
Remove-Item "$buildoutputpath_rdlnative" -Recurse -ErrorAction Ignore
mkdir "$buildoutputpath_rdlnative"
Remove-Item "$buildoutputpath_reader" -Recurse -ErrorAction Ignore
mkdir "$buildoutputpath_reader"
Remove-Item "$buildoutputpath_mapfile" -Recurse -ErrorAction Ignore
mkdir "$buildoutputpath_mapfile"

Copy-Item (Join-Path $CURRENTPATH "ReportDesigner" "bin" $pConfiguration $pTargetFramework) -Destination "$buildoutputpath_designer\" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlDesign" "App.ico") -Destination "$buildoutputpath_designer\" -Recurse

Copy-Item (Join-Path $CURRENTPATH "RdlDesktop" "bin" $pConfiguration $pTargetFrameworkGeneric) -Destination "$buildoutputpath_desktop\" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfiguration $pTargetFrameworkGeneric) -Destination "$buildoutputpath_rdlcmd\" -Recurse


$rdlcmd_win="$buildoutputpath_rdlcmd_selfcontained\win-x64"
$rdlcmd_linux="$buildoutputpath_rdlcmd_selfcontained\linux-x64"
$rdlcmd_osx="$buildoutputpath_rdlcmd_selfcontained\osx-x64"
$rdlcmd_win_arm64="$buildoutputpath_rdlcmd_selfcontained\win-arm64"
$rdlcmd_linux_arm64="$buildoutputpath_rdlcmd_selfcontained\linux-arm64"
$rdlcmd_osx_arm64="$buildoutputpath_rdlcmd_selfcontained\osx-arm64"
mkdir "$rdlcmd_win"
mkdir "$rdlcmd_linux"
mkdir "$rdlcmd_osx"
mkdir "$rdlcmd_win_arm64"
mkdir "$rdlcmd_linux_arm64"
mkdir "$rdlcmd_osx_arm64"

Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfiguration $pTargetFrameworkGeneric "win-x64" "publish") -Destination "$rdlcmd_win" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfiguration $pTargetFrameworkGeneric "win-arm64" "publish") -Destination "$rdlcmd_win_arm64" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "linux-x64" "publish") -Destination "$rdlcmd_linux" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "linux-arm64" "publish") -Destination "$rdlcmd_linux_arm64" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "osx-x64" "publish") -Destination "$rdlcmd_osx" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "osx-arm64" "publish") -Destination "$rdlcmd_osx_arm64" -Recurse

$rdlcmd_win_aot="$buildoutputpath_rdlcmd_aot\win-x64"
$rdlcmd_linux_aot="$buildoutputpath_rdlcmd_aot\linux-x64"
$rdlcmd_osx_aot="$buildoutputpath_rdlcmd_aot\osx-x64"
$rdlcmd_win_arm64_aot="$buildoutputpath_rdlcmd_aot\win-arm64"
$rdlcmd_linux_arm64_aot="$buildoutputpath_rdlcmd_aot\linux-arm64"
$rdlcmd_osx_arm64_aot="$buildoutputpath_rdlcmd_aot\osx-arm64"
mkdir "$rdlcmd_win_aot"
mkdir "$rdlcmd_linux_aot"
mkdir "$rdlcmd_osx_aot"
mkdir "$rdlcmd_win_arm64_aot"
mkdir "$rdlcmd_linux_arm64_aot"
mkdir "$rdlcmd_osx_arm64_aot"

Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfiguration $pTargetFrameworkGeneric "win-x64-aot" "publish") -Destination "$rdlcmd_win_aot" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfiguration $pTargetFrameworkGeneric "win-arm64-aot" "publish") -Destination "$rdlcmd_win_arm64_aot" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "linux-x64-aot" "publish") -Destination "$rdlcmd_linux_aot" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "linux-arm64-aot" "publish") -Destination "$rdlcmd_linux_arm64_aot" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "osx-x64-aot" "publish") -Destination "$rdlcmd_osx_aot" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlCmd" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "osx-arm64-aot" "publish") -Destination "$rdlcmd_osx_arm64_aot" -Recurse

# Native shared library (FFI) — one per platform sub-directory
$rdlnative_linux_x64    ="$buildoutputpath_rdlnative\linux-x64"
$rdlnative_linux_arm64  ="$buildoutputpath_rdlnative\linux-arm64"
$rdlnative_osx_x64      ="$buildoutputpath_rdlnative\osx-x64"
$rdlnative_osx_arm64    ="$buildoutputpath_rdlnative\osx-arm64"
$rdlnative_win_x64      ="$buildoutputpath_rdlnative\win-x64"
$rdlnative_win_arm64    ="$buildoutputpath_rdlnative\win-arm64"
mkdir "$rdlnative_linux_x64"
mkdir "$rdlnative_linux_arm64"
mkdir "$rdlnative_osx_x64"
mkdir "$rdlnative_osx_arm64"
mkdir "$rdlnative_win_x64"
mkdir "$rdlnative_win_arm64"

Copy-Item (Join-Path $CURRENTPATH "RdlNative" "bin" $pConfiguration $pTargetFrameworkGeneric "win-x64-aot" "publish") -Destination "$rdlnative_win_x64" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlNative" "bin" $pConfiguration $pTargetFrameworkGeneric "win-arm64-aot" "publish") -Destination "$rdlnative_win_arm64" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlNative" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "linux-x64-aot" "publish") -Destination "$rdlnative_linux_x64" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlNative" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "linux-arm64-aot" "publish") -Destination "$rdlnative_linux_arm64" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlNative" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "osx-x64-aot" "publish") -Destination "$rdlnative_osx_x64" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlNative" "bin" $pConfigurationCompat $pTargetFrameworkGeneric "osx-arm64-aot" "publish") -Destination "$rdlnative_osx_arm64" -Recurse

# C header and language wrappers at the root of the native package
Copy-Item ".\RdlNative\rdlnative.h"                          "$buildoutputpath_rdlnative\rdlnative.h"
Copy-Item ".\LanguageWrappers\python\report_native.py"        "$buildoutputpath_rdlnative\report_native.py"
Copy-Item ".\LanguageWrappers\php\report_native.php"          "$buildoutputpath_rdlnative\report_native.php"
Copy-Item ".\LanguageWrappers\ruby\report_native.rb"          "$buildoutputpath_rdlnative\report_native.rb"

Copy-Item (Join-Path $CURRENTPATH "RdlReader" "bin" $pConfiguration $pTargetFramework) -Destination "$buildoutputpath_reader\" -Recurse
Copy-Item (Join-Path $CURRENTPATH "RdlMapFile" "bin" $pConfiguration $pTargetFramework) -Destination "$buildoutputpath_mapfile\" -Recurse

# Exclude debug symbols from all release zips
$7zaExclude = "-xr!*.pdb", "-xr!*.dbg"

cd Release-Builds
cd build-output
..\7za.exe a -tzip $Version-majorsilence-reporting-designer-$pTargetFramework-anycpu.zip @7zaExclude majorsilence-reporting-designer-$pTargetFramework-anycpu\
..\7za.exe a -tzip $Version-majorsilence-reporting-desktop-$pTargetFrameworkGeneric-anycpu.zip @7zaExclude majorsilence-reporting-desktop-$pTargetFrameworkGeneric-anycpu\
..\7za.exe a -tzip $Version-majorsilence-reporting-mapfile-$pTargetFrameworkGeneric-anycpu.zip @7zaExclude majorsilence-reporting-mapfile-$pTargetFrameworkGeneric-anycpu\

..\7za.exe a -tzip "$Version-majorsilence-reporting-rdlcmd-$pTargetFrameworkGeneric-anycpu.zip" `
  -x!"majorsilence-reporting-rdlcmd-$pTargetFrameworkGeneric-anycpu\$pTargetFrameworkGeneric\win-arm64\" `
  -x!"majorsilence-reporting-rdlcmd-$pTargetFrameworkGeneric-anycpu\$pTargetFrameworkGeneric\win-x64\" `
  @7zaExclude `
  "majorsilence-reporting-rdlcmd-$pTargetFrameworkGeneric-anycpu\"


..\7za.exe a -tzip $Version-majorsilence-reporting-reader-$pTargetFramework-anycpu.zip @7zaExclude majorsilence-reporting-reader-$pTargetFramework-anycpu\
..\7za.exe a -tzip $Version-majorsilence-reporting-rdlcmd-self-contained.zip @7zaExclude majorsilence-reporting-rdlcmd-self-contained\
..\7za.exe a -tzip $Version-majorsilence-reporting-rdlcmd-aot.zip @7zaExclude majorsilence-reporting-rdlcmd-aot\
..\7za.exe a -tzip $Version-majorsilence-reporting-rdlnative.zip @7zaExclude majorsilence-reporting-rdlnative\
cd "$CURRENTPATH"


# ************* End anycpu *********************************************


# ************* Begin PHP *********************************************
$buildoutputpath_php="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-php"
delete_files "$buildoutputpath_php"
mkdir "$buildoutputpath_php"

Copy-Item ".\LanguageWrappers\php\report.php"        "$buildoutputpath_php\report.php"
Copy-Item ".\LanguageWrappers\php\report_aot.php"    "$buildoutputpath_php\report_aot.php"
Copy-Item ".\LanguageWrappers\php\report_native.php" "$buildoutputpath_php\report_native.php"

cd Release-Builds
cd build-output
..\7za.exe a -tzip $Version-majorsilence-reporting-build-php.zip majorsilence-reporting-php\
cd "$CURRENTPATH"

# ************* End PHP *********************************************



# ************* Begin Python *********************************************
$buildoutputpath_python="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-python"
delete_files "$buildoutputpath_python"
mkdir "$buildoutputpath_python"

Copy-Item ".\LanguageWrappers\python\report.py"        "$buildoutputpath_python\report.py"
Copy-Item ".\LanguageWrappers\python\report_aot.py"    "$buildoutputpath_python\report_aot.py"
Copy-Item ".\LanguageWrappers\python\report_native.py" "$buildoutputpath_python\report_native.py"

cd Release-Builds
cd build-output
..\7za.exe a -tzip $Version-majorsilence-reporting-python.zip majorsilence-reporting-python\
cd "$CURRENTPATH"
# ************* End Python *********************************************


# ************* Begin Ruby *********************************************
$buildoutputpath_ruby="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-ruby"
delete_files "$buildoutputpath_ruby"
mkdir "$buildoutputpath_ruby"

Copy-Item ".\LanguageWrappers\ruby\report.rb"        "$buildoutputpath_ruby\report.rb"
Copy-Item ".\LanguageWrappers\ruby\report_aot.rb"    "$buildoutputpath_ruby\report_aot.rb"
Copy-Item ".\LanguageWrappers\ruby\report_native.rb" "$buildoutputpath_ruby\report_native.rb"

cd Release-Builds
cd build-output
..\7za.exe a -tzip $Version-majorsilence-reporting-ruby.zip majorsilence-reporting-ruby\
cd "$CURRENTPATH"

# ************* End Ruby *********************************************


# ************* Nuget ************************************************
$nugetOutputPath = Join-Path $CURRENTPATH "Release-Builds" "build-output"

dotnet build $solutionPath --configuration $pConfigurationCompat --verbosity minimal -p:GeneratePackageOnBuild=false
dotnet pack $solutionPath --configuration $pConfigurationCompat --no-build --output $nugetOutputPath

dotnet build $solutionPath --configuration $pConfiguration --verbosity minimal -p:GeneratePackageOnBuild=false
dotnet pack $solutionPath --configuration $pConfiguration --no-build --output $nugetOutputPath


Write-Output "Publish nuget packages with commands"
Write-Output "dotnet nuget push $CURRENTPATH\Release-Builds\build-output\*.nupkg -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json --skip-duplicate"
Write-Output "dotnet nuget push $CURRENTPATH\Release-Builds\build-output\*.snupkg -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json --skip-duplicate"
