# Relevo de Claude Code

# Zona 4 V12: ríos, brújula y minimapa (21/11/2026)

- **Rama:** `feature/valle-olvidado`. Sin fusionar.
- **Build:** `Builds/Windows/V12-Rios`.
- **Ríos y lagunas**, a partir del mapa de referencia del usuario:
  - **Mayu:** río principal de norte a sur, hondo; se cruza por 2 puentes o 4 vados.
  - **Chico:** afluente del oeste.
  - **Lagunas Alta, del Medio y Baja**, con el **río de la Salida** hacia el este.
  - **Bofedal** y **río del Sur** al suroeste.
- **Agua:** se vadea con freno; lo hondo bloquea con una pista. Turbia mientras el sector está sucio y clara al limpiarlo.
- **Modelo único:** `ValleWater`. El constructor talla el terreno y genera la malla; el juego lo usa para la profundidad, el sonido y el mapa.
- **Orientación:** brújula y minimapa (`Runtime/UI/NavigationViews.cs`).
  - Lo ubicado (a menos de 24 m o marcado con T) aparece en la brújula, el minimapa y el mapa.
  - El mapa muestra los nombres de ríos y lagunas, puentes y vados.
- **Motor:** `ThirdPersonMotor.MoveBlocked` es un delegado opcional que deshace el paso horizontal.
- **Pruebas:** PlayMode 48/48 y EditMode 38/38. Las pruebas manuales nuevas, Z4-M10..M13, están en el informe y **no se han ejecutado**.

# Zona 4: El valle olvidado (21/11/2026)

- **Rama:** `feature/valle-olvidado`, que incluye el Bloque 05. Sin fusionar.
- **Build:** `Builds/Windows/V11-Valle`; `-zona valle` entra directamente.
- **Juego:** valle de ~210 × 210 m con 4 sectores, 40 residuos de Poly Haven (CC0) y 16 troncos.
  - Bolsa de 8 espacios y separación en 4 contenedores.
  - Troncos que se apartan manteniendo E.
  - Rastreo (T), mapa (M) y medallas.
  - El final del juego pasa a esta zona.
- **Regenerar:** `Bosque.LevelEditor.ValleBuilder.BuildAllBatch`.
- **Pruebas:** PlayMode 45/45 y EditMode 38/38. FPS en la build: valle 53 y refugio 82 (GTX 1650, 1080p).
- **Informe y pruebas manuales Z4-M01..M09:** `Docs/Valle/INFORME-ZONA-4.md`.

# Bloque 05: presentación, menús e interfaz (21/11/2026)

- **Rama:** `feature/presentation-menus`, que sale de `main` `1d143d8`; commit `389a98b`. Sin fusionar.
- **Build:** `Builds/Windows/V10-Presentacion`. Arranca en el menú principal; `-zona claro` y `-zona refugio` siguen funcionando.
- **Contenido:**
  - menú principal con introducción, pausa (Esc) con opciones y controles, y créditos;
  - fundidos, rótulos de zona y HUD uGUI;
  - notas del cuaderno al completar cada zona y pantalla final.
- **Regenerar:** `Bosque.PresentationEditor.PresentationBuilder.BuildAllBatch`.
- **Pruebas:** PlayMode 34/34, EditMode 38/38 y verificador V10 `passed=True`.
- **Informe y pruebas manuales B5-M01..M08:** `Docs/Presentacion/INFORME-BLOQUE-05.md`.
- **Créditos:** editar en `Game/Assets/_Bosque/UI/Creditos.txt`; falta el nombre de quien programa.

# Bloque 04: Zona 3, "El refugio perdido", fauna y final (21/11/2026)

- **Rama:** `feature/character-animation-audio`, commit `5cc5a98`.
- **Build:** `Builds/Windows/V9-Zona3-Refugio`, con las tres zonas encadenadas. Para entrar directamente en la zona 3: `-zona refugio`.
- **Escena:** `Scenes/Zonas/RefugioPerdido.unity`, generada con `Bosque.LevelEditor.RefugioBuilder.BuildAllBatch`.
  - También construye el zorro (`FaunaBuilder`), integra la transición en el claro y registra las tres escenas.
- **Mecánicas:**
  - E recoge y coloca elementos (`RefugeItem`, `RefugeSpot`, `ItemCarrier`) y Q hace crecer el matorral.
  - El zorro de neblina recorre una ruta fija (`FaunaRoute`) con plano de cámara (`FaunaCinematic`).
  - Con las tres zonas aparece el final en `RefugioHud`.
- **Animal:** el modelo Fox de Khronos (CC0 + CC-BY 4.0, **requiere atribución en los créditos**), procesado con `Docs/Fauna/pipeline/preparar_zorro.py`.
- **Pruebas:** PlayMode 26/26 y EditMode 38/38. El verificador sobre V9 da `passed=True`.
- **Informe y pruebas manuales Z3-M01..M08:** `Docs/Zonas/Refugio/INFORME-ZONA-3.md`.

# Bloque 03: Zona 2, "El claro vacío" (21/11/2026)

- **Rama:** `feature/character-animation-audio`, commits `7855cdd` y `40b863c`.
- **Build:** `Builds/Windows/V8-Zona2-Claro`. Para entrar directamente en la zona 2: `-zona claro`.
- **Escena:** `Scenes/Zonas/ClaroVacio.unity`, generada con `Bosque.LevelEditor.ClaroBuilder.BuildAllBatch`, que además integra la transición en `ArroyoPlayable`.
- **Habilidad Q:** `Runtime/Abilities/GrowthAbility.cs`, con lechos `GrowthPoint`. El progreso entre zonas vive en `GameProgress`, solo en memoria.
- **Pruebas:** PlayMode 18/18, EditMode 38/38 y sonda 43 OK.
- **Informe y pruebas manuales:** `Docs/Zonas/Claro/INFORME-ZONA-2.md`.

# Bloque 02: personaje, animaciones y audio (20/11/2026)

- **Rama:** `feature/character-animation-audio`, que sale de la integración V6 `4c973e5`.
- **Build:** `Builds/Windows/V7-Character-Audio`.
- **Informe y pruebas manuales:** `Docs/Character/INFORME-BLOQUE-02.md`.
- **Regenerar:** `Bosque.CharacterEditor.GuardianaBuilder.BuildAllBatch`.
- **Pruebas:** PlayMode en `Tests/PlayMode` (9/9) y EditMode 38/38.
- **Cambios en el motor** (compatibles con lo anterior): eventos de solo lectura, Shift para caminar y margen de apoyo de 0,1 s.

# Bloque 01.2: integración del arte V6 en ArroyoPlayable (20/11/2026)

- **Rama:** `feature/arroyo-v6-integration` (sin fusionar). Contiene `develop` (arte V6 y QA V6), las correcciones P1 `afcbe4d` y RC1.
- **Commits del bloque:**
  - `122b504`: QA6-001 a QA6-004, efecto único y modo integrado de la sonda;
  - `38acc1d`: escena vestida y build V6;
  - `2cc512c`: rendimiento;
  - además, un commit de documentación.
- **Escena:** `ArroyoPlayable` vestida con `Bosque.ArtV6.Editor.ArroyoArtV6Dresser.DressPlayableBatch`. Colliders y componentes son idénticos a `EVIDENCIAS/contrato-arroyoplayable-pre-v6.json`.
- **Build:** `Tools/Build-ArroyoRC1.ps1 -Configuration V6` → `Builds/Windows/V6`. Se verifica con `Tools/Verify-ArroyoV4.ps1 -Executable Builds/Windows/V6/BosqueDeRegreso.exe -AllowMissingReleaseCounters`.
- **Sonda integrada:** `-executeMethod Bosque.Acceptance.ArroyoQAV6Probe.RunBatch -qa6-integrada`.
- **Reparto de rendimiento:** `BosqueV6Preview.exe -v6-perf -v6-perf-breakdown`.
- **Necesita a Codex:**
  - QA6-016: la prueba `ActiveVisualBudgetStaysBoundedInBothStates` aplica el presupuesto de V3; no se cambió el criterio.
  - QA6-006: para llegar a 60 FPS en restaurado haría falta tocar las cascadas de sombra o la escala de render del asset URP compartido.
- **Detalle:** `Docs/QA/Entregable06/INFORME-ACEPTACION-V6.md`, sección "Integración en ArroyoPlayable".

# C-02: revisión del recorrido con T06 integrado
Fecha: 12 de noviembre de 2026. Estado: terminado salvo las pruebas manuales, que quedan pendientes para Diego.

## Resumen
- **Pruebas manuales con teclado y ratón:** ninguna ejecutada. No tengo acceso interactivo a Play Mode. Los 22 casos M-01..M-22 de `Docs/QA/ARROYO-ACEPTACION.md` quedan **Pendientes** para Diego.
- **Comprobaciones automatizadas** sobre el controlador real de Codex: 22 OK, 0 FALLA y 9 informativas (sección R del documento de QA).
- **A-03 (conservación de contenido ajeno):** OK en batch, sobre una copia temporal que ya se eliminó.
- **Escenas:** la de integración y la de nivel no se modificaron. Sus hashes SHA256 son los mismos antes y después de todas las ejecuciones:
  - `ArroyoPlayable.unity` `39EE49F2B467668782E0CE53AC8CD52B4A8F92C26D74C725E6085D1039836ED0`
  - `ArroyoGreybox.unity` `617AD893A90A3784D633DDDCFDD1163545F9B2EF6B4FD0964A0A0D14A0F3BBD2`
  - **No hace falta reintegrar.**
- **Límites respetados:** no toqué Packages, ProjectSettings, Runtime/Player ni Scenes/Integration. No creé UI, interacción, recuperación, pausa ni reinicio. No hice operaciones Git.

## Archivos creados o modificados en C-02
- `Game/Assets/_Bosque/Tests/Acceptance/Editor/ArroyoTraversalProbe.cs` (nuevo): sonda automatizada. Abre ArroyoPlayable sin guardarla y controla `ThirdPersonMotor.Simulate` y `ThirdPersonCamera.Step`.
- `Game/Assets/_Bosque/Tests/Acceptance/Editor/ArroyoA03PreservationCheck.cs` (nuevo): A-03 sobre una copia temporal que borra al terminar.
- `.meta` de lo anterior y de la carpeta `Tests/Acceptance/Editor`, generados por Unity.
  - Estas clases se compilan en `Assembly-CSharp-Editor` (carpeta `Editor`, sin `.asmdef`).
  - Usan `Bosque.Runtime` y `Bosque.LevelTools`.
- `Game/Assets/_Bosque/Editor/LevelTools/ArroyoGreyboxGenerator.cs`: se añadió `Generate(string scenePath)` para generar sobre una copia de prueba.
  - Rechaza cualquier ruta bajo `Assets/_Bosque/Scenes/`, así que no puede escribir en la integración.
  - `Generate()` y el menú se comportan igual que antes.
- `Docs/QA/ARROYO-ACEPTACION.md`:
  - A-03 resuelta.
  - Nuevas secciones M (manual) y R (automatizada).
  - B-01..B-08 pasan a Pendiente porque T06 ya está listo.
  - Incidencias I-01..I-05.

## Cómo repetir
Ejecutar con el editor de Unity cerrado sobre Game/:
```
"C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe" -batchmode -projectPath "<repo>\Game" -executeMethod Bosque.Acceptance.ArroyoTraversalProbe.RunBatch -logFile <log>
"C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe" -batchmode -projectPath "<repo>\Game" -executeMethod Bosque.Acceptance.ArroyoA03PreservationCheck.RunBatch -logFile <log>
```
Los resultados aparecen en el log con las líneas `[C02]` y `[A03]`. Salida 0 si no hubo excepciones; la A-03 sale con 2 si alguna comprobación falla.

## Ejecuciones reales (Unity 6000.3.22f1, batch, 12-nov)
1. **Sonda, primera pasada:** 20 OK y 1 FALLA. La falla (R-06) era un error de la sonda: el personaje salía del cauce y la sonda seguía empujando hasta hacerlo caer por el borde abierto. Corregí la sonda; el nivel no tenía ese fallo.
2. **Sonda, segunda pasada:** 22 OK, 0 FALLA y 9 informativas. Salida 0 y sin errores de compilación.
3. **A-03:** correcta, salida 0. Se conservan:
   - Un objeto ajeno bajo Geometry.
   - Un `AudioSource` y un hijo añadidos a Obstacle_01.
   - Una raíz ajena en la escena.
   - Un objeto ajeno dentro de DegradedVisuals.
   - SafePoint_02 movido a otro padre: no se duplica y conserva su posición.

   El recuento pasa de 144 a 144 objetos y el validador da CORRECTA. Un ajuste manual de posición en Obstacle_01 se sobrescribe, que es el comportamiento documentado.

**Recorrido y alcance (medido, no jugado):**
- Spawn → Exit en 8.3 s sin saltos.
- Las rampas funcionan en ambos sentidos.
- Desde el borde de la orilla, la superficie de cada obstáculo queda a 0.72–0.85 m del pecho y su centro a 1.88 m en horizontal. **Los tres obstáculos son alcanzables desde la orilla.**
- Caída por el borde abierto: retorno en 0.54 s; cinco caídas seguidas, cinco retornos.
- Muros y barandilla bloquean.
- La cámara nunca queda dentro de geometría en las 504 orientaciones medidas.

**Qué no se comprobó:**
- Lectura real de teclado y ratón con Input System, y captura, liberación y foco del cursor (M-19..M-21).
- Sensación de control y de cámara, Play Mode real, rendimiento, build.
- Lo que se ve cuando la cámara se acerca al cuerpo (I-05).
- Mis capturas de referencia son las de Codex (`.dev-logs/arroyo-*.png`); no hice capturas nuevas.

## Incidencias reproducibles
El detalle está en `Docs/QA/ARROYO-ACEPTACION.md`, sección Incidencias.
- **I-01, media:** el jugador se sube encima de Obstacle_01 (+0.20 m) y Obstacle_03 (+0.30 m) y cruza el cauce sobre ellos. Para reproducirlo: en ArroyoPlayable, desde (2, 0, 10), caminar hacia -X. Cuando T08 retire un obstáculo con el jugador encima, este caerá a un tramo del cauce sin rampa.
- **I-02, baja:** entre los obstáculos (Z 10.4–17.3 y Z 18.7–25.4) solo se sale del cauce saltando, porque la pared mide 0.6 m y el escalón 0.3 m.
- **I-03, baja:** LowLog_Optional (0.35 m) se sube caminando: el collider de cápsula redondeado actúa como rampa.
- **I-04, baja, de Codex:** el retorno siempre va a SafeRespawn (Z 2). Retroceso de hasta unos 26 m.
- **I-05, baja, de Codex y pendiente de M-13:** junto a los muros sur y norte la cámara queda a 0.26 m del foco, dentro del radio del cuerpo (0.35 m).

## Ajustes de geometría propuestos (no aplicados)
No los apliqué para no desincronizar la escena de integración sin acuerdo. Son ajustes de mi generador: si Codex y Diego los aprueban, los implemento con una regeneración y Codex reintegra la copia.
1. **I-01, techo de obstáculos ≥ +0.45 m sobre la orilla** (escalón 0.3 m + 0.15 m de margen). Los centros en X no cambian, así que el alcance medido apenas varía; la superficie queda unos 0.1 m más cerca.
   - Obstacle_01: diámetro del tronco de 0.8 a 1.1 m. Raíz en Y -0.05, BoxCollider 2.9 × 1.1 × 1.1. Techo +0.50.
   - Obstacle_03: BoxCollider 2.8 × 1.1 × 1.2, raíz en Y -0.05, con una rama visual más alta. Techo +0.50.
   - Obstacle_02: BoxCollider 2.8 × 1.1 × 1.4, raíz en Y -0.05. Pasa de +0.40 a +0.50 para tener margen.
2. **I-02, dos rampas más:** `Ramp_Mid_A` en Z 14 y `Ramp_Mid_B` en Z 22, iguales a las actuales (2.5 m de ancho, 16.7°). Cada tramo del cauce tendría salida a pie, a 3.5 m o menos de cualquier punto. Alternativa descartada: reducir la profundidad a 0.3 m, porque el cauce se leería peor.
3. **I-03:** si Diego quiere que el tronco enseñe a saltar, cambiar a un BoxCollider de 0.5 m de alto y mantener el rodeo de 2 m por el este. Si no, dejarlo como decorado y quitarle "Optional" del nombre en la próxima regeneración.
4. **Barandilla (R-12, opcional):** se salta con un solo salto (1.06 m frente a 0.5 m). Si se quiere impedir, subirla a 1.2 m. Recomiendo no tocarla; el retorno funciona.

## Necesidades para Codex (C-02)
- Decidir si se aplican los ajustes 1 y 2. Si se aplican, reintegrar Obstacles y Geometry en ArroyoPlayable.
- T09: valorar el retorno por tramo con SafePoint_01..03 (I-04).
- Cámara: revisar I-05 cuando Diego ejecute M-13. Posibles soluciones: ocultar el cuerpo con la cámara a menos de 0.5 m o subir el pitch.

---

# Registro C-01
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
- No ejecuté la prueba A-03, que comprueba que se conserva contenido ajeno. *Resuelto en C-02: A-03 OK en batch.*
- No probé nada jugable: no existen controlador, interacción ni restauración. Todos los casos B de `Docs/QA/ARROYO-ACEPTACION.md` están marcados como `Bloqueada`.

## Incidencias
1. **`Game/ProjectSettings/SceneTemplateSettings.json`:** Unity lo crea al arrancar el editor sobre este proyecto. Apareció en mi primera ejecución, lo borré y volvió a aparecer en la segunda, que no creó ninguna escena nueva. Tiene contenido por defecto y no lo escribe mi código. *Resuelto: Codex decidió conservarlo (relevo de Codex, T06).*
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
