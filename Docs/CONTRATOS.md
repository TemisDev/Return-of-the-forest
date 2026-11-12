# Contratos del primer corte
Estado: acuerdo inicial de integración, 11 de noviembre de 2026. No describe sistemas de gameplay ya implementados.

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

## Interacción pendiente
Codex definirá el componente/contrato de interacción y su alcance durante T08. Claude no debe suponer nombres de métodos o eventos.
La recuperación debe ser única por zona y permitir reiniciar, pero su implementación corresponde a Codex en T09.

## UI pendiente
Antes de implementar Runtime/UI, Codex debe publicar:
- Lectura de objetivo y progreso.
- Eventos al actualizar objetivo, recuperar hábitat y cambiar pausa.
- Comandos de pausa, reanudación y reinicio.
Estos son requisitos, no firmas de C# acordadas todavía. Claude presenta una propuesta si necesita otro dato.
