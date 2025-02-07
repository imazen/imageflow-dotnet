
# Get the current directory
$currentDir = Get-Location

# Get the directory this file is in, and change to it.
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $scriptPath

# Get the first argument, which is the target architecture.

$targetArchitecture = $args[0]
if ($null -eq $targetArchitecture) {
    Write-Error "Target architecture not provided. Exiting."
    exit 1
}
# Delete/clear the publish folder, ignore errors
Remove-Item -Recurse -Force ./test-publish -ErrorAction SilentlyContinue
# bin/obj too, since issues?
Remove-Item -Recurse -Force ./bin -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force ./obj -ErrorAction SilentlyContinue

# First, change to the ../../src directory
Set-Location ../../src/

# First, let's restore the solution
dotnet restore -v diag ../tests/Imageflow.TestWebAOT/Imageflow.TestWebAOT.csproj -r $targetArchitecture

# List, recursively, all files in the .nuget/packages/imageflow.nativeruntime.* directories, flattened to just files ending in .dll, .so, .dylib, .a, .lib
$nativeRuntimeFiles = Get-ChildItem -Path ~/.nuget/packages/imageflow.nativeruntime.* -Recurse | Where-Object { $_.Extension -in '.dll', '.so', '.dylib', '.a', '.lib' }

# Map them to simple full paths
$nativeRuntimeFiles = $nativeRuntimeFiles | ForEach-Object { $_.FullName }

# print the files
Write-Output "Native runtime files in .nuget/packages/imageflow.nativeruntime.*:"
Write-Output $nativeRuntimeFiles



# Then publish the project
dotnet publish --force  -v diag -c Release ../tests/Imageflow.TestWebAOT/Imageflow.TestWebAOT.csproj -o $scriptPath/test-publish -r $targetArchitecture
# if the above fails, exit with a non-zero exit code.


if ($LASTEXITCODE -ne 0) {
    Write-Output "Failed to publish the AOT test project. Exiting."
    Write-Warning "Failed to publish the AOT test project. Exiting."

    Set-Location $currentDir
    exit 1
}

Set-Location $scriptPath

# Change back to the tests directory


# run the executable in the background in ./publish/native/Imageflow.TestWebAOT.exe or ./publish/native/Imageflow.TestWebAOT
$process = $null
$server_pid = $null

if (Test-Path -Path $scriptPath/test-publish/Imageflow.TestWebAOT.exe) {
    $process = Start-Process -FilePath $scriptPath/test-publish/Imageflow.TestWebAOT.exe -NoNewWindow -PassThru -RedirectStandardOutput "./output.log"
} else {
    $process = Start-Process -FilePath $scriptPath/test-publish/Imageflow.TestWebAOT -NoNewWindow -PassThru -RedirectStandardOutput "./output.log"
}

# wait for the process to start
Start-Sleep -Seconds 1
# report on the process, if it started
if ($null -eq $process) {
    Write-Output "Failed to start the server. Exiting."
    Write-Warning "Failed to start the server. Exiting."
    # kill the process if it's running
    Stop-Process -Id $process.Id
    exit 1
}
Write-Output "Started the server with PID $($process.Id)"
$server_pid = $process.Id

try{
    # quit if the process failed to start
    if ($LASTEXITCODE -ne 0) {
        exit 1
    }
    # store the PID of the executable
    $server_pid = $process.Id

    # wait for the server to start 200ms
    Start-Sleep -Milliseconds 200

    $output = (Get-Content "./output.log");
    # if null, it failed to start
    if ($output -eq $null) {
        Write-Error "Failed to start the server (no output). Exiting."
        exit 1
    }

    Write-Output "Server output:"
    Write-Output $output

    # parse the port from the output log
    $port = 5000
    $portRegex = [regex]::new("Now listening on: http://localhost:(\d+)")
    $portMatch = $portRegex.Match($output)
    if ($portMatch.Success) {
        $port = $portMatch.Groups[1].Value
    }



    # if the process doesn't respond to a request, sleep 5 seconds and try again
    $timeout = 5
    $timeoutCounter = 0
    while ($timeoutCounter -lt $timeout) {

        # try to make a request to the server
        $timeoutMs = $timeoutCounter * 500 + 200
        $url = "http://localhost:$port/"
        try{
            $response = Invoke-WebRequest -Uri $url -TimeoutSec 1 -OutVariable response
            if ($response -ne $null) {
                Write-Output "Server responded to GET $url with status code $($response.StatusCode)"
                break
            }
        } catch {
            Write-Warning "Failed to make a request to $url with exception $_"
            $timeoutCounter++

            # if the process is not running, exit with a non-zero exit code
            if (-not (Get-Process -Id $server_pid -ErrorAction SilentlyContinue)) {
                Write-Warning "Server process with PID $server_pid is not running, crash detected. Exiting."
                exit 1
            }
            Start-Sleep -Seconds 1
            continue
        }
        Write-Warning "Server is not responding to requests at $url yet (timeout $timeoutMs), sleeping 1 second"
        # Find what's new in the output log that isn't in $output
        $newOutput = Get-Content "./output.log" | Select-Object -Skip $output.Length
        Write-Output $newOutput

        Start-Sleep -Seconds 1
        $timeoutCounter++
    }

    $testsFailed = 0
    try
    {
        # test /imageflow/version
        $version = Invoke-WebRequest -Uri http://localhost:5000/imageflow/version
        if ($LASTEXITCODE -ne 0)
        {
            Write-Error "Request to /imageflow/version failed with exit code $LASTEXITCODE"
            $testsFailed += 1
        }
    } catch {
        Write-Error "Request to /imageflow/version failed with exception $_"
        $testsFailed += 1
    }

    # test /imageflow/resize/width/10
    try
    {
        $resize = Invoke-WebRequest -Uri http://localhost:5000/imageflow/resize/width/10
        if ($LASTEXITCODE -ne 0)
        {
            Write-Warning "Request to /imageflow/resize/width/10 failed with exit code $LASTEXITCODE"
            $testsFailed += 1
        }
    } catch {
        Write-Warning "Request to /imageflow/resize/width/10 failed with exception $_"
        $testsFailed += 1
    }
    # exit with a non-zero exit code if any tests failed
    if ($testsFailed -ne 0)
    {
        Write-Warning "$testsFailed tests failed. Exiting."
    }
} finally {

    # kill the server
    Stop-Process -Id $server_pid
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Failed to kill the server process with PID $server_pid"
    }

    # print the process output
    Get-Content "./output.log"

    # restore the current directory
    Set-Location $currentDir

    # exit with a non-zero exit code if any tests failed

    if ($testsFailed -ne 0) {
        Write-Warning "$testsFailed tests failed. Exiting."
        exit 1
    }
    Write-Output "YAYYYY"
    Write-Output "All tests passed. Exiting."
    exit 0
}
