# Bloque 03: Zona 2, "El claro vacío"

**Rama:** `feature/character-animation-audio`, commits `7855cdd` y `40b863c`.

**Build:** `Builds/Windows/V8-Zona2-Claro/BosqueDeRegreso.exe`, commit `40b863c`, 0 errores. Incluye el Arroyo y el Claro. La build anterior del bloque (`7855cdd`) se conserva en `Builds/Windows/V8-Zona2-Claro-7855cdd`.

## Qué se juega

1. **Transición desde el Arroyo:**
   - Al cruzar el arco del arroyo aparece el aviso "Nueva habilidad: CRECIMIENTO [Q]".
   - A los 4,5 s se carga el claro. Pulsar R antes lo cancela.
2. **Puzle del claro:** en un claro seco hay **cinco lechos de siembra**.
   - **Habilidad:** con **Q** se hace brotar una planta nativa en el lecho cercano.
   - **Lechos válidos:** solo prospera en los **tres de suelo húmedo**. La pista es visual: tierra más oscura y brillante, musgo y un reguero que va del manantial al borde de la barranca.
   - **Lechos secos:** no crece nada, aparece una pista ("Aquí no prospera…") y suena tierra seca. Se puede volver a intentar en otro lecho.
   - Cada lecho crece una sola vez, aunque se pulse Q repetidamente.
3. **Recompensa:**
   - Con los tres brotes, el claro revive: vegetación, cielo, niebla, aves, brisa y música.
   - Crece un **puente de raíces** sobre la barranca. Tiene 4 m, más que el salto, y caer devuelve al punto seguro.
4. **Salida:**
   - Al cruzar el puente y el arco norte se completa la zona y aparece el mensaje ODS 15.
   - Por ahora indica "Siguiente zona: El refugio perdido (próximamente)".
5. **Teclas:** R reinicia la zona y F1 muestra la ayuda.

**Para probar el claro sin jugar el arroyo:** `BosqueDeRegreso.exe -zona claro`.

## Sistemas

Son reutilizables para la zona 3 y las mecánicas siguientes.

| Pieza | Archivo | Qué hace |
|---|---|---|
| Progreso de la partida | `Runtime/World/GameProgress.cs` | Zonas completadas y habilidades, solo en memoria; el GDD no pide guardado. Completar el arroyo concede el crecimiento. |
| Habilidad de crecimiento | `Runtime/Abilities/GrowthAbility.cs` | Q, alcance de 2,6 m, enfriamiento de 0,6 s. Eventos `Grew` y `Rejected` (con la pista). |
| Lecho de siembra | `Runtime/World/GrowthPoint.cs` | Suelo apto o no, pista, planta y animación de crecimiento (`GrowthReveal`). |
| Zona | `Runtime/Session/ClaroSession.cs`, `ClaroExit.cs` | Ver la nota debajo de la tabla. |
| Interfaz | `Runtime/Session/ClaroHud.cs` | Objetivo, progreso de brotes, aviso [Q], pista tras un error, ayuda F1 y mensaje final. |
| Audio y gestos | `Runtime/Session/ClaroAudioBridge.cs` | Brote, intento fallido, puente, restauración, ambiente y música. La Guardiana hace su gesto de restaurar al brotar. |
| Transición | `Runtime/Session/ArroyoZoneTransition.cs` | Arroyo → Claro. El verificador de builds (`-arroyo-verify`) no cambia de escena. |
| Atajo de pruebas | `Runtime/Session/ZoneLauncher.cs` | `-zona claro`. |
| Construcción de la escena | `Editor/Levels/Claro/ClaroBuilder.cs` | Ver la nota debajo de la tabla. |

**Zona:** usa el mismo modelo de progreso que el arroyo, con los lechos aptos como objetivos. La salida (`ClaroExit.cs`) funciona igual que la del arroyo.

**Construcción de la escena:**
- Idempotente: `Bosque.LevelEditor.ClaroBuilder.BuildAllBatch`.
- Usa el kit de arte V6, la Guardiana y el audio de zona.
- Registra las dos escenas en Build Settings.

## Medidas del nivel

- Campo sur en z −4..20, barranca en z 20..24 (4 m), campo norte en z 24..42 y arco en z 41. Muros de 28 × 46 m.
- Lechos húmedos en (−6,5; 4,5), (−10,5; 10,5) y (2,6; 18,2) junto al puente. Lechos secos en (7; 6) y (10,5; 14).
- Renderers: 241 en degradado y 548 en restaurado (el arroyo tiene 1100).

## Resultados automatizados

Esto no sustituye a las pruebas manuales.

| Prueba | Resultado |
|---|---|
| PlayMode del claro (`ClaroPlayModeTests`) | **8/8** |
| PlayMode completo (Guardiana + claro) | **18/18** |
| EditMode | **38/38** |
| Sonda V6 integrada del arroyo (con la transición añadida) | **43 OK, 0 FALLA** |
| Verificador de builds sobre V8 (arroyo) | `passed=True`; restaurado 71 FPS, p95 17,2 ms |
| Arranque de la build con `-zona claro` (25 s) | Sin errores ni excepciones en el log |

Qué cubren las pruebas del claro:
- la habilidad está bloqueada hasta completar el arroyo;
- en suelo seco hay pista y no hay avance, sin repetir el sonido;
- cada lecho húmedo brota una sola vez;
- la restauración hace crecer el puente con colisión, con sonidos únicos y ambiente progresivo;
- la barranca no se puede saltar y caer devuelve al punto seguro;
- el puente lleva a la salida y la zona se completa;
- el reinicio deja la zona limpia;
- la transición real arroyo → claro con la habilidad desbloqueada;
- las capturas.

Capturas: [CAPTURAS/](CAPTURAS/).

## Pruebas manuales pendientes

Son para Diego. **No están ejecutadas.**

| ID | Pasos | Esperado |
|---|---|---|
| Z2-M01 | Completar el arroyo y esperar en el arco. | Aviso de la habilidad y paso al claro sin parones largos. |
| Z2-M02 | Sin pistas verbales, ¿encuentra los lechos buenos? (contar intentos fallidos) | Lee el reguero y el barro como "humedad". |
| Z2-M03 | Pulsar Q en un lecho seco y luego mantenerla. | Una pista, sin sonidos repetidos y sin bloquear el movimiento. |
| Z2-M04 | Hacer brotar los tres lechos húmedos. | El brote se ve crecer; gesto y sonido una vez por lecho. |
| Z2-M05 | Intentar saltar la barranca. | No se puede; la caída devuelve al inicio. |
| Z2-M06 | Cruzar el puente de raíces. | Se camina sin trabas y la cámara no se mete en el tronco. |
| Z2-M07 | Pulsar R a mitad y al final. | Todo vuelve al estado seco, sin plantas ni puente. |
| Z2-M08 | FPS en la PC de la presentación con `-zona claro`. | Igual o mejor que el arroyo. |

## Pendiente

- La zona 3 ("El refugio perdido") y la fauna. Hasta entonces, el claro termina con el mensaje ODS 15 y un aviso.
- No se midió el rendimiento del claro en la build con el verificador, porque este solo recorre el arroyo. Tiene la mitad de renderers que el arroyo restaurado.
- La animación de crecimiento es un escalado con rebote. Hace falta validarla con jugadores (Z2-M04).
