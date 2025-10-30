# This script runs tests with code coverage and generates an HTML report.

# --- Configuration ---
$ErrorActionPreference = "Stop"

$solutionFile = "src/Imageflow.dncore.sln"
$coverageDir = "./CoverageReport"
$coverageFile = "./TestResults/**/coverage.opencover.xml"

# --- Steps ---

Write-Host "Step 1: Cleaning up previous builds and caches..."
try {
    # Clean the solution first, which requires package assets from the previous run.
    dotnet clean $solutionFile --configuration Release


    if (Test-Path $coverageDir) {
        Remove-Item -Recurse -Force $coverageDir
    }
    if (Test-Path "./TestResults") {
        Remove-Item -Recurse -Force "./TestResults"
    }
    Write-Host "Cleanup successful."
} catch {
    Write-Warning "Cleanup step failed. $_"
}

Write-Host "Step 2: Restoring dependencies..."
dotnet restore $solutionFile

Write-Host "Step 3: Running tests and collecting code coverage..."
# Note: The path in 'ResultsDirectory' must match the glob pattern for the coverage file.
dotnet test $solutionFile -c Release --collect:"XPlat Code Coverage" --results-directory ./TestResults --settings "./tests/coverlet.runsettings"

Write-Host "Step 4: Generating HTML report..."
# Ensure ReportGenerator is installed
if (-not (dotnet tool list --global | Select-String 'reportgenerator')) {
    Write-Host "ReportGenerator not found, installing as a global tool..."
    dotnet tool install --global dotnet-reportgenerator-globaltool
}

reportgenerator `
    -reports:$coverageFile `
    -targetdir:$coverageDir `
    -reporttypes:"Html;TextSummary"

Write-Host "Code coverage report generated successfully!"
$reportPath = (Join-Path $PWD.Path "$coverageDir\index.html")
Write-Host "You can view the report here: $reportPath"

# Automatically open the report in the default browser
Start-Process $reportPath
