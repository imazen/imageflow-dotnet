#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test legacy .NET Framework projects (mirrors GitHub Actions test_legacy workflow)

.DESCRIPTION
    This script builds and tests the legacy .NET Framework projects using MSBuild and dotnet test.
    It runs tests for both x86 and x64 architectures.

.EXAMPLE
    .\test_legacy.ps1
#>

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$FULL_SLN = ".\src\Imageflow.dnfull.sln"

Write-Host "=== Building Legacy .NET Framework Projects ===" -ForegroundColor Cyan

# Find MSBuild
Write-Host "`nLocating MSBuild..." -ForegroundColor Yellow
$msbuildPath = $null

# Try vswhere first (most reliable for VS 2017+)
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $vsPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -property installationPath
    if ($vsPath) {
        $msbuildPath = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
        if (-not (Test-Path $msbuildPath)) {
            $msbuildPath = Join-Path $vsPath "MSBuild\15.0\Bin\MSBuild.exe"
        }
    }
}

# Fallback: try common paths
if (-not $msbuildPath -or -not (Test-Path $msbuildPath)) {
    $possiblePaths = @(
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    )

    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $msbuildPath = $path
            break
        }
    }
}

if (-not $msbuildPath -or -not (Test-Path $msbuildPath)) {
    Write-Error "MSBuild not found. Please install Visual Studio or Visual Studio Build Tools."
    exit 1
}

Write-Host "Found MSBuild: $msbuildPath" -ForegroundColor Green

# Step 1: Restore with dotnet
Write-Host "`nRestoring with dotnet..." -ForegroundColor Yellow
dotnet restore $FULL_SLN
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet restore failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Step 2: Restore with nuget
Write-Host "`nRestoring with nuget..." -ForegroundColor Yellow
nuget restore $FULL_SLN -SolutionDirectory src
if ($LASTEXITCODE -ne 0) {
    Write-Warning "nuget restore had warnings, continuing..."
}

# Step 3: Build with MSBuild
Write-Host "`nBuilding with MSBuild..." -ForegroundColor Yellow
& $msbuildPath $FULL_SLN /p:Configuration=Release /p:Platform="Any CPU" /v:minimal
if ($LASTEXITCODE -ne 0) {
    Write-Error "msbuild failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "`n=== Running Legacy Tests ===" -ForegroundColor Cyan

$testDlls = @(
    "tests\Imageflow.TestDotNetFull\bin\Release\Imageflow.TestDotNetFull.dll",
    "tests\Imageflow.TestDotNetFullPackageReference\bin\Release\Imageflow.TestDotNetFullPackageReference.dll"
)

$platforms = @("", "x86", "x64")
$failedTests = @()

foreach ($dll in $testDlls) {
    $dllName = Split-Path $dll -Leaf

    foreach ($platform in $platforms) {
        if ($platform -eq "") {
            Write-Host "`n--- Testing $dllName (AnyCPU) ---" -ForegroundColor Yellow
            dotnet test $dll
        } else {
            Write-Host "`n--- Testing $dllName ($platform) ---" -ForegroundColor Yellow
            dotnet test $dll --platform:$platform
        }

        if ($LASTEXITCODE -ne 0) {
            $platformLabel = if ($platform -eq "") { "AnyCPU" } else { $platform }
            $failedTest = "$dllName [$platformLabel]"
            $failedTests += $failedTest
            Write-Warning "Test failed: $failedTest"
        }
    }
}

Write-Host "`n=== Test Summary ===" -ForegroundColor Cyan

if ($failedTests.Count -eq 0) {
    Write-Host "All tests passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Failed tests:" -ForegroundColor Red
    foreach ($test in $failedTests) {
        Write-Host "  - $test" -ForegroundColor Red
    }
    exit 1
}
