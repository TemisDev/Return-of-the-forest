# Bosque de regreso
Prototipo Unity 3D para PC sobre recuperación de hábitats y ODS 15.

## Abrir el proyecto
En Unity Hub, usa Add y selecciona la carpeta **Game**. Editor: **6000.3.22f1**.
Abre **Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity** y pulsa Play.
WASD mueve, el ratón gira la cámara y Espacio salta. Escape libera el cursor y clic izquierdo lo captura de nuevo. Escape todavía no pausa.
El arroyo y el personaje usan formas provisionales. Interacción y recuperación del agua siguen pendientes.

## Dependencias de la base importada
- Universal Render Pipeline: 17.3.0.
- Input System: 1.20.0.
- Unity Test Framework: 1.6.0.
La fuente de versiones es Game/Packages/manifest.json y su resolución en packages-lock.json. Codex mantiene esos archivos.

## Desarrollo con dos asistentes
- Codex: base, gameplay y compilaciones.
- Claude Code: herramientas de nivel, interfaz y pruebas dentro de su carpeta.
- Juan: arte. Diego: validación y apoyo al diseño. Tú: programación y coordinación.

Lee [AGENTS.md](AGENTS.md), [reparto de trabajo](Docs/COORDINACION-IA.md) y [contratos](Docs/CONTRATOS.md).
Para comenzar en Claude Code, abre esta carpeta y utiliza [el encargo inicial](Docs/ENCARGO-PARA-CLAUDE.md).
Tras C-01, utiliza [el encargo C-02](Docs/ENCARGO-PARA-CLAUDE-C02.md) para revisar el recorrido integrado.
No ejecutes dos instancias de Unity sobre Game/.

## Primeras entregas
1. Codex: T05, base y control de versiones; después T06, movimiento y cámara.
2. Claude: C-01, herramienta que genere ArroyoGreybox y casos de aceptación.
3. Codex: interacción y recuperación del arroyo.
4. Diego: validación del primer recorrido jugable.

## Referencias y seguimiento
- [GDD](GDD-Bosque-de-regreso-v0.1.md).
- [Plan y tareas](tasks/plan.md).
- [Trello](https://trello.com/b/rv1JFCL7/bosque-de-regreso-gdd-y-desarrollo-unity-3d).
- Dirección visual y fuentes en assets/. Las imágenes son arte conceptual, no modelos 3D.

## Git
Repositorio local para este juego, rama inicial codex/bootstrap-game. No hay remoto configurado ni publicación.
Game/Library, cachés y registros quedan fuera del repositorio. Se conservan .meta. Los recursos binarios del juego usarán Git LFS.
Antes de añadir un remoto y compartir, los tres integrantes deben tener Git LFS instalado y usar la misma versión de Unity.
