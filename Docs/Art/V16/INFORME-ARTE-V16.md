# Informe de arte V16 — Juan

> **Estado del documento:** PREPARADO PARA REVISIÓN. **No es una aprobación visual o sonora.**
> Las observaciones, capturas, mediciones y escuchas reales deben incorporarlas Juan en Unity y en la build V16; este paquete no contiene evidencia inventada.

## 0. Identificación y alcance

| Campo | Valor |
| --- | --- |
| Proyecto | Bosque de regreso |
| Responsable humano | Juan — arte 3D y presentación audiovisual |
| Fecha prevista | 24/11/2026, 18:00 (America/Lima) |
| Rama base | `feature/valle-olvidado` |
| Commit base exigido | `f09f186263b21c7754645f264cefe891ccadb713` |
| Rama de entrega | `feature/arte-v16-juan` |
| Motor | Unity 6000.3.22f1, URP |
| Zonas | Arroyo, Claro, Refugio y Valle |
| Arte existente que se conserva | Entorno realista; Guardiana Rocketbox; interfaz de cuaderno de campo |
| Guía obligatoria | `GUIA-RAMAS-Y-PR.md`, `AGENTS.md` y relevos del repositorio (leer antes de modificar) |

**Regla de alcance:** no modificar escenas jugables, `Runtime/UI`, fuentes TMP compartidas, lógica del inventario, colliders, audio buses, fundidos o código de audio. No incorporar arte antiguo ni cambiar la dirección visual actual. Hasta tres propuestas artísticas nuevas únicamente si están justificadas por evidencia real.

## J1. Inspección comparada — cuatro zonas

**Estado:** PENDIENTE DE EJECUCIÓN HUMANA. Recorrer cada zona en **degradado** y **restaurado**, en cámara y resolución comparables. Inspeccionar materiales URP, escala, contacto con suelo, vegetación, agua, charcos, puentes y señalética. V16 ya corrigió diversos elementos: verificar primero, no repetir correcciones por intuición.

| Zona | Degradado (archivo) | Restaurado (archivo) | Recorrido confirmado | Hallazgos asociados |
| --- | --- | --- | --- | --- |
| Arroyo | `EVIDENCIAS/ARROYO/arroyo_degradado.png` | `EVIDENCIAS/ARROYO/arroyo_restaurado.png` | NO EJECUTADO | Sin observaciones aún |
| Claro | `EVIDENCIAS/CLARO/claro_degradado.png` | `EVIDENCIAS/CLARO/claro_restaurado.png` | NO EJECUTADO | Sin observaciones aún |
| Refugio | `EVIDENCIAS/REFUGIO/refugio_degradado.png` | `EVIDENCIAS/REFUGIO/refugio_restaurado.png` | NO EJECUTADO | Sin observaciones aún |
| Valle | `EVIDENCIAS/VALLE/valle_degradado.png` | `EVIDENCIAS/VALLE/valle_restaurado.png` | NO EJECUTADO | Sin observaciones aún |

Completar `OBSERVACIONES-V16.csv` con zona, estado, objeto/coordenada, prioridad, captura, tipo (**DEFECTO**, **MEJORA** o **LIMITACIÓN**), causa probable si se verificó y propuesta. Si no hay defectos, dejarlo expresamente documentado tras la revisión; no inventar filas.

### Criterios mínimos por zona

- [ ] Materiales URP correctos, sin rosa ni referencias perdidas.
- [ ] Apoyo en el suelo y escala creíble de árboles, rocas, objetos y la Guardiana.
- [ ] Vegetación y transiciones de restauración coherentes, sin elementos suspendidos.
- [ ] Cauce, agua, charcos y reflejos sin cortes visuales inexplicados.
- [ ] Puentes, obstáculos y señalética legibles y alineados.
- [ ] Captura degradada y restaurada desde posición, FOV y resolución comparables.
- [ ] Cada defecto observado con evidencia localizable.

## J2. Hasta tres mejoras visuales pequeñas

**Estado:** NO SE HAN SELECCIONADO MEJORAS NI CREADO ASSETS. Seleccionar 0–3 mejoras **solo después** de las observaciones. Anotar en la tabla y en `INTEGRACION-JUAN-V16.md`. Si no hay defectos suficientemente demostrables, entregar revisión sin cambios de arte.

| ID | Defecto evidenciado | Recurso propuesto | Referencia afectada | Riesgo | Métricas reales | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| V16-J-01 | POR DETERMINAR | NINGUNO | POR DETERMINAR | POR DETERMINAR | NO MEDIDAS | SIN PROPUESTA |
| V16-J-02 | POR DETERMINAR | NINGUNO | POR DETERMINAR | POR DETERMINAR | NO MEDIDAS | SIN PROPUESTA |
| V16-J-03 | POR DETERMINAR | NINGUNO | POR DETERMINAR | POR DETERMINAR | NO MEDIDAS | SIN PROPUESTA |

Cuando se realicen cambios, conservar modelo/material/textura/prefab junto con `.meta` y dependencias bajo `Game/Assets/_Bosque/Art/Entregas/JuanV16/` (carpeta **solo condicional**, no incluida en este paquete). Registrar recuento real de materiales, geometría y resolución de texturas **si se midieron**. No deducir aumento de FPS a partir de mallas más simples.

**Aceptación técnica de cada variante:** importación Unity URP sin materiales rosa ni scripts/referencias perdidas; pivote y escala comprobados en escena temporal; destino de integración definido; no aplicarla directamente en escena jugable.

## J3. Inventario, mapa y rótulos de reciclaje

**Estado:** PENDIENTE DE REVISIÓN REAL. Probar en **1280×720** y **1920×1080**, capturando inventario, mapa y señalética. Completar `REVISION-UI-V16.csv` y poner capturas anotadas en `EVIDENCIAS/UI/`.

Validar contraste, jerarquía, truncamiento, recorte, tamaño aparente, significado de iconos y coherencia con el cuaderno de campo. Si hacen falta nuevos gráficos, registrar archivo, formato, dimensiones en píxeles, licencia, vista previa y destino en la guía de integración. Cambios a código UI, TMP o atlas deben quedar **solo como solicitud al integrador**.

## J4. Revisión estética del audio

**Estado:** ESCUCHA HUMANA PENDIENTE. Con auriculares, escuchar ambiente degradado → transición → restaurado en **al menos dos zonas representativas**, identificarlas. Registrar dispositivo, nivel de volumen, momento del problema, archivo si se identifica, severidad y recomendación en `REVISION-AUDIO-V16.csv`.

Comprobar particularmente el viento V16 (que no suene a ventilador/motor), la transición de ambiente y el tono de la música. No sustituir pistas, editar buses, fundidos ni lógica. Métricas automáticas de loudness **no sustituyen escucha**.

## J5. Licencias, trazabilidad e integración

**Estado:** REGISTRO PREPARADO; VERIFICACIÓN DE NUEVAS FUENTES PENDIENTE SI SE AÑADEN RECURSOS.** El encargo indica Rocketbox MIT, zorro con atribuciones CC-BY y fuentes de audios V16 registradas; verificar contra avisos reales existentes y preservarlos. Registrar **cada recurso nuevo** en `REGISTRO-RECURSOS-V16.csv`; no sustituir avisos existentes ni declarar revisión jurídica completa.

Para cada propuesta, `INTEGRACION-JUAN-V16.md` debe identificar ruta de recurso, destino propuesto, referencia a sustituir y comprobaciones en Unity. Si la forma visual cambia, advertir al integrador sobre posible revisión de collider; **no editar el collider**.

## Evidencias y limitaciones de esta entrega

- Este paquete es un **andamiaje de documentación**, no una ejecución visual o sonora V16.
- No se recibió un checkout de `f09f186263b21c7754645f264cefe891ccadb713` ni build Unity V16 en esta sesión.
- No hay imágenes reales, escucha con auriculares, mediciones de geometría/materiales/texturas ni licencia nueva que verificar todavía.
- Las capturas deben tomar las escenas/build reales, no imágenes ilustrativas, generadas o recicladas de versiones antiguas.

## Checklist de cierre — rellenar por Juan

- [ ] Inspeccioné **las cuatro zonas**, degradado y restaurado.
- [ ] Adjunté al menos **cuatro parejas** de capturas comparables (una por zona).
- [ ] Registré hallazgos diferenciando DEFECTO / MEJORA / LIMITACIÓN, o declaré revisión sin hallazgos.
- [ ] Entregué de **cero a tres** variantes justificadas, autocontenidas y con `.meta` cuando corresponde.
- [ ] Revisé inventario, mapa y señalética en **ambas resoluciones**, con capturas anotadas.
- [ ] Escuché con auriculares al menos dos zonas y documenté condiciones reales, o marqué pendientes explícitos.
- [ ] Registré procedencia, licencias y atribución de todos los nuevos recursos (si los hay).
- [ ] No modifiqué escenas jugables, lógica, colliders ni audio runtime.
- [ ] Abrí PR `feature/arte-v16-juan` → `feature/valle-olvidado`, con alcance y evidencias.
- [ ] Solicité repetir con Diego el QA afectado **después de la integración y nueva build identificada**.

**Decisión de Juan (pendiente de firma/evidencia):** SIN DICTAMEN. Firma/fecha: ______________.
