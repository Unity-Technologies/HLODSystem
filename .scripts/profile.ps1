# sets up the "profile" for the current session.
# Mainly set up where modules can be found.

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$modulesDir  = Join-Path -Path $scriptDir -ChildPath modules

#set it up so that this profile can be included multiple times
if(-not($env:PSModulePath.Contains($modulesDir)))
{
    $env:PSModulePath = $env:PSModulePath + [System.IO.Path]::PathSeparator + $modulesDir
}

$ErrorActionPreference = "Stop"
