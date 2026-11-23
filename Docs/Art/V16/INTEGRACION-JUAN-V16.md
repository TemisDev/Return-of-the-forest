# Integración de propuestas de Juan — V16

**Estado:** SIN ASSETS NUEVOS NI CAMBIOS PROPUESTOS AÚN. No integrar nada basándose únicamente en esta plantilla.

## Contratos de integración

- Rama de Juan: `feature/arte-v16-juan` desde commit `f09f186263b21c7754645f264cefe891ccadb713` de `feature/valle-olvidado`.
- El integrador **no** debe aplicar variantes sin evidencia J1/J3, ubicación exacta, procedencia y comprobación de referencia.
- Carpeta permitida para nuevos recursos: `Game/Assets/_Bosque/Art/Entregas/JuanV16/`, preservando `.meta`/GUID y dependencias.
- Las escenas jugables, `Runtime/UI`, TMP compartidas, código de inventario, audio/buses y colliders **no son alcance de Juan**.
- Si una variante cambia silueta, escala o pivote, **solicitar al integrador revisar el collider asociado**; no declarar compatibilidad por apariencia.
- No extrapolar efectos sobre FPS: medir en build identificada si la integración tiene impacto.

## Solicitudes para el integrador (máximo 3)

| ID | Zona/objeto y coordenada | Problema y captura | Ruta de variante en JuanV16 | Ruta de referencia actual a sustituir | Propósito | Dependencias/licencia | Riesgos | Comprobación necesaria | Estado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| V16-J-01 | NO DETERMINADO | NO OBSERVADO | NO APLICA | NO DETERMINADA | — | — | — | Validar visual, escala/pivote, URP y colisión con nueva build | SIN PROPUESTA |
| V16-J-02 | NO DETERMINADO | NO OBSERVADO | NO APLICA | NO DETERMINADA | — | — | — | Validar visual, escala/pivote, URP y colisión con nueva build | SIN PROPUESTA |
| V16-J-03 | NO DETERMINADO | NO OBSERVADO | NO APLICA | NO DETERMINADA | — | — | — | Validar visual, escala/pivote, URP y colisión con nueva build | SIN PROPUESTA |

## Posibles necesidades de UI (no modificar código)

| Vista | Resolución | Captura | Solicitud al integrador | Estado |
| --- | --- | --- | --- | --- |
| Inventario | 1280×720 / 1920×1080 | Pendiente | Solo si se observa defecto | NO EVALUADO |
| Mapa | 1280×720 / 1920×1080 | Pendiente | Solo si se observa defecto | NO EVALUADO |
| Reciclaje/señalética | 1280×720 / 1920×1080 | Pendiente | Solo si se observa defecto | NO EVALUADO |

## Audio (recomendaciones sin tocar el runtime)

Completar desde `REVISION-AUDIO-V16.csv`; añadir referencias a momentos, archivos y grabaciones si están autorizadas. No sustituir música V16, no tocar buses ni fundidos.

## Validación posterior a la integración

1. Confirmar commit de Juan y ruta exacta de recursos aplicados.
2. Abrir la build identificada y comparar capturas antes/después, mismo lugar, cámara y resolución.
3. Revalidar materiales URP, referencias, escala, pivotes, visibilidad y colisiones afectadas.
4. Solicitar a Diego repetir las pruebas de QA que la variante pudiera afectar (incluida legibilidad/audio cuando aplique).
5. Registrar hash de build, fecha, evaluador y decisión de integración; no validar por el solo hecho de fusionar PR.

## PR

**Origen:** `feature/arte-v16-juan`  
**Destino:** `feature/valle-olvidado`  
**Estado:** NO ABIERTO POR ESTE PAQUETE. Adjuntar evidencia real, hasta 3 mejoras justificadas y una declaración expresa de qué carpetas fueron modificadas. **No auto-merge**.
