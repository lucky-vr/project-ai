"""Editable Blender-only village environment kit.

Run: blender --background --python Assets/VillageDraft/Models/EnvironmentFestival/generate_festival_environment.py
Coordinates in this recipe are Unity village (x,z,height). The mesh writer maps them
to Blender (x,-z,height), matching the project's FBX/Unity placement convention.
AI-assisted reference art: disclose as AI-generated in coursework.
"""
import bpy, math, os, json, random
from collections import defaultdict
from mathutils import Matrix, Vector

ROOT=os.path.dirname(os.path.abspath(__file__))
FBX=os.path.join(ROOT,'FBX')
SOURCE=os.path.join(ROOT,'Source')
os.makedirs(FBX,exist_ok=True);os.makedirs(SOURCE,exist_ok=True)
TEXTURES=os.path.join(os.path.dirname(ROOT),'Textures','PolyHaven')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.unit_settings.system='METRIC'

PALETTE={
 'grass':((.47,.60,.37),'leafy_grass'),
 'meadow':((.58,.66,.43),'leafy_grass'),
 'drygrass':((.70,.63,.40),'leafy_grass'),
 'sand':((.77,.68,.51),'sand_02'),
 'gravel':((.55,.55,.49),'gravel_ground_01'),
 'stone':((.72,.69,.60),'cobblestone_floor_04'),
 'terracotta':((.57,.34,.27),'cobblestone_floor_04'),
 'bluestone':((.38,.51,.55),'cobblestone_floor_04'),
 'plank':((.48,.32,.21),'brown_planks_07'),
 'timber':((.40,.25,.16),'brown_planks_07'),
 'ivory':((.86,.82,.68),None),
 'bronze':((.66,.47,.22),None),
 'metal':((.22,.34,.40),None),
 'coral':((.79,.24,.25),None),
 'gold':((.98,.70,.24),None),
 'teal':((.11,.55,.57),None),
 'navy':((.13,.24,.39),None),
 'leaf':((.24,.49,.29),None),
 'flower':((.83,.31,.43),None),
 'water':((.10,.43,.54),None),
 'ridge_sage':((.48,.62,.48),'leafy_grass'),
 'ridge_ochre':((.66,.67,.49),'leafy_grass'),
 'ridge_leaf':((.32,.48,.35),None),
 'ridge_pine':((.22,.39,.35),None),
 'ridge_bark':((.32,.30,.26),None),
}
MATERIALS={}
for name,(color,slug) in PALETTE.items():
 m=bpy.data.materials.new(name)
 m.diffuse_color=(*color,1)
 m.use_nodes=True
 bsdf=m.node_tree.nodes.get('Principled BSDF')
 bsdf.inputs['Base Color'].default_value=(*color,1)
 bsdf.inputs['Roughness'].default_value=.83 if slug else .64
 if slug:
  p=os.path.join(TEXTURES,slug+'_diff_1k.jpg')
  if os.path.exists(p):
   tex=m.node_tree.nodes.new('ShaderNodeTexImage')
   tex.image=bpy.data.images.load(p,check_existing=True)
   m.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
 MATERIALS[name]=m

def V(x,z,h): return (float(x),float(-z),float(h))

class MeshBuilder:
 def __init__(self,name):
  self.name=name;self.verts=[];self.faces=[];self.mats=[];self.uvs=[]
 def face(self,points,mat,uv=None):
  start=len(self.verts)
  self.verts.extend(points)
  self.faces.append(tuple(range(start,start+len(points))))
  self.mats.append(mat)
  self.uvs.append(uv or [(p[0]/3,p[1]/3) for p in points])
 def box(self,x,z,h,w,d,height,mat,yaw=0):
  ca=math.cos(yaw);sa=math.sin(yaw)
  def p(dx,dz,hh):return V(x+dx*ca-dz*sa,z+dx*sa+dz*ca,hh)
  corners=[(-w/2,-d/2),(-w/2,d/2),(w/2,d/2),(w/2,-d/2)]
  bottom=[p(a,b,h-height/2) for a,b in corners]
  top=[p(a,b,h+height/2) for a,b in corners]
  self.face(top,mat)
  self.face(list(reversed(bottom)),mat)
  for i in range(4):
   j=(i+1)%4
   self.face([bottom[i],bottom[j],top[j],top[i]],mat)
 def path(self,a,b,width,mat,h=.042):
  dx=b[0]-a[0];dz=b[1]-a[1];length=math.hypot(dx,dz)
  if length<.08:return
  rx=-dz/length*width/2;rz=dx/length*width/2
  points=[V(a[0]+rx,a[1]+rz,h),V(b[0]+rx,b[1]+rz,h),
          V(b[0]-rx,b[1]-rz,h),V(a[0]-rx,a[1]-rz,h)]
  self.face(points,mat,[(0,0),(0,length/3),(width/3,length/3),(width/3,0)])
 def disc(self,x,z,h,rx,rz,mat,sides=32,noise=0,seed=0):
  r=random.Random(seed)
  radii=[1+noise*(r.random()-.5)*2 for _ in range(sides)]
  c=V(x,z,h)
  for i in range(sides):
   a=i*2*math.pi/sides;b=(i+1)*2*math.pi/sides
   p=V(x+math.sin(a)*rx*radii[i],z+math.cos(a)*rz*radii[i],h)
   q=V(x+math.sin(b)*rx*radii[(i+1)%sides],
       z+math.cos(b)*rz*radii[(i+1)%sides],h)
   self.face([c,p,q],mat,[(x/3,z/3),(p[0]/3,-p[1]/3),(q[0]/3,-q[1]/3)])
 def cylinder(self,x,z,h,r,height,mat,sides=12):
  top=h+height/2;bottom=h-height/2
  ring=[(x+math.sin(i*2*math.pi/sides)*r,z+math.cos(i*2*math.pi/sides)*r)
        for i in range(sides)]
  for i in range(sides):
   j=(i+1)%sides
   a,b=ring[i],ring[j]
   self.face([V(x,z,top),V(*a,top),V(*b,top)],mat)
   self.face([V(x,z,bottom),V(*b,bottom),V(*a,bottom)],mat)
   self.face([V(*a,bottom),V(*b,bottom),V(*b,top),V(*a,top)],mat)
 def cone(self,x,z,base,rx,rz,height,mat,sides=16):
  for i in range(sides):
   a=i*2*math.pi/sides;b=(i+1)*2*math.pi/sides
   p=V(x+math.sin(a)*rx,z+math.cos(a)*rz,base)
   q=V(x+math.sin(b)*rx,z+math.cos(b)*rz,base)
   tip=V(x,z,base+height)
   self.face([p,q,tip],mat)
 def sphere(self,x,z,h,rx,rz,ry,mat,rings=5,sides=10):
  # A small faceted lantern/berry/ornament, not a default Unity primitive.
  for j in range(rings):
   ta=-math.pi/2+j*math.pi/rings;tb=-math.pi/2+(j+1)*math.pi/rings
   for i in range(sides):
    aa=i*2*math.pi/sides;ab=(i+1)*2*math.pi/sides
    def p(t,a):return V(x+math.cos(t)*math.sin(a)*rx,
                         z+math.cos(t)*math.cos(a)*rz,h+math.sin(t)*ry)
    self.face([p(ta,aa),p(tb,aa),p(tb,ab),p(ta,ab)],mat)
 def object(self,collection):
  mesh=bpy.data.meshes.new(self.name)
  mesh.from_pydata(self.verts,[],self.faces);mesh.update()
  uv=mesh.uv_layers.new(name='World UV')
  used=list(dict.fromkeys(self.mats))
  for key in used:mesh.materials.append(MATERIALS[key])
  index={key:i for i,key in enumerate(used)}
  for poly,key,faceuv in zip(mesh.polygons,self.mats,self.uvs):
   poly.material_index=index[key]
   for loop_idx,xy in zip(poly.loop_indices,faceuv):uv.data[loop_idx].uv=xy
  obj=bpy.data.objects.new(self.name,mesh)
  collection.objects.link(obj)
  return obj

def collection(name):
 c=bpy.data.collections.new(name);bpy.context.scene.collection.children.link(c);return c
COL={name:collection(name) for name in
     ['base_ground','district_ground','paths','civic','garden_arts','waterfront','water','maker_sky','perimeter','market_wayfinding']}

def mesh_text(builder,body,x,z,h,size,mat):
 """Bake native Blender font geometry into the single export mesh."""
 curve=bpy.data.curves.new(body+' lettering','FONT')
 curve.body=body;curve.size=size;curve.align_x='CENTER';curve.align_y='CENTER'
 curve.extrude=.007;curve.bevel_depth=0
 obj=bpy.data.objects.new(body+' raised letters',curve)
 bpy.context.scene.collection.objects.link(obj)
 # Match the venue asset generator's FONT rotation. Its later 180-degree FBX
 # source turn and Unity PlaceModel basis make this read left-to-right in game.
 obj.matrix_world=(Matrix.Translation(Vector(V(x,z,h))) @
                   Matrix.Rotation(math.pi/2,4,'X'))
 bpy.ops.object.select_all(action='DESELECT')
 obj.select_set(True);bpy.context.view_layer.objects.active=obj
 bpy.ops.object.convert(target='MESH')
 obj=bpy.context.object
 for poly in obj.data.polygons:
  builder.face([tuple(obj.matrix_world@obj.data.vertices[i].co) for i in poly.vertices],mat)
 bpy.data.objects.remove(obj,do_unlink=True)

def base_height(x,z):
 outward=max(abs(x),abs(z))-108
 hill=max(0,min(1,outward/80))
 return -.08+hill*hill*(4.5+2*math.sin(x*.041+z*.024)**2)

# The sole visible base ground: flat where walking/teleport is supported, gently
# rolling only outside the 220m template collision plane.
g=MeshBuilder('Festival village ground and distant meadow')
N=56;extent=250
grid=[]
for row in range(N+1):
 z=-extent+2*extent*row/N;line=[]
 for col in range(N+1):
  x=-extent+2*extent*col/N
  line.append((x,z,base_height(x,z)))
 grid.append(line)
for row in range(N):
 for col in range(N):
  a=grid[row][col];b=grid[row+1][col]
  c=grid[row+1][col+1];d=grid[row][col+1]
  pts=[V(*a),V(*b),V(*c),V(*d)]
  g.face(pts,'grass',[(a[0]/2.7,a[1]/2.7),(b[0]/2.7,b[1]/2.7),
                      (c[0]/2.7,c[1]/2.7),(d[0]/2.7,d[1]/2.7)])
g.object(COL['base_ground'])

zones=MeshBuilder('Irregular festival district ground')
for label,x,z,rx,rz,material,seed in [
 ('oldtown',-43,24,37,31,'drygrass',1),('civic',3,37,42,31,'meadow',2),
 ('garden',-35,-20,36,31,'meadow',3),('arts',-34,-58,38,28,'drygrass',4),
 ('coast',40,-55,39,30,'sand',5),('maker',69,-3,33,36,'gravel',6),
 ('sky',65,62,33,31,'drygrass',7),('arrival',0,-17,34,27,'meadow',8)]:
 zones.disc(x,z,.012+seed*.002,rx,rz,material,24,.09,seed)
zones.object(COL['district_ground'])

routes=MeshBuilder('Village festival paths and civic paving')
ROUTES=[
 ('stone',4.3,[(0,-31),(-2,-20),(0,-11)]),
 # The civic square is a walkable ring around the stage and clock tower.
 ('stone',3.3,[(0,10),(-7,12),(-7,20),(-6,25),(-6,34),(0,39)]),
 ('stone',3.3,[(0,10),(7,12),(7,20),(6,25),(6,34),(0,39)]),
 # Branch around the fountain basin so the visual route remains genuinely walkable.
 ('stone',2.5,[(0,-11),(-5,-6),(-5,5),(0,10)]),
 ('stone',2.5,[(0,-11),(5,-6),(5,5),(0,10)]),
 ('stone',3.8,[(0,39),(-11,43),(-17,45),(1,42),(16,43)]),
 ('terracotta',3.7,[(-8,7),(-18,12),(-32,25),(-45,28),(-50,31)]),
 ('terracotta',3.2,[(-18,12),(-24,-2),(-33,-12),(-43,-18)]),
 ('sand',3.4,[(-8,-8),(-19,-24),(-30,-35),(-39,-43),(-43,-49)]),
 ('sand',3.1,[(-30,-35),(-20,-42),(-12,-47),(-20,-66),(-39,-67),(-43,-49)]),
 ('bluestone',3.8,[(8,-7),(20,-17),(24,-29),(37,-38),(44,-45),(48,-49)]),
 ('bluestone',3.3,[(8,8),(24,20),(40,27),(52,42),(61,55),(64,58)]),
 ('bluestone',3.1,[(24,20),(41,5),(54,-7),(60,-12),(44,-45)]),
 ('plank',3.8,[(20,-55),(21,-65),(36,-65),(53,-65),(62,-62)]),
 ('sand',3.0,[(-12,-47),(0,-47),(12,-48),(20,-55)]),
 ('terracotta',3.0,[(-43,-18),(-56,-23),(-67,-28),(-70,-31)])]
for mat,width,points in ROUTES:
 for a,b in zip(points,points[1:]):routes.path(a,b,width,mat)
VENUES=[(-29,18),(17,14),(-47,-20),(-20,-12),(25,-38),(-57,34),(-17,54),
        (19,55),(48,36),(-46,-54),(66,-12),(69,65),(53,-56),(-14,-54),(-75,-32)]
APPROACH=[(-28,18),(13,11),(-43,-18),(-19,-24),(24,-29),(-50,31),(-17,45),
          (16,43),(40,27),(-43,-49),(60,-12),(64,58),(48,-49),(-12,-47),(-70,-31)]
for i,((x,z),a) in enumerate(zip(VENUES,APPROACH)):
 length=math.hypot(x,z)
 dest=(x-x/length*8.5,z-z/length*8.5)
 routes.path(a,dest,2.8,'plank' if i in (4,12) else 'stone')
for x,z,w,d,mat in [(0,28,33,16,'stone'),(49,-61,28,9,'plank'),
                     (-28,-74,27,13,'terracotta'),(77,-29,24,16,'bluestone')]:
 routes.box(x,z,.017,w,d,.03,mat)
routes.object(COL['paths'])

# Clock/civic stage: visual hierarchy and material detail, all components seat.
civic=MeshBuilder('Civic clock and festival finale')
civic.cylinder(0,29,.27,2.6,.54,'stone',16)
civic.box(0,29,4.1,2.3,2.3,7.7,'terracotta')
for h,w in [(7.95,3.2),(9.0,3.0)]:civic.box(0,29,h,w,w,.35,'ivory')
civic.cone(0,29,9.15,2.1,2.1,1.7,'teal',12)
civic.cylinder(0,29,11.05,.18,.65,'bronze')
for side in (-1,1):
 # The clock faces are vertical and flush with the gallery, visible from both streets.
 civic.box(0,29+side*1.2,8.45,1.38,.055,1.38,'ivory')
 civic.box(side*1.2,29,8.45,.055,1.38,1.38,'ivory')
 civic.box(0,29+side*1.25,8.45,.075,.045,.58,'metal')
 civic.box(side*1.25,29,8.45,.045,.075,.58,'metal')
civic.box(0,15,.18,8,4.4,.36,'plank')
civic.box(0,12.77,.38,8.2,.17,.13,'bronze')
for x in (-3.85,3.85):
 for z in (13.1,16.9):civic.box(x,z,2.45,.16,.16,4.9,'timber')
# A pitched cloth canopy, with both upper and lower faces visible at player height.
roof_left=[V(-4.25,17.65,4.78),V(0,17.65,5.45),
           V(0,12.45,5.45),V(-4.25,12.45,4.78)]
roof_right=[V(0,17.65,5.45),V(4.25,17.65,4.78),
            V(4.25,12.45,4.78),V(0,12.45,5.45)]
for panel,mat in ((roof_left,'coral'),(roof_right,'teal')):
 civic.face(panel,mat)
 civic.face(list(reversed(panel)),'ivory')
civic.box(0,17.05,1.98,7.3,.16,3.05,'navy')
for x,mat in ((-2.4,'coral'),(0,'gold'),(2.4,'teal')):
 civic.box(x,16.94,1.85,1.05,.055,2.2,mat)
civic.sphere(0,16.82,2.55,.55,.09,.55,'gold',4,10)
for z,h in ((11.9,.12),(12.25,.24),(12.6,.36)):
 civic.box(0,z,h*.5,4.2,.33,h,'stone')
# Public fountain and the three physical sign frames are integrated in Blender;
# Unity keeps only their live text/progress/button components.
civic.cylinder(0,0,.20,2.85,.40,'stone',24)
civic.cylinder(0,0,.45,2.58,.13,'bronze',24)
civic.cylinder(0,0,1.22,.25,1.55,'ivory',12)
civic.sphere(0,0,2.23,.52,.52,.52,'gold',5,12)
for bx,bz,bw,bh in [(-7.8,-15,7.6,3.3),(8.3,-15,7.6,3.5),(15,-20,5.2,1.25)]:
 center_h=1.9 if bz==-15 else 2.35
 civic.box(bx,bz,center_h,bw,.14 if bz==-20 else .22,bh,'navy')
 civic.box(bx,bz-.16,center_h+bh*.52,bw+.18,.16,.13,'bronze')
 if bz==-15:
  for sx in (-1,1):civic.box(bx+sx*(bw/2-.23),bz,1.05,.20,.27,2.1,'timber')
civic.cylinder(15,-20.4,1.05,.54,.34,'teal',12)
civic.cylinder(15,-20.4,1.22,.67,.06,'bronze',12)
civic.box(0,12.37,4.67,8.35,.07,.07,'bronze')
for i in range(15):
 x=-3.5+i*.5
 civic.sphere(x,12.30,4.57,.15,.15,.18,'gold',3,8)
# An arrival arch marks the walk from the welcome boards toward the square.
for x in (-10.2,10.2):civic.box(x,-22,2.4,.16,.16,4.8,'timber')
civic.box(0,-22,4.55,20.4,.06,.055,'bronze')
for i in range(20):
 x=-9.7+i*1.02
 p=[V(x,-22,4.50),V(x+.72,-22,4.50),V(x+.36,-22,3.86)]
 mat=('coral','gold','teal','ivory')[i%4]
 civic.face(p,mat)
 civic.face(list(reversed(p)),mat)
for side in (-1,1):
 for z in (21,33):
  x=side*11
  civic.box(x,z,.25,2.25,1.05,.5,'timber')
  civic.box(x,z,.53,1.95,.78,.075,'gravel')
  for j in range(5):
   bx=x-.78+j*.39
   civic.sphere(bx,z,.85,.17,.17,.23,'flower' if j%2 else 'leaf',3,8)
for side in (-1,1):
 x=side*12
 civic.box(x,34,.55,2.8,.7,.16,'plank')
 civic.box(x,34.36,1.02,2.8,.13,.8,'timber')
 for leg in (-1,1):civic.box(x+leg*1.08,34,.26,.16,.58,.52,'metal')
civic.object(COL['civic'])

garden=MeshBuilder('Productive gardens and outdoor arts')
for row in range(3):
 x=-28+row*5.2
 garden.box(x,-34,.28,3.7,8.2,.56,'timber')
 garden.box(x,-34,.58,3.3,7.8,.06,'gravel')
 for j in range(6):
  z=-37.3+j*1.32
  garden.cylinder(x,z,.87,.10,.54,'leaf',7)
  garden.sphere(x,z,1.22,.37,.34,.24,'flower' if (row+j)%2 else 'leaf',3,8)
garden.cylinder(-53,-78,.18,4.2,.36,'plank',24)
for ring in range(3):
 count=7+ring*2
 for seat in range(count):
  angle=(-116+232*seat/(count-1))*math.pi/180
  radius=5.5+ring*1.9
  garden.box(-53+math.sin(angle)*radius,-78+math.cos(angle)*radius,
             .28,1.35,.72,.55,'stone',-angle)
for i in range(3):
 x=-38+i*10;z=-77+(i%2)*2
 garden.cylinder(x,z,.36,1.25,.72,'stone',12)
 if i==0:
  garden.cylinder(x,z,1.95,.18,2.45,'bronze',10)
  garden.sphere(x,z,3.35,.66,.66,.66,'gold',4,10)
 elif i==1:
  for j in range(3):garden.box(x,z,1.05+j*.7,1.55,1.55,.15,
                                 ('coral','teal','gold')[j],j*.4)
 else:
  garden.cylinder(x,z,1.7,.22,2.4,'ivory',10)
  garden.sphere(x,z,3.1,.95,.95,.95,'teal',5,10)
garden.object(COL['garden_arts'])

coast=MeshBuilder('Harbor piers, railings and seaside festival furniture')
coast.disc(35,-77,.008,27.5,12.5,'sand',40,.055,10)
for a,b,w in [((52,-63),(52,-70),3),((52,-70),(51,-80),3),
              ((20,-64),(18,-69),2.5),((18,-69),(19,-78),2.5),
              ((19,-78),(22,-86),2.5)]:
 coast.path(a,b,w,'plank',.18)
 # Deck ribs visibly seat into the planks; joists end at the path edges.
 length=math.dist(a,b);count=max(2,int(length/1.5))
 for j in range(count+1):
  t=j/count;x=a[0]+(b[0]-a[0])*t;z=a[1]+(b[1]-a[1])*t
  coast.box(x,z,.05,w,.13,.28,'timber',math.atan2(b[1]-a[1],b[0]-a[0]))
for side in (-1,1):
 x=19+side*1.46
 for j in range(5):coast.cylinder(x,-81+j*3,.54,.09,1.08,'ivory',8)
 coast.box(x,-75,.94,.12,16,.08,'ivory')
for i in range(8):
 x=17+i*5.4
 coast.cylinder(x,-66,.70,.28,1.4,'timber',10)
 coast.cylinder(x,-66,1.43,.37,.15,'metal',10)
coast.cylinder(58,-81,1.5,.94,3.0,'ivory',12)
coast.cone(58,-81,2.95,1.10,1.10,.65,'coral',12)
coast.sphere(58,-81,3.68,.48,.48,.48,'gold',5,12)
for i in range(3):
 x=12+i*3
 coast.box(x,-56,.40,1.55,3.0,.16,'ivory',.2)
 coast.cylinder(x,-57.3,1.34,.07,2.55,'ivory',8)
 coast.disc(x,-57.3,2.64,1.4,1.4,'teal' if i%2 else 'coral',12)
coast.object(COL['waterfront'])
water=MeshBuilder('Tidal inlet water')
water.disc(35,-77,.075,25,10,'water',48,.04,5)
water.disc(0,0,.515,2.43,2.43,'water',32)
water.object(COL['water'])

maker=MeshBuilder('Recycling crane, maker yard and sky garden')
maker.box(-87,-54,5.3,.8,.8,10.6,'metal')
maker.box(-82.8,-54,9.6,9.2,.7,.56,'metal')
maker.cylinder(-79.4,-54,6.8,.07,5.6,'metal',8)
maker.box(-79.4,-54,4.15,.65,.32,.75,'gold')
for i in range(3):
 x=-82+i*6
 maker.box(x,-54,.77,4,3.2,1.54,('bluestone','leaf','terracotta')[i])
 maker.box(x,-54,1.75,3.3,2.5,.6,'gravel')
for i in range(3):maker.box(78+(i-1)*6,-30,.05,.17,11,.025,'gold')
maker.box(78,-26,4,16,.45,.4,'metal')
for side in (-1,1):maker.box(78+side*7.7,-26,2,.4,.45,4,'metal')
for radius,mat in [(12,'ivory'),(9.5,'navy'),(7,'ivory')]:
 maker.disc(80,82,.015+(12-radius)*.002,radius,radius,mat,40)
for i in range(12):
 a=i*math.pi/6
 maker.sphere(80+math.sin(a)*10,82+math.cos(a)*10,.18,.18,.18,.18,'gold',3,8)
maker.cylinder(80,82,1.25,.14,2.5,'bronze',10)
maker.sphere(80,82,3.15,1.22,1.22,1.22,'navy',6,12)
maker.sphere(80,82,3.15,.35,.35,.35,'gold',5,10)
maker.object(COL['maker_sky'])

# One distant Blender mesh gives the square grass horizon a crafted silhouette.
# The inner edge is beyond all venues, the observatory sky garden, and XR ground.
perimeter=MeshBuilder('Distant foothills and clustered horizon groves')
RIDGES=(144,151,161,177,193,215,238)
SECTORS=96
def hill_point(r,a):
 wave=2.8*math.sin(a*3.0+.35)+1.4*math.sin(a*7.0-1.1)
 r+=wave
 x=math.sin(a)*r;z=math.cos(a)*r
 t=max(0,min(1,(r-144)/94))
 relief=(max(0,math.sin(math.pi*t))**1.45)*(
  13.5+3.3*math.sin(a*4.0+.6)+1.6*math.sin(a*11.0-.4))
 return (x,z,base_height(x,z)+relief+.14)
rings=[[hill_point(r,i*2*math.pi/SECTORS) for i in range(SECTORS)] for r in RIDGES]
for band in range(len(RIDGES)-1):
 for i in range(SECTORS):
  j=(i+1)%SECTORS
  a=(i+.5)*2*math.pi/SECTORS
  if band in (0,len(RIDGES)-2):mat='grass'
  elif band in (2,3) and math.sin(a*5.0+.7)>.30:mat='ridge_ochre'
  else:mat='ridge_sage'
  perimeter.face([V(*rings[band][i]),V(*rings[band+1][i]),
                  V(*rings[band+1][j]),V(*rings[band][j])],mat)

# Canopy groups change from garden broadleaf at the old town to coastal pine
# around the inlet. Their footprints stay entirely outside the play surface.
groves=[(-10,13,14),(34,13,15),(75,14,16),(115,16,13),
        (157,16,12),(201,16,17),(244,18,16),(289,15,15),(330,13,13)]
rng=random.Random(51873)
tree_count=0
for angle,spread,count in groves:
 for i in range(count):
  a=math.radians(angle+rng.uniform(-spread,spread))
  # Place most crowns on the ridge crest so they read from player height.
  r=164+rng.uniform(0,32)
  x,z,h=hill_point(r,a)
  h-=.22 # taper the trunk into the triangulated hillside
  scale=.82+rng.random()*.42
  coastal=z < -50 or x > 95
  pine=rng.random() < (.78 if coastal else .33)
  if pine:
   perimeter.cone(x,z,h,.28*scale,.30*scale,6.4*scale,'ridge_bark',6)
   for lift,wide,height in ((1.8,2.15,4.3),(3.35,1.69,3.9),(4.9,1.16,3.65)):
    perimeter.cone(x,z,h+lift*scale,wide*scale,wide*scale,
                   height*scale,'ridge_pine',8)
  else:
   perimeter.cone(x,z,h,.30*scale,.31*scale,4.8*scale,'ridge_bark',6)
   offset=.38*math.sin(a*3+i)
   perimeter.sphere(x+offset,z,h+5.15*scale,2.0*scale,1.65*scale,
                    2.1*scale,'ridge_leaf',3,8)
   perimeter.sphere(x+offset+1.15*scale,z-.30*scale,h+5.0*scale,
                    1.4*scale,1.35*scale,1.5*scale,'ridge_leaf',3,7)
  tree_count+=1
perimeter.object(COL['perimeter'])
print('PERIMETER GROVE TREES',tree_count)

# The first assignment is Market. Its roof pennant clears the welcome board
# from the spawn sightline, while a trailpost and inset badges identify the
# actual left branch around the fountain without touching XR collision.
guide=MeshBuilder('Market first-visit pennant, trailpost and pavement inlays')
guide.cylinder(-29,18,8.38,.085,3.58,'timber',10) # mounted into the 7.26m roof
guide.sphere(-29,18,10.26,.18,.18,.18,'gold',4,10)
flag=[V(-29,18,9.92),V(-26.23,18,9.92),V(-26.75,18,9.34),
      V(-26.23,18,8.77),V(-29,18,8.77)]
guide.face(flag,'coral');guide.face(list(reversed(flag)),'coral')
guide.box(-27.65,17.955,9.35,.73,.035,.42,'gold')
guide.sphere(-27.65,17.91,9.62,.22,.045,.12,'leaf',3,8)

# The south-facing trailpost stands off the right edge of the walk. The
# raised lettering is modeled with Blender's font conversion, then baked in.
guide.cylinder(6.08,-10,1.31,.115,2.62,'timber',10)
guide.sphere(6.08,-10,2.68,.18,.18,.18,'gold',3,8)
guide.box(5.42,-10,2.06,2.54,.12,.61,'timber')
guide.box(5.42,-10.075,2.06,2.41,.028,.49,'navy')
mesh_text(guide,'MARKET',5.08,-10.106,2.04,.32,'ivory')
guide.box(6.16,-10.125,2.06,.45,.025,.065,'gold')
arrow=[V(5.74,-10.145,2.06),V(6.00,-10.145,2.28),
       V(6.00,-10.145,1.84)]
guide.face(arrow,'gold');guide.face(list(reversed(arrow)),'gold')

market_cues=[(0,-24),(-2,-17),(-4,-8),(-6,4),(-12,9),(-18,12),(-21.8,13.5)]
for i,(x,z) in enumerate(market_cues):
 guide.disc(x,z,.058,.37,.37,'coral',12)
 tx,tz=market_cues[i+1] if i+1<len(market_cues) else (-29,18)
 dx=tx-x;dz=tz-z;length=math.hypot(dx,dz)
 dx/=length;dz/=length;rx=-dz;rz=dx
 outline=[(-.27,-.105),(.06,-.105),(.06,-.21),(.34,0),
          (.06,.21),(.06,.105),(-.27,.105)]
 glyph=[V(x+dx*f+rx*l,z+dz*f+rz*l,.068) for f,l in outline]
 guide.face(glyph,'gold');guide.face(list(reversed(glyph)),'gold')
guide.object(COL['market_wayfinding'])

BLEND=os.path.join(SOURCE,'festival_environment.blend')
bpy.ops.wm.save_as_mainfile(filepath=BLEND,compress=True)

manifest={'provenance':'AI-assisted Blender environment reference; disclose as AI-generated in coursework.',
          'source_blend':os.path.relpath(BLEND,ROOT),'fbx':{},
          'market_wayfinding':{'roof_pennant_world':[-29,18,10.26],
                               'trailpost_world':[5.42,-10,2.06],
                               'inlaid_marker_world_xz':market_cues,
                               'colliders':0},
          'axis':'Blender Z-up; project FBX -Z forward/Y up; Unity builder uses project model placement basis.'}
for key,c in COL.items():
 bpy.ops.object.select_all(action='DESELECT')
 objects=[o for o in c.objects if o.type=='MESH']
 if not objects:continue
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0]
 joined=objects[0].copy()
 joined.data=objects[0].data.copy()
 bpy.context.scene.collection.objects.link(joined)
 bpy.ops.object.select_all(action='DESELECT')
 joined.select_set(True)
 bpy.context.view_layer.objects.active=joined
 joined.name=key+' optimized Blender export'
 bpy.context.scene.cursor.location=(0,0,0)
 bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 # The existing PlaceModel(-90 X, 180 Y) basis needs this source turn.
 # Unity Game-view QA showed the new wayfinding FBX's X axis mirrored by the
 # combined import/placement basis, reversing MARKET, the arrow, and route.
 # Cancel that X turn only for this mesh; reverse winding to retain outward
 # facing front/top surfaces after the one-axis reflection.
 for vertex in joined.data.vertices:
  if key!='market_wayfinding':vertex.co.x=-vertex.co.x
  vertex.co.y=-vertex.co.y
 if key=='market_wayfinding':
  for polygon in joined.data.polygons:polygon.flip()
  joined.data.update()
 path=os.path.join(FBX,key+'.fbx')
 bpy.ops.export_scene.fbx(filepath=path,use_selection=True,apply_unit_scale=True,
                          axis_forward='-Z',axis_up='Y',add_leaf_bones=False,path_mode='AUTO')
 tris=sum(len(p.vertices)-2 for p in joined.data.polygons)
 manifest['fbx'][key]={'path':os.path.relpath(path,ROOT),'triangles':tris,
                       'materials':[m.name for m in joined.data.materials],
                       'source_objects':len(objects)}
 bpy.data.objects.remove(joined,do_unlink=True)
with open(os.path.join(ROOT,'asset_manifest.json'),'w',encoding='utf8') as fh:
 json.dump(manifest,fh,indent=2)
print('FESTIVAL ENVIRONMENT COMPLETE',json.dumps({k:v['triangles'] for k,v in manifest['fbx'].items()}))
