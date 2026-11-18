# Arte V3 — Arroyo dormido

**Objetivo:** pulir el escenario sin interferir con la lógica T08/T09 ni modificar la escena integrada.

## Entrega real
- 3 mallas OBJ originales, de bajo detalle: `RockCluster_V3`, `FernCluster_V3`, `Driftwood_V3`.
- 4 texturas raster de 256×256 (difusas/de referencia de materiales; *no* son materiales URP creados).
- 4 iconos PNG RGBA para interfaz (no se colocan automáticamente en el Canvas).
- 1 textura de partículas RGBA y una muestra de paleta de colores.

Los OBJ usan metros y eje Y vertical. Sus mallas son ilustrativas, sin colisiones ni rig. Los assets **no se han importado ni verificado dentro de Unity**.

## Integración segura
1. En la rama `feature/arroyo-art-v3`, copiar `Game/` y `Docs/` a la raíz del repositorio, sin eliminar las carpetas preexistentes.
2. Abrir Unity 6000.3.22f1 y comprobar importación, orientación, escala, normales y materiales URP.
3. Crear materiales desde el shader `Universal Render Pipeline/Lit` o el configurado en el proyecto; asignar las texturas pertinentes, ajustar color y transparencia si aplica.
4. Crear prefabs a partir de las mallas. Mantenerlos **solo como elementos visuales**: los colliders, los `Obstacle_01..03`, los eventos de restauración y las referencias serializadas siguen bajo responsabilidad de Codex.
5. Integrar visuales mediante Codex en `Scenes/Integration/ArroyoPlayable.unity`, sin modificar el greybox generador.
6. Repetir pruebas de cámara, interacción, caída y regeneración del estado tras integrar.
7. Incluir archivos `.meta` generados por Unity en el commit posterior a la importación.

## Guía de estados
- Degradado: menos saturación, ribera seca y agua turbia; mantener los obstáculos distinguibles.
- Restaurado: musgo, ribera verde y agua más limpia; preservar contraste del HUD.
- No sacrificar visibilidad por vegetación cerca de la cámara.

## Pendientes intencionados
- No se crean prefabs Unity ni materiales `.mat` por no disponer del proyecto abierto y sus GUID de referencia.
- No se crean escenas ni se modifican los scripts de interacción.
- La optimización y FPS deben medirse en una build real.
