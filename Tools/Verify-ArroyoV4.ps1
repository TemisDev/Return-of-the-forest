param(
    [ValidateRange(640,3840)][int]$Width = 1920,
    [ValidateRange(480,2160)][int]$Height = 1080,
    [ValidateRange(3,120)][int]$SecondsPerState = 60,
    [string]$OutputDirectory = '.dev-logs/v4-validation-1080'
)
$ErrorActionPreference = 'Stop'
$brRoot = Split-Path $PSScriptRoot -Parent
$brExe = Join-Path $brRoot 'Builds/ArroyoV4/BosqueDeRegreso.exe'
if (-not (Test-Path -LiteralPath $brExe)) { throw 'Primero generar la build Windows V4.' }
if (-not [IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory = Join-Path $brRoot $OutputDirectory }
$brOutput = [IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $brOutput) -and (Get-ChildItem -LiteralPath $brOutput -Force | Select-Object -First 1)) {
    throw 'Usar una carpeta de resultados nueva o vacia para no confundir ejecuciones.'
}
[IO.Directory]::CreateDirectory($brOutput) | Out-Null
$brLog = Join-Path $brOutput 'Player.log'
$brArguments = @('-screen-width', $Width, '-screen-height', $Height, '-screen-fullscreen', '0',
    '-arroyo-verify', '-arroyo-verify-seconds', $SecondsPerState, '-arroyo-benchmark-route',
    '-arroyo-verify-output', ('"' + $brOutput + '"'), '-logFile', ('"' + $brLog + '"'))
$brPlayer = Start-Process -FilePath $brExe -ArgumentList $brArguments -WindowStyle Normal -PassThru
$brSamples = [Collections.Generic.List[object]]::new()
$brClock = [Diagnostics.Stopwatch]::StartNew()
while (-not $brPlayer.HasExited) {
    $brPlayer.Refresh()
    if (-not $brPlayer.HasExited) {
        $brPhase = 'startup'
        if (Test-Path -LiteralPath (Join-Path $brOutput '01-degraded-interaction.png')) { $brPhase = 'degraded' }
        if (Test-Path -LiteralPath (Join-Path $brOutput '02-restoration-effect.png')) { $brPhase = 'restored' }
        if (Test-Path -LiteralPath (Join-Path $brOutput '04-completed.png')) { $brPhase = 'completed' }
        $brSamples.Add([pscustomobject]@{seconds=[Math]::Round($brClock.Elapsed.TotalSeconds,3);
            phaseByCapture=$brPhase; workingSetMiB=[Math]::Round($brPlayer.WorkingSet64 / 1MB,3)})
        Start-Sleep -Seconds 1
    }
}
$brPlayer.WaitForExit()
$brSamples | Export-Csv -LiteralPath (Join-Path $brOutput 'memory.csv') -NoTypeInformation -Encoding utf8
$brPeak = ($brSamples | Measure-Object workingSetMiB -Maximum).Maximum
[ordered]@{source='External Windows .NET Process.WorkingSet64'; intervalSeconds=1; width=$Width; height=$Height;
    exitCode=$brPlayer.ExitCode; peakWorkingSetMiB=$brPeak; sampleCount=$brSamples.Count;
    phaseNote='Capture milestones approximate phases; include startup and transitions; not VRAM or instantaneous peak';
    phases=@($brSamples | Group-Object phaseByCapture | ForEach-Object {
        [pscustomobject]@{phase=$_.Name;peakWorkingSetMiB=($_.Group|Measure-Object workingSetMiB -Maximum).Maximum;samples=$_.Count}
    })} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $brOutput 'windows-memory.json') -Encoding utf8
$brResult = Get-Content -LiteralPath (Join-Path $brOutput 'results.json') -Raw | ConvertFrom-Json
if ($brPlayer.ExitCode -ne 0 -or -not $brResult.passed -or $brPeak -le 0 -or
    $brResult.width -ne $Width -or $brResult.height -ne $Height -or
    -not $brResult.degraded.drawCallsAvailable -or -not $brResult.recovered.drawCallsAvailable) {
    throw "Verificacion incompleta: revisar $brOutput"
}
Write-Output "Verificacion aprobada, proceso cerrado normalmente. Resultados: $brOutput"
Write-Output "Working set maximo observado: $brPeak MiB"
