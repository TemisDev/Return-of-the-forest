"""Convierte los modelos de Poly Haven (glTF 1K) a FBX listos para Unity (zona 4, "El valle olvidado").

Uso:
  blender -b --factory-startup --python convertir_modelos.py -- <carpeta_descargas> <carpeta_modelos> <carpeta_texturas>

Por modelo: conserva solo las piezas indicadas, une la malla, reduce triángulos si es muy pesada, deja el pivote
en la base (centro) y exporta SM_Valle_<Nombre>.fbx con un único material "M_Valle_<Nombre>".
Texturas: T_Valle_<Nombre>_BaseColor.jpg, _Normal.jpg (OpenGL) y _MetalSmooth.png (R metal, A suavidad), que se
derivan del mapa ARM (R oclusión, G rugosidad, B metal).
"""
import glob, os, shutil, sys
import bpy
import numpy as np

argv = sys.argv[sys.argv.index('--') + 1:]
SRC, OUT_MODELS, OUT_TEX = argv
os.makedirs(OUT_MODELS, exist_ok=True); os.makedirs(OUT_TEX, exist_ok=True)

# id: (nombre, piezas a conservar (prefijos; None = todas), triángulos máximos, prefijo de texturas)
CONFIG = {
    "plastic_bottle_gallon": ("BidonPlastico", None, 3000, "plastic_bottle_gallon"),
    "bleach_bottle": ("BotellaLejia", None, 2500, "bleach_bottle_01"),
    "plastic_jerrycan": ("GaloneraPlastico", None, 3000, "plastic_jerrycan"),
    "multi_cleaner_bottle": ("BotellaLimpiador", None, 3000, "multi_cleaner_bottle"),
    "wine_bottles_01": ("BotellaVidrio", ["wine_bottles_01_bordeaux"], 4000, "wine_bottles_01"),
    "can_rusted": ("LataOxidada", None, 2500, "can_rusted"),
    "oil_tin": ("LataAceite", None, 2500, "oil_tin"),
    "metal_jerrycan": ("BidonMetal", None, 4000, "metal_jerrycan"),
    "spray_paint_bottles": ("Aerosol", ["spray_paint_bottles"], 2500, "spray_paint_bottles"),
    "cardboard_box_01": ("CajaCarton", None, 3000, "cardboard_box_01"),
    "cement_bag": ("BolsaPapel", None, 2000, "cement_bag"),
    "trashbag": ("BolsaBasura", None, 3000, "trashbag"),
    "old_tyre": ("Llanta", None, 3000, "old_tyre"),
    "dead_tree_trunk": ("TroncoDelgado", None, 7000, "dead_tree_trunk"),
    "dead_tree_trunk_02": ("TroncoGrueso", None, 9000, "dead_tree_trunk_02"),
    "dry_branches_medium_01": ("Ramas", None, 6000, "dry_branches_medium_01"),
    "metal_trash_can": ("Contenedor", ["metal_trash_can", "metal_trash_can_handle_left", "metal_trash_can_handle_right", "metal_trash_can_lid"], 6000, "metal_trash_can"),
}

report = []
for aid, (name, keep, max_tris, texp) in CONFIG.items():
    d = os.path.join(SRC, aid)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=glob.glob(os.path.join(d, '*.gltf'))[0])
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    if keep:
        for o in meshes:
            if not any(o.name == k or o.name.startswith(k + '.') for k in keep):
                bpy.data.objects.remove(o, do_unlink=True)
        meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    for o in list(bpy.data.objects):
        if o.type != 'MESH': bpy.data.objects.remove(o, do_unlink=True)
    bpy.ops.object.select_all(action='DESELECT')
    for o in meshes: o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    for o in meshes: o.parent = None
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if len(meshes) > 1: bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = 'SM_Valle_' + name; ob.data.name = ob.name
    tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    if tris > max_tris:
        dec = ob.modifiers.new('Reducir', 'DECIMATE'); dec.ratio = max_tris / tris; dec.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=dec.name)
    # Un solo material con nombre estable para el remapeo en Unity.
    mat = bpy.data.materials.new('M_Valle_' + name)
    ob.data.materials.clear(); ob.data.materials.append(mat)
    # Pivote en la base, centrado.
    import mathutils
    vs = np.array([v.co[:] for v in ob.data.vertices])
    lo, hi = vs.min(0), vs.max(0)
    off = mathutils.Vector(((lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, lo[2]))
    for v in ob.data.vertices: v.co -= off
    ob.location = (0, 0, 0)
    final_tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    fbx = os.path.join(OUT_MODELS, ob.name + '.fbx')
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'MESH'}, axis_forward='-Z', axis_up='Y',
                             bake_space_transform=True, apply_scale_options='FBX_SCALE_ALL', mesh_smooth_type='FACE',
                             path_mode='STRIP', add_leaf_bones=False)
    # Texturas
    t = os.path.join(d, 'textures')
    def tex(kind): return os.path.join(t, f"{texp}_{kind}_1k.jpg")
    base = 'T_Valle_' + name
    shutil.copy(tex('diff'), os.path.join(OUT_TEX, base + '_BaseColor.jpg'))
    shutil.copy(tex('nor_gl'), os.path.join(OUT_TEX, base + '_Normal.jpg'))
    arm = bpy.data.images.load(tex('arm'))
    w, h = arm.size
    px = np.array(arm.pixels[:], np.float32).reshape(h, w, 4)
    ms = np.zeros_like(px)
    ms[..., 0] = px[..., 2]            # metal
    ms[..., 1] = px[..., 0]            # oclusión (por si se usa)
    ms[..., 3] = 1.0 - px[..., 1]      # suavidad = 1 - rugosidad
    img = bpy.data.images.new(base + '_MetalSmooth', w, h, alpha=True)
    img.colorspace_settings.name = 'Non-Color'
    img.pixels.foreach_set(ms.ravel())
    img.filepath_raw = os.path.join(OUT_TEX, base + '_MetalSmooth.png'); img.file_format = 'PNG'; img.save()
    dims = [round(float(x), 2) for x in (hi - lo)]
    report.append(f"{aid} -> {ob.name}: {tris} -> {final_tris} triángulos, medidas {dims}")

print("\n".join("REPORTE " + r for r in report))
