# Arte V5 — revisión de la candidata RC1
**Proyecto:** Bosque de regreso · Unity 6000.3.22f1 / URP  
**Rama:** `feature/arroyo-art-v5` · Meta del ciclo: 24/11/2026 (propuesta).

## Estado y alcance
Final Polish V4 fue publicado según el coordinador, pero no se ha inspeccionado su commit ni la escena actual. RC1 no está disponible para esta revisión. V3/V4 ya aportan geometría, texturas, iconografía y herramientas de prefabs. Este paquete no introduce modelos adicionales ni modifica Unity.

El trabajo de esta entrega consiste en verificar las mejoras de V4 en una build concreta, documentar defectos comprobables y entregar correcciones selectivas. La revisión visual y las correcciones quedan pendientes de evidencia; el kit de revisión sí está incluido y preparado.

## Flujo
1. Acordar con Codex el SHA de Final Polish y el SHA de RC1, resolución y calidad. No comparar builds distintas sin identificarlas.
2. Ejecutar desde la raíz `py -3 Docs/Art/Entregable05/Tools/auditar_recursos.py`. El auditor solo lee Art y escribe `AUDITORIA-RECURSOS.csv`; detecta problemas estructurales, no valida importación ni estilo.
3. Capturar los encuadres de `GUIA-CAPTURAS.md`. Guardar los archivos con nombres acordados en `CAPTURAS/`.
4. Completar `REVISION-VISUAL.csv` y `REGISTRO-CORRECCIONES.csv` con defectos reproducibles, no hipótesis.
5. Ejecutar `py -3 Docs/Art/Entregable05/Tools/comparar_capturas.py` y abrir `COMPARATIVA.html` localmente. No incluye capturas de ejemplo ni aporta capturas inexistentes.
6. Corregir solo recursos propios bajo Art; conservar los `.meta` y GUID existentes. Cambios de escena, colliders, componentes de interacción, iluminación de escena o HUD se coordinan con Codex. Confirmar visualmente cada cambio.
7. Completar `ACTA-ARTE-RC1.md`, subir los archivos reales y abrir PR hacia develop. Si RC1 todavía no existe, publicar la preparación como tal.

## Prioridad de cierre
P0: recurso impide jugar o compilar. P1: material roto, objetivo ilegible, camino oculto o transformación no reconocible. P2: defecto visible no bloqueante. P3: cosmético. No crear nuevos recursos salvo que un defecto demostrado lo justifique.

Python 3 estándar; no instala dependencias. Los resultados de las herramientas no sustituyen Unity ni las pruebas de Diego. No versionar Builds/, Library/ ni logs masivos; revisar permisos y privacidad de las capturas.
