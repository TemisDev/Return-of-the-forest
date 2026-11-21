"""Descarga modelos CC0 de Poly Haven (glTF a 1K) para la zona 4, "El valle olvidado".

Uso: python -I descargar_polyhaven.py <carpeta_destino>
Cada modelo queda en <destino>/<id>/ con el .gltf, el .bin y sus texturas; además escribe fuentes.json.
"""
import json, os, sys, urllib.request

IDS = [
    # Plástico
    "plastic_bottle_gallon", "bleach_bottle", "plastic_jerrycan", "multi_cleaner_bottle",
    # Vidrio y metal
    "wine_bottles_01", "can_rusted", "oil_tin", "metal_jerrycan", "spray_paint_bottles",
    # Papel y cartón
    "cardboard_box_01", "cement_bag",
    # No aprovechables
    "trashbag", "old_tyre",
    # Madera caída
    "dead_tree_trunk", "dead_tree_trunk_02", "dry_branches_medium_01",
    # Estaciones de reciclaje
    "metal_trash_can",
]

out = sys.argv[1]
os.makedirs(out, exist_ok=True)
UA = {"User-Agent": "bosque-de-regreso-asset-fetch"}


def get(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=120) as r:
        return r.read()


fuentes = {}
for aid in IDS:
    info = json.loads(get(f"https://api.polyhaven.com/info/{aid}"))
    files = json.loads(get(f"https://api.polyhaven.com/files/{aid}"))
    g = files["gltf"]["1k"]["gltf"]
    d = os.path.join(out, aid); os.makedirs(d, exist_ok=True)
    open(os.path.join(d, os.path.basename(g["url"])), "wb").write(get(g["url"]))
    for rel, inc in g.get("include", {}).items():
        p = os.path.join(d, rel); os.makedirs(os.path.dirname(p), exist_ok=True)
        open(p, "wb").write(get(inc["url"]))
    fuentes[aid] = {"nombre": info.get("name"), "autores": list(info.get("authors", {}).keys()),
                    "licencia": "CC0 1.0", "url": f"https://polyhaven.com/a/{aid}"}
    print("ok", aid, fuentes[aid]["autores"])
json.dump(fuentes, open(os.path.join(out, "fuentes.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=2)
