param(
    [ValidateRange(3,120)][int]$SecondsPerState = 60,
    [int]$Width = 1920, [int]$Height = 1080,
    [ValidateSet(0,1)][int]$Fullscreen = 0,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [switch]$Diagnostics
)
$brCandidate = if ($Diagnostics) { 'RC1-Diagnostics' } else { 'RC1' }
& (Join-Path $PSScriptRoot 'Verify-ArroyoV4.ps1') -Executable "Builds/Windows/$brCandidate/BosqueDeRegreso.exe" `
    -OutputDirectory $OutputDirectory -SecondsPerState $SecondsPerState -Width $Width -Height $Height `
    -Fullscreen $Fullscreen -AllowMissingReleaseCounters
