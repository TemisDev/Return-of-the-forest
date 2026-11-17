# Arroyo jugable v2 — contrato de gameplay, Arte y QA

Fecha: 16 de noviembre de 2026. Ciclo: hasta el 22 de noviembre.
Alcance: Arroyo dormido, T08 y T09. Unity 6000.3.22f1 / URP 17.3.0 / Input System 1.20.0.

## Abrir y jugar
Agregar `Game/` a Unity Hub. Abrir `Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity` y Play.
WASD: mover; ratón: mirar; Espacio: salto; E: interactuar; R: reiniciar el arroyo. Escape libera cursor y clic izquierdo lo recaptura. Escape no pausa la simulación.

Acercarse desde la orilla a los obstáculos en Z 10, 18 y 26. La indicación [E] aparece cuando hay un objetivo disponible, cercano y visible. Se pueden retirar en cualquier orden. A 3/3 vuelve el agua, cambia la vegetación y desaparece la barrera de salida. Cruzar el volumen en Z 36.3 completa el recorrido, muestra victoria y libera el cursor. Reiniciar devuelve personaje, obstáculos, entorno, progreso y barrera al estado inicial.

## IDs estables
| Concepto | ID | Objeto inicial |
|---|---|---|
| Zona | arroyo | ArroyoGameplayV2 |
| Objetivo de diseño | arroyo.restore | Completar los tres obstáculos |
| Obstáculo tronco | arroyo.log | Obstacle_01 |
| Obstáculo rocas | arroyo.rocks | Obstacle_02 |
| Obstáculo ramas | arroyo.branches | Obstacle_03 |

Los IDs de obstáculo son cadenas exactas, únicas y no vacías. `arroyo` y `arroyo.restore` identifican el diseño/documentación; no existe todavía un registro general de misiones. El runtime interactivo no compara nombres de GameObjects. Solo la herramienta de integración encuentra los marcadores del nivel al configurar referencias.

## Contratos C# reales
Assembly: `Bosque.Runtime`. No se añaden paquetes ni se reemplaza el controlador de T06.

| Tipo | API y responsabilidad |
|---|---|
| Bosque.Interaction.InteractionTarget | MonoBehaviour abstracto: `string Prompt`, `bool CanInteract(PlayerInteractor)`, `bool TryInteract(PlayerInteractor)` |
| Bosque.Interaction.PlayerInteractor | `Target`, `Prompt`, `Origin`, `MaxDistance`; `Scan()`, `TryInteract()`, `ClearTarget()`, `CanReach(target, collider)` |
| Bosque.World.RestorationProgress | Modelo lógico: `RemovedCount`, `TotalCount`, `IsRestored`, `IsCompleted`, `IsRemoved(id)`; `TryRemove(id)`, `TryComplete()`, `Reset()` |
| Bosque.World.RemovableObstacle | InteractionTarget: `ObstacleId`, `Configure(id, collider)`, `Bind(session)`; consulta el progreso de su sesión, no guarda un segundo contador |
| Bosque.Session.ArroyoSession | Referencias serializadas y representación del estado. `Progress`, `Player`, `InitializeFromScene()`, `Configure(...)`, `Restart()`, `TryExit(actor)` |
| Bosque.Session.ArroyoExit | BoxCollider de llegada y referencia serializada a sesión; comprueba ocupación del personaje configurado dentro del volumen |
| Bosque.Session.ArroyoPrototypeHud | Vista provisional de indicación, progreso, objetivo, victoria y reinicio; delega estado/comandos a sesión |

`ThirdPersonMotor.Teleport(position, rotation)` reutiliza el controlador existente, limpia velocidad vertical y recoloca sin colisión intermedia. Respawn usa ese mismo método. `ThirdPersonInput.CaptureCursor()` permite recuperar el control al reiniciar. No se añaden otros motores ni cámaras.

### Eventos
- `RestorationProgress.Changed`: tras retirada válida, primera finalización y cada Reset. Los intentos desconocidos o repetidos no emiten cambios.
- `RestorationProgress.Restored`: una vez al pasar a todos los obstáculos retirados. Puede volver a emitirse en una nueva partida después de Reset.
- `ArroyoSession.StateChanged`: después de aplicar el estado lógico a sus visuales, barrera y controles. La configuración inicial también aplica/emite el estado.
- El HUD definitivo puede escuchar `StateChanged` y leer `session.Progress`. Suscribirse después de `InitializeFromScene()` y retirar suscripciones al deshabilitar/destruir. El modelo `Progress` se reemplaza al volver a ejecutar `Configure`, no al reiniciar.
- No existen aún contratos de pausa/reanudación ni guardado. La UI no debe implementarlos por su cuenta.

## Condiciones de interacción
- Distancia máxima serializada en PlayerInteractor: 2,5 m inicialmente, medida desde el pecho (base + 0,9 m) al punto más cercano del collider, no al centro del transform.
- Selección de superficie disponible más cercana. No exige apuntar con la cámara; la indicación identifica la acción.
- Collider sólido activo, componente activo, objetivo disponible, personaje en alcance y línea de visión libre de geometría ajena. Se ignoran colliders del propio personaje.
- La tecla E se acepta con foco de aplicación y cursor capturado. El intento revalida estado y alcance; una referencia antigua no permite interactuar desde lejos.
- I-01: si la proyección del personaje solapa el obstáculo y sus pies están sobre su parte superior, no se retira. El collider y los visuales permanecen activos. Bajar primero a la orilla. Esto también protege un salto encima del soporte.
- La retirada desactiva el GameObject del obstáculo completo. Progreso controla qué obstáculos están retirados. Pulsaciones repetidas no vuelven a contar.
- Consultas físicas sin asignaciones por frame, con capacidad de 128 resultados; si se satura la consulta se rechaza la selección. Si Arte añade mucha colisión, revisar esa capacidad y las capas.

## Estados, salida y reinicio
| Estado lógico | DegradedVisuals | RestoredVisuals | ExitBlocker | Control |
|---|---|---|---|---|
| 0–2 obstáculos | Activo | Inactivo | Activo | Jugable |
| 3 obstáculos, sin llegar | Inactivo | Activo | Inactivo | Jugable |
| Restaurado y llegada | Inactivo | Activo | Inactivo | Movimiento/interacción deshabilitados; cursor libre |
| Reinicio | Activo | Inactivo | Activo | Jugable en PlayerSpawn |

La barrera tiene un BoxCollider independiente de los grupos de estado. Abarca cauce y orilla y supera la altura de salto; no depende de la malla artística. Su visual está bajo `ExitBlocker/VisualMount` y puede reemplazarse sin tocar el collider.

La llegada se detecta comprobando el punto del pecho dentro del BoxCollider de `ExitTrigger`, respetando escala y orientación mediante ClosestPoint. No depende de eventos de trigger: estos no notificaron al CharacterController en la prueba con teletransportes. No se necesita un Rigidbody adicional. La sesión exige además los tres obstáculos y la identidad del personaje configurado, y finaliza una sola vez.

Reinicio: teletransportar a PlayerSpawn y limpiar velocidad vertical; vaciar IDs retirados/finalización; reactivar los tres obstáculos, visuales degradados y barrera; desactivar restaurados; limpiar objetivo; habilitar movimiento/interacción y capturar cursor. No recarga escena ni duplica objetos. El respawn por caída continúa usando SafeRespawn, sin restaurar el puzle.

## Prefabs y recursos de Arte
- Kit revisado de `feature/art`, ya incluido en develop: cinco OBJ originales, diez texturas planas e iconos. No se asumió que fueran prefabs terminados.
- Se instancian referencias de los modelos importados en la escena integrada; los materiales nuevos están en `Scenes/Integration/Visuals/` y referencian las texturas originales.
- Los grupos `KitVisualMount` dentro de los estados cambian junto con el entorno; el grupo permanente permanece visible.
- Los meshes del kit son visuales sin colliders nuevos. No cambian navegación ni colisión base.
- Un prefab de obstáculo debe conservar raíz estable con RemovableObstacle, collider sólido de gameplay y referencia a ese collider. Sus hijos visuales/materiales son reemplazables. El ID y la referencia de sesión los configura programación.
- No mover colliders a hijos que desaparezcan antes de que cambie el estado. El collider no puede estar en DegradedVisuals/RestoredVisuals si es necesario para navegación permanente.
- No incluir lógica de contador, restauración ni GameManager en prefabs artísticos.
- Parámetros de aspecto ajustables: meshes, escala/posición de hijos visuales, materiales URP, Base Map/Base Color y composición de los grupos de estado. Cambiar colliders, raíces, IDs, capas o alcance requiere revisión de programación y nuevas pruebas.
- No se modificaron los OBJ, PNG ni materiales originales de Arte, ni la escena base ArroyoGreybox.

## Propiedad de archivos y entrega a QA
- Programación: Runtime/Interaction, World, Session, Player, Contracts; Editor/CoreSetup; Scenes/Integration; Packages y ProjectSettings.
- Arte: recursos bajo Art/PrototypeKit y documentos de Docs/Art en su propia rama; proponer prefabs/variantes visuales para revisión. Los cambios finales de la escena integrada los realiza programación.
- QA: documentos, matrices y pruebas en sus rutas acordadas. No editar el estado de gameplay para que una prueba apruebe. No reemplazar el plan original con la matriz propuesta sin conciliar IDs.
- Runtime/UI sigue reservado a Claude. Este HUD está en Runtime/Session como vista provisional explícita para hacer el corte verificable; se puede sustituir escuchando los contratos.
- Cada asistente mantiene su relevo. La integración de este ciclo no modifica los archivos originales de Arte ni los documentos de QA de Diego.

### Aceptación manual para Diego (pendiente hasta registrar ejecución)
1. Iniciar en 0/3: salida bloqueada, entorno seco, movimiento y cámara utilizables.
2. Acercarse a cada obstáculo desde la orilla: aparece [E]; alejarse: desaparece. Fuera de alcance o tras una pared no se retira.
3. Intentar retirar estando encima: se conserva el soporte. Bajar y retirar desde un costado seguro.
4. Completar en orden 3–1–2, pulsando E repetidamente: cuenta exactamente 0,1,2,3; la restauración ocurre una sola vez.
5. Intentar cruzar antes de 3/3, caminando y saltando: no completar. Tras 3/3, observar agua/vegetación, cruzar y ver victoria/cursor libre.
6. R o botón Reiniciar tras victoria y a mitad del puzle: estado inicial completo, personaje en spawn, controles recuperados. Repetir tres veces.
7. Caer antes/después de restaurar: vuelve a SafeRespawn y conserva el progreso que corresponde.
8. Probar captura, liberación y foco de cursor, cámara cerca de paredes y lectura del HUD en la PC del equipo; registrar FPS/resolución/hardware en la build.

## Incidencias anteriores
- I-01 mitigada sin modificar geometría: bloqueo de interacción encima, con prueba automatizada.
- I-02: cauce entre rampas sigue requiriendo salto para salir. No se aplicaron rampas propuestas; el salto actual permite recuperación. Validación de facilidad pendiente.
- I-03: tronco opcional sigue subible caminando; se considera decorado en este corte, no tutorial obligatorio de salto.
- I-04: retorno al SafeRespawn inicial se conserva; no se introducen checkpoints por tramo.
- I-05: posible cámara dentro de cápsula junto a paredes sigue pendiente de revisión visual manual; no se declara corregida.

## Herramientas reproducibles
Con Unity cerrado para este proyecto, ejecutar una sola operación cada vez:
- Integración idempotente: `-batchmode -nographics -projectPath "<repo>/Game" -executeMethod Bosque.CoreSetup.ArroyoVerification.SetupBatch -logFile "<log>"`.
- Pruebas: `-batchmode -nographics -projectPath "<repo>/Game" -runTests -testPlatform EditMode -testResults "<resultados.xml>" -logFile "<log>"`. Incluye una prueba UnityTest que entra/sale de Play Mode.
- Capturas renderizadas: `-batchmode -projectPath "<repo>/Game" -executeMethod Bosque.CoreSetup.ArroyoV2Preview.CaptureBatch -logFile "<log>"` (requiere dispositivo gráfico; no usar -nographics).
- Build Windows: `-batchmode -projectPath "<repo>/Game" -executeMethod Bosque.CoreSetup.ArroyoBuild.BuildWindowsBatch -logFile "<log>"`.

Usar Unity 6000.3.22f1. Los métodos de herramientas cierran el proceso y devuelven error si fallan. No añadir -quit al runner de pruebas. La integración abre/actualiza la escena integrada; guardar trabajo manual antes de ejecutarla. La verificación de idempotencia no debe usarse sobre una escena con una partida guardada accidentalmente en estado intermedio.
