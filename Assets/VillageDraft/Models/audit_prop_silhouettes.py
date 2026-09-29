"""Render neutral front/top/three-quarter masks of every exported Blender prop."""
import bpy
import json
import os
from mathutils import Vector

BASE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(os.environ.get('VILLAGE_AUDIT_ROOT', '/tmp/village_model_audit'),
                   'prop_silhouettes')
os.makedirs(OUT, exist_ok=True)
manifest = json.load(open(os.path.join(BASE, 'asset_manifest.json'), encoding='utf-8'))
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
bpy.ops.object.camera_add()
camera = bpy.context.object
scene.camera = camera
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 4.5
scene.render.engine = 'BLENDER_WORKBENCH'
scene.render.film_transparent = True
scene.display.shading.light = 'FLAT'
scene.display.shading.color_type = 'SINGLE'
scene.display.shading.single_color = (1, 1, 1)
scene.render.resolution_x = 256
scene.render.resolution_y = 256
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'

for vid, record in manifest['venues'].items():
    source = os.path.join(BASE, record['blend'])
    with bpy.data.libraries.load(source, link=False) as (src, dst):
        dst.collections = [name for name in src.collections if name[:2].isdigit() and not name.startswith('00')]
    for number, asset in enumerate(record['assets'][1:], 1):
        coll = next(item for item in dst.collections if item.name.startswith(f'{number:02d} '))
        meshes = [obj for obj in coll.objects if obj.type == 'MESH']
        for obj in meshes:
            scene.collection.objects.link(obj)
        for view, location, target in (
            ('front', (0, 10, 1.6), (0, 0, 1.6)),
            ('top', (0, 0, 10), (0, 0, 0)),
            ('three_quarter', (7, 10, 5), (0, 0, 1.5)),
        ):
            camera.location = location
            camera.rotation_euler = (Vector(target) - camera.location).to_track_quat('-Z', 'Y').to_euler()
            scene.render.filepath = os.path.join(OUT, asset['name'] + '_' + view + '.png')
            bpy.ops.render.render(write_still=True)
        for obj in meshes:
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(coll, do_unlink=True)
        print('AUDITED PROP', asset['name'])
