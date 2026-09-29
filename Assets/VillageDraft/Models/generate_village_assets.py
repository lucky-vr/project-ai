"""Generate editable, AI-assisted VR village reference assets with Blender 5.x.

Run: blender --background --python Assets/VillageDraft/Models/generate_village_assets.py
Outputs one .blend per venue and eight individual Unity-friendly FBX exports per venue.
These references are AI-assisted, so the course rubric gives them NO self-modeling credit.
"""

import bpy
import json
import math
import os
import random
import struct
import zlib
from mathutils import Matrix, Vector

BASE = os.path.dirname(os.path.abspath(__file__))
SOURCE = os.path.join(BASE, "Source")
EXPORT = os.path.join(BASE, "FBX")
TEXTURES = os.path.join(BASE, "Textures")
os.makedirs(SOURCE, exist_ok=True)
os.makedirs(EXPORT, exist_ok=True)
os.makedirs(TEXTURES, exist_ok=True)

# id, display name, accent, seven independently exported theme props.
VENUES = [
    ("market", "MARKET", (0.84, .25, .20), [("Produce stand", "display"), ("Wicker basket", "basket"), ("Checkout register", "machine"), ("Wooden cart", "cart"), ("Fruit scale", "scale"), ("Bread loaf", "bread"), ("Red apple", "apple")]),
    ("cafe", "CAFE", (.91, .43, .17), [("Espresso machine", "machine"), ("Cafe counter", "counter"), ("Cafe table", "table"), ("Bar stool", "stool"), ("Coffee grinder", "grinder"), ("Serving tray", "tray"), ("Serving cup", "cup")]),
    ("workshop", "WORKSHOP", (.22, .49, .75), [("Windmill tower", "windmill"), ("Workbench", "counter"), ("Tool rack", "rack"), ("Power console", "machine"), ("Gear assembly", "gear"), ("Toolbox", "crate"), ("Repair vise", "vise")]),
    ("greenhouse", "GREENHOUSE", (.28, .64, .37), [("Planting bench", "counter"), ("Water pump", "pump"), ("Seed cabinet", "cabinet"), ("Clay planter", "plant"), ("Garden trellis", "trellis"), ("Watering can", "can"), ("Compost barrel", "barrel")]),
    ("pool", "POOL", (.20, .68, .70), [("Lifeguard chair", "chair"), ("Ring target", "ring"), ("Diving board", "board"), ("Pool lounger", "lounger"), ("Changing screen", "screen"), ("Toss ring", "tossring"), ("Towel cart", "cart")]),
    ("bakery", "BAKERY", (.87, .55, .23), [("Bread oven", "oven"), ("Dough counter", "counter"), ("Rolling rack", "rack"), ("Dough mixer", "mixer"), ("Baguette basket", "basket"), ("Pastry display", "display"), ("Flour barrel", "barrel")]),
    ("post", "POST OFFICE", (.71, .38, .50), [("Sorting counter", "counter"), ("Mail slots", "cabinet"), ("Parcel trolley", "cart"), ("Stamp press", "press"), ("Letter scale", "scale"), ("Postal box", "mailbox"), ("Wrapping station", "table")]),
    ("clinic", "CLINIC", (.26, .65, .60), [("Examination bed", "bed"), ("Medicine cabinet", "cabinet"), ("Heart monitor", "monitor"), ("First aid cart", "cart"), ("Treatment lamp", "lamp"), ("Clinic desk", "table"), ("Medical tray", "tray")]),
    ("library", "LIBRARY", (.49, .43, .70), [("Bookcase", "cabinet"), ("Reading desk", "table"), ("Book return cart", "cart"), ("Story podium", "podium"), ("Reading lamp", "lamp"), ("Open book", "book"), ("Study bench", "bench")]),
    ("music", "MUSIC HALL", (.72, .30, .57), [("Stage piano", "piano"), ("Concert drum", "drum"), ("Microphone stand", "microphone"), ("Music stand", "stand"), ("Speaker stack", "speaker"), ("Stage spotlight", "lamp"), ("Instrument case", "case")]),
    ("garage", "GARAGE", (.51, .59, .65), [("Vehicle lift", "lift"), ("Mechanic bench", "counter"), ("Tire rack", "rack"), ("Engine block", "engine"), ("Fuel pump", "pump"), ("Service trolley", "cart"), ("Traffic cone", "cone")]),
    ("observatory", "OBSERVATORY", (.33, .43, .73), [("Astronomy telescope", "telescope"), ("Star chart desk", "table"), ("Orrery", "orrery"), ("Observation podium", "podium"), ("Lens cabinet", "cabinet"), ("Moon globe", "globe"), ("Tripod beacon", "lamp")]),
    ("harbor", "HARBOR", (.19, .55, .74), [("Fishing boat", "boat"), ("Mooring bollard", "bollard"), ("Cargo crane", "crane"), ("Fish crate", "crate"), ("Life buoy", "ring"), ("Harbor barrel", "barrel"), ("Dock lantern", "lamp")]),
    ("art", "ART STUDIO", (.74, .40, .42), [("Painting easel", "easel"), ("Sculpture plinth", "podium"), ("Paint trolley", "cart"), ("Pottery wheel", "wheel"), ("Art canvas", "canvas"), ("Brush rack", "rack"), ("Palette table", "table")]),
    ("recycling", "RECYCLE DEPOT", (.39, .64, .46), [("Sorting bins", "bins"), ("Bottle crusher", "press"), ("Conveyor", "conveyor"), ("Collection cart", "cart"), ("Glass crate", "crate"), ("Compost tumbler", "tumbler"), ("Recycle scale", "scale")]),
]

MATS = {}

def material(name, color, metallic=0, rough=.55, texture_path=None):
    if name in MATS:
        return MATS[name]
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = rough
    if texture_path:
        image = bpy.data.images.load(texture_path, check_existing=True)
        image.colorspace_settings.name = 'sRGB'
        tex = m.node_tree.nodes.new('ShaderNodeTexImage')
        tex.image = image
        tex.label = 'Authored low-resolution tileable colour map'
        m.node_tree.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
    MATS[name] = m
    return m

def mat(obj, m):
    obj.data.materials.append(m)
    return obj

def mesh_uv(obj):
    if obj.type != 'MESH' or not obj.data.polygons:
        return
    # Every editable mesh gets UV coordinates in source and exported FBX.
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(island_margin=.02)
    bpy.ops.object.mode_set(mode='OBJECT')
    obj.select_set(False)

def link_collection(name):
    c = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(c)
    return c

def put(obj, coll):
    for c in tuple(obj.users_collection): c.objects.unlink(obj)
    coll.objects.link(obj)
    return obj

def box(c, name, xyz, dims, m, bevel=.035):
    bpy.ops.mesh.primitive_cube_add(size=1, location=xyz)
    o = bpy.context.object
    o.name = name
    o.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new('soft modeled edges', 'BEVEL')
        mod.width = min(bevel, min(dims) * .22)
        mod.segments = 2
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
        o.modifiers.new('weighted corner normals', 'WEIGHTED_NORMAL')
    mat(o, m); put(o, c); mesh_uv(o)
    return o

def cylinder(c, name, xyz, radius, depth, m, vertices=16, bevel=.018, rot=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=xyz)
    o = bpy.context.object; o.name = name
    if rot: o.rotation_euler = rot
    if bevel:
        mod = o.modifiers.new('edge rounds', 'BEVEL'); mod.width = bevel; mod.segments = 2
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
        o.modifiers.new('weighted normals', 'WEIGHTED_NORMAL')
    mat(o,m); put(o,c); mesh_uv(o)
    return o

def torus(c, name, xyz, major, minor, m, rot=None):
    bpy.ops.mesh.primitive_torus_add(major_segments=24, minor_segments=8,
        location=xyz, major_radius=major, minor_radius=minor)
    o=bpy.context.object; o.name=name
    if rot: o.rotation_euler=rot
    mat(o,m); put(o,c); mesh_uv(o)
    return o

def ball(c,name,xyz,radius,m,segments=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=8,radius=radius,location=xyz)
    o=bpy.context.object;o.name=name
    mat(o,m);put(o,c);mesh_uv(o);return o

def prism(c,name,verts,faces,m):
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(verts,[],faces); mesh.update()
    o=bpy.data.objects.new(name,mesh);c.objects.link(o);mat(o,m);mesh_uv(o);return o

def beam(c,name,a,b,width,m):
    mid=(Vector(a)+Vector(b))/2
    o=box(c,name,mid,(width,width,(Vector(b)-Vector(a)).length),m,width*.15)
    o.rotation_euler=(Vector(b)-Vector(a)).to_track_quat('Z','Y').to_euler()
    return o

def text3d(c,name,content,xyz,size,m):
    cu=bpy.data.curves.new(name,'FONT');cu.body=content;cu.align_x='CENTER';cu.align_y='CENTER';cu.size=size;cu.extrude=.007;cu.bevel_depth=.002
    o=bpy.data.objects.new(name,cu);c.objects.link(o);o.location=xyz;o.rotation_euler=(math.pi/2,0,0)
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.convert(target='MESH');o=bpy.context.object;mesh_uv(o);mat(o,m)
    return o

def palette(accent, venue_id):
    return {
        'wall':material('Limestone plaster',(.87,.82,.69),texture_path=os.path.join(TEXTURES,'limestone_blocks.png')),
        'trim':material('Chalk painted timber',(.96,.92,.80),texture_path=os.path.join(TEXTURES,'chalk_grain.png')),
        'wood':material('Warm cedar timber',(.34,.21,.13),texture_path=os.path.join(TEXTURES,'cedar_planks.png')),
        'dark':material('Blue charcoal metal',(.10,.16,.20),.35,texture_path=os.path.join(TEXTURES,'charcoal_ribbed.png')),
        'metal':material('Brushed brass',(.75,.57,.28),.65,.29,texture_path=os.path.join(TEXTURES,'brushed_brass.png')),
        'glass':material('Pale aqua glass',(.49,.78,.82),.15,.13,texture_path=os.path.join(TEXTURES,'aqua_glazing.png')),
        'accent':material('Venue accent',accent,.06,.45,texture_path=os.path.join(TEXTURES,venue_id+'_finish.png')),
        'light':material('Warm lantern glass',(1,.84,.43),.08,.18),
        'floor':material('Warm sandstone paving',(.66,.57,.43),texture_path=os.path.join(TEXTURES,'sandstone_pavers.png')),
        'leaf':material('Garden leaf',(.21,.48,.27)),
        'food':material('Baked crust',(.78,.44,.16)),
        'apple':material('Orchard red',(.73,.13,.12),.02,.33),
    }

def write_rgba_png(path, size, pixel):
    """Small deterministic tileable maps; no external Python imaging package."""
    def chunk(tag, payload):
        return struct.pack('!I',len(payload))+tag+payload+struct.pack('!I',zlib.crc32(tag+payload)&0xffffffff)
    rows=[]
    for y in range(size):
        row=bytearray([0])
        for x in range(size):row.extend(pixel(x,y,size))
        rows.append(bytes(row))
    raw=b''.join(rows)
    png=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('!2I5B',size,size,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,8))+chunk(b'IEND',b'')
    with open(path,'wb') as f:f.write(png)

def noise(x,y,seed=0):
    n=(x*374761393+y*668265263+seed*2654435761)&0xffffffff
    n=((n^(n>>13))*1274126177)&0xffffffff
    return ((n^(n>>16))&255)/255

def textile_color(x,y,size,kind,base):
    # Colour variation remains visible under URP Lit without expensive normal maps.
    q=noise(x,y,17); dark=0.0; bright=0.0
    if kind=='cedar_planks':
        board=(y//42)%2
        dark=.39 if y%42<3 or (x+board*49)%164<3 else 0
        grain=.09*math.sin(x*.18+math.sin(y*.28)*2)+.06*math.sin(x*.55+y*.045)
        v=1+grain+(q-.5)*.12-dark
    elif kind=='sandstone_pavers':
        offset=(y//48%2)*36
        dark=.34 if y%48<3 or (x+offset)%72<3 else 0
        v=1+(q-.5)*.20+.07*math.sin((x//72)*5+(y//48)*11)-dark
    elif kind=='limestone_blocks':
        offset=(y//55%2)*61
        dark=.20 if y%55<3 or (x+offset)%122<3 else 0
        v=1+(q-.5)*.14+.04*math.sin(x*.05+y*.02)-dark
    elif kind=='charcoal_ribbed':
        dark=.18 if x%36<3 else 0
        bright=.10 if x%36 in (5,6,7) else 0
        v=1+(q-.5)*.06-dark+bright
    elif kind=='brushed_brass':
        v=1+.08*math.sin(y*.9)+(q-.5)*.13
    elif kind=='aqua_glazing':
        bright=.22 if (x-y+size)%151<5 else 0
        dark=.08 if x%127<3 or y%127<3 else 0
        v=1+(q-.5)*.04+bright-dark
    elif kind=='chalk_grain':
        v=1+.04*math.sin(x*.32+y*.06)+(q-.5)*.08
    else:  # coloured architectural finish, small staggered roof/timber seams
        seam=(y%48<3 or (x+(y//48%2)*57)%114<3)
        v=1+(q-.5)*.12+.065*math.sin(x*.05+y*.09)-(.25 if seam else 0)
    return bytes([max(0,min(255,round(c*v*255))) for c in base]+[255])

def ensure_textures():
    names={
        'cedar_planks':(.40,.25,.16),'sandstone_pavers':(.70,.61,.49),
        'limestone_blocks':(.82,.78,.68),'charcoal_ribbed':(.16,.22,.26),
        'brushed_brass':(.75,.58,.32),'aqua_glazing':(.52,.79,.82),
        'chalk_grain':(.94,.91,.82),
    }
    for name,col in names.items():
        write_rgba_png(os.path.join(TEXTURES,name+'.png'),512,
                       lambda x,y,s,n=name,b=col:textile_color(x,y,s,n,b))
    for vid,_,col,_ in VENUES:
        write_rgba_png(os.path.join(TEXTURES,vid+'_finish.png'),256,
                       lambda x,y,s,b=col:textile_color(x,y,s,'accent',b))

def arch(c, name, x, y, bottom, radius, thickness, depth, material):
    """Connected faceted masonry arch, with real side faces and a walk-through void."""
    steps=12; outer=[]; inner=[]
    for i in range(steps+1):
        a=math.pi*i/steps
        outer.append((x+(radius+thickness)*math.cos(a),bottom+(radius+thickness)*math.sin(a)))
        inner.append((x+radius*math.cos(a),bottom+radius*math.sin(a)))
    verts=[(px, yy, z) for yy in (y-depth/2,y+depth/2) for ring in (outer,inner) for px,z in ring]
    n=steps+1; faces=[]
    for i in range(steps):
        faces.extend([(i,i+1,n+i+1,n+i),(2*n+i,3*n+i,3*n+i+1,2*n+i+1),
                      (i,2*n+i,2*n+i+1,i+1),(n+i,n+i+1,3*n+i+1,3*n+i)])
    faces.extend([(0,n,3*n,2*n),(steps,2*n+steps,3*n+steps,n+steps)])
    return prism(c,name,verts,faces,material)

def canopy(c,name,width,depth,z,front_z,material):
    """Solid mono-pitch canopy with a visible top and underside."""
    x=width/2; y=depth/2; t=.16
    verts=[(-x,-y,front_z),(x,-y,front_z),(-x,y,z),(x,y,z),
           (-x,-y,front_z+t),(x,-y,front_z+t),(-x,y,z+t),(x,y,z+t)]
    return prism(c,name,verts,[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],material)

def roof_gable(c,name,width,depth,eave,peak,material):
    x=width/2; y=depth/2
    verts=[(-x,-y,eave),(x,-y,eave),(0,-y,peak),(-x,y,eave),(x,y,eave),(0,y,peak)]
    return prism(c,name,verts,[(0,1,2),(3,5,4),(0,3,4,1),(0,2,5,3),(1,4,5,2)],material)

def roof_hip(c,name,width,depth,eave,peak,ridge,material):
    """Five planar roof faces with deliberate thickness at the supported eave."""
    x=width/2;y=depth/2;r=ridge/2
    vertices=[(-x,-y,eave),(x,-y,eave),(x,y,eave),(-x,y,eave),
              (0,-r,peak),(0,r,peak),(-x,-y,eave-.14),(x,-y,eave-.14),
              (x,y,eave-.14),(-x,y,eave-.14)]
    faces=[(0,1,4),(1,2,5,4),(2,3,5),(3,0,4,5),(9,8,7,6),
           (0,6,7,1),(1,7,8,2),(2,8,9,3),(3,9,6,0)]
    return prism(c,name,vertices,faces,material)

def barrel_roof(c,name,width,depth,eave,rise,material,segments=12):
    """A continuous, thick polygonal barrel vault with joined roof panels."""
    radius=width/2; verts=[]
    for front_y in (-depth/2,depth/2):
        for inset in (0,.13):
            for i in range(segments+1):
                angle=math.pi*i/segments
                verts.append((radius*math.cos(angle),front_y,
                              eave+rise*math.sin(angle)-inset))
    n=segments+1;faces=[]
    for i in range(segments):
        faces.extend([(i,2*n+i,2*n+i+1,i+1),
                      (n+i,n+i+1,3*n+i+1,3*n+i),
                      (i,n+i,n+i+1,i+1),
                      (2*n+i,2*n+i+1,3*n+i+1,3*n+i)])
    faces.extend([(0,2*n,n*3,n),(segments,n+segments,3*n+segments,2*n+segments)])
    return prism(c,name,verts,faces,material)

def butterfly_roof(c,name,side,width,depth,low,high,material):
    """One solid wing of a split, upward-folded pavilion roof."""
    a=0 if side>0 else -width/2;b=width/2 if side>0 else 0;y=depth/2
    za=low if side>0 else high;zb=high if side>0 else low;t=.16
    verts=[(a,-y,za),(b,-y,zb),(a,y,za),(b,y,zb),
           (a,-y,za-t),(b,-y,zb-t),(a,y,za-t),(b,y,zb-t)]
    faces=[(0,1,3,2),(7,5,4,6),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)]
    return prism(c,name,verts,faces,material)

def shaped_floor(c,name,outline,material):
    """An individually profiled but walkable platform, centred at the venue origin."""
    n=len(outline)
    verts=[(x,y,z) for z in (-.10,.10) for x,y in outline]
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]
    faces.extend((i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n))
    return prism(c,name,verts,faces,material)

def dome(c,name,xyz,radius,material):
    cx,cy,base=xyz; rings=5; sides=20; verts=[]
    for k in range(rings):
        a=math.pi*k/(2*rings); rr=radius*math.cos(a)
        verts.extend([(cx+rr*math.cos(i*2*math.pi/sides),cy+rr*math.sin(i*2*math.pi/sides),base+radius*math.sin(a)) for i in range(sides)])
    verts.append((cx,cy,base+radius))
    faces=[(k*sides+i,k*sides+(i+1)%sides,(k+1)*sides+(i+1)%sides,(k+1)*sides+i)
           for k in range(rings-1) for i in range(sides)]
    faces.extend([((rings-1)*sides+i,(rings-1)*sides+(i+1)%sides,rings*sides) for i in range(sides)])
    return prism(c,name,verts,faces,material)

def sign(c,label,p,x=0,y=-5.3,z=3.8,width=5.2):
    box(c,label+' sculpted sign backing',(x,y,z),(width,.24,.67),p['dark'],.065)
    text3d(c,label+' raised lettering',label,(x,y-.15,z),.39,p['trim'])

def pier(c,name,x,y,height,p,width=.30):
    box(c,name,(x,y,(height+.1)/2),(width,width,height-.1),p,.045)

def building(c, vid, label, p, index):
    """Fifteen venue-specific architectures. The clear floor is an XR play space."""
    A,W,D,M,G,T,F,L=p['accent'],p['wood'],p['dark'],p['metal'],p['glass'],p['trim'],p['floor'],p['light']
    footprint={'pool':(16,12),'music':(16,12),'harbor':(16,12),'art':(15,11),
               'market':(14,11),'cafe':(14,11),'recycling':(15,11),
               'greenhouse':(12,12),'observatory':(14,12)}.get(vid,(12,10))
    w,d=footprint
    if vid in ('cafe','library','observatory'):
        n=12 if vid=='observatory' else 10
        outline=[(w*.5*math.cos(i*2*math.pi/n),d*.5*math.sin(i*2*math.pi/n)) for i in range(n)]
        shaped_floor(c,vid+' faceted gathering platform',outline,F)
    elif vid in ('market','music','art'):
        inset=1.25 if vid!='music' else 1.7
        shaped_floor(c,vid+' chamfered festival platform',
                     [(-w/2+inset,-d/2),(w/2-inset,-d/2),(w/2,-d/2+inset),
                      (w/2,d/2-inset),(w/2-inset,d/2),(-w/2+inset,d/2),
                      (-w/2,d/2-inset),(-w/2,-d/2+inset)],F)
    else:
        box(c,vid+' paving platform',(0,0,0),(*footprint,.20),F,.07)

    if vid=='market':
        # Open timber bazaar: a floating four-sided market roof and striped stalls.
        for x in (-6.45,6.45):
            for y in (-4.9,4.9): pier(c,'Market timber colonnade',x,y,4.15,W,.32)
        for y in (-5.05,5.05):box(c,'Continuous bazaar beam',(0,y,4.05),(13.2,.28,.31),W,.04)
        for x in (-6.55,6.55):box(c,'Bazaar side beam',(x,0,4.05),(.24,10.1,.31),W,.04)
        roof_hip(c,'Four-sided festival marquee',14,11,4.23,7.15,.55,A)
        for x in (-6.95,6.95):
            for y in (-5.45,5.45):
                beam(c,'Marquee radial seam',(x,y,4.25),(0,.27 if y>0 else -.27,7.18),.075,T)
        beam(c,'Marquee sculpted ridge',(0,-.27,7.2),(0,.27,7.2),.12,M)
        for x in (-4.7,4.7):
            box(c,'Produce display canopy',(x,-5.6,2.85),(3.8,1.4,.13),T,.025)
            box(c,'Market outer stall counter',(x,-5.5,1.03),(3.3,.6,.19),W,.04)
            for j in (-.95,0,.95):
                box(c,'Visible stacked market produce crate',(x+j,-5.55,1.30),(.78,.43,.34),A,.035)
        for i in range(11):
            x=-5.8+i*1.16
            prism(c,'Alternating hanging bazaar pennant',[(x,-5.65,3.65),(x+.88,-5.65,3.65),(x+.44,-5.65,3.03)],
                  [(0,1,2)],T if i%2 else A)
        for x in (-6.47,6.47):
            box(c,'Carved bazaar entrance column band',(x,-4.93,2.28),(.43,.43,.25),M,.025)
        for x in (-3.4,0,3.4):
            cylinder(c,'Hanging bazaar festival lantern',(x,-5.19,3.48),.22,.42,L,vertices=10)
            box(c,'Lantern stamped copper crown',(x,-5.19,3.73),(.37,.37,.08),M,.012)
        sign(c,label,p,0,-5.75,4.03,4.3)

    elif vid=='cafe':
        # Round terrace gazebo at the street, with a smaller enclosed service bar.
        box(c,'Café compact rear serving kiosk',(0,4.75,1.57),(6.6,.3,2.94),W,.055)
        for x in (-5.5,5.5):
            box(c,'Café low terrace parapet',(x,-.5,.66),(.26,9.5,1.1),T,.05)
        for x in (-5.25,5.25):
            pier(c,'Café front terrace column',x,-4.4,4.1,M,.16)
            pier(c,'Café rear canopy column',x,4.3,4.55,M,.16)
            beam(c,'Café long terrace string-light bearer',
                 (x,-4.4,4.1),(x,4.3,4.55),.085,T)
        terrace_roof=canopy(c,'Cantilevered café service roof',12.2,5.2,4.55,4.25,T)
        terrace_roof.location.y=2.35
        for i,x in enumerate((-3.7,0,3.7)):
            cylinder(c,'Terrace parasol mast',(x,-3.1,2.07),.08,3.9,W,vertices=12)
            dome(c,'Sculpted parasol canopy',(x,-3.1,3.75),1.55,A if i!=1 else T)
            torus(c,'Café parasol trim',(x,-3.1,3.75),1.54,.055,D)
        box(c,'Open serving bar',(0,3.83,1.07),(6.5,.7,.25),W,.055)
        sign(c,label,p,0,4.5,3.85,3.8)

    elif vid=='workshop':
        # Industrial bay: asymmetric sawtooth roof, exhausts, and a windmill.
        box(c,'Factory rear masonry machine core',(0,4.8,1.7),(8.6,.3,3.2),D,.04)
        for x in (-5.65,5.65):
            box(c,'Factory side knee wall',(x,0,.98),(.28,9.5,1.75),W,.05)
            for y in (-4.5,0,4.5):pier(c,'Factory steel column',x,y,4.7,M,.19)
        # Two separated industrial roof lanes leave a four metre sky strip.
        # Their staggered front edges avoid the former generic full-box outline.
        for x,y,depth in ((-3.95,.3,9.1),(3.95,1.55,6.65)):
            roof=roof_gable(c,'Separated workshop sawtooth lane',4.35,depth,4.72,6.35,A)
            roof.location=(x,y,0)
            for rib_y in (y-depth*.27,y+depth*.25):
                beam(c,'Sawtooth clerestory rib',(x,rib_y,6.27),(x,rib_y,6.65),.07,M)
        for x in (-4.1,4.1):
            cylinder(c,'Industrial exhaust stack',(x,3.1,5.9),.27,2.6,D,vertices=12)
            cylinder(c,'Stack collar',(x,3.1,7.27),.38,.12,M,vertices=12)
        # Landmark mill is outside the work floor, placed against the rear flank.
        prism(c,'Tapered mill tower',[(-7.5,3,.1),(-6.1,3,.1),(-6.1,4.4,.1),(-7.5,4.4,.1),
             (-7.2,3.3,8.1),(-6.4,3.3,8.1),(-6.4,4.1,8.1),(-7.2,4.1,8.1)],
             [(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],W)
        for a in (0,math.pi/2,math.pi,3*math.pi/2):
            beam(c,'Four-blade mill rotor',(-6.8,2.9,7.2),(-6.8+1.65*math.cos(a),2.9,7.2+1.65*math.sin(a)),.14,T)
        sign(c,label,p,0,-5.15,4.2,5.2)

    elif vid=='greenhouse':
        # Translucent glasshouse with visible timber ribs, vent ridge and vines.
        for x in (-5.85,5.85):
            for y in (-4.8,-2.4,0,2.4,4.8):pier(c,'Greenhouse steel glazing mullion',x,y,4.2,M,.12)
            box(c,'Full-length greenhouse glass',(x,0,2.12),(.08,9.3,3.92),G,.005)
        for y in (-4.8,-2.4,0,2.4,4.8):
            for i in range(12):
                a=math.pi*i/12;b=math.pi*(i+1)/12
                beam(c,'Curved glasshouse roof rib',
                     (6.05*math.cos(a),y,4.18+2.25*math.sin(a)),
                     (6.05*math.cos(b),y,4.18+2.25*math.sin(b)),.085,M)
        barrel_roof(c,'Barrel-vaulted glasshouse',12.1,10.0,4.18,2.25,G)
        arch(c,'Rounded front glasshouse entrance',0,-5.03,2.2,2.0,.18,.24,M)
        for x in (-4.2,4.2):
            box(c,'Raised planter box',(x,-4.1,.54),(2.3,.62,.84),W,.06)
            for dx in (-.6,0,.6):ball(c,'Leafy climbing canopy',(x+dx,-4.1,1.14),.34,p['leaf'])
        sign(c,label,p,0,-5.18,3.65,4.9)

    elif vid=='pool':
        # An entirely open-air recreation court: pergola only at rear edge.
        for x in (-7.75,7.75):box(c,'Pool low privacy wall',(x,0,.61),(.26,11.6,1.04),T,.05)
        box(c,'Pool rear changing wall',(0,5.75,1.52),(15.3,.28,2.85),T,.05)
        for x in (-7.5,-5.0,5.0,7.5):pier(c,'Pool deck shade post',x,4.8,3.8,W,.19)
        for y in (4.75,5.7):box(c,'Rear pergola beam',(0,y,3.85),(15.2,.18,.2),W,.02)
        for x in (-7.2,-5.4,-3.6,-1.8,0,1.8,3.6,5.4,7.2):
            box(c,'Open overhead shade fin',(x,5.15,4.02),(.12,1.8,.15),A,.015)
        for x in (-6.6,6.6):
            cylinder(c,'Curved lifeguard landmark mast',(x,-4.4,2.25),.10,4.3,D,vertices=12)
            torus(c,'Life ring wayfinding',(x,-4.48,3.65),.45,.11,A,rot=(math.pi/2,0,0))
        for side in (-1,1):
            prism(c,'Corner triangular carnival shade sail',
                  [(side*7.5,4.8,3.9),(side*4.3,4.8,3.85),(side*6.6,-4.4,4.4)],
                  [(0,1,2),(2,1,0)],A)
        sign(c,label,p,0,5.52,2.55,3.7)

    elif vid=='bakery':
        # Brick bakehouse with barrel-arched ovens and a tall copper chimney.
        box(c,'Brick bakehouse rear oven block',(0,4.82,1.65),(8.4,.3,3.1),W,.07)
        for x in (-5.8,5.8):box(c,'Bakehouse side brickwork',(x,0,2.17),(.3,9.55,4.15),W,.05)
        for x in (-4.2,4.2):pier(c,'Bakehouse corner pilaster',x,-4.8,4.45,T,.45)
        for x in (-3.15,3.15):
            dome(c,'Paired bread-kiln vaulted roof',(x,1.4,4.24),2.98,A)
            torus(c,'Kiln vault base course',(x,1.4,4.24),3.0,.11,D)
        front_awning=canopy(c,'Bakery striped street awning',12.35,3.15,4.44,4.18,T)
        front_awning.location.y=-3.53
        for x in (-5.1,-3.1,-1.1,1.1,3.1,5.1):
            box(c,'Warm pastry canopy band',(x,-5.11,4.24),(.85,.12,.18),A,.02)
        for x in (-3.4,-1.15,1.15,3.4):
            arch(c,'Barrel-arched oven frontage',x,-4.95,2.05,.68,.20,.45,T)
            box(c,'Oven jamb',(x-.78,-4.95,1.1),(.16,.45,1.9),T,.025)
            box(c,'Oven jamb',(x+.78,-4.95,1.1),(.16,.45,1.9),T,.025)
        cylinder(c,'Towering brick bakehouse chimney',(4.3,2.4,6.65),.49,4.7,W,vertices=16)
        cylinder(c,'Copper chimney crown',(4.3,2.4,9.08),.62,.18,M,vertices=16)
        sign(c,label,p,0,-5.28,4.35,4.3)

    elif vid=='post':
        # Civic station with a clock tower, flag, and low pitched mail canopy.
        box(c,'Postal rear sorting counter wall',(-1.5,4.78,1.6),(7.4,.3,3.0),T,.06)
        for x in (-5.65,3.65):
            pier(c,'Postal front portico column',x,-4.7,4.65,D,.28)
            pier(c,'Postal rear portico column',x,-1.9,4.78,D,.28)
        box(c,'Civic postal cornice',(-1,-4.8,4.47),(10.0,.55,.52),A,.06)
        porch=canopy(c,'Shallow front postal portico',10.0,3.15,4.75,4.60,A)
        porch.location=(-1,-3.48,0)
        sorting=canopy(c,'Offset postal sorting wing',5.35,6.75,5.16,4.73,A)
        sorting.location=(2.72,1.5,0)
        box(c,'Clock tower plinth',(3.9,2.9,6.3),(2.8,2.5,3.15),W,.06)
        clock_cap=roof_gable(c,'Clock tower cap',3.25,2.9,7.9,9.4,D)
        clock_cap.location=(3.9,2.9,0)
        cylinder(c,'Postal clock dial',(3.9,1.56,6.72),.72,.09,T,vertices=24,rot=(math.pi/2,0,0))
        for a in (0,math.pi/2,math.pi,3*math.pi/2):
            ball(c,'Clock hour marker',(3.9+.55*math.cos(a),1.49,6.72+.55*math.sin(a)),.06,D)
        beam(c,'Postal flagpole',(-4.4,-4.5,4.9),(-4.4,-4.5,8.1),.065,M)
        prism(c,'Post pennant',[(-4.4,-4.5,7.94),(-2.6,-4.5,7.62),(-4.4,-4.5,7.33)],[(0,1,2)],A)
        sign(c,label,p,-1.5,-5.15,4.05,5.2)

    elif vid=='clinic':
        # Light open pavilion: long roof, cross beacon, and accessible front.
        box(c,'Clinic central privacy screen',(0,4.82,1.45),(7.3,.22,2.7),T,.05)
        for x in (-5.6,5.6):
            box(c,'Clinic white side knee wall',(x,0,.87),(.24,9.5,1.55),T,.05)
            for y in (-1.55,4.45):pier(c,'Clinic pavilion pier',x,y,5.95,G,.22)
        for side in (-1,1):
            roof=butterfly_roof(c,'Rear-set clinic butterfly wing',side,12.4,6.85,4.42,6.06,T)
            roof.location.y=1.59
        box(c,'Clinic central roof valley',(0,1.59,4.36),(.18,6.85,.15),D,.02)
        for x in (-5.7,-2.8,2.8,5.7):
            beam(c,'Clinic butterfly roof seam',(x,-1.84,4.42+1.64*abs(x)/6.2),
                 (x,5.01,4.42+1.64*abs(x)/6.2),.045,A)
        pier(c,'Freestanding clinic beacon support',0,-5.07,4.42,G,.17)
        box(c,'Elevated clinic cross pylon',(0,-5.07,5.05),(1.6,.35,1.58),A,.12)
        box(c,'White medical cross vertical',(0,-5.29,5.05),(.28,.10,1.12),T,.02)
        box(c,'White medical cross horizontal',(0,-5.3,5.05),(1.07,.10,.28),T,.02)
        for x in (-5.52,5.52):
            box(c,'Clinic glazed side window band',(x,-.4,2.45),(.09,5.9,.96),G,.01)
            for y in (-3,-.5,2):box(c,'Clinic white window mullion',(x,-.4+y,2.45),(.12,.08,1.1),T,.014)
        for x in (-4.8,4.8):
            box(c,'Clinic arrival beacon plinth',(x,-4.35,.88),(.55,.62,1.55),G,.04)
            box(c,'Clinic wayfinding cross upright',(x,-4.69,1.08),(.10,.025,.58),A,.005)
            box(c,'Clinic wayfinding cross arm',(x,-4.70,1.08),(.43,.025,.10),A,.005)
        for x in (-4.4,4.4):
            box(c,'Patient arrival wayfinding line',(x,-4.93,.16),(.16,.7,.025),A,.006)
        sign(c,label,p,0,-5.32,3.75,4.2)

    elif vid=='library':
        # Tall reading tower, real arch arcade, buttresses and illuminated panels.
        box(c,'Library central archive wall',(-1.55,4.8,2.45),(7.8,.36,4.7),W,.08)
        for x in (-5.8,5.8):
            box(c,'Library side archive wall',(x,0,2.45),(.36,9.5,4.7),W,.05)
            for y in (-3.4,0,3.4):box(c,'Stone flying buttress base',(x,y,1.15),(.58,.62,2.1),T,.06)
        # A U-shaped covered reading gallery surrounds an open central court.
        for x in (-4.72,4.72):
            side_roof=canopy(c,'Library side gallery roof',2.85,9.65,5.42,4.94,D)
            side_roof.location.x=x
        rear_roof=canopy(c,'Library rear archive roof',11.35,2.85,5.42,4.94,D)
        rear_roof.location.y=3.76
        for x in (-4.72,4.72):
            for y in (-4.25,0,4.25):pier(c,'Stone gallery arcade pier',x,y,4.95,T,.23)
        for x in (-4,0,4):
            arch(c,'Library three-arch arcade',x,-5.2,2.05,.86,.23,.42,T)
            for side in (-1,1):box(c,'Arcade stone jamb',(x+side*1.0,-5.2,1.1),(.19,.42,1.9),T,.025)
        box(c,'Asymmetric library tower',(4.0,3.0,6.6),(2.8,2.7,3.6),W,.06)
        tower_cap=roof_gable(c,'Library tower pointed cap',3.15,3.05,8.38,10.05,D)
        tower_cap.location=(4.0,3.0,0)
        for z in (5.9,7.2):box(c,'Tower illuminated slit',(4.0,1.56,z),(.34,.05,.82),G,.012)
        sign(c,label,p,0,-5.48,4.5,4.7)

    elif vid=='music':
        # Fully open amphitheatre: raised rear stage, curved seating, fan shell.
        box(c,'Outdoor concert stage',(0,3.8,.52),(11.8,3.9,.84),D,.06)
        for row,y in enumerate((-4.7,-3.45,-2.2)):
            for x in (-5.4,-3.0,3.0,5.4):
                box(c,'Stepped audience bench',(x,y,.35+row*.18),(2.06,.56,.34+row*.35),W,.065)
                box(c,'Seat edge inlay',(x,y-.25,.55+row*.35),(1.9,.07,.06),T,.012)
        for x in (-6.3,6.3):pier(c,'Theatre shell side mast',x,4.75,6.0,M,.22)
        for i,x in enumerate((-6.7,-4.0,-1.33,1.33,4.0,6.7)):
            beam(c,'Radiating acoustic shell spar',(0,5.7,7.4),(x,2.15,4.25),.16,T)
        shell_front=[(-7.15,2.0,4.18),(-4.7,2.0,4.73),(-2.35,2.0,5.12),
                     (0,2.0,5.3),(2.35,2.0,5.12),(4.7,2.0,4.73),(7.15,2.0,4.18)]
        for i in range(6):
            a,b=shell_front[i],shell_front[i+1]
            crown=(0,5.7,7.4)
            prism(c,'Faceted fan acoustic shell petal',
                  [a,b,crown,(a[0],a[1],a[2]-.10),(b[0],b[1],b[2]-.10),(0,5.7,7.3)],
                  [(0,1,2),(5,4,3),(0,3,4,1),(1,4,5,2),(2,5,3,0)],
                  A if i%2 else T)
        for x in (-5.1,5.1):
            cylinder(c,'Concert lantern',(x,2.25,4.7),.28,.18,L,vertices=12,rot=(math.pi/2,0,0))
        sign(c,label,p,0,5.55,4.1,5.2)

    elif vid=='garage':
        # Two huge open service bays under a cantilevered industrial roof.
        box(c,'Garage rear service room',(0,4.82,1.58),(7.7,.27,2.96),D,.05)
        for x in (-5.7,0,5.7):
            pier(c,'Exposed garage I-column',x,-5.05,5.05,M,.3)
            box(c,'Service bay column flange',(x,-5.05,4.75),(.55,.42,.15),T,.02)
        for x in (-5.75,5.75):box(c,'Garage low side workshop wall',(x,0,.9),(.27,9.45,1.55),W,.04)
        for x,y,width,depth in ((-3.2,-.25,5.28,9.4),(3.7,1.35,4.0,5.8)):
            roof=barrel_roof(c,'Unequal arched garage service cover',width,depth,4.88,1.05,A)
            roof.location=(x,y,0)
            beam(c,'Exposed roof girder',(x,y-depth/2,4.93),(x,y+depth/2,4.93),.15,D)
        box(c,'Garage freestanding bay lintel',(0,-5.17,5.05),(11.55,.25,.22),D,.03)
        box(c,'Wide bay one warning header',(-2.85,-5.13,4.46),(5.3,.16,.24),T,.025)
        box(c,'Wide bay two warning header',(2.85,-5.13,4.46),(5.3,.16,.24),T,.025)
        for x in (-2.85,2.85):
            torus(c,'Service bay circular wheel badge',(x,-5.17,4.45),.27,.055,D,rot=(math.pi/2,0,0))
            cylinder(c,'Numbered garage bay light',(x,-5.23,4.45),.12,.035,L,vertices=12,rot=(math.pi/2,0,0))
        for i in range(10):
            x=-5.3+i*1.18
            box(c,'Alternating service threshold strip',(x,-4.65,.115),(.50,.50,.018),A if i%2 else D,.005)
        for x in (-5.4,5.4):
            box(c,'Garage side tool display frame',(x,2.55,2.75),(.09,2.75,1.5),M,.02)
            for z in (.68,1.38):
                torus(c,'Stacked entry service tire',(x,-4.08,z),.52,.19,D,rot=(math.pi/2,0,0))
            box(c,'Garage visible steel worklight',(x,-5.24,3.78),(.71,.15,.22),L,.025)
        sign(c,label,p,0,-5.54,5.1,4.5)

    elif vid=='observatory':
        # Round telescope drum at rear and an open viewing terrace at front.
        for a in range(16):
            ang=2*math.pi*a/16
            x=3.55*math.cos(ang); y=2.0+3.1*math.sin(ang)
            pier(c,'Faceted astronomy drum pier',x,y,5.2,T,.18)
            if a%2==0:box(c,'Drum glazing bay',(x,y,3.4),(.58,.14,1.2),G,.01)
        cylinder(c,'Round observatory cornice',(0,2.0,5.23),3.75,.35,D,vertices=20)
        dome(c,'Connected copper telescope dome',(0,2.0,5.43),3.45,A)
        torus(c,'Dome bearing ring',(0,2.0,5.43),3.53,.11,M)
        beam(c,'Open observatory slit rim left',(-.45,-1.10,6.2),(-.45,2.0,8.75),.12,D)
        beam(c,'Open observatory slit rim right',(.45,-1.10,6.2),(.45,2.0,8.75),.12,D)
        for x in (-5.7,5.7):
            box(c,'Viewing terrace low wall',(x,-2.2,.82),(.25,5.1,1.45),T,.04)
        sign(c,label,p,0,-5.34,2.75,5.3)

    elif vid=='harbor':
        # Open timber jetty, mast and a small rear boathouse canopy.
        for x in (-6.5,-5.2,-3.9,-2.6,-1.3,0,1.3,2.6,3.9,5.2,6.5):
            box(c,'Tightly joined dock decking plank',(x,-2.45,.20),(1.27,6.5,.16),W,.018)
        for x in (-6.5,6.5):
            for y in (-4.8,-1.9,1.0,3.9):
                pier(c,'Harbor dock mooring timber',x,y,1.48,D,.24)
                torus(c,'Mooring rope ring',(x,y,1.24),.17,.045,M)
            for y in (-3.4,-.4,2.5):
                beam(c,'Weathered dock guard rope',(x,y,1.15),(x,y+1.5,1.15),.045,T)
        for x in (-5.6,5.6):
            for y in (2.2,5.1):pier(c,'Boathouse canopy post',x,y,4.72 if y<3 else 5.18,W,.24)
        dock_canopy=canopy(c,'Harbor shed roof',12.3,3.9,5.25,4.6,A)
        dock_canopy.location.y=3.65
        beam(c,'Harbor signal mast',(0,4.1,.2),(0,4.1,9.2),.18,W)
        for y,z in ((2.8,4.3),(3.6,6.6)):
            beam(c,'Harbor mast spar',(-2.4,y,z),(2.4,y,z),.12,T)
        prism(c,'Maritime signal sail',[(-.16,4.1,8.75),(-.16,4.1,4.4),(2.6,4.1,4.4)],[(0,1,2)],T)
        for x in (-6.5,6.5):
            beam(c,'Dock entry signal pole',(x,-4.8,1.42),(x,-4.8,4.35),.065,M)
            prism(c,'Harbor pennant flag',[(x,-4.8,4.30),(x,-4.8,3.65),
                                        (x+(.9 if x<0 else -.9),-4.8,3.97)],[(0,1,2)],A)
        for x in (-4.3,4.3):
            box(c,'Harbor stacked cargo crate',(x,4.2,.57),(1.45,1.0,.86),W,.045)
            box(c,'Painted cargo identification stripe',(x,3.68,.65),(1.13,.04,.16),A,.012)
        sign(c,label,p,0,2.19,4.35,4.6)

    elif vid=='art':
        # Outdoor sculpture garden with three portal frames and a compact gallery.
        box(c,'Gallery rear display wall',(0,4.85,2.2),(14.4,.26,4.2),T,.05)
        for x in (-6.7,0,6.7):
            for y in (-4.6,3.2):pier(c,'Gallery sculptural portal leg',x,y,4.75,D,.25)
            box(c,'Gallery portal horizontal',(x,-.7,4.75),(.27,8.15,.24),D,.03)
        for y in (-4.6,3.2):box(c,'Gallery light truss',(0,y,4.78),(13.7,.22,.25),A,.03)
        gallery_roof=canopy(c,'Small rear gallery roof',14.5,2.5,5.1,4.78,T)
        gallery_roof.location.y=4.0
        for side in (-1,1):
            prism(c,'Gallery tilted festival sculpture sail',
                  [(0,-3.9,5.95),(side*6.6,-4.55,4.85),(side*6.6,.1,5.05)],
                  [(0,1,2),(2,1,0)],A if side<0 else G)
        for i,x in enumerate((-4.4,4.4)):
            cylinder(c,'Sculpture plinth',(x,-2.7,.47),.78,.72,W,vertices=12)
            for j in range(3):
                beam(c,'Abstract welded sculpture',(x,-2.7,.83+j*.47),(x+(.42 if j%2 else -.42),-2.7,1.33+j*.47),.17,(A,M,T)[(i+j)%3])
        for side in (-1,1):
            prism(c,'Carved gallery entrance ribbon',
                  [(side*6.65,-4.6,1.30),(side*6.65,-4.6,4.55),
                   (side*6.15,-4.6,4.25),(side*6.15,-4.6,1.30)],
                  [(0,1,2,3),(3,2,1,0)],A if side<0 else G)
        for x in (-4.5,0,4.5):
            box(c,'Gallery picture frame backing',(x,4.61,2.63),(2.5,.12,2.3),D,.06)
            box(c,'Exhibited colour field canvas',(x,4.52,2.63),(2.22,.04,2.02),A if x else G,.025)
            box(c,'Relief art geometry',(x,4.47,2.63),(.64,.04,1.49),T,.01)
        sign(c,label,p,0,4.55,3.95,5.0)

    elif vid=='recycling':
        # Outdoor sorting yard: stepped bin bays, open roof, conveyor beacon.
        box(c,'Depot rear retaining wall',(0,4.85,1.50),(14.5,.28,2.8),W,.055)
        for x,height in ((-6.9,5.16),(-2.3,5.48),(2.3,5.80),(6.9,5.80)):
            pier(c,'Recycling yard steel mast',x,3.8,height,D,.20)
            box(c,'Open sorting bay low divider',(x,1.95,.78),(.20,5.6,1.36),W,.04)
        for bay,x in enumerate((-4.55,0,4.55)):
            shed=canopy(c,'Stepped recycled-sheet sorting roof',4.65,4.7,
                        5.45+bay*.32,4.72+bay*.32,A)
            shed.location.x=x;shed.location.y=3.30
        for x in (-4.55,0,4.55):
            box(c,'Oversized colour-coded hopper',(x,2.85,1.25),(3.25,1.45,1.9),A,.10)
            box(c,'Hopper intake lip',(x,2.05,2.12),(3.4,.18,.2),D,.022)
        for x in (-5.6,5.6):
            cylinder(c,'Vertical recycling beacon',(x,-4.4,3.05),.11,5.8,M,vertices=12)
            torus(c,'Circular recycling emblem',(x,-4.4,5.6),.46,.10,T,rot=(math.pi/2,0,0))
        sign(c,label,p,0,4.58,4.18,5.5)

def prop(c, label, kind, p, seed):
    random.seed(seed)
    A,W,D,M,G,L,F,T=p['accent'],p['wood'],p['dark'],p['metal'],p['glass'],p['light'],p['floor'],p['trim']
    if kind in ('bread','apple','cup','tossring'):
        if kind=='bread':
            outline=[(-.28,.10),(-.33,.23),(-.25,.43),(0,.55),(.25,.43),(.33,.23),(.28,.10)]
            verts=[(x,y,z) for x in (-.53,.53) for y,z in outline]
            n=len(outline)
            faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]
            faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
            prism(c,'Hand-shaped scored loaf',verts,faces,p['food'])
            for x in (-.24,0,.24):
                beam(c,'Baked diagonal score',(x,-.17,.48),(x+.12,.14,.48),.022,F)
        elif kind=='apple':
            profile=[(.06,.06),(.11,.19),(.20,.27),(.35,.30),(.49,.25),(.56,.12),(.51,.04)]
            n=16;verts=[]
            for z,r in profile:
                verts += [(r*math.cos(i*2*math.pi/n),r*math.sin(i*2*math.pi/n),z) for i in range(n)]
            faces=[]
            for k in range(len(profile)-1):
                for i in range(n):faces.append((k*n+i,k*n+(i+1)%n,(k+1)*n+(i+1)%n,(k+1)*n+i))
            prism(c,'Lobed apple body',verts,faces,p['apple'])
            beam(c,'Bent apple stem',(0,0,.51),(.08,0,.67),.035,W)
            ball(c,'Apple leaf',(.15,0,.61),.09,p['leaf'])
        elif kind=='cup':
            cylinder(c,'Ceramic cup body',(0,0,.25),.24,.44,T,vertices=16,bevel=.035)
            torus(c,'Rolled cup rim',(0,0,.47),.235,.025,A)
            cylinder(c,'Dark coffee surface',(0,0,.468),.20,.012,W,vertices=16,bevel=0)
            torus(c,'Arched mug handle',(.28,0,.27),.16,.04,A,rot=(0,math.pi/2,0))
            cylinder(c,'Foot ring',(0,0,.04),.16,.05,A,vertices=16)
        else:
            torus(c,'Continuous hand-grip ring',(0,0,.12),.42,.10,A)
            for i in range(4):
                a=i*math.pi/2
                ball(c,'Contrasting grip patch',(.42*math.cos(a),.42*math.sin(a),.12),.07,T)
    elif kind in ('counter','table','bench','bed','lounger','board','conveyor'):
        w={'counter':2.4,'table':1.5,'bench':1.8,'bed':2.2,'lounger':2,'board':2.1,'conveyor':2.6}[kind]
        h={'bed':.62,'lounger':.42,'bench':.48,'board':.75}.get(kind,.90)
        box(c,'Layered hardwood top',(0,0,h),(w,.78,.14),W,.06)
        box(c,'Inlaid upper surface',(0,0,h+.08),(w-.14,.67,.03),A,.02)
        for x in (-w*.43,w*.43):
            for y in (-.29,.29): box(c,'Tapered structural leg',(x,y,h/2),(.10,.10,h),D,.025)
        if kind in ('counter','conveyor'):
            box(c,'Lower service shelf',(0,0,.22),(w-.2,.65,.07),F,.02)
            box(c,'Front accent rail',(0,-.4,h-.16),(w-.1,.06,.12),M,.02)
        elif kind in ('bench','lounger','bed'):
            box(c,'Angled upholstered back',(0,.39,h+.43),(w,.15,.8),A,.07)
            for x in (-w*.38,w*.38):box(c,'Handrail',(x,-.03,h+.19),(.07,.8,.10),M,.02)
        elif kind=='board':
            box(c,'Cantilevered diving tip',(0,-.42,h+.1),(w*.6,.55,.11),G,.025)
    elif kind in ('cabinet','display','rack','screen','bins'):
        w=1.9 if kind!='bins' else 2.3
        for x in (-w/2,w/2):box(c,'Upright joinery',(x,0,1.0),(.12,.8,2.0),W,.03)
        for z in (.15, .82,1.5,2.0):box(c,'Shelving board',(0,0,z),(w,.84,.12),W,.035)
        box(c,'Rear beadboard',(0,.38,1),(w-.1,.07,1.85),F,.025)
        if kind=='cabinet':
            for x in (-.45,.45):
                box(c,'Glazed cabinet door',(x,-.43,1.05),(.72,.05,1.45),G,.035)
                cylinder(c,'Door pull',(x+.25,-.48,1.06),.035,.08,M,rot=(math.pi/2,0,0))
        elif kind=='bins':
            for x in (-.63,0,.63):
                box(c,'Color sorted bin',(x,-.29,.77),(.52,.61,.80),A,.07)
                torus(c,'Circular intake',(x,-.63,1.02),.16,.035,D,rot=(math.pi/2,0,0))
        elif kind=='screen':
            for x in (-.6,0,.6):box(c,'Woven privacy panel',(x,-.08,1.04),(.53,.08,1.76),A,.02)
        else:
            for x in (-.52,.52):
                for z in (.35,1.02,1.68):box(c,'Displayed parcel',(x,-.08,z),(.42,.43,.32),A,.045)
    elif kind in ('basket','crate','case','tray','mailbox'):
        w=1.12 if kind not in ('tray','mailbox') else .82
        box(c,'Container floor',(0,0,.13),(w,.70,.16),W,.045)
        for x in (-w/2,w/2):box(c,'Raised side rail',(x,0,.34),(.08,.7,.43),A,.026)
        for y in (-.34,.34):box(c,'Reinforced end rail',(0,y,.34),(w,.08,.43),A,.026)
        for x in (-w*.27,w*.27):box(c,'Woven slat',(x,-.36,.31),(.075,.025,.28),F,.008)
        if kind in ('basket','case'):
            torus(c,'Arched carrying handle',(0,0,.53),.41,.045,M,rot=(math.pi/2,0,0))
        elif kind=='mailbox':
            box(c,'Letter slot',(0,-.39,.50),(.65,.06,.08),D,.018)
            box(c,'Postal lid',(0,0,.60),(w+.1,.78,.10),A,.025)
    elif kind in ('cart','lift','crane'):
        box(c,'Chassis frame',(0,0,.62),(1.55,.9,.15),W,.07)
        box(c,'Cargo deck',(0,0,.75),(1.5,.8,.11),A,.04)
        for x in (-.57,.57):
            for y in (-.49,.49):
                cylinder(c,'Spoked wheel',(x,y,.34),.24,.075,D,rot=(math.pi/2,0,0))
                cylinder(c,'Wheel hub',(x,y-.055,.34),.07,.09,M,rot=(math.pi/2,0,0))
        for x in (-.69,.69):box(c,'Push bar upright',(x,.45,1.18),(.055,.055,.83),M,.012)
        box(c,'Ergonomic push grip',(0,.45,1.56),(1.45,.08,.08),M,.02)
        if kind in ('lift','crane'):
            beam(c,'Hydraulic vertical',(0,.22,.8),(0,.22,2.8),.15,M)
            beam(c,'Extension boom',(0,.22,2.65),(1.5,.22,2.65),.10,A)
            cylinder(c,'Pulley wheel',(1.5,.22,2.65),.18,.1,D,rot=(math.pi/2,0,0))
    elif kind in ('machine','grinder','mixer','press','pump','monitor','speaker','engine','oven','tumbler'):
        w=1.3 if kind!='speaker' else .82
        box(c,'Cast machine housing',(0,0,.76),(w,.92,1.25),A,.13)
        box(c,'Steel footplate',(0,0,.10),(w+.15,1.04,.20),D,.05)
        box(c,'Recessed control panel',(0,-.49,1.00),(w*.78,.055,.45),D,.025)
        for x in (-.3,0,.3):cylinder(c,'Brass control dial',(x,-.535,1.03),.07,.04,M,rot=(math.pi/2,0,0))
        box(c,'Display / access glass',(0,-.49,.49),(w*.62,.05,.30),G,.025)
        if kind in ('grinder','mixer','tumbler'):
            cylinder(c,'Rotary vessel',(0,0,1.65),.43,.58,M)
            torus(c,'Vessel rim',(0,0,1.96),.42,.035,D)
        elif kind in ('press','pump'):
            cylinder(c,'Operating ram',(0,0,1.77),.09,.72,M)
            box(c,'Cross handle',(0,0,2.15),(.68,.07,.07),W,.02)
        elif kind=='oven':
            box(c,'Firebox door',(0,-.50,.72),(.74,.055,.68),D,.07)
            torus(c,'Oven door bezel',(0,-.56,.72),.34,.03,M,rot=(math.pi/2,0,0))
        elif kind=='speaker':
            for z in (.44,1.02):torus(c,'Driver surround',(0,-.52,z),.22,.07,D,rot=(math.pi/2,0,0))
        elif kind=='monitor':
            box(c,'Screen bezel',(0,-.55,1.35),(.9,.06,.65),D,.04)
            box(c,'Illuminated screen',(0,-.59,1.35),(.78,.035,.51),G,.01)
        else:
            cylinder(c,'Indicator lamp',(0,-.53,1.41),.09,.05,L,rot=(math.pi/2,0,0))
    elif kind in ('barrel','can','dome','plant','globe','buoy'):
        cylinder(c,'Turned vessel body',(0,0,.57),.42,.94,A,vertices=20,bevel=.07)
        for z in (.20,.96):torus(c,'Rolled rim',(0,0,z),.41,.045,M)
        if kind=='plant':
            for i in range(5):
                a=i*2*math.pi/5
                beam(c,'Stem',(0,0,1),(math.cos(a)*.26,math.sin(a)*.26,1.7),.045,p['leaf'])
                ball(c,'Leaf',(math.cos(a)*.31,math.sin(a)*.31,1.72),.22,p['leaf'])
        elif kind in ('globe','buoy','dome'):
            ball(c,'Rounded top',(0,0,1.27),.42,G if kind=='dome' else L)
        else:
            beam(c,'Pouring spout',(.35,0,.88),(.82,0,1.18),.09,M)
            torus(c,'Side grip',(-.51,0,.67),.22,.05,D,rot=(0,math.pi/2,0))
    elif kind in ('lamp','microphone','stand','podium','bollard','cone'):
        cylinder(c,'Foot plinth',(0,0,.12),.42,.24,D)
        beam(c,'Turned support',(0,0,.2),(0,0,1.9),.10,M)
        if kind=='lamp':
            cylinder(c,'Lantern collar',(0,0,1.98),.35,.12,D)
            ball(c,'Opal light glass',(0,0,2.24),.31,L)
            cylinder(c,'Lantern crown',(0,0,2.56),.25,.08,A)
        elif kind=='microphone':ball(c,'Microphone capsule',(0,0,2.05),.18,D)
        elif kind=='podium':box(c,'Sloped lectern shelf',(0,-.1,1.65),(.93,.57,.18),W,.05)
        elif kind=='bollard':torus(c,'Mooring loop',(0,0,1.90),.24,.06,D)
        elif kind=='cone':
            prism(c,'Pyramidal safety cone',[(-.3,-.3,.2),(.3,-.3,.2),(.3,.3,.2),(-.3,.3,.2),(-.08,-.08,1.0),(.08,-.08,1.0),(.08,.08,1.0),(-.08,.08,1.0)],[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],A)
        else:box(c,'Display plate',(0,-.16,1.9),(.55,.07,.38),A,.025)
    elif kind in ('ring','gear','wheel','orrery','scale'):
        cylinder(c,'Instrument foundation',(0,0,.12),.57,.24,D)
        beam(c,'Support mast',(0,0,.21),(0,0,1.3),.09,M)
        torus(c,'Machined outer ring',(0,0,1.52),.49,.085,A,rot=(math.pi/2,0,0))
        torus(c,'Inner brass bezel',(0,-.06,1.52),.31,.035,M,rot=(math.pi/2,0,0))
        if kind in ('gear','wheel'):
            for i in range(8):
                a=i*math.pi/4
                beam(c,'Radial spoke',(0,-.07,1.52),(.42*math.cos(a),-.07,1.52+.42*math.sin(a)),.05,M)
        elif kind=='scale':
            beam(c,'Balance beam',(-.6,0,1.95),(.6,0,1.95),.075,W)
            for x in (-.58,.58):
                beam(c,'Pan hanger',(x,0,1.95),(x,0,1.4),.025,M)
                cylinder(c,'Brass weighing pan',(x,0,1.38),.3,.04,M)
        elif kind=='orrery':
            for x,z,r in ((-.35,1.65,.14),(.36,1.29,.10)):
                beam(c,'Orbital arm',(0,0,1.52),(x,0,z),.035,M)
                ball(c,'Planet',(x,0,z),r,L)
    elif kind in ('piano','drum','telescope','boat','easel','canvas','book','vise','chair','trellis','windmill'):
        if kind=='piano':
            box(c,'Carved piano body',(0,0,.77),(1.7,.8,.48),D,.12)
            box(c,'Keybed',(0,-.47,.91),(1.53,.40,.10),F,.02)
            for i in range(11):box(c,'Ivory key',(-.68+i*.135,-.51,.98),(.115,.28,.03),p['trim'],.008)
            box(c,'Music shelf',(0,.1,1.45),(1.2,.1,.72),W,.05)
        elif kind=='drum':
            cylinder(c,'Tensioned drum shell',(0,0,.72),.62,1.06,A,vertices=24)
            for z in (.18,1.25):torus(c,'Chrome drum rim',(0,0,z),.61,.05,M)
            for i in range(8):
                a=i*math.pi/4
                beam(c,'Tension rod',(.60*math.cos(a),.60*math.sin(a),.23),(.60*math.cos(a),.60*math.sin(a),1.2),.025,M)
        elif kind=='telescope':
            for a in (0,2.1,4.2):beam(c,'Tripod leg',(0,0,1.30),(.65*math.cos(a),.65*math.sin(a),.10),.075,W)
            o=cylinder(c,'Optical tube',(0,0,1.65),.28,1.80,D,vertices=20,rot=(0,math.pi/4,0))
            torus(c,'Objective rim',(.58,0,2.25),.27,.04,M,rot=(0,math.pi/4,0))
            cylinder(c,'Lens glass',(.62,0,2.27),.24,.03,G,rot=(0,math.pi/4,0))
        elif kind=='boat':
            prism(c,'Modeled tapered boat hull',[(-1.5,-.6,.6),(1.5,-.6,.6),(-1.5,.6,.6),(1.5,.6,.6),(-.6,-.35,.08),(.6,-.35,.08),(-.6,.35,.08),(.6,.35,.08)],[(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5),(4,5,7,6)],W)
            for y in (-.40,0,.40):box(c,'Ribbed seat',(0,y,.64),(1.9,.09,.12),A,.025)
            beam(c,'Oar lock',(-.9,0,.8),(-1.7,-1.2,.8),.06,M)
        elif kind in ('easel','canvas'):
            for x in (-.5,.5):beam(c,'A-frame leg',(x,0,.10),(0,0,2.1),.08,W)
            beam(c,'Rear kickstand',(0,.08,2),(0,.72,.12),.07,W)
            box(c,'Stretched canvas',(0,-.12,1.25),(1.35,.13,1.18),F,.03)
            box(c,'Abstract paint panel',(0,-.20,1.25),(1.11,.025,.94),A,.01)
            box(c,'Easel tray',(0,-.27,.6),(1.42,.15,.08),W,.03)
        elif kind=='book':
            box(c,'Embossed book cover',(0,0,.09),(1.1,.77,.11),A,.025)
            box(c,'Page block',(0,0,.17),(1.02,.70,.11),F,.018)
            box(c,'Open left page',(-.27,-.03,.25),(.54,.67,.04),p['trim'],.008)
            box(c,'Open right page',(.27,-.03,.25),(.54,.67,.04),p['trim'],.008)
            beam(c,'Raised spine',(0,-.35,.1),(0,.35,.1),.07,M)
        elif kind=='vise':
            box(c,'Vise shoe',(0,0,.17),(1.1,.65,.34),D,.05)
            for x in (-.4,.4):box(c,'Steel jaw',(x,0,.58),(.18,.55,.55),M,.025)
            cylinder(c,'Lead screw',(.52,0,.38),.08,.67,W,rot=(0,math.pi/2,0))
            box(c,'T bar handle',(.86,0,.38),(.07,.53,.07),M,.02)
        elif kind=='chair':
            box(c,'Guard seat',(0,0,1.7),(.8,.72,.12),W,.04)
            for x in (-.33,.33):
                for y in (-.3,.3):beam(c,'Tall lifeguard leg',(x,y,.10),(x,y,1.7),.07,A)
            box(c,'Backrest',(0,.34,2.07),(.8,.11,.72),A,.05)
            for z in (.50,1.0,1.48):box(c,'Ladder rung',(0,-.39,z),(.65,.07,.07),M,.015)
        elif kind=='windmill':
            prism(c,'Tapered timber windmill tower',[(-.62,-.62,.10),(.62,-.62,.10),(.62,.62,.10),(-.62,.62,.10),(-.36,-.36,2.95),(.36,-.36,2.95),(.36,.36,2.95),(-.36,.36,2.95)],[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],W)
            cylinder(c,'Rotor axle',(0,-.46,2.48),.10,.62,M,vertices=12,rot=(math.pi/2,0,0))
            cylinder(c,'Rotor hub',(0,-.80,2.48),.21,.16,D,vertices=12,rot=(math.pi/2,0,0))
            for i in range(4):
                a=i*math.pi/2
                beam(c,'Windmill sail spar',(0,-.90,2.48),(.92*math.cos(a),-.90,2.48+.92*math.sin(a)),.10,T)
                box(c,'Sail tip vane',(.75*math.cos(a),-.93,2.48+.75*math.sin(a)),(.28,.04,.18),A,.018)
            box(c,'Mill service hatch',(0,-.64,.80),(.46,.05,.75),D,.03)
        else:
            for x in (-.72,0,.72):beam(c,'Garden trellis pole',(x,0,.1),(x,0,2.25),.06,W)
            for z in (.4,.85,1.3,1.75,2.15):box(c,'Lattice cross rail',(0,0,z),(1.6,.07,.06),W,.015)
            for x in (-.45,.35):beam(c,'Climbing vine',(x,0,.3),(x+.32,0,2),.04,p['leaf'])
    else:
        # Fallback remains an assembled sculptural asset, never a lone primitive.
        box(c,'Handcrafted base',(0,0,.1),(1.1,.75,.20),W,.045)
        cylinder(c,'Turned stem',(0,0,.7),.13,1.1,M)
        box(c,'Accent cap',(0,0,1.3),(.55,.50,.12),A,.04)

    prop_identity(c,label,kind,p)

def replace_common_prop_base(c,label,kind,p):
    """Replace a shared blockout chassis when it misstates a prop's function."""
    replacement={'grinder','mixer','tumbler','engine','monitor','oven','pump',
                 'press','speaker','display','screen','bins','rack','lift',
                 'crane','can','globe','mailbox','case','cone','bollard',
                 'lounger','bench','bed','canvas','orrery','ring','basket'}
    if kind not in replacement:return
    for obj in tuple(c.objects):bpy.data.objects.remove(obj,do_unlink=True)
    A,W,D,M,G,L,F,T=p['accent'],p['wood'],p['dark'],p['metal'],p['glass'],p['light'],p['floor'],p['trim']
    k=label.lower()
    if kind=='grinder':
        box(c,'Coffee grinder narrow cast base',(0,0,.67),(.80,.70,1.18),D,.08)
        box(c,'Ground coffee chute',(0,-.41,1.04),(.38,.21,.51),M,.03)
        cylinder(c,'Hopper throat',(0,0,1.57),.22,.62,M,vertices=12)
    elif kind=='mixer':
        cylinder(c,'Bakery dough mixer turned pillar',(0,0,.63),.29,1.13,A,vertices=12)
        for x in (-.70,.70):
            box(c,'Mixer splayed ground foot',(x,0,.14),(.76,.31,.20),D,.035)
        box(c,'Overhead kneader support',(0,.30,1.48),(.35,.55,.90),D,.035)
    elif kind=='tumbler':
        box(c,'Compost drum ground cradle',(0,0,.24),(1.85,1.10,.32),W,.05)
        for x in (-.70,.70):
            box(c,'Drum bearing A-frame',(x,0,.95),(.20,.86,1.48),M,.035)
    elif kind=='engine':
        box(c,'Garage cast engine block',(0,0,.78),(1.54,.90,1.27),D,.09)
        box(c,'Engine service skid',(0,0,.13),(1.78,1.13,.20),M,.025)
    elif kind=='monitor':
        box(c,'Clinic rolling monitor foot',(0,0,.13),(1.12,.78,.20),M,.04)
        beam(c,'Clinical monitor slim pedestal',(0,0,.18),(0,0,1.89),.12,T)
        for x in (-.45,.45):cylinder(c,'Monitor caster',(x,-.34,.12),.11,.08,D,vertices=10)
    elif kind=='oven':
        box(c,'Brick bread oven kiln body',(0,0,.79),(1.64,1.30,1.38),W,.07)
        box(c,'Bread oven brick crown course',(0,0,1.48),(1.72,1.34,.18),T,.02)
        arch(c,'Recessed bread oven mouth',0,-.68,.80,.47,.12,.15,D)
    elif kind=='pump':
        if 'water' in k:
            cylinder(c,'Garden hand pump cast pipe',(0,0,1.12),.28,1.91,M,vertices=12)
            box(c,'Water pump ground pad',(0,0,.11),(1.06,.90,.19),W,.04)
            beam(c,'Hand pump rising handle post',(0,0,1.78),(0,0,2.35),.065,W)
        else:
            box(c,'Forecourt fuel pump pedestal',(0,0,1.18),(.80,.70,2.15),A,.08)
            box(c,'Fuel pump price display',(0,-.39,1.68),(.57,.05,.51),G,.02)
    elif kind=='press':
        if 'stamp' in k:
            box(c,'Desktop postal press cast stand',(0,0,.76),(1.14,.84,1.22),D,.07)
            beam(c,'Press vertical ram',(0,0,1.27),(0,0,2.03),.10,M)
        else:
            box(c,'Industrial bottle compactor cabinet',(0,0,1.01),(1.52,1.06,1.70),A,.10)
            box(c,'Bottle compactor reinforced door',(0,-.57,.89),(1.14,.08,1.14),D,.035)
    elif kind=='speaker':
        box(c,'Stage speaker tall sound cabinet',(0,0,1.13),(.98,.77,2.12),D,.045)
        for z,r in ((.66,.28),(1.56,.36)):
            torus(c,'Speaker cone surround',(0,-.40,z),r,.07,A,rot=(math.pi/2,0,0))
            cylinder(c,'Speaker center driver',(0,-.43,z),r*.50,.04,M,
                     vertices=12,rot=(math.pi/2,0,0))
    elif kind=='display':
        if 'produce' in k:
            cylinder(c,'Bazaar fruit carousel pedestal',(0,0,.56),.70,.91,W,vertices=12)
            box(c,'Raised produce display sweep',(0,-.09,1.05),(2.22,1.02,.18),T,.025)
            for x in (-1.05,1.05):pier(c,'Stall valance post',x,.24,2.32,M,.065)
        else:
            box(c,'Bakery glass-case counter',(0,0,.86),(2.25,1.04,1.51),W,.06)
            box(c,'Pastel pastry display plinth',(0,-.16,1.62),(2.08,.72,.10),T,.02)
    elif kind=='screen':
        for x in (-1.09,0,1.09):pier(c,'Changing screen upright',x,0,2.27,W,.065)
        for z in (.24,2.25):box(c,'Changing cubicle horizontal rail',(0,0,z),(2.38,.10,.09),M,.012)
        for x in (-1.06,1.06):box(c,'Changing screen stable foot',(x,0,.09),(.42,.92,.13),W,.012)
    elif kind=='bins':
        for x,color in ((-.88,G),(0,A),(.88,T)):
            box(c,'Separated recycling sorting bin',(x,0,.75),(.53,.78,1.35),color,.07)
            torus(c,'Recycling container round intake',(x,-.42,1.10),.19,.045,D,
                  rot=(math.pi/2,0,0))
        box(c,'Sorting bin canopy label bar',(0,.22,1.66),(2.48,.25,.21),T,.025)
    elif kind=='rack':
        if 'tool' in k:
            for x in (-1,1):pier(c,'Workshop pegboard side standard',x,0,2.25,M,.10)
            box(c,'Workshop sparse bottom tool shelf',(0,-.10,.35),(2.12,.66,.13),W,.02)
        elif 'rolling' in k:
            for x in (-.96,.96):
                for y in (-.35,.35):pier(c,'Bakery rack light rail',x,y,2.31,M,.07)
            for z in (.35,1.06,1.76):box(c,'Bakery slatted cooling tray',(0,0,z),(2.05,.82,.10),W,.02)
            for x in (-.82,.82):cylinder(c,'Bakery rack caster',(x,-.39,.12),.11,.07,D,vertices=10)
        elif 'tire' in k:
            for x in (-.94,.94):pier(c,'Garage tire rack end post',x,0,2.17,M,.10)
            for z in (.36,1.19,2.05):box(c,'Tire rack heavy angle iron',(0,0,z),(2.04,.85,.12),D,.01)
        else:
            box(c,'Artist brush rack wide pigment trough',(0,0,.63),(2.1,.75,1.12),W,.055)
            for x in (-.96,.96):pier(c,'Tall artist brush holder',x,.15,2.25,M,.07)
    elif kind=='lift':
        box(c,'Garage lift concrete base',(0,0,.14),(2.15,1.4,.22),D,.03)
        box(c,'Lift fixed service platform',(0,0,.78),(1.86,1.11,.18),M,.03)
    elif kind=='crane':
        box(c,'Harbor crane anchored dock plinth',(0,0,.25),(1.65,1.44,.39),W,.04)
        box(c,'Crane central slewing tower',(-.64,.10,1.47),(.46,.52,2.23),M,.03)
    elif kind=='can':
        cylinder(c,'Watering can tapered reservoir',(0,0,.57),.48,.88,A,vertices=12)
        beam(c,'True angled watering spout',(.35,0,.72),(1.05,0,1.12),.13,M)
        torus(c,'Watering can broad side handle',(-.54,0,.65),.34,.065,W,
              rot=(0,math.pi/2,0))
        cylinder(c,'Shower rose nozzle',(1.10,0,1.17),.22,.09,M,vertices=10,
                 rot=(0,math.pi/3,0))
    elif kind=='globe':
        box(c,'Moon globe astronomical pedestal',(0,0,.37),(.65,.65,.62),D,.07)
        beam(c,'Moon globe inclination spindle',(0,0,.62),(.10,0,1.61),.08,M)
        ball(c,'Illuminated lunar globe',(.12,0,1.80),.70,G)
        torus(c,'Globe equatorial guide',(.12,0,1.80),.72,.035,T)
    elif kind=='mailbox':
        cylinder(c,'Postal public letterbox body',(0,0,1.06),.49,1.58,A,vertices=12)
        cylinder(c,'Postal letterbox dome cap',(0,0,1.88),.52,.16,T,vertices=12)
        box(c,'Mail collection slot',(0,-.51,1.57),(.72,.08,.12),D,.02)
        box(c,'Letterbox ground casting',(0,0,.18),(.81,.78,.28),D,.04)
    elif kind=='case':
        box(c,'Instrument travel case long shell',(0,0,.42),(1.86,.72,.72),D,.14)
        box(c,'Instrument case brass latch',(0,-.38,.49),(.33,.09,.16),M,.02)
        beam(c,'Case hand carry grip',(0,0,.82),(0,0,1.14),.08,W)
    elif kind=='cone':
        box(c,'Traffic cone weighted black base',(0,0,.11),(.78,.78,.15),D,.03)
        prism(c,'Solid tapered traffic cone body',
              [(-.30,-.30,.18),(.30,-.30,.18),(.30,.30,.18),(-.30,.30,.18),
               (-.08,-.08,1.21),(.08,-.08,1.21),(.08,.08,1.21),(-.08,.08,1.21)],
              [(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],A)
        box(c,'Cone broad reflective front stripe',(0,-.185,.72),(.35,.035,.15),T,.008)
    elif kind=='bollard':
        cylinder(c,'Heavy dock cleat concrete body',(0,0,.69),.46,1.19,W,vertices=12)
        box(c,'Mooring dock bolted footing',(0,0,.12),(1.02,.91,.20),D,.03)
    elif kind=='lounger':
        box(c,'Long pool chaise seat',(0,0,.50),(2.31,.79,.15),G,.055)
        back=box(c,'Reclining lounge back',(0,.43,.91),(2.22,.13,1.05),A,.05)
        back.rotation_euler.x=.45
        for x in (-.94,.94):
            for y in (-.30,.30):box(c,'Chaise support leg',(x,y,.27),(.10,.10,.46),M,.02)
    elif kind=='bench':
        box(c,'Library timber study seat',(0,0,.47),(1.82,.63,.14),W,.04)
        box(c,'Upright study bench back',(0,.33,.96),(1.82,.12,.87),A,.04)
        for x in (-.72,.72):
            for y in (-.23,.23):box(c,'Study bench leg',(x,y,.24),(.12,.11,.43),D,.02)
    elif kind=='bed':
        box(c,'Clinic padded examination couch',(0,0,.63),(2.17,.85,.18),G,.065)
        box(c,'Examination bed rigid base',(0,0,.32),(1.72,.68,.51),T,.03)
        box(c,'Raised medical pillow',(-.76,0,.78),(.40,.64,.16),F,.055)
    elif kind=='canvas':
        box(c,'Free-standing gallery canvas foot',(0,0,.14),(2.14,.59,.20),D,.025)
        for x in (-.84,.84):
            pier(c,'Gallery canvas display support',x,.13,1.51,W,.07)
    elif kind=='orrery':
        cylinder(c,'Ornate astronomical orrery table',(0,0,.18),.77,.28,D,vertices=12)
        beam(c,'Orrery turning pedestal',(0,0,.24),(0,0,1.69),.13,M)
    elif kind=='ring':
        if 'target' in k:
            box(c,'Pool ring toss target cross-foot',(0,0,.16),(1.64,.75,.20),W,.025)
            beam(c,'Target stand central mast',(0,0,.20),(0,0,2.41),.10,M)
        else:
            box(c,'Lifebuoy shore-side bracket',(0,0,.17),(1.12,.73,.22),W,.025)
            beam(c,'Life buoy harbor support',(0,0,.23),(0,0,1.72),.09,M)
    elif kind=='basket':
        if 'wicker' in k:
            cylinder(c,'Wide woven market fruit basket',(0,0,.30),.59,.47,W,vertices=12)
            torus(c,'Market basket woven upper rim',(0,0,.53),.61,.07,A)
        else:
            cylinder(c,'Tall round bakery baguette hamper',(0,0,.38),.52,.59,W,vertices=12)
            for z in (.17,.61):torus(c,'Baguette hamper woven bands',(0,0,z),.53,.055,T)

def prop_identity(c,label,kind,p):
    """Authored silhouettes for repeated furniture roles across venue themes.

    The common joinery under each item is a reusable construction language;
    these functional upper assemblies make each exported prop a distinct model.
    """
    replace_common_prop_base(c,label,kind,p)
    A,W,D,M,G,L,F,T=p['accent'],p['wood'],p['dark'],p['metal'],p['glass'],p['light'],p['floor'],p['trim']
    k=label.lower()
    if kind=='cart':
        if 'wooden' in k:
            for x in (-.68,.68):pier(c,'Market cart canopy upright',x,.32,2.0,W,.07)
            box(c,'Market striped rolling stall roof',(0,0,2.09),(1.9,1.15,.13),T,.02)
            for x in (-.48,0,.48):box(c,'Crated fresh produce',(x,-.2,1.03),(.40,.48,.43),A,.05)
        elif 'towel' in k:
            for z in (1.0,1.35,1.7):
                cylinder(c,'Rolled pool towel',(0,-.1,z),.17,1.48,T,vertices=10,rot=(0,math.pi/2,0))
            for x in (-.76,.76):box(c,'Towel trolley tall side',(x,0,1.27),(.07,.85,1.08),G,.02)
        elif 'parcel' in k:
            for x,z in ((-.4,1.08),(.33,1.20),(0,1.62)):
                box(c,'Unequal delivery parcel',(x,-.05,z),(.62,.62,.45),W,.035)
            box(c,'Tall postal cart cage back',(0,.42,1.4),(1.55,.07,1.6),M,.02)
        elif 'first aid' in k:
            box(c,'Enclosed emergency supply case',(0,0,1.42),(1.28,.72,1.32),T,.06)
            box(c,'First aid cross upright',(0,-.41,1.47),(.16,.04,.69),A,.01)
            box(c,'First aid cross arm',(0,-.42,1.47),(.64,.04,.16),A,.01)
        elif 'book return' in k:
            for x in (-.55,0,.55):
                box(c,'Upright return book',(x,-.10,1.43),(.29,.62,.92),A if x==0 else W,.02)
            box(c,'Slanted book trolley rack',(0,.34,1.77),(1.55,.12,.95),M,.03)
        elif 'service' in k:
            box(c,'Mechanic mobile tool chest',(0,-.05,1.16),(1.54,.8,.72),D,.055)
            for z in (1.0,1.25,1.48):box(c,'Tool drawer face',(0,-.48,z),(1.38,.045,.16),A,.012)
            box(c,'Tool trolley side pegboard',(.78,0,1.47),(.08,.86,1.1),M,.02)
        elif 'paint' in k:
            box(c,'Paint trolley upright canvas rack',(0,.37,1.60),(1.15,.09,1.48),W,.03)
            box(c,'Color panel on mobile easel',(0,.29,1.62),(.95,.04,1.12),A,.01)
            for x in (-.45,.45):cylinder(c,'Paint tin',(x,-.26,1.08),.17,.38,T,vertices=10)
        else:
            box(c,'High recycled-material collecting tub',(0,0,1.48),(1.72,.86,1.26),A,.075)
            box(c,'Slanted recycling intake lip',(0,-.53,2.09),(1.82,.24,.18),T,.02)
    elif kind=='counter':
        if 'cafe' in k:
            for x in (-.95,.95):pier(c,'Coffee bar canopy post',x,.25,2.10,M,.07)
            box(c,'Cafe bar hanging menu canopy',(0,.25,2.15),(2.65,.8,.12),A,.03)
            cylinder(c,'Counter espresso appliance',(-.58,-.05,1.2),.29,.5,M,vertices=12)
        elif 'workbench' in k:
            box(c,'Workbench tall tool pegboard',(0,.35,1.56),(2.30,.12,1.13),D,.02)
            for x in (-.73,0,.73):beam(c,'Workbench hanging tool',(x,.24,1.9),(x,.24,1.3),.06,M)
        elif 'planting' in k:
            for x in (-.9,.9):beam(c,'Planting bench timber trellis',(x,.32,.9),(x,.32,2.15),.07,W)
            for z in (1.35,1.82,2.15):box(c,'Planting trellis rail',(0,.32,z),(2.0,.07,.08),W,.01)
            for x in (-.58,.58):box(c,'Seedling tray',(x,-.1,1.06),(.6,.45,.18),A,.02)
        elif 'dough' in k:
            box(c,'Bakery rounded flour guard',(0,.35,1.22),(2.6,.14,.57),T,.06)
            for x in (-.73,0,.73):ball(c,'Raised dough ball',(x,-.1,1.12),.22,F)
        elif 'sorting' in k:
            box(c,'Postal sorting pigeonhole wall',(0,.34,1.65),(2.25,.35,1.35),A,.03)
            for x in (-.72,0,.72):box(c,'Open mail slot',(x,-.02,1.66),(.52,.05,.28),D,.01)
        else:
            for x in (-1.0,1.0):beam(c,'Mechanic bench tool gantry',(x,.30,.9),(x,.30,2.25),.07,M)
            box(c,'Mechanic overhead tool rail',(0,.30,2.24),(2.15,.11,.14),D,.01)
            for x in (-.60,0,.60):beam(c,'Suspended mechanic tool',(x,.30,2.2),(x,.30,1.52),.045,A)
    elif kind=='table':
        if 'cafe' in k:
            cylinder(c,'Round cafe bistro surface',(0,0,.98),.98,.11,A,vertices=16)
            cylinder(c,'Cafe table flower vase',(0,0,1.12),.11,.18,G,vertices=10)
        elif 'wrapping' in k:
            for x in (-.65,.65):beam(c,'Wrapping roll stand',(x,.23,.95),(x,.23,1.48),.065,M)
            cylinder(c,'Suspended paper roll',(0,.23,1.47),.22,1.5,T,vertices=12,rot=(0,math.pi/2,0))
        elif 'clinic' in k:
            box(c,'Doctor desk L-return',(.78,.35,.90),(.72,1.45,.14),T,.035)
            box(c,'Patient record upright',(-.38,.25,1.25),(.65,.08,.66),G,.025)
        elif 'reading' in k:
            box(c,'Library wide slanted book rest',(0,.37,1.24),(1.75,.13,.65),W,.03)
            for x in (-.37,.37):box(c,'Standing reference book',(x,.15,1.13),(.25,.39,.44),A,.016)
        elif 'star chart' in k:
            cylinder(c,'Round astronomy chart tabletop',(0,0,.98),1.07,.13,D,vertices=16)
            torus(c,'Inlaid orbital chart ring',(0,0,1.06),.83,.055,T)
            ball(c,'Planet marker',(0,0,1.20),.15,L)
        else:
            box(c,'Artist palette overhang',(.56,-.05,.99),(1.05,1.21,.10),A,.055)
            for x in (-.45,0,.45):cylinder(c,'Paint pigment pot',(x,.20,1.16),.14,.23,T,vertices=10)
    elif kind=='cabinet':
        if 'seed' in k:
            roof_gable(c,'Seed cabinet potting roof',2.42,1.10,2.08,2.72,A)
            for x in (-.60,0,.60):box(c,'Seedling germination tray',(x,-.52,1.37),(.43,.30,.23),G,.02)
        elif 'mail' in k:
            box(c,'Postal pigeonhole headbox',(0,-.08,2.38),(2.23,.95,.63),A,.05)
            for x in (-.6,0,.6):box(c,'Tall letter drop slot',(x,-.58,2.4),(.4,.06,.12),D,.01)
        elif 'medicine' in k:
            box(c,'Medicine cabinet cross mast',(0,-.40,2.53),(.17,.06,.92),A,.012)
            box(c,'Medicine cabinet cross arm',(0,-.41,2.53),(.81,.06,.17),A,.012)
        elif 'bookcase' in k:
            roof_hip(c,'Library bookcase carved crown',2.34,1.02,2.12,2.92,.25,D)
            for x in (-.65,-.2,.25,.7):box(c,'Library bound books',(x,-.39,1.75),(.27,.40,.52),A,.02)
        else:
            cylinder(c,'Lens display optical tube',(0,-.46,2.46),.25,1.35,M,vertices=12,rot=(0,math.pi/2,0))
            torus(c,'Lens cabinet objective rim',(.72,-.46,2.46),.25,.05,G,rot=(0,math.pi/2,0))
    elif kind=='lamp':
        if 'treatment' in k:
            beam(c,'Clinic articulated light arm',(0,0,1.9),(.86,-.10,2.38),.075,M)
            cylinder(c,'Clinic downward examination reflector',(.86,-.10,2.34),.43,.23,T,vertices=12)
        elif 'reading' in k:
            beam(c,'Curved library reading arm',(0,0,1.9),(-.75,-.05,2.58),.07,W)
            box(c,'Library fabric shade',(-.75,-.05,2.43),(.67,.55,.32),A,.05)
        elif 'spotlight' in k:
            cylinder(c,'Wide stage spotlight barrel',(0,-.36,2.15),.42,.74,D,vertices=12,rot=(math.pi/2,0,0))
            torus(c,'Theatre reflector rim',(0,-.76,2.15),.43,.06,A,rot=(math.pi/2,0,0))
        elif 'beacon' in k:
            for x in (-.8,.8):beam(c,'Astronomy beacon outer tripod',(0,0,1.7),(x,-.28,.12),.055,M)
            ball(c,'Astronomy beacon signal orb',(0,0,2.91),.28,G)
        else:
            beam(c,'Harbor crook lantern bracket',(0,0,1.9),(.64,0,2.78),.075,W)
            box(c,'Hanging dock lantern',(.64,0,2.40),(.46,.45,.62),G,.035)
    elif kind=='rack':
        if 'tool' in k:
            box(c,'Workshop rack pegboard',(0,-.46,1.73),(2.1,.13,.86),D,.025)
            for x in (-.7,0,.7):beam(c,'Rack hand tool',(x,-.58,2.03),(x,-.58,1.41),.06,M)
        elif 'rolling' in k:
            for z in (.45,1.12,1.78):
                for x in (-.53,.53):ball(c,'Bakery rack bread roll',(x,-.22,z+.18),.19,F)
            box(c,'Baker rack arched top',(0,0,2.33),(2.0,.9,.13),A,.04)
        elif 'tire' in k:
            for x in (-.56,.56):
                for z in (.66,1.55):torus(c,'Garage mounted tire',(x,-.50,z),.39,.15,D,rot=(math.pi/2,0,0))
        else:
            for x in (-.70,-.35,0,.35,.70):
                beam(c,'Tall artist brush',(x,-.48,1.48),(x,-.48,2.69-abs(x)*.35),.045,W)
            box(c,'Brush rack paint-splash header',(0,0,2.22),(2.0,.9,.14),A,.04)
    elif kind=='machine':
        if 'checkout' in k:
            box(c,'Market register keypad hood',(0,-.64,1.61),(1.34,.42,.52),D,.05)
            box(c,'Till receipt roll',(.36,-.32,2.02),(.45,.38,.60),T,.035)
        elif 'espresso' in k:
            for x in (-.43,.43):cylinder(c,'Espresso group head',(x,-.67,1.50),.19,.48,M,vertices=12,rot=(math.pi/2,0,0))
            cylinder(c,'Coffee boiler vessel',(0,.10,2.02),.49,.46,D,vertices=12)
        else:
            box(c,'Wide workshop console monitor',(0,-.41,1.84),(1.9,.16,.78),G,.04)
            for x in (-.85,.85):beam(c,'Console overhead sensor',(x,.11,1.3),(x,.11,2.57),.055,M)
    elif kind=='scale':
        if 'fruit' in k:
            for x in (-.55,.55):cylinder(c,'Market produce weighing basket',(x,0,1.39),.42,.29,W,vertices=12)
            box(c,'Market scale arched price flag',(0,0,2.30),(.8,.12,.50),A,.04)
        elif 'letter' in k:
            box(c,'Postal letter scale flat sorting tray',(0,-.38,1.41),(1.72,.80,.12),T,.03)
            box(c,'Postal price dial',(0,-.02,2.23),(.59,.13,.59),G,.025)
        else:
            box(c,'Industrial recycling hopper',(0,-.22,2.16),(1.74,1.0,.87),A,.07)
            box(c,'Hopper inclined feed lip',(0,-.79,2.43),(1.86,.24,.18),D,.02)
    elif kind=='crate':
        if 'toolbox' in k:
            box(c,'Raised steel toolbox lid',(0,.05,.69),(1.35,.79,.13),M,.035)
            beam(c,'Long toolbox carry handle',(0,-.02,.71),(0,-.02,1.12),.06,T)
        elif 'fish' in k:
            for x in (-.38,.38):ball(c,'Fresh fish-market contents',(x,0,.75),.32,G)
            box(c,'Tall harbor ice cage',(0,.38,.83),(1.24,.10,.9),A,.025)
        else:
            for x in (-.39,0,.39):cylinder(c,'Bottles in recycling crate',(x,0,.69),.13,.77,G,vertices=10)
            box(c,'Reinforced glass crate side rail',(.63,0,.73),(.10,.83,1.05),D,.025)
    elif kind=='barrel':
        if 'compost' in k:
            box(c,'Compost vessel peaked lid',(0,0,1.14),(.91,.90,.22),W,.055)
            beam(c,'Compost turning lever',(.39,0,.76),(.94,0,.76),.075,M)
            box(c,'Tall aeration intake housing',(0,.17,1.49),(.64,.55,.54),G,.06)
            cylinder(c,'Compost hand crank wheel',(.95,0,.76),.23,.08,D,
                     vertices=10,rot=(0,math.pi/2,0))
        elif 'flour' in k:
            box(c,'Flour barrel overflowing sack',(0,0,1.32),(.78,.78,.67),F,.12)
            box(c,'Bakery flour scoop',(.44,-.23,1.56),(.59,.16,.08),W,.02)
        else:
            torus(c,'Harbor rope coil',(0,0,1.19),.67,.13,W)
            box(c,'Harbor barrel side spigot',(.61,0,.63),(.48,.15,.14),M,.02)
    elif kind=='podium':
        if 'story' in k:
            box(c,'Library open lectern book',(0,-.21,1.84),(1.35,.76,.13),F,.03)
            roof_gable(c,'Story podium reading canopy',1.35,.82,1.95,2.18,A)
        elif 'observation' in k:
            torus(c,'Observatory instrument sighting hoop',(0,-.17,2.19),.56,.07,G,rot=(math.pi/2,0,0))
            beam(c,'Sighting mast',(0,0,1.8),(0,0,2.6),.055,M)
        else:
            box(c,'Sculpture plinth broad monolith',(0,0,1.4),(1.30,1.14,1.75),T,.07)
            ball(c,'Displayed abstract art sculpture',(.23,0,2.66),.38,A)
    elif kind=='display':
        if 'produce' in k:
            for x in (-.65,0,.65):box(c,'Tilted fresh produce crate',(x,-.22,1.38),(.51,.68,.52),A,.04)
            dome(c,'Market fruit-carousel festive canopy',(0,.12,2.32),1.21,T)
        else:
            for x in (-.55,.55):dome(c,'Pastry glass display bell',(x,-.29,1.60),.49,G)
            box(c,'Bakery pastry counter front card',(0,-.49,1.34),(1.35,.07,.40),A,.02)
    elif kind=='basket':
        if 'wicker' in k:
            for x in (-.32,0,.32):ball(c,'Basket of market apples',(x,0,.69),.21,p['apple'])
            torus(c,'Wide wicker basket collar',(0,0,.58),.67,.08,W)
        else:
            for x in (-.32,0,.32):
                cylinder(c,'Upright baguette in bakery basket',(x,0,.88),.12,1.18,F,vertices=10)
            box(c,'Baguette basket flour cloth',(0,-.10,.43),(1.20,.78,.11),T,.02)
    elif kind=='tray':
        if 'serving' in k:
            for x in (-.32,.32):cylinder(c,'Cafe tray tall takeaway cup',(x,-.06,.65),.16,.75,T,vertices=10)
            box(c,'Cafe tray carried menu',(0,.22,.43),(.89,.39,.05),A,.01)
        else:
            beam(c,'Medical tray forceps',(-.40,-.12,.62),(.40,.18,.62),.045,M)
            cylinder(c,'Medical tray instrument flask',(.35,-.12,.68),.15,.65,G,vertices=10)
    elif kind=='pump':
        if 'water' in k:
            beam(c,'Garden pump outflow pipe',(.36,0,1.11),(.97,-.24,1.38),.10,M)
            box(c,'Garden hand pump T lever',(0,0,2.35),(1.55,.12,.12),W,.02)
        else:
            box(c,'Garage fuel delivery tower',(0,.16,1.77),(.83,.75,1.48),D,.06)
            beam(c,'Fuel hose arm',(.30,-.35,1.88),(1.07,-.35,1.32),.065,M)
    elif kind=='ring':
        if 'target' in k:
            torus(c,'Raised pool bullseye hoop',(0,-.08,2.43),.52,.12,A,rot=(math.pi/2,0,0))
            for x in (-.56,.56):beam(c,'Pool target side stand',(x,0,.15),(x,0,2.38),.06,W)
        else:
            torus(c,'Harbor life-saving buoy',(0,-.28,1.45),.75,.18,T,rot=(math.pi/2,0,0))
            beam(c,'Buoy rope hanging post',(-.78,0,.2),(-.78,0,2.44),.07,W)
    elif kind=='press':
        if 'stamp' in k:
            beam(c,'Postal hand stamp lever',(0,0,1.88),(.72,-.15,2.35),.09,W)
            box(c,'Document press platen',(0,-.38,.90),(1.21,.48,.15),T,.03)
        else:
            box(c,'Bottle crusher high intake hopper',(0,.1,2.08),(1.68,.98,.76),D,.07)
            box(c,'Bottle crusher outgoing chute',(.81,-.30,.55),(.78,.62,.44),M,.03)
    elif kind=='grinder':
        cylinder(c,'Conical coffee bean hopper',(0,0,2.18),.55,.72,G,vertices=12)
        box(c,'Grinder side dosing lever',(.62,-.15,1.31),(.74,.14,.12),W,.025)
        cylinder(c,'Ground coffee catch cup',(0,-.65,.74),.22,.43,T,vertices=12)
    elif kind=='mixer':
        cylinder(c,'Open bakery mixing bowl',(0,-.35,1.51),.65,.55,M,vertices=14)
        box(c,'Heavy dough kneading arm',(0,-.07,2.02),(.35,1.0,.18),D,.055)
        cylinder(c,'Mixer drive motor',(.46,.24,1.74),.34,.68,A,vertices=12)
    elif kind=='tumbler':
        cylinder(c,'Horizontal compost tumbling drum',(0,-.06,1.67),.64,1.48,A,
                 vertices=14,rot=(0,math.pi/2,0))
        for x in (-.67,.67):torus(c,'Tumbler end bearing',(x,-.06,1.67),.65,.08,M,
                                   rot=(0,math.pi/2,0))
        beam(c,'Compost tumbler loading handle',(.78,-.08,1.68),(1.21,-.08,1.68),.08,W)
    elif kind=='gear':
        for i in range(12):
            angle=i*2*math.pi/12
            box(c,'Machined gear tooth',(.54*math.cos(angle),-.03,1.52+.54*math.sin(angle)),
                (.22,.18,.15),M,.012)
        cylinder(c,'Large exposed central axle',(0,-.16,1.52),.20,.38,D,
                 vertices=12,rot=(math.pi/2,0,0))
    elif kind=='wheel':
        cylinder(c,'Broad pottery throwing disk',(0,0,1.46),.92,.16,T,vertices=16)
        cylinder(c,'Unfinished hand-thrown vase',(0,0,1.68),.31,.52,A,vertices=12)
        box(c,'Pottery wheel foot pedal',(.73,-.35,.22),(.56,.31,.09),W,.018)
    elif kind=='orrery':
        for radius,height in ((.73,1.76),(.98,1.97)):
            torus(c,'Inclined orrery orbital ring',(0,0,height),radius,.035,M)
        ball(c,'Orrery central sun',(0,0,1.78),.31,L)
    elif kind=='lift':
        for x in (-.82,.82):
            box(c,'Twin-ram garage lift column',(x,.25,1.82),(.27,.31,3.35),M,.03)
            box(c,'Raised vehicle support fork',(x,-.35,1.43),(.78,1.20,.16),A,.03)
        box(c,'Vehicle lift overhead crossbar',(0,.25,3.45),(1.9,.32,.22),D,.02)
    elif kind=='crane':
        for i in range(5):
            x=i*.52-.48
            beam(c,'Harbor lattice crane diagonal',(x,.12,1.54+i*.24),
                 (x+.51,.12,1.79+i*.24),.055,M)
        beam(c,'Harbor long cargo boom',(-.72,.12,2.34),(2.32,.12,3.73),.12,A)
        beam(c,'Cargo crane hanging cable',(2.32,.12,3.73),(2.32,.12,1.80),.035,D)
        box(c,'Crane broad dock cargo hook',(2.32,.12,1.76),(.32,.28,.23),T,.025)
    elif kind=='easel':
        box(c,'Painter portrait canvas outer frame',(0,-.23,1.39),(1.48,.15,1.66),W,.04)
        box(c,'Asymmetric paint study',(0,-.33,1.39),(1.20,.035,1.38),A,.01)
        box(c,'Easel side palette shelf',(.87,-.19,.86),(.63,.42,.09),M,.025)
    elif kind=='canvas':
        box(c,'Landscape art canvas outer frame',(0,-.23,1.37),(2.26,.16,.93),W,.04)
        box(c,'Abstract horizontal artwork',(0,-.33,1.37),(1.99,.035,.69),A,.01)
        for x in (-.93,.93):box(c,'Artist frame carved corner',(x,-.34,1.37),(.14,.05,.93),T,.02)
    elif kind=='oven':
        roof_gable(c,'Bakery oven refractory cap',1.75,1.25,1.52,2.43,W)
        box(c,'Wood-fired oven hearth projection',(0,-.77,.76),(1.24,.57,.22),T,.04)
        cylinder(c,'Bakery oven exhaust flue',(.43,.26,2.52),.19,1.55,D,vertices=12)
    elif kind=='engine':
        for x in (-.43,0,.43):
            cylinder(c,'Exposed engine cylinder',(x,0,1.64),.25,.80,M,vertices=10)
            for z in (1.40,1.62,1.84):
                torus(c,'Engine cooling-fin ring',(x,0,z),.27,.025,D)
        box(c,'Engine block offset exhaust manifold',(.66,.25,1.41),(.40,1.10,.20),A,.03)
    elif kind=='monitor':
        box(c,'Clinic large upright vital display',(0,-.54,1.92),(1.35,.17,1.08),G,.04)
        box(c,'Vital monitor side equipment shelf',(.81,-.15,.88),(.55,.80,.13),T,.02)
        cylinder(c,'Medical alert beacon',(0,.06,2.58),.13,.31,A,vertices=12)
    elif kind=='screen':
        box(c,'Pool changing bay wide curtain rail',(0,-.17,2.26),(2.44,.18,.14),W,.025)
        for x in (-.75,0,.75):
            box(c,'Hanging pool changing curtain',(x,-.30,1.33),(.67,.05,1.61),G,.02)
    elif kind=='stand':
        box(c,'Concert music manuscript tray',(0,-.22,2.01),(1.50,.16,.88),D,.04)
        box(c,'Sheet music ledge',(0,-.42,1.55),(1.60,.25,.12),T,.02)
    elif kind=='bollard':
        cylinder(c,'Short heavy mooring cap',(0,0,1.10),.54,.37,D,vertices=12)
        for x in (-.45,.45):
            beam(c,'Harbor mooring crosshorn',(x,0,1.18),(x*1.7,0,1.18),.11,M)

def seed_packet(c,p):
    """Small XRI-ready greenhouse seed packet with a raised sprout motif."""
    A,W,D,G,T=p['accent'],p['wood'],p['dark'],p['glass'],p['trim']
    box(c,'Folded paper seed envelope',(0,0,.265),(.34,.045,.45),T,.009)
    box(c,'Seed packet colored label',(0,-.027,.27),(.28,.008,.30),A,.003)
    for x in (-.115,0,.115):
        box(c,'Crimped packet top seam',(x,0,.497),(.025,.047,.014),W,.002)
    beam(c,'Raised sprout stalk',(0,-.034,.20),(0,-.034,.36),.010,D)
    for side in (-1,1):
        beam(c,'Raised seedling leaf',(0,-.038,.31),(side*.075,-.038,.36),.012,G)

def export_asset(coll,path):
    bpy.ops.object.select_all(action='DESELECT')
    for o in coll.objects:
        if o.type=='MESH':o.select_set(True)
    active=next((o for o in coll.objects if o.type=='MESH'),None)
    if active is None:raise RuntimeError(f'No meshes in {coll.name}')
    bpy.context.view_layer.objects.active=active
    source_parts=sum(o.type=='MESH' for o in coll.objects)
    # Keep the source project fully editable, but use one static renderer per
    # imported FBX. Joining retains per-face UVs and material assignments.
    bpy.ops.object.duplicate()
    bpy.ops.object.join()
    joined=bpy.context.object
    joined.name=coll.name+' optimized export mesh'
    bpy.context.scene.cursor.location=(0,0,0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    metrics={
        'source_mesh_parts':source_parts,
        'export_meshes':1,
        'material_slots':len(joined.data.materials),
        'triangles':sum(max(0,len(poly.vertices)-2) for poly in joined.data.polygons),
    }
    bpy.ops.export_scene.fbx(filepath=path,use_selection=True,
        apply_unit_scale=True,axis_forward='-Z',axis_up='Y',
        add_leaf_bones=False,path_mode='AUTO')
    bpy.data.objects.remove(joined,do_unlink=True)
    bpy.ops.object.select_all(action='DESELECT')
    return metrics

def face_unity_negative_z(coll):
    # Blender FBX import into Unity maps our authored -Y frontage to +Z.
    # Rotate authored geometry 180 degrees before saving/exporting so the actual
    # imported frontage and front-facing props land at Unity local -Z.
    turn=Matrix.Rotation(math.pi,4,'Z')
    for obj in coll.objects:
        obj.matrix_world=turn @ obj.matrix_world

def scale_prop_geometry(coll,factors):
    """Bake per-prop authored proportions into editable source mesh geometry."""
    if all(abs(value-1)<1e-8 for value in factors):return
    stretch=Matrix.Diagonal((*factors,1.0))
    for obj in coll.objects:
        if obj.type!='MESH':continue
        if obj.data.users>1:obj.data=obj.data.copy()
        obj.data.transform(obj.matrix_world.inverted() @ stretch @ obj.matrix_world)
        obj.data.update()

ONLY_VENUE=os.environ.get('VILLAGE_ONLY','').strip()
if ONLY_VENUE and ONLY_VENUE not in {v[0] for v in VENUES}:
    raise ValueError('Unknown VILLAGE_ONLY venue: '+ONLY_VENUE)
manifest_path=os.path.join(BASE,'asset_manifest.json')
if ONLY_VENUE:
    with open(manifest_path,encoding='utf-8') as source:
        manifest=json.load(source)
else:
    manifest={'provenance':'AI-assisted procedural Blender reference models; declare in coursework; zero self-modeled rubric credit.',
              'blender_version':bpy.app.version_string,'coordinate_system':'Blender Z-up; FBX Unity Y-up; venue front faces local -Z in Unity.',
              'venues':{}}

# Seven display stations hug the perimeter, leaving a broad center aisle for
# teleport, joystick locomotion, and the interactive gameplay stations.
PROP_STATIONS=[
    ((-4.35,.10,2.75),-90),((-4.35,.10,.10),-90),((-4.35,.10,-2.55),-90),
    ((4.35,.10,2.75),90),((4.35,.10,.10),90),((4.35,.10,-2.55),90),
    ((0,.10,3.45),0),
]
with open(os.path.join(BASE,'prop_variant_scales.json'),encoding='utf-8') as scale_file:
    PROP_VARIANT_SCALES=json.load(scale_file)
ACTION_VISUALS={
    ('market','wicker_basket'):('Basket',.92),
    ('market','checkout_register'):('Register',.78),
    ('market','bread_loaf'):('Grab bread',.42),
    ('market','red_apple'):('Grab apple',.55),
    ('cafe','espresso_machine'):('Coffee machine',.82),
    ('cafe','serving_cup'):('Grab coffee cup',.52),
    ('pool','toss_ring'):('Grab and toss ring',1.0),
}

if not ONLY_VENUE:
    ensure_textures()
for venue_index,(vid,label,color,items) in enumerate(VENUES):
    if ONLY_VENUE and vid!=ONLY_VENUE:
        continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    MATS.clear()
    p=palette(color,vid)
    folder=os.path.join(EXPORT,vid);os.makedirs(folder,exist_ok=True)
    records=[]
    exterior=link_collection('00 Exterior - '+label)
    building(exterior,vid,label,p,venue_index)
    face_unity_negative_z(exterior)
    name=f'{vid}_exterior'
    path=os.path.join(folder,name+'.fbx')
    metrics=export_asset(exterior,path)
    records.append({'name':name,'role':'exterior','fbx':os.path.relpath(path,BASE).replace(os.sep,'/'),
                    'suggested_local_position':[0,0,0],'suggested_yaw_degrees':0,'suggested_scale':1.0,
                    'preferred_action_object':'',**metrics})
    for i,(pretty,kind) in enumerate(items,1):
        coll=link_collection(f'{i:02d} {pretty}')
        prop(coll,pretty,kind,p,venue_index*41+i)
        name=f'{vid}_{pretty.lower().replace(" ","_")}'
        scale_prop_geometry(coll,PROP_VARIANT_SCALES.get(name,(1,1,1)))
        face_unity_negative_z(coll)
        path=os.path.join(folder,name+'.fbx')
        metrics=export_asset(coll,path)
        pos,yaw=PROP_STATIONS[i-1]
        action,visual_scale=ACTION_VISUALS.get((vid,name[len(vid)+1:]),('',1.0))
        records.append({'name':name,'role':kind,'fbx':os.path.relpath(path,BASE).replace(os.sep,'/'),
                        'suggested_local_position':list(pos),'suggested_yaw_degrees':yaw,
                        'suggested_scale':1.0,'preferred_action_object':action,
                        'action_visual_scale':visual_scale,**metrics})
    extras=[]
    if vid=='greenhouse':
        coll=link_collection('08 Seed packet gameplay pickup')
        seed_packet(coll,p)
        face_unity_negative_z(coll)
        extra_path=os.path.join(folder,'greenhouse_seed_packet.fbx')
        metrics=export_asset(coll,extra_path)
        extras.append({'name':'greenhouse_seed_packet','role':'seed_packet',
                       'fbx':os.path.relpath(extra_path,BASE).replace(os.sep,'/'),**metrics})
    blend_path=os.path.join(SOURCE,vid+'.blend')
    bpy.ops.wm.save_as_mainfile(filepath=blend_path,compress=True)
    manifest['venues'][vid]={'label':label,'blend':os.path.relpath(blend_path,BASE).replace(os.sep,'/'),'accent_rgb':color,'assets':records,'extras':extras}
    print(f'FINISHED {vid}: {len(records)} FBX; {sum(r["source_mesh_parts"] for r in records)} modeled source parts')

with open(manifest_path,'w',encoding='utf-8') as f:
    json.dump(manifest,f,indent=2)
print('FINISHED',ONLY_VENUE or 'ALL',len(manifest['venues']),'VENUES',sum(len(v['assets']) for v in manifest['venues'].values()),'FBX')
