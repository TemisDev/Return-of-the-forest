# Relevo de Claude Code
Fecha: 11 de noviembre de 2026.
Encargo: C-01 / T07, herramienta de editor y escena greybox del arroyo, más casos de aceptación para T10.

## Encargo y estado
- Herramienta escrita, compilada y ejecutada en Unity 6000.3.22f1 (modo batch). Escena generada y validada.
- Sin gameplay: no hay controlador, cámara jugable, interacción, restauración, GameManager ni UI.
- No se modificó Packages, ProjectSettings (ver incidencia 1), carpetas de Codex ni la rama Git. No hice commits.

## Archivos creados
Todos dentro de rutas de Claude:
- `Game/Assets/_Bosque/Editor/LevelTools/ArroyoGreyboxGenerator.cs`: menú, entrada batch y trazado del nivel.
- `Game/Assets/_Bosque/Editor/LevelTools/ArroyoGreyboxValidator.cs`: comprobación del contrato de escena.
- `Game/Assets/_Bosque/Editor/LevelTools/GreyboxBuilder.cs`: utilidades de buscar-o-crear (nunca destruye).
- `Game/Assets/_Bosque/Levels/Arroyo/ArroyoGreybox.unity`: generada por la herramienta mediante `EditorSceneManager`.
- `Game/Assets/_Bosque/Levels/Arroyo/GreyboxMaterials/AG_*.mat` (16 materiales URP Lit), generados con `AssetDatabase.CreateAsset`.
- Los `.meta` de todo lo anterior los generó Unity.
- `Docs/QA/ARROYO-ACEPTACION.md` y este archivo.

Las clases están en el ensamblado `Assembly-CSharp-Editor` porque cuelgan de una carpeta `Editor`. No añadí `.asmdef`. Namespace: `Bosque.LevelTools`.

## Cómo generar o abrir la escena
- Editor: `Tools > Bosque > Niveles > Arroyo - Generar o actualizar greybox`. Antes pide guardar la escena abierta. Al terminar valida y muestra el resultado.
- Solo validar: `Tools > Bosque > Niveles > Arroyo - Validar escena`.
- Batch: ejecutar con el editor de Unity cerrado sobre Game/.
  ```
  "C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe" -batchmode -projectPath "<repo>\Game" -executeMethod Bosque.LevelTools.ArroyoGreyboxGenerator.GenerateAndValidateBatch -logFile <ruta.log>
  ```
  Códigos de salida: 0 correcto, 1 excepción, 2 validación fallida. Si solo quieres validar sin modificar la escena, usa `...ArroyoGreyboxGenerator.ValidateBatch`.

Comportamiento de actualización:
- Busca cada objeto por nombre y actualiza su transform, malla, material y collider. Crea solo lo que falte.
- No destruye objetos ni componentes. Los componentes que Codex añada a `Obstacle_0x` se conservan.
- Si un marcador del contrato se mueve a otro padre bajo `ArroyoGreybox`, se reutiliza sin moverlo.
- Los valores que gestiona la herramienta (posiciones, tamaño del BoxCollider de los obstáculos y materiales `AG_*`) se sobrescriben al regenerar. Los ajustes manuales de esos objetos deben pasarse al generador.
- Si hay dos raíces `ArroyoGreybox`, aborta sin tocar nada.
- Los objetos que se retiren del código en el futuro no se borran de la escena; habrá que eliminarlos a mano.

## Dimensiones del nivel
1 unidad = 1 m. Eje Y hacia arriba. El recorrido avanza hacia +Z. La raíz está en el origen.

| Elemento | Medida |
|---|---|
| Orilla transitable | X -1..6 (7 m; 6.5 m útiles junto a la barandilla), Z -4..40. Suelo Y = 0 |
| Cauce seco | X -4..-1 (3 m de ancho), fondo Y = -0.6 |
| Muros | Oeste X -5.5..-4; sur Z -5.5..-4; norte Z 40..41.5. Altura Y -1..3 |
| Barandilla este | 0.5 × 0.5 m en Z -4..12 y Z 28..40. **Borde de caída abierto entre Z 12 y 28** |
| Rampas al cauce | Z 5 y Z 31.5, 2.5 m de ancho, pendiente ≈ 16.7° |
| PlayerSpawn | (3, 0, 0), mirando +Z |
| SafeRespawn | (3, 0, 2), mirando +Z |
| SafePoint_01..03 (propuesta) | (2, 0, 11), (1, 0, 20), (3, 0, 33) |
| ExitMarker | (3, 0, 36). Distancia desde PlayerSpawn: 36 m |
| Portal de salida | Pilares en X 0.5 y 5.5, Z 38.5. Hueco de 4.2 m × 2.5 m |
| Obstacle_01 (tronco) | Centro (-2.5, -0.2, 10). BoxCollider 2.9 × 0.8 × 0.8 |
| Obstacle_02 (rocas) | Centro (-2.5, -0.1, 18). BoxCollider 2.8 × 1.0 × 1.4 |
| Obstacle_03 (ramas) | Centro (-2.5, -0.15, 26). BoxCollider 2.8 × 0.9 × 1.2 |
| Tronco opcional | Z 30, X 0..3.5, 0.35 m de alto. Rodeable por el este con 2 m libres |

No hay saltos obligatorios. Desde el borde de la orilla (X ≈ -0.6) al centro de cada obstáculo hay unos 1.9 m. El extremo cercano del obstáculo queda a unos 0.5 m.

Jerarquía bajo `ArroyoGreybox`:
- `Geometry`: suelo, cauce, muros, barandillas, rampas, `LowLog_Optional` y `Trees/Tree_0x/Trunk`. Todo con collider.
- `Markers`: `PlayerSpawn`, `SafeRespawn` y `SafePoints/SafePoint_0x`.
- `Obstacles`: `Obstacle_0x`. La raíz de cada uno es un vacío de escala 1 con BoxCollider; los hijos `Visual_*` no tienen collider.
- `Exit`: `ExitMarker`, `ExitPad` (visual), pilares y dintel con collider.
- `DegradedVisuals` (activo): barro agrietado, hierba seca, arbustos secos, ramas desnudas y `ExitBrambles`.
- `RestoredVisuals` (inactivo): `Water`, hierba, arbustos mayores, flores (tallo y corola), copas y musgo del portal.
- `Lighting/Sun`: direccional, sombras suaves y ambiente trilight de escena.
- `PreviewCamera`: inactiva.
- `EditorHelpers`: tag `EditorOnly` (integrado en Unity, no es nuevo). Contiene una cápsula de 1.8 m sin collider.

Ningún objeto de los grupos visuales tiene collider: la física es igual en ambos estados y el validador lo comprueba.

## Pruebas realmente ejecutadas
1. **Compilación:** Unity 6000.3.22f1 en batch compiló los tres scripts sin errores `CS`.
2. **Primera generación** (`GenerateAndValidateBatch`): salida 0, 140 objetos creados y validación `CORRECTA (0 errores, 0 avisos)`. El validador comprobó:
   - Una sola raíz, con cada nombre del contrato una vez y ninguno fuera de ella.
   - DegradedVisuals activo y RestoredVisuals inactivo, sin anidar.
   - Grupos visuales sin colliders y obstáculos con collider sólido activo.
   - PlayerSpawn, SafeRespawn, ExitMarker y los SafePoints sobre `Bank` a 0.00 m de la superficie, por raycast, con 2 m libres por encima.
   - Recorrido de 36.0 m.
   - Ninguna cámara activa, todos los renderers con URP Lit y ningún script perdido.
3. **Segunda generación sobre la escena existente:** salida 0, 0 creados, 142 actualizados. La escena sigue con 141 objetos, sin duplicados, y la validación sigue correcta.

**No ejecutado:**
- No abrí la escena en el editor con interfaz, así que no la revisé visualmente: no comprobé materiales en pantalla, iluminación ni legibilidad de los obstáculos.
- No probé los menús interactivos.
- No ejecuté la prueba A-03, que comprueba que se conserva contenido ajeno. El código está escrito para ello, pero no lo verifiqué.
- No probé nada jugable: no existen controlador, interacción ni restauración. Todos los casos B de `Docs/QA/ARROYO-ACEPTACION.md` están marcados como `Bloqueada`.

## Incidencias
1. **`Game/ProjectSettings/SceneTemplateSettings.json`:** Unity lo crea al arrancar el editor sobre este proyecto. Apareció en mi primera ejecución, lo borré y volvió a aparecer en la segunda, que no creó ninguna escena nueva. Tiene contenido por defecto y no lo escribe mi código. Ahora existe sin rastrear. **Codex decide si versionarlo o borrarlo**; yo no vuelvo a tocarlo.
2. La primera ejecución batch tardó varios minutos por la importación inicial; la segunda fue mucho más rápida. No hubo errores.
3. Las zarzas del portal son solo visuales y no bloquean físicamente. El bloqueo de la salida antes de restaurar debe implementarlo T09.

## Necesidades de integración (para Codex)
- **Carga de la escena:** cargar `ArroyoGreybox.unity` aditiva o como escena de nivel desde la integración. **No la añadí a Build Settings.**
- **Luz duplicada:** `Lighting/Sun` y los ajustes de ambiente están en la escena. Si la integración ya trae una luz direccional, desactiva `ArroyoGreybox/Lighting`. Cargada de forma aditiva, la escena activa decide `RenderSettings`.
- **Caídas:** no hay volumen de caída. Se sugiere detectar la caída cuando Y < -5. Debajo del borde este no hay nada.
- **Alcance de interacción (T08):** el jugador en la orilla queda a unos 1.9 m del centro de cada obstáculo y a 0.5–1 m de su extremo. Si el alcance se mide al centro del transform, conviene que sea de 2.5 m o más.
- **Componentes en obstáculos:** se pueden añadir componentes a `Obstacle_0x` sin que la regeneración los elimine. Si la retirada del obstáculo desactiva su GameObject, sus visuales hijos desaparecen con él.
- **Altura del controlador:** el controlador debe colocarse con su base en Y = 0 sobre PlayerSpawn, como dice el contrato.

## Cambios de contrato propuestos
- **Puntos seguros por tramo:** `SafePoint_01..03` existen como propuesta para hacer retornos por tramo. El contrato solo exige `SafeRespawn`, que sigue siendo único. Si T09 los usa, conviene añadirlos a `Docs/CONTRATOS.md`; si no, se ignoran.
- **Barrera física de salida:** si Codex quiere una barrera física de salida gestionada por T09, propongo un objeto de contrato `ExitBlocker` con collider fuera de los grupos visuales. No lo he creado para no adelantar lógica de T09.
