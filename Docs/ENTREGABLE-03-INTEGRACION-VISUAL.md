# Entregable 03 — integración artística del arroyo

Fecha: 16 de noviembre de 2026. Base: develop df6830a. Rama: feature/arroyo-visual-integration.

## Resultado y alcance
ArroyoPlayable utiliza los prefabs/materiales de Arte V2 para troncos, rocas, árboles, arbustos, hierba y juncos. Se sustituyen visuales provisionales sin cambiar el estado lógico del nivel. Se mantiene la cápsula del personaje: no hay un modelo de protagonista entregado en este kit.

- Obstáculos: reemplazados sus renderers provisionales por instancias de prefabs bajo ArtV3VisualMount, ajustados al volumen del collider existente. Se conservan raíces, IDs y componentes.
- Barrera: representación de madera, en lugar del bloque opaco provisional. El mismo collider continúa bloqueando el paso hasta completar 3/3.
- Orilla: superficies visuales con materiales degradado/restaurado; el collider de Bank se conserva.
- Ribera: juncos, arbustos y hierba cambian de densidad y tamaño según los grupos de estado existentes.
- Árboles: copas más pequeñas y apagadas en degradado; más grandes y verdes en restaurado. Sin nuevas colisiones.
- Paisaje: rocas facetadas sobre el límite oeste existente y dos masas lejanas fuera del recorrido al este. No aportan suelo transitable ni alteran el borde de caída.
- Iluminación: una luz direccional existente con tono cálido, ambiente de tres colores y niebla lineal. No se añaden luces, partículas ni efectos de bloom.

## Protección de gameplay
No hay cambios en Runtime, T08/T09, el motor, la cámara o el sistema de restauración. No se añade un GameManager.
Se compararon los 36 bloques originales de física/scripts de la escena con develop: **0 modificados**. La herramienta también comprobó sus componentes serializados antes y después de integrar. No se mueve ni sustituye ningún collider original.
La escena base ArroyoGreybox mantiene SHA256 `617AD893A90A3784D633DDDCFDD1163545F9B2EF6B4FD0964A0A0D14A0F3BBD2`.

Los renderers originales sustituidos quedan deshabilitados en la escena integrada, conservando objetos y referencias. Los montajes visuales nuevos se incluyen en DegradedVisuals/RestoredVisuals o bajo la barrera correspondiente; la lógica anterior sigue controlándolos.

## Recursos y generador
Se inspeccionó y ejecutó `ArroyoArtKitBuilder.Generate()` desde la integración. Ya existían siete prefabs y ocho materiales, por lo que no se recrearon los prefabs. Se validaron meshes, ausencia de colliders/scripts, materiales URP/Lit y conservación de GUIDs.
Los siete prefabs originales y sus `.meta` no cambian. En los ocho materiales, Unity sincronizó el campo de compatibilidad `_Color` con `_BaseColor`; se conserva la paleta original y el shader. No se alteran OBJ, PNG, generador de Arte ni sus documentos.

Instancias conectadas a `Assets/_Bosque/Art/Prefabs/VIS_*.prefab`, con materiales `Assets/_Bosque/Art/Materials/MAT_*.mat`. Los overrides de estado degradado/restaurado viven en la escena, no en los originales.

## Verificación ejecutada
| Comprobación | Resultado |
|---|---|
| Unity | 6000.3.22f1, URP 17.3.0 |
| Suite automatizada | **24/24 aprobadas**, 0 fallidas, 0 omitidas; salida 0 |
| Regresiones anteriores | Se conservan y pasan las 21 pruebas de gameplay, incluida la prueba que entra en Play Mode |
| Pruebas visuales nuevas | Prefabs válidos, montajes sin gameplay/colisión, estados correctos y presupuesto acotado |
| Estado degradado | 79 renderers activos, 2.832 triángulos |
| Estado restaurado | 77 renderers activos, 3.012 triángulos |
| Scripts/física originales | 36 bloques comparados con develop, 0 diferencias |
| Capturas | Cuatro renders reales del mismo punto/cámara; restauración mediante PlayerInteractor, sin guardar una partida en la escena |
| Build Windows x64 Development | **Succeeded, 0 errores, salida 0** |
| Arranque de build | Activo 6 s en modo headless, Input System inicializado, sin errores/excepciones encontrados; proceso propio detenido al acabar |

Los conteos de geometría son un presupuesto estático comprobado, **no una medición de FPS ni una garantía de rendimiento en otra GPU**. No se añaden scripts por frame, colliders, luces o partículas. La vegetación pequeña no proyecta sombras. Los materiales y meshes son compartidos por las instancias.

Ejecutable: `Builds/ArroyoV3/BosqueDeRegreso.exe`. Mantener juntos el EXE, los datos y las DLL de su carpeta. La build está ignorada por Git y no se publica como release. El arranque headless no valida la imagen final en una GPU distinta ni sustituye una partida manual. Registros: `.dev-logs/arroyo-v3-build.log` y `arroyo-v3-player.log`.

## Comparación visual
Las imágenes son renders de Unity, no ilustraciones. Muestran el escenario; el HUD no aparece porque se capturó la cámara fuera de Play Mode.

| Estado | Antes de integrar Arte V2 | Después |
|---|---|---|
| Degradado | [Antes](Evidence/ArroyoV3/before-degraded.png) | [Después](Evidence/ArroyoV3/after-degraded.png) |
| Restaurado | [Antes](Evidence/ArroyoV3/before-restored.png) | [Después](Evidence/ArroyoV3/after-restored.png) |

[Resultados de pruebas](Evidence/ArroyoV3/pruebas.json), generados desde el XML real. Registros completos locales en `.dev-logs/arroyo-v3-*`; no se incluyen cachés ni builds en Git.

## Aprobación manual previa y repetición pendiente
El usuario informó que aprobó nueve aspectos **antes de esta integración**. Se registra como evidencia aportada por el usuario, no como prueba manual realizada por Codex después de integrar arte.

| Aspecto | Antes (usuario) | Después de V3 |
|---|---|---|
| Movimiento/salto | Aprobado | Regresión automatizada aprobada; repetir manualmente |
| Cámara | Aprobado | Física/código conservados y prueba automatizada aprobada; revisar oclusión visual manualmente |
| Interacción con E | Aprobado | API y alcance aprobados; repetir tecla física/foco |
| Tres obstáculos | Aprobado | Retirada en escena real aprobada automáticamente |
| Progreso 3/3 | Aprobado | Aprobado automáticamente |
| Restauración | Aprobado | Estados y renders comprobados; revisar legibilidad manualmente |
| Desbloqueo de salida | Aprobado | Aprobado automáticamente, incluido Play Mode |
| Reinicio | Aprobado | Ciclos de reinicio aprobados automáticamente |
| Consola sin errores rojos | Aprobado | Ejecuciones de verificación sin errores de compilación; revisar consola en la partida manual |

Diego/usuario: repetir los nueve aspectos en la nueva escena/build, recorrer ambos estados y girar la cámara junto a vegetación, paredes, rocas, obstáculos y salida. Medir FPS a resolución/hardware registrados, confirmar lectura de obstáculos e indicación [E], y comprobar que el paisaje lejano no se confunda con un camino accesible.

La geometría física sigue siendo un prototipo acotado, con límites laterales y retorno por caída. El paisaje nuevo es decorativo. No se declara resuelta la incidencia histórica de cámara cerca del cuerpo ni se introduce cámara con colisión contra cada hoja. Las comprobaciones manuales posteriores siguen pendientes hasta registrar su ejecución.

## Herramientas y mantenimiento
- `ArroyoVisualIntegration.IntegrateBatch`: abre la escena existente, valida kit e integra visuales una sola vez. Si detecta VisualIntegrationV3, conserva el trabajo en vez de regenerarlo.
- `ArroyoVisualIntegration.FinishLandscapeBatch`: acabado incremental de la ribera/fondo; detecta ValleyBackdrop para evitar duplicados. No reconstruye la escena.
- `ArroyoVisualPreview.BeforeBatch` / `AfterBatch`: capturas reproducibles, sin guardar los estados jugados.
- `ArroyoBuild.BuildVisualWindowsBatch`: build Development Windows en `Builds/ArroyoV3/`, separada de V2.
- Pruebas: runner EditMode habitual; incluye la prueba que entra/sale de Play Mode. Ejecutar un solo proceso Unity por proyecto.

Conservar `.meta` y vínculos de prefabs. Arte puede proponer nuevas variantes, pero los cambios a la escena integrada se revisan con programación. No activar estados desde prefabs artísticos ni cambiar los colliders para adaptarlos a una malla: ajustar el montaje visual o documentar un cambio de diseño aparte.

## Entrega
Commits pequeños, rama publicada y [PR #2 hacia develop](https://github.com/TemisDev/Return-of-the-forest/pull/2). No se modifica main ni se fusiona automáticamente. La copia de trabajo queda limpia al cerrar la entrega.
