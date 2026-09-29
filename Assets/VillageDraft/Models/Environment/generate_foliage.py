"""Create three editable AI-assisted village foliage references.

Run: blender --background --python Assets/VillageDraft/Models/Environment/generate_foliage.py
Exports FBX into the shared village model import pipeline; useFileScale must be false in Unity.
"""
import bpy
import math
import os
import random
from mathutils import Matrix, Vector

ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE=os.path.join(ROOT,'Environment','Source')
FBX=os.path.join(ROOT,'FBX','environment')
os.makedirs(SOURCE,exist_ok=True);os.makedirs(FBX,exist_ok=True)

def material(name,color,texture=None):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    bsdf=m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value=(*color,1)
    bsdf.inputs['Roughness'].default_value=.72
    if texture:
        tex=m.node_tree.nodes.new('ShaderNodeTexImage')
        tex.image=bpy.data.images.load(os.path.join(ROOT,'Textures',texture),check_existing=True)
        m.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
    return m

def uv(obj):
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
    obj.select_set(False)

def assign(obj,name,mat):
    obj.name=name;obj.data.materials.append(mat);uv(obj);return obj

def branch(name,a,b,r0,r1,mat,sides=9):
    start=Vector(a);end=Vector(b);axis=(end-start).normalized()
    helper=Vector((0,1,0)) if abs(axis.y)<.9 else Vector((1,0,0))
    u=axis.cross(helper).normalized();v=axis.cross(u).normalized()
    verts=[]
    for center,radius in ((start,r0),(end,r1)):
        verts.extend([center+radius*(u*math.cos(i*2*math.pi/sides)+v*math.sin(i*2*math.pi/sides)) for i in range(sides)])
    faces=[tuple(range(sides-1,-1,-1)),tuple(range(sides,2*sides))]
    faces.extend([(i,(i+1)%sides,(i+1)%sides+sides,i+sides) for i in range(sides)])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(obj)
    return assign(obj,name,mat)

def folded_leaf_geometry(start,end,width):
    """An eight-face, double-sided leaf with a real folded ridge and pointed tip."""
    a=Vector(start);b=Vector(end);along=(b-a).normalized()
    ref=Vector((0,0,1)) if abs(along.z)<.92 else Vector((0,1,0))
    side=along.cross(ref).normalized()
    normal=side.cross(along).normalized()
    mid=a+(b-a)*.54
    verts=[a,mid-side*width*.5,b,mid+side*width*.5,
           mid+normal*width*.13,mid-normal*width*.10]
    faces=[(0,1,4),(1,2,4),(2,3,4),(3,0,4),
           (1,0,5),(2,1,5),(3,2,5),(0,3,5)]
    return verts,faces

def assembled_foliage(name, elements, mat):
    verts=[];faces=[]
    for start,end,width in elements:
        vv,ff=folded_leaf_geometry(start,end,width)
        offset=len(verts);verts.extend(vv)
        faces.extend(tuple(offset+i for i in f) for f in ff)
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(obj)
    return assign(obj,name,mat)

def five_petal_flower(name,center,radius,petal,heart):
    """Connected five-point flower petals; each petal has a raised folded middle."""
    c=Vector(center);petals=[]
    for k in range(5):
        a=k*2*math.pi/5
        direction=Vector((math.cos(a),math.sin(a),.29))
        petals.append((c+direction*.07,c+direction*radius,radius*.68))
    assembled_foliage(name+' petals',petals,petal)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=radius*.18,location=c)
    assign(bpy.context.object,name+' raised pollen centre',heart)

def export(name):
    # Geometry uses the same FBX basis as the architecture; parent Unity model
    # builder rotates -90 X, 180 Y to put source Z vertical in the scene.
    turn=Matrix.Rotation(math.pi,4,'Z')
    for obj in bpy.data.objects:
        if obj.type=='MESH':obj.matrix_world=turn@obj.matrix_world
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SOURCE,name+'.blend'),compress=True)
    bpy.ops.object.select_all(action='DESELECT')
    objects=[o for o in bpy.data.objects if o.type=='MESH']
    for obj in objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.duplicate();bpy.ops.object.join()
    joined=bpy.context.object;joined.name=name+' optimized export mesh'
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.export_scene.fbx(filepath=os.path.join(FBX,name+'.fbx'),use_selection=True,
                             apply_unit_scale=True,axis_forward='-Z',axis_up='Y',
                             add_leaf_bones=False,path_mode='AUTO')
    print('FINISHED FOLIAGE',name,'parts',len(objects),'materials',len(joined.data.materials),
          'triangles',sum(len(poly.vertices)-2 for poly in joined.data.polygons))

def setup():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    return (material('Warm cedar timber',(.4,.25,.16),'cedar_planks.png'),
            material('Garden leaf',(.2,.5,.24)),
            material('Orchard red',(.78,.18,.3)),
            material('Warm lantern glass',(1,.84,.43)))

# A broad-crowned deciduous tree. The silhouette is built from forked timber
# and many individual pointed leaves rather than an intersecting sphere stack.
wood,leaf,flower,gold=setup(); rng=random.Random(1041)
branch('Broad root flare',(0,0,-.06),(.08,.03,.78),.39,.29,wood)
branch('Slightly swept trunk',(.08,.03,.78),(.18,.1,3.55),.29,.16,wood)
for i in range(6):
    a=i*math.pi/3+.19
    branch('Visible buttress root '+str(i),(.03,.02,.54),(.68*math.cos(a),.68*math.sin(a),.02),.12,.035,wood,7)
leaves=[]
for i in range(8):
    a=i*2*math.pi/8+.13
    root=Vector((.15,.08,2.55+i*.09))
    middle=Vector((1.02*math.cos(a),.90*math.sin(a),3.82+(i%3)*.18))
    end=Vector((1.55*math.cos(a+.1),1.43*math.sin(a+.1),4.57+(i%3)*.28))
    branch('Sweeping deciduous bough '+str(i),root,middle,.14,.075,wood,7)
    branch('Fine deciduous branch '+str(i),middle,end,.075,.025,wood,6)
    for j in range(20):
        theta=j*2.4+i*.7
        anchor=end+Vector((rng.uniform(-.33,.33),rng.uniform(-.33,.33),rng.uniform(-.31,.31)))
        tip=anchor+Vector((.37*math.cos(theta),.39*math.sin(theta),.38+rng.uniform(-.18,.29)))
        leaves.append((anchor,tip,.40+rng.uniform(-.06,.10)))
for i in range(36):
    a=i*2.39996;r=.35+.75*(i%5)/4
    anchor=(r*math.cos(a),r*math.sin(a),5.28+.2*(i%4))
    tip=(anchor[0]+.33*math.cos(a),anchor[1]+.33*math.sin(a),anchor[2]+.41)
    leaves.append((anchor,tip,.34))
assembled_foliage('Layered pointed deciduous leaf sprays',leaves,leaf)
export('deciduous_tree')

# Windswept coastal pine: exposed radial boughs and folded needle fans. There
# are deliberate gaps between tiers so the trunk reads from standing height.
wood,leaf,flower,gold=setup();rng=random.Random(264)
branch('Pine thick root bole',(0,0,-.06),(-.10,.05,2.3),.32,.21,wood)
branch('Pine tapered upper bole',(-.10,.05,2.3),(.10,.10,7.16),.21,.035,wood)
for i in range(5):
    a=i*2*math.pi/5
    branch('Splayed pine root',(-.02,0,.45),(.58*math.cos(a),.58*math.sin(a),.02),.09,.02,wood,6)
needles=[]
for tier,(z,radius) in enumerate(((1.78,2.00),(2.80,1.75),(3.82,1.54),(4.84,1.24),(5.76,.95))):
    count=7 if tier<3 else 6
    for j in range(count):
        a=j*2*math.pi/count+tier*.36
        base=Vector((.03,0,z+.28))
        mid=Vector((radius*.48*math.cos(a),radius*.48*math.sin(a),z-.11))
        end=Vector((radius*math.cos(a),radius*math.sin(a),z-.23+(j%3)*.08))
        branch('Bent pine bough root',base,mid,.105,.055,wood,6)
        branch('Bent pine bough tip',mid,end,.055,.015,wood,6)
        for k in range(5):
            t=.30+k*.15
            centre=mid.lerp(end,t)
            for side in (-1,1):
                side_angle=a+side*math.pi/2
                tip=centre+Vector((.43*math.cos(side_angle)+.27*math.cos(a),
                                   .43*math.sin(side_angle)+.27*math.sin(a),
                                   .35+rng.uniform(-.05,.13)))
                needles.append((centre,tip,.28+.03*(k%2)))
        needles.append((end,end+Vector((.31*math.cos(a),.31*math.sin(a),.48)),.31))
for i in range(12):
    a=i*2.4
    start=(.17*math.cos(a),.17*math.sin(a),6.40+(i%3)*.12)
    end=(.37*math.cos(a),.37*math.sin(a),7.0+(i%3)*.12)
    needles.append((start,end,.23))
assembled_foliage('Folded coastal pine needle sprays',needles,leaf)
export('coastal_pine')

# Flowering shrub: low forked woody habit with leaves and readable five-petal
# blooms visible from the road, not oversized faceted balls.
wood,leaf,flower,gold=setup();rng=random.Random(902)
branch('Flowering crown root',(0,0,-.06),(.04,.02,.34),.20,.13,wood,8)
leaflets=[];blooms=[]
for i in range(11):
    a=i*2*math.pi/11+.22
    end=Vector((1.06*math.cos(a),.92*math.sin(a),1.08+(i%4)*.13))
    branch('Shrub hand-shaped twig '+str(i),(.02,.01,.18),end,.075,.018,wood,6)
    for j in range(12):
        aa=a+j*2.4
        anchor=end+Vector((rng.uniform(-.23,.23),rng.uniform(-.18,.18),rng.uniform(-.23,.23)))
        tip=anchor+Vector((.27*math.cos(aa),.28*math.sin(aa),.26+rng.uniform(-.07,.13)))
        leaflets.append((anchor,tip,.28+rng.uniform(-.025,.025)))
    if i%2==0:
        blooms.append(end+Vector((.17*math.cos(a),.17*math.sin(a),.22)))
        blooms.append(end+Vector((-.17*math.cos(a),-.17*math.sin(a),.02)))
assembled_foliage('Dense pointed shrub leaves',leaflets,leaf)
for i,at in enumerate(blooms):
    five_petal_flower('Distinct flower '+str(i),at,.27,flower,wood)
export('flowering_shrub')
