# Contratos del primer corte
Estado: T06, T08 y T09 implementados para el arroyo, 16 de noviembre de 2026. Contrato detallado: [INTEGRACION-ARROYO-V2.md](INTEGRACION-ARROYO-V2.md).

## Base
- Editor Unity 6000.3.22f1.
- URP e Input System según el manifiesto real del proyecto, que mantiene Codex.
- 1 unidad Unity representa 1 metro. Eje Y hacia arriba.
- Prototipo del arroyo con primitivas. Recursos de la presentación son referencia artística.

## Escena entregada por Claude
Ruta: Game/Assets/_Bosque/Levels/Arroyo/ArroyoGreybox.unity.
Herramienta de generación: Game/Assets/_Bosque/Editor/LevelTools/.
Raíz de escena: ArroyoGreybox.

Marcadores y grupos requeridos:
| Nombre | Finalidad |
|---|---|
| PlayerSpawn | Transform de inicio. Y coincide con la superficie segura del suelo; Codex aplica la altura del controlador |
| SafeRespawn | Transform de retorno tras caída |
| ExitMarker | Lugar de llegada; su bloqueo lógico se integra en T09 |
| Obstacle_01, Obstacle_02, Obstacle_03 | Tres obstáculos físicos con collider. Codex conectará sus componentes interactivos |
| DegradedVisuals | Visuales iniciales activos |
| RestoredVisuals | Visuales recuperados inicialmente inactivos |

Cada marcador debe ser único y estar bajo ArroyoGreybox. Evitar dependencias en tags o layers nuevos. No cambiar Physics o Input Settings para que el nivel funcione.

## Diseño y medidas propuestas
Recorrido orientativo: 25–40 metros. Paso útil mínimo aproximado: 2 metros. Obstáculos de salto iniciales bajos, ajustables tras T06.
No diseñar un salto obligatorio hasta medir las capacidades reales del controlador.
Sin cámara de gameplay ni personaje persistente en la escena de nivel; puede existir una cámara solo de vista previa, desactivada para integración.

## Movimiento implementado (T06)
Assembly: Bosque.Runtime, referencia Unity.InputSystem. Namespace: Bosque.Player.
- ThirdPersonMotor.SetRespawn(Transform): define el marcador de retorno.
- ThirdPersonMotor.Simulate(Vector2 input, bool jump, float yaw, float dt): movimiento relativo al giro horizontal de cámara, gravedad y salto solo en suelo.
- ThirdPersonCamera.Configure(Transform): fija objetivo. Yaw expone el giro horizontal; Step(Vector2 lookDelta, float dt) permite simular cámara para pruebas.
- ThirdPersonInput.Configure(ThirdPersonCamera): conecta teclado y ratón.
Velocidad inicial 4,5 m/s; salto configurado 1,1 m; gravedad -24 m/s². Controlador de 1,8 m con radio 0,35 m. Se usa SafeRespawn; los SafePoint adicionales no tienen lógica aún.
Escena integrada: Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity. Es una copia del nivel: Codex debe reintegrar futuros cambios explícitamente. El menú de preparación abre la copia existente sin sobrescribirla.
Escape libera el cursor; todavía no hay pausa de sesión. ExitMarker no implementa bloqueo de salida.

## Interacción y restauración (T08/T09)
InteractionTarget define Prompt, CanInteract(actor) y TryInteract(actor). PlayerInteractor selecciona una superficie válida a 2,5 m del pecho, con línea de visión, y revalida al pulsar E. RemovableObstacle impide retirar el soporte cuando el jugador está encima.
RestorationProgress mantiene IDs únicos retirados, progreso y estados restaurado/completado. ArroyoSession aplica el estado a objetos/visuales, barrera y controles. Tras tres obstáculos se abre la barrera; ocupar el volumen de llegada completa una sola vez. R reinicia el nivel sin recargar escena.
IDs: arroyo.log, arroyo.rocks y arroyo.branches. Eventos: Progress.Changed, Progress.Restored y ArroyoSession.StateChanged. Detalles, firmas, prefabs y criterios de QA en la guía de integración v2.

## UI pendiente
Existe un HUD provisional bajo Runtime/Session, sustituible por Runtime/UI. Puede leer ArroyoSession.Progress y escuchar StateChanged; el reinicio es ArroyoSession.Restart(). La pausa y reanudación siguen sin implementar y no tienen contrato de eventos. No añadir un segundo estado de sesión desde UI.
