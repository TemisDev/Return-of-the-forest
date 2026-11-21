# Bloque 04: Zona 3, "El refugio perdido", fauna y final

**Rama:** `feature/character-animation-audio`, commit `5cc5a98`.

**Build:** `Builds/Windows/V9-Zona3-Refugio/BosqueDeRegreso.exe`, commit `5cc5a98`, 0 errores. Incluye las tres zonas: Arroyo → Claro → Refugio.

**Para probar el refugio sin jugar las zonas anteriores:** `BosqueDeRegreso.exe -zona refugio`. El arroyo y el claro se dan por completados, así que al terminar se ve el final.

## Qué se juega

1. **Transición desde el claro:** al cruzar el arco norte del claro aparece "Siguiente zona: El refugio perdido…". A los 4,5 s se carga la zona. Pulsar R antes lo cancela.
2. **Llegada:** la Guardiana entra al refugio por el puente de raíces del claro. Así se cumple el "usar el paso" del GDD.
3. **Puzle del refugio:** hay que devolver al paso del animal tres condiciones de refugio.
   - **Tronco hueco y piedras:** se recogen con **E**, la Guardiana los lleva junto a su hombro y se colocan con **E** en un **lugar indicado** (anillo de piedras). Mientras carga, camina al 80 % de su velocidad. **E** sin objetivo suelta el elemento en el suelo.
   - **Lugares indicados:** hay cuatro.
     - **Válidos (2):** junto al paso del animal, con huellas al lado y cobertura (un tocón, las rocas de la madriguera).
     - **Expuestos (2):** en el centro abierto. Dan una pista ("Aquí no sirve…") y la Guardiana conserva el elemento.
   - **Matorral nativo:** crece con **Q** en el lecho húmedo junto a la madriguera. En el lecho seco del centro no crece y aparece una pista.
   - **Pista principal:** las **huellas** marcan el recorrido del animal desde el bosque del oeste hasta la madriguera, entre rocas al noreste.
4. **Recompensa:**
   - Con las tres condiciones, la zona revive: vegetación, cielo, aves, brisa y música.
   - El paso queda bordeado de matorrales, como un corredor conectado.
   - A los 1,8 s, el **zorro de neblina** sale del bosque y recorre el paso hasta la madriguera (27,5 m). La cámara lo acompaña desde un punto fijo y la Guardiana se queda quieta mientras tanto.
5. **Final:** cuando el zorro llega, la zona se completa.
   - **Con las tres zonas completadas,** se muestra el final "EL BOSQUE REGRESA". Resume qué condición se recuperó en cada zona y aclara que la recuperación real toma años, seguimiento y conocimiento local. Cierra con "cuidar y mantener". Es el mensaje que piden las secciones 1.3 y 3.1 del GDD.
   - **Si falta alguna zona** (solo puede pasar en pruebas), se muestra el mensaje de la zona.
6. **Teclas:** R reinicia la zona, incluido el zorro y la cámara. F1 muestra la ayuda.

## El animal: zorro de neblina

Es la especie ficticia pequeña inspirada en un mamífero local que pide el GDD. Su referencia es el **zorro andino o culpeo**: lomo gris jaspeado, cabeza, flancos y patas rojizos, garganta crema y punta de la cola oscura. Mide 0,95 m del hocico a la cola.

- **Origen:** el modelo animado de muestra "Fox" de Khronos (glTF Sample Assets).
  - Modelo de PixelMannen, CC0.
  - Rig y animaciones de tomkranis, CC-BY 4.0.
  - Conversión de @AsoboStudio y @scurest, CC-BY 4.0.
  - Ver [LICENCIAS-ZONA-3.csv](../../Fauna/LICENCIAS-ZONA-3.csv).
- **Proceso:** `Docs/Fauna/pipeline/preparar_zorro.py`, ejecutado con Blender en modo batch.
  - Malla: se soldaron los vértices y se aplicaron dos niveles de subdivisión (de 576 a 13 824 triángulos) para que no se vean las facetas del original low-poly.
  - Pelaje: se pintó uno nuevo a 2K a partir de la posición 3D de cada texel.
  - Vista previa: `Docs/Fauna/pipeline/vista_previa_zorro.png`.
- **Unity:** `Bosque.CharacterEditor.FaunaBuilder`.
  - Rig Generic a escala real y material URP.
  - Animator con reposo (Survey), paso (Walk) y carrera (Run) según la velocidad.
  - Prefab: `Characters/Fauna/ZorroNeblina/Prefabs/PF_ZorroNeblina.prefab`.
- **Pendiente de validar:** el ciclo de paso se ajustó a 1,35 m/s por estimación, sin medir el deslizamiento de patas.

## Sistemas nuevos

Todos son reutilizables.

| Pieza | Archivo | Qué hace |
|---|---|---|
| Elemento de refugio | `Runtime/World/RefugeItem.cs` | Objetivo de E con cuatro estados: en su sitio, llevado, en el suelo o colocado. `ResetItem()` lo devuelve a su sitio. |
| Lugar indicado | `Runtime/World/RefugeSpot.cs` | Lugar válido o expuesto, con pista. Admite un solo elemento. Eventos `Placed` y `Rejected`. |
| Carga | `Runtime/Interaction/ItemCarrier.cs` | Lleva un elemento a la vez y lo suelta con E sin objetivo. Si la Guardiana cae al vacío, el elemento vuelve a su sitio. |
| Recorrido de fauna | `Runtime/World/FaunaRoute.cs` | Ruta por puntos, giro suave, apoyo en el suelo y parámetro `Speed` del Animator. Evento `ArrivedAtRefuge`. |
| Plano de cierre | `Runtime/Session/FaunaCinematic.cs` | La cámara sigue al animal desde un punto fijo. `Stop()` devuelve la cámara de juego. |
| Zona | `Runtime/Session/RefugioSession.cs` | Ver la nota debajo de la tabla. |
| Interfaz y final | `Runtime/Session/RefugioHud.cs` | Objetivo, avisos [E]/[Q], pistas, ayuda F1 y pantalla final. |
| Audio y gestos | `Runtime/Session/RefugioAudioBridge.cs` | Recoger, colocar (tronco o piedras), intento fallido, matorral, restauración y final. |
| Transición | `Runtime/Session/ClaroZoneTransition.cs` | Claro → Refugio. `ClaroHud` anuncia la zona siguiente. |
| Construcción de la escena | `Editor/Levels/Refugio/RefugioBuilder.cs` | Ver la nota debajo de la tabla. |
| Velocidad del motor | `ThirdPersonMotor.SpeedFactor` | Multiplicador de velocidad; vale 1 por defecto y 0,8 al cargar. |

**Zona:** tiene tres objetivos de progreso: tronco, piedras y matorral. Al recuperarse inicia el regreso del animal, que completa la zona al llegar. El reinicio deja la zona limpia.

**Construcción de la escena:**
- Idempotente: `Bosque.LevelEditor.RefugioBuilder.BuildAllBatch`.
- Primero construye el zorro y después la escena.
- Integra la transición en el claro.
- Registra las tres escenas en Build Settings.

## Medidas del nivel

- **Suelo:** 28 × 44 m (z −4..40). La llegada por el puente está en z −4 y el punto de inicio en (0; −1,5).
- **Elementos:** tronco hueco (1,6 m) en (−6,5; 6) y piedras en (7,5; 8).
- **Lugares válidos:** tocón (−3,6; 25,3) y rocas (9,4; 30,3). Los dos están a entre 1 y 2,2 m del paso, cerca sin bloquearlo.
- **Lugares expuestos:** (−1; 12,5) y (5,5; 16,5), a más de 5 m del paso.
- **Lechos:** húmedo en (6,4; 27) y seco en (−4,5; 15,5).
- **Paso del animal:** siete puntos desde (−13,2; 31) hasta la madriguera en (11; 34), con 61 huellas.
- **Renderers:** 332 en degradado y 534 en restaurado. El arroyo tiene 1100 y el claro 241 / 548.

## Resultados automatizados

Esto no sustituye a las pruebas manuales.

| Prueba | Resultado |
|---|---|
| PlayMode del refugio (`RefugioPlayModeTests`) | **8/8** |
| PlayMode completo (Guardiana, claro y refugio) | **26/26** |
| EditMode | **38/38** |
| Verificador de builds sobre V9 (arroyo) | `passed=True`. Ver la nota debajo de la tabla. |
| Arranque de la build con `-zona refugio` (25 s) | Carga la zona; sin errores ni excepciones en el log |

**Verificador sobre V9:** degradado a 90 FPS de media (p95 15,2 ms) y restaurado a 69 FPS (p95 18,0 ms). GTX 1650, 1080p.

Qué cubren las pruebas del refugio:
- **Trazado:** los lugares válidos están junto al paso y los expuestos lejos. El zorro está oculto al empezar.
- **Lugar expuesto:** da pista y la Guardiana conserva el elemento, con un solo sonido de error. El lecho seco también da pista.
- **Soltar y caer:** soltar deja el elemento apoyado en el suelo y se puede recoger otra vez. Caer al vacío lo devuelve a su sitio.
- **Recorrido completo:** se colocan los dos elementos, un lugar ocupado no admite otro y crece el matorral; la zona se restaura y se bloquea el control durante el regreso. El zorro aparece, la cámara lo sigue y la animación de paso se activa. Camina sobre el suelo sin desviarse más de 0,8 m de la ruta, llega a la madriguera y queda en reposo. La zona se completa y se muestra el final con su sonido.
- **Final:** no se muestra si faltan zonas.
- **Reinicio:** devuelve elementos, lugares, matorral, zorro, cámara y control a su estado inicial, y se puede volver a jugar.
- **Transición:** completar el claro carga la escena del refugio.
- **Capturas.**

Capturas: [CAPTURAS/](CAPTURAS/). No incluyen la interfaz porque las cámaras se renderizan sin IMGUI.

## Pruebas manuales pendientes

Son para Diego. **No están ejecutadas.**

| ID | Pasos | Esperado |
|---|---|---|
| Z3-M01 | Completar el claro y esperar en el arco. | Aviso de la zona siguiente y carga del refugio sin parones largos. |
| Z3-M02 | Sin pistas verbales, ¿lleva los elementos a los lugares buenos? (contar intentos fallidos) | Lee las huellas y la cobertura como el paso del animal. |
| Z3-M03 | Llevar el tronco: caminar, saltar y girar la cámara. | El tronco no tapa la vista ni atraviesa la cámara. La lentitud se nota pero no molesta. |
| Z3-M04 | Pulsar E sin objetivo mientras lleva algo, y E repetidamente en un lugar expuesto. | Suelta en el suelo; la pista aparece sin sonidos repetidos molestos. |
| Z3-M05 | Completar el refugio y mirar el regreso del zorro. | Plano estable, el zorro camina sin deslizarse de forma evidente y entra en la madriguera. |
| Z3-M06 | Pantalla final: leerla en voz alta. | Se entiende y se distinguen las tres condiciones (agua, plantas y refugio). |
| Z3-M07 | Pulsar R durante el regreso del zorro y en el final. | Todo vuelve al inicio y se recupera el control y la cámara. |
| Z3-M08 | Partida completa desde el arroyo y FPS con `-zona refugio` en la PC de la presentación. | Las tres transiciones funcionan; el rendimiento es igual o mejor que en el arroyo. |

## Limitaciones conocidas

- **Carga:** no hay animación de cargar. El elemento flota junto al hombro de la Guardiana (se presenta como una ayuda de la guardiana). Si se quiere una pose de brazos, se puede añadir en la capa de gestos.
- **Zorro:** el modelo de origen es sencillo. Tras suavizarlo y repintarlo encaja a la distancia de juego, pero de muy cerca se ve que no tiene pelo real.
- **Pantalla final:** es un panel de la interfaz. El menú principal, la pausa con opciones y los créditos son del Bloque 05.
- **Rendimiento:** el verificador solo mide el arroyo. No se midió el refugio en la build; tiene la mitad de renderers que el arroyo restaurado.
