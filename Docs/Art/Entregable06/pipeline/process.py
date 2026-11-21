import bpy, sys, os, glob, json, shutil, re
sys.path.insert(0, os.path.dirname(__file__))
import importlib, common; importlib.reload(common)
from common import *

args = sys.argv[sys.argv.index('--') + 1:]
RAW, OUT = args[0], args[1]
only = set(args[2].split(',')) if len(args) > 2 and args[2] else None
report = {}

L = 'ABCDEFGHIJKLMNOP'
SPECS = [
    ('rock_moss_set_01', 'RockMossLarge', 2048, False, [(f'rock0{i+1}', f'SM_RockMossLarge_{L[i]}', 4000) for i in range(6)]),
    ('rock_moss_set_02', 'RockMoss', 2048, False, [(f'rock{7+i:02d}', f'SM_RockMoss_{L[i]}', 3000) for i in range(7)]),
    ('boulder_01', 'BoulderDryA', 2048, False, [('_LOD3', 'SM_BoulderDry_A', 8300)]),
    ('namaqualand_boulder_02', 'BoulderDryB', 2048, False, [('_LOD0', 'SM_BoulderDry_B', 8000)]),
    ('dead_tree_trunk_02', 'DeadLog', 2048, False, [('_LOD3', 'SM_DeadLog_A', 10400)]),
    ('dry_branches_medium_01', 'DryBranches', 2048, False, [(f'_{c}', f'SM_DryBranch_{c.upper()}', 2500) for c in 'abc']),
    ('tree_stump_01', 'Stump', 2048, False, [('tree_stump_01', 'SM_Stump_A', 6000)]),
    ('root_cluster_01', 'RootCluster', 1024, False, [('root_cluster_01', 'SM_RootCluster_A', 9000)]),
    ('pine_roots', None, 1024, False, [('pine_roots_a', 'SM_PineRoots_A', 6000), ('pine_roots_b', 'SM_PineRoots_B', 6000)]),
    ('fern_02', 'Fern', 1024, True, [(f'_{c}', f'SM_Fern_{c.upper()}', 2400) for c in 'abcd']),
    ('grass_medium_01', 'GrassA', 1024, True, [(f'_{s}_LOD0', f'SM_GrassA_{s.title().replace("_", "")}', 2000)
        for s in ['large_a', 'large_b', 'large_c', 'mid_a', 'mid_b', 'mid_c', 'small_a', 'small_b', 'tall_a', 'tall_b', 'tall_c']]),
    ('grass_medium_02', 'GrassB', 1024, True, [(f'_{c}', f'SM_GrassB_{c.upper()}', 2000) for c in 'abcde']),
    ('shrub_04', 'Shrub', 1024, True, [(f'_{c}_LOD0', f'SM_Shrub_{c.upper()}', 2500) for c in 'abcd']),
    ('celandine_01', 'FlowerYellow', 1024, True, [(f'_{c}_LOD0', f'SM_FlowerYellow_{c.upper()}', 1500) for c in 'abcde']),
    ('dandelion_01', 'Dandelion', 1024, True, [(f'_{c}_LOD1', f'SM_Dandelion_{c.upper()}', 2000) for c in 'abcde']),
]

GROUND = {
    'forrest_ground_01': 'ForestGround', 'brown_mud_leaves_01': 'MudLeaves', 'mud_cracked_dry_riverbed_002': 'DryRiverbed',
    'dry_ground_rocks': 'DryGroundRocks', 'river_small_rocks': 'RiverPebbles', 'mossy_rock': 'MossyRock',
    'rock_boulder_dry': 'RockDry', 'leafy_grass': 'LeafyGrass', 'bark_brown_02': 'Bark', 'rocky_terrain_02': 'RockyTerrain',
}

def find(folder, prefix, kind):
    hits = sorted(glob.glob(os.path.join(folder, f'{prefix}*_{kind}_*')))
    return hits[0] if hits else None

def ground():
    import numpy as np
    for src, name in GROUND.items():
        if only and name not in only: continue
        f = os.path.join(RAW, 'textures', src)
        out = os.path.join(OUT, 'Textures', 'Ground')
        texture_set(name, out, find(f, src, 'diff'), find(f, src, 'nor_gl'), find(f, src, 'rough'),
                    ao=find(f, src, 'ao'), size=2048, mask_size=1024)
        # height into Mask.B
        mp = f'{out}/T_{name}_Mask.png'
        m, w, h = pixels(load(mp, True))
        hgt, _, _ = pixels(load(find(f, src, 'disp'), True), w)
        m = m.copy(); m[..., 2] = hgt[..., 0]
        save(m, mp)
        for im in list(bpy.data.images): bpy.data.images.remove(im)
        report[name] = 'ground 2k'
    for h in glob.glob(os.path.join(RAW, 'hdri', '*.hdr')):
        dst = os.path.join(OUT, 'Sky', 'HDRI_' + os.path.basename(h).replace('_2k', ''))
        os.makedirs(os.path.dirname(dst), exist_ok=True); shutil.copy2(h, dst)

def props():
    for src, setname, res, foliage, items in SPECS:
        sets = [setname] if setname else [it[1].replace('SM_', '').replace('_', '') for it in items]
        if only and not (set(sets) & only): continue
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=os.path.join(RAW, 'models', src, src + '.fbx'))
        meshes = [o for o in bpy.data.objects if o.type == 'MESH']
        tf = os.path.join(RAW, 'models', src, 'textures')
        texdir = os.path.join(OUT, 'Textures', 'Props')
        if setname:
            texture_set(setname, texdir, find(tf, src, 'diff'), find(tf, src, 'nor_gl'), find(tf, src, 'rough'),
                        alpha=find(tf, src, 'alpha'), size=res)
        for token, outname, target in items:
            cands = [o for o in meshes if o.name.endswith(token) or o.name == token]
            if not cands:
                cands = [o for o in meshes if token in o.name]
            if not cands:
                print('MISSING', src, token); continue
            ob = cands[0]
            tset = setname or outname.replace('SM_', '').replace('_', '')
            if not setname:
                texture_set(tset, texdir, find(tf, token, 'diff'), find(tf, token, 'nor_gl'), find(tf, token, 'rough'), size=res)
            if ob.parent:
                mw = ob.matrix_world.copy(); ob.parent = None; ob.matrix_world = mw
            apply_transforms(ob)
            before = tri_count(ob)
            after = decimate(ob, target)
            dims = set_origin_base(ob, 'base')
            mat = bpy.data.materials.new('M_' + tset)
            ob.data.materials.clear(); ob.data.materials.append(mat)
            ob.name = outname; ob.data.name = outname
            export_fbx([ob], os.path.join(OUT, 'Models', outname + '.fbx'))
            report[outname] = {'src': src, 'tris_before': before, 'tris': after, 'dims_m': [round(x, 3) for x in dims],
                               'material': 'M_' + tset, 'foliage': foliage}
            print('@@', outname, before, '->', after, [round(x, 2) for x in dims])

if __name__ == '__main__':
    if not only or (only & set(GROUND.values())): ground()
    props()
    rp = os.path.join(OUT, '..', 'process_report.json')
    old = json.load(open(rp)) if os.path.exists(rp) else {}
    old.update(report)
    json.dump(old, open(rp, 'w'), indent=1)
