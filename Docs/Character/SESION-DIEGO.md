# Sesión manual de Diego — preparada, no ejecutada

20/11/2026. El usuario confirmó que esta es la PC de presentación y que Diego todavía no puede realizar la sesión. Ningún caso humano queda aprobado con esta preparación.

## Versiones que se deben probar

| Sesión | Carpeta de build | Commit de código compilado | Alcance |
|---|---|---|---|
| V6 | Builds/Windows/V6 | `7b94a1da145563104d12b92c0ebe4a243bc102a7` | M15 del PR #7, repetición de M01–M14 y regresiones corregidas |
| V7 | Builds/Windows/V7-Character-Audio | `8a944c8e05a8616603968230499e134ca2002ade` | B2-M01–B2-M09 de personaje y audio |

La referencia `2cc512c` del formulario V6 anterior quedó superada. El código de Game en el head del PR #7 (`4c973e5`) es idéntico al de `7b94a1d`. V7 añade personaje y audio: aprobar V7 no aprueba automáticamente la versión V6 del PR #7.

## Preparación y apertura

Conectar el cargador y auriculares; anotar salida de audio y volumen de Windows. Cerrar Unity y otros juegos. Reservar unos 35–45 minutos entre ambas sesiones. Abrir PowerShell en la raíz de esta copia y ejecutar una sesión a la vez:

```powershell
& .\Tools\Iniciar-Sesion-Diego.ps1 -Build V6 -OutputDirectory '.dev-logs/diego-v6-01'
& .\Tools\Iniciar-Sesion-Diego.ps1 -Build V7 -OutputDirectory '.dev-logs/diego-v7-01'
```

Usar sufijos nuevos para repeticiones. `-PrepareOnly` crea fichas sin abrir el juego; para jugar después, ejecutar sin esa opción y con otra carpeta nueva. El lanzador comprueba el commit declarado, registra el SHA256 del ejecutable y abre el juego sin control automático. Las builds se conservan localmente y no se incluyen en Git.

Controles: WASD, ratón, Espacio, E, R, F1. En V7, Shift camina. Escape libera el cursor y un clic lo recaptura. La salida del juego registra el código de cierre, pero no decide la aprobación de los casos.

## Recorrido V6 — M15 del PR #7

Usar los pasos detallados de [FORMULARIO-PRUEBA-MANUAL-V6.md](../QA/Entregable06/FORMULARIO-PRUEBA-MANUAL-V6.md), aplicados a la versión de esta ficha.

1. M01–M05: movimiento/salto, E en cada obstáculo y polvo único, reconocimiento visual de obstáculos, cámara en muro oeste y visibilidad entre vegetación.
2. M06–M09: restauración y salida; R tras 1/3 y tras 3/3; arco y mensaje ODS 15; legibilidad del HUD.
3. M10–M12: cinco minutos de comodidad visual; agua y charcos; viento y cambios de LOD sin saltos molestos.
4. M13: consultar el informe de medición de esta PC; un fallo de rendimiento mantiene M15 sin aprobar, aunque los pasos humanos sean satisfactorios.
5. M14: comprobar pantalla completa y ventana de 1280×800, registrando cómo se cambió el modo/resolución.
6. Comprobar QA6-001 (raíces/barrera), QA6-002 (ramas), QA6-003 (tocones), QA6-004 (extremo del tronco), QA6-009 (un solo efecto) y QA6-005 (arranque sin congelación persistente). Registrar cada observación dentro de M15 y citar captura o clip.

## Recorrido V7 — personaje y audio

Seguir íntegramente la tabla de [INFORME-BLOQUE-02.md](INFORME-BLOQUE-02.md#pruebas-manuales-pendientes):

| ID | Acción y observación necesarias |
|---|---|
| B2-M01 | WASD, giro y reposo: pies, suavidad y ausencia de deslizamiento. |
| B2-M02 | Mantener Shift y comprobar caminar lento con animación propia. |
| B2-M03 | Saltar quieto, corriendo y contra la barrera; respuesta y colisión. |
| B2-M04 | Caer por el borde este; caída, reaparición y aterrizaje sin audio doble. |
| B2-M05 | Pulsar y mantener E en cada obstáculo; un gesto/sonido y movimiento disponible. |
| B2-M06 | Tercer obstáculo y escuchar 10 s; gesto, sonido, fundido del río/aves y salida. |
| B2-M07 | R tras 1/3 y completar; vuelta suave al ambiente degradado sin bucles atascados. |
| B2-M08 | Cámara junto al muro oeste y árboles; Guardiana visible y sin penetración. |
| B2-M09 | Cinco minutos con auriculares; mezcla, repetición y comodidad. Anotar auriculares y volumen. |

B2-M10 es la medición instrumentada del informe de esta PC, separada de las nueve observaciones humanas.

## Registro y criterio de cierre

Completar `RESULTADOS-DIEGO.csv`: APROBADO, FALLIDO o BLOQUEADO, resultado observado, fecha y ruta de evidencia. No marcar APROBADO por la mera ausencia de errores. Para defectos: pasos, esperado, observado, frecuencia y captura/clip. Conservar los registros de ambas versiones por separado.

Al terminar, entregar CSV, capturas y los `sesion.json`. El `Player.log` local sirve para revisar errores; revisar datos personales antes de publicarlo. M15 solo se aprueba con todos sus casos satisfactorios, incluido M13. B2-M09 exige escucha humana. Las pruebas de usuarios nuevos del formulario original siguen siendo un bloque separado y no quedan ejecutadas aquí.
