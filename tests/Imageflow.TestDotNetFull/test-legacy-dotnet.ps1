
# change to the parent dir of the parent dir of this script, regardless of where it's run from
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $scriptPath
Set-Location ..
Set-Location ..
# print current directory
Write-Output "Current directory: $(Get-Location)"


function TestAndAddToPath($programName, $pathList, $installWithWinget = $false, $wingetPackageName = "")
{
    # Check if the program is already in the path
    $programPresent = Get-Command $programName -ErrorAction SilentlyContinue
    if ($programPresent -ne $null)
    {
        Write-Output "$programName is already in the path at: $($programPresent.Path)\n"
        return
    }

    # If the program is not in the path, iterate over the list of paths
    foreach ($path in $pathList)
    {
        $fullPath = Join-Path -Path $path -ChildPath $programName
        # Check if the program exists in the current path
        if (Test-Path $fullPath)
        {
            # If the program is found, add the path to the system path and exit the function
            Write-Output "Adding $programName to path: $fullPath\n"
            $env:Path = $env:Path + ";" + $path
            return
        }
        # if not, and if $path is a directory, search it recursively
        if ((Test-Path $path) -and (Get-ChildItem $path -Recurse -Filter $programName -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName) -ne $null)
        {
            # If the program is found, add the path to the system path and exit the function
            Write-Output "Adding (found via search) $programName to path: $path\n"
            $env:Path = $env:Path + ";" + $path
            return
        }
    }
    # If the program is not found, print an error and exit
    Write-Output "$programName not found in any of the following paths:"
    foreach ($path in $pathList)
    {
        Write-Output $path
    }
    if ($installWithWinget)
    {
        Write-Output "Install $wingetPackageName with winget to add it to the path."
        $wingetInstallCommand = "winget install $wingetPackageName"
        Write-Output "Run the following command to install $wingetPackageName with winget:"
        Write-Output $wingetInstallCommand
    }
    exit 1
}

$nugetLocations = @(
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\NuGet",
    "C:\Program Files (x86)\NuGet\NuGet.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\")

TestAndAddToPath "nuget.exe" $nugetLocations $true "Microsoft.NuGet"
TestAndAddToPath "msbuild.exe" @("C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin", "C:\Program Files (x86)\Microsoft Visual Studio\2022\", "C:\Program Files (x86)\Microsoft Visual Studio\") $false 
TestAndAddToPath "vstest.console.exe" @("C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\Common7\IDE", "C:\Program Files (x86)\Microsoft Visual Studio\2022\", "C:\Program Files (x86)\Microsoft Visual Studio\") $false

$projects = @(
    "tests/Imageflow.TestDotNetFull/Imageflow.TestDotNetFull.csproj",
    "tests/Imageflow.TestDotNetFullPackageReference/Imageflow.TestDotNetFullPackageReference.csproj"
)

# First pack the nuget packages
dotnet pack ./src/Imageflow/Imageflow.Net.csproj -c Release --include-source -o NuGetPackages/Release/
dotnet pack ./src/Imageflow.AllPlatforms/Imageflow.AllPlatforms.csproj -c Release --include-source -o NuGetPackages/Release/


foreach ($project in $projects)
{
    if (-not (Test-Path $project))
    {
        Write-Output "Project not found: $project"
        exit 1
    }
    Write-Output "Restoring NuGet packages for project: $project"
    if ((nuget restore $project -SolutionDirectory src -ForceEvaluate) -ne 0)
    {
        Write-Output "NuGet restore failed for project: $project"
        exit 1
    }
}
Write-Output "Building: src/Imageflow.dnfull.sln"
# Build the solution with MSBuild
if ((msbuild src/Imageflow.dnfull.sln /p:Configuration=Release /p:Platform="Any CPU") -ne 0)
{
    Write-Output "MSBuild failed for solution: src/Imageflow.dnfull.sln"
    exit 1
}

$testDlls = @(
    "tests\Imageflow.TestDotNetFull\bin\Release\Imageflow.TestDotNetFull.dll",
    "tests\Imageflow.TestDotNetFullPackageReference\bin\Release\Imageflow.TestDotNetFullPackageReference.dll"
)

foreach ($dll in $testDlls)
{
    if (-not (Test-Path $dll))
    {
        Write-Output "Test DLL not found: $dll"
        exit 1
    }
    
    # test with /Platform:x86 , /Platform:x64, and no platform specified
    $platforms = @("x86", "x64", "")
    foreach ($platform in $platforms)
    {
        $platformArg = ""
        if ($platform -ne "")
        {
            $platformArg = "/Platform:$platform"
        }
        if ((vstest.console $platformArg $dll) -ne 0)
        {
            Write-Output "vstest.console $platormArg failed for DLL: $dll"
            exit 1
        }
    }
}
