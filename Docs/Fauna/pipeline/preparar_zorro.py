"""Convierte el zorro de muestra de Khronos (Fox.glb) en el "zorro de neblina", el animal ficticio de la zona 3.

Uso:
  blender -b --factory-startup --python preparar_zorro.py -- <Fox.glb> <carpeta_salida>

Qué hace:
- Quita la esfera auxiliar del glTF y suaviza la malla (una subdivisión, conservando los pesos del esqueleto).
- Pinta un pelaje nuevo a 2K inspirado en el zorro andino (culpeo): lomo gris jaspeado, cabeza, orejas y patas
  rojizas, garganta crema y punta de la cola oscura. Se calcula por texel a partir de la posición 3D en reposo
  (rasterizando los triángulos UV), así que no depende de cómo esté repartido el atlas.
- Exporta SK_ZorroNeblina.fbx con las tres animaciones (Survey, Walk, Run) y una vista previa.

Licencia del original: modelo de PixelMannen (CC0), rig y animación de tomkranis (CC-BY 4.0),
conversión glTF de @AsoboStudio y @scurest (CC-BY 4.0). Ver Docs/Fauna/LICENCIAS-ZONA-3.csv.
"""
import math, os, sys
import bpy
import numpy as np

argv = sys.argv[sys.argv.index('--') + 1:]
SRC, OUT = argv[0], argv[1]
os.makedirs(OUT, exist_ok=True)
RES = 2048
rng = np.random.default_rng(7)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)
for o in list(bpy.data.objects):
    if o.type == 'MESH' and o.name.lower().startswith('icosphere'):
        bpy.data.objects.remove(o, do_unlink=True)
fox = next(o for o in bpy.data.objects if o.type == 'MESH')
arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
fox.name = 'ZorroNeblina'; fox.data.name = 'ZorroNeblina'
arm.data.pose_position = 'REST'
bpy.context.view_layer.update()

# ---------- atributos 3D por texel ----------
me = fox.data
uv = me.uv_layers.active.data
verts = np.array([v.co[:] for v in me.vertices])
lo, hi = verts.min(0), verts.max(0)
print('bounds', lo, hi)
# Eje largo = cuerpo. En el glTF importado el hocico apunta a -Y o +Y; se detecta por la altura (la cabeza
# está más alta que la cola en reposo en el extremo con más vértices altos).
pos_img = np.zeros((RES, RES, 3), np.float32)
nrm_img = np.zeros((RES, RES, 3), np.float32)
lab_img = np.zeros((RES, RES), np.float32)
src_img = next(im for im in bpy.data.images if im.size[0] > 0)
sw, sh = src_img.size
src = np.array(src_img.pixels[:], np.float32).reshape(sh, sw, 4)

me.calc_loop_triangles()
for tri in me.loop_triangles:
    uvs = np.array([uv[l].uv[:] for l in tri.loops]) * RES
    ps = np.array([me.vertices[v].co[:] for v in tri.vertices])
    ns = np.array([me.vertices[v].normal[:] for v in tri.vertices])
    x0, y0 = np.floor(uvs.min(0)).astype(int) - 2
    x1, y1 = np.ceil(uvs.max(0)).astype(int) + 2
    x0, y0 = max(x0, 0), max(y0, 0); x1, y1 = min(x1, RES - 1), min(y1, RES - 1)
    if x1 <= x0 or y1 <= y0: continue
    xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
    a, b, c = uvs
    den = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
    if abs(den) < 1e-9: continue
    w0 = ((b[1] - c[1]) * (xs - c[0]) + (c[0] - b[0]) * (ys - c[1])) / den
    w1 = ((c[1] - a[1]) * (xs - c[0]) + (a[0] - c[0]) * (ys - c[1])) / den
    w2 = 1 - w0 - w1
    m = (w0 >= -.02) & (w1 >= -.02) & (w2 >= -.02)  # algo de margen para el sangrado
    if not m.any(): continue
    W = np.stack([w0, w1, w2], -1)[m]
    yy, xx = (ys[m] - .5).astype(int), (xs[m] - .5).astype(int)
    pos_img[yy, xx] = W @ ps
    n = W @ ns; n /= np.linalg.norm(n, axis=1, keepdims=True) + 1e-9
    nrm_img[yy, xx] = n
    lab_img[yy, xx] = 1

# Color original por texel para conservar zonas blancas (garganta, pecho) y la trufa negra.
sy = np.clip((np.arange(RES) + .5) / RES * sh, 0, sh - 1).astype(int)
sx = np.clip((np.arange(RES) + .5) / RES * sw, 0, sw - 1).astype(int)
orig = src[sy][:, sx, :3]
lum = orig.mean(-1)
white = (lum > .85).astype(np.float32)
black = (lum < .16).astype(np.float32)
dark = ((orig[..., 0] < .45) & (lum > .16) & (lum < .4)).astype(np.float32)  # patas y dorso de orejas


def blur(a, r):
    """Desenfoque gaussiano separable (numpy puro)."""
    if r <= 0: return a
    k = np.exp(-np.linspace(-2.5, 2.5, 2 * r + 1) ** 2); k /= k.sum()
    out = a
    for ax in (0, 1):
        out = np.apply_along_axis(lambda v: np.convolve(v, k, 'same'), ax, out)
    return out


def noise(scale, octaves=4, seed=0):
    g = np.random.default_rng(seed)
    acc = np.zeros((RES, RES), np.float32); amp = 1; tot = 0
    for o in range(octaves):
        n = max(2, min(RES // 2, int(scale * 2 ** o)))
        small = g.random((n + 1, n + 1)).astype(np.float32)
        t = (np.arange(RES) + .5) / RES * n
        i = np.minimum(t.astype(int), n - 1); f = t - i; f = f * f * (3 - 2 * f)
        rows = small[i] * (1 - f)[:, None] + small[i + 1] * f[:, None]
        acc += (rows[:, i] * (1 - f)[None, :] + rows[:, i + 1] * f[None, :]) * amp
        tot += amp; amp *= .55
    return acc / tot


white_s = blur(white, 6); black_s = blur(black, 3); dark_s = blur(dark, 10)

# Coordenadas 3D normalizadas: L a lo largo del cuerpo, H altura.
span = hi - lo
long_ax = int(np.argmax(span[:2]))
up = 2
L = (pos_img[..., long_ax] - lo[long_ax]) / span[long_ax]
H = (pos_img[..., up] - lo[up]) / span[up]
Nz = nrm_img[..., up]
# Extremo de la cola: el lado del eje largo donde los vértices son más bajos en promedio en la mitad trasera.
# La trufa (texels negros del original) marca el extremo de la cabeza.
nose_L = L[(black > 0) & (lab_img > 0)].mean()
head_high = nose_L > .5
Lh = L if head_high else 1 - L   # 1 = hocico, 0 = punta de la cola
print('head at high end of axis', long_ax, head_high)

# Paleta (lineal aproximada a partir de sRGB)
def lin(*c): return np.array([(x / 255) ** 2.2 for x in c], np.float32)
GREY = lin(118, 112, 104); GREY_D = lin(70, 66, 61); RUFOUS = lin(156, 98, 58); RUFOUS_D = lin(104, 60, 34)
CREAM = lin(222, 210, 188); SOOT = lin(28, 24, 22); BROWN_D = lin(66, 40, 24)

fur_fine = noise(160, 3, 1); fur_mid = noise(24, 3, 2); patch = noise(6, 2, 3)
hair = blur(rng.random((RES, RES)).astype(np.float32), 1)  # pelo individual
hair = (hair - hair.min()) / (hair.max() - hair.min())
streak = hair
# Lomo gris jaspeado (normales hacia arriba y parte alta), flancos, cabeza y patas rojizos.
dorsal = np.clip((Nz + .05) / .55, 0, 1) * np.clip((H - .3) / .25, 0, 1)
head = np.clip((Lh - .8) / .08, 0, 1)
legs = np.clip((.38 - H) / .2, 0, 1) * np.clip((Lh - .12) / .1, 0, 1)
tail = np.clip((.2 - Lh) / .1, 0, 1)
tail_tip = np.clip((.1 - Lh) / .05, 0, 1)

base = RUFOUS * (1 - fur_mid[..., None] * .35) + RUFOUS_D * (fur_mid[..., None] * .35)
grizzle = GREY * (.8 + .3 * fur_fine[..., None]) * (.7 + .6 * hair[..., None])
tip = np.clip((hair - .58) / .12, 0, 1)[..., None] * .55  # pelos con punta clara
grizzle = grizzle * (1 - tip) + CREAM * .75 * tip
d = np.clip(dorsal * (1 - head * .85) * (1 - legs), 0, 1)[..., None]
col = base * (1 - d) + grizzle * d
col = col * (1 - (dark_s[..., None] * .8)) + RUFOUS_D * .55 * dark_s[..., None] * .8 + BROWN_D * 0
col = col * (1 - tail[..., None] * .6) + GREY_D * tail[..., None] * .6 * (.8 + .4 * fur_fine[..., None])
wt = (white_s * (1 - np.clip((.22 - Lh) / .04, 0, 1)))[..., None]  # la cola no lleva blanco
col = col * (1 - wt) + CREAM * (.9 + .15 * fur_fine[..., None]) * wt
col = col * (1 - tail_tip[..., None]) + SOOT * (.8 + .4 * hair[..., None]) * tail_tip[..., None]
col = col * (1 - black_s[..., None]) + SOOT * black_s[..., None]
col *= (.88 + .24 * fur_fine[..., None]) * (.94 + .12 * patch[..., None])
col = np.clip(col, 0, 1)

# Sangrado de bordes de isla para evitar costuras con mipmaps.
mask = lab_img > 0
filled = col.copy()
for _ in range(12):
    acc = np.zeros_like(filled); cnt = np.zeros(mask.shape, np.float32)
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        acc += np.roll(np.roll(filled * mask[..., None], dy, 0), dx, 1)
        cnt += np.roll(np.roll(mask.astype(np.float32), dy, 0), dx, 1)
    grow = (~mask) & (cnt > 0)
    filled[grow] = acc[grow] / cnt[grow][:, None]
    mask = mask | grow
filled[~mask] = col[mask].mean(0)

srgb = np.clip(filled, 0, 1) ** (1 / 2.2)

def save(arr, name, colorspace):
    img = bpy.data.images.new(name, RES, RES, alpha=False)
    img.colorspace_settings.name = colorspace
    rgba = np.concatenate([arr, np.ones((RES, RES, 1), np.float32)], -1)
    img.pixels.foreach_set(rgba.astype(np.float32).ravel())
    img.filepath_raw = os.path.join(OUT, name + '.png'); img.file_format = 'PNG'; img.save()
    return img


albedo = save(srgb, 'T_ZorroNeblina_BaseColor', 'sRGB')

mat = fox.data.materials[0]
mat.name = 'M_ZorroNeblina'
nt = mat.node_tree
bsdf = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
tex = next(n for n in nt.nodes if n.type == 'TEX_IMAGE')
tex.image = albedo
bsdf.inputs['Roughness'].default_value = .88
bsdf.inputs['Metallic'].default_value = 0

# ---------- suavizado conservando pesos ----------
arm.data.pose_position = 'REST'
bpy.context.view_layer.objects.active = fox
for o in bpy.data.objects: o.select_set(o == fox)
# El glTF trae cada triángulo con vértices propios (sombreado plano): se sueldan antes de suavizar.
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.remove_doubles(threshold=.01, use_sharp_edge_from_normals=False)
bpy.ops.mesh.mark_sharp(clear=True)
bpy.ops.object.mode_set(mode='OBJECT')
if fox.data.has_custom_normals: bpy.ops.mesh.customdata_custom_splitnormals_clear()
print('verts after weld', len(fox.data.vertices))
sub = fox.modifiers.new('Suavizado', 'SUBSURF')
sub.levels = sub.render_levels = 2
sub.boundary_smooth = 'PRESERVE_CORNERS'
sub.show_viewport = True
while fox.modifiers[0] != sub:
    bpy.ops.object.modifier_move_up(modifier=sub.name)
bpy.ops.object.modifier_apply(modifier=sub.name)
for p in fox.data.polygons: p.use_smooth = True
arm.data.pose_position = 'POSE'
tris = sum(len(p.vertices) - 2 for p in fox.data.polygons)
print('tris after smoothing', tris)

# ---------- exportación ----------
fbx = os.path.join(OUT, 'SK_ZorroNeblina.fbx')
for o in bpy.data.objects: o.select_set(o in (fox, arm))
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'ARMATURE', 'MESH'},
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True, axis_forward='-Z', axis_up='Y', path_mode='STRIP', mesh_smooth_type='FACE')
print('exported', fbx)

# ---------- vista previa ----------
act = bpy.data.actions.get('Walk')
if act:
    arm.animation_data_create(); arm.animation_data.action = act
    if hasattr(arm.animation_data, 'action_slot') and act.slots: arm.animation_data.action_slot = act.slots[0]
sc = bpy.context.scene
sc.frame_set(6)
size = max(fox.dimensions)
cam_data = bpy.data.cameras.new('cam'); cam = bpy.data.objects.new('cam', cam_data); sc.collection.objects.link(cam)
center = sum((fox.matrix_world @ v.co for v in fox.data.vertices), __import__('mathutils').Vector()) / len(fox.data.vertices)
cam.location = center + __import__('mathutils').Vector((size * 1.1, -size * .9, size * .45))
dirv = center - cam.location
cam.rotation_euler = dirv.to_track_quat('-Z', 'Y').to_euler()
sc.camera = cam
sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); sun.rotation_euler = (math.radians(50), 0, math.radians(30))
sun.data.energy = 3; sc.collection.objects.link(sun)
sc.world = bpy.data.worlds.new('w'); sc.world.use_nodes = True
sc.world.node_tree.nodes['Background'].inputs[0].default_value = (.35, .4, .42, 1)
sc.render.engine = 'BLENDER_EEVEE' if 'BLENDER_EEVEE' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_WORKBENCH'
sc.render.resolution_x, sc.render.resolution_y = 1000, 700
sc.render.filepath = os.path.join(OUT, 'vista_previa_zorro.png')
bpy.ops.render.render(write_still=True)
print('preview written')
