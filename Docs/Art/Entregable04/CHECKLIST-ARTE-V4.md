# Checklist de importación — Juan V4

> Estado inicial: NO VERIFICADO. Marcar solo después de revisar en Unity.

- [ ] Se importaron todos los OBJ y PNG sin errores.
- [ ] Unity generó `.meta` y se incluirán en el commit.
- [ ] Menú Arte V4 visible; generador ejecutado sin excepciones.
- [ ] Materiales URP funcionan sin magenta.
- [ ] Prefabs nuevos renderizan el mesh correcto y conservan escala 1u = 1m.
- [ ] Los materiales creados no sobrescribieron retoques existentes.
- [ ] La máscara alfa de espuma se evaluó visualmente y se configuró transparencia manual si era necesario.
- [ ] Los prefabs son decorativos y no tienen colliders ni scripts de gameplay.
- [ ] La cámara, saltos y raycast E no se ven obstaculizados (validación al integrar con Codex).
- [ ] Estado degradado/restaurado se distingue en escena real (capturas con mismo encuadre).
- [ ] No se editó la escena integrada ni recursos propiedad de Codex.
- [ ] Evidencia visual real adjunta a `CAPTURAS/` o al Pull Request.

## Comprobación de importación — 18 de noviembre de 2026
Unity 6000.3.22f1 ejecutado en modo batch en Windows. Importación y compilación completadas con código de salida 0. ArroyoArtV4Builder.Generate creó 4 materiales y 4 prefabs, con 0 modelos ausentes. Se incluyen los archivos .meta generados por Unity. La inspección visual en escena, la transparencia de la espuma y las pruebas de integración siguen pendientes.

