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
dotnet publish RdlEngine\Majorsilence.Reporting.RdlEngine.csproj -c $pConfigurationCompat -r win-x64 -f $pTargetFrameworkGeneric -p:PublishAot=true  
dotnet publish RdlCreator\Majorsilence.Reporting.RdlCreator.csproj -c $pConfigurationCompat -r win-x64 -f $pTargetFrameworkGeneric -p:PublishAot=true  
#dotnet publish RdlCreator\Majorsilence.Reporting.RdlCreator.csproj -c $pConfigurationCompat -r win-arm64 -f $pTargetFrameworkGeneric -p:PublishAot=true  


Write-Output "Build and AOT publish completed."
Write-Output ""
Write-Output "Install python library majorsilence_reporting_rdlcreator by running 'cd RdlCreator\python_project_root && pip install . '"
Write-Output ""
Write-Output "from majorsilence_reporting_rdlcreator import majorsilence"
Write-Output "creatorReport = majorsilence.reporting.rdlcreator.Report()"
Write-Output ""
Write-Output "Install python library majorsilence_reporting_rdlengine by running 'cd RdlEngine\python_project_root && pip install . '"
Write-Output ""
Write-Output "from majorsilence_reporting_rdlengine import majorsilence"
Write-Output "engineReport = majorsilence.reporting.rdl.Report()"
