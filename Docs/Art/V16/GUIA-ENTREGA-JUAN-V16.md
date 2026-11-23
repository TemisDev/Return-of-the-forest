# Instrucciones operativas para Juan — V16

## Antes de actuar

1. Usar **una copia propia y limpia** del repositorio; no trabajar sobre la copia compartida mientras Claude o Codex editan Unity.
2. Leer `AGENTS.md`, `GUIA-RAMAS-Y-PR.md` y los relevos del repositorio. Si su política de ramas contradice los comandos orientativos, **seguir el documento del repositorio**.
3. Verificar el punto de partida con `git show -s --oneline f09f186263b21c7754645f264cefe891ccadb713` y `git status`.
4. Crear `feature/arte-v16-juan` desde el commit solicitado (solo si no existe previamente). No modificar `main` ni `develop`.

## Comandos orientativos PowerShell

```powershell
# Ejecutar desde la raíz de la copia propia del repositorio
git status --short
git fetch origin
git show -s --oneline f09f186263b21c7754645f264cefe891ccadb713
git switch -c feature/arte-v16-juan f09f186263b21c7754645f264cefe891ccadb713
```

**Si la rama ya existe, no ejecutar `switch -c` de nuevo:** inspeccionar su estado e historia. No usar `git reset --hard`, `git clean -fd` ni `git push --force`.

Copiar `Docs/Art/V16/` de este ZIP a la raíz de la copia propia. **NO** copiar un directorio `Game/Assets/.../JuanV16/` hasta tener recursos realmente creados, con `.meta` y dependencias.

## Trabajo de campo

1. Abrir Unity 6000.3.22f1/URP; confirmar cuatro zonas reales del commit base.
2. Realizar J1 y J3. Copiar capturas genuinas, enlazar cada hallazgo a su imagen, medir parámetros que se reporten.
3. Escuchar J4 con auriculares en dos zonas y registrar condiciones.
4. Elegir 0–3 mejoras J2 basándose en defectos observados. No alterar escenas ni runtime.
5. Completar J5: origen, licencia y atribuciones; revisar avisos originales.
6. Actualizar estatus en `INFORME-ARTE-V16.md` y `INTEGRACION-JUAN-V16.md`; borrar filas vacías de propuesta si no se usan, sin afirmar que fueron ejecutadas.

## Publicación

```powershell
git status --short
git add -- Docs/Art/V16/
# Solo si efectivamente se crearon variantes con sus .meta:
# git add -- Game/Assets/_Bosque/Art/Entregas/JuanV16/
git diff --cached --stat
git diff --cached --name-only
git commit -m "art: review V16 visual consistency and handoff targeted variants"
git push -u origin feature/arte-v16-juan
```

Abrir PR **`feature/arte-v16-juan` → `feature/valle-olvidado`**. Descripción: commit base, zonas revisadas, captura pareada por zona, 0–3 mejoras, resultado UI/audio, licencias, riesgos y pruebas a repetir por Diego. No fusionar automáticamente.

### Importante

El paquete proporcionado por ChatGPT **es una plantilla inicial**. Su publicación tal cual **NO** satisface J1–J5 ni debe presentarse como entrega terminada. Las pruebas visuales y de audio requieren observación humana real.
