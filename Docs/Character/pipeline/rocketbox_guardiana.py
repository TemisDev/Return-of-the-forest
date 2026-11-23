"""Prepara la Guardiana V14 a partir de Microsoft Rocketbox "Female_Adult_07" (licencia MIT).

Uso: python -I rocketbox_guardiana.py <carpeta_descarga_Female_Adult_07> <raiz_Characters/Guardiana>
La carpeta de descarga tiene Export/Female_Adult_07.fbx y Textures/f007_*.tga (repositorio
https://github.com/microsoft/Microsoft-Rocketbox, Assets/Avatars/Adults/Female_Adult_07).

- Copia el FBX (esqueleto Biped "Bip01", Unity lo mapea a Humanoid) a Source/Rocketbox/SK_Guardiana_RB.fbx.
- Convierte las texturas TGA de 2048 px: color y normal a JPG, el mapa especular a la alfa de un MetalSmooth
  (R metal = 0, A suavidad) y el de pelo y pestañas a PNG con alfa.
- Escribe guardiana-urp.json con el material de cada parte (lo lee GuardianaBuilder).
"""
import json, os, shutil, sys
from PIL import Image

src, root = sys.argv[1], sys.argv[2]
out_src = os.path.join(root, "Source", "Rocketbox"); out_tex = os.path.join(root, "Textures", "Rocketbox")
os.makedirs(out_src, exist_ok=True); os.makedirs(out_tex, exist_ok=True)
shutil.copy(os.path.join(src, "Export", "Female_Adult_07.fbx"), os.path.join(out_src, "SK_Guardiana_RB.fbx"))
T = lambda n: os.path.join(src, "Textures", f"f007_{n}.tga")


def smooth(spec_name, out_name, scale):
    s = Image.open(T(spec_name)).convert("L").point(lambda v: int(min(255, v * scale)))
    z = Image.new("L", s.size, 0)
    Image.merge("RGBA", (z, z, z, s)).save(os.path.join(out_tex, out_name), optimize=True)


for part, name, sc in (("body", "Cuerpo", .55), ("head", "Cabeza", .5)):
    Image.open(T(f"{part}_color")).convert("RGB").save(os.path.join(out_tex, f"T_GuardianaRB_{name}_BaseColor.jpg"), quality=93)
    Image.open(T(f"{part}_normal")).convert("RGB").save(os.path.join(out_tex, f"T_GuardianaRB_{name}_Normal.jpg"), quality=95)
    smooth(f"{part}_specular", f"T_GuardianaRB_{name}_MetalSmooth.png", sc)
Image.open(T("opacity_color")).convert("RGBA").save(os.path.join(out_tex, "T_GuardianaRB_Pelo_BaseColor.png"), optimize=True)

manifest = {"materiales": [
    {"material": "f007_body", "parte": "RB_Cuerpo", "base": "T_GuardianaRB_Cuerpo_BaseColor.jpg", "normal": "T_GuardianaRB_Cuerpo_Normal.jpg",
     "smoothMap": "T_GuardianaRB_Cuerpo_MetalSmooth.png", "smoothness": .75, "alphaClip": 0, "doubleSided": False},
    {"material": "f007_head", "parte": "RB_Cabeza", "base": "T_GuardianaRB_Cabeza_BaseColor.jpg", "normal": "T_GuardianaRB_Cabeza_Normal.jpg",
     "smoothMap": "T_GuardianaRB_Cabeza_MetalSmooth.png", "smoothness": .8, "alphaClip": 0, "doubleSided": False},
    {"material": "f007_opacity", "parte": "RB_Pelo", "base": "T_GuardianaRB_Pelo_BaseColor.png", "normal": "",
     "smoothMap": "", "smoothness": .32, "alphaClip": .42, "doubleSided": True},
]}
json.dump(manifest, open(os.path.join(out_src, "guardiana-urp.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("ok")
