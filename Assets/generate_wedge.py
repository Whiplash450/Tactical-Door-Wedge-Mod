import bpy
import bmesh
import math
import os
import shutil
from mathutils import Vector, Euler

def setup_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if not bpy.data.collections:
        col = bpy.data.collections.new("TacticalWedge")
        bpy.context.scene.collection.children.link(col)

def create_orange_polymer_material():
    mat = bpy.data.materials.new(name="OrangePolymer")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    node_out = nodes.new(type='ShaderNodeOutputMaterial')
    node_out.location = (400, 0)

    node_bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
    node_bsdf.location = (100, 0)
    node_bsdf.inputs['Base Color'].default_value = (0.86, 0.055, 0.006, 1.0)
    node_bsdf.inputs['Roughness'].default_value = 0.28
    node_bsdf.inputs['Specular'].default_value = 0.55

    links.new(node_bsdf.outputs['BSDF'], node_out.inputs['Surface'])
    return mat

def create_black_rubber_material():
    mat = bpy.data.materials.new(name="BlackRubber")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    node_out = nodes.new(type='ShaderNodeOutputMaterial')
    node_out.location = (400, 0)

    node_bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
    node_bsdf.location = (100, 0)
    node_bsdf.inputs['Base Color'].default_value = (0.018, 0.018, 0.020, 1.0)
    node_bsdf.inputs['Roughness'].default_value = 0.65
    node_bsdf.inputs['Specular'].default_value = 0.25

    node_tex = nodes.new(type='ShaderNodeTexNoise')
    node_tex.location = (-300, -100)
    node_tex.inputs['Scale'].default_value = 350.0
    node_tex.inputs['Detail'].default_value = 6.0

    node_bump = nodes.new(type='ShaderNodeBump')
    node_bump.location = (-100, -100)
    node_bump.inputs['Strength'].default_value = 0.04
    node_bump.inputs['Distance'].default_value = 0.002

    links.new(node_tex.outputs['Fac'], node_bump.inputs['Height'])
    links.new(node_bump.outputs['Normal'], node_bsdf.inputs['Normal'])
    links.new(node_bsdf.outputs['BSDF'], node_out.inputs['Surface'])
    return mat

def apply_mod(obj, mod_name):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    try:
        with bpy.context.temp_override(active_object=obj, object=obj, selected_editable_objects=[obj]):
            bpy.ops.object.modifier_apply(modifier=mod_name)
    except Exception as e:
        print(f"Notice applying {mod_name}: {e}")

def build_fma_tactical_wedge(mat_orange, mat_black):
    thick = 0.048
    half_y = thick / 2.0
    
    mesh_body = bpy.data.meshes.new("OrangeCore_Mesh")
    obj_body = bpy.data.objects.new("OrangeCore", mesh_body)
    bpy.context.scene.collection.objects.link(obj_body)

    outer_poly = [
        (-0.046,  0.024),
        (-0.020,  0.040),
        ( 0.042, -0.010),
        ( 0.046, -0.034),
        ( 0.020, -0.038),
        (-0.012, -0.014),
        (-0.036,  0.002),
        (-0.046,  0.012)
    ]

    bm = bmesh.new()
    v_top = []
    v_bot = []

    for pt in outer_poly:
        v_top.append(bm.verts.new((pt[0],  half_y, pt[1])))
        v_bot.append(bm.verts.new((pt[0], -half_y, pt[1])))

    n = len(outer_poly)
    for i in range(n):
        nxt = (i + 1) % n
        bm.faces.new((v_top[i], v_top[nxt], v_bot[nxt], v_bot[i]))

    bm.faces.new(reversed(v_top))
    bm.faces.new(v_bot)

    bm.to_mesh(mesh_body)
    bm.free()

    bpy.context.view_layer.objects.active = obj_body
    obj_body.select_set(True)

    # 1. Central Hinge Hook
    bpy.ops.mesh.primitive_cylinder_add(
        radius=0.016,
        depth=thick * 2.5,
        location=(-0.006, 0.0, -0.002),
        rotation=(math.radians(90), 0, 0),
        vertices=48
    )
    cyl_hook = bpy.context.active_object
    mod_hook = obj_body.modifiers.new(name="HookHole", type='BOOLEAN')
    mod_hook.operation = 'DIFFERENCE'
    mod_hook.object = cyl_hook
    apply_mod(obj_body, mod_hook.name)
    bpy.data.objects.remove(cyl_hook, do_unlink=True)

    # 2. Hook Entrance Slot
    bpy.ops.mesh.primitive_cube_add(
        size=1.0,
        location=(-0.026, 0.0, -0.007),
        rotation=(0, math.radians(22), 0),
        scale=(0.026, thick * 2.5, 0.019)
    )
    slot_cutter = bpy.context.active_object
    mod_slot = obj_body.modifiers.new(name="ThroatSlot", type='BOOLEAN')
    mod_slot.operation = 'DIFFERENCE'
    mod_slot.object = slot_cutter
    apply_mod(obj_body, mod_slot.name)
    bpy.data.objects.remove(slot_cutter, do_unlink=True)

    # 3. Lower Handle Slot
    bpy.ops.mesh.primitive_cube_add(
        size=1.0,
        location=(0.022, 0.0, -0.026),
        rotation=(0, math.radians(-32), 0),
        scale=(0.022, thick * 2.5, 0.011)
    )
    handle_cutter = bpy.context.active_object
    mod_handle = obj_body.modifiers.new(name="HandleSlot", type='BOOLEAN')
    mod_handle.operation = 'DIFFERENCE'
    mod_handle.object = handle_cutter
    apply_mod(obj_body, mod_handle.name)
    bpy.data.objects.remove(handle_cutter, do_unlink=True)

    # 4. Radial Stiffening Ribs
    num_pockets = 6
    arc_start = math.radians(-38)
    arc_end = math.radians(108)
    arc_step = (arc_end - arc_start) / (num_pockets - 1)

    for side in [-1, 1]:
        y_pos = side * (half_y - 0.0022)
        for i in range(num_pockets):
            a = arc_start + i * arc_step
            px = -0.006 + 0.023 * math.cos(a)
            pz = -0.002 + 0.023 * math.sin(a)
            
            bpy.ops.mesh.primitive_cube_add(
                size=1.0,
                location=(px, y_pos, pz),
                rotation=(0, -a, 0),
                scale=(0.009, 0.006, 0.0042)
            )
            pocket_cutter = bpy.context.active_object
            pocket_mod = obj_body.modifiers.new(name=f"Pocket_{side}_{i}", type='BOOLEAN')
            pocket_mod.operation = 'DIFFERENCE'
            pocket_mod.object = pocket_cutter
            apply_mod(obj_body, pocket_mod.name)
            bpy.data.objects.remove(pocket_cutter, do_unlink=True)

    # 5. Black Rubber Cap 1: Top Head Cap
    bpy.ops.mesh.primitive_cube_add(
        size=1.0,
        location=(-0.036, 0.0, 0.031),
        rotation=(0, math.radians(-26), 0),
        scale=(0.027, thick * 1.05, 0.016)
    )
    cap_top = bpy.context.active_object
    cap_top.name = "BlackCap_Top"

    for g in range(3):
        gx = -0.042 + g * 0.007
        bpy.ops.mesh.primitive_cylinder_add(
            radius=0.0016,
            depth=thick * 1.5,
            location=(gx, 0.0, 0.038),
            rotation=(math.radians(90), 0, 0),
            vertices=16
        )
        groove = bpy.context.active_object
        mod_g = cap_top.modifiers.new(name=f"Groove_T_{g}", type='BOOLEAN')
        mod_g.operation = 'DIFFERENCE'
        mod_g.object = groove
        apply_mod(cap_top, mod_g.name)
        bpy.data.objects.remove(groove, do_unlink=True)

    # 6. Black Rubber Cap 2: Bottom Toe Cap
    bpy.ops.mesh.primitive_cube_add(
        size=1.0,
        location=(0.036, 0.0, -0.027),
        rotation=(0, math.radians(34), 0),
        scale=(0.029, thick * 1.05, 0.016)
    )
    cap_bot = bpy.context.active_object
    cap_bot.name = "BlackCap_Bottom"

    for g in range(4):
        gz = -0.035 + g * 0.0065
        bpy.ops.mesh.primitive_cylinder_add(
            radius=0.0015,
            depth=thick * 1.5,
            location=(0.044, 0.0, gz),
            rotation=(math.radians(90), 0, 0),
            vertices=16
        )
        groove = bpy.context.active_object
        mod_gb = cap_bot.modifiers.new(name=f"Groove_B_{g}", type='BOOLEAN')
        mod_gb.operation = 'DIFFERENCE'
        mod_gb.object = groove
        apply_mod(cap_bot, mod_gb.name)
        bpy.data.objects.remove(groove, do_unlink=True)

    # Rivet
    bpy.ops.mesh.primitive_cylinder_add(
        radius=0.0022,
        depth=0.006,
        location=(0.036, half_y * 1.02, -0.026),
        rotation=(math.radians(90), 0, 0),
        vertices=24
    )
    rivet = bpy.context.active_object
    mod_r = cap_bot.modifiers.new(name="Rivet", type='BOOLEAN')
    mod_r.operation = 'DIFFERENCE'
    mod_r.object = rivet
    apply_mod(cap_bot, mod_r.name)
    bpy.data.objects.remove(rivet, do_unlink=True)

    # 7. Black Rubber Spine Strip
    bpy.ops.mesh.primitive_cube_add(
        size=1.0,
        location=(0.015, half_y * 0.85, 0.012),
        rotation=(0, math.radians(36), 0),
        scale=(0.065, thick * 0.28, 0.004)
    )
    spine_strip = bpy.context.active_object
    spine_strip.name = "BlackSpine_Strip"

    # Materials
    obj_body.data.materials.clear()
    obj_body.data.materials.append(mat_orange)
    for p in obj_body.data.polygons:
        p.material_index = 0

    cap_top.data.materials.clear()
    cap_top.data.materials.append(mat_black)
    for p in cap_top.data.polygons:
        p.material_index = 0

    cap_bot.data.materials.clear()
    cap_bot.data.materials.append(mat_black)
    for p in cap_bot.data.polygons:
        p.material_index = 0

    spine_strip.data.materials.clear()
    spine_strip.data.materials.append(mat_black)
    for p in spine_strip.data.polygons:
        p.material_index = 0

    # Join
    bpy.ops.object.select_all(action='DESELECT')
    obj_body.select_set(True)
    cap_top.select_set(True)
    cap_bot.select_set(True)
    spine_strip.select_set(True)
    bpy.context.view_layer.objects.active = obj_body
    bpy.ops.object.join()
    obj_body.name = "Wedge_Visual"

    # Angled 45-degree isometric pose exactly matching reference photo
    obj_body.rotation_euler = Euler((math.radians(115), math.radians(20), math.radians(-75)), 'XYZ')

    mod_b = obj_body.modifiers.new(name="UnifiedBevel", type='BEVEL')
    mod_b.width = 0.0008
    mod_b.segments = 2
    mod_b.limit_method = 'ANGLE'
    mod_b.angle_limit = math.radians(35.0)

    for poly in obj_body.data.polygons:
        poly.use_smooth = True
    obj_body.data.use_auto_smooth = True
    obj_body.data.auto_smooth_angle = math.radians(40.0)

    return obj_body

def create_low_poly_collider():
    mesh = bpy.data.meshes.new("Wedge_Collider_Mesh")
    obj = bpy.data.objects.new("Wedge_Collider", mesh)
    bpy.context.scene.collection.objects.link(obj)

    bm = bmesh.new()
    lx = 0.050
    ly = 0.025
    hz = 0.040

    v0 = bm.verts.new((-lx, -ly, -hz))
    v1 = bm.verts.new(( lx, -ly, -hz))
    v2 = bm.verts.new(( lx,  ly, -hz))
    v3 = bm.verts.new((-lx,  ly, -hz))
    v4 = bm.verts.new((-lx, -ly,  hz))
    v5 = bm.verts.new((-lx,  ly,  hz))

    bm.faces.new((v0, v1, v2, v3))
    bm.faces.new((v0, v4, v5, v3))
    bm.faces.new((v1, v4, v5, v2))
    bm.faces.new((v0, v1, v4))
    bm.faces.new((v3, v5, v2))

    bm.to_mesh(mesh)
    bm.free()
    obj.hide_render = True
    return obj

def setup_studio_and_render(output_image_path):
    scene = bpy.context.scene

    scene.render.engine = 'BLENDER_EEVEE'
    scene.eevee.use_gtao = True
    scene.eevee.use_ssr = True
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.film_transparent = False

    world = bpy.data.worlds.new("StudioWorld")
    world.use_nodes = True
    bg_node = world.node_tree.nodes.get("Background")
    if bg_node:
        bg_node.inputs['Color'].default_value = (0.92, 0.93, 0.95, 1.0)
        bg_node.inputs['Strength'].default_value = 0.5
    scene.world = world

    # Ground plane
    bpy.ops.mesh.primitive_plane_add(size=3.0, location=(0, 0, -0.050))
    ground = bpy.context.active_object
    ground.name = "Ground"
    ground_mat = bpy.data.materials.new("GroundMat")
    ground_mat.use_nodes = True
    g_bsdf = ground_mat.node_tree.nodes.get("Principled BSDF")
    if g_bsdf:
        g_bsdf.inputs['Base Color'].default_value = (0.91, 0.92, 0.94, 1.0)
        g_bsdf.inputs['Roughness'].default_value = 0.55
    ground.data.materials.append(ground_mat)

    # Key light
    bpy.ops.object.light_add(type='AREA', location=(0.10, -0.25, 0.30))
    key_light = bpy.context.active_object
    key_light.data.energy = 5.0
    key_light.data.size = 0.4
    key_light.data.color = (1.0, 0.98, 0.96)
    dir_k = Vector((0.0, 0.0, 0.0)) - key_light.location
    key_light.rotation_euler = dir_k.to_track_quat('-Z', 'Y').to_euler()

    # Fill light
    bpy.ops.object.light_add(type='AREA', location=(-0.25, 0.18, 0.25))
    fill_light = bpy.context.active_object
    fill_light.data.energy = 2.8
    fill_light.data.size = 0.5
    fill_light.data.color = (0.94, 0.96, 1.0)
    dir_f = Vector((0.0, 0.0, 0.0)) - fill_light.location
    fill_light.rotation_euler = dir_f.to_track_quat('-Z', 'Y').to_euler()

    # Camera
    cam_data = bpy.data.cameras.new("StudioCamera")
    cam_data.lens = 72
    cam_obj = bpy.data.objects.new("Camera", cam_data)
    scene.collection.objects.link(cam_obj)
    scene.camera = cam_obj

    cam_obj.location = (0.01, -0.28, 0.18)
    dir_c = Vector((0.0, 0.0, -0.005)) - cam_obj.location
    cam_obj.rotation_euler = dir_c.to_track_quat('-Z', 'Y').to_euler()

    scene.render.filepath = output_image_path
    bpy.ops.render.render(write_still=True)
    print(f"[SUCCESS] Rendered preview image to: {output_image_path}")

def main():
    setup_scene()
    mat_orange = create_orange_polymer_material()
    mat_black = create_black_rubber_material()
    
    vis_obj = build_fma_tactical_wedge(mat_orange, mat_black)
    col_obj = create_low_poly_collider()

    assets_dir = r"c:\Users\Hugh\Documents\antigravity\zealous-faraday\Tactical-Door-Wedge-Mod\Assets"
    os.makedirs(assets_dir, exist_ok=True)

    render_path = os.path.join(assets_dir, "fma_wedge_preview.png")
    setup_studio_and_render(render_path)

    depsgraph = bpy.context.evaluated_depsgraph_get()
    vis_eval = vis_obj.evaluated_get(depsgraph)
    mesh_from_eval = bpy.data.meshes.new_from_object(vis_eval)
    export_vis_obj = bpy.data.objects.new("Wedge_Visual_Export", mesh_from_eval)
    bpy.context.scene.collection.objects.link(export_vis_obj)

    vis_path = os.path.join(assets_dir, "Wedge_Visual.obj")
    bpy.ops.object.select_all(action='DESELECT')
    export_vis_obj.select_set(True)
    bpy.context.view_layer.objects.active = export_vis_obj
    bpy.ops.wm.obj_export(filepath=vis_path, export_selected_objects=True)
    print(f"[SUCCESS] Exported visual mesh: {vis_path}")

    col_path = os.path.join(assets_dir, "Wedge_Collider.obj")
    bpy.ops.object.select_all(action='DESELECT')
    col_obj.select_set(True)
    bpy.context.view_layer.objects.active = col_obj
    bpy.ops.wm.obj_export(filepath=col_path, export_selected_objects=True)
    print(f"[SUCCESS] Exported collider mesh: {col_path}")

    artifact_dir = r"C:\Users\Hugh\.gemini\antigravity\brain\b4a97d0f-4c8c-4ffd-8611-309afec50012"
    if os.path.exists(render_path) and os.path.isdir(artifact_dir):
        artifact_img = os.path.join(artifact_dir, "fma_wedge_preview.png")
        shutil.copy2(render_path, artifact_img)
        print(f"[SUCCESS] Copied preview to artifact directory: {artifact_img}")

if __name__ == "__main__":
    main()
