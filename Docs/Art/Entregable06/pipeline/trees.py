import bpy, bmesh, sys, os, json, math
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
import importlib, common; importlib.reload(common)
from common import *

args = sys.argv[sys.argv.index('--') + 1:]
RAW, OUT = args[0], args[1]
rng = np.random.default_rng(7)
report = {}

# ---------------------------------------------------------------- leaf cluster textures
LEAF_RECTS = [(15, 20, 150, 525), (165, 20, 345, 405), (365, 30, 495, 372), (525, 45, 668, 372), (705, 18, 838, 428),
              (12, 640, 192, 1024), (218, 588, 368, 1024), (428, 612, 580, 1024)]

def leaf_sprites():
    tf = os.path.join(RAW, 'models', 'island_tree_02', 'textures')
    d, w, h = pixels(load(os.path.join(tf, 'island_tree_02_leaves_diff_1k.png'), True))
    a, _, _ = pixels(load(os.path.join(tf, 'island_tree_02_leaves_alpha_1k.png'), True))
    img = d.copy(); img[..., 3] = a[..., 0]
    img = img[::-1]  # blender rows bottom-up -> top-down
    sprites = []
    for x0, y0, x1, y1 in LEAF_RECTS:
        s = img[y0:y1, x0:x1].copy()
        s[..., 3] = np.where(s[..., 3] > 0.5, s[..., 3], 0)
        sprites.append(s)
    return sprites

def stamp(canvas, hcanvas, spr, cx, cy, ang, scale, tint, dome=1.0, desat=False):
    sh, sw = spr.shape[:2]
    H, W = canvas.shape[:2]
    r = int(max(sh, sw) * scale / 2) + 2
    y0, y1, x0, x1 = max(0, cy - r), min(H, cy + r), max(0, cx - r), min(W, cx + r)
    if y1 <= y0 or x1 <= x0: return
    ys, xs = np.mgrid[y0:y1, x0:x1]
    dx, dy = xs - cx, ys - cy
    ca, sa = math.cos(-ang), math.sin(-ang)
    u = (dx * ca - dy * sa) / scale + sw / 2
    v = (dx * sa + dy * ca) / scale + sh / 2
    ok = (u >= 0) & (u < sw - 1) & (v >= 0) & (v < sh - 1)
    ui, vi = u[ok].astype(int), v[ok].astype(int)
    px = spr[vi, ui]
    al = px[:, 3:4]
    yy, xx = ys[ok], xs[ok]
    col = px[:, :3]
    if desat:
        col = (col @ np.array([0.3, 0.59, 0.11]))[:, None] * 1.9
    col = col * tint
    canvas[yy, xx, :3] = canvas[yy, xx, :3] * (1 - al) + col * al
    canvas[yy, xx, 3] = np.maximum(canvas[yy, xx, 3], al[:, 0])
    # dome height for normals: distance from leaf centerline
    nu = (u[ok] / sw - 0.5) * 2
    hgt = np.clip(1 - nu * nu, 0, 1) * dome
    hcanvas[yy, xx] = np.where(al[:, 0] > 0.5, hgt + 0.02 * rng.random(), hcanvas[yy, xx])

def draw_twig(canvas, x0, y0, x1, y1, w0, w1, col):
    n = int(math.hypot(x1 - x0, y1 - y0))
    for i in range(n):
        t = i / max(1, n)
        x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        rr = w0 + (w1 - w0) * t
        y_a, y_b = int(max(0, y - rr)), int(max(0, min(canvas.shape[0], y + rr + 1)))
        x_a, x_b = int(max(0, x - rr)), int(max(0, min(canvas.shape[1], x + rr + 1)))
        if y_b <= y_a or x_b <= x_a: continue
        canvas[y_a:y_b, x_a:x_b, :3] = col
        canvas[y_a:y_b, x_a:x_b, 3] = 1

def cluster_texture(name, sprites, tint_base, count=46, size=1024, dry=False):
    C = np.zeros((size, size, 4), np.float32)
    Hm = np.zeros((size, size), np.float32)
    root = (size // 2, size - 30)
    twigs = []
    for k in range(7):
        ang = -math.pi / 2 + (k - 3) * 0.27 + rng.normal(0, 0.05)
        L = size * rng.uniform(0.6, 0.8) * (1 - abs(k - 3) * 0.09)
        x1, y1 = root[0] + math.cos(ang) * L, root[1] + math.sin(ang) * L
        twigs.append((root[0], root[1], x1, y1, ang))
        draw_twig(C, root[0], root[1], x1, y1, 4, 1.2, np.array([0.18, 0.12, 0.07]))
    for i in range(count):
        x0, y0, x1, y1, ang = twigs[rng.integers(len(twigs))]
        t = rng.uniform(0.18, 1.0) ** 0.7
        px, py = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        side = rng.choice([-1, 1])
        la = ang + side * rng.uniform(0.5, 1.1) + math.pi / 2
        depth = i / count
        shade = 0.78 + 0.34 * depth
        tint = np.array(tint_base) * shade * rng.uniform(0.9, 1.1, 3)
        if dry and rng.random() < 0.35: continue
        if not (90 < px < size - 90 and 90 < py < size - 40): continue
        stamp(C, Hm, sprites[rng.integers(len(sprites))], int(px + math.cos(la - math.pi / 2) * 28),
              int(py + math.sin(la - math.pi / 2) * 28), la, rng.uniform(0.26, 0.40), tint, desat=dry)
    # bleed colour into transparent pixels to avoid dark halos
    rgb = C[..., :3].copy(); a = C[..., 3]
    filled = a > 0.5
    for _ in range(12):
        nb = np.zeros_like(rgb); cnt = np.zeros(a.shape, np.float32)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            sh = np.roll(np.roll(rgb, dy, 0), dx, 1); sf = np.roll(np.roll(filled, dy, 0), dx, 1)
            nb += sh * sf[..., None]; cnt += sf
        grow = (~filled) & (cnt > 0)
        rgb[grow] = nb[grow] / cnt[grow][:, None]; filled = filled | grow
    C[..., :3] = rgb
    out = os.path.join(OUT, 'Textures', 'Foliage')
    save(C[::-1], f'{out}/T_{name}_BaseMap.png')
    gy, gx = np.gradient(Hm * 6.0)
    n = np.stack([-gx, gy, np.ones_like(gx)], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    N = np.concatenate([n * 0.5 + 0.5, np.ones(a.shape + (1,), np.float32)], -1)
    save(N[::-1], f'{out}/T_{name}_Normal.jpg', alpha=False, quality=95)
    M = np.zeros((size // 2, size // 2, 4), np.float32); M[..., 1] = 1; M[..., 3] = 0.28 if not dry else 0.12
    save(M, f'{out}/T_{name}_Mask.png')
    report['T_' + name] = 'procedural leaf cluster from island_tree_02 leaf photos (CC0)'

# ---------------------------------------------------------------- tree meshes
def split_by_material(ob):
    parts = {}
    for idx, mat in enumerate(ob.data.materials):
        me = ob.data.copy()
        bm = bmesh.new(); bm.from_mesh(me)
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index != idx], context='FACES')
        bm.to_mesh(me); bm.free()
        o = bpy.data.objects.new(mat.name + '_part', me)
        bpy.context.scene.collection.objects.link(o)
        o.data.materials.clear(); o.data.materials.append(mat)
        parts[mat.name] = o
    return parts

def leaf_cards(leaf_ob, cell, card_scale, center):
    me = leaf_ob.data
    n = len(me.polygons)
    c = np.empty(n * 3, np.float32); me.polygons.foreach_get('center', c); c = c.reshape(-1, 3)
    key = np.floor(c / cell).astype(np.int64)
    uniq, inv, cnt = np.unique(key, axis=0, return_inverse=True, return_counts=True)
    inv = inv.ravel()
    sums = np.zeros((len(uniq), 3)); np.add.at(sums, inv, c)
    cent = sums / cnt[:, None]
    keep = cnt >= max(3, np.percentile(cnt, 15))
    cent = cent[keep]
    verts, faces, uvs, nrm = [], [], [], []
    for p in cent:
        out = p - center; out[2] *= 0.6
        out /= (np.linalg.norm(out) + 1e-6)
        s = cell * card_scale * rng.uniform(0.85, 1.2)
        for k in range(2):
            yaw = rng.uniform(0, math.pi) + k * math.pi / 2
            ax = np.array([math.cos(yaw), math.sin(yaw), 0.0])
            up = np.array([0, 0, 1.0]) * 0.85 + out * 0.3
            up /= np.linalg.norm(up)
            ax = ax - up * ax.dot(up); ax /= np.linalg.norm(ax)
            tilt = rng.uniform(-0.35, 0.35)
            q = [p - ax * s / 2 - up * s * (0.5 + tilt * 0.2), p + ax * s / 2 - up * s * (0.5 - tilt * 0.2),
                 p + ax * s / 2 + up * s * 0.5, p - ax * s / 2 + up * s * 0.5]
            b = len(verts); verts += [tuple(v) for v in q]
            faces.append((b, b + 1, b + 2, b + 3))
            uvs += [(0, 0), (1, 0), (1, 1), (0, 1)]
            nrm += [tuple(out)] * 4
    m = bpy.data.meshes.new('cards'); m.from_pydata(verts, [], faces); m.update()
    uvl = m.uv_layers.new(name='UVMap')
    uvl.data.foreach_set('uv', np.array(uvs, np.float32).ravel())
    m.normals_split_custom_set(nrm)
    o = bpy.data.objects.new('cards', m); bpy.context.scene.collection.objects.link(o)
    return o, len(cent)

def tree(src, outname, trunk_mats, branch_mat, leaf_mat, targets, cell, card_scale):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(RAW, 'models', src, src + '.fbx'))
    ob = [o for o in bpy.data.objects if o.type == 'MESH'][0]
    if ob.parent:
        mw = ob.matrix_world.copy(); ob.parent = None; ob.matrix_world = mw
    apply_transforms(ob)
    co = np.empty(len(ob.data.vertices) * 3, np.float32); ob.data.vertices.foreach_get('co', co); co = co.reshape(-1, 3)
    mn, mx = co.min(0), co.max(0)
    base = np.array([(mn[0] + mx[0]) / 2, (mn[1] + mx[1]) / 2, mn[2]])
    parts = split_by_material(ob)
    bpy.data.objects.remove(ob)
    for o in parts.values():
        o.data.transform(__import__('mathutils').Matrix.Translation(-__import__('mathutils').Vector(base)))
    wood = []
    for mname, tgt in targets.items():
        o = parts[mname]
        before = tri_count(o); after = decimate(o, tgt)
        o.data.materials.clear()
        o.data.materials.append(bpy.data.materials.new('M_' + trunk_mats[mname]))
        wood.append(o); print('@@', outname, mname, before, '->', after)
    leaf = parts[leaf_mat]
    lc = np.empty(len(leaf.data.vertices) * 3, np.float32); leaf.data.vertices.foreach_get('co', lc); lc = lc.reshape(-1, 3)
    center = (lc.min(0) + lc.max(0)) / 2
    cards, ncl = leaf_cards(leaf, cell, card_scale, center)
    bpy.data.objects.remove(leaf)
    wood_names = {o.name for o in wood}
    for o in list(bpy.data.objects):
        if o.type == 'MESH' and o.name not in wood_names and o is not cards:
            bpy.data.objects.remove(o)
    # join wood into one object with 2 materials
    bpy.ops.object.select_all(action='DESELECT')
    for o in wood: o.select_set(True)
    bpy.context.view_layer.objects.active = wood[0]
    with bpy.context.temp_override(active_object=wood[0], selected_editable_objects=wood, selected_objects=wood):
        bpy.ops.object.join()
    w = wood[0]; w.name = outname; w.data.name = outname
    cards.name = outname + '_Canopy'; cards.data.name = cards.name
    cards.data.materials.append(bpy.data.materials.new('M_LeafCluster'))
    export_fbx([w], os.path.join(OUT, 'Models', outname + '.fbx'))
    # LOD1 de madera para distancia media/lejana
    lod = w.copy(); lod.data = w.data.copy(); bpy.context.scene.collection.objects.link(lod)
    decimate(lod, 2600); lod.name = outname + '_LOD1'; lod.data.name = lod.name
    export_fbx([lod], os.path.join(OUT, 'Models', outname + '_LOD1.fbx'))
    report[outname + '_LOD1'] = {'wood_tris': tri_count(lod)}
    export_fbx([cards], os.path.join(OUT, 'Models', outname + '_Canopy.fbx'))
    dims = (mx - mn).tolist()
    report[outname] = {'src': src, 'wood_tris': tri_count(w), 'canopy_cards': ncl, 'canopy_tris': tri_count(cards),
                       'dims_m': [round(x, 2) for x in dims]}
    print('@@', outname, report[outname])

def wood_textures():
    tp = os.path.join(OUT, 'Textures', 'Props')
    t1 = os.path.join(RAW, 'models', 'island_tree_02', 'textures')
    texture_set('TreeABark', tp, f'{t1}/island_tree_02_diff_1k.jpg', f'{t1}/island_tree_02_nor_gl_1k.exr', f'{t1}/island_tree_02_rough_1k.exr')
    texture_set('TreeABranches', tp, f'{t1}/island_tree_02_branches_diff_1k.png', f'{t1}/island_tree_02_branches_nor_gl_1k.png', f'{t1}/island_tree_02_branches_rough_1k.png')
    t2 = os.path.join(RAW, 'models', 'tree_small_02', 'textures')
    texture_set('TreeBBark', tp, f'{t2}/tree_small_02_diff_1k.jpg', f'{t2}/tree_small_02_nor_gl_1k.exr', f'{t2}/tree_small_02_rough_1k.exr')
    texture_set('TreeBBranches', tp, f'{t2}/tree_small_02_branch_diff_1k.png', f'{t2}/tree_small_02_branch_nor_gl_1k.png', f'{t2}/tree_small_02_branch_rough_1k.png')

if __name__ == '__main__':
    spr = leaf_sprites()
    cluster_texture('LeafClusterGreen', spr, (1.0, 1.05, 0.92), count=150)
    cluster_texture('LeafClusterLush', spr, (0.88, 1.08, 0.82), count=170)
    cluster_texture('LeafClusterDry', spr, (0.95, 0.62, 0.36), count=80, dry=True)
    for im in list(bpy.data.images): bpy.data.images.remove(im)
    wood_textures()
    tree('island_tree_02', 'SM_TreeBroadleaf_A', {'island_tree_02': 'TreeABark', 'island_tree_02_branches': 'TreeABranches'},
         'island_tree_02_branches', 'island_tree_02_leaves', {'island_tree_02': 5000, 'island_tree_02_branches': 9000}, 0.42, 2.3)
    tree('tree_small_02', 'SM_TreeBroadleaf_B', {'tree_small_02_trunk': 'TreeBBark', 'tree_small_02_branches': 'TreeBBranches'},
         'tree_small_02_branches', 'tree_small_02_leaves', {'tree_small_02_trunk': 5000, 'tree_small_02_branches': 8000}, 0.45, 2.3)
    rp = os.path.join(OUT, '..', 'process_report.json')
    old = json.load(open(rp)) if os.path.exists(rp) else {}
    old.update(report); json.dump(old, open(rp, 'w'), indent=1)
