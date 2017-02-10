param(
    [string]$packageVersion = $null,
    [string]$config = "Release",
    [string]$target = "Rebuild",
    [string]$verbosity = "Minimal"
)

# Initialization
$rootFolder = Split-Path -parent $script:MyInvocation.MyCommand.Path
. $rootFolder\myget.include.ps1

# Clean
MyGet-Build-Clean $rootFolder

# MyGet
$packageVersion = MyGet-Package-Version $packageVersion

# MSBUILD
$MSBuildPath = "${env:ProgramFiles(x86)}\MSBuild\14.0\Bin\msbuild.exe"

# Solution

$nuspec = Join-Path $rootFolder "src\mongomigrations\mongomigrations.nuspec"
$outputFolder = Join-Path $rootFolder "bin"
$platforms = @("AnyCpu")
$targetFrameworks = @("v4.6.1")
$projects = @(
	"src\mongomigrations\mongomigrations.csproj",
	"src\runmongomigrations\runmongomigrations.csproj"
)

MyGet-Build-Solution -sln src\mongomigrations.sln `
	-rootFolder $rootFolder `
	-projects $projects `
	-outputFolder $outputFolder `
	-version $packageVersion `
	-config $config `
	-target $target `
	-platforms $platforms `
	-targetFrameworks $targetFrameworks `
	-verbosity $verbosity `
	-excludeNupkgProjects $projects `
	-MSBuildCustomProperties $MSBuildCustomProperties `
	-MSBuildPath $MSBuildPath

$platforms | ForEach-Object {
	$platform = $_	
	$nupkgOutputFolder = Join-Path $outputFolder "$packageVersion\$platform\$config\$targetFramework"

	MyGet-Build-Nupkg -rootFolder $rootFolder `
	-outputFolder $nupkgOutputFolder `
	-project src\mongomigrations.sln `
	-config $config `
	-version $packageVersion `
	-platform $platform `
	-nuspec $nuspec `
}