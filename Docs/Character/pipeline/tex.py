import os, sys
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
O = os.path.join(src, 'x-outfits', 'Modular Character Outfits - Fantasy[Standard]', 'Textures')
B = os.path.join(src, 'x-ubc', 'Universal Base Characters[Standard]', 'Base Characters', 'Textures')
def save(im, name, size=2048):
    if im.size[0] > size: im = im.resize((size, size), Image.LANCZOS)
    im.save(os.path.join(out, name)); print(name, im.size, im.mode)
def metal_smooth(metal, rough, size=2048):
    rough = rough.convert('L').resize((size, size), Image.LANCZOS)
    metal = (metal.convert('L').resize((size, size), Image.LANCZOS) if metal else Image.new('L', (size, size), 0))
    smooth = rough.point(lambda v: 255 - v)
    z = Image.new('L', (size, size), 0)
    return Image.merge('RGBA', (metal, z, z, smooth))
# Traje Ranger (verde/marrón)
save(Image.open(os.path.join(O, 'Ranger', 'T_Ranger_BaseColor.png')).convert('RGB'), 'T_Guardiana_Outfit_BaseColor.png')
save(Image.open(os.path.join(O, 'Ranger', 'T_Ranger_Normal.png')).convert('RGB'), 'T_Guardiana_Outfit_Normal.png')
orm = Image.open(os.path.join(O, 'Ranger', 'T_Ranger_ORM.png')).convert('RGB'); o, r, m = orm.split()
save(metal_smooth(m, r), 'T_Guardiana_Outfit_MetalSmooth.png')
save(o, 'T_Guardiana_Outfit_Occlusion.png')
# Piel: brazos (cuerpo Regular) y cabeza (base Superhero), tono oscuro
RB = os.path.join(O, 'Ranger', 'Base Chars')
save(Image.open(os.path.join(RB, 'T_Regular_Female_Dark_BaseColor.png')).convert('RGB'), 'T_Guardiana_SkinBody_BaseColor.png')
save(Image.open(os.path.join(RB, 'T_Regular_Female_Normal.png')).convert('RGB'), 'T_Guardiana_SkinBody_Normal.png')
save(metal_smooth(None, Image.open(os.path.join(RB, 'T_Regular_Female_Roughness.png'))), 'T_Guardiana_SkinBody_MetalSmooth.png')
save(Image.open(os.path.join(B, 'T_Superhero_Female_Dark_BaseColor.png')).convert('RGB'), 'T_Guardiana_SkinHead_BaseColor.png')
save(Image.open(os.path.join(B, 'T_Superhero_Female_Normal.png')).convert('RGB'), 'T_Guardiana_SkinHead_Normal.png')
save(metal_smooth(None, Image.open(os.path.join(B, 'T_Superhero_Female_Roughness.png'))), 'T_Guardiana_SkinHead_MetalSmooth.png')
save(Image.open(os.path.join(B, 'T_Eye_Brown.png')), 'T_Guardiana_Eye_BaseColor.png', 256)
save(Image.open(os.path.join(B, 'T_Hair_2_BaseColor.png')).convert('RGB'), 'T_Guardiana_Hair_BaseColor.png', 1024)
