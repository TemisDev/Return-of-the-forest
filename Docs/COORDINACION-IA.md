# Reparto Codex y Claude Code
Inicio de desarrollo: 11 de noviembre de 2026. Entrega objetivo: 27 de noviembre.
El reparto de IA complementa los roles humanos: tú lideras programación; Juan arte; Diego pruebas y apoyo al diseño.

## Bloques
| Bloque | Responsable IA | Entregable |
|---|---|---|
| Base técnica e integración | Codex | Proyecto Unity, versión fija, paquetes, Git y compilaciones |
| Gameplay | Codex | Movimiento, cámara, interacción, puzles, restauración, crecimiento y sesión |
| Nivel y herramientas | Claude Code | Arroyo en bloques y generador de escena, luego claro y refugio |
| Interfaz | Claude Code, después del contrato | HUD y vistas de menú/pausa; Codex mantiene estado y comandos de sesión |
| Verificación | Claude Code prepara; Diego valida | Casos de aceptación, reproducción de fallos y revisión de regresiones |
| Arte | Juan, con apoyo de Claude | Recursos compatibles, escala, materiales, prefabs y licencias |
| Entrega | Codex integra; Claude documenta | Build, controles, incidencias y guía |

## Archivos
Todas las rutas Unity siguientes dependen de Game/Assets/_Bosque/.
- Codex: Runtime/Player, Runtime/Interaction, Runtime/World, Runtime/Session, Runtime/Contracts; Editor/CoreSetup; Scenes/Integration.
- Claude: Editor/LevelTools, Levels, Runtime/UI, Tests/Acceptance.
- Art compartido con Juan: Claude propone recursos en Levels/Arroyo/ArtProvisional. La sustitución final se revisa con Juan.
- Cada asistente escribe únicamente su propio archivo Docs/Handoffs/CODEX.md o CLAUDE.md.
- Codex integra y fija los .asmdef cuando sean necesarios. Claude propone su necesidad antes de alterar ensamblados.

## Orden de arranque
1. Codex termina T05 y documenta editor, paquetes y comprobación de apertura.
2. Codex inicia T06: cápsula controlable, salto y cámara.
3. Claude ejecuta C-01: T07, herramienta y escena del arroyo sin gameplay. Puede escribir código de editor mientras Codex programa archivos distintos.
4. Codex verifica y conecta escena, interacción y recuperación: T08 y T09.
5. Diego ejecuta T10 con los casos preparados por Claude.
6. Con el arroyo validado, ampliar hacia claro/refugio y conectar UI.

No se considera ejecutado el encargo de Claude hasta recibir archivos y evidencia de su sesión. Esta conversación no controla ni inicia Claude Code.

## Contratos
Consulta Docs/CONTRATOS.md. El primer corte usa marcadores de escena, no APIs de gameplay inventadas.
Para UI, Codex publicará primero eventos y comandos de sesión. Claude no controla Time.timeScale ni implementa un segundo GameManager.

## Control de integración
- Un solo proceso de Unity importa/ejecuta Game/ cada vez.
- Las operaciones Git se serializan. En esta primera fase se comparte la copia con propiedad de archivos; no se cambian ramas mientras otro asistente escribe.
- Cada entrega enumera sus archivos y pruebas. Codex revisa y hace el commit de integración.
- Para trabajo simultáneo prolongado, separar copias de Git con su propio Library y coordinar integración. No arrancar dos Unity sobre una misma ruta.
- No subir ni publicar repositorio, build o recursos sin una instrucción específica.

## Hitos vigentes
- 13 de noviembre: primera zona jugable. Quedan dos días desde este arranque.
- 20 de noviembre: recorrido de tres zonas, si el primer corte está validado.
- 27 de noviembre: entrega.
Se mantiene la contingencia del GDD: si el arroyo no funciona el 13, reducir el alcance a una zona completa con final breve. No se marca H1 como terminado por haber pasado su fecha.

## Trello
https://trello.com/b/rv1JFCL7/bosque-de-regreso-gdd-y-desarrollo-unity-3d
Los títulos conservan responsables humanos. Las descripciones identifican el apoyo IA. No se reasignan cuentas ni se marcan tareas terminadas sin evidencia.
