# Plan QA — Entregable 03

**Alcance:** integración artística V3 y regresión del gameplay T08/T09 del Arroyo dormido. **Fecha objetivo:** 23 de noviembre de 2026.

## Línea base confirmada por el usuario
En la conversación, el 16 de noviembre el usuario reportó **9/9 comprobaciones manuales aprobadas** en su versión previa: movimiento/salto, cámara, interacción E, retirada de 3 obstáculos, contador 3/3, cambio visual, desbloqueo, reinicio y consola sin errores rojos. Esto **no valida automáticamente** la siguiente versión con arte integrado.

## Orden de trabajo
1. Identificar commit, rama, escena, versión de Unity, sistema operativo, CPU/RAM/GPU y captura de consola inicial.
2. Ejecutar `REGRESION-FUNCIONAL.csv` (12 casos).
3. Si el arte V3 está integrado, ejecutar `AUDITORIA-VISUAL.csv` (11 casos), capturando antes/después.
4. Ejecutar `PRUEBAS-RENDIMIENTO.csv` (8 casos) en build Windows y anotar herramientas, FPS mínimos/promedio y condiciones. No rellenar con estimaciones.
5. Revalidar las incidencias históricas I-01..I-05; no marcarlas resueltas sin evidencia.
6. Emitir `INFORME-ACEPTACION.md` y crear issues reproducibles para fallos P0/P1.

## Estados válidos
APROBADO, FALLIDO, BLOQUEADO, NO EJECUTADO. Todas las matrices se entregan inicialmente como **NO EJECUTADO**.

## Criterios de salida
Ningún P0/P1 abierto sin decisión explícita; T08/T09 y reinicio completos; build verificada; referencias visuales correctas; pruebas repetibles con evidencia.

## Limitaciones
Estos documentos se prepararon sin ejecutar Unity ni ver la build final. No confundir preparación de pruebas con ejecución.
