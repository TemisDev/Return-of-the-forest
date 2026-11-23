param(
    [Parameter(Mandatory=$true)][ValidateSet('V6','V7')][string]$Build,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [switch]$PrepareOnly
)
$ErrorActionPreference = 'Stop'
$brRoot = Split-Path $PSScriptRoot -Parent
$brFolder = if ($Build -eq 'V6') { 'V6' } else { 'V7-Character-Audio' }
$brExpected = if ($Build -eq 'V6') { '7b94a1da145563104d12b92c0ebe4a243bc102a7' } else { '8a944c8e05a8616603968230499e134ca2002ade' }
$brBuildDir = Join-Path $brRoot ('Builds/Windows/'+$brFolder)
$brInfo = Get-Content (Join-Path $brBuildDir 'build-info.json') -Raw | ConvertFrom-Json
if ($brInfo.commit -ne $brExpected) { throw 'Build distinta: actualizar la ficha de QA antes de ensayar.' }
$brExe = Join-Path $brBuildDir 'BosqueDeRegreso.exe'
if (-not (Test-Path -LiteralPath $brExe)) { throw 'Falta el ejecutable Windows.' }
$brOut = if ([IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $brRoot $OutputDirectory }
$brOut = [IO.Path]::GetFullPath($brOut)
if (Test-Path -LiteralPath $brOut) { throw 'Usar carpeta nueva para conservar sesiones anteriores.' }
if (-not $PrepareOnly -and (Get-Process -Name Unity,BosqueDeRegreso -ErrorAction SilentlyContinue)) {
    throw 'Cerrar el editor y los jugadores antes de la sesion manual.'
}
[IO.Directory]::CreateDirectory($brOut) | Out-Null
Copy-Item -LiteralPath (Join-Path $brRoot 'Docs/Character/SESION-DIEGO.md') -Destination (Join-Path $brOut 'INSTRUCCIONES.md')
$brIds = if ($Build -eq 'V7') { 1..9 | ForEach-Object {'B2-M'+$_.ToString('00')} } else {
    @('M01','M02','M03','M04','M05','M06','M07','M08','M09','M10','M11','M12','M14','M15')
}
$brIds | ForEach-Object { [pscustomobject]@{id=$_;build=$Build;commit=$brExpected;
    responsable='Diego';estado='NO EJECUTADO';resultadoObservado='';evidencia='';incidencia='';fecha=''} } |
    Export-Csv (Join-Path $brOut 'RESULTADOS-DIEGO.csv') -NoTypeInformation -Encoding utf8
$brSession = [ordered]@{build=$Build;commit=$brExpected;executableSha256=(Get-FileHash $brExe -Algorithm SHA256).Hash;
    created=(Get-Date).ToString('o');mode='Manual, sin patrulla ni verificador';preparedOnly=[bool]$PrepareOnly;
    reviewer='Diego (pendiente de confirmacion humana)';requestedResolution='1920x1080 windowed';exitCode=$null;started=$null;finished=$null}
$brSession | ConvertTo-Json | Set-Content (Join-Path $brOut 'sesion.json') -Encoding utf8
if ($PrepareOnly) { Write-Output ('Sesion preparada sin abrir el juego: '+$brOut); return }
$brArgs = @('-screen-width','1920','-screen-height','1080','-screen-fullscreen','0',
    '-logFile',('"'+(Join-Path $brOut 'Player.log')+'"'))
$brSession.started=(Get-Date).ToString('o')
$brSession | ConvertTo-Json | Set-Content (Join-Path $brOut 'sesion.json') -Encoding utf8
$brPlayer=Start-Process -FilePath $brExe -WorkingDirectory $brBuildDir -ArgumentList $brArgs -WindowStyle Normal -PassThru
$brPlayer.WaitForExit()
$brSession.finished=(Get-Date).ToString('o'); $brSession.exitCode=$brPlayer.ExitCode
$brSession | ConvertTo-Json | Set-Content (Join-Path $brOut 'sesion.json') -Encoding utf8
Write-Output ('Juego cerrado. Diego debe completar RESULTADOS-DIEGO.csv: '+$brOut)
