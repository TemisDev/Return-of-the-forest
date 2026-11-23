# Subir el Arte V6 a GitHub (Juan)

Repositorio: `https://github.com/TemisDev/Return-of-the-forest`
Rama nueva: `feature/arroyo-art-v6`, creada desde `develop` (último commit esperado: `299a479`).
Archivo recibido: `BosqueDeRegreso-ArteV6-para-Juan.zip`. Sus rutas internas ya coinciden con las del repositorio.

## Contenido del ZIP

| Ruta | Archivos |
|---|---|
| `Game/Assets/_Bosque/Art/V6/` | Arte, con todos sus `.meta` |
| `Game/Assets/_Bosque/Art/V6.meta` | Meta de la carpeta |
| `Docs/Art/Entregable06/` | Guía, inventario, capturas, rendimiento, scripts de Blender y este archivo |

No incluye `.gitattributes`: se modifica con `git lfs track` en el paso 4, para no sobrescribir el del repo.

## Requisitos

- Git y Git LFS instalados. Se comprueba con `git lfs version`.
- **Unity cerrado** mientras copias los archivos y haces el commit.
- Una copia del repositorio en tu PC. Si no la tienes, cámbiate primero a la carpeta donde quieras guardarla:

```
git clone https://github.com/TemisDev/Return-of-the-forest.git
```

## Comandos (CMD de Windows)

Cambia `C:\ruta\Return-of-the-forest` por la carpeta de tu copia y `%USERPROFILE%\Downloads\...` por la ruta donde guardaste el ZIP.

### 1. Entrar al repo y dejarlo al día

```
cd /d C:\ruta\Return-of-the-forest
git lfs install
git status
git fetch origin
git checkout develop
git pull origin develop
git log -1 --oneline
```

`git status` no debe mostrar cambios sin commit. Si los hay, guárdalos o súbelos antes de seguir.
El último `git log` debe mostrar `299a479` o un commit más nuevo de `develop`.

### 2. Crear la rama de la entrega

```
git checkout -b feature/arroyo-art-v6
```

### 3. Descomprimir el ZIP en la raíz del repo

```
tar -xf "%USERPROFILE%\Downloads\BosqueDeRegreso-ArteV6-para-Juan.zip"
dir Game\Assets\_Bosque\Art\V6
```

Deben verse estas carpetas:

| | | | |
|---|---|---|---|
| `Editor` | `Materials` | `Models` | `Particles` |
| `PostFX` | `Prefabs` | `Runtime` | `Shaders` |
| `Showcase` | `Sky` | `Textures` | |

### 4. Guardar los cielos .hdr en Git LFS

```
git lfs track "*.hdr"
```

Este comando añade una línea a `.gitattributes`.

### 5. Verificar que se copió todo

En PowerShell, desde la raíz del repo. El resultado debe ser `0 diferencias`:

```powershell
$m = Get-Content Docs\Art\Entregable06\MANIFIESTO-SHA256.txt; $bad = 0; foreach ($l in $m) { $h, $p = $l -split '  ', 2; if ((Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash -ne $h) { $bad++; "DIFERENTE: $p" } }; "$bad diferencias"
```

### 6. Añadir y revisar antes del commit

```
git add .gitattributes Game/Assets/_Bosque/Art/V6.meta Game/Assets/_Bosque/Art/V6 Docs/Art/Entregable06
git status --short
```

Es normal que `git add` muestre avisos `CRLF will be replaced by LF`: el repositorio normaliza los finales de línea.

Revisa lo que lista `git status --short`. Deben ser unos 752 archivos y **solo** de estas rutas:

- `Art/V6`
- `Art/V6.meta`
- `Docs/Art/Entregable06`
- `.gitattributes`

Después comprueba que los binarios van por LFS:

```
git lfs status
```

Las imágenes, los `.fbx`, los `.exr` y los `.hdr` deben aparecer como `LFS`.

### 7. Commit y push

```
git commit -m "Arte: agrega arte realista V6 del arroyo, partículas y escena de muestra"
git push -u origin feature/arroyo-art-v6
```

La subida es de unos 200 MB por LFS y puede tardar varios minutos.

### 8. Abrir el Pull Request

En GitHub, crea un **Compare & pull request** de `feature/arroyo-art-v6` hacia `develop`. **No lo fusiones**: lo revisan Diego (QA) y el integrador.

Texto sugerido para el PR:

> Arte V6 realista del Arroyo dormido.
> - Incluye 63 modelos CC0 de Poly Haven reducidos para juego, 5 shaders URP, 49 materiales, 91 prefabs sin colliders, 14 sistemas de partículas, cielo HDRI y post-proceso por estado.
> - La escena de muestra jugable está en `Art/V6/Showcase/ArroyoV6_Showcase.unity`. No modifica `ArroyoPlayable`, ProjectSettings ni Packages.
> - Guía, licencias y rendimiento: `Docs/Art/Entregable06/LEEME-ARTE-V6.md`.
> - Pendiente: revisión de QA, aprobación artística e integración en `ArroyoPlayable`.

## Comprobar en Unity (opcional, recomendado)

1. Abre `Game/` con **Unity 6000.3.22f1**. La primera importación de V6 tarda unos minutos.
2. Abre `Assets/_Bosque/Art/V6/Showcase/ArroyoV6_Showcase.unity` y pulsa Play.
3. Si la consola muestra errores en rojo, no subas y avisa con una captura.

## Si algo sale mal

- **`tar` no existe** (Windows anterior a la 10): descomprime con clic derecho > Extraer todo, eligiendo como destino la raíz del repo.
- **Ya existía la rama:** usa `git checkout feature/arroyo-art-v6` en lugar del paso 2.
- **El push pide usuario:** inicia sesión en GitHub con tu cuenta, que debe tener acceso de escritura al repo.
- **Error de cuota de LFS:** avisa antes de reintentar. La cuenta gratuita tiene 1 GB de almacenamiento y 1 GB de descarga al mes.
