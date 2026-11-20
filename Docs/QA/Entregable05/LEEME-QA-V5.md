# QA V5 — puerta de aceptación RC1
**Proyecto:** Bosque de regreso · Rama `feature/arroyo-qa-v5` · Meta propuesta: 24/11/2026.

## Estado
El usuario aprobó nueve pruebas manuales en V2 (movimiento/salto, cámara, E, obstáculos, 3/3, restauración, salida, reinicio, consola). No se dispone del SHA exacto de aquella ejecución ni de una build RC1 para inspeccionar. Final Polish se reportó publicado, no validado aquí. Todos los resultados V5 empiezan NO EJECUTADO. Las incidencias históricas no se cierran por inferencia.

## Proceso
1. Confirmar que Final Polish fue integrado en develop y que Codex entregó una build Windows RC1 completa con SHA de origen, ruta al EXE y configuración de compilación. QA puede preparar su rama antes de recibir la build, pero no confundir el SHA de su rama documental con el de la build.
2. Verificar que `Docs/QA/Entregable04/MATRIZ-QA-V4.csv` está disponible: los 41 casos V4 siguen vigentes. `PUERTA-RC1.csv` selecciona comprobaciones de cierre en Windows, no reemplaza todos esos casos. Para registrar ejecución de V4, copiarla a Entregable05 con otro nombre, conservar sus IDs, añadir evidencia real y documentar entorno por ejecución; no sobrescribir el original.
3. Ejecutar la build con `Tools/ejecutar_build.ps1` como se indica en `PROCEDIMIENTO-RC1.md`. El script registra hash del EXE, datos básicos de equipo, log solicitado, exit code y marcas temporales. No juega por el usuario ni mide FPS.
4. Completar la puerta de RC1; guardar evidencias reales relativas a este directorio; registrar incidencias nuevas en `INCIDENCIAS-RC1.csv`. Revalidar históricas.
5. Medir rendimiento con una herramienta conocida y registrar método, resolución, calidad y tiempos. El objetivo de 60 FPS a 1080p es orientativo, no un resultado.
6. Generar resumen `py -3 Docs/QA/Entregable05/Tools/resumen_rc1.py`; completar `INFORME-ACEPTACION-RC1.md` y consultar a Codex si quedan P0/P1.
7. Subir solo documentación y evidencias revisadas. No incluir el EXE, datos generados de Unity o información personal. Los logs pueden contener rutas privadas: revisar y anonimizar antes de publicarlos.

Un pase registrado exige SHA de build, entorno, resultado y evidencia que realmente existe. Cualquier cambio a gameplay/recursos obliga a reevaluar los casos afectados en la nueva build antes de liberar.
