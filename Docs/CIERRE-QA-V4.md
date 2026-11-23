# Cierre de aceptación V4 — para Diego y el usuario

Abrir `Builds/ArroyoV4/BosqueDeRegreso.exe` conservando toda su carpeta. En Unity, abrir `Game/` con 6000.3.22f1 y la escena `Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity`. Informe: `Docs/ENTREGABLE-04-INTEGRACION-Y-PULIDO.md`. No modificar las matrices originales para registrar pruebas que todavía no se realizaron.

## Recorrido de aceptación con teclado y ratón

Este formulario está **pendiente**. Las pruebas automáticas por API y eventos simulados no lo rellenan en nombre del jugador.

| Paso | Acción | Resultado esperado | Resultado real / evidencia |
|---|---|---|---|
| 1 | Iniciar. Caminar con WASD y saltar con Espacio | Movimiento y salto sin bloqueo; HUD 0/3 | PENDIENTE |
| 2 | F1 dos veces; Escape; clic dentro del juego | Ayuda abre/cierra; Escape libera cursor; clic lo recaptura | PENDIENTE |
| 3 | E lejos del primer bloqueo y mirando al suelo | No hay retirada fuera de alcance. Mirar al suelo por sí solo no cancela un objetivo cercano: se selecciona por proximidad y línea de visión | PENDIENTE |
| 4 | Acercarse por la orilla al primer bloqueo y pulsar E | Desaparece una vez; 1/3. Pulsaciones repetidas no duplican progreso | PENDIENTE |
| 5 | R tras 1/3 | Retorno al inicio, tres obstáculos, 0/3, agua degradada, salida cerrada | PENDIENTE |
| 6 | Retirar los tres bloqueos desde la orilla (Z 10, 18 y 26) | 0/3→1/3→2/3→3/3. Agua y vegetación cambian; efecto una vez; salida habilitada | PENDIENTE |
| 7 | Orbitar ratón junto al barranco, juncos, rocas y muros; recorrer rampas | Personaje y objetivos visibles; recorrido accesible. Registrar cercanía de cámara a cápsula como I-05 si resulta molesta | PENDIENTE |
| 8 | Revisar agua/espuma desde varios ángulos; probar pantalla completa | Sin parpadeo de superficies ni recorte del HUD; registrar resolución | PENDIENTE |
| 9 | Cruzar el arco final | Finalización, mensaje ODS 15 y botones visibles; controles de movimiento desactivados | PENDIENTE |
| 10 | Pulsar botón Reiniciar; repetir nivel; pulsar Salir | Reinicio limpio y cierre normal por botón | PENDIENTE |
| 11 | Relanzar, cerrar con X, relanzar otra vez | Cierre y arranque normales; comienza 0/3 | PENDIENTE |
| 12 | Revisar consola del editor y Player.log tras el recorrido | Sin errores rojos ni excepciones | PENDIENTE |

No se cambió el diseño de los pendientes conocidos: salir del cauce entre rampas requiere salto (I-02), tronco opcional subible caminando (I-03), respawn inicial sin checkpoints (I-04), cámara cerca de la cápsula junto a muros (I-05). Registrar aceptación o incidencia reproducible; no dar por corregidos sin evidencia.

## UX con una persona que no participó en el desarrollo

Sin explicarle la solución, registrar:

- U01: ¿puede empezar y terminar sin ayuda? Dudas y ayudas necesarias: **PENDIENTE**.
- U02: tiempo real del primer recorrido, inicio/fin e interrupciones: **PENDIENTE**.
- U03: respuesta espontánea a «¿Qué cambió cuando recuperaste el arroyo?»: **PENDIENTE**.
- U04: respuesta a «¿Qué significan los iconos de la pantalla?»: **PENDIENTE**.

Participante (identificador sin datos personales): **PENDIENTE**. Fecha: **PENDIENTE**. Build/commit: **PENDIENTE**. CPU/GPU/RAM/resolución: **PENDIENTE**.

## Decisión para RC1

Responsable de QA: **PENDIENTE**. Incidencias bloqueantes abiertas: **PENDIENTE**. Pendientes aceptados expresamente con justificación: **PENDIENTE**. Aceptación de RC1: **PENDIENTE**.

No fusionar el PR ni publicar una release de forma automática por ejecutar estas pruebas.
