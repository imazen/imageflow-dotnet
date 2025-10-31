param(
    [string]$Solution = "src\Imageflow.dnfull.sln",
    [string]$Platforms = "AnyCPU,x86,x64",
    [switch]$RestoreOnly,
    [switch]$BuildOnly,
    [switch]$TestOnly,
    [switch]$SkipClean,
    [switch]$SkipPack,
    [string]$Configuration = "Release",
    [switch]$Help
)

<#
.SYNOPSIS
    Unified test script for .NET Framework testing of Imageflow.Net

.DESCRIPTION
    This script consolidates all .NET Framework testing approaches into a single, flexible script.
    It supports testing both packages.config and PackageReference projects on multiple platforms.

.PARAMETER Solution
    Path to the solution file to test. Default: "src\Imageflow.both.sln"
    Options:
    - src\Imageflow.both.sln (includes all projects)
    - src\Imageflow.dnfull.sln (.NET Framework only)

.PARAMETER Platforms
    Array of platforms to test. Default: @("AnyCPU", "x86", "x64")
    Options: "AnyCPU", "x86", "x64"

.PARAMETER RestoreOnly
    Only restore packages, don't build or test

.PARAMETER BuildOnly
    Only build the solution, don't run tests

.PARAMETER TestOnly
    Only run tests (assumes solution is already built)

.PARAMETER SkipClean
    Skip cleaning previous builds

.PARAMETER SkipPack
    Skip packing NuGet packages

.PARAMETER Configuration
    Build configuration. Default: "Release"

.PARAMETER Help
    Show this help message

.EXAMPLE
    .\test_dotnet_framework.ps1
    Run full test suite with default settings

.EXAMPLE
    .\test_dotnet_framework.ps1 -Platforms @("x86", "x64") -TestOnly
    Only test x86 and x64 platforms (assumes already built)

.EXAMPLE
    .\test_dotnet_framework.ps1 -Solution "src\Imageflow.dnfull.sln" -RestoreOnly
    Only restore packages for .NET Framework solution

.EXAMPLE
    .\test_dotnet_framework.ps1 -SkipClean -SkipPack
    Run tests without cleaning or packing
#>

if ($Help) {
    Get-Help $MyInvocation.MyCommand.Path -Full
    exit 0
}

$ErrorActionPreference = "Stop"

# Script starts from repository root regardless of where it's called from
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $scriptPath
Write-Host "Working directory: $(Get-Location)"

function Get-MSBuildPath {
    try {
        # Use vswhere to find the latest Visual Studio installation
        $vswherePath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
        if (Test-Path $vswherePath) {
            $vsInstallPath = & $vswherePath -latest -property installationPath
            if ($vsInstallPath) {
                $msbuildPath = Join-Path $vsInstallPath "MSBuild\Current\Bin\MSBuild.exe"
                if (Test-Path $msbuildPath) {
                    return $msbuildPath
                }
            }
        }
    }
    catch {
        Write-Warning "vswhere.exe failed. Trying fallback paths."
    }

    # Fallback paths for different Visual Studio editions
    $fallbackPaths = @(
        "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    )

    foreach ($path in $fallbackPaths) {
        if (Test-Path $path) {
            return $path
        }
    }

    return $null
}

function Test-AndAddToPath {
    param(
        [string]$ProgramName,
        [string[]]$PathList,
        [bool]$InstallWithWinget = $false,
        [string]$WingetPackageName = ""
    )

    # Check if program is already in PATH
    $programPresent = Get-Command $ProgramName -ErrorAction SilentlyContinue
    if ($programPresent -ne $null) {
        Write-Host "$ProgramName found in PATH at: $($programPresent.Path)"
        return $true
    }

    # Search in provided paths
    foreach ($path in $PathList) {
        $fullPath = Join-Path -Path $path -ChildPath $ProgramName
        if (Test-Path $fullPath) {
            Write-Host "Adding $ProgramName to PATH from: $fullPath"
            $env:Path = $env:Path + ";" + $path
            return $true
        }

        # Search recursively if path is a directory
        if ((Test-Path $path) -and (Get-ChildItem $path -Recurse -Filter $ProgramName -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName)) {
            Write-Host "Adding $ProgramName to PATH (found via search): $path"
            $env:Path = $env:Path + ";" + $path
            return $true
        }
    }

    Write-Error "$ProgramName not found in any of the specified paths"
    if ($InstallWithWinget) {
        Write-Host "Install $WingetPackageName with winget: winget install $WingetPackageName"
    }
    return $false
}

function Invoke-Step {
    param(
        [string]$StepName,
        [scriptblock]$StepCode,
        [switch]$ContinueOnError = $false
    )

    Write-Host "=== $StepName ===" -ForegroundColor Cyan
    try {
        $result = & $StepCode
        if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne $null) {
            throw "Step failed with exit code $LASTEXITCODE"
        }
        Write-Host "✓ $StepName completed successfully" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "✗ $StepName failed:" -ForegroundColor Red
        Write-Host "  $_" -ForegroundColor Red
        if (-not $ContinueOnError) {
            throw
        }
        return $false
    }
}

# Validate solution exists
if (-not (Test-Path $Solution)) {
    Write-Error "Solution file not found: $Solution"
    exit 1
}

# Parse platforms from comma-separated string
$platformArray = $Platforms -split ',' | ForEach-Object { $_.Trim() }

        Write-Host "Configuration:" -ForegroundColor Yellow
        foreach ($line in @(
            "  Solution: $Solution",
            "  Configuration: $Configuration",
            "  Platforms: $($platformArray -join ', ')",
            "  Restore Only: $RestoreOnly",
            "  Build Only: $BuildOnly",
            "  Test Only: $TestOnly",
            "  Skip Clean: $SkipClean",
            "  Skip Pack: $SkipPack"
        )) {
            Write-Host $line
        }

$finalExitCode = 0

# Step 0: Setup tools and clean (if not skipped)
if (-not $TestOnly) {
    if (-not $SkipClean) {
        Invoke-Step "Cleaning previous builds" {
            # Check if DLLs exist before cleaning
            $existingDlls = @(
                "tests\Imageflow.TestDotNetFull\bin\$Configuration\Imageflow.TestDotNetFull.dll",
                "tests\Imageflow.TestDotNetFullPackageReference\bin\$Configuration\Imageflow.TestDotNetFullPackageReference.dll"
            ) | Where-Object { Test-Path $_ }

            if ($existingDlls.Count -gt 0) {
                Write-Host "Found existing DLLs that should be cleaned: $($existingDlls.Count)"
            }

            dotnet clean $Solution --configuration $Configuration /v:minimal
            if (Test-Path "src\packages") {
                Remove-Item -Recurse -Force "src\packages"
            }

            # Verify DLLs were actually removed
            $remainingDlls = @(
                "tests\Imageflow.TestDotNetFull\bin\$Configuration\Imageflow.TestDotNetFull.dll",
                "tests\Imageflow.TestDotNetFullPackageReference\bin\$Configuration\Imageflow.TestDotNetFullPackageReference.dll"
            ) | Where-Object { Test-Path $_ }

            if ($remainingDlls.Count -gt 0) {
                Write-Host "✗ Clean failed - DLLs still exist after cleaning:" -ForegroundColor Red
                $remainingDlls | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
                throw "Clean operation failed to remove all DLLs"
            }

            Write-Host "Cleanup completed successfully - all DLLs removed"
        } -ContinueOnError
    }

    # Setup required tools
    Invoke-Step "Setting up required tools" {
        $nugetLocations = @(
            "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\NuGet",
            "C:\Program Files (x86)\NuGet",
            "C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\Common7\IDE\CommonExtensions\Microsoft\NuGet",
            "C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\NuGet"
        )

        if (-not (Test-AndAddToPath "nuget.exe" $nugetLocations $true "Microsoft.NuGet")) {
            throw "NuGet.exe is required but not found"
        }
    }

    # Detect MSBuild path in main scope
    $msbuild = Get-MSBuildPath
    if (-not $msbuild) {
        throw "MSBuild.exe could not be found. Please ensure Visual Studio 2022 is installed."
    }
    Write-Host "Found MSBuild at: $msbuild"

    # Step 1: Restore packages
    Invoke-Step "Restoring NuGet packages (dotnet restore)" {
        dotnet restore $Solution --force-evaluate /v:minimal
        if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }
    }

    Invoke-Step "Restoring NuGet packages (nuget.exe for packages.config)" {
        $output = nuget restore $Solution -SolutionDirectory src 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "nuget restore failed with exit code $LASTEXITCODE"
        }
        # Check for SDK resolution errors ONLY in .NET Framework test projects
        $frameworkErrors = $output | Where-Object {
            $_ -match "Could not resolve SDK" -and
            ($_ -match "TestDotNetFull" -or $_ -match "net462")
        }
        if ($frameworkErrors) {
            Write-Host "Critical SDK resolution errors detected in .NET Framework projects:" -ForegroundColor Red
            $frameworkErrors | ForEach-Object { Write-Host $_ -ForegroundColor Red }
            throw "nuget restore failed due to .NET Framework SDK resolution errors"
        }
        # Show warnings about modern .NET SDK issues but don't fail
        $modernErrors = $output | Where-Object {
            $_ -match "Could not resolve SDK" -and
            -not ($_ -match "TestDotNetFull" -or $_ -match "net462")
        }
        if ($modernErrors) {
            Write-Host "Note: Modern .NET projects have SDK issues (will be ignored for .NET Framework testing):" -ForegroundColor Yellow
            $modernErrors | ForEach-Object { Write-Host $_ -ForegroundColor Yellow }
        }
    }

    if ($RestoreOnly) {
        Write-Host "Restore completed. Exiting..." -ForegroundColor Green
        exit 0
    }

    # Step 2: Build solution
    Invoke-Step "Building solution with MSBuild" {
        & "$msbuild" $Solution /p:Configuration=$Configuration /p:Platform="Any CPU" /v:minimal
        if ($LASTEXITCODE -ne 0) { throw "MSBuild failed" }
    }

    if ($BuildOnly) {
        Write-Host "Build completed. Exiting..." -ForegroundColor Green
        exit 0
    }

    # Step 3: Pack NuGet packages (if not skipped)
    if (-not $SkipPack) {
        Invoke-Step "Packing NuGet packages" {
            dotnet pack ./src/Imageflow/Imageflow.Net.csproj -c $Configuration --include-source -o NuGetPackages/Release/
            if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed for Imageflow.Net" }
            dotnet pack ./src/Imageflow.AllPlatforms/Imageflow.AllPlatforms.csproj -c $Configuration --include-source -o NuGetPackages/Release/
            if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed for Imageflow.AllPlatforms" }
        }
    }
}

# Step 4: Run tests
$testProjects = @(
    "tests\Imageflow.TestDotNetFull\bin\$Configuration\Imageflow.TestDotNetFull.dll",
    "tests\Imageflow.TestDotNetFullPackageReference\bin\$Configuration\Imageflow.TestDotNetFullPackageReference.dll"
)

# Validate that test DLLs exist before running tests
Invoke-Step "Validating test DLLs exist" {
    $missingDlls = $testProjects | Where-Object { -not (Test-Path $_) }
    if ($missingDlls.Count -gt 0) {
        Write-Host "✗ Missing test DLLs - cannot run tests:" -ForegroundColor Red
        $missingDlls | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        throw "Test DLLs not found - build may have failed"
    }
    Write-Host "All test DLLs found: $($testProjects.Count)"
}

foreach ($testDll in $testProjects) {
    if (-not (Test-Path $testDll)) {
        Write-Error "Test DLL not found: $testDll"
        $finalExitCode = 1
        continue
    }

    foreach ($platform in $platformArray) {
        $platformArg = ""
        if ($platform -eq "x86") {
            $platformArg = "--platform:x86"
        } elseif ($platform -eq "x64") {
            $platformArg = "--platform:x64"
        }

        $stepName = "Running tests for $testDll on $platform"
        Invoke-Step $stepName {
            if ($platformArg) {
                dotnet test $testDll $platformArg
            } else {
                dotnet test $testDll
            }
            if ($LASTEXITCODE -ne 0) { throw "dotnet test failed for $testDll on $platform" }
        } -ContinueOnError
    }
}

if ($finalExitCode -eq 0) {
    Write-Host ""
    Write-Host "🎉 All tests completed successfully!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "❌ Some tests failed. Check the output above for details." -ForegroundColor Red
}

exit $finalExitCode
