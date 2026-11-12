# Encargo para Claude Code
Lee primero AGENTS.md, Docs/COORDINACION-IA.md, Docs/CONTRATOS.md y GDD-Bosque-de-regreso-v0.1.md.

Tu papel es desarrollo de niveles y herramientas de editor, interfaz visual y pruebas de aceptación. Codex mantiene gameplay e integración.

## Primer encargo: C-01
Construye un arroyo de prueba con primitivas, sin depender de scripts de gameplay todavía inexistentes.
Implementa una herramienta de editor en Game/Assets/_Bosque/Editor/LevelTools/ que genere una escena en Game/Assets/_Bosque/Levels/Arroyo/ArroyoGreybox.unity.

La escena debe incluir:
- Punto de inicio, recorrido corto, cauce y tres obstáculos localizables.
- Zona de salida y puntos seguros.
- Colliders y escala de metros coherente.
- Marcadores con los nombres del contrato.
- Grupos DegradedVisuals y RestoredVisuals separados. RestoredVisuals empieza inactivo.
- Luz y materiales provisionales compatibles con URP.

No crees controlador de jugador, cámara jugable, interacción, estado de restauración ni una segunda implementación de esos sistemas.
No agregues paquetes ni cambies ProjectSettings. No generes archivos .unity o .prefab inventando GUID a mano: usa las APIs de editor de Unity.
La herramienta debe crear o actualizar solo su salida administrada y no borrar contenido ajeno.

## Pruebas y entrega
Crea Docs/QA/ARROYO-ACEPTACION.md con pasos, resultado esperado y espacio para registrar resultado real. Incluye caída, cámara, interacción fuera de alcance, pulsaciones repetidas, salida bloqueada antes de restaurar y reinicio.
Entrega en Docs/Handoffs/CLAUDE.md: archivos modificados, cómo ejecutar la herramienta, dimensiones del nivel, pruebas realizadas, incidencias y necesidades de integración.

Ejecuta Unity solo cuando Codex no esté usando Game/. Si el editor está ocupado, entrega el código de editor y deja la ejecución pendiente de verificación.
Tu primer encargo no exige implementar UI ni nuevos sistemas. Espera los contratos de gameplay antes de conectar HUD o pausa.
