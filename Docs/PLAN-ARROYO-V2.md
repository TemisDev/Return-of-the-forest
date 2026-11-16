# Entregable 02 — plan técnico

Fecha: 16 de noviembre de 2026. Ciclo hasta el 22 de noviembre.
Base: develop actualizado (d060233 al iniciar), copia limpia, rama feature/arroyo-gameplay-v2.

## Auditoría inicial
T06 y C-02 existen; T08/T09 no están implementados. Arte y QA ya están incorporados en develop. Arte contiene cinco OBJ, texturas e iconos, no prefabs configurados. La matriz nueva de QA es una propuesta no ejecutada; no reemplaza las pruebas históricas originales.

## Orden
1. Contratos y pruebas de progreso/interacción con fallo demostrado.
2. InteractionTarget extensible y PlayerInteractor: distancia a superficie, selección del objetivo disponible más cercano, línea de visión, tecla E y revalidación al ejecutar.
3. RestorationProgress como estado lógico independiente de Unity; ArroyoSession coordina representación, salida, victoria y reinicio.
4. RemovableObstacle con ID estable y referencia serializada al collider. Mitigar I-01 rechazando interacción cuando el personaje está encima, sin modificar la geometría base.
5. Actualizar solo ArroyoPlayable mediante herramientas de editor: collider de salida completo, detector de llegada y HUD provisional bajo Runtime/Session. No editar Runtime/UI de Claude.
6. Crear materiales y conexiones visuales de integración que referencien el kit original sin modificarlo.
7. Pruebas de lógica, física e integración, seis pruebas anteriores, compilación/build, revisión y PR hacia develop. No fusionarlo.

## Contratos previstos
- Zona: arroyo. Obstáculos: arroyo.log, arroyo.rocks, arroyo.branches. Objetivo: arroyo.restore.
- InteractionTarget: Prompt, CanInteract(PlayerInteractor), TryInteract(PlayerInteractor).
- PlayerInteractor: Target, Origin, Scan(), TryInteract(); el intento revalida rango/visibilidad/disponibilidad.
- RestorationProgress: RemovedCount, TotalCount, IsRestored, IsCompleted; Changed y Restored; TryRemove(id), TryComplete(), Reset(). IDs desconocidos o repetidos no cuentan.
- ArroyoSession: estado Progress, evento StateChanged, Restart(), TryExit(actor); referencias serializadas a obstáculos, grupos de estado, barrera, personaje y spawn.
- Los visuales escuchan estado; no calculan progreso. La salida requiere restauración y llegada física del personaje configurado.

La guía final publicará firmas y parámetros reales tras la verificación. Pausa, crecimiento y otras zonas quedan fuera de este ciclo. Escape sigue liberando cursor; R reiniciará este nivel.
