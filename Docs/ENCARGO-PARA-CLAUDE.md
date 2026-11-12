# Mensaje para iniciar Claude Code
Abre Claude Code en la carpeta Bosque de regreso y pega el siguiente mensaje.

---

Vamos a desarrollar Bosque de regreso, un prototipo 3D en Unity sobre ODS 15. Trabajas en colaboración con Codex.

Lee AGENTS.md, CLAUDE.md, Docs/COORDINACION-IA.md, Docs/CONTRATOS.md y Docs/Handoffs/CODEX.md. Revisa el GDD antes de proponer cambios de diseño.

Ejecuta únicamente C-01: crear una herramienta de editor que genere el nivel ArroyoGreybox y documentar las pruebas de aceptación. Sus requisitos y rutas están en CLAUDE.md.

Codex mantiene el proyecto y los sistemas de gameplay. No modifiques sus carpetas ni Packages/ProjectSettings. No crees un controlador, GameManager, sistema de interacción o restauración alternativos.

Primero inspecciona Game/ProjectSettings/ProjectVersion.txt y Game/Packages/manifest.json. Trabaja con esa versión y dependencias. Si Game/ aún está importándose o Unity está ocupado, no abras otra instancia sobre la misma carpeta. Puedes preparar tus archivos de editor, pero deja constancia de lo no ejecutado.

Entrega archivos concretos dentro de tus rutas autorizadas. Usa las APIs de editor para generar escenas. No inventes GUID ni escribas una escena YAML a mano. No borres contenido ajeno ni cambies de rama de Git mientras Codex trabaja.

Al terminar, actualiza únicamente Docs/Handoffs/CLAUDE.md con cambios, instrucciones, verificaciones reales y problemas pendientes. No declares que probaste el juego sin haberlo ejecutado.
