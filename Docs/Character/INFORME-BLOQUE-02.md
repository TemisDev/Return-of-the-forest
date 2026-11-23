# Bloque 02: personaje, animaciones y audio

**Rama:** `feature/character-animation-audio`, que sale de la integración V6 `4c973e5` (PR #7, sin fusionar).

**Build:** `Builds/Windows/V7-Character-Audio/BosqueDeRegreso.exe`, commit `ea19aad` (Guardiana realista), 0 errores, 382 MB ([build-info](EVIDENCIAS/build-info-V7.json)). La build anterior, con el personaje de Quaternius (`8a944c8`), se conserva en `Builds/Windows/V7-Character-Audio-8a944c8`. Ninguna está en Git: se regeneran con `Tools/Build-ArroyoRC1.ps1 -Configuration V7-Character-Audio` desde una copia limpia.

**Rendimiento del personaje realista** (misma sesión, ejecuciones alternas): restaurado 70 y 65 FPS (p95 18,1 y 18,6 ms) frente a 61 FPS (p95 20,0 ms) con el personaje anterior, así que no tiene coste medible. Verificación funcional completa en las 3 ejecuciones.

## Qué hay en el juego

| Pieza | Dónde | Notas |
|---|---|---|
| **Guardiana** | `Characters/Guardiana/Prefabs/PF_Guardiana.prefab` | Ver el detalle debajo de la tabla. |
| **Animator** | `Characters/Guardiana/Animation/AC_Guardiana.controller` | Ver el detalle debajo de la tabla. |
| **Motor** | `Runtime/Player/ThirdPersonMotor.cs` | Ver el detalle debajo de la tabla. |
| **Cámara** | `ArroyoPlayable` | La altura de seguimiento pasa de 1,35 a 1,45 m y la distancia de 4,4 a 4,2 m. Se mantienen la colisión y el suavizado existentes. |
| **Audio** | `Runtime/Audio/*` | Ver el detalle debajo de la tabla. |
| **Ambientes** | `Audio_Arroyo/Ambiente` | Ver el detalle debajo de la tabla. |
| **Efectos** | `Audio/SFX_Biblioteca.asset` | Pasos, salto, aterrizaje, interacción válida, retirar tronco, retirar rocas, retirar ramas, restauración, desbloqueo de la salida y final del nivel. Tienen variación de tono y distancia 3D. |
| **Música** | `Audio_Arroyo/Musica` | Orquestal. Suena baja en degradado y crece al restaurar. |

**Guardiana (realista, desde el 21/11/2026):**
- **Generación:** humana de proporciones reales hecha con MPFB (MakeHuman para Blender). Mujer joven de rasgos andinos y 1,53 m.
- **Aspecto:** piel de tono medio, trenza, suéter de lana verde oliva, pantalón cargo y botines de cuero.
- **Licencias:** todos los recursos de MakeHuman son CC0; la herramienta MPFB es GPL, pero el modelo que genera no hereda esa licencia.
- **Sustituye** al personaje estilizado de Quaternius, que no encajaba con la calidad del entorno V6.
- **Regeneración:** `Docs/Character/pipeline/mpfb/` (configuración, generación, preparación de texturas y descarga parcial de los packs).
- **Coste:** 33 218 triángulos, 8 mallas y 8 materiales URP Lit con texturas a 2K. Rig `game_engine` de 53 huesos, importado como Humanoid. La piel tapada por la ropa se elimina para que no asome.
- Sustituye a la cápsula: el objeto `ProvisionalBody` y su collider se conservan, solo se oculta su render.
- **Orientación** (corregida el 21/11):
  - Al trotar, el cuerpo iba girado unos 19° a la derecha de la dirección de avance.
  - Se compensa por clip (trote +32°, caminar +8°, con "Body Orientation").
  - Medido en juego: desvío menor de 3° y ladeo menor de 1,1° en reposo, al caminar y al trotar. Lo vigila la prueba `PostureFacesMovementWithoutSideLean`.

**Animator:**
- Mezcla de locomoción Idle/Walk/Jog/Sprint según la velocidad real del motor. Los umbrales son la velocidad propia de cada clip (0,97, 5,36 y 8,25 m/s), así que los pies no patinan.
- Estados de salto: impulso, aire (que también sirve de caída) y aterrizaje.
- Capa "Gestos" con máscara de torso y brazos para interactuar y restaurar. Las piernas siguen la locomoción, así que el gesto nunca bloquea el movimiento.
- Sin root motion.

**Motor:**
- API de solo lectura: velocidad, suelo y eventos de salto y aterrizaje.
- **Shift: caminar.**
- Margen de apoyo de 0,1 s. Antes, una pulsación de salto podía perderse estando quieto, porque `isGrounded` parpadea.
- Velocidad (4,5 m/s), altura de salto (1,1 m) y colliders sin cambios.

**Audio:**
- Buses Master, Music, Ambience y SFX, con la API `BosqueAudio.SetVolume(bus, 0..1)` persistente, pensada para el menú de opciones.
- No hay AudioMixer, porque Unity no permite crearlo por script de editor.
- `ArroyoAudioBridge` es el único punto acoplado al arroyo. El audio y el personaje sirven para otras zonas.

**Ambientes:**
- **Degradado:** viento seco, crujidos y ramas secas, y algún ave lejana.
- **Restaurado:** río, cauce en 3D, aves y brisa en las hojas.
- El paso de un estado a otro es un fundido de 5 s ligado al evento real de restauración. Al reiniciar vuelve en 1,2 s, sin reiniciar ni duplicar los bucles.

**Extensible para el próximo bloque** (crecimiento de plantas, semillas, fauna):
- `CharacterAnimationDriver.PlayRestore()` y el disparador `Restore`.
- `PlayerInteractor.Interacted`.
- `SfxLibrary` con identificadores.
- `AmbienceController.SetRestored`, que admite capas por zona.

## Cómo regenerarlo

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe" -batchmode -projectPath Game -executeMethod Bosque.CharacterEditor.GuardianaBuilder.BuildAllBatch -logFile .dev-logs\guardiana.log
```

El constructor es idempotente: importaciones Humanoid, materiales, cabeza, máscara, controlador, prefab, biblioteca de efectos e integración en `ArroyoPlayable`. El audio se procesa con [pipeline/audio_build.py](pipeline/audio_build.py) (ffmpeg y soundfile) y las texturas con [pipeline/tex.py](pipeline/tex.py). Procedencia y licencias: [LICENCIAS-BLOQUE-02.csv](LICENCIAS-BLOQUE-02.csv); todo es CC0 o síntesis propia.

## Resultados automatizados

Esto **no** sustituye a las pruebas manuales.

| Prueba | Resultado |
|---|---|
| EditMode (incluye movimiento, sesión, visuales V6 y presupuesto) | **38/38** ([xml](EVIDENCIAS/pruebas-editmode.xml)) |
| PlayMode nuevas, sobre `ArroyoPlayable` real | **9/9** ([xml](EVIDENCIAS/pruebas-playmode.xml)) |
| Sonda V6 integrada | **43 OK, 0 AVISO, 0 FALLA** |
| Verificador de la build V7 (patrulla a 1080p, 3 reinicios) | `passed=True` en 2 de 2 ejecuciones |

Qué comprueban las 9 pruebas PlayMode:
- avatar y rig Humanoid válidos y sin colliders nuevos;
- que el personaje no queda en pose T y mira hacia donde avanza;
- mezcla de locomoción a la velocidad real y Shift para caminar;
- salto completo (impulso, aire, aterrizaje, locomoción) y caída;
- interacción con un solo gesto, una sola retirada y un solo sonido aunque se pulse tres veces, sin bloquear el movimiento;
- restauración: sonido único, gesto, fundido progresivo del ambiente, crecimiento de la música, desbloqueo de la salida una sola vez y final una sola vez;
- reinicio: ambiente degradado, sin duplicar bucles y personaje en reposo;
- pasos solo al moverse (entre 3 y 12 en 2 s) y nunca en reposo;
- API de volúmenes;
- capturas.

## Rendimiento

GTX 1650, 1080p, URP equilibrado, verificador con 30 s por estado en ejecuciones alternas.

| Build | Degradado | Restaurado | Memoria pico |
|---|---|---|---|
| V7, 1.ª ejecución | 101 FPS, p95 12,2 ms | 68 FPS, p95 18,3 ms | 773 MiB |
| V6 `7b94a1d` (referencia) | 84 FPS, p95 15,3 ms | 65 FPS, p95 19,2 ms | 722 MiB |
| V7, 3.ª ejecución (en caliente) | 78 FPS, p95 16,4 ms | 57 FPS, p95 21,4 ms | 768 MiB |

- **Coste del personaje y del audio:** sin diferencia de FPS por encima del ruido térmico; unos 50 MB más de memoria.
- **Por qué es barato:**
  - Personaje: 1 Animator (siempre activo) y 10 mallas con skinning (31 k triángulos).
  - Audio: unas 8 voces simultáneas habituales sobre 27 AudioSources (14 de efectos, 6 bucles, 6 puntuales y 1 de música). Los bucles largos se reproducen en streaming.
- **Sin cerrar:** el estado restaurado sigue algo por encima de 16,67 ms en p95 cuando el equipo está caliente (QA6-006, pendiente de M13).

## Capturas reales

- Personaje en juego, con animación: [CAPTURAS/](CAPTURAS/), que son reposo, caminar, trotar, salto, interacción, restauración y vista de juego.
- Con HUD, desde la build V7: [EVIDENCIAS/v7_hud_*.jpg](EVIDENCIAS/).

## Pruebas manuales pendientes

**Actualización Codex, 20/11/2026:** el usuario confirmó que esta es la PC de presentación y pidió preparar la sesión porque Diego no está disponible. Guía vigente: [SESION-DIEGO.md](SESION-DIEGO.md). Resultado de M13/B2-M10 y evidencias: [VALIDACION-PC-PRESENTACION.md](VALIDACION-PC-PRESENTACION.md). B2-M01–B2-M09 y M15 siguen sin ejecución humana; no interpretar las verificaciones automáticas como aprobación manual.

Son para Diego, con la build V7. **No están ejecutadas.**

| ID | Pasos | Esperado |
|---|---|---|
| B2-M01 | Moverse con WASD, girar la cámara y soltar las teclas. | Los pies no patinan; los giros son suaves (no instantáneos); el personaje vuelve a reposo. |
| B2-M02 | Mantener Shift mientras se camina. | Camina despacio con su animación propia. |
| B2-M03 | Saltar quieto, saltar corriendo y saltar contra la barrera. | Responde siempre; no flota ni atraviesa la barrera. |
| B2-M04 | Caer por el borde este. | Pose de caída, reaparición y aterrizaje sin sonidos dobles. |
| B2-M05 | Pulsar E una vez y luego mantenerla en cada obstáculo. | Un gesto y un sonido por obstáculo; se puede seguir moviéndose. |
| B2-M06 | Retirar el tercer obstáculo y escuchar 10 s. | Gesto de restauración, sonido de restauración, el río y las aves entran poco a poco y la salida se desbloquea. |
| B2-M07 | Pulsar R tras 1/3 y tras completar el nivel. | Vuelven el viento y los crujidos, sin cortes bruscos ni sonidos que se queden sonando. |
| B2-M08 | Pegarse al muro oeste y a los árboles y mover la cámara. | La cámara no atraviesa a la Guardiana y siempre se la ve. |
| B2-M09 | Jugar 5 minutos con auriculares. | Mezcla equilibrada: la música no tapa los efectos y nada es molesto ni repetitivo. |
| B2-M10 | Medir FPS en la PC de la presentación (equivale a M13). | p95 ≤ 16,67 ms; anotar el equipo. |

## Pendiente

- Ejecutar B2-M01–B2-M09 y M15 con Diego. M13/B2-M10 se han medido en esta PC: consultar el informe vigente enlazado arriba para su resultado y el bloqueo de rendimiento QA6-006; una medición ejecutada no equivale a un criterio aprobado.
- El sonido aún no se ha escuchado con oídos humanos. La elección se hizo por nombre, duración y nivel (ver [AUDIO-PROCESADO.json](AUDIO-PROCESADO.json)); B2-M09 puede pedir cambiar algún clip.
- Animaciones de giro en el sitio (Turn): no hay clip en la librería gratuita. El motor gira a 720°/s con suavizado.
- AudioMixer con snapshots: los buses actuales cubren los volúmenes del menú. Si hace falta ducking, crear el mixer a mano en el editor y conectarlo a `BosqueAudio`.
- La línea inferior del HUD no menciona Shift; la ayuda F1 sí.
