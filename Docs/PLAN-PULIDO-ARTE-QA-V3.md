# Pulido Arte/QA V3 — 18 de noviembre de 2026

## Auditoría
- Rama feature/arroyo-visual-integration, copia limpia, develop df6830a ya contenido en la rama.
- Arte bbca565 y QA 9535f78 no estaban integrados en develop. El usuario autorizó fusionar ambas ramas únicamente en la rama de trabajo. Se conservaron los commits visuales previos; develop y main no se modifican.
- Línea base ejecutada: 24/24 pruebas aprobadas.
- Arte V3 aporta tres OBJ sin UV, cuatro texturas de entorno, cuatro iconos y una textura RGBA de partículas. No trae materiales/prefabs V3 ni un efecto configurado.
- QA V3 aporta matrices propuestas, no resultados ejecutados. No se reescriben sus archivos; la ejecución de Codex se registra aparte con los mismos IDs y límites de cobertura.

## Implementación
1. Crear materiales/prefabs derivados en Scenes/Integration, conservando originales y GUIDs de Arte.
2. Usar color mate en meshes sin UV; texturas de ribera en superficies con UV, con mosaico a escala de metros. No asignar una textura estirada a los OBJ.
3. Sustituir solo montajes visuales de obstáculos; añadir helechos y grupos de roca fuera del camino y de la orilla de interacción.
4. Integrar iconos en el HUD existente, respetando proporciones y consumiendo el mismo progreso. Sin nuevo controlador de estado.
5. Efecto de restauración breve, con un máximo de 24 partículas, escuchando StateChanged; no se repite al finalizar ni deja residuos al reiniciar.
6. Conservar física, contratos y T08/T09; comprobar pruebas, render, build Windows y recorrido automatizado de la build, señalando qué requiere validación humana.

Objetivo general del proyecto: 27 de noviembre. El plan de QA propone revisión el 23; no equivale a aceptación obtenida.
