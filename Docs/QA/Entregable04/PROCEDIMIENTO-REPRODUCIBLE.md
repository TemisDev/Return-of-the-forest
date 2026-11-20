# Protocolo de ejecución QA

## Preflight
```cmd
git status
git rev-parse --short HEAD
git branch --show-current
```
- Anotar nombre exacto de la build, ruta del ejecutable, hash opcional, hora local, commit, Unity, Windows y hardware.
- Preferir una build desde una rama integrada, no de la rama exclusiva de QA.
- Verificar que los artefactos de Juan estén realmente **colocados** en la escena; disponer de PNG en disco no es integración visual.

## Repetición
- Empezar cada caso en el estado inicial que corresponda; usar R para restablecer si está disponible.
- Grabar los pasos mínimos; anotar el resultado observado en vez de marcar simplemente APROBADO.
- Etiquetar evidencia `CASO_commit_fecha.ext` (sin datos privados ni rutas completas de usuarios).
- Si se detecta bug, crear incidencia reproducible y asociar ID, commit, log y captura.

## Medición
- FPS: indicar herramienta (Unity Profiler / overlay), momento de prueba, resolución, calidad, número de repeticiones, máximo/mínimo/promedio cuando disponible. No inventar datos.
- RAM: especificar herramienta y si se midió Editor o build.
- Player.log: registrarlo sin contraseñas, rutas sensibles o datos personales.

## Prioridad
- P0: bloquea inicio o finalización / corrupción grave.
- P1: mecánica central rota o fallo recurrente.
- P2: impacto visible o de experiencia con alternativa.
- P3: cosmético o molestia menor.

## Aceptación
No declarar el juego «sin errores» por ausencia de errores rojos en el Editor; hace falta registrar pruebas de build y regresión.
