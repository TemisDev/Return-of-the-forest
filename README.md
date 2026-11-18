# Bosque de regreso
Prototipo Unity 3D para PC sobre recuperación de hábitats y ODS 15.

## Abrir el proyecto
En Unity Hub, usa Add y selecciona la carpeta **Game**. Editor: **6000.3.22f1**.
Abre **Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity** y pulsa Play.
Entrega vigente: **V4**, en `feature/arroyo-final-polish`. [Integración, evidencias y pasos para QA](Docs/ENTREGABLE-04-INTEGRACION-Y-PULIDO.md). Build local Windows Development: **Builds/ArroyoV4/BosqueDeRegreso.exe**; conservar completa la carpeta. F1 muestra ayuda; al completar aparecen el mensaje ODS 15 y los botones de reinicio y salida. La ayuda no pausa. La aceptación manual para RC1 sigue pendiente.
WASD mueve, el ratón gira la cámara y Espacio salta. **E** retira el obstáculo cercano y **R** reinicia. Escape libera el cursor y clic izquierdo lo captura de nuevo. Escape todavía no pausa.
Retira los tres obstáculos desde la orilla (Z 10, 18 y 26). A 3/3 vuelve el agua y se habilita la salida; crúzala para completar. No se puede retirar un obstáculo estando encima.
El personaje sigue siendo una cápsula provisional. El entorno usa geometría del prototipo y modelos/texturas del kit de Arte. [Contrato de integración y QA](Docs/INTEGRACION-ARROYO-V2.md).
La integración artística V3 utiliza los prefabs de Arte V2, con estados de suelo/vegetación, ribera y barrera visual. [Comparación y verificación del Entregable 03](Docs/ENTREGABLE-03-INTEGRACION-VISUAL.md).
Build visual anterior: **Builds/ArroyoV3/BosqueDeRegreso.exe** (conservar completa la carpeta).
Pulido Arte/QA V3 del 18/11: **Builds/ArroyoV3Polish/BosqueDeRegreso.exe**, con todas sus carpetas y DLL. Incluye los recursos V3 y los iconos del HUD. [Informe, comparaciones y QA pendiente](Docs/ENTREGABLE-03-PULIDO-Y-VALIDACION.md).
Build local Windows del Entregable 02: **Builds/ArroyoV2/BosqueDeRegreso.exe**. Mantener juntos el EXE y sus carpetas de datos. La build no está versionada en Git.

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
- [Entregable 02: cambios, pruebas y limitaciones](Docs/ENTREGABLE-02-RESULTADOS.md).
- [GDD](GDD-Bosque-de-regreso-v0.1.md).
- [Plan y tareas](tasks/plan.md).
- [Trello](https://trello.com/b/rv1JFCL7/bosque-de-regreso-gdd-y-desarrollo-unity-3d).
- Dirección visual y fuentes en assets/. Las imágenes son arte conceptual, no modelos 3D.

## Git
Repositorio del juego: https://github.com/TemisDev/Return-of-the-forest. Base de integración `develop`; `main` se conserva. El trabajo V4 se entrega mediante PR para revisión, sin fusión automática.
Game/Library, cachés y registros quedan fuera del repositorio. Se conservan .meta. Los recursos binarios del juego usarán Git LFS.
Antes de añadir un remoto y compartir, los tres integrantes deben tener Git LFS instalado y usar la misma versión de Unity.
