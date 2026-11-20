# Bosque de regreso — contexto y estado para otras IA

**Fecha de este resumen: 13 de noviembre de 2026, zona America/Lima.**

Documento de entrada para comprender el proyecto y continuar sin perder decisiones ni sobrescribir trabajo. Resume la conversación, los archivos locales y los relevos disponibles. Es una fotografía del estado actual: antes de actuar, consultar los archivos y el estado Git más recientes.

## Actualización vigente — Entregable 05, RC1
Entrega en GitHub: PR #6 https://github.com/TemisDev/Return-of-the-forest/pull/6, abierto hacia develop, sin fusionar. Los archivos V6 locales de Arte/QA y el relevo posterior de Claude se conservan fuera de la candidata y de este PR; no confundirlos con los recursos de la build 8838ff5.
Base develop 299a479 ya incorpora Final Polish V4, Arte V5 y QA V5. Rama feature/arroyo-release-candidate. Build exacta **8838ff5c74038a5399d15b141e30a6b60e5fc949**, Unity 6000.3.22f1, Windows x64 **no Development**, cero errores/advertencias, en Builds/Windows/RC1/. ZIP completo local Builds/Windows/BosqueDeRegreso-RC1-8838ff5-Win64.zip, verificado por 177 hashes y ejecución de la copia extraída. No está en Git ni publicado como release. Diagnóstico separado en RC1-Diagnostics; verificador optativo inactivo en partida normal.
Suite vigente 38/38, A03 correcto. La sonda C02 antigua mantiene 21 OK/1 FALLA/9 INFO (exit0): espera salida sin restaurar, contrato anterior a T08/T09; no es aprobación global ni un nuevo fallo de la barrera. Escena/26 bloques físicos conservados; cambio visible mínimo E: retirar en controles iniciales. No agregar zonas ni modelos para esta fase.
Tres patrullas 60s/estado a 1080p pasaron, con medias 221–245FPS y RAM externa hasta 616.582MiB; hay picos de recorrido hasta 56.42ms y transición repetida hasta 37.47ms. RC1-PERF-01 P2 requiere revisión/riesgo; no garantizar 60FPS en cada frame. Candidata preparada para evaluación, no aprobación definitiva. Puerta técnica V5: 7 aprobados acotados, 1 bloqueado G16 y 9 pendientes de ejecución humana completa; resumen NO APTA PARA ACEPTACIÓN final hasta cerrar pruebas. Fuentes: Docs/ENTREGABLE-05-RC1.md, Docs/Handoffs/CODEX.md, Docs/Handoffs/RC1-PARA-JUAN-Y-DIEGO.md y Docs/Evidence/RC1/. Fecha objetivo RC1 22/11/2026, entrega final 27/11/2026. Los apartados siguientes son históricos.

## Historial — 18 de noviembre de 2026, Entregable 04
Ampliación posterior solicitada por el usuario: **36/36 pruebas finales**, con movimiento/salto durante interacción, nueve aproximaciones a obstáculos, 480 posiciones de cámara y eventos de teclado confirmados sin foco. Ver Docs/Evidence/ArroyoV4/pruebas-cierre.json. Cobertura actual 17 cubiertos en su modo, 20 parciales y 4 no ejecutados (UX). Nuevo Tools/Verify-ArroyoV4.ps1: patrulla visible 1080p con RAM externa, cierre normal 0, máximo 636.332 MiB, medias 193.48/172.63 FPS y mínimos por frame 61.13/48.79. No garantiza 60 FPS en cada frame. Runtime/escena de build e692340 intactos. Aceptación humana de controles/cámara/botones/pantalla completa/UX pendiente con formulario Docs/CIERRE-QA-V4.md. PR #3 sigue abierto sin fusionar; los siguientes datos de 31 pruebas y primer benchmark son historia de la misma entrega.
Base develop 6b594e9, con Arte V4 4d19206 y QA V4 ya incorporados. Rama feature/arroyo-final-polish. Integración V4 efectiva realizada y capturada en Unity: piedras, ramas, juncos, suelo, espuma transparente, madera/senal de salida, iconos y partículas suaves. Derivados bajo Scenes/Integration/ArtV4, sin alterar originales ni regenerar la escena. Núcleo T08/T09 y los 26 bloques físicos conservados. Se combinaron los grupos fragmentados de los OBJ: degradado 181→94 renderers con los mismos 3868 triángulos; restaurado 109/5054. HUD existente con ayuda F1 y mensaje ODS 15; Escape/F1 no pausan, R reinicia, botones al completar.
Suite final 31/31. Build Windows Development Succeeded con cero errores, en Builds/ArroyoV4/BosqueDeRegreso.exe. Fuente vigente de resultados, métricas reales, evidencias y pendientes: Docs/ENTREGABLE-04-INTEGRACION-Y-PULIDO.md y Docs/Evidence/ArroyoV4/. Las pruebas por API no sustituyen pulsaciones físicas ni QA humano; repetir los nueve criterios sobre V4 y los 41 casos de Diego según su cobertura. Preparar RC1 después de aceptación, no declarar publicación definitiva. Fecha final solicitada: 27 de noviembre de 2026. Los apartados siguientes conservan el historial y pueden estar superados.

## Historial — 18 de noviembre de 2026, pulido Arte/QA V3
Arte bbca565 y QA 9535f78 se incorporaron con autorización expresa del usuario únicamente en feature/arroyo-visual-integration; develop/main siguen sin fusionar esta entrega. Nuevos prefabs/materiales derivados, texturas, iconos y efecto breve usan T09 existente. Núcleo de gameplay y física conservados. Suite final 27/27; build Windows verificada con recorrido automatizado y tres ciclos completos en dos resoluciones. Ejecutable: Builds/ArroyoV3Polish/BosqueDeRegreso.exe.
Fuente actual: Docs/ENTREGABLE-03-PULIDO-Y-VALIDACION.md y último relevo CODEX. Hay capturas reales del HUD y métricas estacionarias de 3 s con ventana visible; no confundirlas con benchmark 60 s ni pruebas humanas de E/R. La repetición manual de los nueve criterios históricos y la aceptación final de Diego siguen pendientes. Los apartados inferiores son históricos.

## Actualización prioritaria — 16 de noviembre, Entregable 02
### Estado posterior: Entregable 03, integración visual
develop ya incorpora Programación, Arte y QA V2. El usuario confirmó aprobación manual de nueve aspectos del gameplay V2. En feature/arroyo-visual-integration se integran prefabs/materiales de Arte V2 sin modificar Runtime ni los 36 bloques de física/scripts originales de escena. Suite: 24/24 aprobadas. Consultar Docs/ENTREGABLE-03-INTEGRACION-VISUAL.md y el relevo de Codex para evidencia, build y PR. La repetición manual tras los cambios visuales sigue pendiente; no confundirla con la aprobación anterior.

El cuerpo siguiente conserva el estado histórico del 13 de noviembre. Para continuar usar primero Docs/INTEGRACION-ARROYO-V2.md, Docs/ENTREGABLE-02-RESULTADOS.md y el último relevo de Codex.
Ahora hay remoto https://github.com/TemisDev/Return-of-the-forest, develop con Arte/QA incorporados y rama de trabajo feature/arroyo-gameplay-v2. La copia estaba limpia al comenzar este ciclo y se actualizó desde origin/develop.
T08/T09 ya tienen implementación en la escena integrada: interacción con E, tres IDs de obstáculo, progreso, restauración, barrera, llegada/victoria y R para reiniciar. Movimiento/cámara se conservan. Hay HUD provisional y referencias al kit artístico sin modificar originales ni la escena base. I-01 se mitiga rechazando interacción estando encima; I-02 a I-05 mantienen las limitaciones documentadas. Los resultados reales y límites de verificación se publican en el informe del Entregable 02. La entrega de este ciclo apunta al 22 de noviembre; la fecha general anterior era el 27. Claro y Refugio quedan fuera de este ciclo.

## 1. Resumen inmediato

Bosque de regreso es un prototipo universitario de videojuego 3D en Unity, centrado en la restauración de hábitats y la ODS 15: Vida de ecosistemas terrestres. El equipo humano tiene tres integrantes y usa Codex y Claude Code como apoyo.

**Existe un arroyo provisional que se puede recorrer, con personaje de cápsula, movimiento, salto, cámara y retorno tras caídas. Todavía no está implementado el puzle de limpiar/restaurar el arroyo.** Las imágenes conceptuales de la presentación no representan el aspecto real del prototipo.

El siguiente bloque de desarrollo es T08/T09: interacción contextual, retirada de tres obstáculos, recuperación del arroyo y desbloqueo de salida. Antes debe revisarse la entrega C-02 de Claude y sus incidencias. Las pruebas manuales siguen pendientes en los documentos; no asumir que el usuario las realizó por haber pedido abrir Unity.

## 2. Ubicación y apertura

Raíz del repositorio del juego en este equipo:

```text
C:\Users\vince\OneDrive\Documents\ChatGPT\Personal life\Bosque de regreso
```

Carpeta que se agrega en Unity Hub:

```text
C:\Users\vince\OneDrive\Documents\ChatGPT\Personal life\Bosque de regreso\Game
```

- Editor acordado e instalado: **Unity 6000.3.22f1**.
- Proyecto creado desde plantilla **3D Universal / URP**.
- Escena que se debe abrir para ver el avance: `Game/Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity`.
- En el panel Project de Unity: `Assets > _Bosque > Scenes > Integration > ArroyoPlayable`; después pulsar Play.
- La escena `Levels/Arroyo/ArroyoGreybox.unity` es la base del nivel de Claude, no la escena de gameplay integrada.

### Controles implementados

| Entrada | Comportamiento actual |
|---|---|
| WASD | Movimiento relativo al giro horizontal de cámara |
| Ratón | Cámara orbital |
| Espacio | Salto cuando está en el suelo |
| Escape | Libera el cursor; **no pausa** |
| Clic izquierdo en Game | Recaptura el cursor |

E para interactuar y Q para crecimiento pertenecen al diseño; **no están implementadas todavía**.

## 3. Concepto y alcance del diseño

Fuente de diseño: `GDD-Bosque-de-regreso-v0.1.md`. Es una propuesta inicial; varias decisiones narrativas y artísticas siguen ajustables.

- Aventura para un jugador, tercera persona, exploración, plataformas sencillas y puzles ambientales.
- Plataforma de trabajo: PC Windows, teclado y ratón.
- Duración objetivo de la versión propuesta: 10–15 minutos; no es una duración ya conseguida.
- Protagonista propuesta: joven guardiana que regresa a un bosque degradado.
- Mundo ficticio inspirado en paisajes andinos; no se presenta como una simulación científica de un ecosistema real.
- Bucle: explorar → observar → resolver → recuperar hábitat → abrir camino.
- Tres zonas previstas: **arroyo dormido**, **claro vacío** y **refugio perdido**.
- Arroyo: retirar obstáculos para devolver el agua. Claro: crecimiento en puntos definidos. Refugio: recuperar condiciones para el retorno de fauna.
- Dirección artística: low poly estilizado, materiales mates, vegetación modular; contraste entre degradación y recuperación.
- ODS principal: 15, con relación de diseño a metas 15.1 y 15.2.
- La recuperación instantánea es una simplificación fantástica. El mensaje educativo debe reconocer el tiempo y seguimiento que exige la restauración real.

Fuera del alcance: combate, mundo abierto, multijugador, inventario complejo, simulación ecológica, guardado persistente y servicios externos. No añadir estos sistemas para ampliar el prototipo.

## 4. Equipo, responsabilidades y calendario

| Persona o asistente | Responsabilidad |
|---|---|
| Usuario | Programación Unity, coordinación e integración humana |
| Juan | Arte 3D, recursos visuales, animación y apoyo en audio |
| Diego | Pruebas, diseño de niveles y documentación |
| Codex | Base técnica, gameplay, contratos, integración, configuración y builds |
| Claude Code | Herramientas de nivel, niveles, casos de aceptación y, después de acordar contratos, interfaz |

El usuario eligió programación y propuso arte/pruebas para sus compañeros. Las descripciones de Trello conservan responsables humanos; no equivalen a asignaciones a cuentas verificadas.

Plazo original: tres semanas desde el 6 de noviembre. **Entrega objetivo: 27 de noviembre de 2026.**

| Hito previsto | Fecha | Situación que puede afirmarse |
|---|---|---|
| H1: alcance, historia, mapa y guía visual | 6–9 noviembre | Existen documentos y referencias; no asumir aceptación completa |
| H2: primera zona jugable | 13 noviembre | Recorrido provisional disponible; puzle y aceptación manual pendientes |
| H3: tres zonas conectadas | 20 noviembre | Pendiente |
| H4: correcciones y entrega | 27 noviembre | Pendiente |

Contingencia acordada: si el arroyo completo no funciona el 13 de noviembre, reducir a una zona completa con un final breve y posponer claro/refugio. A la fecha de este resumen la lógica de recuperación sigue pendiente; corresponde revisar el alcance, no marcar H2 completado por fecha.

## 5. Documentación, presentación y Trello realizados

- GDD v0.1 con concepto, mecánicas, narrativa/arte, implementación, equipo, hitos y tareas.
- Presentación elaborada y rediseñada con un estilo más cuidado, imágenes conceptuales y notas. La versión ampliada trabajada contiene **18 diapositivas**.
- Se añadieron referentes de videojuegos y una explicación del problema ambiental en Perú y el mundo con fuentes.
- Se entregó en conversación un guion para las primeras ocho diapositivas. No se identificó un archivo independiente del guion en la raíz del proyecto.
- Referentes de diseño incorporados: **Kena: Bridge of Spirits**, **Terra Nil** y **Alba: A Wildlife Adventure**.
- Beyond Blue, Dave the Diver y Hungry Shark World pertenecían al antecedente **Marea Alta**; no confundirlos con referentes adoptados automáticamente para este juego.
- Dirección visual conservada en `assets/direccion-visual-v2.md`; fuentes de las diapositivas ampliadas en `assets/fuentes-ampliacion-v3.md`.
- Se generó arte conceptual para arroyo, claro, refugio, mapa, guardiana, crecimiento, kit modular y prueba de salto. Es referencia visual; no son modelos 3D importables ni capturas del gameplay.
- En `.slides-build/` hay respaldos JSON de la presentación, entre ellos `presentacion-v3-18-diapositivas.json` y `presentacion-v3-final.json`. Esa carpeta es auxiliar e ignorada por Git. No se ha confirmado aquí una exportación local PPTX; no prometer un archivo PowerPoint inexistente.
- Identificador del artefacto de presentación registrado en la conversación: `artifact_xcrtYeSi5wC9EU_qQfG8rEyIXGIcV2_H`. Puede requerir acceso a la sesión original.
- Tablero creado con 24 tareas y tarjetas de documentación: [Trello de Bosque de regreso](https://trello.com/b/rv1JFCL7/bosque-de-regreso-gdd-y-desarrollo-unity-3d).
- Listas: Guía y GDD, Por hacer, En curso, En revisión y Terminado. Dependencias y enlaces de tareas en `tasks/plan.md`.
- Último cambio de Trello confirmado por Codex: T06 y T07 pasaron a **En revisión**. Esto no verifica cambios posteriores del tablero.

Las cifras ambientales de la presentación tienen fecha y ámbito documentados en `assets/fuentes-ampliacion-v3.md`: proceden de consultas del 6 de noviembre y reportes de años concretos. Si se necesitan cifras actuales, verificarlas de nuevo; no presentarlas como mediciones de hoy.

## 6. Base técnica y archivos implementados

### Versiones

| Componente | Versión local |
|---|---|
| Unity | 6000.3.22f1 |
| Universal Render Pipeline | 17.3.0 |
| Input System | 1.20.0 |
| Unity Test Framework | 1.6.0 |

Fuentes: `Game/ProjectSettings/ProjectVersion.txt`, `Game/Packages/manifest.json` y `packages-lock.json`. La presencia de paquetes de plantilla no significa que sus funcionalidades estén usadas por el juego. La cámara actual es propia, no Cinemachine.

### Base T05

Proyecto creado, importado y reabierto en Unity batch con salida 0. Se establecieron estructura de carpetas, serialización de texto, conservación de `.meta`, Git, Git LFS, exclusiones de cachés y reglas de coordinación. Falta confirmar apertura en otro equipo y una build.

### Nivel C-01 / T07, entregado por Claude

- `Game/Assets/_Bosque/Editor/LevelTools/ArroyoGreyboxGenerator.cs`: genera/actualiza nivel.
- `ArroyoGreyboxValidator.cs`: comprueba contrato de escena.
- `GreyboxBuilder.cs`: utilidades de buscar/crear preservando contenido ajeno según reglas documentadas.
- `Game/Assets/_Bosque/Levels/Arroyo/ArroyoGreybox.unity`: nivel base con primitivas.
- `Levels/Arroyo/GreyboxMaterials/`: 16 materiales URP provisionales.
- `Docs/QA/ARROYO-ACEPTACION.md`: aceptación e incidencias.

El nivel contiene orilla, cauce, rampas, tres obstáculos, salida, límites y un borde abierto para probar caídas. Marcadores/grupos del contrato: `PlayerSpawn`, `SafeRespawn`, `ExitMarker`, `Obstacle_01..03`, `DegradedVisuals`, `RestoredVisuals`. También existen `SafePoint_01..03` como propuesta, sin lógica de checkpoints.

Menús: `Tools > Bosque > Niveles > Arroyo - Generar o actualizar greybox` y `Arroyo - Validar escena`. La regeneración puede restablecer propiedades que controla la herramienta; no equivale a conservar cualquier ajuste manual de geometría.

### Movimiento y cámara T06, implementados por Codex

En `Game/Assets/_Bosque/Runtime/Player/`:

- `ThirdPersonMotor.cs`: CharacterController, movimiento relativo a cámara, velocidad diagonal limitada, gravedad, salto solo en suelo y retorno tras caer por debajo de Y=-5.
- `ThirdPersonCamera.cs`: seguimiento orbital, suavizado y ajuste frente a obstáculos mediante consulta física.
- `ThirdPersonInput.cs`: teclado/ratón y liberación/recaptura de cursor.
- Assembly `Game/Assets/_Bosque/Runtime/Bosque.Runtime.asmdef`, con referencia a Unity.InputSystem.

Parámetros iniciales: 4,5 m/s; salto configurado de 1,1 m; gravedad -24 m/s²; controlador de 1,8 m de altura y 0,35 m de radio. El retorno usa siempre `SafeRespawn`.

En `Game/Assets/_Bosque/Editor/CoreSetup/`:

- `ArroyoPlayableSetup.cs`: crea o abre la copia integrada; no sobrescribe una integración existente.
- `ArroyoPreview.cs`: captura renderizada y avance por simulación para comprobación visual.
- `Tests/MovementTests.cs` y su assembly de pruebas.

Escena integrada: `Game/Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity`, con cápsula provisional y cámara. Está en Build Settings, pero eso **no significa que exista una build realizada**.

La integración es una **copia independiente** del nivel. Los cambios futuros de Claude requieren reintegración explícita de Codex. No asumir sincronización automática ni regenerar encima de ella.

## 7. Verificaciones y entrega C-02

### Evidencia anterior de Codex

- Validador del nivel: 0 errores, 0 avisos en la integración inicial.
- Suite EditMode: **6/6 aprobadas**, salida 0. Cubre dirección relativa a cámara/diagonal, pared, salto sin repetición aérea, retorno tras caída, dt cero y cámara frente a pared.
- Se comprobó que la prueba de salto detectaba una regresión deliberada; el cambio defectuoso se retiró y la suite final pasó.
- Render y avance simulado hasta `(3.00, 0.03, 14.40)`, salida 0.
- Registros y capturas locales en `.dev-logs/`: `movement-final.xml`, `movement-final.log`, `integration-preview.log`, `arroyo-start.png` y `arroyo-walk.png`. Son auxiliares ignorados por Git.

### C-02 de Claude, fechada 12 de noviembre

**C-02 ya fue entregada. No volver a encargarla como si estuviera por empezar.** El relevo de Codex y algunos textos de planificación son anteriores a esta entrega.

- Según `Docs/Handoffs/CLAUDE.md`: sonda final con **22 OK, 0 FALLA y 9 informativas**.
- Comprobación A-03 de conservación de contenido ajeno: aprobada en una copia temporal eliminada después.
- Nuevos scripts: `Game/Assets/_Bosque/Tests/Acceptance/Editor/ArroyoTraversalProbe.cs` y `ArroyoA03PreservationCheck.cs`, con sus `.meta`.
- Se añadió `Generate(string scenePath)` al generador para pruebas sobre copias, rechazando rutas bajo `Assets/_Bosque/Scenes/`.
- Se ampliaron QA y el relevo de Claude. Compilan en Assembly-CSharp-Editor, sin un `.asmdef` nuevo.
- Recorrido medido Spawn → Exit: 8,3 s. Rampas transitables en ambos sentidos; obstáculos alcanzables desde la orilla; cinco caídas, cinco retornos; 504 orientaciones de cámara comprobadas.
- Estos son resultados reportados por Claude, revisados documentalmente para este resumen; **no se repitieron las ejecuciones en esta sesión de documentación**.
- Claude no ejecutó pruebas manuales. Los 22 casos M-01..M-22 quedan pendientes para Diego. Sus sondas no validan entrada real, cursor, sensación de control, rendimiento ni build.

Hashes de escenas comprobados al preparar este resumen, coincidentes con C-02:

```text
ArroyoPlayable.unity
39EE49F2B467668782E0CE53AC8CD52B4A8F92C26D74C725E6085D1039836ED0

ArroyoGreybox.unity
617AD893A90A3784D633DDDCFDD1163545F9B2EF6B4FD0964A0A0D14A0F3BBD2
```

## 8. Incidencias abiertas y decisiones pendientes

| ID | Observación | Estado / siguiente decisión |
|---|---|---|
| I-01 | Se puede caminar sobre obstáculos 1 y 3; retirarlos con el jugador encima podría hacerlo caer al cauce | Media. Resolver para T08; Claude propone elevar geometría/colliders |
| I-02 | Entre obstáculos se sale del cauce saltando, no caminando | Baja. Claude propone rampas en Z 14 y Z 22 |
| I-03 | El tronco opcional de 0,35 m se sube andando por su collider redondeado | Baja. Decidir si debe enseñar salto o ser decorado |
| I-04 | Retorno siempre al inicio, con retroceso de hasta unos 26 m | Baja. Valorar puntos seguros por tramo en T09 |
| I-05 | Cerca de paredes la cámara puede acercarse al interior del cuerpo | Baja, efecto visual aún por confirmar manualmente en M-13 |

Los ajustes de geometría de C-02 son **propuestas no aplicadas**. Consultar medidas y reproducción exacta en el relevo de Claude y QA. No afirmar que los defectos ya están corregidos.

## 9. Sistemas que todavía faltan

- T08: interacción contextual, alcance, selección de objetivo e indicación para el jugador.
- T09: retirada única de obstáculos, progreso, recuperación de agua/visuales, bloqueo y desbloqueo real de salida.
- Reinicio limpio de estado, pausa real y comandos/eventos de sesión.
- Habilidad de crecimiento, puzles del claro y refugio, conexión de zonas y final.
- HUD, menús, interfaz de pausa/reinicio y progreso.
- Protagonista definitiva, animaciones, fauna funcional, audio, efectos y recursos finales con licencias.
- Validación manual de Diego, pruebas con jugadores, rendimiento y build Windows en otra PC.

Existen grupos visuales de estado y obstáculos físicos, pero no lógica de restauración. Actualmente se alcanza el portal sin resolver un puzle; no confundir recorrido con zona completa.

## 10. Reglas para trabajar sin conflictos

Leer primero `AGENTS.md`, `Docs/COORDINACION-IA.md`, `Docs/CONTRATOS.md` y ambos archivos de `Docs/Handoffs/`. Claude debe leer además `CLAUDE.md`.

Rutas bajo `Game/Assets/_Bosque/`, salvo indicación:

| Propietario | Carpetas |
|---|---|
| Codex | Runtime/Player, Interaction, World, Session, Contracts; Editor/CoreSetup; Scenes/Integration; además Game/Packages y Game/ProjectSettings |
| Claude | Editor/LevelTools, Levels, Runtime/UI, Tests/Acceptance; además Docs/QA y su relevo |

- Cada asistente actualiza únicamente su propio relevo. No modificar archivos del otro bloque sin acuerdo registrado.
- Codex integra configuración, escenas finales, assemblies y builds.
- No inventar APIs compartidas. Para UI faltan publicar lectura de progreso, eventos y comandos de pausa/reanudación/reinicio. Claude no debe crear un segundo GameManager ni controlar Time.timeScale por su cuenta.
- Conservar `.meta`; Unity genera los nuevos. 1 unidad equivale a 1 metro; Y hacia arriba.
- No ejecutar dos procesos Unity sobre `Game/` a la vez, ni operaciones Git simultáneas.
- No ejecutar cambios de rama, reset, clean o stash destructivos durante el trabajo compartido.
- No publicar repo, build o recursos sin instrucción específica.
- Verificar en Unity antes de declarar compilación o funcionamiento. Identificar siempre si la evidencia es manual, automatizada o solo lectura.

## 11. Estado Git al preparar este resumen

Repositorio propio dentro de la carpeta del juego; no operar sobre el repositorio padre `Personal life`.
Rama: `codex/bootstrap-game`. Sin remotos configurados en la inspección realizada. Git LFS está configurado para recursos binarios; cachés y registros están excluidos.

Últimos commits existentes:

| Commit | Contenido |
|---|---|
| 7ce4d2e | Coordinación y responsabilidades |
| 992c92f | Base Unity URP |
| 67e3b40 | Arte conceptual y referencias con LFS |
| f51644b | Entrega C-01 del arroyo y aceptación |
| 327de14 | Movimiento/cámara y escena integrada |
| 67da60c | Relevo para revisión del recorrido |

**La entrega C-02 está en archivos locales, todavía sin commit en el estado inspeccionado.** Hay modificaciones en el generador, relevo de Claude y QA, además de nuevos scripts/.meta de Acceptance/Editor. También hay cambios locales en `Game/Assets/Settings/DefaultVolumeProfile.asset` y `Mobile_RPAsset.asset`: su origen y necesidad no se determinaron al redactar este resumen. Revisarlos antes de incluirlos en una integración; no descartarlos ni atribuirlos automáticamente a una persona.

Este documento se crea como archivo nuevo; no se han integrado ni confirmado en Git los cambios pendientes de C-02 durante esta tarea.

## 12. Orden recomendado para continuar

1. Revisar estado Git, el relevo C-02 y sus scripts; preservar los cambios locales existentes.
2. Ejecutar los casos manuales M con Diego, especialmente cursor/foco y cámara junto a paredes. Registrar resultados reales.
3. Resolver o acordar mitigación de I-01/I-02 antes de retirar obstáculos; si cambia el nivel, Codex debe reintegrarlo sin perder componentes de gameplay.
4. Revisar e integrar C-02 con verificaciones apropiadas, sin mezclar ajustes de configuración no entendidos.
5. Codex implementa T08/T09 y publica contratos de progreso/sesión. Verificar alcance, acciones repetidas, orden de obstáculos, restauración única, salida y reinicio.
6. Claude implementa UI cuando estén los contratos; Juan entrega arte compatible. Diego valida la primera zona completa.
7. Evaluar la contingencia de alcance según el estado real del arroyo, y reservar tiempo para build y correcciones.

## 13. Fuentes internas y antecedentes

- `GDD-Bosque-de-regreso-v0.1.md`: propuesta de diseño.
- `README.md`: apertura rápida.
- `AGENTS.md`, `CLAUDE.md`: instrucciones de trabajo.
- `Docs/COORDINACION-IA.md`, `Docs/CONTRATOS.md`: responsabilidades y contratos.
- `Docs/Handoffs/CODEX.md`, `Docs/Handoffs/CLAUDE.md`: entregas y evidencia; los apartados históricos pueden describir estados ya superados.
- `Docs/QA/ARROYO-ACEPTACION.md`: pruebas e incidencias actuales.
- `Docs/ENCARGO-PARA-CLAUDE.md`, `Docs/ENCARGO-PARA-CLAUDE-C02.md`: encargos históricos ya entregados.
- `tasks/plan.md`: hitos, dependencias y enlaces de tarjetas.
- `assets/direccion-visual-v2.md`, `assets/fuentes-ampliacion-v3.md`: presentación, referentes y fuentes ambientales.
- Plantilla FigJam aportada por el usuario: https://www.figma.com/board/UxWlvDCrafihgv2clOFEP8/Template-GDD-for-Universidad-Continental-students--Community-?node-id=0-1
- Presentación anterior aportada por el usuario: https://canva.link/5tc0guaui2kfwpt

**Al actualizar este resumen:** cambiar la fecha, distinguir lo implementado de lo propuesto, añadir evidencia y mantener visibles los pendientes. No convertir planes o resultados de otra sesión en pruebas ejecutadas personalmente.
