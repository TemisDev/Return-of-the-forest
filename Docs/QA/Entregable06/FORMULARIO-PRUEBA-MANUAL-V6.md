# Hoja de campo — prueba manual del Arte V6

Rellenar a mano mientras se juega. Una hoja por sesión y por equipo. **No marcar APROBADO sin haberlo hecho de verdad.**

- Tester:
- Fecha y hora:
- Equipo (CPU / GPU / RAM / Windows):
- Qué se probó: escena `ArroyoV6_Showcase` en editor / build. Commit de la rama `feature/arroyo-art-v6`:
- Resolución y modo (ventana o pantalla completa):
- ¿Había otros programas pesados abiertos?

## Casos (ver detalle en MATRIZ-QA-V6.csv)

| ID | Resultado (APROBADO / FALLA / BLOQUEADO) | Observación breve | Evidencia (archivo) |
|---|---|---|---|
| M01 Movimiento y salto | | | |
| M02 E en los 3 obstáculos + polvo al retirar | | | |
| M03 Bloqueos reconocibles sin ayuda | | | |
| M04 Cámara junto al muro oeste | | | |
| M05 La vegetación no tapa al jugador ni los objetivos | | | |
| M06 Transición al restaurar | | | |
| M07 Reinicio tras 1/3 y 3/3 | | | |
| M08 Arco y mensaje ODS 15 | | | |
| M09 HUD legible en ambos estados | | | |
| M10 Confort visual (5 min) | | | |
| M11 Agua y charcos | | | |
| M12 Viento y LOD de la vegetación | | | |
| M13 FPS en la PC de presentación | | | |
| M14 Pantalla completa y 1280×800 | | | |

**Además, comprobar a propósito** (son hallazgos de la sonda; confirmar o descartar):

- [ ] En X≈1,75 Z≈30 (orilla, antes de la salida), ¿el personaje choca con algo que no se ve? (QA6-013)
- [ ] Cerca del inicio, junto al árbol del borde derecho, ¿choca con un poste invisible o atraviesa el tronco? (QA6-014)
- [ ] Al empujar contra la barrera, ¿el personaje se mete dentro de las raíces? (QA6-001)
- [ ] ¿Hay un congelamiento de 1 a 4 segundos al arrancar la build? (QA6-005)

## M15 — build integrada

- Ejecutable: `Builds/Windows/V6/BosqueDeRegreso.exe`.
- Abre `build-info.json`, al lado del ejecutable, y anota aquí su `commit`, que debe ser `2cc512c…`: ______
- Repite M01–M14 sobre esta build y anota los resultados en una hoja aparte, marcada como M15.
- Además, comprueba:
  - [ ] QA6-001: al empujar contra la barrera, el personaje no se mete en las raíces.
  - [ ] QA6-002: cerca de los árboles de la orilla derecha, ninguna rama atraviesa al personaje.
  - [ ] QA6-003: no hay tocones en la orilla; ahora están en lo alto del muro izquierdo.
  - [ ] QA6-004: en el tronco caído (primer obstáculo), el personaje no pisa la madera al pulsar E.
  - [ ] QA6-009: al retirar el tercer obstáculo se ve **un solo** efecto de partículas, junto al personaje.
  - [ ] QA6-005: desde que se abre la ventana hasta que se puede mover, ¿cuántos segundos pasan? ___ s. ¿Hay algún parón **durante** el juego?

## UX con una persona ajena (U01–U04)

No explicar la solución. Solo leer en voz alta la primera pantalla si lo pide.

- U01 Dudas y ayudas que necesitó:
- U02 Tiempo del primer recorrido completo (cronómetro): ___ min ___ s
- U03 Al terminar, «¿qué cambió?» (respuesta literal):
- U04 «¿Qué te indicaban los brillos verdes o los iconos?» (respuesta literal):

## Cierre

- Incidencias nuevas registradas en `INCIDENCIAS-V6.csv` (IDs):
- Decisión del tester: APTO / APTO CON CONDICIONES / NO APTO, y por qué:
