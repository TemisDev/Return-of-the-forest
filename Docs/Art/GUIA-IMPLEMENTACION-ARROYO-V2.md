# Dirección técnica de arte — Arroyo dormido, Entregable 02

## Objetivo
Recursos decorativos low poly reutilizables con dos lecturas ambientales (degradado/restaurado), manteniendo siluetas fáciles de reconocer, colores mates y legibilidad de gameplay.

## Paleta propuesta
| Uso | HEX | Nota |
|---|---|---|
| Suelo degradado | `#786F5C` | opaco / desaturado |
| Suelo restaurado | `#6C8156` | verde apagado |
| Follaje degradado | `#777A4F` | amarillento |
| Follaje restaurado | `#477B58` | verde medio |
| Agua degradada | `#68736C` | gris verdoso |
| Agua restaurada | `#4A93A1` | azul petróleo mate |
| Roca | `#8B9089` | neutro |
| Madera | `#826346` | cálido |

Evitar bloom excesivo, realismo fotográfico y colores saturados. Dar lectura visual mediante forma y color, no mediante partículas abundantes.

## Recursos entregados
- `RiverReeds_01.obj`: juncos estilizados para riberas, decorativos; ~1 unidad de alto.
- `ShrubCluster_01.obj`: arbusto bajo estilizado; decorativo.
- `reeds_degraded.png` / `reeds_restored.png`: texturas de apoyo; no sustituyen los materiales de follaje.
- Herramienta de Editor para materiales URP y prefabs a partir del kit anterior y de estos OBJ.

## Reglas de integración
- Los obstáculos 01/02/03, sus colliders y los grupos `DegradedVisuals` / `RestoredVisuals` pertenecen al contrato del nivel. **No renombrarlos.**
- Nunca activar/restaurar estados desde el arte: la lógica de Codex determina cuándo mostrar cada conjunto.
- Los prefabs de vegetación **no llevan colliders**. Colocar lejos de trayectorias de salto y de la cámara.
- La herramienta no inserta objetos en `ArroyoPlayable.unity`; la escena final la integra Codex.
- Toda importación debe preservar GUIDs de `.meta` generados por Unity.

## Pendientes para aceptar entrega
- [ ] Unity importa OBJ sin errores
- [ ] Menú de generación ejecutado sin errores
- [ ] Materiales URP renderizan sin magenta
- [ ] Prefabs contienen el mesh correcto y escala aceptable
- [ ] Estado degradado vs. restaurado distinguible en una escena de prueba
- [ ] Ningún recurso altera escena integrada ni sistema de gameplay
- [ ] Capturas reales anexadas al PR
