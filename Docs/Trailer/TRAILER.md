# Tráiler de Bosque de regreso (V16)

- **Archivos:** `Builds/Trailer/BosqueDeRegreso_Trailer_4K.mp4` (3840×2160, 30 fps, H.264 ~72 Mb/s) y `BosqueDeRegreso_Trailer_1080p.mp4` (~25 Mb/s). Se codifican desde un máster CRF 15.
- **Duración:** 29 s.
- **Audio:** AAC 320 kb/s a -14 LUFS, con picos de -1 dBTP (estándar para web y redes).

## Guion

| Tiempo | Toma (`TrailerCapture.cs`) | Narración (es-PE, voz neuronal Camila) |
|---|---|---|
| 0,0–4,2 | **S1** La Guardiana camina por la orilla del cauce seco | «Hace años, este bosque andino era un corredor de vida.» |
| 4,2–8,0 | **S2** Vuelo sobre el valle degradado: lagunas turbias, árboles secos | «El abandono lo secó, y la vida se fue apagando.» |
| 8,0–13,0 | **S3** Retira el tronco y el agua vuelve al arroyo; la grúa abre el plano | «Ahora, una guardiana regresa. Devuelve el agua al cauce,» |
| 13,0–15,8 | **S4** El último brote crece y el claro reverdece | «hace brotar las plantas nativas,» |
| 15,8–18,4 | **S5** El zorro cruza el prado hacia la madriguera; la Guardiana observa | «y abre un refugio para que la fauna vuelva.» |
| 18,4–22,6 | **S6** La Guardiana cruza el puente sobre el Mayu restaurado | «Limpia el valle, río por río.» |
| 22,6–29,0 | **S7** Vuelo sobre las lagunas restauradas y título | «Porque un bosque no se reemplaza. Se recupera. Bosque de regreso.» |

- **Música:** la del juego. *Cathedral in the forest* (congusbongus, CC0) suena hasta la restauración del arroyo, y después pasa por fundido cruzado a *Forest Whisper* (Cleyton Kauffman, CC0).
- **Ambiente:** viento seco al principio; después, agua y aves.
- **Efectos:** retirar el tronco, restauración, brote y nivel completado.
- **Mezcla:** la voz baja la música y el ambiente (compresión de cadena lateral).
- **Imagen:** etalonado suave (contraste y saturación), viñeta ligera, fundidos de 0,4 s entre tomas y fundido a negro al final.
- **Título:** Alegreya SC, la tipografía del mapa.

## Cómo regenerarlo

1. **Tomas** (Unity, unos 10 min). Se graban fotograma a fotograma a 4K con el tiempo de juego fijo, así que no hay tirones. Salen en `output/trailer/raw/<toma>/`, que no se versiona.
   ```
   Unity.exe -batchmode -projectPath Game -runTests -testPlatform PlayMode -testFilter TrailerCapture -testResults r.xml
   ```
   Una toma suelta se repite con `-testFilter "TrailerCapture\.Refugio$"` (o `Arroyo`, `Claro`, `Valle`).
2. **Voz:** edge-tts 7.3.1, voz `es-PE-CamilaNeural`, `--rate=-8%`, una frase por archivo. Después se recorta el silencio con ffmpeg (`silenceremove`) a `v1.wav` … `v9.wav`.
3. **Montaje:**
   ```
   python -I Docs/Trailer/montar_trailer.py <raiz_repo> <carpeta_voz> Builds/Trailer
   ```

## Notas

- **Plano del zorro:** la cámara elige sola la posición con más visibilidad del recorrido (trazado de rayos), porque la madriguera está rodeada de rocas.
- **DaVinci Resolve:** está instalado, pero la versión gratuita no se puede automatizar desde fuera (eso requiere Studio). El montaje con ffmpeg es equivalente en calidad. Si se quiere retocar a mano, las tomas en `output/trailer/raw` se pueden importar en Resolve como secuencias de imágenes a 30 fps.
