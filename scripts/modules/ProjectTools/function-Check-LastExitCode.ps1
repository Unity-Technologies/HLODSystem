function Check-LastExitCode {
    if ($LastExitCode -ne 0) {
        $msg = @"
EXE RETURNED EXIT CODE $LastExitCode
CALLSTACK:$(Get-PSCallStack | Out-String)
"@
        throw $msg
    }
}
