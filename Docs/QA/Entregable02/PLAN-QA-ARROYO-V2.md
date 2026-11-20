# Plan QA — Entregable 02 / Arroyo dormido

**Estado:** preparación documental, sin sesiones manuales ejecutadas.
**Producto:** Bosque de regreso, Unity 6000.3.22f1 / PC Windows.
**Alcance:** interacciones T08, progreso/restauración/salida/reinicio T09, regresión de movimiento/cámara, arte visual y build.

## Orden recomendado
1. Verificar `git rev-parse HEAD`, escena y versión de Unity; abrir `Game/Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity`.
2. Ejecutar la matriz **original** de `Docs/QA/ARROYO-ACEPTACION.md` M-01..M-22, documentando resultado real; no reinterpretar los IDs a partir de este ZIP.
3. Tras el PR de Codex, probar `MATRIZ-T08-T09.csv`; registrar commit exacto.
4. Tras integración de Arte, probar `MATRIZ-VISUAL.csv` sobre la escena actualizada.
5. Probar la build Windows en equipo independiente cuando exista; si no existe, BLOQUEADO.

## Estados y severidad
Estados: APROBADO, FALLIDO, BLOQUEADO, NO EJECUTADO. Severidad: P0 bloquea ejecución/entrega; P1 bloquea la mecánica principal; P2 problema relevante no bloqueante; P3 cosmético.

## Regla de evidencia
Cada resultado APROBADO/FALLIDO requiere: commit, fecha, versión del Editor o build, pasos reproducibles, resultado real, equipo y evidencia disponible. Los resultados de Claude de C-02 fueron reportados anteriormente; no cuentan como una ejecución de esta ronda.

## Criterio de salida (propuesto)
- 0 defectos P0 y P1 abiertos; T08/T09 y final de nivel comprobados manualmente.
- Las regresiones críticas de cámara/salto no aparecen en el commit final.
- El arte importado no bloquea interacción, visión o movimiento.
- Se entrega informe con limitaciones y casos no ejecutados; evitar reportar cobertura 100% si hay bloqueados.
