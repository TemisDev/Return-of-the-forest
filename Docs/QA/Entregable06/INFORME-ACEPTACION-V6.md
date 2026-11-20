# Informe de aceptación — Arte V6 realista

**Decisión actual: NO APTO PARA INTEGRAR TODAVÍA.** Hay 2 incidencias P1 de colisión invisible que se corrigen en arte. Después de corregirlas, el arte V6 queda **apto para prueba manual**. La aceptación final requiere los casos manuales y la prueba de UX.

| Dato | Valor |
|---|---|
| Qué se probó | Arte V6 sin commit. ZIP de Juan `4C26AE3B…` y su manifiesto, 750 archivos idénticos. Escena `Art/V6/Showcase/ArroyoV6_Showcase.unity`. |
| Herramientas | Unity 6000.3.22f1 en modo batch con la sonda `Bosque.Acceptance.ArroyoQAV6Probe`. Build Windows de la escena de muestra con `-v6-perf`. |
| Equipo | i5-10300H, GTX 1650 4 GB, Windows 11, 1920×1080. Durante la última medición había otro editor de Unity abierto con otro proyecto. |
| Ejecutado por | Claude Code en el rol de QA. Todo es **automatizado**: no hay pulsaciones reales de teclado ni ratón, ni juicio humano. |
| Fecha | 20/11/2026 |

## Resultado de la sonda (43 comprobaciones)

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

1. Corregir QA6-013 y QA6-014 en el arte. Se recomienda corregir también QA6-001 a QA6-004, que salen de la misma revisión.
2. Volver a ejecutar la sonda: **0 FALLA** y el aviso K03 por debajo del 2 %.
3. Ejecutar M01 a M14 en la escena de muestra (casos manuales en editor y build).
4. Medir M13 en la PC de la presentación.

Después de integrar en `ArroyoPlayable`, ejecutar M15 sobre la build integrada, identificada por su SHA.
