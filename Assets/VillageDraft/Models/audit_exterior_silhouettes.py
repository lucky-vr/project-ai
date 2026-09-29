"""Render material-neutral front/top silhouette masks for the 15 venue exteriors.

The common walkable floor is excluded to measure venue architecture rather
than platform area. A companion Pillow analysis computes pairwise Jaccard IoU.
"""
import bpy
import json
import os
from mathutils import Vector

BASE=os.path.dirname(os.path.abspath(__file__))
OUT=os.path.join(os.environ.get('VILLAGE_AUDIT_ROOT','/tmp/village_model_audit'),
                 'exterior_silhouettes')
os.makedirs(OUT,exist_ok=True)
manifest=json.load(open(os.path.join(BASE,'asset_manifest.json'),encoding='utf-8'))
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
bpy.ops.object.camera_add()
camera=bpy.context.object;scene.camera=camera
camera.data.type='ORTHO';camera.data.ortho_scale=20
scene.render.engine='BLENDER_WORKBENCH'
scene.render.film_transparent=True
scene.display.shading.light='FLAT'
scene.display.shading.color_type='SINGLE'
scene.display.shading.single_color=(1,1,1)
scene.render.resolution_x=384;scene.render.resolution_y=384
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.image_settings.color_mode='RGBA'

for vid,record in manifest['venues'].items():
    source=os.path.join(BASE,record['blend'])
    with bpy.data.libraries.load(source,link=False) as (src,dst):
        dst.collections=[n for n in src.collections if n.startswith('00 Exterior')]
    coll=dst.collections[0]
    meshes=[]
    for obj in coll.objects:
        if obj.type!='MESH' or 'platform' in obj.name.lower():continue
        scene.collection.objects.link(obj);meshes.append(obj)
    for view,location,target in (
        ('front',(0,22,5.2),(0,0,5.2)),
        ('top',(0,0,25),(0,0,0))):
        camera.location=location
        camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
        scene.render.filepath=os.path.join(OUT,vid+'_'+view+'.png')
        bpy.ops.render.render(write_still=True)
    for obj in meshes:bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.collections.remove(coll,do_unlink=True)
    print('AUDITED',vid)
