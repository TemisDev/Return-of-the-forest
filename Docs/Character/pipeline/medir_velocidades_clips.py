import bpy, sys
path = sys.argv[sys.argv.index('--') + 1]
bpy.ops.import_scene.fbx(filepath=path)
arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
fps = bpy.context.scene.render.fps
want = ['Idle_Loop', 'Walk_Loop', 'Walk_Formal_Loop', 'Jog_Fwd_Loop', 'Sprint_Loop', 'Jump_Start', 'Jump_Loop', 'Jump_Land', 'Interact', 'PickUp_Table', 'Spell_Simple_Shoot', 'Spell_Simple_Enter', 'Push_Loop', 'Roll', 'Fixing_Kneeling']
for act in bpy.data.actions:
    name = act.name.split('|')[-1]
    if name not in want: continue
    arm.animation_data.action = act
    f0, f1 = act.frame_range
    sc = bpy.context.scene
    sc.frame_set(int(f0)); p0 = (arm.matrix_world @ arm.pose.bones['root'].matrix).translation.copy(); h0 = (arm.matrix_world @ arm.pose.bones['pelvis'].matrix).translation.copy()
    sc.frame_set(int(f1)); p1 = (arm.matrix_world @ arm.pose.bones['root'].matrix).translation.copy()
    dur = (f1 - f0) / fps; d = (p1 - p0); d.z = 0
    print(f'CLIP {name}: {dur:.2f}s frames {f0:.0f}-{f1:.0f} root_dist {d.length:.2f} m speed {d.length/max(dur,1e-3):.2f} m/s')
