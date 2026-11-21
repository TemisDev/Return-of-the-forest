# Zona 4: El valle olvidado

**Rama:** `feature/valle-olvidado`, que sale de `feature/presentation-menus` e incluye el Bloque 05.

**Build:** `Builds/Windows/V11-Valle/BosqueDeRegreso.exe`.
- Partida completa: menú → arroyo → claro → refugio → **valle** → final.
- Para entrar directamente en el valle: `-zona valle`.

## La idea

Es un escenario grande y abierto. Hay que limpiar **todo** el valle, basura y troncos caídos, y solo cuando todo está limpio y separado el valle revive. Para que no sea fácil hay que gestionar la bolsa, separar los residuos, hacer trabajo físico con los troncos, explorar y saltar.

## El mapa

- **Tamaño:** unos **210 × 210 m** jugables sobre un Terrain de Unity de 320 m, rodeado de montañas de 50–70 m que no se pueden escalar.
  - Cruzarlo corriendo lleva unos 45 s.
  - La niebla deja ver unos 150 m.
- **Relieve:** suave en general, un cerro de 13 m al sureste y un río seco que baja de las montañas del norte y sale por el este.
- **Vegetación:** 392 árboles (secos al empezar) con colisión, ichu (pasto seco) en el terreno y rocas.
- **Campamento:** en el centro, donde empieza la Guardiana, con un punto de reciclaje.

| Sector | Dónde | Qué tiene | Dificultad |
|---|---|---|---|
| **La quebrada** (NE) | El cauce seco del río | Residuos en el cauce y las orillas; troncos gruesos atravesados en el río | Troncos de 3,6 s; hay que bajar al cauce |
| **El bosque** (NO) | Bosque denso (190 árboles) | Residuos escondidos entre árboles; troncos en los claros | Visibilidad: conviene usar el rastreo |
| **La pampa** (SO) | Llanura abierta | Un **basural** con llantas, bolsas y latas, y residuos sueltos muy separados | Las llantas ocupan 3 espacios; mucho recorrido |
| **El mirador** (SE) | El cerro | Residuos en las laderas y **dos en lo alto de salientes de roca** | Hay que subir saltando escalones de 0,9 m |

**Total:** 40 residuos y 16 troncos o ramas, 14 por sector.

## Mecánicas y dificultad

1. **Recoger (E):** la bolsa tiene **8 espacios** y una **llanta ocupa 3**. Con la bolsa llena aparece una pista y hay que ir a vaciarla.
2. **Separar:** hay **3 puntos de reciclaje** (campamento, quebrada y pampa), cada uno con cuatro contenedores con los colores de la norma peruana NTP 900.058:
   - **Plástico:** blanco;
   - **Vidrio y metal:** amarillo;
   - **Papel y cartón:** azul;
   - **No aprovechable:** negro.

   E en un contenedor deposita solo los residuos de su tipo. Si no llevas de ese tipo, aparece una pista y cuenta como intento fallido.
3. **Troncos (mantener E):** ramas 1,6 s, tronco 2,4 s y tronco grueso 3,6 s, con barra de progreso. Si sueltas o te alejas, el avance se va perdiendo.
4. **Rastreo (T):** durante 6 s marca con columnas de luz lo pendiente en 40 m. Se recarga en 20 s.
5. **Mapa (M):** mapa ilustrado con relieve, río, árboles, los puntos de reciclaje, tu posición y el avance de cada sector. Pausa el juego mientras está abierto.
6. **Cronómetro y medallas:** oro en menos de 12 min y plata en menos de 18. El tiempo, la medalla y los intentos fallidos se muestran en el final.

## Recuperación

- **Al recoger un residuo o apartar un tronco:** queda una mancha de pasto en ese lugar.
- **Al completar un sector:** el sector reverdece en 3 s.
  - El suelo pasa a pasto, el ichu a pasto verde y los árboles a su versión verde.
  - Aparecen flores y mariposas.
  - El filtro gris del valle se aclara un 20 %.
- **Con los cuatro sectores limpios y todo depositado:**
  - el valle entero revive: cielo, luz, aves y música;
  - **vuelve el agua del río**;
  - el **zorro de neblina** baja del bosque al río, seguido por la cámara;
  - después aparece la última nota del cuaderno y el **final del juego**.
- **El final:** ahora se muestra al terminar el valle, no el refugio, y resume las cuatro zonas.

## Recursos

| Recursos | Origen |
|---|---|
| 17 modelos: botellas, latas, bidones, galoneras, caja, bolsa de papel, bolsa de basura, llanta, dos troncos, ramas y contenedor | **Poly Haven, CC0**; autores en [LICENCIAS-ZONA-4.csv](LICENCIAS-ZONA-4.csv) y en los créditos del juego |
| Árboles, pasto, rocas, suelos y partículas | Kit V6 del proyecto |

**Proceso de los modelos de Poly Haven:**
1. `Docs/Valle/pipeline/descargar_polyhaven.py`: descarga a 1K.
2. `convertir_modelos.py` (Blender): reduce la malla (los troncos pasan de 100k a 7–9k triángulos), deja el pivote en la base y convierte las texturas para URP.

## Sistemas

| Pieza | Archivo |
|---|---|
| Residuo y tipos | `Runtime/World/Valle/LitterItem.cs` |
| Bolsa | `Runtime/World/Valle/LitterBag.cs` |
| Contenedor | `Runtime/World/Valle/RecycleBin.cs` |
| Tronco caído | `Runtime/World/Valle/FallenWood.cs` |
| Sector | `Runtime/World/Valle/ValleSector.cs` |
| Recuperación del terreno | `Runtime/World/Valle/TerrainRestorer.cs` (trabaja sobre una copia de TerrainData) |
| Rastreo | `Runtime/World/Valle/TrackingPulse.cs` |
| Zona | `Runtime/Session/ValleSession.cs` |
| Datos del HUD y mapa | `Runtime/Session/ValleHud.cs` (`IZoneHud`, `IZoneHudExtra`, `IZoneMap`) |
| Audio y gestos | `Runtime/Session/ValleAudioBridge.cs` |
| Refugio → valle | `Runtime/Session/RefugioZoneTransition.cs` |
| Filtro por sectores | `Levels/Valle/ValleAmbienceLink.cs` (ajusta `BosqueV6Ambience.partial`) |
| HUD lateral, barra de trabajo y mapa | `Runtime/UI/PresentationViews.cs` |
| Medición en builds | `Runtime/Session/FpsProbe.cs` (`-medir-fps N -salir`) |
| Construcción de la escena | `Editor/Levels/Valle/ValleBuilder.cs`: idempotente, `Bosque.LevelEditor.ValleBuilder.BuildAllBatch` |

## Resultados automatizados

Esto no sustituye a las pruebas manuales.

| Prueba | Resultado |
|---|---|
| PlayMode del valle (`VallePlayModeTests`) | **11/11** |
| PlayMode completo (Guardiana, claro, refugio, presentación y valle) | **45/45** |
| EditMode | **38/38** |
| FPS en la build V11, valle, 1080p, GTX 1650 (`-zona valle -medir-fps 25`) | **53,1 FPS** de media, p95 21,7 ms |
| FPS en la build V11, refugio, mismas condiciones | 81,7 FPS de media, p95 16,8 ms |

Qué cubren las pruebas del valle:
- **Trazado:** 4 sectores de 14, 3 puntos con los 4 contenedores, residuos de cada tipo, llantas de 3 espacios, nada enterrado y dos residuos en salientes.
- **Bolsa:** se llena (8) y rechaza con pista; una llanta no cabe con 2 espacios libres.
- **Contenedores:** cada uno solo acepta su tipo.
- **Troncos:** una pulsación no basta; mantener E avanza (con barra en el HUD) y soltar lo hace retroceder. Al terminar deja de bloquear y suena.
- **Sector limpio:** el suelo, los árboles y la decoración reverdecen solo en ese sector.
- **Salientes:** los escalones se suben saltando con el motor real.
- **Rastreo:** marca lo pendiente cercano y tiene enfriamiento.
- **Partida completa:** limpieza total y depósito de todo; revive el valle con río y sonido, el zorro completa la zona, aparecen la nota final y el final.
- **Reinicio:** deja todo como al principio.
- **Transición:** completar el refugio lleva al valle (y ya no al final).
- **Capturas:** [CAPTURAS/](CAPTURAS/).

## Pruebas manuales pendientes

Son para Diego. **No están ejecutadas.**

| ID | Pasos | Esperado |
|---|---|---|
| Z4-M01 | Partida completa del valle sin ayuda; anotar el tiempo. | Entre 10 y 20 minutos; ningún residuo inalcanzable. |
| Z4-M02 | ¿Se entiende la separación? Contar los intentos fallidos en los contenedores. | Pocos errores tras el primero; los rótulos se leen. |
| Z4-M03 | Llenar la bolsa con una llanta y ver la pista. | Se entiende por qué no cabe. |
| Z4-M04 | Apartar un tronco grueso soltando E a la mitad. | Se nota que el avance retrocede; la barra lo muestra. |
| Z4-M05 | Subir a las dos salientes del mirador. | Se puede con saltos normales; la cámara no estorba. |
| Z4-M06 | Usar T y M para encontrar los últimos residuos. | Ayudan sin resolver el juego solos. |
| Z4-M07 | Limpiar un sector completo. | El cambio de color y de los árboles se nota y motiva. |
| Z4-M08 | FPS en la PC de la presentación (`-zona valle -medir-fps 30 -salir`, mirar Player.log). | Al menos 30 FPS. |
| Z4-M09 | Pulsar R a mitad de partida. | Todo vuelve al estado inicial, incluido el cronómetro. |

## Limitaciones conocidas

- **Árboles:** el valle usa los árboles V6 (copas y troncos genéricos), no queñuas modeladas.
- **Filtro gris:** mientras no esté todo limpio, el valle sigue algo grisáceo. Cada sector lo aclara un 20 % y la recuperación completa lo quita.
- **Escalones del mirador:** son rocas V6 escaladas sobre cajas de colisión. Funcionan, pero de cerca se ven como un pilar de roca.
- **Rendimiento:** no se midió el valle restaurado en build (tiene más pasto verde y flores). Ver Z4-M08.
