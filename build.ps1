param(
    [Parameter(Position = 0, ValueFromPipeline)]
    [string] $Version = "1.0.0",
    [Parameter(Position = 1, ValueFromPipeline)]
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",
    [Parameter(Position = 2, ValueFromPipeline)]
    [switch] $RunTests,
    [Parameter(Position = 3, ValueFromPipeline)]
    [switch] $Nupkg
)

Write-Host "Building MongoMigrations.Core v$Version" -ForegroundColor Green

# Build
dotnet build --configuration $Configuration /p:Version=$Version /p:GeneratePackageOnBuild=$Nupkg

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit $LASTEXITCODE
}

# Test (if requested)
if ($RunTests) {
    Write-Host "Running tests..." -ForegroundColor Green
    dotnet test --configuration $Configuration --no-build
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed"
        exit $LASTEXITCODE
    }
}

Write-Host "Done!" -ForegroundColor Green
