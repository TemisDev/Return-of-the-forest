# Bloque 05: presentación, menús e interfaz

**Rama:** `feature/presentation-menus`, que sale de `main` (`1d143d8`). Commit `389a98b`.

**Build:** `Builds/Windows/V10-Presentacion/BosqueDeRegreso.exe`, commit `389a98b`, 0 errores. Arranca en el menú principal.

## Qué cambia al jugar

1. **Menú principal:** el título va sobre el bosque restaurado (la Guardiana y el zorro en un claro), con la cámara girando despacio, música y aves.
   - Botones: **Comenzar**, **Opciones**, **Controles**, **Créditos** y **Salir**.
   - Avisos: "El progreso no se guarda al cerrar el juego" (GDD 1.4) y "ODS 15".
   - Se puede usar con ratón o teclado.
2. **Introducción:** tres páginas manuscritas presentan el bosque fragmentado, la guardiana y el cuaderno de su cuidadora. Se pueden saltar.
3. **Fundidos y rótulos:** cada cambio de escena funde a negro. Al entrar en una zona aparece su rótulo ("ZONA 2 · El claro vacío" y lo que hay que hacer).
4. **HUD nuevo:**
   - **Objetivo:** tarjeta con la zona, el objetivo y el progreso con iconos.
   - **Aviso de acción:** abajo, con la tecla destacada.
   - **Pistas:** tarjeta con acento ámbar.
   - **Ayuda F1:** panel en el centro.
   - **Teclas:** recordatorio de Esc, F1 y R.
   Sustituye a los paneles planos de depuración.
5. **Pausa (Esc):** detiene el tiempo y el audio y libera el cursor.
   - Opciones: Continuar, Reiniciar zona, Opciones, Controles, Menú principal y Salir.
   - Esc cierra el submenú abierto o vuelve al juego.
6. **Opciones:** se guardan entre sesiones. Son ajustes, no progreso.
   - **Volumen:** general, música, ambiente y efectos.
   - **Ratón:** sensibilidad e inversión del eje vertical.
   - **Pantalla:** completa o en ventana.
   - **Restablecer:** vuelve a los valores por defecto.
7. **Cuaderno de la cuidadora** (GDD 3.1): al recuperar una zona aparece una página manuscrita.
   - Contenido: la nota, lo que se aprendió y la recompensa (en el arroyo, la habilidad CRECIMIENTO [Q]).
   - **Continuar** (botón, E, Enter o Espacio) funde a la zona siguiente. Antes había que esperar 4,5 s sin poder leer con calma.
8. **Final:** tras el regreso del zorro aparece la última nota y después la pantalla "El bosque regresa".
   - Resume las tres condiciones recuperadas e incluye el mensaje del GDD 1.3.
   - Botones: Créditos, Jugar de nuevo y Menú principal.
9. **Créditos:** equipo y atribuciones de todos los recursos, incluida la CC-BY obligatoria del zorro.
   - Se editan en `Game/Assets/_Bosque/UI/Creditos.txt`.
   - **Falta el nombre de quien programa:** ahora dice "Equipo de Bosque de regreso".

## Sistemas

| Pieza | Archivo | Qué hace |
|---|---|---|
| Capa de presentación | `Runtime/UI/Presentation.cs` | Ver la nota debajo de la tabla. |
| Vistas | `Runtime/UI/PresentationViews.cs` | HUD, cuaderno, final, pausa y rótulo, construidos con uGUI y TextMeshPro. |
| Fábrica de interfaz | `Runtime/UI/Ui.cs`, `UiTheme.cs` | Lienzo de referencia 1920×1080, paneles, botones, deslizadores y casillas con el estilo del juego. |
| Paneles compartidos | `Runtime/UI/MenuPanels.cs` | Opciones, controles y créditos (los usan el menú y la pausa). |
| Menú principal | `Runtime/UI/MainMenu.cs` | Menú, introducción y cámara orbital. |
| Flujo y textos | `Runtime/UI/GameFlow.cs` | Escenas, nueva partida, vuelta al menú, rótulos y notas del cuaderno. |
| Pausa | `Runtime/UI/GamePause.cs` | `timeScale` y `AudioListener.pause`. |
| Ajustes de cámara | `Runtime/Player/CameraSettings.cs` | Sensibilidad e inversión, en PlayerPrefs. |
| Datos del HUD | `Runtime/UI/IZoneHud.cs` | Ver la nota debajo de la tabla. |
| Construcción | `Editor/Presentation/PresentationBuilder.cs` | Ver la nota debajo de la tabla. |

**Capa de presentación:** se crea sola al arrancar y sobrevive a los cambios de escena. Gestiona los fundidos, los rótulos, el HUD, el cuaderno, la pausa y el final.

**Datos del HUD:** cada HUD de zona (`ArroyoPrototypeHud`, `ClaroHud`, `RefugioHud`) solo expone sus datos y su dibujo IMGUI antiguo queda como respaldo. La lógica de juego no cambia.

**Construcción:** `Bosque.PresentationEditor.PresentationBuilder.BuildAllBatch`.
- Crea las tipografías TMP, los fondos generados, el tema (`UI/Resources/BosqueUiTheme`) y la escena `Scenes/MenuPrincipal.unity`.
- Deja Build Settings como [Menú, Arroyo, Claro, Refugio].
- Requiere los recursos esenciales de TextMeshPro, que ya están importados en `Assets/TextMesh Pro`.

**Cambios compatibles:**
- **Esc:** ya no suelta el cursor; abre la pausa, que sí lo suelta.
- **Transiciones:** esperan a que se cierre el cuaderno.
- **Verificador de builds** (`-arroyo-verify`): carga el arroyo aunque la build empiece en el menú, y no muestra rótulos ni notas.
- **Atajos `-zona claro` y `-zona refugio`:** siguen funcionando.

## Tipografías

| Uso | Tipografía | Licencia |
|---|---|---|
| Interfaz | Alegreya Sans: Regular, Bold, ExtraBold e Italic | SIL OFL 1.1 |
| Cuaderno | Kalam Regular | SIL OFL 1.1 |

Los textos de las licencias están en `Game/Assets/_Bosque/UI/Fonts/OFL-*.txt`.

## Resultados automatizados

Esto no sustituye a las pruebas manuales.

| Prueba | Resultado |
|---|---|
| PlayMode de presentación (`PresentationPlayModeTests`) | **8/8** |
| PlayMode completo (Guardiana, claro, refugio y presentación) | **34/34** |
| EditMode | **38/38** |
| Verificador de builds sobre V10 (arranca en el menú y carga el arroyo) | `passed=True`. Ver la nota debajo de la tabla. |
| Arranque de la build en el menú y con `-zona refugio` (20 s cada uno) | Sin errores ni excepciones |

**Verificador sobre V10:** degradado a 92 FPS (p95 15,5 ms); restaurado entre 60 y 66 FPS (p95 19,6 a 22,1 ms). Antes daba 69 FPS. La diferencia puede deberse al HUD uGUI o a que otro proceso de Unity (QA de Codex) estaba abierto durante la medición; hay que confirmarlo en B5-M07 / Z3-M08.

Qué cubren las pruebas de presentación:
- **Tema:** tema, tipografías con glifos del español, créditos con la atribución del zorro y una sola capa de presentación.
- **Menú:** botones del menú; la introducción de tres páginas empieza una partida nueva en el arroyo con su rótulo.
- **Pausa:** congela tiempo, audio y movimiento; tiene todos sus botones, abre los controles y vuelve. "Reiniciar zona" reinicia.
- **Opciones:** cambian volúmenes y sensibilidad (la cámara gira el doble con 2×) y se restablecen.
- **HUD:** muestra objetivo, aviso [Q] y pista en el claro.
- **Final:** tras el zorro, la última nota, el final, los créditos y la vuelta al menú.
- **Cuaderno entre zonas:** al completar el arroyo y el claro aparece la nota y solo se avanza al pulsar Continuar (pruebas del claro y del refugio, actualizadas).
- **Capturas:** [CAPTURAS/](CAPTURAS/) (menú, opciones, créditos, introducción, rótulo, HUD, pausa, cuaderno y final).

## Pruebas manuales pendientes

Son para Diego. **No están ejecutadas.**

| ID | Pasos | Esperado |
|---|---|---|
| B5-M01 | Abrir la build; recorrer el menú con el ratón y luego solo con el teclado (flechas, Enter y Esc). | Todo responde; Esc vuelve atrás. |
| B5-M02 | Leer la introducción y pulsar Saltar. | Texto legible; Saltar empieza la partida. |
| B5-M03 | En cada zona: Esc, cambiar volúmenes y sensibilidad, y continuar. | El juego se congela, los cambios se oyen o se notan al momento y el cursor vuelve a capturarse. |
| B5-M04 | Pantalla completa activada y desactivada desde Opciones. | Cambia sin cerrar el juego. |
| B5-M05 | Completar cada zona y leer la nota del cuaderno. | La nota se entiende; Continuar lleva a la zona siguiente con fundido. |
| B5-M06 | Partida completa hasta el final; abrir Créditos y luego Jugar de nuevo. | El final se ve completo; Jugar de nuevo empieza en el arroyo sin progreso. |
| B5-M07 | Resoluciones 1280×720, 1920×1080 y ultrapanorámica. | Nada se corta ni se solapa. |
| B5-M08 | Cerrar y volver a abrir el juego. | Las opciones se conservan y el progreso no (como se avisa). |

## Notas

- **Copia de trabajo:** al compilar V10 había documentos de QA de Codex sin commit (`Docs/Handoffs/CODEX.md`, `Docs/Validacion-Codex/`, `output/`). No forman parte del juego y no se tocaron. Por eso la build se lanzó con el mismo comando de Unity que `Tools/Build-ArroyoRC1.ps1`, en lugar de con el script, que exige una copia limpia.
- **Constructores antiguos:** `ClaroBuilder` deja Build Settings con 2 escenas. Si se vuelve a ejecutar, después hay que ejecutar `RefugioBuilder`, que conserva el menú, o `PresentationBuilder`.
