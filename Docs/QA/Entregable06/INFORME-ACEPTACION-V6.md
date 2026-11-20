# Informe de aceptación — Arte V6 realista

**Decisión actual (20/11/2026, integración): INTEGRACIÓN TÉCNICA COMPLETA. APTA PARA M15, NO APTA TODAVÍA PARA FUSIONAR A `develop`.**

Para fusionar faltan tres cosas:
- la prueba manual M15 sobre la build integrada;
- una decisión sobre QA6-016, el presupuesto visual heredado de V3;
- la medición M13 en la PC de la presentación.

## Integración en ArroyoPlayable (20/11/2026)

| Dato | Valor |
|---|---|
| Rama | `feature/arroyo-v6-integration`: incluye `develop` (arte V6 `42041da` y QA V6 `41b2bee`), las correcciones P1 `afcbe4d` y RC1 (`a7984f3`) |
| Build | `Builds/Windows/V6/BosqueDeRegreso.exe`, commit `2cc512c`, 0 errores y 0 avisos, 329 MB ([build-info](EVIDENCIAS/integracion/build-info-V6.json)) |
| Escena | `ArroyoPlayable` vestida con `ArroyoArtV6Dresser.DressPlayableBatch` |

Resultados automatizados (no sustituyen a M15):

| Prueba | Resultado |
|---|---|
| Sonda V6 integrada (`-qa6-integrada`) | **43 OK, 0 AVISO, 0 FALLA.** C09: ArroyoPlayable vestida tiene 0 diferencias de colliders y componentes frente a la [línea base pre-V6](EVIDENCIAS/contrato-arroyoplayable-pre-v6.json) (sha `A4134AF6…`) |
| Sonda V6 sobre la escena de muestra | 43 OK, 0 AVISO, 0 FALLA |
| Verificador de builds (patrulla, 1080p) | `passed=True` en todas las ejecuciones: movimiento, salto, salida bloqueada, 3 retiradas, un solo efecto de restauración, salida y 3 reinicios |
| Pruebas EditMode | **37 de 38.** Falla `ActiveVisualBudgetStaysBoundedInBothStates`, que exige el presupuesto de V3 (menos de 180 renderers y 20 000 triángulos); V6 tiene 590 y 2,0 M. **No se cambió el criterio** (QA6-016) |
| HUD sobre el fondo V6 | Legible en todos los estados ([capturas reales `hud_*.jpg`](EVIDENCIAS/integracion/)). Los botones de la pantalla final usan el estilo por defecto (QA6-017) |
| Git LFS | `*.hdr` está en LFS y los 2 HDRI son punteros cuyos objetos tiene el servidor (QA6-011 cerrada) |

Correcciones de esta integración (commit `122b504`):

- **QA6-001:** la maraña ya no sobresale por delante de la barrera.
- **QA6-002:** los árboles de la orilla se orientan sin madera en el paso.
- **QA6-003:** los tocones pasan a lo alto del muro.
- **QA6-004:** el tronco termina en el borde de la orilla.
- **QA6-009:** queda un solo efecto de restauración. `RestorationFeedback` reproduce una ola V6 local y el sistema V3 deja de verse.
- K03 en degradado baja del 1,8 % al 0,3 %.

**Cambios en la sonda**, para que QA los revise:

- Nuevo modo `-qa6-integrada`, que prueba `ArroyoPlayable` en lugar de la escena de muestra.
- C02–C04 y C09 comparan ahora contra una línea base exportada antes de vestir la escena (`ExportBaselineBatch`), en lugar de contra el hash de un archivo que la integración cambia a propósito.
- No se quitó ni se relajó ninguna comprobación.

**Rendimiento** ([RENDIMIENTO-V6.csv](RENDIMIENTO-V6.csv): 4 ejecuciones seguidas con el mismo verificador, 30 s por estado; el equipo se calienta entre ejecuciones):

| Build | Degradado | Restaurado | Primer frame |
|---|---|---|---|
| V6 `2cc512c` (1.ª ejecución) | 84 FPS, p95 15,0 ms | 54 FPS, p95 24,6 ms | 3,45 s |
| V6 `38acc1d`, sin optimizar | 59 FPS, p95 20,1 ms | 39 FPS, p95 31,6 ms | 3,78 s |
| V6 `2cc512c` (3.ª ejecución, en caliente) | 66 FPS, p95 19,7 ms | 53 FPS, p95 22,8 ms | 3,76 s |
| RC1 `8838ff5` (referencia) | 219 FPS | 193 FPS | 3,66 s |

- **Qué se optimizó** (`2cc512c`):
  - Se conservan la mitad de los árboles del valle y dos tercios de los de la meseta.
  - La vegetación baja ya no proyecta sombra.
- **Reparto del coste**, medido con fases alternadas ([reparto](EVIDENCIAS/integracion/reparto-coste-restaurado.json), [candidatas](EVIDENCIAS/integracion/optimizaciones-candidatas.json)):
  - sombras ~30 %;
  - escala de render ~28 %;
  - árboles del fondo ~18 %;
  - vegetación ~16 %.
  Cambiar la calidad de la sombra suave solo da un 4–5 %, y forzar el LOD1 de los árboles no mejora nada.
- **Qué queda pendiente:** el estado restaurado sigue por encima de 16,67 ms en p95 (QA6-006). Lo siguiente sería reducir las cascadas de sombra o la escala de render, pero ambas cosas cambian el asset URP compartido y hay que acordarlas con Codex.

**QA6-005 (congelamiento inicial):**

- **Diagnóstico:** no es un congelamiento durante el juego.
  - El frame largo (2,7 s; antes 4,3 s) es el delta del primer frame e incluye la carga desde que arranca el proceso. Sale igual con DX11 y con DX12, así que no son los PSO de DX12.
  - El primer render real tarda entre 0,6 y 1,0 s, antes de que el jugador pueda moverse.
- **Medida:** el primer frame con contenido llega en 3,45–3,78 s, frente a 3,66 s de RC1 en la misma sesión.

> Decisión anterior, sobre el ZIP de Juan: no apto para integrar por QA6-013 y QA6-014, que se corrigieron después (ver la revalidación). Las secciones siguientes describen esas ejecuciones.

| Dato | Valor |
|---|---|
| Qué se probó | Arte V6 sin commit. ZIP de Juan `4C26AE3B…` y su manifiesto, 750 archivos idénticos. Escena `Art/V6/Showcase/ArroyoV6_Showcase.unity`. |
| Herramientas | Unity 6000.3.22f1 en modo batch con la sonda `Bosque.Acceptance.ArroyoQAV6Probe`. Build Windows de la escena de muestra con `-v6-perf`. |
| Equipo | i5-10300H, GTX 1650 4 GB, Windows 11, 1920×1080. Durante la última medición había otro editor de Unity abierto con otro proyecto. |
| Ejecutado por | Claude Code en el rol de QA. Todo es **automatizado**: no hay pulsaciones reales de teclado ni ratón, ni juicio humano. |
| Fecha | 20/11/2026 |

## Revalidación tras la corrección de las P1 (20/11/2026)

Rama `feature/arroyo-art-v6-fix-p1`, creada desde `develop` `d840df9`. Solo cambia el vestidor V6 (`ArroyoArtV6Dresser.cs`), la herramienta de capturas y esta sonda. Colliders, escenas de integración y gameplay no cambian (C01–C11 siguen OK).

| ID | Corrección | Resultado de la sonda |
|---|---|---|
| QA6-013 | El vestidor coloca `V6_LowLog_Optional` (tronco seco, igual en los dos estados) ajustado a la cápsula del tronco opcional: 3,50 × 0,40 × 0,40 m sobre 3,50 × 0,35 × 0,35 m. Capturas `*_08_tronco_opcional.png`. | C12 OK |
| QA6-014 | Cada árbol con collider se endereza y se desplaza hasta que su corteza queda centrada en la cápsula del tronco (desvío medido de 0,000 a 0,002 m en la base y a 3 m). `Tree_01`–`04` pasan al modelo B, de tronco recto, porque el A se bifurca antes de los 3 m del collider. La corteza mide 0,13–0,18 m de radio, dentro del collider de 0,20 m: el jugador se detiene a menos de 7 cm de la madera. | C12 OK: 0 colliders sin visual (antes 8) |

**Sonda: 43 OK, 0 AVISO, 0 FALLA.**

Cambios en la sonda (documentados para que QA los revise):

- **K03** ahora solo cuenta las celdas libres de colliders de juego. Antes también contaba celdas ocupadas por un tronco u obstáculo, donde el jugador no puede estar, y eso inflaba el aviso. Con la regla nueva: 1,8 % en degradado (21 de 1155 celdas: raíces de la barrera, tocones y ramas bajas, es decir, QA6-001 a QA6-003) y 0,3 % en restaurado.
- **V01** acepta cualquier número de capturas mientras sea al menos 7 por estado e igual en ambos estados. Antes exigía exactamente 14, y la herramienta de capturas añade ahora la vista 08.

QA6-001 a QA6-004 (P2 y P3) siguen abiertas.

## Resultado de la primera ejecución de la sonda (43 comprobaciones)

**41 OK, 1 AVISO, 1 FALLA.** Detalle en [EVIDENCIAS/sonda-qa-v6.csv](EVIDENCIAS/sonda-qa-v6.csv) y [EVIDENCIAS/sonda-qa-v6.json](EVIDENCIAS/sonda-qa-v6.json).

### Lo que está bien

- **Contrato intacto:**
  - `ArroyoPlayable` y `ArroyoGreybox` conservan sus hashes.
  - Los 26 colliders de integración son idénticos en ruta, tipo, transform y forma.
  - V6 no añade colliders.
  - Se conservan los 11 componentes de gameplay, con 0 scripts perdidos.
  - Los marcadores son únicos, los IDs son `arroyo.log`, `arroyo.rocks` y `arroyo.branches`, y el estado inicial es correcto.
- **Flujo de juego con el decorado V6 presente:**
  - recorrido hasta la barrera, y la barrera bloquea incluso saltando;
  - retirada de los tres obstáculos desde la orilla (el decorado no tapa la línea de visión);
  - cada pulsación repetida de E cuenta una sola vez;
  - con 3/3 cambian los visuales, aparece el agua y desaparece la maraña;
  - la salida se completa una vez y R restablece todo;
  - la caída por el borde este devuelve al punto de reaparición.
- **Cámara:** en 2736 posiciones por estado (misma lógica que `ThirdPersonCamera`), solo un 0,3 % queda dentro de roca o tronco V6. La línea de vista queda cortada en un 0,7 % por roca y un 1,4 % por vegetación.
- **Recursos:**
  - 5 shaders sin errores.
  - 91 prefabs sin collider, material nulo ni script perdido, todos con LOD.
  - 111 texturas bien importadas.
  - 19 sistemas de partículas acotados.
  - 14 capturas sin magenta (que indicaría un shader roto) ni imágenes vacías.
- **Legibilidad:** el visual de cada obstáculo cubre el 100 % del ancho de su collider, y ningún collider sobresale por encima del visual (no hay pared invisible en el bloqueo).

### Lo que falla o requiere decisión

| ID | Sev. | Hallazgo |
|---|---|---|
| QA6-013 | **P1** | `LowLog_Optional` queda invisible: el vestidor oculta su visual y no pone otro, pero el collider (0,35 m) sigue en el camino, en X 1,75 y Z 30. |
| QA6-014 | **P1** | Árboles de la orilla (`Tree_05`, `06` y `07`): el tronco V6 no coincide con el collider. El jugador choca contra un poste invisible (0 % de su volumen está cubierto por la malla) y atraviesa el tronco visible. La causa es que el pivote del modelo está en el centro de su caja, no en el tronco. |
| QA6-001 | P2 | Las raíces de la barrera ocupan 1 m delante de su collider (Z 33,5–34,5) y el jugador se mete visualmente en ellas. |
| QA6-002 | P2 | Las ramas bajas de esos árboles son atravesables cerca del inicio. |
| QA6-005 | P2 | Pico de **4,3 s** en los primeros segundos de la build (otra ejecución tuvo 1,5 s). Probablemente es la compilación de shaders en DirectX 12. |
| QA6-006 | P2 | Estado restaurado en frío a 78 FPS (p95 15,4 ms), pero al calentarse el equipo baja a 48–69 FPS (p95 17,7–26 ms). Falta medir en la PC de la presentación. |
| QA6-011 | P2 | Hay que verificar en el PR de arte que `*.hdr` va por Git LFS. |
| Otras P3 | P3 | Tocones atravesables, extremo del tronco en el punto de interacción, charcos poco naturales, línea del agua, doble efecto de restauración, tamaño de la build y protagonista cápsula. |

Lista completa en [INCIDENCIAS-V6.csv](INCIDENCIAS-V6.csv). Revalidación de I-01 a I-05 en [REVALIDACION-HISTORICA-V6.csv](REVALIDACION-HISTORICA-V6.csv): I-03 **empeora** (ver QA6-013) e I-05 mejora frente a lo esperado.

## Rendimiento

Ver [RENDIMIENTO-V6.csv](RENDIMIENTO-V6.csv): 6 ejecuciones y 18 fases. El objetivo de 60 FPS en p95 a 1080p **se cumple en frío y no se garantiza en caliente**. No se aprueba hasta medir en la PC de la presentación (caso M13).

## Pendiente manual (19 casos: NO EJECUTADO)

M01–M15 y U01–U04, en [MATRIZ-QA-V6.csv](MATRIZ-QA-V6.csv) y con hoja de campo en [FORMULARIO-PRUEBA-MANUAL-V6.md](FORMULARIO-PRUEBA-MANUAL-V6.md). Son especialmente importantes:

- controles reales;
- legibilidad del HUD sobre el fondo nuevo (las capturas automáticas no muestran el HUD);
- confort visual con niebla y grano;
- la prueba con una persona ajena al proyecto.

## Condiciones para pasar a integración

1. ~~Corregir QA6-013 y QA6-014 en el arte~~ (hecho, pendiente de comprobar a mano). Se recomienda corregir también QA6-001 a QA6-004, que salen de la misma revisión.
2. ~~Volver a ejecutar la sonda: **0 FALLA** y el aviso K03 por debajo del 2 %~~ (hecho: 43 OK).
3. Ejecutar M01 a M14 en la escena de muestra (casos manuales en editor y build).
4. Medir M13 en la PC de la presentación.

Después de integrar en `ArroyoPlayable`, ejecutar M15 sobre la build integrada, identificada por su SHA.
