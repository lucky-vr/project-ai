"""Render a contact sheet of the 15 authored exteriors without changing the sources."""
import bpy
import json
import os
from mathutils import Vector

BASE=os.path.dirname(os.path.abspath(__file__))
manifest=json.load(open(os.path.join(BASE,'asset_manifest.json')))
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene

for index,(vid,record) in enumerate(manifest['venues'].items()):
    source=os.path.join(BASE,record['blend'])
    with bpy.data.libraries.load(source,link=False) as (src,dst):
        dst.collections=[x for x in src.collections if x.startswith('00 Exterior')]
    coll=dst.collections[0]
    instance=bpy.data.objects.new(vid+' exterior reference',None)
    scene.collection.objects.link(instance)
    instance.instance_type='COLLECTION'
    instance.instance_collection=coll
    instance.location=((index%5-2)*16,(index//5-1)*18,0)

bpy.ops.mesh.primitive_plane_add(size=2,location=(0,0,-.06))
ground=bpy.context.object;ground.name='Contact sheet background';ground.scale=(46,36,1)
mat=bpy.data.materials.new('muted grass');mat.diffuse_color=(.23,.38,.29,1)
ground.data.materials.append(mat)

bpy.ops.object.light_add(type='SUN',location=(-25,35,45))
sun=bpy.context.object;sun.data.energy=2.0;sun.rotation_euler=(Vector((0,0,0))-sun.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(0,75,64))
camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,1.5))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=87;scene.camera=camera
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=1800;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=os.path.join(BASE,'exterior_contact_sheet.png')
scene.world=bpy.data.worlds.new('Soft daylight')
scene.world.color=(.72,.81,.91)
scene.world.use_nodes=True
scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.72,.81,.91,1)
scene.view_settings.view_transform='AgX'
bpy.ops.render.render(write_still=True)
print('SAVED',scene.render.filepath)
