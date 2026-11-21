# Plan QA V4 — Regresión + validación de release

**Proyecto:** Bosque de regreso, Arroyo dormido, Unity 6000.3.22f1 (versión documentada).  
**Meta:** evaluar integración visual y build Windows antes de entrega del 27 de noviembre.  
**Responsable:** Diego · rama `feature/arroyo-qa-v4`.

## Línea base (no es resultado V4)
El usuario reportó **9/9 pruebas manuales básicas aprobadas** sobre una versión anterior: movimiento/salto, cámara, interacción E, obstáculos, progreso, restauración, salida, reinicio, sin errores rojos en consola. Este antecedente NO acredita la build V4.

## Paquete de pruebas
`MATRIZ-QA-V4.csv`: **41 casos** (9 regresión, 10 negativas, 10 visuales, 8 build/rendimiento, 4 UX). Todos empiezan como `NO EJECUTADO`.

## Orden de trabajo
1. Anotar commit exacto bajo prueba: `git rev-parse HEAD`; rama, escena, fecha, SO/CPU/RAM/GPU y versión Unity/build.
2. Ejecutar R01–R09 y N01–N10 primero sobre integración V4 disponible.
3. Ejecutar V01–V10 después de que Codex haya colocado los assets de Arte V4 en la escena.
4. Ejecutar B01–B08 sobre build Windows distribuible, nunca solo desde Editor.
5. Hacer U01–U04 con al menos una persona diferente a quien programó, registrar respuesta sin guion.
6. Revalidar I-01..I-05, conservando su historial si no se reproducen.
7. Añadir captura/log o referencia verificable por caso; emitir informe de aceptación con recomendación.

## Estados
- **APROBADO**: ejecutado y cumple el esperado.
- **FALLIDO**: ejecutado y no cumple; abrir incidencia.
- **BLOQUEADO**: no se puede ejecutar por dependencia.
- **NO EJECUTADO**: aún no intentado.

## Criterio de salida
- 0 incidencias P0 abiertas; P1 resueltas o aceptadas explícitamente por responsable técnico con riesgo documentado.
- 9 casos de regresión ejecutados en la build final o justificar ausencia.
- Evidencia de apertura, recorrido y finalización en build Windows.
- Estado de materiales, cámara e interacción tras arte final verificado.
- Reporte de hardware, commit, FPS medidos y casos bloqueados.

**Sin ejecución real no se puede emitir APTO.**
