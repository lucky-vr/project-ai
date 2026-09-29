"""Render an eye-height contact set of representative venue-specific props."""
import bpy
import json
import os
from mathutils import Vector

BASE = os.path.dirname(os.path.abspath(__file__))
OUT = '/tmp/village_prop_preview'
os.makedirs(OUT, exist_ok=True)
chosen = (
    'market_wooden_cart', 'pool_towel_cart', 'post_parcel_trolley',
    'clinic_first_aid_cart', 'library_book_return_cart',
    'garage_service_trolley', 'art_paint_trolley',
    'recycling_collection_cart', 'cafe_coffee_grinder',
    'bakery_dough_mixer', 'garage_vehicle_lift', 'harbor_cargo_crane',
)
manifest = json.load(open(os.path.join(BASE, 'asset_manifest.json'), encoding='utf-8'))
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
bpy.ops.mesh.primitive_plane_add(size=100, location=(0, 0, -.05))
ground = bpy.context.object
mat = bpy.data.materials.new('Muted inspection floor')
mat.diffuse_color = (.34, .40, .37, 1)
ground.data.materials.append(mat)
bpy.ops.object.light_add(type='SUN', location=(-5, 7, 12))
sun = bpy.context.object
sun.data.energy = 2
sun.rotation_euler = (Vector((0, 0, 0)) - sun.location).to_track_quat('-Z', 'Y').to_euler()
bpy.ops.object.camera_add(location=(4.5, 7, 4.0))
camera = bpy.context.object
camera.rotation_euler = (Vector((0, 0, 1.3)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 4.7
scene.camera = camera
scene.render.engine = 'BLENDER_EEVEE'
scene.render.resolution_x = 400
scene.render.resolution_y = 400
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Prop inspection daylight')
scene.world.use_nodes = True
scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value = (.7, .8, .88, 1)

for name in chosen:
    venue, record = next((vid, venue) for vid, venue in manifest['venues'].items()
                         if any(item['name'] == name for item in venue['assets'][1:]))
    number = next(i for i, item in enumerate(record['assets']) if item['name'] == name)
    source = os.path.join(BASE, record['blend'])
    with bpy.data.libraries.load(source, link=False) as (src, dst):
        dst.collections = [item for item in src.collections if item.startswith(f'{number:02d} ')]
    coll = dst.collections[0]
    instance = bpy.data.objects.new(name, None)
    scene.collection.objects.link(instance)
    instance.instance_type = 'COLLECTION'
    instance.instance_collection = coll
    scene.render.filepath = os.path.join(OUT, name + '.png')
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(instance, do_unlink=True)
    bpy.data.collections.remove(coll, do_unlink=True)
    print('PROP FRAME', name)
