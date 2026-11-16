# Plan QA MVP — Arroyo dormido

**Fecha de preparación:** 15-11-2026. **Estado:** matriz diseñada, **no ejecutada**. Fuente: `CONTEXTO-PARA-OTRAS-IA.md` (fotografía de 13-11-2026). Los nombres M-01..M-22 son una **matriz de cobertura propuesta**, no una transcripción exacta del archivo `Docs/QA/ARROYO-ACEPTACION.md`, que no fue proporcionado. Deben conciliarse con el documento original antes de reemplazar nada.

## Alcance y fuentes

- Escena a probar: `Game/Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity`.
- Motor: Unity 6000.3.22f1 / URP.
- Controles documentados: WASD, ratón, espacio, Escape (solo libera cursor), clic izquierdo (recaptura).
- No considerar E ni Q funcionales sin nueva evidencia.
- Las pruebas automatizadas anteriores 6/6 y la sonda C-02 22 OK **son resultados históricos reportados**, no reproducidos en este paquete.

## Entregables

`Matriz/casos-manuales-propuestos.csv`: 22 pruebas manuales para revisar junto a M-01..M-22 original.

`Matriz/incidencias-conocidas.csv`: I-01..I-05, sin cierres ficticios.

`Matriz/pruebas-futuras-T08-T09.csv`: batería de regresión cuando exista interacción/restauración.

`PLANTILLA-INCIDENCIA.md` e `INFORME-INICIAL.md`: documentación de ejecución y diagnóstico.

## Ejecución

1. Clonar la rama, abrir `Game/` en Unity Hub con versión requerida.
2. Abrir **ArroyoPlayable**, no el greybox aislado. Anotar hash (`git rev-parse --short HEAD`), hardware y versión.
3. Ejecutar casos uno a uno. Adjuntar capturas/videos nombrados por ID.
4. Marcar exclusivamente APROBADO, FALLIDO, BLOQUEADO o NO EJECUTADO.
5. Si falla: reproducir nuevamente, crear informe con pasos exactos y severidad.
6. Repetir casos afectados tras correcciones y en build Windows distinta del Editor.
7. Para juego compartido entre asistentes, no operar dos procesos de Unity sobre el mismo proyecto.

## Severidad

P0: impide ejecutar/completar; P1: impide mecánica principal; P2: afecta control/uso con alternativa; P3: cosmético o menor.

## Criterios mínimos para aceptar release

Sin P0/P1 abiertas; T08/T09 terminadas y comprobadas, salida real desbloqueable; flujo completo reproducible; sin excepciones bloqueantes; prueba manual documentada; build Windows probada; contraste antes/después de restauración comprensible; registro de cambios.

## Nota sobre GDD y QA original

No sobrescribir el `Docs/QA/ARROYO-ACEPTACION.md` existente. Abrirlo y cotejar números/pasos del conjunto M original con esta nueva matriz antes de asignar su nombre oficial.
