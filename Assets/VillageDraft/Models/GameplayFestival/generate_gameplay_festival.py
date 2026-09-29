"""Deterministic Blender source for the village's tactile festival gameplay kit.

Run: blender -b --python generate_gameplay_festival.py
Each asset is a single-user joined mesh with painted material slots. X is width,
Y is depth (front is -Y), and Z is height. Small assets occupy a unit envelope.
"""
import math
import os
import bpy

ROOT = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(ROOT, "FBX")
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)

PALETTE = {
    "timber": (0.28, .17, .095, 1), "timber_light": (.52, .35, .19, 1),
    "stone": (.58, .55, .48, 1), "cream": (.90, .79, .57, 1),
    "brass": (.72, .48, .13, 1), "ink": (.08, .10, .13, 1),
    "glass": (.23, .54, .56, 1), "lamp": (1.0, .72, .27, 1),
    "white": (.86, .88, .83, 1), "leaf": (.22, .52, .29, 1),
    "red": (.76, .19, .14, 1), "blue": (.19, .34, .59, 1),
}
VENUES = {
    "market": (.78,.24,.17,1), "cafe": (.77,.43,.20,1),
    "workshop": (.22,.42,.67,1), "greenhouse": (.25,.58,.30,1),
    "pool": (.19,.62,.68,1), "bakery": (.82,.49,.22,1),
    "post": (.76,.28,.20,1), "clinic": (.22,.65,.64,1),
    "library": (.28,.42,.66,1), "music": (.72,.40,.22,1),
    "garage": (.26,.43,.62,1), "observatory": (.26,.61,.65,1),
    "harbor": (.21,.60,.67,1), "art": (.75,.29,.31,1),
    "recycling": (.27,.58,.38,1),
}

MATS = {}
def mat(name, rgba):
    if name not in MATS:
        m = bpy.data.materials.new(name)
        m.diffuse_color = rgba
        m.use_nodes = True
        m.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = rgba
        m.node_tree.nodes.get("Principled BSDF").inputs["Roughness"].default_value = .76
        MATS[name] = m
    return MATS[name]

for name, color in PALETTE.items(): mat(name, color)
for name, color in VENUES.items(): mat("accent_" + name, color)

PARTS = []
def add_material(obj, material):
    obj.data.materials.append(MATS[material])
    PARTS.append(obj)
    return obj

def box(name, p, size, material):
    bpy.ops.mesh.primitive_cube_add(size=1, location=p)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return add_material(obj, material)

def cyl(name, p, radius, depth, material, sides=12, rotation=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius, depth=depth, location=p)
    obj = bpy.context.object
    obj.name = name
    if rotation: obj.rotation_euler = rotation
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return add_material(obj, material)

def ring(name, p, major, minor, material, rotation=None):
    bpy.ops.mesh.primitive_torus_add(major_segments=16, minor_segments=6,
        location=p, major_radius=major, minor_radius=minor)
    obj = bpy.context.object
    obj.name = name
    if rotation: obj.rotation_euler = rotation
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    return add_material(obj, material)

EXPORTED = []
def finish(name):
    global PARTS
    assert PARTS, name
    bpy.ops.object.select_all(action="DESELECT")
    for obj in PARTS: obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    if name != "yard_floor" and name != "yard_spur":
        # Soft machined and carpentered edges catch light in VR at arm's length.
        # The modifier stays editable in the .blend and is baked into each FBX.
        bevel = obj.modifiers.new("Festival edge finish", "BEVEL")
        bevel.width = .012 if name.startswith("station_") else .009
        bevel.segments = 2
        bevel.limit_method = "ANGLE"
        bevel.angle_limit = math.radians(29)
    bpy.context.scene.cursor.location = (0,0,0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True,
        use_mesh_modifiers=True,
        axis_forward='-Z', axis_up='Y', add_leaf_bones=False, path_mode='AUTO')
    EXPORTED.append((name, len(obj.data.polygons)))
    index = len(EXPORTED)-1
    obj.location = ((index % 12)*3.8, (index // 12)*4.4, 0)
    PARTS = []

def footed_table(accent):
    box("joined slat worktop", (0,0,.35), (1,.84,.12), "timber_light")
    for x in (-.40,.40):
        for y in (-.33,.33):
            box("square leg", (x,y,-.10), (.105,.105,.80), "timber")
    box("front festival ribbon", (0,-.43,.32), (.94,.025,.065), accent)
    for x in (-.33,0,.33):
        box("separate top board", (x,0,.418), (.30,.83,.02), "cream")

def station(id, layout):
    accent = "accent_" + id
    if layout == 0:
        footed_table(accent)
        cyl("inlaid preparation tray", (.25,.04,.455), .18,.035, "brass")
    elif layout == 1:
        footed_table(accent)
        for x in (-.42,.42):
            box("upper rack support", (x,.31,.43), (.07,.07,.14), "timber")
        box("raised supply shelf", (0,.31,.51), (.93,.26,.045), accent)
    elif layout == 2:
        footed_table(accent)
        box("rack back", (-.43,.26,.47), (.08,.27,.12), "timber")
        for y in (.14,.25,.36):
            cyl("rack peg", (-.42,y,.49), .018,.12, "brass", 8, (math.pi/2,0,0))
    elif layout == 3:
        # Left work surface and a real open sorting bin on the right.
        box("preparation stand", (-.24,0,.26), (.55,.81,.14), "timber_light")
        for x in (-.46,-.02):
            for y in (-.34,.34):
                box("preparation leg", (x,y,-.15), (.08,.08,.78), "timber")
        box("bin floor", (.28,.03,-.42), (.43,.52,.07), "timber")
        for x in (.065,.495):
            box("bin side", (x,.03,-.17), (.06,.52,.48), accent)
        for y in (-.23,.29):
            box("bin end", (.28,y,-.17), (.43,.06,.48), accent)
    else:
        cyl("octagonal inspection pedestal", (0,0,-.13), .39,.74, "stone", 8)
        cyl("inspection rim", (0,0,.28), .48,.10, accent, 12)
        cyl("inspection surface", (0,0,.345), .38,.035, "cream", 12)
        for x in (-.22,.22):
            cyl("inspection brass fastener", (x,-.34,.34), .022,.025, "brass", 8)
    finish(f"station_{id}_{layout}")

for venue in VENUES:
    for layout in range(5): station(venue,layout)

# Tactile controls and fittings: all pieces contact their parent surface.
cyl("fluted press base", (0,0,-.31), .48,.31,"brass",12)
ring("pressed trim ring", (0,0,-.14), .40,.07,"ink")
cyl("colored plunger", (0,0,.17), .30,.46,"red",12)
cyl("convex plunger cap", (0,0,.43), .26,.12,"lamp",12)
finish("press_button")

cyl("placement well bed", (0,0,-.30), .44,.32,"ink",12)
ring("socket brass lip", (0,0,-.08), .38,.075,"brass")
ring("inner colored guide", (0,0,.09), .28,.025,"glass")
finish("placement_socket")

box("festival parcel body", (0,0,0), (.78,.72,.77),"cream")
box("vertical parcel ribbon", (0,0,.015), (.14,.74,.81),"red")
box("horizontal parcel ribbon", (0,0,.015), (.80,.13,.81),"red")
box("parcel tie top", (0,0,.43), (.27,.23,.09),"brass")
finish("grab_token")

cyl("display stand foot", (0,0,-.34), .45,.18,"timber",12)
cyl("display stand stem", (0,0,-.02), .16,.52,"brass",10)
cyl("display stand dish", (0,0,.28), .49,.13,"cream",12)
finish("grab_pedestal")

box("number plaque wood", (0,0,0), (.98,.96,.70),"timber")
box("number inset dark face", (0,-.385,0), (.85,.035,.76),"ink")
for x in (-.41,.41):
    for z in (-.38,.38): cyl("brass mounting pin", (x,-.411,z),.025,.025,"brass",8,(math.pi/2,0,0))
finish("number_plate")

box("inset indicator bezel", (0,0,0), (.94,.95,.78),"brass")
box("lantern glass", (0,-.39,0), (.68,.04,.56),"lamp")
finish("current_light")

box("progress strip bed", (0,0,0), (1,.98,.75),"timber")
box("woven colored strip", (0,-.49,0), (.93,.02,.43),"glass")
finish("status_strip")

cyl("lantern base", (0,0,-.37),.40,.20,"brass",10)
box("lantern glass chamber", (0,0,0), (.43,.43,.57),"lamp")
cyl("lantern roof", (0,0,.39),.45,.20,"timber",10)
finish("status_lamp")

# A small shelf-mounted first-task pointer. Unity adds the changing caption.
box("tutorial marker planted foot", (0,0,.045), (.25,.23,.09), "brass")
box("tutorial marker timber stem", (0,.01,.36), (.075,.08,.64), "timber")
box("tutorial marker sign surround", (0,-.055,.73), (.98,.12,.36), "brass")
box("tutorial marker dark caption inset", (0,-.13,.73), (.88,.035,.25), "ink")
box("tutorial marker colored cap", (0,-.13,.91), (.98,.035,.06), "lamp")
finish("tutorial_marker")

cyl("star reward hub", (0,0,0),.32,.32,"brass",10)
for a in range(5):
    ang=a*math.tau/5
    box("radiating festival spark", (math.sin(ang)*.32,math.cos(ang)*.32,0),
        (.12,.24,.16),"lamp")
finish("effect_token")

box("signboard timber backing", (0,0,0), (1,.18,.82),"timber")
box("recessed black sign face", (0,-.105,.01), (.92,.035,.65),"ink")
box("festival colored cap", (0,-.105,.38), (1,.04,.12),"red")
finish("direction_sign")

box("axle stem", (0,0,0), (.38,.38,1),"timber")
finish("machine_axle")
ring("mounted brass flywheel", (0,0,0), .40,.10,"brass")
cyl("flywheel hub", (0,0,0), .13,.22,"ink",10)
finish("machine_wheel")

# A timber and ceramic freestanding task kiosk; ground at Z=0, text belongs to Unity UI.
box("kiosk ground sill", (0,0,.10), (3.40,.52,.20),"stone")
for x in (-1.52,1.52):
    box("kiosk planted post", (x,0,1.39), (.13,.14,2.58),"timber")
box("kiosk signframe", (0,0,1.76), (3.45,.17,1.96),"timber_light")
box("kiosk dark inset", (0,-.10,1.76), (3.23,.035,1.72),"ink")
box("kiosk colored gable", (0,-.05,2.77), (3.48,.26,.13),"red")
for x in (-1.32,1.32):
    cyl("kiosk brass rivet", (x,-.13,2.54), .055,.035,"brass",8,(math.pi/2,0,0))
finish("task_kiosk")

# Controller frame: 0.50 m wide, 0.61 m tall; screen opening faces Blender -Y.
box("wrist menu back", (0,.008,0), (.50,.028,.61),"timber")
box("wrist menu recessed display", (0,-.011,0), (.475,.011,.575),"ink")
for x in (-.236,.236):
    box("wrist menu side rail", (x,-.013,0), (.028,.036,.61),"brass")
for z in (-.29,.29):
    box("wrist menu end rail", (0,-.013,z), (.50,.036,.025),"brass")
box("left wrist strap", (-.18,.033,-.19), (.115,.055,.16),"timber_light")
box("right wrist strap", (.18,.033,-.19), (.115,.055,.16),"timber_light")
finish("controller_menu_frame")

cyl("festival plinth stone", (0,0,-.20), .48,.60,"stone",12)
ring("festival plinth gilt lip", (0,0,.16), .42,.055,"brass")
cyl("festival plinth display", (0,0,.32), .36,.12,"cream",12)
finish("festival_plinth")

box("replay panel timber frame", (0,0,0), (1,.95,1),"timber")
box("replay panel black inset", (0,-.49,0), (.95,.025,.86),"ink")
box("replay panel top bunting", (0,-.52,.43), (1,.02,.12),"red")
finish("replay_panel")

cyl("replay console foot", (0,0,-.28),.48,.32,"timber",12)
cyl("replay brass bezel", (0,0,-.06),.44,.15,"brass",12)
cyl("replay jewel switch", (0,0,.19),.29,.37,"glass",12)
finish("replay_button")

box("activity yard planted base", (0,0,-.012), (6.8,12.15,.025),"stone")
for ix in range(8):
    for iy in range(14):
        x=-2.96+ix*.845+(iy%2)*.035
        y=-5.61+iy*.86
        box("individual cut flagstone", (x,y,.006), (.79,.80,.018),
            "cream" if (ix+iy)%3 else "stone")
finish("yard_floor")

box("yard approach substrate",(0,0,-.012),(7.4,1.9,.025),"stone")
for ix in range(9):
    for iy in range(2):
        box("approach flagstone",(-3.27+ix*.815,-.47+iy*.94,.006),
            (.77,.87,.018),"cream" if ix%3 else "stone")
finish("yard_spur")

box("scripted result backing", (0,0,0), (.86,.86,.86),"brass")
box("scripted result inset", (0,-.44,0), (.60,.03,.60),"lamp")
finish("scripted_result")

for id, color in VENUES.items():
    accent = "accent_"+id
    # Core machine has a ground-connected distinct silhouette; progress focus/secondary
    # are separate skinned objects so Unity can still move them by task state.
    cyl("machine planted base", (0,0,.16),.48,.32,"stone",12)
    if id in ("market","cafe","bakery","post"):
        box("service cabinet", (0,.08,.88), (.76,.60,1.28),"timber")
        box("service front inset", (0,-.235,1.08), (.56,.03,.60),accent)
        if id=="bakery": ring("oven door rim",(0,-.26,1.08),.27,.06,"brass",(math.pi/2,0,0))
        if id=="post": box("outgoing parcel slot",(0,-.27,.81),(.46,.05,.14),"ink")
    elif id in ("clinic","library","music","observatory"):
        for x in (-.33,.33): box("display upright",(x,.14,1.13),(.07,.12,1.58),"timber")
        box("display head",(0,.14,1.92),(.77,.20,.12),accent)
        box("display dark panel",(0,-.02,1.39),(.60,.05,.72),"ink")
        if id=="observatory": cyl("scope bearing",(0,-.09,1.10),.24,.18,"brass",10,(math.pi/2,0,0))
    elif id in ("garage","harbor","workshop","recycling"):
        for x in (-.31,.31): box("industrial frame",(x,.13,1.07),(.12,.14,1.50),"timber")
        box("industrial crossbeam",(0,.13,1.85),(.75,.20,.15),accent)
        cyl("industrial drive",(0,-.14,1.10),.28,.15,"brass",10,(math.pi/2,0,0))
    else:
        cyl("outdoor vessel",(0,.09,.90),.41,1.15,"timber",10)
        ring("outdoor colored collar",(0,.09,1.53),.38,.075,accent)
        if id=="greenhouse": cyl("planter soil",(0,.09,1.59),.31,.08,"leaf",10)
        if id=="pool": ring("pool race hoop",(0,-.28,1.07),.31,.07,"glass",(math.pi/2,0,0))
        if id=="art": box("gallery canvas",(0,-.37,1.17),(.55,.08,.68),"white")
    # The task lamps and status plaque mount to this timber frame above the
    # mechanism instead of reading as disconnected floating UI.
    for x in (-.63,.63):
        box("festival indicator upright", (x,-.49,2.43), (.085,.10,1.88),"timber")
    finish("machine_core_"+id)

    # Three distinct visual action parts per trade, normalized for placement on
    # Unity's invisible interaction transforms and progress animation targets.
    if id in ("bakery","cafe","market"):
        cyl("crafted serving vessel",(0,0,-.08),.37,.65,"cream",12)
        ring("vessel rim",(0,0,.28),.33,.05,accent)
    elif id in ("greenhouse","art","pool"):
        cyl("rounded festival display",(0,0,-.18),.39,.52,accent,10)
        ring("display collar",(0,0,.16),.38,.065,"brass")
    elif id in ("music","library","post"):
        box("handled object",(0,0,0),(.76,.55,.74),accent)
        box("crafted spine",(-.38,0,0),(.08,.58,.78),"brass")
    else:
        cyl("mechanism barrel",(0,0,0),.35,.72,"brass",10)
        ring("mechanism collar",(0,0,.33),.34,.06,accent)
    finish("machine_focus_"+id)

    box("moving mechanism",(0,0,0),(.65,.40,.78),accent)
    box("mechanism bracket",(0,.23,-.29),(.80,.10,.18),"brass")
    finish("machine_secondary_"+id)

    # Contribution icons are different silhouettes, visible only at festival completion.
    if id in ("market","post","recycling"):
        box("festival package",(0,0,0),(.69,.68,.67),accent)
        box("package seal",(0,-.36,.10),(.36,.035,.20),"brass")
    elif id in ("cafe","bakery","greenhouse"):
        cyl("harvest serving",(0,0,-.10),.37,.62,accent,12)
        ring("harvest rim",(0,0,.26),.32,.055,"brass")
    elif id in ("workshop","garage","harbor","observatory"):
        cyl("festival engineering",(0,0,0),.36,.60,"brass",10)
        for a in range(4):
            ang=a*math.pi/2
            box("machine vane",(math.cos(ang)*.34,math.sin(ang)*.34,0),(.16,.16,.45),accent)
    else:
        cyl("festival badge stem",(0,0,-.16),.14,.66,"brass",10)
        cyl("festival badge face",(0,0,.20),.39,.18,accent,12)
    finish("artifact_"+id)

box("painted art stroke", (0,0,0), (1,.12,.48),"red")
finish("art_stroke")

box("shelf timber",(0,0,0),(1,.92,.65),"timber_light")
box("shelf brass lip",(0,-.47,.26),(1,.06,.13),"brass")
finish("shelf_board")

bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,"GameplayFestivalKit.blend"))
print("GAMEPLAY FESTIVAL KIT", len(EXPORTED), "FBX assets,",
      sum(p for _,p in EXPORTED), "polygons")
