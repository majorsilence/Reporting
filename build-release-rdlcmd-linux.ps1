#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"
$CURRENTPATH=$pwd.Path

function GetVersions([ref]$theVersion)
{
	$csprojPath = Join-Path $CURRENTPATH "./RdlEngine/RdlEngine.csproj"
	$xml = [xml](Get-Content $csprojPath)
	$theVersion.Value = $xml.Project.PropertyGroup.Version
}

function BuildRdlCmd{
	Get-ChildItem ./ -include bin,obj,build-output -Recurse | foreach ($_) { remove-item $_.fullname -Force -Recurse }

	$Version=""
	GetVersions([ref]$Version)
	Write-Host  $Version
	dotnet restore "./MajorsilenceReporting.sln"

	Write-Output "Linux x64 rdlcmd build"

	$buildoutputpath_rdlcmd_linux="$CURRENTPATH/Release-Builds/build-output/majorsilence-reporting-rdlcmd-linux-x64"
	rmf -rf $buildoutputpath_rdlcmd_linux
	mkdir -p $buildoutputpath_rdlcmd_linux

	dotnet publish RdlCmd -c Release -r linux-x64 -f net8.0 -p:PublishReadyToRun=true --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
	Copy-Item ./RdlCmd/bin/Release/net8.0/linux-x64/publish -Destination "$buildoutputpath_rdlcmd_linux/" -Recurse

	cd Release-Builds
	cd build-output	
	7za a $Version-majorsilence-reporting-rdlcmd-linux-x64.zip majorsilence-reporting-rdlcmd-linux-x64\
	cd "$CURRENTPATH"
}

param (
    [Parameter(Mandatory=$false)]
    [string]$arg
)

if ($arg -eq "build") {
    BuildRdlCmd
} else {
	Write-Output "Usage: ./build-release-linux.ps1 docker"
	Write-Output "net8.0 using the mono System.Drawing.Common library from nuget package ZKWeb.System.Drawing"
	Write-Output "requires libgdiplus be installed apt-get install -y libgdiplus"
	Write-Output "Micrsoft has deprecated System.Drawing.Common in .net 6.0"
	Write-Output "https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/system-drawing-common-windows-only"
    docker run --memory 4gb --rm -v ${CURRENTPATH}:/src mcr.microsoft.com/dotnet/sdk:8.0 bash -c "cd /src && ./build-release-linux.ps1 build"
}

		