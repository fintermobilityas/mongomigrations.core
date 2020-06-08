param(
    [Parameter(Position = 0, ValueFromPipeline)]
    [string] $Version = "0.0.0",
    [Parameter(Position = 1, ValueFromPipeline)]
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",
    [Parameter(Position = 2, ValueFromPipeline)]
    [switch] $RunTests,
    [Parameter(Position = 3, ValueFromPipeline)]
    [switch] $Nupkg
)

$ErrorActionPreference = "Stop"; 
$ConfirmPreference = "None"; 

$WorkingDirectory = Split-Path -parent $MyInvocation.MyCommand.Definition
. $WorkingDirectory\common.ps1

$BuildOutputDirectory = Join-Path $WorkingDirectory build\$Version
$TestResultsOutputDirectory = Join-Path $WorkingDirectory build\$Version\TestResults

Resolve-Shell-Dependency dotnet

Invoke-Command-Colored dotnet @("clean")

Invoke-Command-Colored dotnet @(
    ("build {0}" -f (Join-Path $WorkingDirectory MongoMigrations.sln))
    "/p:Version=$Version",
    "/p:GeneratePackageOnBuild=$Nupkg"
    "--output $BuildOutputDirectory"
    "--configuration $Configuration"
)

if($RunTests) {
    $TestProjects = Get-ChildItem -Path $BuildOutputDirectory -filter *.tests.dll | Select-Object -Expand FullName
    $TestProjectsCount = $TestProjects.Length
    
    Write-Output-Header ("Running tests. Projects: {0}" -f ($TestProjectsCount))

    foreach ($TestProject in $TestProjects)
    {
        $TestProjectName = Split-Path $TestProject -LeafBase
        $TestResultOutputDirectory = Join-Path $TestResultsOutputDirectory $TestProjectName

        Write-Output "Project: $TestProject"
        Write-Output "Test result directory: $TestResultOutputDirectory"
        Write-Output ""

        Invoke-Command-Colored dotnet @(
            "test" 
            "$TestProject"
            "--test-adapter-path:."
            "--logger:xunit"
            "--verbosity normal"
            "--results-directory $TestResultOutputDirectory"
        )
    }
}