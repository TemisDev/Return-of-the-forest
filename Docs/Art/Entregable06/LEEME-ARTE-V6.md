# Arte V6 realista — Arroyo dormido

Fecha: 20 de noviembre de 2026. Preparado con Claude Code para Juan (arte), con revisión posterior de Diego (QA) e integración con programación.
Estado: **recursos y escena de muestra listos y verificados en Unity por lotes. No integrado todavía en `ArroyoPlayable`. Sin aprobación humana de arte.**

![Antes y después](ANTES-DESPUES-RC1-V6.png)

Izquierda: RC1 (`8838ff5`). Derecha: V6. Arriba, estado degradado; abajo, restaurado. Son capturas reales de Unity 6000.3.22f1 con URP y post-proceso, no arte conceptual.

## Cómo verlo

1. Abrir Unity con `Game/` y la escena **`Assets/_Bosque/Art/V6/Showcase/ArroyoV6_Showcase.unity`**.
2. Pulsar Play. Es una **copia jugable** de `ArroyoPlayable` vestida con V6: WASD, E para retirar, R para reiniciar. Al retirar los tres bloqueos, el cielo, la niebla, la luz y el post-proceso pasan de degradado a restaurado en unos 3,5 s, con una ola de partículas.
3. Para fotografiar sin jugar, en el editor: activar `RestoredVisuals` y desactivar `DegradedVisuals` y los tres `Obstacle_0X`.

La escena de integración de Codex (`Scenes/Integration/ArroyoPlayable.unity`) **no se modificó**: su SHA256 sigue siendo `a4134af6…7b84`. Tampoco cambiaron `ProjectSettings`, `Packages` ni `Assets/Settings`.

## Qué incluye (`Game/Assets/_Bosque/Art/V6`, unos 150 MB)

| Carpeta | Contenido |
|---|---|
| `Models/` | 63 FBX: 13 rocas con musgo, 2 peñascos secos, tronco muerto, 3 ramas secas, tocón, 3 raíces, 4 helechos, 16 matas de hierba, 4 arbustos, 10 flores y 2 árboles con copa y LOD. Todos con escala real en metros y pivote en la base. |
| `Textures/` | 10 suelos PBR 2K (`Ground/`), texturas de props, 3 racimos de hojas (`Foliage/`), 9 texturas de partículas (`FX/`) y mapas del agua (`Water/`). |
| `Shaders/` | `Bosque/V6/Foliage`: recorte alfa, doble cara, translucidez y viento por vértice. `TerrainTriplanar`: suelo arriba y roca en paredes, humedad junto al agua; no depende de las UV, así que funciona sobre los bloques del nivel. `Water`: corriente, refracción, cáusticas, reflejo y espuma, con modo charco. `Particle`: partículas suaves que reciben la luz de la escena. `SkyBlend`: mezcla de dos HDRI. |
| `Materials/` | 49 materiales: props URP Lit, vegetación, 11 suelos triplanares por estado, agua corriente y estancada, cielo y efectos. |
| `Prefabs/` | 91 visuales `VIS_V6_*`, **sin colliders**, con LOD o corte por distancia. Hay variantes secas de la hierba, árboles verdes y muertos, y rocas de acantilado por estado. |
| `Particles/` | 14 sistemas: polvo en suspensión, ráfagas de polvo, neblina del arroyo, salpicaduras con gotas, ondas, polen, luciérnagas, hojas que caen (verdes y secas), mariposas, brillo del arco, indicador del obstáculo, ráfaga al retirar un obstáculo y ola de restauración. |
| `PostFX/` | `VP_V6_Degraded` (desaturado, cálido, viñeta y grano) y `VP_V6_Restored` (vivo, bloom suave). Ambos con tonemapping ACES. |
| `Sky/` | 2 HDRI 2K: nublado (degradado) y parcialmente nublado (restaurado). |
| `Runtime/` | `BosqueV6Ambience` mezcla cielo, niebla, sol y post-proceso, y lanza la ola de partículas. **Solo lee** si `RestoredVisuals` está activo. `BosqueV6RemovalBurst` lanza polvo y escombros cuando la sesión desactiva un obstáculo. `BosqueV6PerfProbe` es una medición que solo se activa con `-v6-perf`. |
| `Editor/` | Generadores reproducibles (ver abajo). |

## Contrato respetado

- No se crean, mueven ni borran colliders, scripts de gameplay, marcadores ni IDs (`Obstacle_01..03`, `DegradedVisuals`, `RestoredVisuals`, `ExitMarker`, `PlayerSpawn`, `SafeRespawn`).
- Al vestir una escena solo se **desactivan MeshRenderers antiguos**; los colliders siguen igual. Los visuales nuevos cuelgan de:
  - `DegradedVisuals` y `RestoredVisuals`;
  - cada `Obstacle_0X`, para que desaparezcan al retirarlo;
  - `ExitBlocker/VisualMount`;
  - una raíz nueva `ArtV6`.
- No hay un segundo sistema de progreso, pausa ni reinicio. `BosqueV6Ambience` no escribe estado de juego ni toca `Time.timeScale`.
- Los recursos V1–V5 de Juan y sus `.meta` no se tocaron.

## Regenerar

Menú **Tools > Bosque > Arte > V6**:

1. **Generar materiales, prefabs y partículas**: configura los importadores y regenera todo lo que hay en `V6`.
2. **Crear escena de muestra (copia jugable)**: copia `ArroyoPlayable` a `Showcase/` y la viste.
3. **Vestir la escena abierta con arte V6**: es el paso de integración. Es idempotente y se puede repetir.
4. **Capturar escena de muestra**: genera las 14 capturas de `CAPTURAS/`.

En lote: `Unity.exe -batchmode -projectPath Game -executeMethod Bosque.ArtV6.Editor.ArroyoArtV6Capture.BuildAndCaptureBatch`.

El procesado en Blender 5.3 está en `pipeline/`:

| Script | Función |
|---|---|
| `dl.py` | Descarga con verificación MD5 |
| `process.py` | Reducción de mallas y texturas URP |
| `trees.py` | Árboles, LOD y racimos de hojas |
| `fx.py` | Texturas de partículas y agua |

## Integración propuesta (siguiente paso, con programación)

Ejecutar el paso 3 sobre `ArroyoPlayable` **cuando el usuario y Codex lo aprueben**. El paso:

- desactiva 160 MeshRenderers V1–V4;
- añade los visuales V6;
- activa el post-proceso y SMAA en `GameplayCamera`, y amplía su plano lejano a 250 m;
- orienta el `Sun` a (42°, 300°);
- añade `ArtV6/V6_Ambience`.

Después hay que:

- repetir las pruebas automáticas (38/38) y el recorrido;
- decidir si se mantiene `RestorationFeedback` (V3), que ahora se suma a la ola V6;
- añadir `*.hdr` a Git LFS en `.gitattributes`: hoy los dos HDRI (9 MB) entrarían como binario normal.

## Rendimiento medido

Build Windows de la escena de muestra, sin modo Development, a 1920×1080 y VSync 0. Equipo: i5-10300H y GTX 1650. Recorrido automático de 20 s por fase. Datos completos en `RENDIMIENTO-V6.json`.

| Fase | FPS medios | p95 |
|---|---:|---:|
| Degradado | 74–84 | 14,5–16,7 ms |
| Restaurado | 48–68 | 18–26 ms |

- El portátil se calentó tras horas de compilaciones: dentro de una misma ejecución el degradado bajó de 82 a 63 FPS. Hay que medir con el equipo en frío antes de decidir.
- Probar sombras más ligeras (2 cascadas a 30 m) **no mejoró** los resultados, así que las sombras no son el cuello de botella.
- Ya aplicado: LOD en árboles, corte por distancia de la vegetación pequeña y sin sombras fuera del recorrido. Sin estas medidas, el restaurado daba 58 FPS y el degradado tenía picos de 1,5 s.
- Si el restaurado no llega a 60 FPS en la PC de la presentación, estas son las palancas por orden de impacto:
  1. Menos hierba (`Vegetation()` en `ArroyoArtV6Dresser`).
  2. Render scale 0,85 en el asset URP (lo decide programación).
  3. Menos polen y neblina.
- La RC1 low-poly iba a 220–245 FPS. El cambio visual tiene un coste real.
- La build de muestra ocupa unos 328 MB, frente a los 104 MB de la RC1. Se puede reducir bajando las texturas de suelo a 1K si hace falta.

## Licencias

Todos los modelos, texturas fotográficas y HDRI vienen de [Poly Haven](https://polyhaven.com) con licencia **CC0 1.0**, que permite uso comercial sin atribución obligatoria. Aun así se acredita a los autores en `INVENTARIO-ARTE-V6.csv`, con origen, URL, triángulos antes y después, y proceso de cada recurso.

Las texturas de efectos y agua son procedurales y propias. Los racimos de hojas se compusieron por script a partir de hojas fotografiadas CC0. **No se usó IA generativa de imágenes.**

## Pendiente y limitaciones (no ocultar)

- **Protagonista:** sigue siendo la cápsula naranja y es lo que más rompe la inmersión. Falta modelo, rig y animaciones (andar, saltar, retirar). Es la prioridad de arte siguiente.
- **Audio:** falta por completo (agua, viento, pájaros, pasos). Pertenece a `audio-design`.
- **Charcos del degradado:** se leen como óvalos con borde de barro; son mejorables. **Agua restaurada:** deja una línea recta donde se junta con la orilla.
- La copa de los árboles usa tarjetas de hojas. A muy corta distancia se nota que son planos.
- **Primer frame en DirectX 12:** en la primera medición hubo un pico de 1,5 s al compilar shaders. No se repitió en las siguientes, pero conviene un precalentado de shaders en la build final.
- **Verificación:** todo es automatizado (compilación sin errores, 5 shaders sin errores, capturas reales y medición de FPS). **No hay prueba con teclado y ratón ni aprobación artística de Juan.** Diego debe revisar:
  - legibilidad de los tres obstáculos;
  - cámara junto a los muros con las losas de roca;
  - hierba que tape la vista;
  - mareo por niebla o partículas;
  - contraste del HUD sobre el nuevo fondo.
