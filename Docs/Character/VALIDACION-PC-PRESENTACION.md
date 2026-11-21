# Validación en la PC de presentación — 20/11/2026

## Alcance y decisión

El usuario confirmó que esta es la PC de presentación. Diego no estaba disponible; se preparó [su sesión](SESION-DIEGO.md) y no se ejecutaron B2-M01–B2-M09 ni M15 como pruebas humanas.

M13 (V6 del PR #7) y B2-M10 (V7 de personaje/audio) se midieron mediante la patrulla automatizada existente. **Ambos FALLIDOS**: el estado restaurado excedió el p95 de 16,67 ms en alguna repetición. QA6-006 sigue abierto y M15 no puede aprobarse todavía.

## Método

Intel Core i5-10300H, NVIDIA GTX 1650, 16 GB de RAM, Windows 11. Cargador conectado; plan Equilibrado. Direct3D 12, ventana 1920×1080, calidad PC, VSync 0, sin límite de FPS, renderScale 0,85, dos cascadas de sombras y distancia de 40 m. No se modificaron ajustes ni escena para este ensayo.

Tres ejecuciones por versión, alternando V6/V7, 30 segundos por estado, sin enfriamiento artificial. Sin Unity ni otro jugador de esta copia; las aplicaciones del escritorio no estaban controladas. El criterio adoptado es p95 ≤ 1000/60 ms en ambos estados y en las tres repeticiones. La media de FPS no sustituye este criterio.

Las builds no Development conservan el verificador optativo de QA; se activa únicamente para medir. La patrulla y los eventos se impulsan por API: no prueban la comodidad del teclado, ratón, cámara ni la percepción auditiva.

V6 compila `7b94a1d`; el código de Game es idéntico al head `4c973e5` del PR #7. V7 compila `8a944c8`; el posterior `51eb0c6` documenta resultados y cambia de PNG a JPG las capturas de una prueba PlayMode, sin cambios al runtime de esa build. No se mezclan sus aceptaciones.

## Resultados

| Versión | Ejecución | FPS medio degradado / restaurado | p95 ms degradado / restaurado | RAM pico MiB |
|---|---|---|---|---|
| V6 | 1 | 112.00 / 78.38 | 11.60 / 15.41 | 723.64 |
| V6 | 2 | 87.45 / 62.66 | 14.14 / 19.16 | 734.14 |
| V6 | 3 | 77.43 / 62.26 | 16.96 / 21.15 | 723.36 |
| V7 | 1 | 95.75 / 67.13 | 13.33 / 18.80 | 765.09 |
| V7 | 2 | 80.38 / 59.88 | 15.20 / 19.91 | 769.16 |
| V7 | 3 | 75.86 / 61.63 | 16.37 / 21.29 | 775.82 |

Los JSON completos y hashes de ejecutable están en [EVIDENCIAS/PC-PRESENTACION-20261120](EVIDENCIAS/PC-PRESENTACION-20261120/). `automaticVerificationPassed` comprueba el recorrido funcional por API y es independiente de `criterionMet`, que decide rendimiento. Los errores de consola se revisan por separado; el cierre normal no aprueba las observaciones humanas.

Seis de seis recorridos funcionales aprobados por API y seis cierres normales (código 0). Revisión de los seis Player.log: cero coincidencias de `Exception`, `NullReference`, `MissingReference`, `Shader error` o `ERROR`; es una búsqueda acotada, no una inspección humana de la consola. La suite Unity anterior no se volvió a ejecutar porque no hubo cambios al juego. Capturas reales V7: [degradado](EVIDENCIAS/PC-PRESENTACION-20261120/V7-01-degraded-interaction.png) y [restaurado](EVIDENCIAS/PC-PRESENTACION-20261120/V7-03-restored.png).

## Telemetría y límites

El recolector inicial `nvidia-smi -l -f` dejó archivos vacíos al detenerse. No se atribuyen temperaturas a esas ejecuciones ni se rellenan sus campos nulos. Se conservó un muestreo parcial independiente, con marca horaria, iniciado durante la sesión: `telemetria-parcial.csv`. El intervalo exacto y el rango térmico se registran al cerrar el informe. No cubre el principio de las dos primeras ejecuciones y no demuestra por sí solo la causa de la caída de rendimiento.

El script reutilizable se corrigió para invocar una consulta por muestra y persistirla inmediatamente. Se comprobó que deja muestras válidas al detener su tarea. Las seis mediciones de esta sesión utilizan el recolector original; sus percentiles proceden del jugador y no dependen de ese recolector térmico.

## Entrega a Diego y siguiente corrección

El lanzador `Tools/Iniciar-Sesion-Diego.ps1` valida el commit declarado y registra SHA256, fechas, log y código de salida. En modo de preparación se comprobaron ambas fichas, nueve casos V7 inicialmente NO EJECUTADO y el rechazo a sobrescribir una sesión existente. No se abrió una sesión humana con este lanzador.

Para aprobar: revisar el coste gráfico del estado restaurado con perfil, contrastar energía/refrigeración del equipo y repetir exactamente estas mediciones después de una mejora demostrada. La telemetría registrada es evidencia del entorno, no prueba causal de throttling. No bajar el criterio ni declarar 60 FPS estables a partir de una sola ejecución fría. Después, Diego debe completar las dos fichas con observaciones y evidencias reales; B2-M09 requiere auriculares y M15 incluye la aceptación de M13.

No se cambió gameplay, escena, recursos, paquetes ni ajustes Unity. No se fusionó ningún PR.


Muestreo parcial publicado: 50 muestras, 2026/11/20 17:09:57.970 a 2026/11/20 17:14:08.129 (hora local), rango GPU 85–93 °C. Es una instantánea del muestreo parcial; no completa las muestras térmicas nulas de las corridas.
