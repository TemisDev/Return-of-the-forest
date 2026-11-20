# Dirección artística V4 — pulido del Arroyo dormido

## Meta visual
Conservar low poly de tonos opacos, paisaje ficticio inspirado en entorno andino, lectura clara del camino y del puzle ecológico. Una escena limpia y legible vale más que añadir muchos props.

## Jerarquía
1. Jugador y obstáculos: siempre distinguibles del suelo (forma, silueta y contraste de valor).
2. Camino y salida: reconocibles desde tercera persona, sin que el decorado indique rutas falsas.
3. Restauración: cambio evidente en vegetación, ribera y agua, sin depender solamente de un filtro de color.
4. Atmosfera: apoyo mediante detalles decorativos, sin obstaculizar salto, cámara ni interacción E.

## Paleta
Referencia de V2, preservada; véase `PALETA-REFERENCIA-V4.png`.
Degradado: suelo #786F5C, follaje #777A4F, agua #68736C.
Restaurado: suelo #6C8156, follaje #477B58, agua #4A93A1.
Apoyo: piedra #8B9089, madera #826346, HUD claro #F3ECD8.

## Uso de recursos V4
- `RiverStoneCluster_V4`: bordes del cauce / puntos de apoyo visual. Solo decorativo.
- `ReedFan_V4`: ribera restaurada, mantener fuera de raycasts de interacción si se agregan colliders externos.
- `MossyBranch_V4`: narrativa ambiental sin bloquear paso.
- `FoamRibbon_V4`: plano decorativo para superficie restaurada; requerirá revisar el alpha, altura respecto al agua y posible z-fighting.
- Texturas de banco seco/húmedo, piedra, madera y musgo: detalles de superficie de materiales mate, no pinturas realistas.
- Iconos: propuestas de recursos, no UI integrada; Codex debe vincularlos a elementos UI existentes.

## Contrato estricto
No renombrar `Obstacle_01..03`, `DegradedVisuals`, `RestoredVisuals`, `PlayerSpawn`, `SafeRespawn`, `ExitMarker`.
No crear un segundo sistema de progreso, restauración o reinicio.
No añadir colliders al kit decorativo; los colliders de gameplay pertenecen a programación.
No mover/renombrar recursos V1–V3 ni sus `.meta`.

## Capturas de aceptación que debe producir una persona desde Unity
- Misma cámara/posición antes y después de restauración.
- Materiales y luz sin sombras extrañas o magenta.
- Obstáculos y salida visibles con la cámara real.
- Vista de Profiler o Stats y hardware utilizado, si se dispone.
No sustituir con el muestrario de este paquete.
