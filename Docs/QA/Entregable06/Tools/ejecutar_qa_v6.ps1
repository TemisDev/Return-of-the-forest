# Ejecuta la sonda QA V6 en Unity (modo batch) y conserva la salida en una carpeta nueva.
# Uso (desde la raíz del repositorio):  & .\Docs\QA\Entregable06\Tools\ejecutar_qa_v6.ps1
param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe"
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
if (-not (Test-Path $Unity)) { throw "No se encontró Unity 6000.3.22f1 en $Unity" }
if (Test-Path (Join-Path $root 'Game\Temp\UnityLockfile')) {
    Write-Warning 'Puede haber un Unity abierto sobre Game/. Ciérralo antes de continuar.'
}
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$out = Join-Path $root ".dev-logs\qa-v6-$stamp"
New-Item -ItemType Directory -Force $out | Out-Null
$log = Join-Path $out 'unity.log'
$p = Start-Process -FilePath $Unity -ArgumentList @('-batchmode', '-projectPath', "`"$root\Game`"",
    '-executeMethod', 'Bosque.Acceptance.ArroyoQAV6Probe.RunBatch', '-logFile', "`"$log`"") -Wait -PassThru
Copy-Item (Join-Path $root 'Docs\QA\Entregable06\EVIDENCIAS\sonda-qa-v6.*') $out
Select-String -Path $log -Pattern '\[QA6\]' | ForEach-Object { $_.Line }
switch ($p.ExitCode) {
    0 { 'Resultado: sin FALLA' }
    2 { 'Resultado: hay FALLA (ver líneas [QA6] ... FALLA)' }
    default { "Error de ejecución (código $($p.ExitCode)); revisar $log" }
}
"Salida guardada en $out (fuera de Git; revisar rutas personales antes de compartir el log)"
exit $p.ExitCode
