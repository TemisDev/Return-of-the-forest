# Guía artística MVP — Bosque de regreso

**Estado:** propuesta original elaborada a partir del resumen de proyecto del 13-11-2026; pendiente de cotejo con GDD, dirección visual y escena Unity actuales.

## Propósito

Dar una identidad low poly mate al **Arroyo dormido**, con una lectura clara del antes (degradado) y después (restaurado). Referencia conceptual: bosque ficticio inspirado en los Andes, no simulación científica. No trasladar recursos visuales a la escena integrada sin aprobación de programación.

## Paleta y lógica visual

| Uso | Color | Hex |
|---|---|---|
| soil_degraded | Muestra plana | `#796E60` |
| soil_restored | Muestra plana | `#766B52` |
| water_degraded | Muestra plana | `#596A62` |
| water_restored | Muestra plana | `#4D9FAD` |
| leaf_degraded | Muestra plana | `#8C8064` |
| leaf_restored | Muestra plana | `#5F916A` |
| wood | Muestra plana | `#6D5540` |
| rock | Muestra plana | `#85847F` |
| ui_cream | Muestra plana | `#F2EBD9` |
| ui_dark | Muestra plana | `#263B3A` |


La versión degradada usa bajo contraste cromático y vegetación seca; la recuperada incrementa saturación de agua y follaje. Mantener una diferencia perceptible también por formas y disposición, no solo por color.

## Recursos entregados

- Cinco meshes `.obj` originales: roca, tocón, tronco de árbol, copa de pino, mata de hierba. Son **proxies** de arte y no una escena final.
- Diez texturas PNG de color plano de 256×256 para crear materiales URP lit/opacos.
- Tres iconos transparentes de UI: interact, progress, restored (128×128).
- `paleta-preview.png` para revisión rápida.

## Instrucciones de integración

1. Copiar `Game/Assets/_Bosque/Art/PrototypeKit/` a la misma ruta del repositorio.
2. Abrir Unity 6000.3.22f1; permitir importación y generación de `.meta`. No copiar `.meta` de otra versión ni cambiar GUID de assets existentes.
3. Crear materiales URP/Lit (Surface Opaque), usando las texturas como Base Map o sus colores equivalentes.
4. Armar prefabs en una carpeta artística acordada. Mantener los colliders y triggers de gameplay existentes; los OBJ son **visuales**, no colisiones.
5. Para un árbol modular, colocar la copa por encima del tronco y agrupar en prefab.
6. El responsable de programación integrará las instancias en la escena `Scenes/Integration/ArroyoPlayable.unity`, sin regenerar sobre gameplay existente.

## Prioridad para el cierre

**P0:** arroyo degradado/restaurado, obstáculos legibles, indicadores de interacción y progreso.
**P1:** vegetación y rocas modulares, iluminación coherente, señales ambientales.
**P2:** protagonista nueva, fauna decorativa, partículas, variantes extensas.

## Criterios de aceptación

- Los recursos importan sin errores en Unity y sin materiales rosados.
- No hay cambios a `Runtime/`, `Editor/CoreSetup/`, `Scenes/Integration/`, `ProjectSettings/` o `Packages/`.
- La cámara no atraviesa recursos añadidos en el recorrido; el personaje conserva el movimiento.
- El contraste antes/después se entiende incluso sin la UI.
- Los recursos visuales no se presentan como funcionalidad de restauración implementada.

## Propiedad y licencia

Geometría, texturas planas e iconos generados originalmente para este paquete. No contienen modelos de terceros. Se pueden adaptar en el repositorio del equipo.
