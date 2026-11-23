"""Convierte el muelle modular de Poly Haven en piezas de puente para la zona 4 (V13)."""
import bpy, sys, glob, os, shutil, bmesh, mathutils
import numpy as np
argv = sys.argv[sys.argv.index('--') + 1:]
SRC, OUT_MODELS, OUT_TEX = argv
PIECES = {  # objeto: (nombre, triángulos máximos, pivote)
    "modular_wooden_pier_section_02": ("PuenteTramoA", 4500, "deck"),
    "modular_wooden_pier_section_03": ("PuenteTramoB", 4500, "deck"),
    "modular_wooden_pier_section_01": ("PuenteTramoC", 6000, "deck"),
    "modular_wooden_pier_planks": ("Tablas", 2500, "center"),
    "POLE": ("Poste", 600, "base"),
}
report = []
def load():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=glob.glob(SRC + '/*.gltf')[0])
for src, (name, max_tris, pivot) in PIECES.items():
    load()
    if src == "POLE":
        ob = bpy.data.objects["modular_wooden_pier_poles"]
        bpy.context.view_layer.objects.active = ob; ob.select_set(True)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.separate(type='LOOSE'); bpy.ops.object.mode_set(mode='OBJECT')
        parts = [o for o in bpy.data.objects if o.type == 'MESH' and o.name.startswith("modular_wooden_pier_poles")]
        ob = max(parts, key=lambda o: o.dimensions.z)
        for o in list(bpy.data.objects):
            if o != ob: bpy.data.objects.remove(o, do_unlink=True)
    else:
        ob = bpy.data.objects[src]
        for o in list(bpy.data.objects):
            if o != ob: bpy.data.objects.remove(o, do_unlink=True)
    ob.parent = None
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    if tris > max_tris:
        dec = ob.modifiers.new('Reducir', 'DECIMATE'); dec.ratio = max_tris / tris; dec.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=dec.name)
    # Materiales con nombre estable.
    for slot in ob.material_slots:
        n = slot.material.name
        slot.material = bpy.data.materials.new("M_Valle_MaderaTablas" if "planks" in n else "M_Valle_MaderaPostes")
    vs = np.array([v.co[:] for v in ob.data.vertices]); lo, hi = vs.min(0), vs.max(0)
    if pivot == "deck":
        # Parte superior de las tablas = 0; centro del tramo en X/Y.
        pi = [i for i, sl in enumerate(ob.material_slots) if "Tablas" in sl.material.name][0]
        zs = [ob.data.vertices[v].co.z for f in ob.data.polygons if f.material_index == pi for v in f.vertices]
        top = float(np.percentile(zs, 90))
        off = mathutils.Vector(((lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, top))
    elif pivot == "base":
        off = mathutils.Vector(((lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, lo[2]))
    else:
        off = mathutils.Vector(((lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, (lo[2] + hi[2]) / 2))
    for v in ob.data.vertices: v.co -= off
    ob.name = 'SM_Valle_' + name; ob.data.name = ob.name
    vs = np.array([v.co[:] for v in ob.data.vertices]); lo, hi = vs.min(0), vs.max(0)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_MODELS, ob.name + '.fbx'), use_selection=True, object_types={'MESH'},
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True, apply_scale_options='FBX_SCALE_ALL',
                             mesh_smooth_type='FACE', path_mode='STRIP', add_leaf_bones=False)
    report.append(f"{src} -> {ob.name}: {tris} -> {sum(len(p.vertices) - 2 for p in ob.data.polygons)} tris, min {np.round(lo,2)} max {np.round(hi,2)} mats {[s.material.name for s in ob.material_slots]}")
t = os.path.join(SRC, 'textures')
for part, name in (("planks", "MaderaTablas"), ("poles", "MaderaPostes")):
    base = 'T_Valle_' + name
    shutil.copy(os.path.join(t, f"modular_wooden_pier_{part}_diff_1k.jpg"), os.path.join(OUT_TEX, base + '_BaseColor.jpg'))
    shutil.copy(os.path.join(t, f"modular_wooden_pier_{part}_nor_gl_1k.jpg"), os.path.join(OUT_TEX, base + '_Normal.jpg'))
    arm = bpy.data.images.load(os.path.join(t, f"modular_wooden_pier_{part}_arm_1k.jpg"))
    w, h = arm.size; px = np.array(arm.pixels[:], np.float32).reshape(h, w, 4)
    ms = np.zeros_like(px); ms[..., 0] = px[..., 2]; ms[..., 1] = px[..., 0]; ms[..., 3] = 1 - px[..., 1]
    img = bpy.data.images.new(base + '_MetalSmooth', w, h, alpha=True); img.colorspace_settings.name = 'Non-Color'
    img.pixels.foreach_set(ms.ravel()); img.filepath_raw = os.path.join(OUT_TEX, base + '_MetalSmooth.png'); img.file_format = 'PNG'; img.save()
print("\n".join("REPORTE " + r for r in report))
