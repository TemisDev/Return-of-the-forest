"""Convierte las texturas de MakeHuman (CC0) de la Guardiana a texturas listas para URP en el proyecto Unity.
Uso: python preparar_texturas.py <guardiana-materiales.json> <carpeta_destino_unity>
Escribe T_Guardiana_<parte>_BaseColor/Normal.png y guardiana-urp.json (material de Blender -> ajustes URP).
"""
import json, os, sys
from PIL import Image, ImageOps

manifest = json.load(open(sys.argv[1], encoding='utf-8'))['materiales']
out = sys.argv[2]
os.makedirs(out, exist_ok=True)


def fit(im, size=2048):
    if max(im.size) > size:
        im = im.resize((size, size) if im.size[0] == im.size[1] else (size, int(size * im.size[1] / im.size[0])), Image.LANCZOS)
    return im


def tint(path, color, contrast=1.15, lift=.08):
    """Recolorea conservando el detalle: luminancia -> color destino (para pasar el suéter rojo a verde oliva, etc.)."""
    im = Image.open(path).convert('RGB')
    l = ImageOps.autocontrast(im.convert('L'), cutoff=1)
    lut = [min(255, int(255 * (lift + (1 - lift) * ((v / 255) ** (1 / contrast))))) for v in range(256)]
    l = l.point(lut)
    ch = [l.point(lambda v, c=c: int(v * c)) for c in color]
    return Image.merge('RGB', ch)


def find_normal(mat):
    if 'normalmapTexture' in mat: return mat['normalmapTexture']
    folder = os.path.dirname(mat.get('diffuseTexture', ''))
    for f in os.listdir(folder) if folder else []:
        if 'norm' in f.lower() and f.lower().endswith('.png'): return os.path.join(folder, f)
    return None


parts = {
    'Human.body': ('Piel', dict(smoothness=.38)),
    'Human.high-poly': ('Ojos', dict(smoothness=.85, alphaClip=.3)),
    'Human.eyebrow001': ('Cejas', dict(smoothness=.2, alphaClip=.35, doubleSided=True)),
    'Human.eyelashes01': ('Pestanas', dict(smoothness=.2, alphaClip=.35, doubleSided=True)),
    'Human.braid01': ('Trenza', dict(smoothness=.45, alphaClip=.4, doubleSided=True)),
    'Human.toigo_fisherman_sweater': ('Sueter', dict(smoothness=.15, tint=(.42, .47, .27))),
    'Human.cortu_cargo_pants': ('Pantalon', dict(smoothness=.22)),
    'Human.toigo_ankle_boots_female': ('Botas', dict(smoothness=.42, tint=(.44, .28, .16))),
}
urp = {}
for obj, mat in manifest.items():
    key = (mat.get('materiales_blender') or [obj])[0]
    part, cfg = parts[key]
    base = f'T_Guardiana_{part}_BaseColor.png'
    if 'tint' in cfg:
        img = tint(mat['diffuseTexture'], cfg['tint'])
    else:
        img = Image.open(mat['diffuseTexture'])
        img = img.convert('RGBA') if cfg.get('alphaClip') else img.convert('RGB')
    fit(img).save(os.path.join(out, base))
    entry = {'parte': part, 'base': base, 'smoothness': cfg['smoothness'], 'alphaClip': cfg.get('alphaClip', 0),
             'doubleSided': cfg.get('doubleSided', False), 'fuente': mat.get('asset', 'skins/' + os.path.basename(os.path.dirname(mat['mhmat'])))}
    n = find_normal(mat)
    if n:
        Image.open(n).convert('RGB').pipe(fit).save(os.path.join(out, f'T_Guardiana_{part}_Normal.png')) if hasattr(Image.Image, 'pipe') else \
            fit(Image.open(n).convert('RGB')).save(os.path.join(out, f'T_Guardiana_{part}_Normal.png'))
        entry['normal'] = f'T_Guardiana_{part}_Normal.png'
    urp[key] = entry
    print(key, entry)
json.dump(urp, open(os.path.join(out, 'guardiana-urp.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
