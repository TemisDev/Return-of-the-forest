import bpy, numpy as np, os

def load(path, noncolor=False):
    im = bpy.data.images.load(path, check_existing=False)
    if noncolor:
        im.colorspace_settings.name = 'Non-Color'
    return im

def pixels(im, size=None):
    if size and (im.size[0] != size or im.size[1] != size):
        im.scale(size, size)
    w, h = im.size
    a = np.empty(w * h * 4, np.float32)
    im.pixels.foreach_get(a)
    return a.reshape(h, w, 4), w, h

def save(arr, path, alpha=True, quality=92):
    h, w, _ = arr.shape
    fmt = 'PNG' if path.lower().endswith('.png') else 'JPEG'
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=alpha, float_buffer=False)
    im.colorspace_settings.name = 'Non-Color'  # raw write, no transform
    im.pixels.foreach_set(np.clip(arr, 0, 1).astype(np.float32).ravel())
    im.filepath_raw = path
    im.file_format = fmt
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if fmt == 'JPEG':
        im.save(filepath=path, quality=quality)
    else:
        im.save(filepath=path)
    bpy.data.images.remove(im)

def texture_set(name, outdir, diff, nor, rough, alpha=None, ao=None, size=None, mask_size=None):
    """URP Lit set: BaseMap (jpg or png+alpha), Normal (jpg, OpenGL), Mask (png: R metal 0, G AO, B 0, A smoothness)."""
    d, w, h = pixels(load(diff, True), size)  # raw: evita linealizar PNG de 16 bits
    if alpha:
        a, _, _ = pixels(load(alpha, True), w)
        d = d.copy(); d[..., 3] = a[..., 0]
        save(d, f'{outdir}/T_{name}_BaseMap.png')
    elif d[..., 3].min() < 0.99:
        save(d, f'{outdir}/T_{name}_BaseMap.png')
    else:
        save(d, f'{outdir}/T_{name}_BaseMap.jpg', alpha=False)
    n, _, _ = pixels(load(nor, True), w)
    n = n.copy(); n[..., 3] = 1
    save(n, f'{outdir}/T_{name}_Normal.jpg', alpha=False, quality=95)
    ms = mask_size or max(512, w // 2)
    r, _, _ = pixels(load(rough, True), ms)
    m = np.zeros_like(r)
    m[..., 3] = np.clip(1.0 - r[..., 0], 0, 1)
    if ao:
        o, _, _ = pixels(load(ao, True), ms)
        m[..., 1] = o[..., 0]
    else:
        m[..., 1] = 1.0
    save(m, f'{outdir}/T_{name}_Mask.png')
    for im in list(bpy.data.images):
        bpy.data.images.remove(im)

def tri_count(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)

def decimate(ob, target):
    cur = tri_count(ob)
    if cur <= target:
        return cur
    mod = ob.modifiers.new('dec', 'DECIMATE')
    mod.decimate_type = 'COLLAPSE'
    mod.ratio = target / cur
    mod.use_collapse_triangulate = True
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    old = ob.data
    ob.modifiers.clear()
    ob.data = me
    bpy.data.meshes.remove(old)
    return tri_count(ob)

def apply_transforms(ob):
    mw = ob.matrix_world.copy()
    ob.data.transform(mw)
    ob.matrix_world.identity()
    ob.data.update()

def set_origin_base(ob, mode='base'):
    co = np.empty(len(ob.data.vertices) * 3, np.float32)
    ob.data.vertices.foreach_get('co', co)
    co = co.reshape(-1, 3)
    mn, mx = co.min(0), co.max(0)
    c = (mn + mx) / 2
    off = np.array([c[0], c[1], mn[2] if mode == 'base' else c[2]])
    co -= off
    ob.data.vertices.foreach_set('co', co.ravel())
    ob.data.update()
    return (mx - mn).tolist()

def export_fbx(objs, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH'},
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                             mesh_smooth_type='OFF', use_mesh_modifiers=False, add_leaf_bones=False,
                             path_mode='STRIP', embed_textures=False)
