# Resultados automatizados V16 — registro y procedimiento

**Estado:** NO EJECUTADO. Los siguientes comandos son una guía; no se han ejecutado aquí. Diego debe confirmar nombre real de `Unity.exe`, versión de editor, ruta y si la suite está disponible.

## 1. Identidad del proyecto

- Base indicada: `feature/valle-olvidado`, `f09f186263b21c7754645f264cefe891ccadb713`.
- Copia independiente de Diego, proyecto dentro de `Game/`.
- Salida XML dentro de `Docs/QA/V16/EVIDENCIAS/AUTOMATIZADAS/` en archivos distintos por corrida.
- `build-info.json`, hash y commit registrados por separado para pruebas en build.

## 2. Ejecución sugerida (PowerShell)

Desde la raíz del repositorio de Diego, con Unity **cerrado en esa copia**:

```powershell
$Repo = (Get-Location).Path
$Editor = 'C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe'
$Project = Join-Path $Repo 'Game'
$Out = Join-Path $Repo 'Docs\QA\V16\EVIDENCIAS\AUTOMATIZADAS'
New-Item -ItemType Directory -Force $Out | Out-Null

& $Editor -batchmode -quit -projectPath $Project -runTests -testPlatform EditMode -testResults (Join-Path $Out 'editmode-base.xml') -logFile (Join-Path $Out 'editmode-base.log')
$EditExit = $LASTEXITCODE

& $Editor -batchmode -quit -projectPath $Project -runTests -testPlatform PlayMode -testResults (Join-Path $Out 'playmode-base.xml') -logFile (Join-Path $Out 'playmode-base.log')
$PlayExit = $LASTEXITCODE

"EditMode exit: $EditExit | PlayMode exit: $PlayExit"
```

Si el Unity Test Runner admite filtrado por suite o clase en esta instalación, ejecutar también `V16PlayModeTests` como test específico. No afirmar que un filtro fue ejecutado antes de verificar que el XML contiene **los casos correctos**. Si se ejecutan las suites completas, puede registrarse la clase desde el XML sin repetir tests innecesarios.

**Nota:** una escena que dependa de renderizado puede no validarse correctamente con `-nographics`; se omite deliberadamente. Si alguna suite requiere modo interactivo, indicar el bloqueo y ejecutarla en editor.

## 3. Resultados reales por campaña (completar)

| Campaña | Suite | Commit del código | XML | Aprobadas | Fallidas | Omitidas | Estado |
|---|---|---|---|---|---|---|---|
| BASE | EditMode | PENDIENTE | PENDIENTE | — | — | — | NO EJECUTADO |
| BASE | PlayMode | PENDIENTE | PENDIENTE | — | — | — | NO EJECUTADO |
| BASE | V16PlayModeTests (verificar presencia) | PENDIENTE | PENDIENTE | — | — | — | NO EJECUTADO |
| REGRESION | EditMode | PENDIENTE | PENDIENTE | — | — | — | NO EJECUTADO |
| REGRESION | PlayMode | PENDIENTE | PENDIENTE | — | — | — | NO EJECUTADO |
| REGRESION | V16PlayModeTests (verificar presencia) | PENDIENTE | PENDIENTE | — | — | — | NO EJECUTADO |

## 4. Auditoría de huecos

- **Inventario en Claro:** la prueba cuyo nombre promete todas las zonas habría omitido Claro. Registrar cobertura real observada en la implementación actual, y probar manualmente **Tab/I en Claro**.
- **Fundido sonoro:** observar si una prueba fuerza el estado restaurado instantáneamente. Un cambio de variables de mezcla no prueba la transición **oída** por una persona.
- **Cierre:** un exit code del proceso Unity Test Runner no equivale al cierre de una build Windows después de jugar.

No reescribir aserciones, silenciar errores, convertir fallos en skipped ni crear XML simulados para lograr una suite verde. Guardar nombre del fallo, logs, condiciones y reproducción en `INCIDENCIAS-V16.csv`.
