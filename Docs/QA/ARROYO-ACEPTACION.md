# Pruebas de aceptación: Arroyo (C-01 / T07 → T10)
Preparado por Claude Code el 11 de noviembre de 2026. Ejecuta: Diego. Integra: Codex.
Escena de nivel: `Game/Assets/_Bosque/Levels/Arroyo/ArroyoGreybox.unity`, generada con `Tools > Bosque > Niveles > Arroyo - Generar o actualizar greybox`.

Las pruebas A-xx se pueden ejecutar ya sobre la escena de nivel. Las pruebas B-xx requieren el controlador y la cámara (T06), la interacción (T08) y la restauración (T09) conectados en la escena de integración de Codex. Hasta entonces quedan **pendientes**: no marcar como superadas sin ejecutarlas.

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
| A-03 Contenido ajeno | A-01 hecha | Añadir un objeto vacío `PruebaAjena` bajo `ArroyoGreybox/Geometry` y un componente cualquiera a `Obstacle_01`; regenerar | `PruebaAjena` y el componente siguen existiendo | | Pendiente |
| A-04 Marcadores | Escena abierta | Buscar en la jerarquía PlayerSpawn, SafeRespawn, ExitMarker, Obstacle_01..03, DegradedVisuals, RestoredVisuals | Cada uno existe una vez bajo `ArroyoGreybox` | | Pendiente |
| A-05 Estado visual inicial | Escena abierta | Mirar el check de activo de los grupos | DegradedVisuals activo; RestoredVisuals inactivo; ambos hermanos, no anidados | | Pendiente |
| A-06 Escala | Escena abierta | Comparar `EditorHelpers/ScaleReference_1_8m` con portal, barandilla y obstáculos | La cápsula de 1.8 m cabe bajo el portal (2.5 m); la barandilla llega a la rodilla | | Pendiente |
| A-07 Colliders | Escena abierta, Gizmos activos | Seleccionar Geometry, Obstacles y Exit | Suelo, cauce, muros, rampas, barandillas, troncos de árbol, obstáculos y pilares tienen collider; los grupos visuales no | | Pendiente |
| A-08 Materiales URP | Escena abierta | Ver la escena en Scene view | Nada en magenta; todos los materiales usan `Universal Render Pipeline/Lit` | | Pendiente |
| A-09 Vista restaurada | Escena abierta, sin guardar al final | Desactivar DegradedVisuals y activar RestoredVisuals | Aparece agua en el cauce, copas verdes, flores y arbustos más grandes; desaparecen zarzas del portal. El cambio se lee por forma, no solo por color. **Revertir sin guardar** | | Pendiente |
| A-10 Cámara de vista previa | Escena abierta | Revisar `PreviewCamera` | Existe y está desactivada; no hay ninguna otra cámara | | Pendiente |

## B. Recorrido jugable (requiere T06, T08, T09 integrados)

| ID | Precondición | Pasos | Resultado esperado | Resultado real | Estado |
|---|---|---|---|---|---|
| B-01 Aparición | Escena de integración en Play | Iniciar | El personaje aparece sobre PlayerSpawn mirando hacia +Z, apoyado en el suelo, sin caer ni atravesarlo | | Bloqueada |
| B-02 Recorrido | B-01 | Caminar de PlayerSpawn hasta ExitMarker por la orilla | Se completa sin saltos obligatorios ni atascos; el paso libre más estrecho mide ≥ 2 m | | Bloqueada |
| B-03 Cauce y rampas | B-01 | Bajar al cauce por Ramp_South y volver a subir por la misma rampa; repetir por Ramp_North | Se baja y se sube caminando, sin saltar | | Bloqueada |
| B-04 Caída | B-01 | Saltar o caminar fuera del borde este entre Z 12 y 28 | El personaje cae y vuelve a SafeRespawn (o al punto seguro que defina T09) en menos de 3 s, sin quedar en el vacío ni perder el control | | Bloqueada |
| B-05 Caída repetida | B-04 | Repetir la caída 5 veces seguidas | Siempre retorna; no se acumula velocidad ni se duplica el personaje | | Bloqueada |
| B-06 Cámara en muro | B-01 | Pegarse al muro oeste y girar la cámara 360°; repetir junto al portal y bajo el dintel | La cámara no atraviesa muros ni deja ver el exterior; el personaje sigue visible | | Bloqueada |
| B-07 Cámara en cauce | B-03 | Dentro del cauce, mirar hacia la orilla y los obstáculos | La cámara no queda dentro de la orilla o del suelo | | Bloqueada |
| B-08 Obstáculos visibles | B-01 | Desde PlayerSpawn, sin moverse, girar la cámara | Al menos Obstacle_01 se distingue; los otros dos se ven al avanzar por la orilla | | Bloqueada |
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
| | | | | | |
