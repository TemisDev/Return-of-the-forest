# Pruebas de aceptación: Arroyo (C-01 / T07 → T10)
Preparado por Claude Code el 11 de noviembre de 2026. Ejecuta: Diego. Integra: Codex.
Escena de nivel: `Game/Assets/_Bosque/Levels/Arroyo/ArroyoGreybox.unity`, generada con `Tools > Bosque > Niveles > Arroyo - Generar o actualizar greybox`.

Las pruebas A-xx se ejecutan sobre la escena de nivel. Las pruebas M-xx (C-02) cubren movimiento y cámara de T06 en `Game/Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity` y ya se pueden ejecutar. Las B-xx que dependen de interacción (T08) o restauración (T09) siguen bloqueadas. Ninguna prueba manual está marcada como superada: solo cuenta lo que una persona haya jugado con teclado y ratón.

La sección R recoge comprobaciones **automatizadas** (Claude, batch) sobre el controlador real. Ayudan a Diego a saber dónde mirar, pero no sustituyen las pruebas M.

## Cómo registrar
- Estado: `Pendiente`, `OK`, `Falla` o `Bloqueada` (no se puede ejecutar todavía).
- Resultado real: qué ocurrió, en una frase. Si falla, añade pasos exactos, posición aproximada (X, Z) y captura o vídeo.
- Anota versión de Unity, commit y fecha en la cabecera de cada sesión.

| Sesión | Fecha | Tester | Commit | Unity | Escena usada |
|---|---|---|---|---|---|
| 1 | | | | 6000.3.22f1 | |

## Referencia del nivel
- 1 unidad = 1 m. El recorrido avanza hacia +Z.
- Orilla transitable: X -1 a 6, Z -4 a 40, suelo en Y = 0. Cauce seco: X -4 a -1, fondo en Y = -0.6.
- PlayerSpawn (3, 0, 0). SafeRespawn (3, 0, 2). ExitMarker (3, 0, 36). Recorrido: 36 m.
- Obstáculos en el cauce: Obstacle_01 tronco (Z 10), Obstacle_02 rocas (Z 18), Obstacle_03 ramas (Z 26).
- Borde de caída: lado este, entre Z 12 y Z 28 (sin barandilla). Resto del borde este: barandilla de 0.5 m.
- Rampas al cauce en Z 5 y Z 31.5. Tronco opcional de 0.35 m en Z 30, rodeable por el este.
- Portal de salida: hueco de 4.2 m × 2.5 m en Z 38.5.

## A. Escena de nivel (sin gameplay)

| ID | Precondición | Pasos | Resultado esperado | Resultado real | Estado |
|---|---|---|---|---|---|
| A-01 Generación | Unity cerrado por otros procesos sobre Game/ | Ejecutar el menú de generación (o el método batch) | Se guarda la escena sin errores; la validación imprime `CORRECTA` | Batch 11-nov (Claude): salida 0, 140 creados, validación CORRECTA. Falta repetir desde el menú | OK (batch) |
| A-02 Regeneración | A-01 hecha | Volver a ejecutar la generación | Sin objetos duplicados: cada nombre del contrato aparece una vez; consola indica 0 objetos creados | Batch 11-nov (Claude): 0 creados, 142 actualizados, 141 objetos, validación CORRECTA | OK (batch) |
| A-03 Contenido ajeno | A-01 hecha | Añadir un objeto vacío `PruebaAjena` bajo `ArroyoGreybox/Geometry` y un componente cualquiera a `Obstacle_01`; regenerar | `PruebaAjena` y el componente siguen existiendo | Batch 12-nov (Claude), sobre copia temporal `Tests/Acceptance/Tmp_A03/` ya eliminada. Se conservan: objeto bajo Geometry, AudioSource e hijo en Obstacle_01, raíz ajena, objeto dentro de DegradedVisuals. SafePoint_02 movido a otro padre sigue único y conserva su posición. 144 → 144 objetos; validación CORRECTA. Un ajuste manual de posición de Obstacle_01 se sobrescribe, como está documentado | OK (batch) |
| A-04 Marcadores | Escena abierta | Buscar en la jerarquía PlayerSpawn, SafeRespawn, ExitMarker, Obstacle_01..03, DegradedVisuals, RestoredVisuals | Cada uno existe una vez bajo `ArroyoGreybox` | | Pendiente |
| A-05 Estado visual inicial | Escena abierta | Mirar el check de activo de los grupos | DegradedVisuals activo; RestoredVisuals inactivo; ambos hermanos, no anidados | | Pendiente |
| A-06 Escala | Escena abierta | Comparar `EditorHelpers/ScaleReference_1_8m` con portal, barandilla y obstáculos | La cápsula de 1.8 m cabe bajo el portal (2.5 m); la barandilla llega a la rodilla | | Pendiente |
| A-07 Colliders | Escena abierta, Gizmos activos | Seleccionar Geometry, Obstacles y Exit | Suelo, cauce, muros, rampas, barandillas, troncos de árbol, obstáculos y pilares tienen collider; los grupos visuales no | | Pendiente |
| A-08 Materiales URP | Escena abierta | Ver la escena en Scene view | Nada en magenta; todos los materiales usan `Universal Render Pipeline/Lit` | | Pendiente |
| A-09 Vista restaurada | Escena abierta, sin guardar al final | Desactivar DegradedVisuals y activar RestoredVisuals | Aparece agua en el cauce, copas verdes, flores y arbustos más grandes; desaparecen zarzas del portal. El cambio se lee por forma, no solo por color. **Revertir sin guardar** | | Pendiente |
| A-10 Cámara de vista previa | Escena abierta | Revisar `PreviewCamera` | Existe y está desactivada; no hay ninguna otra cámara | | Pendiente |

## M. Movimiento y cámara T06 (manual, C-02)
Escena: `Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity` en Play Mode. **No guardar la escena al salir.**
Controles actuales (Codex): WASD para moverse, ratón para la cámara y Espacio para saltar. Escape libera el cursor y el clic izquierdo lo recaptura. Escape todavía no pausa.
Para ver la posición del jugador, selecciona `Gameplay/Player` en la jerarquía durante Play.

| ID | Precondición | Pasos | Resultado esperado | Resultado real | Estado |
|---|---|---|---|---|---|
| M-01 W relativo a cámara | Play, cursor capturado | Girar la cámara 90° a la derecha con el ratón y mantener W 2 s. Repetir a 180° y a 270° | El personaje avanza siempre hacia donde mira la cámara, no hacia +Z del mundo, y gira el cuerpo hacia la dirección de avance | | Pendiente |
| M-02 A/S/D relativos | M-01 | Con la cámara fija, pulsar A, S y D por separado durante 1 s cada una | Izquierda, atrás y derecha respecto a la cámara. S no invierte la cámara ni hace girar al personaje de forma brusca | | Pendiente |
| M-03 Diagonales | Zona libre, por ejemplo en (1, 0, 2) | Mantener W+D 1 s y luego W+A 1 s | Movimiento a 45° de la cámara. La diagonal no es más rápida que W sola: recorre una distancia similar en el mismo tiempo | | Pendiente |
| M-04 Cambio de cámara en marcha | Manteniendo W | Girar el ratón lentamente 360° sin soltar W | La trayectoria se curva siguiendo la cámara sin tirones ni paradas | | Pendiente |
| M-05 Salto simple | En suelo | Pulsar Espacio una vez sin moverse y luego una vez corriendo | Salto de aproximadamente 1 m. Aterriza de pie y puede volver a moverse de inmediato | | Pendiente |
| M-06 Doble pulsación en el aire | En suelo | Pulsar Espacio dos veces muy rápido y también mantenerlo pulsado | Un solo salto. No hay salto doble y mantener la tecla no repite el salto | | Pendiente |
| M-07 Saltar el tronco bajo | Frente a LowLog_Optional (Z 30, X 0–3.5) | Saltarlo y, por separado, caminar hacia él sin saltar | Se puede saltar. Anotar si al caminar lo sube sin saltar (la medición automatizada dice que sí) | | Pendiente |
| M-08 Colisión con muros | Cauce, junto al muro oeste; orilla, junto al muro sur | Empujar contra cada muro 3 s, también en diagonal | No atraviesa, no tiembla y se desliza a lo largo del muro en diagonal | | Pendiente |
| M-09 Colisión con barandilla | Borde este, Z 0–12 | Caminar contra la barandilla | Se detiene. Anotar si es fácil saltarla por accidente | | Pendiente |
| M-10 Colisión con obstáculos | Orilla junto a cada obstáculo (Z 10, 18, 26) | Caminar hacia el cauce de frente al obstáculo | Anotar si el personaje **se sube encima** de Obstacle_01 y Obstacle_03 (la medición automatizada dice que sí) y si eso confunde | | Pendiente |
| M-11 Rampas | Rampas en Z 5 y Z 31.5 | Bajar al cauce y volver a subir caminando | Baja y sube sin saltar ni quedarse trabado en el borde superior | | Pendiente |
| M-12 Salir del cauce sin rampa | Cauce en Z 14, entre Obstacle_01 y Obstacle_02 | Intentar volver a la orilla caminando y luego saltando | Caminando no sube (pared de 0.6 m). Saltando sube. Anotar si un jugador lo descubre sin ayuda | | Pendiente |
| M-13 Cámara junto a paredes | Pegado al muro sur (Z ≈ -3.6) y al muro norte (Z ≈ 39.6) | Girar la cámara 360° a la altura normal, mirando hacia arriba y mirando hacia abajo | La cámara no atraviesa el muro ni muestra el exterior. Anotar si se mete **dentro del personaje** o si la vista se vuelve confusa | | Pendiente |
| M-14 Cámara en el cauce | Cauce junto al muro oeste (X ≈ -3.4) | Girar 360° | La cámara no entra en la orilla, el suelo ni el muro | | Pendiente |
| M-15 Cámara bajo el portal | Bajo el dintel (Z 38.5) | Girar 360° y mirar hacia arriba | La cámara no atraviesa el dintel ni los pilares | | Pendiente |
| M-16 Caída por el borde abierto | Borde este entre Z 12 y Z 28 | Caminar fuera del borde hacia +X | Cae y en menos de 1 s reaparece en SafeRespawn (3, 0, 2) mirando hacia +Z. La cámara se recoloca sin barrido largo. Anotar si el retroceso de unos 18 m molesta | | Pendiente |
| M-17 Caída repetida | M-16 | Caer 5 veces seguidas en puntos distintos del borde | Siempre retorna, sin acumular velocidad ni quedar bajo el suelo | | Pendiente |
| M-18 Caída saltando | En el borde abierto y junto a la barandilla | Saltar hacia fuera | Mismo retorno seguro que M-16 | | Pendiente |
| M-19 Liberar cursor | Play, cursor capturado | Pulsar Escape | El cursor aparece y se libera. La cámara deja de girar con el ratón y WASD deja de mover al personaje | | Pendiente |
| M-20 Recapturar cursor | M-19 | Clic izquierdo dentro de la vista Game | El cursor se oculta y se captura. Cámara y movimiento vuelven a responder. El clic no provoca salto ni otra acción | | Pendiente |
| M-21 Pérdida de foco | Play, cursor capturado | Alt+Tab a otra ventana y volver. Hacer clic en la vista Game | Al perder el foco se libera el cursor y el personaje no sigue andando solo. Con el clic se recupera | | Pendiente |
| M-22 Recorrido completo | Play desde el inicio | Ir de PlayerSpawn a ExitMarker sin saltar, por la orilla | Llega en unos 8–10 s sin atascos y cruza el portal hasta el muro norte | | Pendiente |

## R. Comprobaciones automatizadas (C-02, Claude)
Ejecutadas el 12 de noviembre de 2026 en Unity 6000.3.22f1, modo batch, sobre `ArroyoPlayable.unity` sin guardarla. El hash de la escena era el mismo antes y después.
- **Herramienta:** `Game/Assets/_Bosque/Tests/Acceptance/Editor/ArroyoTraversalProbe.cs`, método `Bosque.Acceptance.ArroyoTraversalProbe.RunBatch`.
- **Cómo funciona:** controla el `ThirdPersonMotor` real mediante `Simulate` con dt 0.02 y la cámara real mediante `ThirdPersonCamera.Step`.
- **Qué no cubre:** no prueba Input System, ratón, cursor, Play Mode ni rendimiento.

Resultado: 22 OK, 0 FALLA y 9 informativas.

| ID | Resultado medido | Estado |
|---|---|---|
| R-01 | Recorrido de Spawn a Exit rodeando el tronco: llega en 8.3 s y nunca baja de Y 0 | OK |
| R-02 | Recorrido en línea recta por X = 3: **también llega**. LowLog_Optional (0.35 m, collider de cápsula redondeado) se sube caminando aunque el escalón máximo es 0.3 | Info |
| R-03 | Saltar el tronco: altura máxima de 1.06 m | OK |
| R-04 | Rampas Z 5 y Z 31.5: baja hasta Y -0.57 y sube caminando | OK |
| R-05 | Cauce en Z 14: caminando no sale (pared de 0.6 m frente a escalón de 0.3 m) | Info |
| R-06 | Cauce en Z 14: con un salto sale en 0.22 s | OK |
| R-07 | Caminando desde la orilla, el jugador **se sube encima** de Obstacle_01 (techo +0.20 m) y Obstacle_03 (+0.30 m) y puede cruzar sobre ellos hasta el muro oeste. Obstacle_02 (+0.40 m) lo detiene | Info |
| R-08 | Desde el borde de la orilla (X -0.62) a los obstáculos 1, 2 y 3: del pecho a la superficie 0.85 / 0.72 / 0.79 m; en horizontal al centro 1.88 m en los tres | OK |
| R-09 | Caída por el borde abierto en Z 20: vuelve a SafeRespawn 0.54 s después de dejar el borde, con un retroceso de unos 18 m | OK |
| R-10 | 5 caídas seguidas: las 5 retornan y quedan apoyadas | OK |
| R-11 | La barandilla este bloquea al caminar | OK |
| R-12 | La barandilla se supera con un solo salto, lo que lleva a la caída y al retorno | Info |
| R-13 | El portal se cruza hasta Z 39.6 | OK |
| R-14 | Muro oeste: no se atraviesa | OK |
| R-15 | Muro sur: no se atraviesa | OK |
| R-16 | Diagonal: 2.25 m en 0.5 s, igual que el valor nominal, sin bonificación diagonal | OK |
| C-01 | Cámara en 7 puntos con 72 orientaciones cada uno: nunca queda dentro de geometría ni con la visión tapada. Junto a los muros sur y norte y en el cauce junto al muro oeste se acerca a **0.26–0.37 m** del foco en 14 de 72 orientaciones, es decir, dentro del radio del cuerpo (0.35 m) | OK, con observación |

## B. Recorrido jugable (requiere T06, T08, T09 integrados)
B-01 a B-08 ya solo dependen de T06 y equivalen a las pruebas M. Se dejan para Diego con estado Pendiente.

| ID | Precondición | Pasos | Resultado esperado | Resultado real | Estado |
|---|---|---|---|---|---|
| B-01 Aparición | Escena de integración en Play | Iniciar | El personaje aparece sobre PlayerSpawn mirando hacia +Z, apoyado en el suelo, sin caer ni atravesarlo | | Pendiente (T06 listo) |
| B-02 Recorrido | B-01 | Caminar de PlayerSpawn hasta ExitMarker por la orilla | Se completa sin saltos obligatorios ni atascos; el paso libre más estrecho mide ≥ 2 m | | Pendiente (T06 listo) |
| B-03 Cauce y rampas | B-01 | Bajar al cauce por Ramp_South y volver a subir por la misma rampa; repetir por Ramp_North | Se baja y se sube caminando, sin saltar | | Pendiente (T06 listo) |
| B-04 Caída | B-01 | Saltar o caminar fuera del borde este entre Z 12 y 28 | El personaje cae y vuelve a SafeRespawn (o al punto seguro que defina T09) en menos de 3 s, sin quedar en el vacío ni perder el control | | Pendiente (T06 listo) |
| B-05 Caída repetida | B-04 | Repetir la caída 5 veces seguidas | Siempre retorna; no se acumula velocidad ni se duplica el personaje | | Pendiente (T06 listo) |
| B-06 Cámara en muro | B-01 | Pegarse al muro oeste y girar la cámara 360°; repetir junto al portal y bajo el dintel | La cámara no atraviesa muros ni deja ver el exterior; el personaje sigue visible | | Pendiente (T06 listo) |
| B-07 Cámara en cauce | B-03 | Dentro del cauce, mirar hacia la orilla y los obstáculos | La cámara no queda dentro de la orilla o del suelo | | Pendiente (T06 listo) |
| B-08 Obstáculos visibles | B-01 | Desde PlayerSpawn, sin moverse, girar la cámara | Al menos Obstacle_01 se distingue; los otros dos se ven al avanzar por la orilla | | Pendiente (T06 listo) |
| B-09 Interacción fuera de alcance | T08 conectado | Situarse a más del alcance definido por Codex de Obstacle_01 y pulsar E | No ocurre nada ni aparece indicación de interacción; no hay error en consola | | Bloqueada |
| B-10 Interacción en alcance | T08 conectado | Acercarse a Obstacle_01 desde la orilla (X ≈ -0.6) y pulsar E | Aparece la indicación antes de pulsar; al pulsar, el obstáculo se retira | | Bloqueada |
| B-11 Pulsaciones repetidas | B-10 | Pulsar E muy rápido 10 veces sobre un obstáculo, y luego sobre el lugar donde estaba | Se retira una sola vez; el progreso cuenta 1, no 10; sin errores | | Bloqueada |
| B-12 Orden de obstáculos | T08 conectado | Retirar los obstáculos en orden 3, 1, 2 | El progreso se completa igual (o, si el diseño exige orden, se muestra pista y se puede reintentar) | | Bloqueada |
| B-13 Salida bloqueada | T09 conectado, 0–2 obstáculos retirados | Ir a ExitMarker e intentar cruzar el portal | No se completa la zona; se informa de lo pendiente | | Bloqueada |
| B-14 Restauración | T09 conectado | Retirar los tres obstáculos | DegradedVisuals se desactiva y RestoredVisuals se activa una sola vez; la salida se habilita | | Bloqueada |
| B-15 Salida tras restaurar | B-14 | Cruzar ExitMarker | Se completa la zona exactamente una vez | | Bloqueada |
| B-16 Reinicio | B-14 o a mitad de progreso | Usar el comando de reinicio (definido por Codex) | Personaje en PlayerSpawn; obstáculos de vuelta; DegradedVisuals activo y RestoredVisuals inactivo; progreso a 0; salida bloqueada de nuevo | | Bloqueada |
| B-17 Reinicio repetido | B-16 | Reiniciar 3 veces seguidas y completar la zona | Sin duplicados ni objetos fantasma; la zona se completa normalmente | | Bloqueada |
| B-18 Rendimiento | Build o editor | Recorrer el nivel con estadísticas visibles | Registrar FPS mínimos y equipo de prueba (objetivo GDD: 30 FPS a 1080p) | | Bloqueada |

## Incidencias encontradas
| ID | Prueba | Descripción y pasos | Gravedad | Responsable | Estado |
|---|---|---|---|---|---|
| I-01 | R-07 / M-10 | **Pasos:** en ArroyoPlayable, situarse en (2, 0, 10) y caminar hacia -X. El jugador sube sobre Obstacle_01 (techo a +0.20 m sobre la orilla) y puede cruzar el cauce por encima hasta X -3.4. Ocurre lo mismo en Z 26 con Obstacle_03 (+0.30 m). **Riesgo:** cuando T08 retire el obstáculo con el jugador encima, este caerá a un tramo del cauce sin rampa | Media | Claude (geometría) | Abierta. Propuesta de ajuste en el relevo |
| I-02 | R-05 / M-12 | **Pasos:** bajar o caer al cauce entre Z 10.4 y 17.3 o entre Z 18.7 y 25.4. Solo se sale saltando, porque la pared mide 0.6 m y el escalón máximo 0.3 m. Contradice "sin saltos obligatorios" del contrato, aunque aquí es una recuperación y no parte del recorrido | Baja | Claude (geometría) | Abierta. Propuesta de ajuste en el relevo |
| I-03 | R-02 / M-07 | **Pasos:** caminar por X = 3 hacia +Z hasta Z 30. LowLog_Optional (0.35 m) se sube sin saltar porque su collider de cápsula es redondeado, así que no funciona como obstáculo de salto | Baja | Claude (geometría) | Abierta. Solo afecta si se quiere enseñar el salto ahí |
| I-04 | R-09 / M-16 | **Pasos:** caer por el borde en Z 20 o Z 27. Se vuelve a SafeRespawn (Z 2), con un retroceso de hasta unos 26 m. Los SafePoint_01..03 existen pero no tienen lógica | Baja | Codex (T09, retorno por tramo) | Abierta. Propuesta |
| I-05 | C-01 / M-13 | **Pasos:** pegarse al muro sur en (3, 0, -3.5) o al muro norte en (3, 0, 39.5) y girar la cámara hacia el muro. La cámara queda a 0.26 m del foco, dentro del radio del cuerpo (0.35 m). Sin verificar visualmente: puede verse el interior de la cápsula o nada del personaje | Baja; a confirmar en M-13 | Codex (cámara) | Abierta, pendiente de prueba manual |
