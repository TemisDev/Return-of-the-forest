# Informe QA integral V16 — Bosque de regreso

**Documento de trabajo, NO una aceptación.** Este paquete contiene matrices preparadas a partir del encargo V16. Ninguna prueba de este paquete se ha ejecutado desde aquí. Diego debe identificar su trabajo y sustituir los campos pendientes **solo con observaciones reales**.

## 1. Identificación del encargo

| Campo | Valor |
|---|---|
| Responsable humano | Diego — QA y documentación |
| Base solicitada | `feature/valle-olvidado` — `f09f186263b21c7754645f264cefe891ccadb713` |
| Rama de entrega | `feature/qa-v16-diego` |
| Motor indicado | Unity 6000.3.22f1 / URP |
| Proyecto | `Game/` |
| Build base esperada | `Builds/Windows/V16-Sonido-Solidez/` **(transferir carpeta completa)** |
| Build posterior a Juan | **PENDIENTE DE RECIBIR** — no reutilizar ni renombrar la base |
| Guía de ramas | `GUIA-RAMAS-Y-PR.md` — revisar desde el repositorio real |
| Plantilla fuente mencionada | `MATRIZ-QA-V16-PLANTILLA.csv` — revisar e incorporar al llegar al repositorio |
| Autor de las pruebas manuales | **SIN REGISTRAR** |
| Resultado global | **NO EJECUTADO / SIN DICTAMEN** |

### Datos de la build y del equipo (completar en cada campaña)

| Campo | BASE V16 | REGRESIÓN después de Juan |
|---|---|---|
| Build ID / carpeta recibida | PENDIENTE | PENDIENTE |
| `build-info.json`: commit declarado | PENDIENTE | PENDIENTE |
| Commit de código al que se compara | PENDIENTE | PENDIENTE |
| SHA-256 de `BosqueDeRegreso.exe` | PENDIENTE | PENDIENTE |
| Fecha de ejecución / Lima | PENDIENTE | PENDIENTE |
| Versión de Unity verificada | PENDIENTE | PENDIENTE |
| Resolución / modo pantalla | PENDIENTE | PENDIENTE |
| CPU / GPU / RAM | PENDIENTE | PENDIENTE |
| Cargador conectado / plan de energía | PENDIENTE | PENDIENTE |
| API gráfica | PENDIENTE | PENDIENTE |
| Salida de audio / volumen | PENDIENTE | PENDIENTE |
| Aplicaciones concurrentes | PENDIENTE | PENDIENTE |
| Persona que ejecutó | PENDIENTE | PENDIENTE |

**Regla de trazabilidad:** si cambia el binario o sus datos, crear un nuevo identificador de build. Cambiar el nombre de una carpeta no demuestra nueva versión. No copiar una cifra histórica a V16.

## 2. Alcance y estado

- EditMode, PlayMode y `V16PlayModeTests`: **NO EJECUTADO**. Consultar `RESULTADOS-AUTOMATIZADOS.md` y adjuntar XML reales.
- Campaña **menú → Arroyo → Claro → Refugio → Valle → final**: **NO EJECUTADO**.
- Solidez de árboles, rocas, tocones y troncos en las cuatro zonas: **NO EJECUTADO**.
- Inventario con **Tab e I** en **las cuatro zonas**, especialmente **Claro**: **NO EJECUTADO**.
- Combinaciones de inventario, mapa, pausa, cambio de zona y reinicio: **NO EJECUTADO**.
- Escucha humana con auriculares, transiciones y reinicio: **NO EJECUTADO**.
- FPS por zona y estado, p95 y cierres D3D12: **NO EJECUTADO**. No se dispone aquí de reserva de PC ni resultados nuevos.
- Jugadores sin conocimiento previo (hasta 3): **NO EJECUTADO**.
- Regresión **posterior a los recursos de Juan**: **NO EJECUTADO**; requiere build nueva y comprobación de hash.

## 3. Huecos conocidos de la automatización que deben cubrirse manualmente

1. `InventoryOpensInEveryZoneAndShowsWhatYouCarry` no cubre **Claro** en la revisión descrita en el encargo. No aprobar CL07 basándose en ese nombre.
2. `MusicStartsSombreAndCrossfadesWhenRestored` comprueba un cambio de estado final inmediato y **no** acredita por sí mismo calidad perceptiva del fundido gradual. No aprobar AUD02 o los casos de escucha basándose solo en API.
3. Un recorrido automatizado por API no acredita teclado físico, ratón, comodidad, descubrimiento sin pistas ni escucha.
4. El final después de Refugio es un comportamiento **obsoleto**: V16 necesita llegar al **Valle y finalizar allí**.

## 4. Seguimiento de resultados

Los casos iniciales residen en `MATRIZ-BASE-V16.csv`. La campaña posterior a Juan se registra **separadamente** en `MATRIZ-REGRESION-V16.csv` y no sustituye el registro base. El vínculo entre ellas es `Caso_base_ref`. Las incidencias confirmadas van en `INCIDENCIAS-V16.csv` y llevan pasos reproducibles, frecuencia y evidencia.

Estados permitidos: **NO EJECUTADO**, **APROBADO**, **FALLIDO**, **BLOQUEADO**, **PARCIAL**. Una prueba mixta solo queda APROBADA si se completan tanto la parte manual como la automatizada que exige el caso. La ausencia de archivos o de una medición supone BLOQUEADO o NO EJECUTADO según el caso, nunca APROBADO.

## 5. Criterios de rendimiento

- Criterio general: objetivo **60 FPS** con **p95 de frame time ≤ 16,67 ms**, por zona y por estado (degradado y restaurado) con medición real.
- Valle: mínimo histórico **30 FPS de media**, registrado **por separado**. Superarlo no da aprobación general ni garantiza p95.
- Se exige señalar duración de muestra, API, estado de zona, modo de pantalla, resolución, equipo, cargador y procesos concurrentes.
- `-medir-fps 30` espera **6 s** y registra la vista activa durante **30 s**; **no** restaura la zona ni recorre automáticamente.
- Atajos disponibles indicados: `-zona claro`, `-zona refugio`, `-zona valle`. **No inventar** `-zona arroyo`.
- Cierres D3D12: realizar hasta cinco ejecuciones y preservar códigos/logs; `-force-d3d11` es comparativa diagnóstica, no recomendación automática de API por defecto.

## 6. Dictamen final condicionado

**Estado a la fecha de preparación:** PENDIENTE DE EJECUCIÓN. No se declara aptitud para integración ni publicación con este paquete inicial.

Al finalizar, Diego debe completar:

| Puerta | Condición | Estado |
|---|---|---|
| Trazabilidad | Hash+commit+build-info de las dos builds | NO EJECUTADO |
| Automatizadas | XML actual y resultados explícitos, sin reducir aserciones | NO EJECUTADO |
| Funcional | Campaña completa y controles en cuatro zonas | NO EJECUTADO |
| Integridad visual | Colisiones y presentación verificadas en juego | NO EJECUTADO |
| UI/audio | Claro con Tab/I, UI combinadas y escucha real | NO EJECUTADO |
| Rendimiento | p95 y media según criterios explícitos | NO EJECUTADO |
| Cierre | Cinco intentos de cierre D3D12, cuando sea viable | NO EJECUTADO |
| Usuarios | Número real de participantes y observaciones | NO EJECUTADO |
| Pos-Juan | Nueva build con hash; repetir pruebas afectadas | NO EJECUTADO |

**Conclusión de Diego (rellenar tras ejecutar):** PENDIENTE.

**Firmado/validado por Diego:** PENDIENTE — no sustituir por firma de IA.

## 7. Límites y fuentes

El encargo V16 proporcionado por coordinación determina casos y criterios. El paquete **no** incluye la `MATRIZ-QA-V16-PLANTILLA.csv` ni la `GUIA-RAMAS-Y-PR.md` del repositorio, que Diego debe leer y usar para conciliar nombres/IDs. El informe QA del 21/11 puede servir de antecedente, **no** de ejecución ni aceptación V16. Los resultados históricos 23/23, 38/38 y 48/48 son referencias y no deben trasladarse como resultados actuales.

Fecha objetivo base: **24–25/11/2026**. Fecha objetivo regresión/dictamen: **26/11/2026 18:00 Lima**. No fusionar PR automáticamente.
