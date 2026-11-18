# Entregable 04 — Integración V4 y pulido

Fecha: 18 de noviembre de 2026, America/Lima. Entrega final prevista: 27 de noviembre. Base `develop` **6b594e9**, que ya incorpora Arte V4 **4d19206** y QA V4 **6b594e9**, además de la integración V3. Rama `feature/arroyo-final-polish`. Esta entrega prepara la revisión para RC1; no constituye aprobación final de publicación.

## Resultado de la integración

Se abrió y guardó en Unity **6000.3.22f1 / URP** la escena `Game/Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity`. Se usaron las APIs del editor, sin regenerar la escena. El generador de Juan se ejecutó: sus cuatro materiales y cuatro prefabs ya existían, no faltaban recursos y se conservaron sus GUID. Los derivados están en `Scenes/Integration/ArtV4/`, con sus `.meta` generados por Unity.

- Piedras V4 sustituyen la decoración redundante del barranco y el aspecto del obstáculo 02. Ramas V4 sustituyen los visuales de 01 y 03, ajustadas a los colliders originales.
- Juncos V4 de menor tamaño y color seco en degradado, mayores y verdes en restaurado. Se ocultan los renderers antiguos redundantes; se conservan los recursos originales.
- Suelo seco/húmedo y fondos de roca/musgo usan las texturas V4 sobre superficies con UV. Los OBJ de piedras, juncos y ramas no tienen UV: usan materiales planos mates derivados.
- Cuatro franjas de espuma con alfa, sin colisión ni sombras, sobre el agua restaurada. Una captura de detalle detectó que Lit no daba legibilidad suficiente; se corrigió el derivado a URP Unlit transparente, blanco con alfa 0.8. No se alteró la textura original. El agua mantiene su textura estática V3.
- Salida con madera texturada y letrero que aparece tras restaurar. Luz direccional existente con sombra suave y fuerza 0.7; paisaje lejano sin sombras. No se agregaron luces.
- Efecto existente de restauración con textura suave V4, tamaño 0.14 y máximo de 24 partículas. Se conserva la emisión única por restauración y su reinicio.
- HUD existente con iconos V4, objetivo inicial y siguiente paso explícitos, contador, ayuda F1 y mensaje educativo ODS 15 al terminar. R reinicia; al completar aparecen botones de reinicio y salida. Escape libera el cursor, no pausa. F1 tampoco pausa. El progreso no se guarda al cerrar.

[Registro de recursos y derivados](Evidence/ArroyoV4/recursos.csv).

## Conservación de gameplay

No se modificaron `Runtime/Player`, `Interaction`, `World` ni `ArroyoSession`. Sin segundo GameManager, restauración ni estado de progreso. Los IDs siguen siendo `arroyo.log`, `arroyo.rocks` y `arroyo.branches`. Las referencias de iconos cambian en el HUD existente; los colliders y referencias del puzle se conservan.

La comparación de los **26 bloques serializados de física** contra 6b594e9 es idéntica: [registro](Evidence/ArroyoV4/preservacion.json). La escena base de Claude conserva SHA256 `617AD893A90A3784D633DDDCFDD1163545F9B2EF6B4FD0964A0A0D14A0F3BBD2`. Fuentes de Arte, matrices originales de QA, paquetes y ajustes del proyecto no se modificaron.

## Optimización comprobada

La primera integración produjo **181 renderers activos** en degradado, con 3868 triángulos, y falló la prueba existente de presupuesto (<180). La causa eran los 13 grupos de cada OBJ de juncos y los cuatro de piedras.

Se generan meshes/prefabs combinados derivados por modelo y se reemplaza únicamente la representación visual de sus instancias. El generador verifica igualdad del número de índices. Se mantienen materiales por estado, geometría, transformaciones y referencias originales.

| Estado final | Renderers activos | Triángulos activos |
|---|---:|---:|
| Degradado | 94 | 3868 |
| Restaurado | 109 | 5054 |

La reducción comprobada del degradado es de 181 a 94 renderers, manteniendo sus triángulos. Esto no equivale directamente a draw calls o FPS: esas métricas se miden en la build. No se añadieron colliders decorativos, geometría de nuevas zonas ni partículas ilimitadas. Los materiales derivados permiten instancing.

## Verificación automatizada

Suite base: **27/27**. Antes de integrar V4, las cuatro pruebas nuevas dieron tres aprobadas y una fallida por montaje ausente. Primera suite con V4: **30/31**, por exceso de renderers. Tras combinar los meshes y corregir espuma/HUD, suite final: **31/31**, cero omitidas, salida 0 del editor. Runner EditMode, incluida la prueba existente de ciclo de vida que entra y sale de Play Mode.

Las nuevas pruebas cubren reinicio tras 1/3 y 3/3 antes de salida, caída tras restauración sin perder progreso y configuración de espuma transparente por encima del agua sin colliders. La inspección visual complementa la configuración: la primera imagen mostró un problema perceptivo que la prueba de propiedades por sí sola no detectaba.

[Resultados por prueba](Evidence/ArroyoV4/pruebas.json). Los logs/XML completos de desarrollo permanecen en `.dev-logs/`, ignorado por Git. Las pruebas por API no certifican pulsaciones físicas de WASD/E/R, botones, legibilidad subjetiva ni comodidad de cámara.

## Evidencias visuales

Las imágenes son capturas reales de Unity, sin retoque. Las vistas antes reutilizan las capturas válidas V3 del mismo escenario base: la escena no cambió entre aquella captura y 6b594e9. Las vistas después se tomaron en este ciclo con el mismo encuadre mediante `ArroyoVisualPreview.AfterBatch`, retirando los obstáculos mediante interacciones reales del controlador. Esas vistas del editor no incluyen HUD; las capturas del ejecutable sí.

| Estado | Antes V3 | Después V4 |
|---|---|---|
| Degradado | [Antes](Evidence/ArroyoV4/before-degraded.png) | [Después](Evidence/ArroyoV4/after-degraded.png) |
| Restaurado | [Antes](Evidence/ArroyoV4/before-restored.png) | [Después](Evidence/ArroyoV4/after-restored.png) |

[Espuma en detalle](Evidence/ArroyoV4/foam-detail.png). Las capturas no sustituyen la inspección de todos los ángulos durante un recorrido humano.

## Build y rendimiento reales

Windows x64 **Development: Succeeded, cero errores, salida 0** del editor. [Resumen de build](Evidence/ArroyoV4/build.json). Ejecutable local: `Builds/ArroyoV4/BosqueDeRegreso.exe`; la carpeta completa queda fuera de Git. Código de la build: **e692340**; los commits posteriores de documentación no cambian el ejecutable. Ambas ejecuciones visibles terminaron normalmente; la segunda registró código de salida 0 desde Windows. Los logs de ambas no contienen excepciones ni errores encontrados por la búsqueda; el verificador también aborta ante errores de Unity.

Hardware real: Intel Core i5-10300H, NVIDIA GTX 1650 (3935 MB de VRAM informados al iniciar), RAM 16221 MB, Windows 11, Direct3D12. Sin ejecutar simultáneamente el editor/pruebas de este proyecto durante el recorrido. Es una sesión de escritorio con aplicaciones de fondo, no un laboratorio aislado.

| Muestra 1920×1080 | Degradado | Restaurado |
|---|---:|---:|
| Duración | 60.002 s | 60.001 s |
| Distancia por API | 272.56 m | 272.52 m |
| FPS medios | 250.96 | 218.95 |
| FPS mínimos (inverso del peor frame) | 100.71 | 81.93 |
| Peor frame | 9.93 ms | 12.21 ms |
| Draw calls medios registrados | 243.46 | 231.91 |
| Máxima memoria asignada de Unity muestreada | 107.05 MiB | 108.17 MiB |

[Datos brutos del recorrido de 60 s](Evidence/ArroyoV4/build-1080.json). El objetivo orientativo de 60 FPS se cumple en estas dos muestras de patrulla. No se extiende a todo ángulo, transición, otro equipo o primera carga. Las muestras excluyen las capturas y el instante de restauración; no se midió el peor pico de esa transición ni percentiles de frametime. Son promedios de frames/tiempo real; el mínimo es de un frame, no un percentil ni FPS mínimo sostenido.

El campo `peakWorkingSetMb` del diagnóstico interno devolvió **0** bajo Mono: significa **medición no disponible**, no consumo nulo. Se conserva el JSON bruto sin presentar ese cero como resultado. La memoria asignada por Unity sí tiene lectura válida. Como comprobación independiente se muestreó desde Windows el working set del proceso a 1 Hz durante el segundo arranque, tres ciclos y cierre a **1280×800**: máximo observado **612.676 MiB**. [Muestras externas](Evidence/ArroyoV4/memoria-windows-800.json). Ese máximo pertenece a la ejecución corta de 1280×800, incluye arranque y no es RAM medida del benchmark de 1080p ni un pico absoluto entre muestras.

La segunda ejecución a 1280×800 aprobó movimiento/salto, tres interacciones por API, restauración única, salida y tres reinicios: [datos](Evidence/ArroyoV4/build-800.json). Sus muestras estacionarias de 3 s sirven para comprobar render/HUD y no sustituyen las de 60 s en movimiento. Se cerró y relanzó entre ejecuciones; el cierre fue automático normal, no se probó aún el botón de cerrar ventana con una persona.

Capturas nativas de la build: [interacción 1080p](Evidence/ArroyoV4/01-degraded-interaction.png), [efecto al restaurar](Evidence/ArroyoV4/02-restoration-effect.png), [restaurado](Evidence/ArroyoV4/03-restored.png), [finalización y ODS 15](Evidence/ArroyoV4/04-completed.png), [reinicio](Evidence/ArroyoV4/05-restarted.png), [interacción 1280×800](Evidence/ArroyoV4/hud-800-interaction.png) y [finalización 1280×800](Evidence/ArroyoV4/hud-800-completed.png). Textos principales y panel educativo visibles sin recorte en esas capturas. Botones finales y F1 necesitan prueba de entrada física.

## Cómo reproducir y continuar

1. Abrir `Game/` con Unity 6000.3.22f1. Abrir la escena integrada y pulsar Play. No abrir dos editores sobre el mismo proyecto.
2. En una copia sin V4, ejecutar `Bosque.CoreSetup.ArroyoFinalPolish.IntegrateBatch`, después `Bosque.CoreSetup.ArroyoV4MeshOptimizer.OptimizeBatch`. El primer paso conserva la escena si encuentra `Gameplay/ArtV4Integration`; el segundo omite instancias que ya tienen `V4CombinedVisual`. No borrar esos marcadores para repetir el proceso. Para la corrección aislada del material de espuma existe `ArroyoFinalPolish.RefineFoamBatch`.
3. Pruebas: `-batchmode -nographics -projectPath <Game> -runTests -testPlatform EditMode -testResults <xml> -logFile <log>`.
4. Build Windows Development: `-batchmode -projectPath <Game> -executeMethod Bosque.CoreSetup.ArroyoBuild.BuildV4WindowsBatch -logFile <log>`. Salida independiente en `Builds/ArroyoV4/`. Conservar EXE, DLL y carpeta de datos juntos.
5. Diagnóstico optativo del ejecutable: `-screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -arroyo-verify -arroyo-verify-seconds 60 -arroyo-benchmark-route -arroyo-verify-output <carpeta> -logFile <log>`. Requiere ventana visible y dispositivo gráfico. Sin esos flags, el juego se controla normalmente.

El verificador usa las APIs existentes para moverse/saltar, retirar tres obstáculos, comprobar restauración, atravesar la salida y completar tres ciclos de reinicio. La ruta medida patrulla la orilla X=3 entre Z=3 y Z=32, 60 s en cada estado. No representa un jugador humano ni todos los trayectos posibles. La memoria asignada de Unity se muestrea a 1 Hz; el working set se obtuvo externamente en la ejecución corta descrita arriba. No se midió consumo real de VRAM ni todas las pausas transitorias.

## Pendientes para aceptación RC1

Diego debe usar los **41 casos** de `Docs/QA/Entregable04/MATRIZ-QA-V4.csv`. El [registro de cobertura por ID](Evidence/ArroyoV4/cobertura-QA-V4.csv) es separado y no reescribe los NO EJECUTADO originales. CUBIERTO significa únicamente el modo y alcance documentados, nunca aprobación humana implícita. Repetir los nueve criterios manuales aprobados en V2 sobre esta versión: movimiento/salto, cámara, E, tres obstáculos, 3/3, restauración, salida, R y consola. Añadir F1, foco/cursor, botones finales, cierre por ventana y pantalla completa. Probar cámara y línea de visión junto a todos los props nuevos; recorrer las rampas; revisar alfa/sombras desde varios ángulos.

Conteo inicial de cobertura: **15 cubiertos**, **19 parciales**, **7 no ejecutados**. Tras la ampliación descrita abajo: **17 cubiertos** en su alcance automático/inspeccionado, **20 parciales**, **4 no ejecutados** (U01–U04). Aceptación humana pendiente en los 41. La revisión del código comprobó separación entre presentación y estado, límites de partículas/geometría y conservación de fuentes/referencias. Las entradas físicas y la sensación de cámara permanecen fuera de la suite automatizada y se entregan expresamente a QA.

La prueba UX con una persona ajena al desarrollo (U01–U04) sigue pendiente: comprensión del objetivo, duración del primer recorrido, reconocimiento del cambio e interpretación de iconos. No inferir esos resultados del recorrido automático.

Incidencias conservadas: I-01 protección de retirada estando encima, revalidar manualmente; I-02 salida del cauce entre rampas requiere salto; I-03 tronco opcional subible caminando; I-04 respawn inicial sin checkpoints; I-05 cercanía de cámara/cápsula junto a muros por observar manualmente. El personaje sigue siendo una cápsula, hay límites geométricos del prototipo visibles, agua estática y audio/animación final pendientes. No se incorporaron nuevas zonas.

No fusionar automáticamente el PR. La aceptación final y la decisión de generar RC1 corresponden a QA y al usuario después de resolver o aceptar expresamente los pendientes.

## Ampliación de cierre solicitada por el usuario — 18/11/2026

Suite final ampliada: **36/36 aprobadas**, cero fallidas y cero omitidas, salida 0 del editor. [Resultados completos y alcance](Evidence/ArroyoV4/pruebas-cierre.json). Pruebas en `ec960ba`; verificador externo en `cc1436f`. El primer intento de mutación no falló y reveló que el teclado virtual no estaba procesando teclas en segundo plano. Se reforzó la prueba para exigir eventos procesados y restaurar sus ajustes temporales. Tras invertir deliberadamente la protección de foco del HUD, la prueba falló (`Expected False but was True`, salida 2); se restauraron los bytes originales y pasó la suite completa. Ninguna mutación se incluyó en un commit ni en la build.

Se añaden cuatro casos de prueba sobre el escenario real: interacción mientras se camina, interacción al saltar desde la orilla, escaneo desde nueve posiciones próximas a los tres obstáculos y recorrido de **480 posiciones de cámara** (15 ubicaciones × 16 ángulos × dos estados). Verifican continuidad del controlador, objetivos accesibles, posiciones finitas de cámara y ausencia de colliders entre su foco y su posición. No prueban que una planta o la cápsula nunca oculten visualmente al jugador: esa percepción sigue pendiente. La matriz separada actualiza N05, N10, V04 y R02 con este alcance.

También se añade una prueba de Play Mode con eventos de teclado procesados por Input System y comprobación explícita de `wasPressedThisFrame`. El entorno batch informa **focus=False y cursorLocked=False**: se verifica la protección de ayuda/reinicio/interacción sin foco. No se presenta como prueba de F1/E/R con foco. Los ajustes temporales y el teclado virtual se restauran al terminar el caso; no se cambia la configuración del juego.

Se intentó comprobar la entrada mediante control de escritorio en la build normal. Las pulsaciones y el clic de cierre no produjeron cambios verificables; no se atribuye una causa al juego ni se aprueban esos pasos. Ese proceso de prueba se terminó durante la limpieza y no se cuenta como cierre normal ni como prueba humana. La apertura, el render y los cierres normales de los recorridos automáticos sí tienen evidencia separada. El formulario [CIERRE-QA-V4](CIERRE-QA-V4.md) deja instrucciones y campos concretos para la entrada física, cámara, botones, pantalla completa y UX.

### Repetición a 1080p con RAM externa

Se entrega `Tools/Verify-ArroyoV4.ps1`: abre la build visible, ejecuta el verificador, muestrea working set desde Windows a 1 Hz, comprueba resolución/render/resultado y código de salida, y conserva CSV/JSON. Rechaza una carpeta no vacía para no mezclar ejecuciones. Sintaxis y protección de resultados existentes verificadas. Ejemplo desde PowerShell en la raíz: `& .\Tools\Verify-ArroyoV4.ps1 -OutputDirectory '.dev-logs/qa-nueva-ejecucion'`. No cerrar/minimizar la ventana durante el recorrido ni ejecutar otra carga pesada.

Misma build de código e692340, mismo hardware y dos muestras de 60 s, a 1920×1080 con render verificado. Tres ciclos completos y **cierre normal 0**. [Datos del juego](Evidence/ArroyoV4/build-1080-ram.json), [resumen de Windows](Evidence/ArroyoV4/windows-memory-1080.json), [128 muestras de RAM](Evidence/ArroyoV4/memory-1080.csv).

| Segunda medición 1080p | Degradado | Restaurado |
|---|---:|---:|
| FPS medios | 193.48 | 172.63 |
| Mínimo por peor frame | 61.13 | 48.79 |
| Peor frame | 16.36 ms | 20.49 ms |
| Distancia | 272.53 m | 272.53 m |
| Working set máximo observado por fase aproximada | 623.008 MiB | 636.332 MiB |

Máximo observado del proceso en la ejecución completa: **636.332 MiB**. Las fases de RAM se asignan por hitos de captura y pueden incluir transición; no son fronteras exactas del muestreo FPS. El máximo es muestreado, no instantáneo, y no incluye VRAM. Las medias superan 60 FPS, pero hubo un frame por debajo de ese objetivo en restaurado. **No se garantiza 60 FPS en todos los frames** ni se atribuye una causa sin un perfil de ese pico. Se conservan ambas mediciones, sin sustituir la primera por la más favorable. La variación entre sesiones requiere revisión final de rendimiento por QA.

La build no se recompila en esta ampliación porque solo se añaden pruebas del editor, un verificador externo y documentación; el código y la escena del ejecutable permanecen intactos. [Manifiesto de 288 archivos de runtime con SHA256](Evidence/ArroyoV4/build-manifest.json), excluyendo la carpeta de símbolos marcada DoNotShip. El manifiesto identifica esta build, no constituye una publicación final ni prueba de apertura en otro equipo.

## Entrega Git

[PR #3 hacia develop](https://github.com/TemisDev/Return-of-the-forest/pull/3), abierto y sin fusionar. Commits pequeños de integración `81948f0`, HUD `63b6462`, diagnóstico/build `e692340` y documentación/evidencias `e246355`. Se conserva `main` en `56d2558` y `develop` en `6b594e9`. No se publica una release ni se crea una etiqueta de versión final.
