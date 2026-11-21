# Procedimiento reproducible — QA V6

## 1. Sonda automatizada (unos 5 minutos)

Con Unity cerrado sobre `Game/` (otros proyectos abiertos no molestan), desde la raíz del repositorio en PowerShell:

```powershell
& .\Docs\QA\Entregable06\Tools\ejecutar_qa_v6.ps1
```

El script:

1. ejecuta `Bosque.Acceptance.ArroyoQAV6Probe.RunBatch`;
2. copia la salida a `.dev-logs/qa-v6-<fecha>/`, que está fuera de Git;
3. actualiza `Docs/QA/Entregable06/EVIDENCIAS/sonda-qa-v6.*`.

Códigos de salida:

| Código | Significado |
|---|---|
| 0 | Sin FALLA |
| 2 | Hay alguna FALLA |
| 1 | Error de ejecución |

Para ejecutarla desde Unity: **Tools > Bosque > QA > V6 - Ejecutar sonda de QA del arte V6**.

## 2. Rendimiento

1. En Unity: **Tools > Bosque > Arte > V6 > 2 - Crear escena de muestra**.
2. Genera la build de muestra con `Bosque.ArtV6.Editor.ArroyoArtV6Capture.BuildPreviewBatch`. Sale en `Builds/ArtV6Preview/`, fuera de Git.
3. Ejecuta, con el equipo en frío y sin otros programas pesados:

```powershell
.\Builds\ArtV6Preview\BosqueV6Preview.exe -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -v6-perf -v6-perf-out "$PWD\.dev-logs\perf-v6.json"
```

La medición tiene 5 fases de 22 s cada una (2 s de calentamiento):

1. degradado
2. restaurado
3. restaurado con sombras ligeras
4. degradado con sombras ligeras
5. restaurado

Añade el resultado como filas nuevas de `RENDIMIENTO-V6.csv` e indica el equipo y las condiciones.

## 3. Manual

Usa `FORMULARIO-PRUEBA-MANUAL-V6.md` y pasa el resultado a la matriz. Hay que conservar el identificador de lo que se probó (commit de la rama de arte o SHA de la build).
