function Get-Configuration {
    $username = Get-Current-Username
    $projectRootPath = Get-Project-Root-Path
    $projectPath = "$projectRootPath/com.unity.hlod/Samples~"
    #$editorExecutablePath = "C:/Program Files\Unity\Hub\Editor\2019.2.14f1\Editor\Unity.exe"
    $editorExecutablePath = "C:/Users/$username/m2/M2/development/Unity/WinEditor/Unity.exe"
    $editorLogPath = "C:/Users/$username/AppData/Local/Unity/Editor/Editor.log"
    $editorCrashLogPath = "C:/Users/$username/AppData/Local/Temp/Unity/Editor/Crashes"    
    $testResultsOutputFolder = "$projectRootPath/scripts/TestResults"
    $logOutputFolder = "$projectRootPath/scripts/Logs"
    
    $result = @{
        "EditorExecutablePath"    = $editorExecutablePath;
        "EditorLogPath"           = $editorLogPath;
        "EditorCrashLogPath"      = $editorCrashLogPath;
        "ProjectRootPath"         = $projectRootPath;
        "ProjectPath"             = $projectPath;
        "TestResultsOutputFolder" = $testResultsOutputFolder;
        "LogOutputFolder"         = $logOutputFolder;
    }

    return $result;
}

function Get-Current-Username {
    return $env:username
}

function Get-Project-Root-Path {
    return  Get-Location | Split-Path -Parent
}