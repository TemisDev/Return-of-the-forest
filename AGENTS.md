# Bosque de regreso: instrucciones compartidas
Fecha de coordinación: 11 de noviembre de 2026.

## Objetivo
Prototipo Unity 3D para PC, un jugador y tercera persona. ODS 15. Primero un arroyo jugable; después claro y refugio. Fuente: GDD-Bosque-de-regreso-v0.1.md. Entrega objetivo: 27 de noviembre de 2026.

## Entrada
- Proyecto Unity: Game/.
- Editor acordado para esta base: 6000.3.22f1.
- Lee Docs/COORDINACION-IA.md y Docs/CONTRATOS.md antes de modificar archivos.
- Lee el último relevo de Docs/Handoffs/ del otro asistente.
- Claude Code debe leer también CLAUDE.md.
- El repositorio de este juego está en esta carpeta. No hagas commits en el repositorio padre Personal life.

## Responsabilidad de archivos
Codex: Game/Packages/, Game/ProjectSettings/, Runtime/Player/, Runtime/Interaction/, Runtime/World/, Runtime/Session/, Runtime/Contracts/, Editor/CoreSetup/, Scenes/Integration/ bajo Game/Assets/_Bosque/.
Claude: Editor/LevelTools/, Levels/, Runtime/UI/, Tests/Acceptance/ bajo Game/Assets/_Bosque/; Docs/QA/ y Docs/Handoffs/CLAUDE.md.
Juan lidera el arte y Diego valida pruebas y diseño. Una imagen de la presentación no es un modelo 3D utilizable en Unity.

## Colaboración
- No modifiques archivos del otro bloque sin acordarlo y registrar el cambio de alcance.
- El único integrador de Packages, ProjectSettings, escenas finales y builds es Codex.
- No cambies APIs compartidas por tu cuenta. Propón el cambio en tu relevo.
- No ejecutes git switch, reset, clean, stash ni operaciones destructivas durante trabajo compartido.
- No ejecutes dos editores o procesos batch de Unity sobre Game/ al mismo tiempo.
- No hagas operaciones Git simultáneas. Entrega archivos y avisa al integrador.
- Conserva todos los .meta de recursos existentes. Unity genera los nuevos.
- Usa una escena de nivel de Claude y una de integración de Codex, nunca ediciones concurrentes de la misma escena.
- Verifica en Unity antes de declarar que compila o funciona. Si solo revisaste texto, indícalo.

## Primer corte
T05: base común. T06: movimiento y cámara. En paralelo, Claude prepara T07 y una lista de pruebas para T10.
T08/T09 se integran después. No añadas combate, mundo abierto, inventario complejo, guardado persistente ni servicios externos.
