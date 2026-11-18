# Relevo de Codex
## Pulido Arte/QA V3 — 18 de noviembre de 2026
Encargo vigente: Docs/ENTREGABLE-03-PULIDO-Y-VALIDACION.md. Se conserva feature/arroyo-visual-integration y su trabajo anterior.
Se auditó la copia limpia y develop df6830a. Arte bbca565 y QA 9535f78 no estaban integrados. El usuario autorizó incorporarlos únicamente a esta rama; merges 99c27e6 y aa566b5. develop y main no se modifican.
Integrados los tres modelos V3 mediante prefabs derivados, las cuatro texturas sobre superficies con UV, los cuatro iconos en el HUD existente y un efecto limitado a 24 partículas. Los OBJ no tienen UV: se usan materiales mates, sin estirarles texturas. Originales de Arte y CSV de QA sin cambios.
T08/T09 y fuentes Runtime/World, Interaction y Player conservados; 26 bloques de física originales intactos. Solo se añaden referencias de iconos al HUD existente y presentación por StateChanged, sin nuevo progreso/GameManager.
Suite final 27/27 aprobada, sin omisiones; incluye Play Mode y pruebas de efecto/reinicio/suscripciones. Mutación deliberada detectada y retirada antes de la suite final.
Build Windows Development Succeeded, 0 errores. Ejecutable: Builds/ArroyoV3Polish/BosqueDeRegreso.exe. Verificador optativo completó movimiento/salto, restauración, salida y tres ciclos completos en 1920x1080 y 1280x800, con render visible y salida normal 0. No se simularon pulsaciones físicas de E/R.
Las capturas/FPS con ventana oculta se descartaron por ausencia de render. Solo se publican muestras visibles válidas: 3 s por estado, GTX1650/i5-10300H/16GB, no equivalentes al benchmark de 60 s. Contadores, resultados y capturas en Docs/Evidence/ArroyoV3Polish/.
Matriz de cobertura separada: 12 casos cubiertos por automatización/inspección dentro del alcance indicado, 19 pendientes completos. Repetir manualmente los nueve criterios históricos, cámara/visibilidad y FPS 60 s; I-02..I-05 no se declaran resueltas.
PR de la rama: https://github.com/TemisDev/Return-of-the-forest/pull/2 (actualizar descripción con esta entrega; no fusionar automáticamente).

## Entregable 03 — 16 de noviembre de 2026
Base develop df6830a, copia limpia al iniciar. Rama feature/arroyo-visual-integration.
El usuario informó aprobación manual de los nueve aspectos de V2 (movimiento/salto, cámara, E, tres obstáculos, progreso, restauración, salida, reinicio y consola). Esa aprobación precede a esta integración y no sustituye su repetición manual posterior.
Arte V2 integrado en ArroyoPlayable mediante prefabs existentes, material de suelo por estado, vegetación ribereña, visuales de obstáculos/barrera, rocas de ribera y paisaje lejano. Se conserva T08/T09 sin cambios de Runtime. Los 36 bloques de física/scripts originales de escena son idénticos a develop; la escena base permanece intacta.
Generador de Arte ejecutado: siete prefabs existentes conservados con GUIDs originales; ocho materiales con sincronización automática del campo _Color. Meshes, texturas, prefabs y código de Arte no alterados.
24/24 pruebas automatizadas aprobadas. Geometría activa: degradado 79 renderers/2832 triángulos; restaurado 77/3012. Comparación en Docs/Evidence/ArroyoV3/. Informe: Docs/ENTREGABLE-03-INTEGRACION-VISUAL.md.
Build Windows x64 Development: Succeeded, 0 errores, salida 0. Ejecutable en Builds/ArroyoV3/BosqueDeRegreso.exe. Arranque headless 6 s sin errores encontrados; proceso de prueba cerrado. Pendiente repetir los nueve aspectos manualmente, revisar oclusión visual de cámara y medir FPS en la PC de prueba.
PR de entrega: https://github.com/TemisDev/Return-of-the-forest/pull/2, abierto hacia develop; sin fusión automática ni cambios a main. No quedan procesos Unity o jugador de prueba activos.

## Entregable 02 — 16 de noviembre de 2026
Rama: feature/arroyo-gameplay-v2, creada desde develop actualizado y copia limpia. Remoto: https://github.com/TemisDev/Return-of-the-forest.
PR de entrega: https://github.com/TemisDev/Return-of-the-forest/pull/1 (base develop, abierto para revisión; sin fusionar).
T08/T09 implementados: interacción E, retirada única de tres obstáculos, progreso, restauración ambiental, barrera, llegada/victoria y reinicio R. Se preserva T06; Teleport reutiliza el controlador y limpia velocidad vertical.
Guía vigente: Docs/INTEGRACION-ARROYO-V2.md. Evidencia y limitaciones: Docs/ENTREGABLE-02-RESULTADOS.md. Estos documentos sustituyen el estado pendiente de T08/T09 del registro histórico inferior.
Suite final: 21/21 aprobadas, incluida prueba automatizada que entra en Play Mode. Integración idempotente 203 → 203 objetos y escena base intacta. Capturas renderizadas de antes/después aprobadas. Build Windows x64 Development: Succeeded, 0 errores, salida 0. Ejecutable en Builds/ArroyoV2/BosqueDeRegreso.exe; arranque headless sostenido 6 segundos sin errores encontrados. No equivale a prueba manual ni en otra PC.
Se conservaron y revisaron los ajustes URP/batching que serializó la build. No hay proceso Unity ni jugador de prueba activo al cerrar la entrega.
I-01 mitigada por rechazo de interacción encima del soporte. I-02..I-05 conservan las limitaciones de la guía. No se editan fuentes originales de Arte, nivel base ni documentos de QA. El kit OBJ/texturas se instancia por referencia en la escena integrada; no se asume un kit de prefabs terminado.
QA siguiente: ejecutar criterios manuales de la guía, entrada real, cámara y build en la PC de prueba. Claude puede reemplazar HUD escuchando StateChanged; no duplicar progreso ni añadir pausa sin contrato.

## Registro histórico T06/T05
Fecha: 11 de noviembre de 2026.
Encargo: T06 e integración de C-01. Este avance sustituye el estado pendiente de T06 descrito más abajo.

## Avance T06
- Entrega C-01 revisada; validador del nivel ejecutado en Unity: 0 errores y 0 avisos.
- Escena jugable: Game/Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity.
- Movimiento relativo a cámara, salto solo en suelo, colisiones y retorno a SafeRespawn al caer por debajo de Y=-5. Cámara orbital con ajuste frente a obstáculos; cuerpo de cápsula provisional.
- Unity 6000.3.22f1: suite EditMode final, 6/6 pruebas aprobadas, salida 0. La prueba de salto detectó una regresión deliberada, retirada antes de repetir toda la suite.
- Capturas renderizadas al inicio y tras simular avance hasta (3.00, 0.03, 14.40), salida 0. Registros locales en .dev-logs/.
- Escena original de Claude intacta. SHA256: 617AD893A90A3784D633DDDCFDD1163545F9B2EF6B4FD0964A0A0D14A0F3BBD2.
- Se conservan los ajustes generados por Unity: SceneTemplateSettings, migración URP del recurso PC y escena integrada en Build Settings.
- Pendiente prueba manual de teclado/ratón, build y apertura en otro equipo. T06/T07 quedan en revisión de Diego.

Controles: WASD, ratón y Espacio. Escape libera cursor y clic izquierdo lo recaptura. Escape todavía no pausa.
No hay interacción, recuperación del agua ni bloqueo lógico de salida: pertenecen a T08/T09.
Claude sigue Docs/ENCARGO-PARA-CLAUDE-C02.md; Codex sigue con T08/T09 y después publica contratos de progreso/sesión antes de encargar UI.
La escena integrada es una copia independiente: los cambios del nivel necesitan reintegración explícita de Codex. El menú de preparación abre la integración existente sin sobrescribirla.

## Registro anterior T05

## Hecho
- GDD y tablero revisados.
- Editor instalado elegido: 6000.3.22f1.
- Game/ creado desde la plantilla 3D Universal instalada.
- Proyecto reabierto y validado por Unity en modo batch: salida 0.
- URP 17.3.0, Input System 1.20.0 y Test Framework 1.6.0, según el manifiesto resuelto.
- Carpetas de responsabilidad creadas y archivos .meta generados por Unity.
- Serialización de texto comprobada y nombre de producto Bosque de regreso.
- Se definieron propiedad de archivos, primer encargo de Claude y marcadores de nivel.

## Verificación
La creación y la reapertura en batch terminaron con código 0. No se encontraron errores de compilación C# en la validación.
No se ha probado aún una copia limpia en otro equipo, un recorrido jugable ni una build. T05 queda para revisión del equipo tras guardar la base en Git.
Todavía no hay controlador ni sistemas de gameplay propios del juego. No hay proceso Unity de Codex activo tras esta entrega.

## Siguiente encargo Codex
T06: movimiento, salto y cámara en una escena de integración. Después T08/T09.
Claude puede comenzar C-01 siguiendo CLAUDE.md y Docs/CONTRATOS.md. Coordinar el uso del proceso Unity.
