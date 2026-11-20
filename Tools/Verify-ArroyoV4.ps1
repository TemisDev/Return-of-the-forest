param(
    [ValidateRange(640,3840)][int]$Width = 1920,
    [ValidateRange(480,2160)][int]$Height = 1080,
    [ValidateRange(3,120)][int]$SecondsPerState = 60,
    [string]$OutputDirectory = '.dev-logs/v4-validation-1080',
    [string]$Executable = 'Builds/ArroyoV4/BosqueDeRegreso.exe',
    [ValidateSet(0,1)][int]$Fullscreen = 0,
    [switch]$AllowMissingReleaseCounters
)
$ErrorActionPreference = 'Stop'
$brRoot = Split-Path $PSScriptRoot -Parent
$brExe = if ([IO.Path]::IsPathRooted($Executable)) { $Executable } else { Join-Path $brRoot $Executable }
if (-not (Test-Path -LiteralPath $brExe)) { throw 'Primero generar la build Windows solicitada.' }
if (-not [IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory = Join-Path $brRoot $OutputDirectory }
$brOutput = [IO.Path]::GetFullPath($OutputDirectory)
if ((Test-Path -LiteralPath $brOutput) -and (Get-ChildItem -LiteralPath $brOutput -Force | Select-Object -First 1)) {
    throw 'Usar una carpeta de resultados nueva o vacia para no confundir ejecuciones.'
}
[IO.Directory]::CreateDirectory($brOutput) | Out-Null
$brLog = Join-Path $brOutput 'Player.log'
$brArguments = @('-screen-width', $Width, '-screen-height', $Height, '-screen-fullscreen', $Fullscreen,
    '-arroyo-verify', '-arroyo-verify-seconds', $SecondsPerState, '-arroyo-benchmark-route',
    '-arroyo-verify-output', ('"' + $brOutput + '"'), '-logFile', ('"' + $brLog + '"'))
$brPlayer = Start-Process -FilePath $brExe -WorkingDirectory (Split-Path -Parent $brExe) -ArgumentList $brArguments -WindowStyle Normal -PassThru
$brSamples = [Collections.Generic.List[object]]::new()
$brClock = [Diagnostics.Stopwatch]::StartNew()
$brFirstCaptureSeconds = $null
while (-not $brPlayer.HasExited) {
    $brPlayer.Refresh()
    if (-not $brPlayer.HasExited) {
        if ($null -eq $brFirstCaptureSeconds -and (Test-Path -LiteralPath (Join-Path $brOutput '00-spawn.png'))) {
            $brFirstCaptureSeconds = $brClock.Elapsed.TotalSeconds
        }
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
    executableSha256=(Get-FileHash -LiteralPath $brExe -Algorithm SHA256).Hash;
    requestedFullscreen=$Fullscreen; launchToFirstCaptureObservedSeconds=$brFirstCaptureSeconds;
    launchWorkingDirectory='Executable folder';
    exitCode=$brPlayer.ExitCode; peakWorkingSetMiB=$brPeak; sampleCount=$brSamples.Count;
    phaseNote='Capture milestones approximate phases; include startup and transitions; not VRAM or instantaneous peak';
    phases=@($brSamples | Group-Object phaseByCapture | ForEach-Object {
        [pscustomobject]@{phase=$_.Name;peakWorkingSetMiB=($_.Group|Measure-Object workingSetMiB -Maximum).Maximum;samples=$_.Count}
    })} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $brOutput 'windows-memory.json') -Encoding utf8
$brResult = Get-Content -LiteralPath (Join-Path $brOutput 'results.json') -Raw | ConvertFrom-Json
if ($brPlayer.ExitCode -ne 0 -or -not $brResult.passed -or $brPeak -le 0 -or
    $brResult.width -ne $Width -or $brResult.height -ne $Height) {
    throw "Verificacion incompleta: revisar $brOutput"
}
if (-not $brResult.degraded.drawCallsAvailable -or -not $brResult.recovered.drawCallsAvailable) {
    if (-not $AllowMissingReleaseCounters -or $brResult.development) { throw 'Contadores de render ausentes.' }
}
# Non-development players may omit Profiler counters. Check the actual framebuffer instead.
Add-Type -AssemblyName System.Drawing
foreach ($brName in @('01-degraded-interaction.png','03-restored.png','04-completed.png')) {
    $brImage = [Drawing.Bitmap]::new((Join-Path $brOutput $brName))
    try {
        if ($brImage.Width -ne $Width -or $brImage.Height -ne $Height) { throw 'Resolucion de captura inesperada.' }
        $brColors = [Collections.Generic.HashSet[int]]::new()
        foreach ($brX in 5..9) { foreach ($brY in 4..8) {
            $brColors.Add($brImage.GetPixel([int]($Width*$brX/10),[int]($Height*$brY/10)).ToArgb()) | Out-Null
        }}
        if ($brColors.Count -lt 5) { throw "Framebuffer sin variacion verificable: $brName" }
    } finally { $brImage.Dispose() }
}
Write-Output "Verificacion aprobada, proceso cerrado normalmente. Resultados: $brOutput"
Write-Output "Working set maximo observado: $brPeak MiB"
