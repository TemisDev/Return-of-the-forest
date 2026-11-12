# C-02: revisión del recorrido con T06 integrado

Lee AGENTS.md, CLAUDE.md, Docs/CONTRATOS.md y Docs/Handoffs/CODEX.md.
Objetivo: preparar la validación humana de movimiento y cámara del arroyo; todavía no implementar interfaz.

## Trabajo
1. Revisa Assets/_Bosque/Scenes/Integration/ArroyoPlayable.unity sin guardar cambios. Coordina el uso de Unity: nunca ejecutar dos procesos sobre Game/.
2. En tus archivos de Docs/QA/, añade casos de WASD relativo a cámara, diagonales, salto simple, colisiones, cámara junto a paredes, caída por el borde abierto, retorno seguro y liberación/recaptura del cursor.
3. Si puedes probar Play Mode, registra acciones y resultados. Si no, deja los casos pendientes para Diego. No declares una prueba manual basándote en lectura de código o resultados automatizados.
4. Revisa si se puede recorrer el nivel y alcanzar los tres obstáculos. Propón ajustes medidos de geometría. No modifiques la escena de integración ni el controlador. Si cambias tu escena de nivel, registra los cambios y pide reintegración a Codex.
5. Completa o deja pendiente A03: comprobar que la generación conserva contenido ajeno, usando una copia de prueba, nunca la escena integrada.
6. Actualiza tu relevo Docs/Handoffs/CLAUDE.md y tus documentos de QA con resultados, rutas e incidencias reproducibles.

## Límites
- No cambiar Packages, ProjectSettings, Runtime/Player o Scenes/Integration.
- No crear UI, contratos de interacción, recuperación de agua, pausa o reinicio: Codex debe publicar primero los contratos.
- No ejecutar operaciones Git simultáneas ni un segundo Unity sobre Game/.
- No regenerar ni sobrescribir la integración, ni generar sobre una escena con trabajo sin guardar.

## Entrega
Informe breve: pruebas ejecutadas, aprobadas, fallidas y pendientes, con pasos para reproducir cada incidencia. Si no hay fallos encontrados, decir qué no se llegó a comprobar.
