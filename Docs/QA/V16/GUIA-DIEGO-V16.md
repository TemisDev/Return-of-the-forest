# Guía operativa de Diego — QA integral V16

## Antes de copiar este paquete

1. Desde **tu propio clon** (no la carpeta del integrador): leer `AGENTS.md`, los relevos y `GUIA-RAMAS-Y-PR.md` del repositorio.
2. Ejecutar `git status --short`; si hay cambios locales, preservarlos antes de cambiar de rama. `git fetch origin`.
3. Verificar que `origin/feature/valle-olvidado` corresponde a la base esperada `f09f186263b21c7754645f264cefe891ccadb713`; si la rama avanzó, **no retrocederla de manera destructiva**: documentar y coordinar.
4. Crear `feature/qa-v16-diego` sobre la base acordada, en una copia separada. No reutilizar una rama de pruebas antigua.
5. Extraer el ZIP **en la raíz del repo**, de forma que los archivos queden bajo `Docs/QA/V16/`.
6. Recibir por canal acordado la **carpeta completa** `Builds/Windows/V16-Sonido-Solidez/`, fuera de Git; revisar `build-info.json` y datos asociados. No ejecutar solo el `.exe` aislado.

### Comandos de ejemplo (PowerShell)

```powershell
# Ejecutar desde el repositorio de Diego, con el estado de trabajo limpio

git status --short
git fetch origin
git switch -c feature/qa-v16-diego f09f186263b21c7754645f264cefe891ccadb713

git rev-parse HEAD
```

La creación falla si ya existe la rama: inspeccionarla primero; **no** borrar ni forzar. Si el integrador cambió la base, consultar la guía de ramas real y coordinar sin destruir commits.

### Identificar y registrar build

```powershell
$BuildDir = 'RUTA_ABSOLUTA_A_LA_CARPETA_COMPLETA_DE_LA_BUILD'
Get-ChildItem -LiteralPath $BuildDir -Recurse -Filter build-info.json
Get-Content (Join-Path $BuildDir 'build-info.json')  # Ajustar si está en otra subcarpeta
$Exe = Join-Path $BuildDir 'BosqueDeRegreso.exe'
Get-FileHash -LiteralPath $Exe -Algorithm SHA256
(Get-Item -LiteralPath $Exe).Length
```

Confirmar los nombres reales. La fecha de la build, su commit declarado, el hash del ejecutable, resolución, API y equipo se registran en cada campaña de la matriz.

### Pruebas por prioridad

- **P0 primero:** identidad de build, campaña completa sin atajos, colisiones necesarias, inventario Tab/I por zona, interacción y reinicio, cambio de zonas, QA automática y cierre.
- **P1 después:** legibilidad UI/arte, audio y calidad del fundido, análisis de rendimiento y prueba con jugadores.
- **P2/P3:** defectos cosméticos menores si el plazo lo permite; documentar sin bloquear automáticamente.

### Rendimiento: precauciones

```powershell
# Solo como muestra aislada del Valle degradado si se inicia allí.
# Ejecutar desde la carpeta completa de la build y después de observar que el exe existe.
.\BosqueDeRegreso.exe -zona valle -medir-fps 30 -salir
```

El comando espera seis segundos, mide la escena activa durante treinta y solicita cierre. No hace una campaña, no restaura el Valle y no mide FPS restaurado. Arroyo se alcanza con Nueva partida; no existe `-zona arroyo` en el encargo. Las otras zonas aceptan `-zona claro` y `-zona refugio`. Para **restaurado y recorrido**, seguir el gameplay real o solicitar instrumentación adicional al integrador. No rellenar métricas sin ejecución. Registrar la fila correspondiente en `RENDIMIENTO-V16.csv`.

### Cierre D3D12

Conservar Player.log, código de salida y argumentos de cada uno de hasta cinco cierres. Registrar en `CIERRES-D3D12-V16.csv`. Si se reproduce, usar `-force-d3d11` únicamente para contraste diagnóstico, sin modificar la API predeterminada.

### Regresión tras Juan

No se acepta la misma carpeta renombrada: obtener **una nueva build identificada** del integrador después de integrar arte. Verificar `build-info.json` y SHA-256. Mantener `MATRIZ-BASE-V16.csv` sin sobrescribir y registrar segunda campaña en `MATRIZ-REGRESION-V16.csv`. Si un cambio de Juan afecta algo no cubierto por la matriz de regresión, **añadir caso específico**.

### Publicación final

```powershell
git status --short
git add -- Docs/QA/V16/
git diff --cached --stat
git commit -m "test: document V16 baseline and post-art QA evidence"
git push -u origin feature/qa-v16-diego
```

No incluir códigos, escenas, `Runtime/UI`, binarios de builds, rutas privadas, grabaciones con datos personales o archivos `Library/`. PR: `feature/qa-v16-diego` → **`feature/valle-olvidado`**. Adjuntar evidencia pertinente y declarar expresamente las pruebas no ejecutadas. **No fusionar automáticamente**.

Fecha propuesta: base 24–25/11/2026; regresión y dictamen 26/11/2026 18:00 hora de Lima.
