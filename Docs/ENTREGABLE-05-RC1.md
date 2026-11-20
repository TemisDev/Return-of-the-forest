# Entregable 05 — Release Candidate 1

**Candidata para evaluación de Juan y Diego; no es aprobación de la entrega definitiva.** Objetivo RC1: 22 de noviembre de 2026. Entrega final: 27 de noviembre. Alcance congelado: Arroyo dormido.

## Identificación y auditoría

Base `develop` **299a479**. La rama existente `feature/arroyo-release-candidate` se actualizó por avance rápido desde 6b594e9, sin reescribir historial. Final Polish V4 (1afe557, merge 5f60634), Arte V5 (f5a1b6b) y QA V5 (f942ce3) son ancestros de la candidata. No se modifica main ni se fusiona automáticamente este PR.

Código exacto de la build: **8838ff5c74038a5399d15b141e30a6b60e5fc949**, generado desde una copia limpia. Unity **6000.3.22f1**, URP, Windows x86-64. [Metadatos de build](Evidence/RC1/build-info.json). Los commits posteriores de herramientas externas, evidencias y documentación no cambian ese ejecutable. Los archivos V6 aparecidos después en la carpeta compartida quedan fuera de esta candidata y de este PR.

La escena `Game/Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity` se conserva sin regeneración. Sus **26 bloques físicos son idénticos** a la base, igual que su archivo completo; la escena original de Claude conserva su hash histórico. [Auditoría de integración y preservación](Evidence/RC1/preservacion.json). Sin nuevas zonas, modelos, colliders decorativos, GameManager ni sistema de restauración. Player, Interaction y World permanecen intactos.

Se revisaron AGENTS, CLAUDE, GDD, contratos, coordinación, relevos y paquetes de Arte/QA V4 y V5. V5 proporciona formatos y herramientas de aceptación, no recursos adicionales que requieran poblar el escenario. Los informes y matrices originales se mantienen intactos; los resultados de este ciclo están bajo `Docs/Evidence/RC1/`.

## Cambios de cierre

- **e5a3877:** la línea inicial de controles ahora explica `E: retirar`, además de movimiento, salto y F1. Se reutiliza el HUD existente; no se introduce otro menú ni estado de sesión.
- **84890d9:** verificador optativo con capturas de inicio/cámara, dos estados desde igual pose, progreso tras cada retirada, estadísticas de geometría/materiales, percentiles de tiempo por frame y una ventana de restauración separada de la codificación de capturas.
- **8838ff5:** build RC1 no Development identificada y build de diagnóstico separada; herramientas reproducibles de compilación/verificación y medición externa de RAM.

La presentación inicial ya muestra el objetivo de retirar tres bloqueos, progreso y controles. El final mantiene la explicación ODS 15: en la naturaleza restaurar requiere tiempo, cuidado y conocimiento local. El cambio inmediato del puzle no se presenta como una simulación científica de recuperación ecológica. Escape y F1 no pausan; no hay guardado persistente.

## Pruebas automatizadas

Suite inicial: **36/36**. Suite final: **38/38**, cero omitidas, salida 0. Incluye integración de escena, movimiento/salto, cámara, rango/LOS, retirada única, soporte bajo el jugador, barrera, progresión, restauración, reinicios, caída y efectos. Mantiene los muestreos de 480 posiciones de cámara y nueve aproximaciones a obstáculos V4. Las dos pruebas añadidas verifican percentiles por rango más cercano y descarte de duraciones inválidas. [Resultados y casos](Evidence/RC1/QA/EVIDENCIAS/pruebas.json).

Antes de implementar las métricas nuevas, las pruebas fallaron por funciones ausentes. Una mutación deliberada del filtro de duraciones produjo dos fallos; se restauró el archivo original y se repitió la suite completa con 38 aprobadas. No se compiló ni publicó una mutación.

Se ejecutaron también las herramientas de Claude:

- [Conservación A03](Evidence/RC1/conservacion-A03.json): correcta, salida 0; trabaja en una copia temporal y conserva el contenido ajeno.
- [Sonda histórica C02](Evidence/RC1/recorrido-historico.json): **21 OK, 1 FALLA, 9 INFO**, aunque el proceso devuelve 0. R-01 espera llegar a la salida sin restaurar, expectativa anterior a T08/T09: ahora la barrera lo impide correctamente. Se conserva el fallo como incompatibilidad de esa herramienta; no se altera el nivel para satisfacerla ni se interpreta su salida 0 como aprobación completa. Las pruebas vigentes de barrera y salida cubren el contrato actual.

Los eventos simulados de teclado en Play Mode verifican protección sin foco. No certifican pulsaciones físicas con foco. El verificador de build dirige las APIs existentes del jugador y la sesión; no representa una prueba humana de WASD/E/R ni una sesión UX con un participante nuevo.

## Build Windows y distribución local

Candidata: **`Builds/Windows/RC1/BosqueDeRegreso.exe`**, `BuildOptions.None`, no Development, cero errores y cero advertencias al compilar. Llevar siempre la carpeta completa. `build-info.json` identifica código, editor, escena, fecha UTC, configuración y resumen de tamaño/tiempo de compilación.

Diagnóstico: `Builds/Windows/RC1-Diagnostics/`, mismo código y escena, con Development. Es una herramienta separada, no el paquete de distribución. Las métricas de la candidata y del diagnóstico se identifican por separado.

RC1 incluye el símbolo de compilación `BOSQUE_RC1_QA` únicamente para habilitar el verificador optativo mediante `-arroyo-verify`. Sin ese argumento no se crea el objeto de diagnóstico y el juego se controla normalmente. No hay controles de prueba visibles en el flujo normal. Para una publicación final se debe revisar expresamente la retirada de esta instrumentación; esta candidata no crea una release pública.

[Manifiesto, tamaño real, SHA256 del EXE/ZIP y comprobación del paquete](Evidence/RC1/package.json). El hash del EXE por sí solo no identifica el contenido de datos/assemblies: el manifiesto y el ZIP completo complementan la procedencia del commit registrada al compilar. Se excluyen de la distribución los directorios marcados DoNotShip, conservados localmente. Builds y todos los ejecutables continúan fuera de Git.

Paquete local: `Builds/Windows/BosqueDeRegreso-RC1-8838ff5-Win64.zip`, **39.627.445 bytes** (37,79 MiB). Runtime con metadatos/instrucciones: **103.848.127 bytes**, 177 archivos. Se verificó el SHA256 de los 177 contenidos del ZIP contra los originales. La copia extraída se ejecutó a 1280×800, completó tres ciclos y cerró normalmente con código 0. [Prueba del paquete](Evidence/RC1/QA/EVIDENCIAS/PACKAGE-CHECK.json). Es la misma PC, no una validación en otro equipo físico.

## Rendimiento y condiciones

Equipo de las ejecuciones: i5-10300H, GTX 1650 de 4 GB, RAM informada 16221 MB, Windows 11, Direct3D12. Calidad PC, VSync 0, límite de FPS -1, según la lectura del ejecutable. Se mide sin editor ni otra compilación de este proyecto en paralelo. Aplicaciones de escritorio permanecen abiertas; no es un laboratorio aislado.

Se realizan tres ejecuciones equivalentes a **1920×1080**, cada una con patrulla automática por la orilla X=3, Z=3↔32, **60 segundos en degradado y 60 en restaurado**, más tres ciclos completos de restauración/finalización/reinicio. [Tabla de las tres ejecuciones](Evidence/RC1/QA/RENDIMIENTO-RC1.csv), [datos y método](Evidence/RC1/QA/EVIDENCIAS/RENDIMIENTO.json). Cada JSON conserva datos por estado, percentiles y mínimos; no se selecciona únicamente la muestra favorable.

| Ejecución | FPS medios degradado / restaurado | p99 ms degradado / restaurado | FPS mínimo de un frame (ambos estados) | RAM máxima MiB |
|---|---:|---:|---:|---:|
| RUN-01 | 235,03 / 220,86 | 8,44 / 11,83 | 17,72 | 616,58 |
| RUN-02 | 245,07 / 224,24 | 7,46 / 8,17 | 18,09 | 604,66 |
| RUN-03 | 232,63 / 223,95 | 6,69 / 6,72 | 26,10 | 602,62 |

Hay outliers de hasta **56,42 ms** durante la verificación de recorrido, pese a las medias elevadas y p99 por debajo de 16,67 ms en esos estados. En las ventanas de restauración repetida, los peores frames fueron **36,07 / 8,26 / 37,47 ms**; sus p99 fueron **19,83 / 6,45 / 6,96 ms**. RC1-PERF-01 queda como observación P2, con G16 bloqueado para el pase final hasta evaluar la fluidez y decidir el riesgo. Las medidas incluyen coste de instrumentación y actividad del escritorio; no se atribuyen estos picos al nivel sin un perfil preciso ni se ocultan con el promedio.

Los máximos de MeshRenderer por estado son 94/109, 3868/5054 triángulos y 16/19 materiales distintos de esas mallas. Se conserva la calidad visual V4. Los primeros frames observados por Unity fueron a 3,26 / 3,76 / 3,94 s de su reloj; el monitor externo detectó la primera captura a 6,11 / 7,11 / 7,12 s. Son marcas distintas, no una medida única exacta de carga. Todas las verificaciones automáticas terminaron con cierre normal 0, incluida pantalla completa real a 1080p, resolución alternativa 1280×800 y diagnóstico separado.

FPS medios = frames válidos / tiempo real. Mínimo = inverso del peor frame, **no 1% low**. p50/p95/p99 usan el rango más cercano sobre duraciones en milisegundos. Se cuenta también cuántos frames exceden 16,67 ms. No se promete 60 FPS en todos los frames por obtener una media superior a 60.

La ventana de restauración dura tres segundos en el **tercer ciclo**, sin codificar capturas durante esa ventana. Es una restauración repetida, no una medición de la primera activación en frío. `restoreCallCpuMs` mide la llamada síncrona; no incluye por sí sola trabajo posterior de render, partículas o drivers. Los picos se conservan y no se les atribuye una causa sin un perfil más preciso.

La RAM es working set del proceso muestreado externamente desde Windows a 1 Hz, en MiB; no es VRAM ni máximo instantáneo. Las fases se aproximan por hitos de captura. El contador interno Mono que devuelve cero se marca no disponible, no consumo nulo. La memoria asignada por Unity tiene su propio indicador de disponibilidad.

`firstRenderedFrameSeconds` es la lectura del reloj de Unity en el primer frame observado por la sonda. El monitor externo arranca su reloj cuando Start-Process devuelve el proceso y detecta la primera captura con muestreo a 1 Hz: incluye inicialización restante y margen de muestreo. No se mezclan ambas como un único tiempo exacto de carga. No se ha controlado el estado frío de las cachés del sistema.

Se conserva la optimización V4: meshes combinados derivados, decoración sin colliders adicionales, materiales mates y hasta 24 partículas. No se reducen sombras ni calidad visual sin una mejora medida. Las estadísticas activas de MeshRenderer/materiales/triángulos están en los reportes; no incluyen el HUD ni todos los costes del motor. Los draw calls se conservan cuando el contador está disponible. Las capturas verifican el render real además de los contadores.

## Arte y evidencias

El auditor original de Juan se ejecutó sobre copias aisladas de sus herramientas: **95 entradas, cero alertas estructurales**. No certifica por sí solo importación, licencias ni aceptación artística. [Auditoría](Evidence/RC1/Art/AUDITORIA-RECURSOS.csv).

[Comparación generada con la herramienta de Juan](Evidence/RC1/Art/COMPARATIVA.html), [revisión técnica por sus 12 criterios](Evidence/RC1/Art/REVISION-VISUAL.csv) y [registro de correcciones](Evidence/RC1/Art/REGISTRO-CORRECCIONES.csv). Hay capturas reales de la candidata y dos estados desde igual pose, sin retoque. La base V4 disponible se identifica en el acta; los encuadres base que no estaban registrados quedan ausentes y el comparador indica FALTA. No se sustituyen por imágenes conceptuales.

## Aceptación y relevo

Los 41 casos V4 se vinculan con los 17 controles V5 mediante sus referencias; no se cuentan como 58 pruebas independientes. [Matriz V4 para esta build](Evidence/RC1/QA/MATRIZ-V4-RC1.csv), [puerta RC1 con modo y alcance](Evidence/RC1/QA/PUERTA-RC1.csv), [resumen de la herramienta de Diego](Evidence/RC1/QA/RESUMEN-RC1.md), [incidencias](Evidence/RC1/QA/INCIDENCIAS-RC1.csv) y [revalidación histórica](Evidence/RC1/QA/REVALIDACION-HISTORICA.csv).

APROBADO en una fila técnica significa exclusivamente el método y evidencia indicados. La aceptación humana permanece pendiente: controles con foco, botones/cierre por interfaz, cámara/percepción junto a muros, pasos/rampas, comprensión de iconos, tiempo del primer recorrido y UX con una persona ajena al desarrollo. La aprobación V2 es histórica y no se traslada a RC1. La ejecución en una segunda PC no está disponible en esta sesión.

La puerta registra **7 aprobaciones técnicas acotadas, 1 bloqueo de aceptación por G16 y 9 casos sin ejecución humana completa**. El resumen de Diego devuelve NO APTA PARA ACEPTACIÓN, sin alertas formales de documentación; se conserva ese resultado. La candidata está preparada para evaluación, no para saltarse esa puerta. El arranque normal sin verificador fue observado: [captura de ventana](Evidence/RC1/Art/CAPTURAS/normal-window.jpg). No se verificó la respuesta de F1 ni el código de salida de esa sesión interactiva; no se cuenta como cierre normal ni aprobación de entrada.

Las observaciones I-02, I-03 e I-04 conservan el diseño existente: salida del cauce por rampas/salto, tronco opcional caminable y respawn inicial sin checkpoints. I-05 conserva cercanía de cámara a muros; la sonda alcanza 0,26 m en algunas orientaciones, aunque no detecta collider entre foco/cámara. Requiere juicio de jugabilidad. La cápsula y el audio/animaciones finales siguen siendo provisionales. No se declaran corregidos por inferencia.

Las verificaciones actuales permiten entregar una candidata identificada al equipo. **No sustituyen la puerta final de aceptación**, que mantiene pendientes manuales P0/P1 y UX. No se han observado bloqueos del producto en las ejecuciones automatizadas descritas; eso no certifica que todos los escenarios humanos estén libres de errores.

[Instrucciones específicas para Juan y Diego](Handoffs/RC1-PARA-JUAN-Y-DIEGO.md). Tras cualquier corrección de código/recursos, generar una candidata nueva con commit y evidencias propios, preservando esta ejecución. No fusionar ni publicar automáticamente.

## Reproducción

1. Desde una copia limpia, con Unity cerrado: `& .\Tools\Build-ArroyoRC1.ps1`. Para diagnóstico separado: añadir `-Configuration Diagnostics`. La herramienta rechaza sobrescribir una build ya identificada.
2. Medición visible: `& .\Tools\Verify-ArroyoRC1.ps1 -OutputDirectory '.dev-logs/rc1-nueva-corrida'`. Repetir tres veces con carpetas distintas. No minimizar la ventana ni ejecutar compilaciones durante la medición.
3. Verificación corta en otra resolución: añadir `-SecondsPerState 3 -Width 1280 -Height 800`. Para pantalla completa: `-Fullscreen 1`. Estos recorridos cortos no reemplazan los tres de 60 s.
4. Pruebas Unity: runner EditMode con `-runTests -testPlatform EditMode -testResults <xml>`, que también contiene casos de Play Mode. Los logs/XML completos permanecen en `.dev-logs/`; se publican resúmenes revisados sin rutas personales.
