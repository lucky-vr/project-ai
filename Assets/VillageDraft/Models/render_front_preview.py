"""Render street-height QA frames; compose them outside Blender with Pillow."""
import bpy
import json
import os
from mathutils import Vector

BASE=os.path.dirname(os.path.abspath(__file__))
OUT='/tmp/village_front_preview'
os.makedirs(OUT,exist_ok=True)
manifest=json.load(open(os.path.join(BASE,'asset_manifest.json')))
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene

bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.12))
ground=bpy.context.object
mat=bpy.data.materials.new('QA muted lawn');mat.diffuse_color=(.28,.39,.28,1)
ground.data.materials.append(mat)
bpy.ops.object.light_add(type='SUN',location=(-14,16,27))
sun=bpy.context.object;sun.data.energy=2.1
sun.rotation_euler=(Vector((0,0,1))-sun.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(13,21,10))
camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,2.6))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO';camera.data.ortho_scale=23;scene.camera=camera
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=640;scene.render.resolution_y=460
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.world=bpy.data.worlds.new('Street daylight')
scene.world.use_nodes=True
scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.7,.8,.9,1)
scene.view_settings.view_transform='AgX'

for vid,record in manifest['venues'].items():
    source=os.path.join(BASE,record['blend'])
    with bpy.data.libraries.load(source,link=False) as (src,dst):
        dst.collections=[x for x in src.collections if x.startswith('00 Exterior')]
    coll=dst.collections[0]
    instance=bpy.data.objects.new(vid+' street reference',None)
    scene.collection.objects.link(instance)
    instance.instance_type='COLLECTION';instance.instance_collection=coll
    scene.render.filepath=os.path.join(OUT,vid+'.png')
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(instance,do_unlink=True)
    bpy.data.collections.remove(coll,do_unlink=True)
    print('STREET FRAME',vid)
