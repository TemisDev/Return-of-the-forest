# Zona 4: El valle olvidado

**Rama:** `feature/valle-olvidado`, que sale de `feature/presentation-menus` e incluye el Bloque 05.

**Build:** `Builds/Windows/V12-Rios/BosqueDeRegreso.exe` (V12 añade los ríos, la brújula y el minimapa; la V11 anterior sigue en `Builds/Windows/V11-Valle`).
- Partida completa: menú → arroyo → claro → refugio → **valle** → final.
- Para entrar directamente en el valle: `-zona valle`.

## La idea

Es un escenario grande y abierto. Hay que limpiar **todo** el valle, basura y troncos caídos, y solo cuando todo está limpio y separado el valle revive. Para que no sea fácil hay que gestionar la bolsa, separar los residuos, hacer trabajo físico con los troncos, explorar y saltar.

## El mapa

- **Tamaño:** unos **210 × 210 m** jugables sobre un Terrain de Unity de 320 m, rodeado de montañas de 50–70 m que no se pueden escalar.
  - Cruzarlo corriendo lleva unos 45 s.
  - La niebla deja ver unos 150 m.
- **Relieve:** suave en general, un cerro de 13 m al sureste y una red de ríos y lagunas (ver abajo).
- **Vegetación:** 392 árboles (secos al empezar) con colisión, ichu (pasto seco) en el terreno y rocas.
- **Campamento:** en el centro, donde empieza la Guardiana, con un punto de reciclaje.

| Sector | Dónde | Qué tiene | Dificultad |
|---|---|---|---|
| **La quebrada** (NE) | Las tres lagunas y el curso alto del Mayu | Residuos flotando en las lagunas y en el Mayu; troncos gruesos que tapan los canales entre lagunas | Troncos de 3,6 s; hay que cruzar el Mayu y vadear |
| **El bosque** (NO) | Bosque denso (190 árboles) y el curso alto del río Chico | Residuos escondidos entre árboles y en la orilla del Chico; un tronco grueso en el río | Visibilidad: conviene usar el rastreo |
| **La pampa** (SO) | Llanura abierta, el bofedal y el río del Sur | Un **basural** con llantas, bolsas y latas, un residuo en el bofedal y otros sueltos; un tronco en el río del Sur | Las llantas ocupan 3 espacios; mucho recorrido |
| **El mirador** (SE) | El cerro | Residuos en las laderas y **dos en lo alto de salientes de roca** | Hay que subir saltando escalones de 0,9 m |

**Total:** 40 residuos y 16 troncos o ramas, 14 por sector.

## Ríos y lagunas (V12)

El diseño sigue el mapa de referencia que pasaste (los grandes ríos y lagos de Norteamérica), adaptado al valle:

| En la referencia | En el valle | Cómo es |
|---|---|---|
| Misisipi (de norte a sur) | **Río Mayu** | Río principal: baja de las montañas del norte, cruza el valle por el este del campamento y sale por el sur. **Hondo**: solo se cruza por **2 puentes** o **4 vados**. |
| Misuri (afluente del oeste) | **Río Chico** | Llega del oeste por el borde del bosque y desemboca en el Mayu al sur del campamento. Poco profundo: se vadea; tiene 1 puente. |
| Grandes Lagos + San Lorenzo | **Laguna Alta, Laguna del Medio y Laguna Baja** + **río de la Salida** | Cadena de tres lagunas al noreste, alimentada por una vertiente del norte; cada una más baja que la anterior, unidas por canales, y desaguan al este. Centro hondo, orillas vadeables. |
| Río Grande / Colorado (suroeste) | **Río del Sur** | Nace en un **bofedal** (humedal andino) de la pampa y baja al suroeste. Poco profundo. |

- **El área:** todo el valle jugable (unos 210 × 210 m); los ríos siguen dentro de las montañas del borde, que no se pueden cruzar.
- **Agua honda:** la Guardiana vadea hasta la cintura y va más lenta cuanto más hondo; donde pasa de unos 95 cm no avanza (como una pared) y aparece una pista: «Cruza por un puente o un vado». No cuenta como intento fallido.
- **Puentes:** de troncos, con barandas (no se cae al agua). **Vados:** piedras que asoman en el agua baja.
- **El agua cambia:** turbia y marrón mientras el sector está sucio, más clara al limpiarlo y cristalina cuando el valle revive.
- **Sonido:** el río suena siempre (más fuerte al revivir) y el sonido viene del agua más cercana.
- **Residuos en el agua:** 6 flotan en lagunas, ríos y bofedal (se alcanzan vadeando) y 5 troncos tapan canales y ríos.
- Todo el agua se genera desde una sola descripción (`ValleWater`): el constructor talla el terreno y crea la malla, y el juego la usa para la profundidad, el sonido y el mapa.

## Orientación: brújula, minimapa y mapa (V12)

- **Brújula** (arriba, al centro): puntos cardinales que giran con la cámara, los **3 puntos de reciclaje** (el más cercano con su nombre y distancia) y los **3 residuos o troncos ubicados más cercanos** con su distancia. Si algo queda detrás, se marca en el borde.
- **Minimapa** (abajo a la derecha): mapa redondo que gira con la cámara, la Guardiana en el centro, la **N** en el borde, los puntos de reciclaje, puentes, vados y lo ubicado. Debajo dice cuántos objetivos ubicados faltan.
- **Ubicar:** lo que pasa a menos de **24 m** de la Guardiana, o lo que marca el rastreo (T), queda ubicado y aparece en la brújula, el minimapa y el mapa. Lo no visto **no** aparece, para que siga habiendo que explorar; el mapa dice cuántos faltan por ubicar en cada sector.
- **Mapa (M):** ahora muestra los nombres de los ríos y lagunas, puentes y vados, lo ubicado y una rosa con el norte.

## Mecánicas y dificultad

1. **Recoger (E):** la bolsa tiene **8 espacios** y una **llanta ocupa 3**. Con la bolsa llena aparece una pista y hay que ir a vaciarla.
2. **Separar:** hay **3 puntos de reciclaje** (campamento, quebrada y pampa), cada uno con cuatro contenedores con los colores de la norma peruana NTP 900.058:
   - **Plástico:** blanco;
   - **Vidrio y metal:** amarillo;
   - **Papel y cartón:** azul;
   - **No aprovechable:** negro.

   E en un contenedor deposita solo los residuos de su tipo. Si no llevas de ese tipo, aparece una pista y cuenta como intento fallido.
3. **Troncos (mantener E):** ramas 1,6 s, tronco 2,4 s y tronco grueso 3,6 s, con barra de progreso. Si sueltas o te alejas, el avance se va perdiendo.
4. **Rastreo (T):** durante 6 s marca con columnas de luz lo pendiente en 40 m y lo deja ubicado en la brújula y los mapas. Se recarga en 20 s.
5. **Mapa (M):** mapa ilustrado con relieve, ríos y lagunas con su nombre, árboles, puntos de reciclaje, puentes, vados, lo ubicado, tu posición y el avance de cada sector. Pausa el juego mientras está abierto.
6. **Agua:** el Mayu y el centro de las lagunas son hondos; hay que planificar la ruta por puentes y vados.
7. **Cronómetro y medallas:** oro en menos de 12 min y plata en menos de 18. El tiempo, la medalla y los intentos fallidos se muestran en el final.

## Recuperación

- **Al recoger un residuo o apartar un tronco:** queda una mancha de pasto en ese lugar.
- **Al completar un sector:** el sector reverdece en 3 s.
  - El suelo pasa a pasto, el ichu a pasto verde y los árboles a su versión verde.
  - Aparecen flores y mariposas.
  - El filtro gris del valle se aclara un 20 %.
- **Con los cuatro sectores limpios y todo depositado:**
  - el valle entero revive: cielo, luz, aves y música;
  - **el agua queda cristalina** y corre más;
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
| Ríos y lagunas (modelo compartido) | `Runtime/World/Valle/ValleWater.cs` |
| Vadeo y agua honda | `Runtime/World/Valle/WaterWading.cs` (usa `ThirdPersonMotor.MoveBlocked`) |
| Color del agua por sector | `Runtime/World/Valle/ValleWaterLook.cs` |
| Sonido del río | `Runtime/World/Valle/WaterSoundAnchor.cs` |
| Brújula y minimapa | `Runtime/UI/NavigationViews.cs` |
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
| Z4-M10 | Intentar cruzar el Mayu por lo hondo; después buscar un puente o vado con el mapa. | No se puede por lo hondo, la pista se entiende y el cruce se encuentra. |
| Z4-M11 | Jugar 5 minutos solo con la brújula y el minimapa (sin abrir el mapa). | Se sabe dónde está el norte, el reciclaje más cercano y lo ya visto. |
| Z4-M12 | Recoger los residuos que flotan en las lagunas. | Se alcanzan vadeando sin quedar atascado. |
| Z4-M13 | Mirar el agua de un sector antes y después de limpiarlo. | El agua pasa de turbia a clara y se nota. |

## Limitaciones conocidas

- **Árboles:** el valle usa los árboles V6 (copas y troncos genéricos), no queñuas modeladas.
- **Filtro gris:** mientras no esté todo limpio, el valle sigue algo grisáceo. Cada sector lo aclara un 20 % y la recuperación completa lo quita.
- **Escalones del mirador:** son rocas V6 escaladas sobre cajas de colisión. Funcionan, pero de cerca se ven como un pilar de roca.
- **Rendimiento:** no se midió el valle restaurado en build (tiene más pasto verde y flores). Ver Z4-M08.
