# Informe técnico previo a aceptación RC1

**Candidata generada para evaluación; aceptación definitiva NO EMITIDA.** Código 8838ff5c74038a5399d15b141e30a6b60e5fc949, Unity 6000.3.22f1, Windows x64 no Development.

La [suite vigente](EVIDENCIAS/pruebas.json) tiene 38/38 aprobadas, cero omitidas. La [puerta RC1](PUERTA-RC1.csv) registra **7 aprobaciones técnicas acotadas, 1 bloqueo de aceptación y 9 controles sin ejecución humana completa**. Las aprobaciones identifican explícitamente API/inspección técnica y no son firmas de Diego. Los 41 casos V4 se conservan vinculados en MATRIZ-V4-RC1.csv, con su parte manual pendiente.

El resumen original de Diego, ejecutado sobre estas copias, devuelve **NO APTA PARA ACEPTACIÓN** por controles P0/P1 todavía pendientes y G16 bloqueado. Eso impide el pase final; no significa que la build no exista o que haya fallado su recorrido automático. No se altera el resultado del resumen para presentar una aceptación inexistente.

G16 tiene tres mediciones equivalentes completas con RAM y tiempos reales, pero requiere decisión sobre RC1-PERF-01 (P2): picos intermitentes durante la verificación. Sus medias no garantizan 60 FPS en cada frame. La causa no se atribuye al nivel sin un perfil preciso; el verificador y las aplicaciones de escritorio también forman parte de las condiciones medidas. Evaluar la fluidez de la partida normal antes del pase final.

La sonda C02 conserva 21 OK / 1 FALLA / 9 INFO: su R-01 intenta salir sin restaurar y no corresponde al contrato actual. Además, devuelve exit=0 aunque registra ese fallo. Se conserva como RC1-TOOL-01 P3; la suite vigente comprueba la barrera y la salida después de 3/3. No se presenta como un nuevo bloqueo del producto ni se oculta el resultado.

La apertura normal sin verificador fue observada y capturada. Las pulsaciones de la herramienta de escritorio no produjeron un cambio verificable de F1/cierre; ese intento no aprueba entradas físicas ni diagnostica su causa. No se verificó el código de salida de esa sesión y no se suma a los cierres normales. Los cierres normales 0 de las verificaciones automáticas tienen evidencia independiente.

Pendientes: WASD/Espacio/E/R/F1 con foco, botones finales y cierre por interfaz, progresión del HUD en un recorrido humano, cámara/vegetación/rampas, caída en Windows, UX con participante nuevo, juicio artístico y otra PC si está disponible. La aprobación V2 no se traslada a esta build.

Ver [incidencias](INCIDENCIAS-RC1.csv), [métricas](RENDIMIENTO-RC1.csv), [históricas](REVALIDACION-HISTORICA.csv), [acta técnica de Arte](../Art/ACTA-ARTE-RC1.md) e [instrucciones para el equipo](../../../Handoffs/RC1-PARA-JUAN-Y-DIEGO.md).

Decisión de Diego/coordinador: **PENDIENTE**. No fusionar el PR ni publicar una release definitiva por el resultado de un script.
