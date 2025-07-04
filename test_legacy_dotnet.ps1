# This script reproduces the 'test_legacy' job from the GitHub Actions workflow.
# It helps debug build failures locally that occur in the CI environment.

# Prerequisites:
# 1. Visual Studio 2022 with MSBuild installed.
# 2. .NET SDKs (6, 8, 9) installed.
# 3. NuGet CLI installed and in your PATH. You can download it from https://www.nuget.org/downloads

# --- Configuration ---
$ErrorActionPreference = "Stop"

$solutionFile = "src/Imageflow.dnfull.sln"
$testProject1 = "tests/Imageflow.TestDotNetFull/Imageflow.TestDotNetFull.csproj"

# --- Helper function to find MSBuild ---
function Get-MSBuildPath {
    try {
        # Use vswhere to find the latest Visual Studio installation
        $vsInstallPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath
        if ($vsInstallPath) {
            $msbuildPath = Join-Path $vsInstallPath "MSBuild\Current\Bin\MSBuild.exe"
            if (Test-Path $msbuildPath) {
                return $msbuildPath
            }
        }
    }
    catch {
        Write-Warning "vswhere.exe not found or failed. Trying default MSBuild path."
    }

    # Fallback for Build Tools or other locations
    $defaultPath = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    if(Test-Path $defaultPath){
        return $defaultPath
    }
    $defaultPath2 = "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
     if(Test-Path $defaultPath2){
        return $defaultPath2
    }

    return $null
}

$finalExitCode = 0
# --- Build Steps ---

Write-Host "Step 1: Restoring NuGet packages for the solution..."
try {
    # Restore all projects in the solution, this should handle SDK projects correctly.
    dotnet restore $solutionFile --force-evaluate
    Write-Host "Dotnet restore for the solution completed successfully."
} catch {
    Write-Warning "Dotnet restore failed, this may cause issues later. $_"
    $finalExitCode = 1
}


Write-Host "Step 2: Restoring NuGet packages for the legacy packages.config project (using nuget.exe)..."
try {
    # This is required for the legacy project that uses packages.config
    nuget restore $testProject1 -SolutionDirectory src
    Write-Host "NuGet restore for legacy project completed successfully."
} catch {
    Write-Error "NuGet restore failed. $_"
    $finalExitCode = 2
}


Write-Host "Step 3: Building the legacy solution with MSBuild..."
$msbuild = Get-MSBuildPath
if (-not $msbuild) {
    Write-Error "MSBuild.exe could not be found. Please ensure Visual Studio 2022 is installed."
    $finalExitCode = 3
}

Write-Host "Found MSBuild at: $msbuild"
try {
    & $msbuild $solutionFile /p:Configuration=Release /p:Platform="Any CPU"
    Write-Host "MSBuild build completed successfully."
} catch {
    Write-Error "MSBuild build failed. $_"
    $finalExitCode = 4
}


Write-Host "Step 4: Running legacy tests..."
try {
    $testDll1 = "tests/Imageflow.TestDotNetFull/bin/Release/Imageflow.TestDotNetFull.dll"
    $testDll2 = "tests/Imageflow.TestDotNetFullPackageReference/bin/Release/Imageflow.TestDotNetFullPackageReference.dll"

    Write-Host "  - Running tests for AnyCPU..."
    dotnet test $testDll1
    dotnet test $testDll2

    Write-Host "  - Running tests for x86..."
    dotnet test $testDll1 --platform:x86
    dotnet test $testDll2 --platform:x86

    Write-Host "  - Running tests for x64..."
    dotnet test $testDll1 --platform:x64
    dotnet test $testDll2 --platform:x64

    Write-Host "All legacy tests completed successfully."
} catch {
    Write-Error "Legacy tests failed. $_"
    $finalExitCode = 5
}
if ($finalExitCode -ne 0) {
    Write-Host "Local reproduction script failed with exit code $finalExitCode."
    exit $finalExitCode
}
Write-Host "Local reproduction script succeeded."
exit $finalExitCode

