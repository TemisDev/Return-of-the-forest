# RC1 — paquete de evaluación para Juan y Diego

Código de la candidata: **8838ff5c74038a5399d15b141e30a6b60e5fc949**. Unity 6000.3.22f1, URP, Windows x86-64, **no Development**. Objetivo de revisión RC1: 22 de noviembre; entrega final: 27 de noviembre. El alcance de esta candidata es únicamente Arroyo dormido.

## Paquete y ejecución

- Carpeta local: `<repositorio>/Builds/Windows/RC1/`.
- EXE: `BosqueDeRegreso.exe`. No separar sus DLL ni la carpeta de datos.
- Paquete preparado: `<repositorio>/Builds/Windows/BosqueDeRegreso-RC1-8838ff5-Win64.zip`.
- [Tamaño, hashes, manifiesto y verificación del ZIP](../Evidence/RC1/package.json).
- [Metadatos de compilación](../Evidence/RC1/build-info.json) y [resultados de las pruebas](../Evidence/RC1/QA/EVIDENCIAS/pruebas.json).

Extraer el ZIP en una carpeta nueva y abrir el EXE normalmente, sin argumentos de verificación. No hace falta abrir Unity para jugar. Los ejecutables no se suben a GitHub ni se publica una release automática; compartir la carpeta o ZIP completo por el medio que acuerde el equipo.

Controles: WASD para desplazarse, ratón para mirar, Espacio para saltar, E para retirar el objetivo cercano. Acercarse desde la orilla, sin subirse al bloqueo. R reinicia; F1 abre/cierra instrucciones. Escape libera el cursor y un clic lo vuelve a capturar. Escape/F1 no pausan y el progreso no se guarda al cerrar.

Tres obstáculos llevan el contador de 0/3 a 3/3; se recupera el arroyo y se habilita el arco de salida. Cruzarlo completa el nivel y presenta el mensaje ODS 15. Los botones finales reinician o cierran. La restauración inmediata es una simplificación fantástica: el texto final explica que la recuperación real requiere tiempo, cuidado y conocimiento local.

## Juan — aceptación artística

Usar los formatos de `Docs/Art/Entregable05/` en una copia identificada como RC1-8838ff5. [Comparativa real](../Evidence/RC1/Art/COMPARATIVA.html), [revisión técnica por AV01–AV12](../Evidence/RC1/Art/REVISION-VISUAL.csv) y [correcciones](../Evidence/RC1/Art/REGISTRO-CORRECCIONES.csv) preparan la inspección, sin sustituir tu firma.

Revisar materiales, escalas, rocas y juncos, siluetas del camino/obstáculos, agua/espuma desde varios ángulos, sombras, lectura del personaje, letrero del arco y contraste del HUD. Comprobar ambos estados desde el mismo encuadre. El auditor estructural da 95 entradas sin alertas; no equivale a aceptación artística ni verificación de licencias. Confirmar también procedencia/créditos de los recursos existentes.

El único cambio visible de este ciclo es la explicación de E en los controles iniciales. No se añadieron props ni se redujo calidad gráfica. En la comparativa se conservan ausentes las capturas base de spawn y cámara que no estaban registradas con ese encuadre. No rellenarlas con arte conceptual ni con imágenes de otra candidata.

Registrar defectos reproducibles con prioridad, captura/ángulo, esperado/obtenido, archivo y commit. Corregir recursos propios conservando .meta/GUID; coordinar cualquier cambio de escena/HUD con Codex. Cada corrección exige nueva build identificada y repetición de los casos afectados.

## Diego — aceptación funcional y UX

Usar los 41 casos V4 y los 17 controles V5 vinculados por sus referencias, sin sumarlos como casos independientes. Las copias para esta candidata están en [QA/](../Evidence/RC1/QA/). Los APROBADO técnicos indican su modo y alcance; no son pruebas manuales realizadas por ti ni aprobación del usuario.

Repetir sobre el EXE normal los nueve criterios históricos: movimiento/salto, cámara, E, tres retiradas únicas, contador 3/3, restauración, desbloqueo de salida, reinicio y ausencia de errores. Añadir E fuera de rango/repetida/durante salto, reinicio tras 1/3 y tras 3/3, caída antes/después, foco/cursor, F1 y botones finales. Revisar todas las rampas, vegetación junto a cámara y pantalla completa. La aprobación V2 no certifica RC1.

Registrar CPU/GPU/RAM, resolución, calidad, versión de Windows, commit de la build y hashes. Los scripts V5 de tu carpeta sirven para registrar una sesión humana y su cierre; no juegan por el participante. Conservar los resultados previos y usar un nombre de sesión nuevo. Revisar y anonimizar Player.log antes de compartirlo.

Para UX, una persona ajena al desarrollo debe iniciar y terminar sin explicación verbal de la solución. Registrar dudas y ayudas (U01), tiempo real de primer recorrido (U02), respuesta espontánea sobre qué cambió (U03) e interpretación de iconos (U04). No convertir tiempos de la patrulla automática en tiempos de un jugador nuevo. [Formulario de cierre](../CIERRE-QA-V4.md), aplicado a esta build y conservando su identificación.

## Observaciones que deben evaluarse

- Hay una cápsula provisional y faltan audio/animaciones finales. No se han añadido nuevas zonas ni una protagonista definitiva durante esta fase.
- I-01 conserva la protección de no retirar el soporte estando encima; la prueba automática pasa, repetir físicamente.
- I-02/I-03/I-04 conservan su diseño: rampas/salto para salir del cauce, tronco opcional caminable y respawn inicial sin checkpoints.
- I-05: la cámara puede quedar a unos 0,26 m junto a muros en ciertas orientaciones. Las pruebas no detectan collider entre foco/cámara; juzgar si la cercanía impide jugar o produce molestias.
- Los reportes de rendimiento conservan picos y percentiles. Las medias fueron 221–245 FPS, pero hubo frames de hasta 56,42 ms en recorrido. Las transiciones repetidas tuvieron máximos 36,07 / 8,26 / 37,47 ms. No se garantiza 60 FPS en cada frame; RC1-PERF-01 es observación P2 y G16 requiere decisión de fluidez/riesgo antes del pase final. No se atribuye una causa sin un perfil preciso; la instrumentación y el escritorio también forman parte de las condiciones medidas.
- La sonda C02 antigua registra un fallo al intentar salir sin restaurar y aun así devuelve código 0. Es una expectativa obsoleta anterior a T08/T09; usar la suite actual y el contrato de barrera. No marcar el nivel como roto ni la sonda como totalmente aprobada por su exit code.

La prueba en otra PC física sigue pendiente de disponibilidad. La verificación del paquete en esta misma PC no la sustituye.

## Decisión

Acta de Juan: **PENDIENTE**. Aceptación manual de Diego: **PENDIENTE**. UX con jugador nuevo: **PENDIENTE**. Riesgos no críticos aceptados por coordinador: **PENDIENTE**. Aprobación de entrega definitiva: **NO EMITIDA**.

El objetivo actual es evaluar esta candidata concreta. No fusionar automáticamente el PR ni crear una release pública al completar un script.
