param([string] $testPlatform, [string] $testResults)

Write-Output "Running $testPlatform Tests"

. $PSScriptRoot/profile.ps1
Import-Module ProjectTools
$cfg = Get-Configuration

Write-Output "Project Root: $($cfg['ProjectRootPath'])"
Write-Output "Create Artifact Directories"
New-Item -ItemType Directory -Force -Path $cfg["LogOutputFolder"]
New-Item -ItemType Directory -Force -Path $cfg["TestResultsOutputFolder"]

$testResultsFilePath = "$($cfg['TestResultsOutputFolder'])/$testResults"

$unityParams = @(
    "-projectpath", $cfg["ProjectPath"],
    "-runTests",
    "-testPlatform", $testPlatform,
    "-testResults", $testResultsFilePath
)

$process = (Start-Process -PassThru -Wait -FilePath $cfg["EditorExecutablePath"] -ArgumentList $unityParams)

Copy-Item -Path $cfg["EditorLogPath"] -Destination "$($cfg['LogOutputFolder'])/Editor.log"

if ($process.ExitCode -ne 0) {
    if (Test-Path $cfg["EditorCrashLogPath"]) {        
        Copy-Item -Path $cfg["EditorCrashLogPath"] -Destination "$($cfg['LogOutputFolder'])/" -recurse -Force        
    }

    Write-Output "Running $testPlatform Tests Failed. Printing last 200 lines of Editor.log:"
    Get-Content -Tail 200  $cfg["EditorLogPath"]
    
    throw "Running Tests Failed with exit code $($process.ExitCode)."
}

Write-Output "All tasks completed."