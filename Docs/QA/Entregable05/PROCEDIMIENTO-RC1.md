# Procedimiento reproducible — Windows RC1
Desde la raíz del repositorio, con la build completa recibida de Codex, abrir PowerShell:

```powershell
powershell -NoProfile -File "Docs/QA/Entregable05/Tools/ejecutar_build.ps1" -Exe "C:/ruta/a/Bosque.exe" -BuildCommit "SHA_REAL_DE_7_A_40_HEX" -Session "RC1-001"
```

Sustituir la ruta y SHA por los reales. No cambiar políticas de seguridad del equipo; si PowerShell bloquea scripts, lanzar manualmente el EXE con `-logFile` y registrar resultados. El script requiere Windows PowerShell y espera a que cierres normalmente el juego. No transmite información a servicios externos. Revisa el log antes de publicarlo. La huella del EXE identifica el binario, no demuestra por sí sola el SHA del código; debe provenir del reporte de build de Codex.

1. Registrar resolución 1920×1080 si está disponible, nivel de calidad, VSync/límite FPS y herramienta de medición. No cambiar entre corridas.
2. Recorrido de referencia: spawn, comprobar salida bloqueada, recorrer obstáculos, retirar 1/3 y reiniciar, retirar los tres, observar restauración, llegar a salida, reiniciar y repetir, cerrar normalmente.
3. Separar las pruebas negativas de la medición de rendimiento; repetir tres recorridos equivalentes a resolución fija. Registrar duración, media/mínimo de FPS según la herramienta y RAM pico; indicar si se midió memoria de proceso o sistema. No presentar mínimo como 1% low.
4. Ejecutar V4 y la puerta RC1. Para APROBADO/FALLIDO indicar observación, SHA de build, entorno y ruta de evidencia; BLOQUEADO debe explicar la dependencia. Vincular un ID de incidencia si falla.
5. Guardar cada corrección en un commit identificable y reejecutar los casos en la build derivada; generar una copia de resultados para conservar histórico de RC1 y RC2.

## Puerta de entrega
No liberar con fallos P0/P1 abiertos, pruebas P0/P1 sin ejecutar o evidence faltante. Casos P2 no aprobados requieren decisión humana documentada y riesgo aceptado. Para el pase definitivo se requiere comprobación de V4, informe de Arte, validación de compilación y prueba en una segunda PC si está disponible. La decisión final la toma el coordinador, no el script.
