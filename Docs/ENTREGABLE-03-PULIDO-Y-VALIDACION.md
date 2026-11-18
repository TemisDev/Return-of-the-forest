# Entregable 03 — Arte V3, interfaz, pulido y validación

Fecha de coordinación: **18 de noviembre de 2026**, America/Lima. Entrega general del videojuego: 27 de noviembre.
Rama: `feature/arroyo-visual-integration`. Amplía la integración visual del 16 de noviembre, no reconstruye el proyecto.

## Auditoría y autorización
La copia estaba limpia y develop seguía en df6830a. La rama de trabajo ya contenía develop y los commits visuales anteriores. Arte V3 bbca565 y QA V3 9535f78 **no estaban fusionados** en develop.
Se solicitó la autorización exigida por el encargo y el usuario respondió: «Sí, integrar ambas en la rama de trabajo». Se fusionaron en dos commits independientes, 99c27e6 y aa566b5, sin conflictos. No se modifica develop ni main ni se fusiona automáticamente el PR.
Línea base reproducida antes de integrar: **24/24 pruebas aprobadas**. Las nueve aprobaciones manuales informadas por el usuario son históricas; no se contabilizan como pruebas de esta nueva versión.

## Inventario y decisiones de integración
| Recurso de Arte V3 | Estado encontrado | Integración |
|---|---|---|
| Driftwood_V3 | OBJ, 16 vértices / 12 caras, sin UV | Prefab derivado mate; visual de obstáculos 1 y 3, ajustado dentro del collider original |
| RockCluster_V3 | OBJ, 18 vértices / 24 caras, sin UV | Prefab derivado para obstáculo 2 y decoración de la ribera oeste |
| FernCluster_V3 | OBJ, 88 vértices / 94 caras, sin UV | Prefab decorativo en el grupo restaurado, fuera de la orilla de interacción |
| riverbank_dry/restored | PNG 256×256, sin material | Materiales derivados sobre superficies con UV y mosaico proporcional a su tamaño |
| stream_moss | PNG 256×256 | Fondo de ribera restaurada, detrás de las rocas; sin collider |
| water_ripple_mask | PNG con color/ondas, pese al nombre de máscara | Base Map de agua mate con mosaico; sin shader complejo ni simulación nueva |
| Cuatro iconos RGBA | PNG, sin configuración de HUD | E, progreso, restauración y salida en el HUD existente; ScaleToFit conserva proporciones |
| restoration_particles | Textura RGBA, no un efecto terminado | Material URP Particles/Unlit y efecto breve de 20 partículas, máximo 24 |

Los tres modelos no tienen UV: **no se les asignan texturas de ribera estiradas**. Usan colores mates compartidos. Los originales de Arte, sus importadores/GUIDs y los informes/matrices de Diego se conservan; los materiales y prefabs nuevos viven en `Scenes/Integration/ArtV3/`.
No se añaden colliders decorativos. Los modelos previos sustituidos se mantienen con renderer deshabilitado, preservando raíces y referencias.

## Interfaz y efecto
Se mejora `ArroyoPrototypeHud`, con panel oscuro, texto marfil, acentos vegetales e iconos. Se conserva la lectura de `ArroyoSession.Progress`, la tecla R y el botón de reinicio. Los mensajes distinguen objetivo, agua recuperada/salida abierta y finalización. No hay otro HUD de progreso independiente ni un sistema de pausa añadido.
`RestorationFeedback` es una presentación que escucha `ArroyoSession.StateChanged`: reproduce el efecto una vez al pasar a restaurado, no al completar la salida, limpia partículas al reiniciar y retira sus suscripciones al deshabilitarse. No modifica progreso ni decide cuándo restaurar.

## Protección de gameplay
- T08/T09, RestorationProgress, RemovableObstacle, PlayerInteractor, ThirdPersonMotor y ThirdPersonCamera permanecen sin cambios.
- **26 bloques originales de física en la escena comparados: 0 modificados**. La única actualización serializada de un script anterior son las cuatro referencias del HUD.
- Se conservan los IDs arroyo.log, arroyo.rocks y arroyo.branches, obstáculos, barrera, punto de retorno y volumen de salida.
- La escena base ArroyoGreybox conserva SHA256 `617AD893A90A3784D633DDDCFDD1163545F9B2EF6B4FD0964A0A0D14A0F3BBD2`.
- No se modifican Packages ni ProjectSettings deliberadamente. Los cambios son de presentación, integración y herramientas de verificación.

## Verificación automatizada ejecutada
| Operación | Resultado |
|---|---|
| Línea base | 24/24, salida 0 |
| RED del efecto | 2 fallos esperados antes de implementar |
| Suite de integración | **27/27 aprobadas**, 0 fallidas y 0 omitidas; salida 0 |
| Casos conservados | Movimiento/cámara, alcance, pared, IDs inválidos, repetición, restauración, salida y reinicios, incluido Play Mode |
| Nuevos casos | Una presentación por ciclo, limpieza/reinicio, suscripciones sin duplicación, iconos y efecto correctamente configurados |
| Sensibilidad de las pruebas | Invertir temporalmente la condición del efecto produjo el fallo esperado; se restauró y la suite final volvió a aprobar 27/27 |
| Preservación | Física original intacta y escena base sin cambios |
| Render | Capturas reales antes/después mediante la cámara Unity y las APIs de interacción existentes |
| Build Windows x64 Development | **Succeeded, 0 errores; salida 0** |
| Recorrido automatizado de la build | Aprobado a 1920×1080 y 1280×800, ambas con render visible y cierre normal (exit 0) |
| Ciclos en la build | Movimiento/salto, retirada de tres obstáculos, efecto único, finalización y tres ciclos completos con reinicio, en cada resolución |

El código de verificación `ArroyoBuildProbe` solo está disponible en Editor/Development y solo se activa pasando `-arroyo-verify`. No aparece en una partida normal ni añade estado de gameplay. Verifica por APIs reales movimiento/salto, salida inicial rechazada, retirada, restauración, finalización y tres ciclos de completar/reiniciar; captura HUD y registra muestras cortas de FPS/draw calls si el contador está disponible. Termina con Application.Quit, no con una detención forzada.

Una ejecución de arranque de build se rechazó porque el proceso de captura de este mismo proyecto todavía estaba cerrándose. Se esperó su terminación y se relanzó la build; no se forzó el cierre de editores ajenos ni se alteró otra carpeta Unity.

La ejecución con ventana oculta no pudo validar render: Direct3D 12 rechazó la captura y una prueba de Direct3D 11 produjo imágenes negras/contador de draw calls no disponible. **Se descartaron esas capturas y esas muestras de FPS.** Al repetir con la ventana visible y la API gráfica predeterminada (Direct3D 12), ambas verificaciones finalizaron con imágenes reales, contadores disponibles y sin errores capturados por el verificador. Los JSON versionados corresponden exclusivamente a esas ejecuciones visibles válidas.

## Diagnóstico de rendimiento medido
Equipo detectado por la build: Intel Core i5-10300H, NVIDIA GeForce GTX 1650, 16.221 MB de RAM reportados por Unity, Windows 11. Muestras estacionarias de aproximadamente **3 segundos por estado**, build Development sin límite de FPS; no son los recorridos de 60 s de QA.

| Resolución | Estado | FPS medios | Peor frame (ms) | Draw calls medios |
|---|---|---:|---:|---:|
| 1920×1080, ventana | Degradado | 164,55 | 37,37 | 281,64 |
| 1920×1080, ventana | Restaurado | 180,48 | 12,35 | 283,07 |
| 1280×800, ventana | Degradado | 439,41 | 4,48 | 281,70 |
| 1280×800, ventana | Restaurado | 370,62 | 21,33 | 282,75 |

Estas medias no demuestran 30 FPS estables durante todo un recorrido: la muestra incluye un frame de 37,37 ms y no cubre todas las posiciones/condiciones. Otros procesos del equipo pueden influir. Medir todavía FPS mínimos/media, picos y resolución completa durante los 60 s prescritos. Los draw calls se obtienen con ProfilerRecorder y el contador [Draw Calls Count de Unity](https://docs.unity3d.com/6000.0/Documentation/Manual/profiler-counters-reference.html); incluyen trabajo adicional a la cantidad de meshes visibles.
Geometría comprobada por las pruebas: 84 renderers / 2.924 triángulos degradado, 94 / 4.208 restaurado. No se añaden luces ni colisiones de decoración; la vegetación pequeña no proyecta sombras. El efecto tiene máximo 24 partículas y no permanece activo tras reiniciar.

## Evidencia y reproducción
Evidencias versionadas en `Docs/Evidence/ArroyoV3Polish/`: capturas anteriores, capturas finales, resumen de pruebas y cobertura de las matrices. Los registros completos permanecen localmente en `.dev-logs/polish-v3-*`, excluidos de Git.

| Evidencia | Enlace |
|---|---|
| Escenario degradado, antes / después | [Antes](Evidence/ArroyoV3Polish/before-degraded.png) · [Después](Evidence/ArroyoV3Polish/after-degraded.png) |
| Escenario restaurado, antes / después | [Antes](Evidence/ArroyoV3Polish/before-restored.png) · [Después](Evidence/ArroyoV3Polish/after-restored.png) |
| HUD E, 1920×1080 / 1280×800 | [16:9](Evidence/ArroyoV3Polish/hud-interaction-1080.png) · [16:10](Evidence/ArroyoV3Polish/hud-interaction-1610.png) |
| Efecto / finalización | [Restauración](Evidence/ArroyoV3Polish/hud-effect-1080.png) · [Victoria](Evidence/ArroyoV3Polish/hud-completed-1080.png) |
| Build, resultados y hardware | [1080p](Evidence/ArroyoV3Polish/build-1080.json) · [16:10](Evidence/ArroyoV3Polish/build-1610.json) |

Las imágenes de HUD son capturas originales del ejecutable, revisadas visualmente; no son composiciones ni renders ilustrativos. Las comparaciones del escenario usan una solicitud de render de cámara fuera de Play Mode y por eso no incluyen HUD.
Build local: `Builds/ArroyoV3Polish/BosqueDeRegreso.exe`, con todos los datos y DLL de su carpeta. No se versionan ejecutables ni se publica una release.

Herramientas de Unity, siempre una operación sobre Game/ a la vez:
- `Bosque.CoreSetup.ArroyoArtV3Polish.IntegrateBatch`: genera derivados, conecta HUD/efecto y añade visuales de forma incremental; respeta el montaje existente ArtV3Polish para no duplicar objetos.
- `Bosque.CoreSetup.ArroyoVisualPreview.AfterBatch`: capturas del escenario, sin HUD y sin guardar una partida en el asset.
- `Bosque.CoreSetup.ArroyoBuild.BuildPolishedWindowsBatch`: build Windows Development separada en `Builds/ArroyoV3Polish/`.
- Runner de pruebas habitual: `-runTests -testPlatform EditMode -testResults <xml>` (contiene una prueba que entra/sale de Play Mode).
- Ejecutable Development: `-arroyo-verify -arroyo-verify-output <directorio> -screen-width 1920 -screen-height 1080 -screen-fullscreen 0`. Requiere dispositivo gráfico para capturar HUD. Sin esa opción se juega normalmente.

## QA V3: interpretación de resultados
Se usan los IDs de REGRESION-FUNCIONAL, AUDITORIA-VISUAL y PRUEBAS-RENDIMIENTO, **sin reescribir los CSV originales**. La cobertura automática se registra aparte. Ejecutar un caso por APIs no prueba automáticamente una pulsación física de E/R ni la sensación de cámara.

[Cobertura por ID y modo de ejecución](Evidence/ArroyoV3Polish/cobertura-QA-V3.csv): 12 casos cubiertos por pruebas automáticas o inspección de capturas dentro del alcance indicado, 19 NO EJECUTADO completos. Esto no equivale a 12 pruebas manuales de Diego ni a una aceptación total. [Detalle de las 27 pruebas Unity](Evidence/ArroyoV3Polish/pruebas.json).

Pendiente para Diego/usuario:
1. Repetir los nueve criterios manuales aprobados en la versión anterior, usando esta build.
2. Recorrer ambos estados y probar cámara junto a los helechos, rocas, muros y cuerpo del personaje. Confirmar que la decoración no oculta obstáculos o señales.
3. Validar E/R, foco, cursor y legibilidad del HUD en resoluciones habituales, incluida pantalla completa.
4. Medir **60 s de recorrido** por estado, FPS mínimos/media y picos de restauración en la PC de prueba; anotar CPU/RAM/GPU/resolución y otros procesos activos.
5. Revisar I-01..I-05 con los pasos de Diego; no marcar resueltas las incidencias solo por la aprobación histórica.

Las muestras cortas automáticas son un diagnóstico estacionario de una build Development, no el benchmark de 60 s ni una garantía de rendimiento en equipos de gama media. No se presenta la matriz original de QA, entregada como NO EJECUTADO, como una aceptación ya realizada.

## Incidencias y recursos faltantes
- I-01: se conserva la protección de no retirar el soporte bajo el personaje; regresión automatizada aprobada. Revalidación manual pendiente.
- I-02: la salida del cauce entre rampas sigue requiriendo salto; no se rediseñó geometría física.
- I-03: el tronco opcional anterior sigue subible caminando; no se usa como tutorial obligatorio.
- I-04: se conserva SafeRespawn inicial, sin checkpoints por tramo.
- I-05: cámara cerca del cuerpo junto a muros sigue requiriendo observación manual. No se declara corregida.
- Protagonista definitiva/animaciones, fauna, audio final y modelado de terreno detallado no están incluidos en Arte V3. La cápsula y límites físicos del prototipo siguen visibles en algunas vistas.
- El agua usa una textura estática; no es una simulación de flujo. El paisaje lejano es decorativo y no amplía el terreno accesible.

## Entrega
La misma rama y el PR hacia develop conservan la integración anterior y añaden Arte/QA V3 autorizados. No se modifica main ni se fusiona automáticamente el PR. Enlace y estado final de la build en el relevo de Codex.
