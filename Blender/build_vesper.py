# -*- coding: utf-8 -*-
"""
MGSUGE - Jalon 2 : construit le chassis de jeu "Vesper" a partir du modele d'art GX-55 Proto.
 - echelle de jeu : centre du bassin a 5,7 m du sol (FRAME_CLEAR), pivot au centre du bassin
 - squelette aux noms du cahier des charges (Root, Body, Torso, Head, Pack, Thruster_*, Wing_*,
   Shoulder/Elbow/Hand, Rifle, Muzzle, Blade, MuzzleL, MissilePod, Lid, PodOut, Hip/Knee/Ankle, Nose, NoseTip)
 - pieces rigides fusionnees par os (une piece = un os), facettes (flat shading)
 - materiaux ranges par role (Armor, Frame, Accent, Visor, Glow, Flame, Blade)
 - poses de reference (Mech, Ship, Crouch, BladeRaise, BladeSweep, LidOpen) -> actions Blender + JSON Unity
Usage : python3 -I build_vesper.py -- <art.blend> <dossier_sortie>
"""
import bpy, bmesh, math, json, sys, os
from mathutils import Vector, Matrix, Euler, Quaternion

SRC, OUT = sys.argv[sys.argv.index('--') + 1:][:2]
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=SRC)
O = bpy.data.objects
rig = O['GX55_Rig']
AB = rig.data.bones

# ------------------------------------------------------------------ echelle / repere de jeu
HIP = rig.matrix_world @ AB['Hips'].head_local
FRAME_CLEAR = 7.0   # valide par l'auteur le 28/10/2026 : chassis de 12 m (prototype : 5,7 m)
S = FRAME_CLEAR / HIP.z
G = Matrix.Scale(S, 4) @ Matrix.Translation(-HIP)          # espace art -> espace modele de jeu (axes Blender)
G3 = G.to_3x3()
def gp(v): return G @ Vector(v)
print(f"echelle de jeu S={S:.5f} (bassin art {HIP.z:.3f} m -> {FRAME_CLEAR} m)")

def bbox_world(o):
    pts = [o.matrix_world @ v.co for v in o.data.vertices]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx
def bbox_in(o, Fi):
    pts = [Fi @ (o.matrix_world @ v.co) for v in o.data.vertices]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx
def center(o):
    a, b = bbox_world(o); return (a + b) / 2

# ------------------------------------------------------------------ 1. fusil dans la main droite
gun = O['GX55_RayGun']
grip_c = center(O['RayGun_Poignee'])
hand_r = O['Socket_Main_D'].matrix_world.translation.copy()
Rg = Matrix.Rotation(math.radians(-90), 4, 'X')   # canon (vers le bas sur la hanche) -> vers l'avant, poignee vers le bas
gun.matrix_world = Matrix.Translation(hand_r) @ Rg @ Matrix.Translation(-grip_c) @ gun.matrix_world
bpy.context.view_layer.update()
muzzle_art = center(O['RayGun_Emetteur'])
grip_art = center(O['RayGun_Poignee'])
print("fusil : poignee", tuple(round(x, 2) for x in grip_art), "bouche", tuple(round(x, 2) for x in muzzle_art))

# ------------------------------------------------------------------ helpers de construction (espace art)
NEW = []   # (objet, os de jeu)
def mk(name, bm, mat, bone):
    me = bpy.data.meshes.new(name); bm.normal_update(); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob)
    if isinstance(mat, (list, tuple)):
        for m in mat: me.materials.append(bpy.data.materials[m])
    else: me.materials.append(bpy.data.materials[mat])
    NEW.append((ob, bone)); return ob
def frame_from(z, yhint):
    z = z.normalized(); y = yhint - z * yhint.dot(z)
    if y.length < 1e-5: y = Vector((0, 1, 0)) - z * z.y
    y.normalize(); x = y.cross(z).normalized()
    return Matrix((x, y, z)).transposed()
def ring(bm, c, M3, r, n, rot=0.0, sx=1.0, sy=1.0):
    return [bm.verts.new(c + M3 @ Vector((r * sx * math.cos(rot + 2 * math.pi * i / n), r * sy * math.sin(rot + 2 * math.pi * i / n), 0))) for i in range(n)]
def bridge(bm, a, b, mi=0):
    n = len(a)
    for i in range(n):
        f = bm.faces.new((a[i], a[(i + 1) % n], b[(i + 1) % n], b[i])); f.material_index = mi
def cap(bm, ringv, flip=False, mi=0):
    f = bm.faces.new(list(reversed(ringv)) if flip else ringv); f.material_index = mi; return f
def cone(bm, base, d, r, L, n=8, mi=0, tip_r=0.0, rot=math.pi / 8, sx=1.0, sy=1.0):
    M3 = frame_from(d, Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((0, 1, 0)))
    a = ring(bm, base, M3, r, n, rot, sx, sy)
    if tip_r <= 0:
        t = bm.verts.new(base + d.normalized() * L)
        for i in range(n):
            f = bm.faces.new((a[i], a[(i + 1) % n], t)); f.material_index = mi
    else:
        b = ring(bm, base + d.normalized() * L, M3, tip_r, n, rot, sx, sy); bridge(bm, a, b, mi); cap(bm, b, False, mi)
    cap(bm, a, True, mi)

def ensure_mat(name, base, emis=None, estr=0.0, metal=0.0, rough=0.4, alpha=1.0):
    if name in bpy.data.materials: return bpy.data.materials[name]
    m = bpy.data.materials.new(name)
    try: m.use_nodes = True
    except Exception: pass
    b = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    b.inputs['Base Color'].default_value = base; b.inputs['Metallic'].default_value = metal
    b.inputs['Roughness'].default_value = rough; b.inputs['Alpha'].default_value = alpha
    if emis: b.inputs['Emission Color'].default_value = emis; b.inputs['Emission Strength'].default_value = estr
    m.diffuse_color = base
    return m
def s2l(h):
    h = h.lstrip('#'); f = lambda c: (c / 255) / 12.92 if c / 255 <= 0.04045 else ((c / 255 + 0.055) / 1.055) ** 2.4
    return (f(int(h[0:2], 16)), f(int(h[2:4], 16)), f(int(h[4:6], 16)), 1.0)
FLAME = ensure_mat('GX55_Flamme', s2l('#FFC27A'), s2l('#FFC27A'), 6.0, alpha=0.85)

# ------------------------------------------------------------------ 2. tuyeres arriere (jupe arriere) + flammes
THRUSTERS = []   # (nom, os parent, sortie art, direction art, rayon art)
for side, sg in (('L', 1), ('R', -1)):
    t = O['GX55_Dos_Tuyere_Flamme_' + ('G' if sg > 0 else 'D')]
    a, b = bbox_world(t)
    THRUSTERS.append(('Thruster_Down_' + side, 'Pack', Vector(((a.x + b.x) / 2, (a.y + b.y) / 2, a.z)), Vector((0, 0, -1)), (b.x - a.x) * 0.42))
    for k in (1, 2):
        t = O[f"GX55_Mollet_Tuyere_Flamme_{'G' if sg > 0 else 'D'}_{k}"]
        a, b = bbox_world(t)
        THRUSTERS.append((f'Thruster_Leg_{side}_{k}', 'Knee_' + side, Vector(((a.x + b.x) / 2, (a.y + b.y) / 2, a.z)), Vector((0, 0, -1)), (b.x - a.x) * 0.42))
    # sortie arriere : sur la grille existante de la jupe arriere (aucune geometrie ajoutee, fidelite au .blend)
    gr = O['GX55_Jupe_Arriere_Grilles']; ga, gb = bbox_world(gr); mwi = gr.matrix_world.inverted()
    org = Vector(((ga.x + gb.x) / 2 + sg * (gb.x - ga.x) * 0.28, gb.y + 5.0, (ga.z + gb.z) / 2))
    ok, loc, nor, _ = gr.ray_cast(mwi @ org, (mwi.to_3x3() @ Vector((0, -1, 0))).normalized())
    p = gr.matrix_world @ loc if ok else Vector((org.x, gb.y, org.z))
    n = (mwi.to_3x3().transposed() @ nor).normalized() if ok else Vector((0, 1, 0))
    if n.y < 0.3: n = Vector((0, 1, 0))
    THRUSTERS.append(('Thruster_Rear_' + side, 'Skirt_Back', p + n * 0.03, n, (gb.z - ga.z) * 0.38))

FLAMES = []
for name, parent, exitp, d, r in THRUSTERS:
    bm = bmesh.new(); cone(bm, exitp, d, r, 1.0 / S, n=8, mi=0)
    ob = mk('Flame_' + name, bm, 'GX55_Flamme', name)
    ob['origin_art'] = list(exitp)
    FLAMES.append((ob, exitp))

# ------------------------------------------------------------------ 3. nez du vaisseau (cache en forme chassis)
neck_top = Vector((0, -0.15, 17.05))
bm = bmesh.new()
M3 = frame_from(Vector((0, 0, 1)), Vector((0, -1, 0)))
rings = [ring(bm, neck_top + Vector((0, 0, h)), M3, r, 8, math.pi / 8, 1.0, 0.82) for h, r in ((0.0, 1.55), (2.2, 1.2), (4.6, 0.55))]
bridge(bm, rings[0], rings[1]); bridge(bm, rings[1], rings[2]); cap(bm, rings[0], True)
tipb = rings[2]; tip = bm.verts.new(neck_top + Vector((0, 0, 5.6)))
for i in range(8):
    f = bm.faces.new((tipb[i], tipb[(i + 1) % 8], tip)); f.material_index = 1
# liseres orange lateraux
for sg in (1, -1):
    c = neck_top + Vector((sg * 1.18, 0, 1.6))
    v = [bm.verts.new(c + Vector((sg * dx, dy, dz))) for dx, dy, dz in ((0.02, -0.5, -0.9), (0.02, 0.5, -0.9), (-0.32, 0.4, 1.4), (-0.32, -0.4, 1.4))]
    f = bm.faces.new(v if sg > 0 else list(reversed(v))); f.material_index = 2
mk('Nose_Mesh', bm, ['GX55_Armure_Blanche', 'GX55_Armure_Charbon', 'GX55_Orangeyellow'], 'Nose')
bm = bmesh.new(); cone(bm, neck_top + Vector((0, 0, 5.15)), Vector((0, 0, 1)), 0.28, 0.75, n=8)
mk('NoseTip_Mesh', bm, 'GX55_Emission_Orange', 'NoseTip')
NOSE_BASE, NOSE_TIP = neck_top, neck_top + Vector((0, 0, 5.6))

# ------------------------------------------------------------------ 4. couvercle de la nacelle a missiles (bouclier)
shield = O['GX55_Bouclier']; F = shield.matrix_world.copy(); Fi = F.inverted(); F3 = F.to_3x3()
pod = O['GX55_Bouclier_Lance_Missiles']
mn, mx = bbox_in(pod, Fi)
for mname in ('GX55_Bouclier_Missile_01', 'GX55_Bouclier_Missile_02'):
    a, b = bbox_in(O[mname], Fi); mx.z = max(mx.z, b.z)
hinge_local = Vector(((mn.x + mx.x) / 2, mn.y, mx.z + 0.01))
bm = bmesh.new()
w, dpt, th = (mx.x - mn.x) + 0.10, (mx.y - mn.y) + 0.06, 0.09
pts = [Vector((-w / 2, 0, 0)), Vector((w / 2, 0, 0)), Vector((w / 2, dpt, 0)), Vector((-w / 2, dpt, 0))]
bot = [bm.verts.new(F @ (hinge_local + p)) for p in pts]
top = [bm.verts.new(F @ (hinge_local + p + Vector((0, 0, th)))) for p in pts]
cap(bm, bot, True); cap(bm, top, False); bridge(bm, bot, top)
mk('Lid_Mesh', bm, 'GX55_Armure_Charbon', 'Lid')
LID_HINGE = F @ hinge_local
LID_AXIS = (F3 @ Vector((1, 0, 0))).normalized()
POD_C = F @ Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, (mn.z + mx.z) / 2))
POD_OUT = F @ Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mx.z + 0.6))
SHIELD_UP = (F3 @ Vector((0, 0, 1))).normalized()
SHIELD_FRONT = (F3 @ Vector((0, -1, 0))).normalized()
shield_tip = max((F @ v.co for o in (O['GX55_Bouclier_Face_D'], O['GX55_Bouclier_Face_G']) for v in o.data.vertices), key=lambda p: (Fi @ p).z)
MUZZLE_L = F @ Vector((0, (Fi @ shield_tip).y - 0.2, (Fi @ shield_tip).z + 0.2))

# ------------------------------------------------------------------ 5. lame d'energie : maillage d'origine du .blend, place a l'emetteur du sabre
sab_grip = center(O['Sabre_Poignee']); sab_emit = center(O['Sabre_Emetteur']); sab_pom = center(O['Sabre_Pommeau'])
blade_dir = (sab_emit - sab_pom).normalized()
emit_tip = sab_emit + blade_dir * 0.05
lame = O['Sabre_Lame_Energie']
lv = [v.co for v in lame.data.vertices]
base_local = Vector(((min(v.x for v in lv) + max(v.x for v in lv)) / 2, (min(v.y for v in lv) + max(v.y for v in lv)) / 2, min(v.z for v in lv)))
side = (O['GX55_Sabre_Plasma'].matrix_world.to_3x3() @ Vector((1, 0, 0))).normalized()
M3 = frame_from(blade_dir, side.cross(blade_dir))
lame.parent = None
lame.matrix_world = Matrix.Translation(emit_tip) @ M3.to_4x4() @ Matrix.Translation(-base_local)
lame.name = 'Blade_Energy'
NEW.append((lame, 'Blade'))
BLADE_LEN = (max(v.z for v in lv) - base_local.z) * S

# ------------------------------------------------------------------ 6. attribution des pieces aux os de jeu
BONE_MAP = {'Root': 'Root', 'Hips': 'Body', 'Spine': 'Spine', 'Chest': 'Torso', 'Neck': 'Neck', 'Head': 'Head', 'Skirt_Back': 'Skirt_Back'}
for s in ('L', 'R'):
    BONE_MAP.update({f'Shoulder.{s}': f'Clavicle_{s}', f'UpperArm.{s}': f'Shoulder_{s}', f'LowerArm.{s}': f'Elbow_{s}', f'Hand.{s}': f'Hand_{s}',
                     f'UpperLeg.{s}': f'Hip_{s}', f'LowerLeg.{s}': f'Knee_{s}', f'Foot.{s}': f'Ankle_{s}', f'Toes.{s}': f'Toe_{s}',
                     f'Skirt_Front.{s}': f'Skirt_Front_{s}', f'Skirt_Side.{s}': f'Skirt_Side_{s}'})
    for f in ('Thumb', 'Index', 'Middle', 'Ring', 'Little'):
        for k in (1, 2): BONE_MAP[f'{f}{k}.{s}'] = f'{f}{k}_{s}'
PACK_COLLS = {'12_Dos_Pack', '12_Details_Dos_Pack'}
def chain(o):
    out = []; p = o
    while p: out.append(p); p = p.parent
    return out
def game_bone(o):
    ch = chain(o); names = [c.name for c in ch]
    if 'GX55_RayGun' in names: return 'Rifle'
    if 'GX55_Sabre_Plasma' in names: return 'Blade'
    if 'GX55_Bouclier_Lance_Missiles' in names: return 'MissilePod'
    if 'GX55_Bouclier' in names: return 'Shield'
    for n in names:
        if n.startswith('GX55_Drone_0') and n[11:12].isdigit() and len(n) == 13: return 'Probe_' + n[12]
        if n.startswith('Pack_Bras_0'): return 'Wing_L' if int(n[-1]) <= 3 else 'Wing_R'
    if 'GX55_Pack_Drones' in names: return 'Pack'
    for c in ch:
        if any(col.name in PACK_COLLS for col in c.users_collection): return 'Pack'
    for c in ch:
        if c.parent_type == 'BONE' and c.parent == rig: return BONE_MAP[c.parent_bone]
    return None

groups = {}
skipped = []
for o in list(O):
    if o.type != 'MESH' or o.name.startswith(('Ref_', 'Sol_')) or o in [n[0] for n in NEW]: continue
    b = game_bone(o)
    if b is None: skipped.append(o.name); continue
    groups.setdefault(b, []).append(o)
print("pieces ignorees :", skipped)

# ------------------------------------------------------------------ 7. nouveau squelette a l'echelle de jeu
arm_data = bpy.data.armatures.new('MGSUGE_Vesper_Rig')
arm = bpy.data.objects.new('MGSUGE_Vesper', arm_data)
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active = arm
for o in bpy.context.selected_objects: o.select_set(False)
arm.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
EB = arm_data.edit_bones
def add(name, parent, head, ydir, zhint, length):
    e = EB.new(name); h = Vector(head); e.head = h; e.tail = h + Vector(ydir).normalized() * length
    e.align_roll(Vector(zhint)); e.use_connect = False; e.use_deform = False
    if parent: e.parent = EB[parent]
    return e
# os d'origine (renommes)
add('Root', None, (0, 0, 0), (0, 1, 0), (0, 0, 1), 0.6)
order = ['Hips', 'Spine', 'Chest', 'Neck', 'Head', 'Skirt_Back']
for s in ('L', 'R'):
    order += [f'Shoulder.{s}', f'UpperArm.{s}', f'LowerArm.{s}', f'Hand.{s}'] + [f'{f}{k}.{s}' for f in ('Thumb', 'Index', 'Middle', 'Ring', 'Little') for k in (1, 2)]
    order += [f'UpperLeg.{s}', f'LowerLeg.{s}', f'Foot.{s}', f'Toes.{s}', f'Skirt_Front.{s}', f'Skirt_Side.{s}']
for bn in order:
    ob = AB[bn]; e = EB.new(BONE_MAP[bn])
    e.head = gp(ob.head_local); e.tail = gp(ob.tail_local)
    e.align_roll(G3 @ ob.matrix_local.to_3x3().col[2]); e.use_connect = False; e.use_deform = False
for bn in order:
    ob = AB[bn]; par = ob.parent.name if ob.parent else 'Root'
    EB[BONE_MAP[bn]].parent = EB[BONE_MAP[par]]
EB['Body'].parent = EB['Root']
# nouveaux os
pack_c = gp(center(O['GX55_Dos_Pack']))
add('Pack', 'Torso', pack_c, (0, 1, 0), (0, 0, 1), 0.6)
for name, parent, exitp, d, r in THRUSTERS:
    add(name, parent, gp(exitp), d, (0, 1, 0) if abs(d.z) > 0.9 else (0, 0, 1), 0.4)
hub = gp(center(O['Pack_Moyeu']))
for side, sg in (('L', 1), ('R', -1)):
    add('Wing_' + side, 'Pack', hub + Vector((0.18 * sg, 0, -0.2)), (0, 1, 0), (0, 0, 1), 0.5)
for i in range(1, 7):
    so = O[f'Socket_Drone_0{i}']; R3 = so.matrix_world.to_3x3().normalized()
    add(f'Probe_{i}', 'Wing_L' if i <= 3 else 'Wing_R', gp(so.matrix_world.translation), R3.col[1], R3.col[2], 0.5)
add('Rifle', 'Hand_R', gp(grip_art), (0, -1, 0), (0, 0, 1), 0.6)
add('Muzzle', 'Rifle', gp(muzzle_art + Vector((0, -0.1, 0))), (0, -1, 0), (0, 0, 1), 0.3)
add('Shield', 'Elbow_L', gp(F.translation), SHIELD_UP, SHIELD_FRONT, 0.6)
add('MissilePod', 'Shield', gp(POD_C), SHIELD_UP, SHIELD_FRONT, 0.4)
add('Lid', 'MissilePod', gp(LID_HINGE), LID_AXIS, SHIELD_UP, 0.3)
add('PodOut', 'MissilePod', gp(POD_OUT), SHIELD_UP, SHIELD_FRONT, 0.3)
add('MuzzleL', 'Shield', gp(MUZZLE_L), SHIELD_UP, SHIELD_FRONT, 0.3)
add('Blade', 'Pack', gp(sab_grip), blade_dir, (0, 1, 0), 0.5)
add('BladeGrip', 'Hand_L', gp(O['Socket_Main_G'].matrix_world.translation), (0, -1, 0), (0, 0, 1), 0.5)
add('Nose', 'Torso', gp(NOSE_BASE), (0, 0, 1), (0, -1, 0), 0.8)
add('NoseTip', 'Nose', gp(NOSE_TIP), (0, 0, 1), (0, -1, 0), 0.3)
bpy.ops.object.mode_set(mode='OBJECT')
BONES = arm.data.bones
print("os :", len(BONES))

# ------------------------------------------------------------------ 8. fusion des pieces par os
# Fidelite au .blend : modificateurs (biseaux 2 segments + normales durcies) appliques tels quels,
# lissage et normales personnalisees conserves (fusion par l'operateur Joindre, qui garde les normales).
dg = bpy.context.evaluated_depsgraph_get()
MESH_OBJS = []
def bone_matrix(bn):
    return arm.matrix_world @ BONES[bn].matrix_local
def merge(name, objs, bone):
    Bm = bone_matrix(bone); Bi = Bm.inverted()
    temps = []
    for o in objs:
        ev = o.evaluated_get(dg)
        me = bpy.data.meshes.new_from_object(ev, preserve_all_data_layers=True, depsgraph=dg)
        me.transform(Bi @ G @ o.matrix_world)
        for k, sl in enumerate(o.material_slots):
            if k < len(me.materials): me.materials[k] = sl.material
            else: me.materials.append(sl.material)
        t = bpy.data.objects.new(name + '_tmp', me); bpy.context.scene.collection.objects.link(t); temps.append(t)
    if len(temps) > 1:
        with bpy.context.temp_override(active_object=temps[0], object=temps[0], selected_objects=temps, selected_editable_objects=temps):
            bpy.ops.object.join()
    ob = temps[0]; ob.name = name; ob.data.name = name
    ob.parent = arm; ob.parent_type = 'BONE'; ob.parent_bone = bone
    ob.matrix_world = Bm
    MESH_OBJS.append(ob); return ob
for bone, objs in sorted(groups.items()):
    merge(bone + '_Mesh', objs, bone)
for ob, bone in NEW:
    nm = ob.name; ob.name = nm + '_src'
    merge(nm, [ob], bone)
bpy.context.view_layer.update()

# ------------------------------------------------------------------ 9. materiaux par role
ROLE_OF = {'GX55_Armure_Blanche': 'Armor', 'GX55_White': 'Armor', 'GX55_Whitywhite': 'Armor', 'Material': 'Armor', 'GX55_Armure_Charbon': 'Armor',
           'GX55_Meca_Gris': 'Frame', 'GX55_Articulation_Noir': 'Frame', 'GX55_Armure_Gris_Secondaire': 'Frame', 'GX55_Serie': 'Frame',
           'GX55_Chrome_Piston': 'Frame', 'GX55_Ailettes_Dorsales': 'Frame',
           'GX55_Orangeyellow': 'Accent', 'GX55_Accent_Orange_Tech': 'Accent', 'Blue': 'Accent',
           'GX55_Emission_Rouge': 'Visor', 'GX55_Emission_Orange': 'Glow', 'GX55_Feux_Orange': 'Glow', 'GX55_Propulseur_Interieur': 'Glow',
           'GX55_Flamme': 'Flame', 'GX55_Lame_Plasma': 'Blade'}
MAIN = {'GX55_Armure_Blanche': 'MGSUGE_Vesper_Armor', 'GX55_Meca_Gris': 'MGSUGE_Vesper_Frame', 'GX55_Orangeyellow': 'MGSUGE_Vesper_Accent',
        'GX55_Emission_Orange': 'MGSUGE_Vesper_Glow', 'GX55_Flamme': 'MGSUGE_Vesper_Flame', 'GX55_Lame_Plasma': 'MGSUGE_Vesper_Blade'}
# la visiere a son propre materiau (role Visor)
visor = bpy.data.materials['GX55_Emission_Orange'].copy(); visor.name = 'MGSUGE_Vesper_Visor'
head = next(o for o in MESH_OBJS if o.name == 'Head_Mesh')
# faces de la visiere : on les reconnait par leur materiau d'origine et leur position (avant du casque)
used = set(m for o in MESH_OBJS for m in o.data.materials)
palette = []
renamed = {}
for m in sorted(used, key=lambda m: m.name):
    if m.name not in ROLE_OF: print("ATTENTION materiau sans role :", m.name); ROLE_OF[m.name] = 'Frame'
    role = ROLE_OF[m.name]
    new = MAIN.get(m.name) or f"MGSUGE_Vesper_{role}_{m.name.replace('GX55_', '')}"
    renamed[m.name] = new
for m in list(used):
    m.name = renamed[m.name]
# visiere : reassigne les faces Glow du maillage de tete situees dans la visiere
vis_src = bpy.data.objects['GX55_Tete_Visiere']; va, vb = bbox_world(vis_src); va = G @ va; vb = G @ vb
hm = head.data; Bm = head.matrix_world
if 'MGSUGE_Vesper_Glow' in [m.name for m in hm.materials]:
    gi = [m.name for m in hm.materials].index('MGSUGE_Vesper_Glow'); hm.materials.append(visor); vi = len(hm.materials) - 1
    nvis = 0
    for p in hm.polygons:
        c = Bm @ p.center
        if p.material_index == gi and all(va[k] - 0.02 <= c[k] <= vb[k] + 0.02 for k in range(3)): p.material_index = vi; nvis += 1
    print("faces de visiere :", nvis)
ROLE_BY_NEW = {renamed[k]: ROLE_OF[k] for k in renamed}; ROLE_BY_NEW['MGSUGE_Vesper_Visor'] = 'Visor'
for o in MESH_OBJS:
    for m in o.data.materials:
        if m is None: continue
        b = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
        entry = dict(name=m.name, role=ROLE_BY_NEW.get(m.name, 'Frame'),
                     baseColor=list(b.inputs['Base Color'].default_value), metallic=b.inputs['Metallic'].default_value,
                     roughness=b.inputs['Roughness'].default_value, emissionColor=list(b.inputs['Emission Color'].default_value)[:3],
                     emissionStrength=b.inputs['Emission Strength'].default_value, alpha=b.inputs['Alpha'].default_value)
        if entry['name'] not in [p['name'] for p in palette]: palette.append(entry)
col = lambda c: {'r': c[0], 'g': c[1], 'b': c[2], 'a': c[3] if len(c) > 3 else 1.0}
uni = [dict(name=p['name'], role=p['role'], baseColor=col(p['baseColor']), metallic=p['metallic'], roughness=p['roughness'],
            emissionColor=col(p['emissionColor']), emissionStrength=p['emissionStrength'], alpha=p['alpha']) for p in palette]
json.dump({'note': 'couleurs lineaires (espace Linear Unity)', 'materials': uni}, open(os.path.join(OUT, 'MGSUGE_Vesper_Palette.json'), 'w'), indent=1)
print("materiaux :", [(p['name'], p['role']) for p in palette])

# ------------------------------------------------------------------ 10. poses (espace modele Blender) -> actions + JSON Unity
def E(x=0, y=0, z=0): return Euler((math.radians(x), math.radians(y), math.radians(z)), 'XYZ').to_matrix()
def AA(axis, deg): return Matrix.Rotation(math.radians(deg), 3, Vector(axis))
fingers = lambda s, ang: {f'{f}{k}_{s}': E(0, ang * (0.55 if k == 1 else 0.45), 0) for f in ('Index', 'Middle', 'Ring', 'Little') for k in (1, 2)}
POSES = {
 'Mech': {**{'Shoulder_R': E(-28, 9, 0), 'Elbow_R': E(-62, 0, 0), 'Hand_R': E(90, 0, 0),
             'Shoulder_L': E(-16, -10, 0), 'Elbow_L': E(-52, 0, 0), 'Hand_L': E(20, 0, 0),
             'Thumb1_R': E(0, -25, 0), 'Thumb1_L': E(0, 20, 0)},
          **fingers('R', -150), **fingers('L', 90), 'Nose': {'scale': 0.0}},
 'Ship': {'Body': E(90, 0, 0), 'Hip_L': E(0, 4, 0), 'Hip_R': E(0, -4, 0), 'Ankle_L': E(90, 0, 0), 'Ankle_R': E(90, 0, 0),
          'Shoulder_R': E(-172, 0, 0), 'Shoulder_L': E(-172, 0, 0), 'Hand_R': E(82, 0, 0), 'Hand_L': E(0, 0, 0),
          **fingers('R', -150), **fingers('L', 90),
          'Wing_L': E(-150, -55, 0), 'Wing_R': E(-150, 55, 0), 'Skirt_Front_L': E(-25, 0, 0), 'Skirt_Front_R': E(-25, 0, 0),
          'Head': {'pos': Vector((0, -0.3, -1.1)) * S, 'scale': 0.72}, 'Nose': {'scale': 1.0}},
 'Crouch': {'Body': {'pos': Vector((0, 0, -1.1))}, 'Torso': E(10, 0, 0), 'Hip_L': E(-38, -3, 0), 'Hip_R': E(-38, 3, 0),
            'Knee_L': E(72, 0, 0), 'Knee_R': E(72, 0, 0), 'Ankle_L': E(-34, 0, 0), 'Ankle_R': E(-34, 0, 0),
            'Skirt_Front_L': E(-28, 0, 0), 'Skirt_Front_R': E(-28, 0, 0), 'Nose': {'scale': 0.0}},
 'BladeRaise': {'Torso': E(0, 0, 18), 'Shoulder_L': E(-150, -15, 0), 'Elbow_L': E(-35, 0, 0), 'Hand_L': E(-20, 0, 0), **fingers('L', 90), 'Nose': {'scale': 0.0}},
 'BladeSweep': {'Torso': E(0, 0, -22), 'Shoulder_L': E(-75, 0, -55), 'Elbow_L': E(-10, 0, 0), 'Hand_L': E(-10, 0, 0), **fingers('L', 90), 'Nose': {'scale': 0.0}},
 'LidOpen': {'Lid': AA(LID_AXIS, 72)},
}
# conversion Blender -> Unity (u = (-b.x, b.z, -b.y))
Mu2b = Matrix(((-1, 0, 0), (0, 0, -1), (0, 1, 0)))
def to_unity_rot(Rb):
    Ru = Mu2b.transposed() @ Rb @ Mu2b
    q = Ru.to_quaternion(); return [q.x, q.y, q.z, q.w]
def to_unity_vec(b): return [-b.x, b.z, -b.y]
pose_list = []
for pn, pose in POSES.items():
    bones = []
    for bn, v in pose.items():
        if bn == 'Nose' and pn not in ('Mech', 'Ship'): continue   # le nez n'est pilote que par la forme
        e = {'bone': bn, 'hasRot': False, 'rot': {'x': 0, 'y': 0, 'z': 0, 'w': 1}, 'hasPos': False, 'pos': {'x': 0, 'y': 0, 'z': 0}, 'hasScale': False, 'scale': 1.0}
        if isinstance(v, dict):
            if 'pos' in v: e['hasPos'] = True; e['pos'] = dict(zip('xyz', to_unity_vec(v['pos'])))
            if 'scale' in v: e['hasScale'] = True; e['scale'] = v['scale']
            if 'rot' in v: e['hasRot'] = True; e['rot'] = dict(zip('xyzw', to_unity_rot(v['rot'])))
        else: e['hasRot'] = True; e['rot'] = dict(zip('xyzw', to_unity_rot(v)))
        bones.append(e)
    pose_list.append({'name': pn, 'bones': bones})
json.dump({'space': 'Unity, espace modele (X droite, Y haut, Z avant), decalages relatifs a la pose de liaison',
           'scale': S, 'frameClear': FRAME_CLEAR, 'poses': pose_list}, open(os.path.join(OUT, 'MGSUGE_Vesper_Poses.json'), 'w'), indent=1)

PB = arm.pose.bones
def apply_pose(pose):
    """offset R (espace modele) : rotation monde = A_parent * R * Bind ; translation via le parent pose"""
    posed = {}; acc = {}
    for b in arm.data.bones:   # ordre parent -> enfant
        rest = b.matrix_local; v = pose.get(b.name, {})
        R = v if isinstance(v, Matrix) else v.get('rot', Matrix.Identity(3)) if isinstance(v, dict) else Matrix.Identity(3)
        pos = v.get('pos', Vector()) if isinstance(v, dict) else Vector()
        sc = max(v.get('scale', 1.0), 1e-3) if isinstance(v, dict) else 1.0
        if b.parent:
            P = posed[b.parent.name]; Prest = b.parent.matrix_local
            head = P @ (Prest.inverted() @ rest.translation)
            A = acc[b.parent.name] @ R
            psc = posed_scale[b.parent.name]
        else:
            head = rest.translation.copy(); A = R; psc = 1.0
        head = head + acc[b.parent.name] @ pos if b.parent else head + pos
        rot = A @ rest.to_3x3()
        M = rot.to_4x4(); M.translation = head
        M = M @ Matrix.Scale(sc * psc, 4)
        posed[b.name] = M; acc[b.name] = A; posed_scale[b.name] = sc * psc
    for b in arm.data.bones:
        pb = PB[b.name]
        if b.parent:
            Pp = posed[b.parent.name]; rel = b.parent.matrix_local.inverted() @ b.matrix_local
            pb.matrix_basis = rel.inverted() @ Pp.inverted() @ posed[b.name]
        else:
            pb.matrix_basis = b.matrix_local.inverted() @ posed[b.name]
posed_scale = {}
def clear_pose():
    for pb in PB: pb.matrix_basis = Matrix.Identity(4)
def keyframe_all(frame):
    for pb in PB:
        pb.keyframe_insert('location', frame=frame); pb.keyframe_insert('rotation_quaternion', frame=frame); pb.keyframe_insert('scale', frame=frame)
for pb in PB: pb.rotation_mode = 'QUATERNION'
arm.animation_data_create()
for an, keys in (('Pose_Mech', [('Mech', 1)]), ('Pose_Ship', [('Ship', 1)]), ('Pose_Crouch', [('Crouch', 1)]),
                 ('Pose_BladeStrike', [('BladeRaise', 1), ('BladeRaise', 7), ('BladeSweep', 17)])):
    act = bpy.data.actions.new(an); act.use_fake_user = True
    arm.animation_data.action = act
    for pn, fr in keys:
        clear_pose(); apply_pose(POSES[pn]); keyframe_all(fr)
arm.animation_data.action = None
clear_pose()
bpy.context.view_layer.update()

# ------------------------------------------------------------------ 11. nettoyage : on ne garde que le chassis de jeu
keep = {arm.name} | {o.name for o in MESH_OBJS}
for o in list(O):
    if o.name not in keep: bpy.data.objects.remove(o, do_unlink=True)
for c in list(bpy.data.collections): bpy.data.collections.remove(c)
root = bpy.context.scene.collection
cm = bpy.data.collections.new('Mesh'); cr = bpy.data.collections.new('Rig'); cf = bpy.data.collections.new('Refs')
for c in (cm, cr, cf): root.children.link(c)
for o in MESH_OBJS:
    for c in list(o.users_collection): c.objects.unlink(o)
    cm.objects.link(o)
for c in list(arm.users_collection): c.objects.unlink(arm)
cr.objects.link(arm)
for o in MESH_OBJS:
    if o.name.startswith('Flame_') or o.name == 'Blade_Energy': o['mgsuge_hidden_at_start'] = True
# reperes d'echelle
bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0); bmesh.ops.scale(bm, vec=(0.5, 0.3, 1.8), verts=bm.verts); bmesh.ops.translate(bm, vec=(6, 0, -FRAME_CLEAR + 0.9), verts=bm.verts)
me = bpy.data.meshes.new('Ref_Humain_1m80'); bm.to_mesh(me); bm.free(); ref = bpy.data.objects.new('Ref_Humain_1m80', me); cf.objects.link(ref)
for m in list(bpy.data.materials):
    if m.users == 0 and not m.use_fake_user: bpy.data.materials.remove(m)
for me in list(bpy.data.meshes):
    if me.users == 0: bpy.data.meshes.remove(me)

tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in MESH_OBJS)
hmin = min((o.matrix_world @ v.co).z for o in MESH_OBJS for v in o.data.vertices)
hmax = max((o.matrix_world @ v.co).z for o in MESH_OBJS if not o.name.startswith(('Nose',)) for v in o.data.vertices)
print(f"triangles : {tris} ; objets maillage : {len(MESH_OBJS)} ; bas {hmin:.2f} m, haut {hmax:.2f} m (hauteur {hmax - hmin:.2f} m)")
json.dump({'scale': S, 'bladeLength': BLADE_LEN, 'triangles': tris, 'meshes': len(MESH_OBJS), 'bones': [b.name for b in arm.data.bones],
           'heightMin': hmin, 'heightMax': hmax}, open(os.path.join(OUT, 'build_report.json'), 'w'), indent=1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'MGSUGE_Vesper.blend'), compress=True)

# ------------------------------------------------------------------ 12. export FBX pour Unity
for o in bpy.context.scene.objects: o.select_set(o.name in keep)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, 'MGSUGE_Vesper_Mech.fbx'), use_selection=True,
                         object_types={'ARMATURE', 'MESH'}, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                         axis_forward='-Z', axis_up='Y', bake_space_transform=True, use_mesh_modifiers=True,
                         mesh_smooth_type='OFF', add_leaf_bones=False, primary_bone_axis='Y', secondary_bone_axis='X',
                         use_armature_deform_only=False, bake_anim=False, path_mode='STRIP', embed_textures=False)
print("FBX exporte")
