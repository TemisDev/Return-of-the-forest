"""Genera la Guardiana realista con MPFB (MakeHuman para Blender) y la exporta a FBX para Unity.

Uso (perfil de Blender aislado con MPFB instalado y los packs CC0 cargados):
  set BLENDER_USER_RESOURCES=<perfil>
  blender -b --python generar_guardiana.py -- <carpeta_salida> [lista]

Todo lo que usa es CC0: base de MakeHuman, piel, ojos, cejas, pestañas, pelo y ropa de los packs.
Escribe: SK_Guardiana.fbx, guardiana-materiales.json (texturas por material) y una vista previa.
"""
import json, os, shutil, sys
import bpy

argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
OUT = argv[0] if argv else os.path.abspath('guardiana_out')
LIST_ONLY = len(argv) > 1 and argv[1] == 'lista'
os.makedirs(OUT, exist_ok=True)

pkg = next(m for m in sys.modules if m.endswith('.mpfb') and m.startswith('bl_ext'))
HumanService = sys.modules[pkg + '.services.humanservice'].HumanService if pkg + '.services.humanservice' in sys.modules else __import__(pkg + '.services.humanservice', fromlist=['HumanService']).HumanService
LocationService = __import__(pkg + '.services.locationservice', fromlist=['LocationService']).LocationService
ExportService = __import__(pkg + '.services.exportservice', fromlist=['ExportService']).ExportService

data_dirs = [LocationService.get_user_data(), LocationService.get_mpfb_data()]


def find(kind, name, ext):
    """Busca <name>.<ext> bajo <data>/<kind>/ en los datos del usuario y del sistema."""
    for root in data_dirs:
        base = os.path.join(root, kind)
        for dirpath, _, files in os.walk(base):
            for f in files:
                if f.lower() == (name + '.' + ext).lower():
                    return os.path.join(dirpath, f)
    return None


def listing(kind, ext):
    out = []
    for root in data_dirs:
        for dirpath, _, files in os.walk(os.path.join(root, kind)):
            out += [os.path.splitext(f)[0] for f in files if f.endswith('.' + ext)]
    return sorted(set(out))


if LIST_ONLY:
    for kind, ext in [('skins', 'mhmat'), ('eyes', 'mhmat'), ('hair', 'mhclo'), ('eyebrows', 'mhclo'), ('eyelashes', 'mhclo'),
                      ('clothes', 'mhclo'), ('proxymeshes', 'proxy'), ('teeth', 'mhclo'), ('tongue', 'mhclo')]:
        print('LISTA', kind, listing(kind, ext))
    sys.exit(0)

CFG = json.load(open(os.path.join(OUT, 'guardiana-config.json'), encoding='utf-8'))

for o in list(bpy.data.objects):
    bpy.data.objects.remove(o, do_unlink=True)

basemesh = HumanService.create_human(macro_detail_dict=CFG['macros'])
materials = {}


def mhmat_textures(mhmat):
    tex = {}
    if not mhmat or not os.path.isfile(mhmat):
        return tex
    for line in open(mhmat, encoding='utf-8', errors='ignore'):
        parts = line.strip().split(None, 1)
        if len(parts) == 2 and parts[0] in ('diffuseTexture', 'normalmapTexture', 'roughnessTexture', 'specularTexture',
                                              'transparencymapTexture', 'aomapTexture', 'bumpmapTexture'):
            p = os.path.join(os.path.dirname(mhmat), parts[1])
            if os.path.isfile(p): tex[parts[0]] = p
        if len(parts) == 2 and parts[0] in ('transparent', 'alphaToCoverage', 'backfaceCull', 'diffuseColor', 'roughness'):
            tex[parts[0]] = parts[1]
    return tex


def mhclo_material(mhclo):
    for line in open(mhclo, encoding='utf-8', errors='ignore'):
        parts = line.strip().split(None, 1)
        if len(parts) == 2 and parts[0] == 'material':
            return os.path.join(os.path.dirname(mhclo), parts[1])
    return None


skin = find('skins', CFG['skin'], 'mhmat')
HumanService.set_character_skin(skin, basemesh, skin_type='GAMEENGINE')
materials['__body__'] = {'mhmat': skin, **mhmat_textures(skin)}

# El rig va antes que la ropa: así MPFB transfiere los pesos del cuerpo a cada prenda, pelo y ojos.
rig = HumanService.add_builtin_rig(basemesh, CFG['rig'])
print('RIG', rig.name, len(rig.data.bones))

added = []
for kind, name in CFG['assets']:
    mhclo = find(kind, name, 'mhclo')
    if mhclo is None:
        raise SystemExit(f'No se encontró el recurso {kind}/{name}')
    before = set(bpy.data.objects)
    HumanService.add_mhclo_asset(mhclo, basemesh, asset_type=kind.capitalize() if kind != 'clothes' else 'Clothes', subdiv_levels=CFG.get('subdiv', {}).get(name, 0),
                                 material_type='MAKESKIN')
    new = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
    mat = mhclo_material(mhclo)
    if kind == 'eyes' and CFG.get('eye_material'):
        mat = find('eyes', CFG['eye_material'], 'mhmat') or mat
    for o in new:
        materials[o.name] = {'mhmat': mat, 'asset': f'{kind}/{name}', **mhmat_textures(mat)}
    added.append((kind, name, [o.name for o in new]))
    print('ASSET', kind, name, [o.name for o in new])

# Copia para exportar: aplica las máscaras (borra el cuerpo oculto bajo la ropa) y quita las mallas auxiliares.
ExportService.bake_modifiers_remove_helpers(basemesh, bake_masks=True, bake_subdiv=False, remove_helpers=True)
for o in bpy.data.objects:
    if o.type == 'MESH':
        for m in list(o.modifiers):
            if m.type in ('MASK', 'SUBSURF'):
                if m.type == 'SUBSURF':
                    m.levels = max(m.levels, m.render_levels); m.show_viewport = True
                bpy.context.view_layer.objects.active = o
                try: bpy.ops.object.modifier_apply(modifier=m.name)
                except Exception as e: print('MOD', o.name, m.name, e)
        if o.data.shape_keys:
            bpy.context.view_layer.objects.active = o
            bpy.ops.object.shape_key_remove(all=True, apply_mix=True)

# Piel tapada por la ropa: se borra para que no asome al deformarse (y ahorra triángulos).
import bmesh
from mathutils.bvhtree import BVHTree
deps = bpy.context.evaluated_depsgraph_get()
cloth_trees = []
for o in bpy.data.objects:
    if o.type == 'MESH' and o != basemesh and any(k in o.name for k in CFG.get('cubren', [])):
        bm2 = bmesh.new(); bm2.from_mesh(o.data); bm2.transform(o.matrix_world)
        cloth_trees.append(BVHTree.FromBMesh(bm2)); bm2.free()
bm = bmesh.new(); bm.from_mesh(basemesh.data); bm.verts.ensure_lookup_table()
mw = basemesh.matrix_world; covered = set()
for v in bm.verts:
    co = mw @ v.co; nrm = (mw.to_3x3() @ v.normal).normalized()
    for tree in cloth_trees:
        hit = tree.ray_cast(co - nrm * .004, nrm, CFG.get('margen_ropa', .045))
        if hit[0] is not None:
            covered.add(v.index); break
faces = [f for f in bm.faces if all(v.index in covered for v in f.verts)]
bmesh.ops.delete(bm, geom=faces, context='FACES_ONLY')
bm.to_mesh(basemesh.data); bm.free()
print('PIEL_TAPADA', len(covered), 'vértices', len(faces), 'caras borradas')

# Nombres estables para Unity y datos de cada malla
stats = []
for o in bpy.data.objects:
    if o.type != 'MESH':
        continue
    tris = sum(len(p.vertices) - 2 for p in o.data.polygons)
    mats = [s.material.name if s.material else None for s in o.material_slots]
    stats.append({'objeto': o.name, 'triangulos': tris, 'materiales': mats, 'grupos': len(o.vertex_groups),
                  'alto_m': round(o.dimensions.z, 3), 'padre': o.parent.name if o.parent else None})
    key = '__body__' if o == basemesh else o.name
    if key in materials:
        materials[key]['materiales_blender'] = mats
materials[basemesh.name] = materials.pop('__body__')

bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type in ('MESH', 'ARMATURE'):
        o.select_set(True)
fbx = os.path.join(OUT, 'SK_Guardiana.fbx')
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'ARMATURE', 'MESH'}, apply_unit_scale=True,
                         apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', add_leaf_bones=False,
                         bake_anim=False, use_armature_deform_only=True, mesh_smooth_type='FACE', use_tspace=True,
                         path_mode='STRIP', embed_textures=False, use_mesh_modifiers=True)
json.dump({'materiales': materials, 'mallas': stats, 'recursos': added, 'config': CFG}, open(os.path.join(OUT, 'guardiana-materiales.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print('TOTAL_TRIANGULOS', sum(s['triangulos'] for s in stats))
for s in stats:
    print('MALLA', s)

# Vista previa rápida (Eevee) para revisar antes de Unity
scene = bpy.context.scene
cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam')); scene.collection.objects.link(cam)
cam.location = (0.0, -3.2, 1.0); cam.rotation_euler = (1.5, 0, 0)
light = bpy.data.objects.new('Sol', bpy.data.lights.new('Sol', 'SUN')); scene.collection.objects.link(light)
light.rotation_euler = (0.8, 0.2, 0.6); light.data.energy = 3.5
scene.camera = cam
scene.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
scene.render.resolution_x, scene.render.resolution_y = 900, 1200
scene.world = scene.world or bpy.data.worlds.new('W'); scene.world.color = (0.35, 0.36, 0.38)
scene.render.filepath = os.path.join(OUT, 'vista_previa.png')
bpy.ops.render.render(write_still=True)
print('LISTO', fbx)
