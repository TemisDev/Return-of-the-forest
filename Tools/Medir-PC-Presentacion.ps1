param(
    [ValidateRange(30,120)][int]$SecondsPerState = 30,
    [ValidateRange(1,5)][int]$Repetitions = 3,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$brRoot = Split-Path $PSScriptRoot -Parent
$brOut = if ([IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $brRoot $OutputDirectory }
$brOut = [IO.Path]::GetFullPath($brOut)
if (Test-Path -LiteralPath $brOut) { throw 'Usar una carpeta de sesion nueva; no se sobrescriben mediciones.' }
if (Get-CimInstance Win32_Process -Filter "Name='Unity.exe' OR Name='BosqueDeRegreso.exe'" |
    Where-Object { $_.CommandLine -like ('*'+$brRoot+'*') }) { throw 'Cerrar el editor/jugador de esta copia antes de medir.' }
$brNv = Get-Command nvidia-smi -ErrorAction SilentlyContinue
if (-not $brNv) { throw 'No se encontro nvidia-smi para registrar temperatura real de esta GPU.' }
$brVersions = @(
    @{name='V6';folder='V6';commit='7b94a1da145563104d12b92c0ebe4a243bc102a7';test='M13'},
    @{name='V7';folder='V7-Character-Audio';commit='8a944c8e05a8616603968230499e134ca2002ade';test='B2-M10'}
)
foreach ($brV in $brVersions) {
    $brInfo = Get-Content (Join-Path $brRoot ('Builds/Windows/'+$brV.folder+'/build-info.json')) -Raw | ConvertFrom-Json
    if ($brInfo.commit -ne $brV.commit) { throw ('Build diferente a la identificada: '+$brV.name) }
}
[IO.Directory]::CreateDirectory($brOut) | Out-Null
$brBattery = @(Get-CimInstance -Namespace root/wmi -ClassName BatteryStatus -ErrorAction SilentlyContinue |
    Select-Object PowerOnline,Charging,Discharging)
$brPower = (& powercfg /getactivescheme) -join ' '
$brPlan = if ($brPower -match '\(([^)]+)\)') { $Matches[1] } else { 'No disponible' }
[ordered]@{date=(Get-Date).ToString('o');presentationPcConfirmedByUser=$true;
    cpu=(Get-CimInstance Win32_Processor|Select-Object -First 1 -ExpandProperty Name);
    physicalMemoryBytes=(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory;
    windows=(Get-CimInstance Win32_OperatingSystem).Caption;powerPlan=$brPlan;battery=$brBattery;
    resolution='1920x1080';mode='Windowed';secondsPerState=$SecondsPerState;repetitionsPerBuild=$Repetitions;
    order='V6,V7 alternadas; sin enfriamiento artificial';gpuTelemetry='nvidia-smi cada 5s; temperatura, utilizacion y reloj';
    method='Patrulla API; no es sesion manual de Diego; p95 de tiempos por frame, no 1% low';
    criterion='Ambos estados deben tener p95 <= 1000/60 ms en todas las repeticiones';
    background='Sin editor ni otro jugador de esta copia; aplicaciones de escritorio no controladas'} |
    ConvertTo-Json -Depth 5 | Set-Content (Join-Path $brOut 'environment.json') -Encoding utf8
$brRows = [Collections.Generic.List[object]]::new()
for ($brI=1; $brI -le $Repetitions; $brI++) {
    foreach ($brV in $brVersions) {
        $brName = $brV.name+'-RUN-'+$brI.ToString('00')
        $brRun = Join-Path $brOut $brName
        $brGpuCsv = Join-Path $brOut ($brName+'-gpu.csv')
        Write-Output ('Iniciando '+$brName+' en PC de presentacion')
        # Una invocacion por muestra escribe inmediatamente; -l/-f pierde el buffer al detener el proceso.
        $brSensor = Start-Job -ArgumentList $brNv.Source,$brGpuCsv -ScriptBlock {
            param($brTool,$brCsv)
            'timestamp,name,temperature.gpu,utilization.gpu,clocks.current.graphics,pstate' | Set-Content $brCsv
            while ($true) {
                & $brTool --query-gpu=timestamp,name,temperature.gpu,utilization.gpu,clocks.current.graphics,pstate --format=csv,noheader,nounits | Add-Content $brCsv
                Start-Sleep -Seconds 5
            }
        }
        try {
            & (Join-Path $PSScriptRoot 'Verify-ArroyoV4.ps1') -Executable ('Builds/Windows/'+$brV.folder+'/BosqueDeRegreso.exe') `
                -OutputDirectory $brRun -Width 1920 -Height 1080 -SecondsPerState $SecondsPerState -AllowMissingReleaseCounters
        } finally {
            Stop-Job $brSensor
            Receive-Job $brSensor | Out-Null
            Remove-Job $brSensor
        }
        $brData = Get-Content (Join-Path $brRun 'results.json') -Raw | ConvertFrom-Json
        $brWin = Get-Content (Join-Path $brRun 'windows-memory.json') -Raw | ConvertFrom-Json
        $brTemps = @(Import-Csv $brGpuCsv | ForEach-Object {
            $brT = $_.PSObject.Properties | Where-Object { $_.Name.Trim() -eq 'temperature.gpu' } | Select-Object -First 1
            $brValue = 0.0
            if ($brT -and [double]::TryParse($brT.Value.Trim(),[ref]$brValue)) { $brValue }
        })
        foreach ($brState in @(@('degraded',$brData.degraded),@('restored',$brData.recovered))) {
            $brSample=$brState[1]
            if ($brSample.p95FrameMs -le 0) { throw 'No hay p95 valido para decidir el criterio.' }
            $brRows.Add([pscustomobject]@{case=$brV.test;version=$brV.name;run=$brI;state=$brState[0];
                buildCommit=$brV.commit;meanFps=$brSample.meanFps;p95FrameMs=$brSample.p95FrameMs;
                minFps=$brSample.minFps;seconds=$brSample.seconds;peakWorkingSetMiB=$brWin.peakWorkingSetMiB;
                gpuMinC=($brTemps|Measure-Object -Minimum).Minimum;gpuMaxC=($brTemps|Measure-Object -Maximum).Maximum;
                gpuSamples=$brTemps.Count;criterionMet=($brSample.p95FrameMs -le (1000.0/60));
                automaticVerificationPassed=$brData.passed;normalExitCode=$brWin.exitCode;
                evidence=$brName+'/results.json';thermalEvidence=$brName+'-gpu.csv'})
        }
        $brRows.ToArray() | Export-Csv (Join-Path $brOut 'mediciones.csv') -NoTypeInformation -Encoding utf8
        $brRows.ToArray() | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $brOut 'mediciones.json') -Encoding utf8
    }
}
foreach ($brV in $brVersions) {
    $brSubset = @($brRows | Where-Object {$_.version -eq $brV.name})
    $brFailed = @($brSubset | Where-Object {-not $_.criterionMet})
    Write-Output ($brV.test+' '+$brV.name+': '+$(if($brFailed.Count){'FALLIDO'}else{'APROBADO (medicion instrumentada)'})+
        '; fases fuera de criterio: '+$brFailed.Count+'/'+$brSubset.Count)
}
