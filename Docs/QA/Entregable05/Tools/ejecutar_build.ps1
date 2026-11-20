param(
    [Parameter(Mandatory=$true)][string]$Exe,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-fA-F]{7,40}$')][string]$BuildCommit,
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9_-]+$')][string]$Session
)
$ErrorActionPreference = 'Stop'
$resolved = (Resolve-Path -LiteralPath $Exe).Path
if (-not (Test-Path -LiteralPath $resolved -PathType Leaf) -or [IO.Path]::GetExtension($resolved) -ne '.exe') { throw 'Se requiere un ejecutable .exe existente.' }
$root = Join-Path (Split-Path -Parent $PSScriptRoot) 'EVIDENCIAS'
$dest = Join-Path $root $Session
if (Test-Path -LiteralPath $dest) { throw 'La sesion ya existe; elige un identificador nuevo.' }
New-Item -ItemType Directory -Path $dest | Out-Null
$log = Join-Path $dest 'Player.log'
$report = Join-Path $dest 'sesion.json'
$sys = Get-CimInstance Win32_ComputerSystem
$cpu = Get-CimInstance Win32_Processor | Select-Object -First 1 -ExpandProperty Name
$gpu = @(Get-CimInstance Win32_VideoController | Select-Object -ExpandProperty Name)
$info = [ordered]@{session=$Session; sha_reportado_por_codex=$BuildCommit; exe=[IO.Path]::GetFileName($resolved); sha256=(Get-FileHash -LiteralPath $resolved -Algorithm SHA256).Hash; cpu=$cpu; gpu=$gpu; ram_gb=[math]::Round($sys.TotalPhysicalMemory/1GB,2); windows=[Environment]::OSVersion.VersionString; inicio=(Get-Date).ToString('o'); fin=$null; exit_code=$null; player_log_presente=$false; resultado='NO EVALUADO'}
$info | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $report -Encoding UTF8
try {
    Write-Host 'Realiza las pruebas manualmente y cierra normalmente el juego.'
    $proc = Start-Process -FilePath $resolved -WorkingDirectory (Split-Path -Parent $resolved) -ArgumentList @('-logFile', ('"' + $log + '"')) -PassThru -Wait
    $info.exit_code = $proc.ExitCode
} finally {
    $info.fin=(Get-Date).ToString('o'); $info.player_log_presente=Test-Path -LiteralPath $log -PathType Leaf
    $info | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $report -Encoding UTF8
}
Write-Host "Evidencia: $dest. Revisa y anonimiza los logs antes de publicar. No implica aprobacion."
