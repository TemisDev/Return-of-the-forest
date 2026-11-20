# Acta técnica de revisión visual RC1

**Aceptación artística humana: PENDIENTE.** Revisión técnica de muestras por Codex; este documento no firma por Juan.

- Final Polish de referencia: 1afe557, integrado en develop por 5f60634. Las capturas de su build son de código e692340; el cierre posterior V4 añadió pruebas/documentación.
- Código exacto de candidata: 8838ff5c74038a5399d15b141e30a6b60e5fc949.
- Unity 6000.3.22f1 / URP / Windows x64 / no Development / calidad PC.
- Capturas nativas 1920×1080, contraste adicional 1280×800 y pantalla completa real según reporte. `normal-window.jpg` es una captura de la ventana normal, con su barra de título, no una captura nativa de framebuffer.
- Auditor de Juan ejecutado en copia aislada: 95 entradas, cero alertas estructurales. No certifica importación ni licencias por sí solo.
- [12 criterios con alcance](REVISION-VISUAL.csv): 7 verificados en muestra, 5 parciales. Firma/decisión de Juan pendiente en todos.
- Corrección efectiva: texto de E explícito en los controles iniciales, commit e5a3877. Sin modelos, colliders ni cambios de iluminación añadidos.

La comparación V4/RC1 usa posiciones aproximadamente equivalentes de las capturas disponibles. Spawn/cámara base no se registraron con los encuadres requeridos y permanecen FALTA en el comparador. El par estado_degradado_rc1 / estado_restaurado_rc1 sí usa igual posición y reinicialización de cámara en la misma candidata: jugador (3,0,8), giro 0 y cámara de gameplay existente. No son imágenes conceptuales ni se han retocado.

La cápsula y los límites del prototipo son visibles. No se han observado materiales rotos en las muestras, pero no se aprueban por inferencia todos los ángulos, escalas o transiciones. Revisar cámara junto a muros/vegetación y agua/espuma en movimiento. La prueba física no sustituye oclusión perceptiva: la cámara puede aproximarse a unos 0,26 m en orientaciones de la sonda histórica.

La procedencia de los recursos existentes se registra en los inventarios de Arte y su guía MVP los describe como originales del paquete. No se añadieron recursos de terceros en este ciclo. Juan debe confirmar licencias/créditos y emitir su decisión sobre la candidata identificada.

Decisión de Juan (APROBADO / CONDICIONADO / RECHAZADO): **PENDIENTE**. Defectos visuales P0/P1 humanos: **sin determinar por el revisor**. Riesgos P2/P3 aceptados: **PENDIENTE**.
