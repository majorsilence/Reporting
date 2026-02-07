#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"
$CURRENTPATH=$pwd.Path

# /p:Configuration="Debug", "Debug-DrawingCompat", "Release", "Release-DrawingCompat"
$pConfiguration="Release"
$pConfigurationCompat="Release-DrawingCompat"
$pTargetFramework="net8.0-windows"
$pTargetFrameworkGeneric="net8.0"

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
Write-Host  $Version

dotnet restore "./MajorsilenceReporting.sln"
# ************* Begin anycpu *********************************************
dotnet build "$CURRENTPATH\MajorsilenceReporting.sln" --configuration Release-DrawingCompat --verbosity minimal
dotnet publish RdlCmd -c Release-DrawingCompat -r linux-x64 -f net8.0 --self-contained true #-p:PublishSingleFile=true
dotnet publish RdlCmd -c Release-DrawingCompat -r linux-arm64 -f net8.0 --self-contained true #-p:PublishSingleFile=true
dotnet publish RdlCmd -c Release-DrawingCompat -r osx-x64 -f net8.0 --self-contained true #-p:PublishSingleFile=true
dotnet publish RdlCmd -c Release-DrawingCompat -r osx-arm64 -f net8.0 --self-contained true #-p:PublishSingleFile=true

dotnet build "$CURRENTPATH\MajorsilenceReporting.sln" --configuration $pConfiguration --verbosity minimal
dotnet publish RdlCmd -c Release -r win-x64 -f net8.0 --self-contained true #-p:PublishSingleFile=true
dotnet publish RdlCmd -c Release -r win-arm64 -f net8.0 --self-contained true #-p:PublishSingleFile=true

$buildoutputpath_designer="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-designer-$pTargetFramework-anycpu"
$buildoutputpath_desktop="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-desktop-$pTargetFrameworkGeneric-anycpu"
$buildoutputpath_rdlcmd="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-rdlcmd-$pTargetFrameworkGeneric-anycpu"
$buildoutputpath_rdlcmd_selfcontained="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-rdlcmd-self-contained"
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
Remove-Item "$buildoutputpath_reader" -Recurse -ErrorAction Ignore
mkdir "$buildoutputpath_reader"
Remove-Item "$buildoutputpath_mapfile" -Recurse -ErrorAction Ignore
mkdir "$buildoutputpath_mapfile"

Copy-Item .\ReportDesigner\bin\$pConfiguration\$pTargetFramework\ -Destination "$buildoutputpath_designer\" -Recurse
Copy-Item .\RdlDesign\App.ico -Destination "$buildoutputpath_designer\" -Recurse

Copy-Item .\RdlDesktop\bin\$pConfiguration\$pTargetFrameworkGeneric\ -Destination "$buildoutputpath_desktop\" -Recurse
Copy-Item .\RdlCmd\bin\$pConfiguration\$pTargetFrameworkGeneric\ -Destination "$buildoutputpath_rdlcmd\" -Recurse


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

Copy-Item .\RdlCmd\bin\$pConfiguration\$pTargetFrameworkGeneric\win-x64\publish -Destination "$rdlcmd_win" -Recurse
Copy-Item .\RdlCmd\bin\$pConfiguration\$pTargetFrameworkGeneric\win-arm64\publish -Destination "$rdlcmd_win_arm64" -Recurse
Copy-Item .\RdlCmd\bin\$pConfigurationCompat\$pTargetFrameworkGeneric\linux-x64\publish -Destination "$rdlcmd_linux" -Recurse
Copy-Item .\RdlCmd\bin\$pConfigurationCompat\$pTargetFrameworkGeneric\linux-arm64\publish -Destination "$rdlcmd_linux_arm64" -Recurse
Copy-Item .\RdlCmd\bin\$pConfigurationCompat\$pTargetFrameworkGeneric\osx-x64\publish -Destination "$rdlcmd_osx" -Recurse
Copy-Item .\RdlCmd\bin\$pConfigurationCompat\$pTargetFrameworkGeneric\osx-arm64\publish -Destination "$rdlcmd_osx_arm64" -Recurse
Copy-Item .\RdlReader\bin\$pConfiguration\$pTargetFramework\ -Destination "$buildoutputpath_reader\" -Recurse
Copy-Item .\RdlMapFile\bin\$pConfiguration\$pTargetFramework\ -Destination "$buildoutputpath_mapfile\" -Recurse

cd Release-Builds
cd build-output	
..\7za.exe a -tzip $Version-majorsilence-reporting-designer-$pTargetFramework-anycpu.zip majorsilence-reporting-designer-$pTargetFramework-anycpu\
..\7za.exe a -tzip $Version-majorsilence-reporting-desktop-$pTargetFrameworkGeneric-anycpu.zip majorsilence-reporting-desktop-$pTargetFrameworkGeneric-anycpu\
..\7za.exe a -tzip $Version-majorsilence-reporting-mapfile-$pTargetFrameworkGeneric-anycpu.zip majorsilence-reporting-mapfile-$pTargetFrameworkGeneric-anycpu\

..\7za.exe a -tzip "$Version-majorsilence-reporting-rdlcmd-$pTargetFrameworkGeneric-anycpu.zip" `
  -x!"majorsilence-reporting-rdlcmd-$pTargetFrameworkGeneric-anycpu\$pTargetFrameworkGeneric\win-arm64\" `
  -x!"majorsilence-reporting-rdlcmd-$pTargetFrameworkGeneric-anycpu\$pTargetFrameworkGeneric\win-x64\" `
  "majorsilence-reporting-rdlcmd-$pTargetFrameworkGeneric-anycpu\"


..\7za.exe a -tzip $Version-majorsilence-reporting-reader-$pTargetFramework-anycpu.zip majorsilence-reporting-reader-$pTargetFramework-anycpu\
..\7za.exe a -tzip $Version-majorsilence-reporting-rdlcmd-self-contained.zip majorsilence-reporting-rdlcmd-self-contained\
cd "$CURRENTPATH"


# ************* End anycpu *********************************************


# ************* Begin PHP *********************************************
$buildoutputpath_php="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-php"
delete_files "$buildoutputpath_php"
mkdir "$buildoutputpath_php"

Copy-Item ".\LanguageWrappers\php\report.php" "$buildoutputpath_php\report.php"

cd Release-Builds
cd build-output	
..\7za.exe a -tzip $Version-majorsilence-reporting-build-php.zip majorsilence-reporting-php\
cd "$CURRENTPATH"

# ************* End PHP *********************************************



# ************* Begin Python *********************************************
$buildoutputpath_python="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-python"
delete_files "$buildoutputpath_python"
mkdir "$buildoutputpath_python"

Copy-Item ".\LanguageWrappers\python\report.py" "$buildoutputpath_python\report.py"

cd Release-Builds
cd build-output	
..\7za.exe a -tzip $Version-majorsilence-reporting-python.zip majorsilence-reporting-python\
cd "$CURRENTPATH"
# ************* End Python *********************************************


# ************* Begin Ruby *********************************************
$buildoutputpath_ruby="$CURRENTPATH\Release-Builds\build-output\majorsilence-reporting-ruby"
delete_files "$buildoutputpath_ruby"
mkdir "$buildoutputpath_ruby"

Copy-Item ".\LanguageWrappers\ruby\report.rb" "$buildoutputpath_ruby\report.rb"

cd Release-Builds
cd build-output	
..\7za.exe a -tzip $Version-majorsilence-reporting-ruby.zip majorsilence-reporting-ruby\
cd "$CURRENTPATH"

# ************* End Ruby *********************************************


# ************* Nuget ************************************************
Get-ChildItem -Recurse -Exclude "$CURRENTPATH\Release-Builds\build-output" .\*.nupkg | Copy-Item -Destination "$CURRENTPATH\Release-Builds\build-output" -Force -ErrorAction SilentlyContinue
Get-ChildItem -Recurse -Exclude "$CURRENTPATH\Release-Builds\build-output" .\*.snupkg | Copy-Item -Destination "$CURRENTPATH\Release-Builds\build-output" -Force -ErrorAction SilentlyContinue


Write-Output "Publish nuget packages with commands"
Write-Output "dotnet nuget push $CURRENTPATH\Release-Builds\build-output\*.nupkg -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json --skip-duplicate"
Write-Output "dotnet nuget push $CURRENTPATH\Release-Builds\build-output\*.snupkg -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json --skip-duplicate"
