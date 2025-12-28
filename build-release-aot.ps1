#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"
$CURRENTPATH=$pwd.Path

# /p:Configuration="Debug", "Debug-DrawingCompat", "Release", "Release-DrawingCompat"
$pConfigurationCompat="Release-DrawingCompat"
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

#Get-ChildItem .\ -include bin,obj,build-output -Recurse | foreach ($_) { remove-item $_.fullname -Force -Recurse }

$Version=""
GetVersions([ref]$Version)
Write-Host  $Version

dotnet restore "./MajorsilenceReporting.sln"
# ************* Begin anycpu *********************************************
dotnet build "$CURRENTPATH\MajorsilenceReporting.sln" --configuration $pConfigurationCompat --verbosity minimal
#dotnet publish RdlCreator\Majorsilence.Reporting.RdlCreator.csproj -c $pConfigurationCompat -r linux-x64 -f $pTargetFrameworkGeneric -p:PublishAot=true  
#dotnet publish RdlCreator\Majorsilence.Reporting.RdlCreator.csproj -c $pConfigurationCompat -r linux-arm64 -f $pTargetFrameworkGeneric -p:PublishAot=true  
#dotnet publish RdlCreator\Majorsilence.Reporting.RdlCreator.csproj -c $pConfigurationCompat -r osx-x64 -f $pTargetFrameworkGeneric -p:PublishAot=true  
#dotnet publish RdlCreator\Majorsilence.Reporting.RdlCreator.csproj -c $pConfigurationCompat -r osx-arm64 -f $pTargetFrameworkGeneric -p:PublishAot=true  
dotnet publish RdlCreator\Majorsilence.Reporting.RdlCreator.csproj -c $pConfigurationCompat -r win-x64 -f $pTargetFrameworkGeneric -p:PublishAot=true  
#dotnet publish RdlCreator\Majorsilence.Reporting.RdlCreator.csproj -c $pConfigurationCompat -r win-arm64 -f $pTargetFrameworkGeneric -p:PublishAot=true  