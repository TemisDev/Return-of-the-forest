# Entregable 02 — Arroyo jugable

Fecha: 16 de noviembre de 2026. Rama: feature/arroyo-gameplay-v2. Base: develop d060233.
Objetivo del ciclo: 22 de noviembre; no incluye Claro ni Refugio.

## Resultado
El arroyo permite retirar con E tres obstáculos en cualquier orden, avanzar de 0/3 a 3/3, recuperar agua/vegetación, desbloquear la barrera, completar al alcanzar la salida y reiniciar con R o botón. La cápsula, movimiento relativo a cámara, salto, gravedad y respawn de T06 permanecen.

La selección usa distancia a superficie, disponibilidad y línea de visión. Los intentos repetidos no cuentan. Se rechaza retirar un obstáculo mientras sostiene al personaje, mitigando I-01 sin cambiar el nivel base.

La lógica está en RestorationProgress; ArroyoSession conecta representación y comandos. El HUD provisional consume ese estado. Los modelos originales del kit de Arte se referencian en la integración con materiales nuevos propios de integración; no se alteran los originales.

Contrato para Arte/QA, APIs y reproducción: [INTEGRACION-ARROYO-V2.md](INTEGRACION-ARROYO-V2.md).

## Archivos principales
- Runtime/Interaction: InteractionTarget, PlayerInteractor.
- Runtime/World: RestorationProgress, RemovableObstacle.
- Runtime/Session: ArroyoSession, ArroyoExit, ArroyoPrototypeHud.
- Runtime/Player: Teleport reutilizado para respawn/reinicio y CaptureCursor; un único controlador y cámara.
- Editor/CoreSetup: integración v2, conexiones artísticas, verificación idempotente, capturas y build.
- Editor/CoreSetup/Tests: nueve pruebas de interacción/progreso, tres de sesión, dos de escena real y una que entra en Play Mode; se conservan las seis pruebas originales.
- Scenes/Integration/ArroyoPlayable.unity y materiales Visuals.

Las rutas Unity anteriores están bajo Game/Assets/_Bosque/. No se modifican Levels, Editor/LevelTools, Art, Runtime/UI ni los documentos originales de QA.

## Evidencia ejecutada en Unity 6000.3.22f1
| Comprobación | Resultado real |
|---|---|
| Primer RED, interacción/progreso | 9 fallos esperados sobre APIs vacías; salida 2 |
| RED de sesión | 3 fallos esperados antes de implementar; salida 2 |
| Suite final | **21 pruebas, 21 aprobadas, 0 fallidas, 0 omitidas; salida 0** |
| Seis pruebas anteriores | Conservadas y aprobadas dentro de la suite |
| Play Mode automatizado | Retirada real, bloqueo inicial, ocupación de salida, victoria y reinicio aprobados |
| Barrera física | Caminata y salto bloqueados antes de restaurar; tránsito permitido después |
| Reinicios | Tres ciclos en prueba de sesión y dos ciclos en escena real, sin duplicados |
| Prueba de sensibilidad | Invertir deliberadamente la condición de finalización produjo fallo; condición restaurada antes de la suite final |
| Integración idempotente | Dos ejecuciones: 203 → 203 objetos; nivel base intacto; salida 0 |
| Render antes/después | Capturas reales del mismo punto, retirando los tres obstáculos por PlayerInteractor; salida 0 |
| Build Windows x64 Development | **Succeeded, 0 errores; salida 0** |
| Arranque del ejecutable | Activo 6 segundos en modo batch/headless; Input System inicializado y sin excepciones/errores encontrados en su log. Proceso de prueba detenido explícitamente después |

Registros locales (ignorados por Git) en `.dev-logs/`: arroyo-v2-red.xml, arroyo-session-red.xml, arroyo-v2-final.xml/.log, arroyo-v2-mutant.xml/.log, arroyo-v2-idempotence.log, arroyo-v2-preview.log y arroyo-v2-build.log.

Ejecutable local: `Builds/ArroyoV2/BosqueDeRegreso.exe`. Distribuir la carpeta completa ArroyoV2 con sus datos y DLL, no solamente el EXE. La build está ignorada por Git y no se publica como release en este ciclo. Registro de arranque: `.dev-logs/arroyo-v2-player.log`.
Se conservan los ajustes serializados generados por la build: prefiltrado de shaders del PC_RPAsset, lista de runtime URP en UniversalRenderPipelineGlobalSettings y batching predeterminado de Standalone en ProjectSettings. Fueron revisados; no son cambios a recursos originales de Arte.

Capturas locales: `.dev-logs/arroyo-v2-before.png` y `.dev-logs/arroyo-v2-restored.png`. Muestran el escenario renderizado; no incluyen el HUD porque se obtuvieron con una solicitud de render de cámara fuera de Play Mode. No son arte conceptual.

Evidencia versionada para revisar sin acceso a los registros locales: [resumen de las 21 pruebas](Evidence/ArroyoV2/pruebas.json), [antes](Evidence/ArroyoV2/antes.png) y [restaurado](Evidence/ArroyoV2/restaurado.png). El JSON se generó desde el XML real de la suite final; las imágenes son copias de las capturas originales.

El nivel base mantiene SHA256 `617AD893A90A3784D633DDDCFDD1163545F9B2EF6B4FD0964A0A0D14A0F3BBD2`. La escena integrada cambia deliberadamente en este entregable.

## Problemas detectados y resueltos durante la verificación
- La herramienta inicial usaba `??` al asegurar componentes. En editor, los objetos nulos de Unity no se comportan como null de C#: faltaba crear un BoxCollider. Se cambió a comparación Unity `== null`; integración e idempotencia aprobadas.
- Invocar Start mediante SendMessage en EditMode causaba un assert del editor. Se expuso InitializeFromScene y se usa la inicialización explícita para pruebas/capturas.
- Los eventos de trigger detectaban geometría, pero no al CharacterController tras teletransportes en la prueba. Se reemplazó la detección por ocupación del BoxCollider de llegada, sin un Rigidbody extra. La prueba fallida se conservó como regresión y ahora aprueba en Play Mode.

## Límites de validación
La prueba en Play Mode mueve el motor y solicita interacción por las APIs reales; no simula manos humanas ni valida la lectura física de E/R, ratón, foco o captura del cursor. No se declara realizada la prueba manual de Diego, ni rendimiento, ni ejecución en otra PC.
I-02 (salida del cauce saltando), I-03 (tronco decorativo subible), I-04 (SafeRespawn inicial) e I-05 (cámara cerca del cuerpo, por confirmar visualmente) siguen documentadas. No se aplicaron los cambios de geometría propuestos por Claude. No hay pausa real, crecimiento, fauna, otras zonas ni arte definitivo.

## Revisión y entrega
La revisión cubre lógica de estados, repetición de interacciones, separación lógica/visual, referencias serializadas, propiedad de archivos y consultas físicas acotadas. No hay cambios de paquetes ni lógica artística duplicada.
La rama se entrega mediante PR hacia develop para revisión humana; no se fusiona automáticamente con develop o main. Consultar el último relevo de Codex para el enlace final.
