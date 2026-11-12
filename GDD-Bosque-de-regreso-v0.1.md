# Bosque de regreso — GDD v0.1
Fecha: 6 de noviembre de 2026.
Estado: propuesta inicial para revisar con el equipo. Solo están confirmados el título elegido, Unity, 3D y un equipo de tres personas. También están confirmados tu rol de programación, los nombres Juan y Diego y un plazo de tres semanas. Plataforma, duración, historia, mecánicas y reparto son propuestas ajustables.

## 1. Concepto del juego
### 1.1 Ficha
- Género: aventura 3D de exploración, plataformas sencillas y puzles ambientales.
- Modalidad: un jugador.
- Plataforma propuesta: PC Windows; teclado y ratón.
- Cámara: tercera persona.
- Público propuesto: adolescentes y jóvenes; dificultad accesible.
- Duración objetivo del prototipo: 10–15 minutos.
- Experiencia buscada: curiosidad al explorar, satisfacción al resolver y esperanza al observar el regreso de la vida.

### 1.2 Premisa y propuesta de valor
Una joven guardiana regresa a un bosque fragmentado por el abandono y la degradación. Debe reconectar tres pequeños hábitats mediante soluciones ambientales. La recuperación modifica el escenario y abre caminos: las acciones del jugador se vuelven visibles y jugables.

Pilares:
1. Recuperar transforma: cada puzle cambia vegetación, sonidos y accesos.
2. Observar permite resolver: agua, suelo y refugios proporcionan pistas.
3. Explorar recompensa: rutas cortas y secretos opcionales invitan a mirar alrededor.

### 1.3 ODS y aprendizaje
ODS principal: 15, Vida de ecosistemas terrestres. El diseño se vincula con la conservación y restauración de ecosistemas (meta 15.1) y la recuperación de bosques degradados (meta 15.2).
Aprendizaje propuesto: reconocer que recuperar un hábitat requiere condiciones relacionadas —agua, vegetación adecuada y refugio—, además de plantar.
Evidencia de aprendizaje: al terminar, el jugador puede explicar qué condición recuperó en cada zona y cómo favoreció el retorno de la fauna.
La recuperación instantánea representa una simplificación fantástica del tiempo ecológico; un mensaje final aclara que los procesos reales requieren tiempo, seguimiento y conocimientos locales.

### 1.4 Alcance de la primera versión
Incluye: un mapa pequeño con tres zonas conectadas, una protagonista, tres puzles, una habilidad de crecimiento, una especie animal con comportamiento sencillo, interfaz básica, pausa, reinicio y final.
Fuera de esta versión: mundo abierto, multijugador, combate, inventario complejo, ciclo día/noche, simulación ecológica y guardado persistente.
La pérdida de progreso al cerrar se comunicará al iniciar. Podrá reiniciarse el nivel completo.

## 2. Mecánicas de juego
### 2.1 Bucle principal
Explorar → observar el problema → interactuar y resolver → recuperar el hábitat → abrir un camino → explorar la siguiente zona.

### 2.2 Acciones y controles propuestos
WASD: moverse. Ratón: orientar cámara. Espacio: saltar. E: interactuar. Q: activar crecimiento en un punto válido. Esc: pausa.
Las teclas son propuesta inicial. Se mostrarán indicaciones junto a objetos utilizables. Las pistas combinarán icono, texto y forma; el color no será la única señal.

### 2.3 Reglas y progresión
- Las interacciones solo funcionan dentro de alcance y sobre objetivos disponibles.
- Cada zona tiene estados Degradada y Recuperada; el cambio ocurre una sola vez al resolver su objetivo.
- La habilidad se obtiene al resolver la primera zona. Hace crecer una planta en puntos definidos para formar un paso; no permite crear plataformas libremente.
- Los intentos incorrectos dan una pista y permiten volver a probar. Caer devuelve al jugador a un punto seguro de la zona.
- Las tres zonas completadas desbloquean la escena final.
- La fauna retorna mediante un recorrido corto predefinido; no se implementa un ecosistema autónomo.

### 2.4 Nivel del prototipo
1. El arroyo dormido: observar el cauce y retirar obstáculos concretos para devolver el agua. Enseña movimiento e interacción; desbloquea crecimiento.
2. El claro vacío: activar el crecimiento de plantas nativas ficticias en puntos señalados, usando pistas de suelo y humedad. Abre un paso vegetal.
3. El refugio perdido: usar el paso y colocar elementos de refugio en lugares indicados. Conecta el hábitat y permite volver a un animal.
La secuencia evita un sistema general de fabricación: se emplean objetos de misión locales con estados simples.

### 2.5 Reto, recompensa y evaluación
El reto combina lectura del entorno, orden de acciones y saltos cortos. La recompensa es un camino nuevo, cambios visuales y sonoros, y la aparición de fauna.
No se premia recolectar indefinidamente. Los elementos opcionales aportan pequeñas notas del bosque.
Prueba propuesta: tres personas ajenas al desarrollo juegan sin instrucciones verbales. Se registra si completan cada objetivo, dónde se atascan y cómo explican la recuperación. Esto es una meta de validación, no un resultado obtenido.

## 3. Narrativa y arte
### 3.1 Mundo e historia
Bosque ficticio inspirado en paisajes andinos, con una zona de arroyo, un claro y un refugio. La inspiración regional requiere un pequeño registro de referencias; no se presentará como reproducción científica de un ecosistema real.
Inicio: la protagonista vuelve al bosque y encuentra el cauce seco y antiguos senderos interrumpidos.
Desarrollo: un cuaderno de su antigua cuidadora orienta la observación. Cada zona recuperada revela una nota sobre las relaciones entre agua, plantas y animales.
Cierre: un animal cruza el corredor restaurado; el bosque comienza a recuperarse. El mensaje es cuidar y mantener, además de reparar.

### 3.2 Personajes
- Protagonista: joven guardiana, silueta legible, ropa de exploración y mochila.
- Cuidadora: presencia mediante notas breves, sin NPC conversacional.
- Animal del bosque: especie ficticia pequeña inspirada en un mamífero local; comunica el retorno de la fauna mediante una animación y un recorrido corto.
Nombres y diseños definitivos quedan pendientes.

### 3.3 Dirección visual
3D estilizado low poly, formas claras, vegetación modular y materiales sencillos.
Zonas degradadas: tierra ocre, verdes apagados, suelo descubierto.
Zonas recuperadas: verdes más ricos, flores puntuales y agua visible. Cambian también siluetas y densidad de vegetación para que la recuperación no dependa solo del color.
La habilidad fantástica puede usar partículas suaves; el resto del bosque mantiene coherencia visual.
Interfaz discreta: objetivo actual, indicación de interacción y progreso de tres hábitats.

### 3.4 Audio y animación
Ambiente de viento y hojas al inicio; agua y cantos se incorporan al recuperar zonas. Música instrumental tenue, efectos claros de interacción y mezcla con controles de volumen.
Animaciones mínimas: protagonista en reposo, caminar, saltar e interactuar; animal en reposo y desplazamiento; crecimiento vegetal corto.
Todos los recursos externos tendrán autor, procedencia, licencia y requisitos de atribución registrados.

### 3.5 Recursos mínimos
Una protagonista; un animal; árboles, rocas, plantas y troncos modulares; obstáculos de arroyo; elementos de refugio; variantes de cada zona; iconos; efectos de interacción, crecimiento y recuperación; ambiente y música con licencia compatible.

## 4. Implementación técnica
### 4.1 Herramientas propuestas
Unity con una versión estable común para los tres; fijar número exacto antes de crear el proyecto. Plantilla 3D con URP, C#, Blender para recursos, Figma/Canva para diseño y presentación, Git con soporte LFS para archivos grandes, Trello para seguimiento.
Input System para entrada. Cinemachine es opcional y se evaluará en el prototipo de cámara. Las versiones de paquetes se fijarán según compatibilidad con el editor elegido.

### 4.2 Sistemas
| Sistema | Responsabilidad | Prioridad | Complejidad estimada |
|---|---|---|---|
| Movimiento y cámara | Desplazarse, saltar, mirar y evitar obstáculos de cámara | Alta | Media |
| Interacción | Detectar objetivo, validar alcance y ejecutar E | Alta | Baja |
| Restauración | Mantener estados por zona y emitir eventos | Alta | Media |
| Puzles | Validar condiciones del arroyo, claro y refugio | Alta | Media |
| Crecimiento | Activar plantas únicamente en puntos válidos | Alta | Media |
| Objetivos e interfaz | Mostrar objetivo, pistas y progreso | Alta | Baja |
| Fauna | Activar animal y recorrido al recuperar refugio | Media | Baja |
| Audio y cambios visuales | Responder al estado de las zonas | Media | Media |
| Pausa, reinicio y cierre | Control de sesión y victoria | Alta | Baja |

Arquitectura propuesta: controlador de jugador independiente de la interacción; componentes de puzle que informan al estado de zona; eventos de recuperación que actualizan arte, audio y objetivos. Evitar un único script que controle todo.
Escenas propuestas: menú y bosque; el final puede ser una secuencia dentro del bosque.
Prefabs para vegetación, puntos de interacción y zonas. Configuración serializada para ajustar parámetros sin modificar código.

### 4.3 Integración del equipo
Carpetas previstas: Scenes, Scripts, Prefabs, Art, Audio y UI.
Mantener archivos .meta en control de versiones; excluir Library, Temp, Logs y Builds. Usar serialización de texto y evitar editar simultáneamente la misma escena.
Tú integras código y escenas; Juan entrega prefabs probados en una escena de arte; Diego valida nivel, objetivos y experiencia.
Ramas cortas y revisiones antes de integrar. Acordar quién edita cada escena al comenzar la sesión.

### 4.4 Rendimiento y calidad
Objetivo provisional: 30 FPS estables a 1080p en la PC de prueba del equipo; registrar CPU, GPU y RAM antes de validar. Medir en una compilación, además del editor.
Limitar sombras, partículas y vegetación; reutilizar materiales y medir con Profiler antes de optimizar.
Criterios de entrega: recorrido completo jugable, tres zonas recuperables, ausencia de bloqueos, caída con recuperación segura, menú/pausa/reinicio funcionales, audio ajustable, créditos y compilación que abra en otra PC Windows.
Pruebas técnicas: progreso sin activaciones duplicadas, reinicio limpio y secuencia final solo tras completar las tres zonas.

## 5. Equipo y producción
Reparto: tú has elegido programación. Se propone Juan para arte y Diego para pruebas, con apoyo de diseño y documentación para cubrir el trabajo restante.
| Persona | Rol principal | Entregables | Revisión cruzada |
|---|---|---|---|
| Tú | Programación Unity e integración | Movimiento, cámara, interacción, puzles, estados, UI, rendimiento y build | Viabilidad técnica |
| Juan | Arte 3D, animación y audio | Guía visual, modelos o recursos licenciados, prefabs, animaciones, iconos, sonidos y créditos | Coherencia y legibilidad |
| Diego | QA, diseño de niveles y documentación | Casos de prueba, registro de errores, GDD, guion, mapa, niveles en bloques y presentación | Jugabilidad y claridad de objetivos |

El rol de Diego combina pruebas con apoyo al diseño, especialmente al inicio. Puede montar niveles con prefabs sin programar sistemas. Las revisiones son compartidas.
Plazo indicado por el usuario: tres semanas desde el 6 de noviembre de 2026. Entrega objetivo: viernes 27 de noviembre de 2026.
Las fechas de Trello se planifican a las 18:00 de Lima y son metas de revisión del hito, no estimaciones de duración individuales.

| Periodo | Hito y resultado |
|---|---|
| 6–9 de noviembre | H1: alcance, guion, mapa y guía visual revisados |
| Hasta el 13 de noviembre | H2: primera zona jugable con formas simples |
| 14–20 de noviembre | H3: tres zonas conectadas y recorrido completo |
| 21–27 de noviembre | H4: interfaz y audio, pruebas, correcciones y entrega |

En la primera semana Juan crea el kit visual mientras se valida el arroyo; Diego prueba cada entrega de programación desde el principio. Reservar los últimos tres días para corregir y empaquetar.
Si al 13 de noviembre la primera zona no funciona, reducir la entrega a un arroyo completo y un final breve; los otros dos hábitats pasan a ampliación. Si un sistema amenaza el plazo, simplificar crecimiento a un único punto de activación y usar recursos licenciados coherentes.

## 6. Estructura del tablero
Tablero: https://trello.com/b/rv1JFCL7/bosque-de-regreso-gdd-y-desarrollo-unity-3d
Listas: Guía y GDD → Por hacer → En curso → En revisión → Terminado.
Cada tarea contiene responsable, hito, dependencia, entregable, aceptación y verificación.
Máximo recomendado: una tarjeta en curso por persona. Una tarea pasa a Terminado después de revisión cruzada y evidencia.
Los prefijos [Tú], [Juan] y [Diego] identifican responsabilidades en texto. No se han asignado tarjetas a cuentas ni invitado compañeros porque sus perfiles Trello aún no se han identificado.

## 7. Referencias y pendientes
- Plantilla GDD FigJam proporcionada: https://www.figma.com/board/UxWlvDCrafihgv2clOFEP8/Template-GDD-for-Universidad-Continental-students--Community-?node-id=0-1
  Se observaron los bloques Concepto del juego, Mecánicas de juego, Narrativa y arte e Implementación técnica. Este documento adapta esa estructura.
- Presentación anterior Marea Alta proporcionada: https://canva.link/5tc0guaui2kfwpt
  Se identificaron las referencias Beyond Blue, Dave the Diver y Hungry Shark World, además de la relación anterior con ODS 14 y 7. Son antecedentes del proyecto anterior; no se trasladan automáticamente a Bosque de regreso.
- ODS 15 y metas oficiales: https://sdgs.un.org/goals/goal15
- Unity, introducción al Input System: https://learn.unity.com/tutorial/setting-up-the-input-system

Pendientes: experiencia de Juan y Diego, sus perfiles Trello, hardware, versión del editor, confirmación de sus roles propuestos, identidad visual y referencias específicas de fauna/vegetación.
