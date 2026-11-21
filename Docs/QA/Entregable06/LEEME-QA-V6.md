# QA V6 — revisión del Arte V6 realista

**Proyecto:** Bosque de regreso · Rama propuesta `feature/arroyo-qa-v6` · Revisa la entrega de Juan `feature/arroyo-art-v6`.

## Estado

- La parte automatizada está ejecutada: **41 OK, 1 AVISO y 1 FALLA** en la sonda sobre el ZIP de Juan; tras corregir las P1, **43 OK, 0 AVISO, 0 FALLA**, y 6 mediciones de rendimiento.
- La parte manual y la de UX están **NO EJECUTADO**.
- Decisión actual: **apto para prueba manual**. Las 2 incidencias P1 de colisión invisible (QA6-013 y QA6-014) están corregidas en `feature/arroyo-art-v6-fix-p1`. Ver [INFORME-ACEPTACION-V6.md](INFORME-ACEPTACION-V6.md).

## Contenido

| Archivo | Para qué |
|---|---|
| `INFORME-ACEPTACION-V6.md` | Decisión, resultados, condiciones para integrar |
| `PLAN-QA-V6.md` | Alcance, criterios de entrada y salida, riesgos |
| `MATRIZ-QA-V6.csv` | 37 casos: 18 automatizados con resultado y 19 manuales/UX pendientes |
| `INCIDENCIAS-V6.csv` | 15 incidencias: 2 P1, 5 P2 y 8 P3 (incluye 1 observación) |
| `REVALIDACION-HISTORICA-V6.csv` | I-01 a I-05 frente al arte V6 |
| `RENDIMIENTO-V6.csv` | 18 fases medidas en 6 ejecuciones (FPS, p50, p95, p99, máximo) |
| `FORMULARIO-PRUEBA-MANUAL-V6.md` | Hoja de campo para la sesión con teclado y ratón, y para la de UX |
| `PROCEDIMIENTO-V6.md` | Cómo repetir la sonda y la medición |
| `PLANTILLA-INCIDENCIA.md` | Formato para incidencias nuevas |
| `EVIDENCIAS/` | Salida de la sonda (`.csv` y `.json`) y medición en frío |
| `Tools/ejecutar_qa_v6.ps1` | Ejecuta la sonda en una carpeta de resultados nueva |
| `COMANDOS-CMD.txt` | Cómo subir esta entrega a GitHub |

El código de la sonda está en `Game/Assets/_Bosque/Tests/Acceptance/Editor/ArroyoQAV6Probe.cs`, junto con su `.meta`. Se abre desde el menú **Tools > Bosque > QA > V6 - Ejecutar sonda**. Es de solo lectura: abre las escenas sin guardarlas y crea colliders de medida solo en memoria.

## Reglas

- Las filas con **APROBADO (automatizado)** solo valen por el método indicado. No sustituyen la prueba con una persona.
- Cualquier corrección de arte obliga a repetir la sonda y los casos afectados.
- No se cierran incidencias por inferencia: hace falta una nueva ejecución con evidencia.
- No subir ejecutables, `Player.log` sin revisar ni rutas personales.
