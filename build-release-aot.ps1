#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"
$CURRENTPATH=$pwd.Path

# /p:Configuration="Debug", "Debug-DrawingCompat", "Release", "Release-DrawingCompat"
$pConfigurationCompat="Release-DrawingCompat"
$pTargetFrameworkGeneric="net10.0"

Write-Output "Building Majorsilence.Reporting with AOT for RdlCreator..."

dotnet restore "./MajorsilenceReporting.sln"
# ************* Begin anycpu *********************************************
dotnet build "$CURRENTPATH\MajorsilenceReporting.sln" --configuration $pConfigurationCompat --verbosity minimal
#dotnet publish RdlCreator\Majorsilence.Reporting.RdlCreator.csproj -c $pConfigurationCompat -r linux-x64 -f $pTargetFrameworkGeneric -p:PublishAot=true  
#dotnet publish RdlCreator\Majorsilence.Reporting.RdlCreator.csproj -c $pConfigurationCompat -r linux-arm64 -f $pTargetFrameworkGeneric -p:PublishAot=true  
#dotnet publish RdlCreator\Majorsilence.Reporting.RdlCreator.csproj -c $pConfigurationCompat -r osx-x64 -f $pTargetFrameworkGeneric -p:PublishAot=true  
#dotnet publish RdlCreator\Majorsilence.Reporting.RdlCreator.csproj -c $pConfigurationCompat -r osx-arm64 -f $pTargetFrameworkGeneric -p:PublishAot=true  
dotnet publish RdlCreator\Majorsilence.Reporting.RdlCreator.csproj -c $pConfigurationCompat -r win-x64 -f $pTargetFrameworkGeneric -p:PublishAot=true  
#dotnet publish RdlCreator\Majorsilence.Reporting.RdlCreator.csproj -c $pConfigurationCompat -r win-arm64 -f $pTargetFrameworkGeneric -p:PublishAot=true  


Write-Output "Build and AOT publish completed."
Write-Output "Install python library by running 'cd RdlCreator\python_project_root && pip install . '"
Write-Output ""
Write-Output "from rdlcreator import majorsilence"
Write-Output "report = majorsilence.reporting.rdlcreator.Report()"
