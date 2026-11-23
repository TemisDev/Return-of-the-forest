# Paquete para Juan: todo lo nuevo desde Arte V6

Fecha de preparación: 23 de noviembre de 2026.

## Qué es
Copia de todos los archivos de Unity añadidos o modificados **después del último entregable de Juan**:

- Desde: `42041da`, rama `feature/arroyo-art-v6`, 20-nov-2026. *"Arte: integra entorno realista, materiales PBR y recursos visuales V6"*.
- Hasta: `acf8d95`, rama `feature/valle-olvidado`, 21-nov-2026. *"feat(v14): interfaz cuaderno de campo y nueva Guardiana"*. Es la punta más reciente del repositorio.

Las rutas son las mismas que en el repositorio. Los binarios (FBX, PNG, WAV, OGG…) están completos, no como punteros de Git LFS.

## Cómo subirlo a tu rama
1. Haz checkout de tu rama. Lo ideal es que parta de `feature/arroyo-art-v6` o la contenga.
2. Copia **la carpeta `Game/` de este paquete sobre la carpeta `Game/` del repositorio**, aceptando reemplazar archivos. Haz lo mismo con `Docs/` si quieres las licencias.
3. **No** copies `LEEME.md`, `MANIFIESTO-SHA256.txt` ni `_Opcional/` a la raíz.
4. Abre Unity 6000.3.22f1 para que importe y comprueba que la consola no tenga errores de compilación.
5. Haz `git add Game Docs` y luego el commit. Los PNG, FBX y audios irán a LFS solos gracias al `.gitattributes`.

Si tu rama parte de `42041da`, el resultado de `Game/Assets` será idéntico al de `feature/valle-olvidado`. En ese tramo no se borró ningún archivo.

> **Alternativa sin copiar a mano:** desde tu rama ejecuta este comando. Hace exactamente lo mismo.
>
> ```
> git fetch origin feature/valle-olvidado
> git checkout origin/feature/valle-olvidado -- Game/Assets
> ```

## Contenido (816 archivos, 224 MB)
- 799 archivos son nuevos.
- 17 son modificaciones de archivos que ya existían en V6. Su `.meta` ya está en tu rama, por eso no viene repetido.
- La lista completa, con estado y SHA256, está en `MANIFIESTO-SHA256.txt`.

| Carpeta | Archivos | Tamaño | Qué contiene |
|---|---:|---:|---|
| `Game/Assets/_Bosque/Art` | 287 | 78.5 MB | `Valle/` (modelos, texturas y materiales de Poly Haven, terreno, agua, puentes, reciclaje), `Zonas/` (Claro y Refugio) y ajustes de `V6/` (agua, ambiente, colisiones QA6) |
| `Game/Assets/_Bosque/Characters` | 104 | 60.8 MB | `Guardiana/` (MPFB/MakeHuman y Rocketbox, animaciones y controlador) y `Fauna/` (zorro de neblina) |
| `Game/Assets/_Bosque/UI` | 57 | 66.8 MB | Fuentes y TMP Font Assets, sprites del cuaderno de campo, `Creditos.txt` |
| `Game/Assets/_Bosque/Audio` | 105 | 5.8 MB | Ambientes, música, SFX y `SFX_Biblioteca.asset` |
| `Game/Assets/_Bosque/Scenes` | 10 | 15.6 MB | `MenuPrincipal`, `Zonas/ClaroVacio`, `Zonas/RefugioPerdido`, `Zonas/ValleOlvidado` y `Integration/ArroyoPlayable` actualizada |
| `Game/Assets/_Bosque/Runtime` | 121 | 0.4 MB | Scripts de los que dependen las escenas y prefabs: habilidades, audio, personaje, UI, mundo y sesión |
| `Game/Assets/_Bosque/Editor` | 24 | 0.2 MB | Herramientas de editor de personaje, niveles y presentación |
| `Game/Assets/_Bosque/Tests` | 15 | 0.1 MB | Pruebas PlayMode y sondas de aceptación |
| `Game/Assets/_Bosque/Levels` | 3 | — | `Levels/Valle` |
| `Game/Assets/TextMesh Pro` | 82 | 4.0 MB | Recursos esenciales de TMP, necesarios para la interfaz |
| `Game/Assets/Settings/PC_RPAsset.asset` | 1 | — | Ajuste URP "equilibrado" para la GTX 1650 (QA6-006) |
| `Docs/…/LICENCIAS-*.csv`, `fuentes-polyhaven.json` | 4 | — | Licencias y fuentes de los recursos de terceros |

**Por qué incluye scripts y escenas:** las escenas, prefabs y materiales apuntan a los scripts por GUID. Si subes solo el arte, Unity mostrará *Missing Script*. Si solo quieres revisar el arte, `Art/`, `Characters/`, `UI/` y `Audio/` son lo tuyo, pero súbelo completo para que el proyecto abra limpio.

## Comprobaciones hechas al preparar el paquete
- **Binarios:** ningún archivo es un puntero LFS; todos son binarios reales.
- **`.meta`:** todo archivo nuevo de `Game/Assets` trae su `.meta`. Los 17 modificados ya tienen el suyo en `42041da`.
- **GUID:** de 491 GUID referenciados por escenas, prefabs, materiales y assets, todos existen en este paquete, en `42041da` o en paquetes que tu rama ya tiene: URP 17.3.0 (`TerrainLit`, `Lit`) y uGUI 2.0.0 (scripts de TextMeshPro). `Packages/` no cambió.
- **No verificado:** no abrí Unity con este paquete aplicado sobre tu rama, así que no comprobé la importación ni la compilación. Ese es el paso 4.

## Notas
- **`_Opcional/Game/ProjectSettings/EditorBuildSettings.asset`:** registra en Build Settings las escenas nuevas (MenuPrincipal, Arroyo, Claro, Refugio y Valle). Cópialo solo si vas a compilar una build desde tu rama. Sin él, las transiciones entre zonas no cargan en build.
- **`Tests/Acceptance/Editor/ArroyoQAV6Probe.cs`:** es de Diego (QA V6) y se modificó después en la integración. Va incluido para que la versión compile con el resto.
- **Créditos y licencias:**
  - `Docs/Character/LICENCIAS-BLOQUE-02.csv`: Guardiana (MPFB/MakeHuman CC0, y Microsoft Rocketbox MIT en V14) y audio.
  - `Docs/Fauna/LICENCIAS-ZONA-3.csv`: zorro (Fox de glTF Sample Assets, CC0).
  - `Docs/Valle/LICENCIAS-ZONA-4.csv`: Poly Haven, CC0.
  - `Game/Assets/_Bosque/UI/Creditos.txt`.
  - Revísalos para la tarea T23.
