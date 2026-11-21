param(
    [ValidateSet('Candidate','Diagnostics','V6','V6-Diagnostics','V7-Character-Audio','V8-Zona2-Claro','V9-Zona3-Refugio','V10-Presentacion')][string]$Configuration = 'Candidate',
    [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe',
    [string]$Git = 'git'
)
$ErrorActionPreference = 'Stop'
$brRoot = Split-Path $PSScriptRoot -Parent
if (-not (Get-Command $Git -ErrorAction SilentlyContinue)) {
    $Git = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/native/git/cmd/git.exe'
}
if (-not (Get-Command $Git -ErrorAction SilentlyContinue)) { throw 'Indicar -Git con la ruta de git.exe.' }
if (-not (Test-Path -LiteralPath $Unity)) { throw 'No se encontro el editor Unity acordado.' }
$brCommit = (& $Git -C $brRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $brCommit -notmatch '^[0-9a-f]{40}$') { throw 'No se pudo identificar el commit.' }
if (& $Git -C $brRoot status --porcelain) { throw 'Guardar los cambios en commits antes de generar una build identificada.' }
$brProject = Join-Path $brRoot 'Game'
$brRunning = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -like ('*'+$brProject+'*') }
if ($brRunning) { throw 'Cerrar el editor o batch de este proyecto antes de compilar.' }
[IO.Directory]::CreateDirectory((Join-Path $brRoot '.dev-logs')) | Out-Null
$brLog = Join-Path $brRoot ('.dev-logs/rc1-build-'+$Configuration+'-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'.log')
$brMethod = switch ($Configuration) {
    'Candidate' { 'Bosque.CoreSetup.ArroyoBuild.BuildRC1WindowsBatch' }
    'Diagnostics' { 'Bosque.CoreSetup.ArroyoBuild.BuildRC1DiagnosticsBatch' }
    'V6' { 'Bosque.CoreSetup.ArroyoBuild.BuildV6WindowsBatch' }
    'V6-Diagnostics' { 'Bosque.CoreSetup.ArroyoBuild.BuildV6DiagnosticsBatch' }
    'V7-Character-Audio' { 'Bosque.CoreSetup.ArroyoBuild.BuildV7WindowsBatch' }
    'V8-Zona2-Claro' { 'Bosque.CoreSetup.ArroyoBuild.BuildV8WindowsBatch' }
    'V9-Zona3-Refugio' { 'Bosque.CoreSetup.ArroyoBuild.BuildV9WindowsBatch' }
    'V10-Presentacion' { 'Bosque.CoreSetup.ArroyoBuild.BuildV10WindowsBatch' }
}
$brPreviousCommit = $env:BOSQUE_BUILD_COMMIT
try {
    $env:BOSQUE_BUILD_COMMIT = $brCommit
    $brProcess = Start-Process $Unity -ArgumentList @('-batchmode','-projectPath',('"'+$brProject+'"'),
        '-executeMethod',$brMethod,'-logFile',('"'+$brLog+'"')) -WindowStyle Hidden -PassThru
    $brProcess.WaitForExit()
    if ($brProcess.ExitCode -ne 0) { throw "Build fallida ($($brProcess.ExitCode)); revisar $brLog" }
} finally { $env:BOSQUE_BUILD_COMMIT = $brPreviousCommit }
Write-Output "Build $Configuration terminada; commit $brCommit; log $brLog"
