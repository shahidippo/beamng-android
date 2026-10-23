"""Generate the original Aster 1.6 engineering vehicle assets with Blender 4.3+.

Run: blender --background --factory-startup --python Tools/Blender/create_aster.py
Geometry is authored in metres: Blender +Y forward, +Z up. The small OBJ writer
exports Unity +Z forward, +Y up explicitly, with reversed triangle winding.
No external assets or textures are used. This is an engineering asset, not the
finished detailed launch vehicle required by the full production brief.
"""
from pathlib import Path
import math
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/ImpactDrive/Art/Resources/ImpactDrive"
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)

def material(name, rgb, metallic=0.0, roughness=0.4):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1.0)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*rgb, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    return mat

PAINT = material("Aster_Paint", (0.035, 0.23, 0.34), 0.65, 0.25)
GLASS = material("Aster_Glass", (0.07, 0.12, 0.16), 0.55, 0.12)
TRIM = material("Aster_Trim", (0.025, 0.03, 0.035), 0.12, 0.58)
RUBBER = material("Aster_Rubber", (0.028, 0.028, 0.028), 0.0, 0.82)
ALLOY = material("Aster_Alloy", (0.51, 0.57, 0.59), 0.86, 0.22)
BRAKE = material("Aster_Brake", (0.20, 0.22, 0.23), 0.7, 0.56)
CALIPER = material("Aster_Caliper", (0.53, 0.10, 0.045), 0.5, 0.33)
LAMP = material("Aster_Headlamp", (0.80, 0.92, 0.97), 0.45, 0.16)
RED = material("Aster_Taillamp", (0.72, 0.025, 0.015), 0.25, 0.22)
AMBER = material("Aster_Indicator", (1.0, 0.32, 0.025), 0.2, 0.22)
SEAT = material("Aster_Fabric", (0.12, 0.145, 0.16), 0.0, 0.95)
CHROME = material("Aster_Chrome", (0.8, 0.84, 0.86), 0.95, 0.18)

body_objects, wheel_objects, brake_objects = [], [], []
current = body_objects

def finish(obj, name, mat, bevel=0.0):
    obj.name = name
    if mat:
        obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new("Manufactured edge radii", "BEVEL")
        mod.width, mod.segments = bevel, 2
        mod.affect = "EDGES"
        obj.modifiers.new("Weighted corner normals", "WEIGHTED_NORMAL")
    current.append(obj)
    return obj

def box(name, center, size, mat, bevel=0.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, mat, bevel)

def mesh(name, vertices, faces, mat, bevel=0.0):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    return finish(obj, name, mat, bevel)

def panel(name, verts, mat, thickness=0.012):
    obj = mesh(name, verts, [tuple(range(len(verts)))], mat)
    solid = obj.modifiers.new("Panel thickness", "SOLIDIFY")
    solid.thickness = thickness
    return obj

def cylinder(name, center, radius, depth, mat, axis="X", vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=center)
    obj = bpy.context.object
    if axis == "X": obj.rotation_euler[1] = math.pi / 2
    elif axis == "Y": obj.rotation_euler[0] = math.pi / 2
    for face in obj.data.polygons: face.use_smooth = True
    return finish(obj, name, mat, 0.003)

def rod(name, a, b, radius, mat):
    av, bv = Vector(a), Vector(b)
    obj = cylinder(name, (av + bv) / 2, radius, (bv - av).length, mat, "Z", 12)
    obj.rotation_euler = (bv - av).to_track_quat("Z", "Y").to_euler()
    return obj

# Six body stations provide sloping front/rear corners without a scaled box body.
stations = [(-2.01, .67, -.10, .29), (-1.72, .86, -.12, .43),
            (-.95, .89, -.12, .46), (.65, .88, -.12, .44),
            (1.58, .84, -.10, .38), (2.02, .70, -.04, .25)]
verts = []
for y, width, low, high in stations:
    verts += [(-width, y, low), (width, y, low), (width, y, high), (-width, y, high)]
faces = [(3, 2, 1, 0)]
for i in range(len(stations)-1):
    for j in range(4): faces.append((i*4+j, i*4+(j+1)%4, (i+1)*4+(j+1)%4, (i+1)*4+j))
faces.append(tuple((len(stations)-1)*4+j for j in range(4)))
faces = [tuple(reversed(face)) for face in faces]
hull = mesh("Body_Shell", verts, faces, PAINT, .025)
# Cut physical wheel arches out of the shell. Booleans are evaluated in the export.
for axle in (-1.30, 1.30):
    bpy.ops.mesh.primitive_cylinder_add(vertices=48, radius=.405, depth=2.2,
                                      location=(0, axle, -.015), rotation=(0, math.pi/2, 0))
    cutter = bpy.context.object
    cutter.name = "WheelArchCutter"
    mod = hull.modifiers.new("Wheel travel clearance", "BOOLEAN")
    mod.operation, mod.solver, mod.object = "DIFFERENCE", "EXACT", cutter
    # Apply now before removing the construction cutter.
    bpy.context.view_layer.objects.active = hull
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter, do_unlink=True)

# Roof with distinct pillars and glass panels; open cabin interior below.
roof = box("Roof", (0, -.30, 1.015), (1.37, 1.22, .075), PAINT, .065)
panel("Front_Windscreen", [(-.71,.34,.99),(.71,.34,.99),(.80,.85,.47),(-.80,.85,.47)], GLASS)
panel("Rear_Glass", [(-.70,-.94,.99),(-.79,-1.48,.47),(.79,-1.48,.47),(.70,-.94,.99)], GLASS)
for side in (-1, 1):
    x = side
    panel("Side_Front_Glass_"+str(side), [(x*.80,.81,.47),(x*.71,.32,.98),(x*.71,-.24,.98),(x*.87,-.24,.47)], GLASS)
    panel("Side_Rear_Glass_"+str(side), [(x*.87,-.30,.47),(x*.71,-.30,.98),(x*.70,-.94,.98),(x*.79,-1.45,.47)], GLASS)
    for label,a,b,r in [
        ("A_Pillar",(x*.80,.86,.45),(x*.71,.34,1.01),.035),
        ("B_Pillar",(x*.87,-.27,.44),(x*.71,-.27,1.02),.035),
        ("C_Pillar",(x*.80,-1.50,.44),(x*.70,-.95,1.01),.05),
        ("DoorSill",(x*.88,-.88,-.06),(x*.88,.84,-.06),.025),
        ("DoorSeam",(x*.89,-.27,.02),(x*.88,-.27,.44),.006),
        ("LowerWindowTrim",(x*.89,-1.11,.45),(x*.83,.82,.45),.015)]:
        rod(label+str(side),a,b,r,TRIM if "Trim" in label or "Seam" in label else PAINT)
    box("Door_Handle_Front"+str(side),(x*.885,-.10,.36),(.028,.17,.027),CHROME,.008)
    box("Door_Handle_Rear"+str(side),(x*.87,-.92,.36),(.028,.15,.027),CHROME,.008)
    rod("MirrorArm"+str(side),(x*.81,.71,.51),(x*.99,.65,.57),.025,TRIM)
    box("Wing_Mirror"+str(side),(x*1.015,.64,.59),(.19,.19,.105),PAINT,.035)
    box("MirrorGlass"+str(side),(x*1.015,.55,.59),(.145,.012,.072),GLASS,.012)
    box("Headlight"+str(side),(x*.55,1.938,.25),(.39,.09,.125),LAMP,.028)
    box("Indicator_Front"+str(side),(x*.76,1.82,.24),(.10,.14,.065),AMBER,.018)
    box("Taillight"+str(side),(x*.61,-1.94,.29),(.29,.085,.15),RED,.025)
    box("Indicator_Rear"+str(side),(x*.70,-1.941,.34),(.105,.09,.045),AMBER,.008)
    box("FrontSeat_Cushion"+str(side),(x*.42,-.01,.23),(.55,.48,.17),SEAT,.065)
    seat_back = box("FrontSeat_Back"+str(side),(x*.42,-.27,.50),(.54,.15,.56),SEAT,.06)
    seat_back.rotation_euler[0]=math.radians(-9)
    box("Headrest"+str(side),(x*.42,-.32,.83),(.28,.12,.20),SEAT,.035)

box("Front_Bumper",(0,1.94,.035),(1.42,.17,.16),PAINT,.045)
box("Rear_Bumper",(0,-1.97,.02),(1.43,.16,.16),PAINT,.04)
box("Front_LowerGrille",(0,2.035,.075),(.79,.018,.13),TRIM,.015)
for x in (-.30,-.20,-.10,0,.10,.20,.30): box("GrilleSlat",(x,2.05,.075),(.016,.008,.09),BRAKE,.001)
box("RearPlate",(0,-2.055,.19),(.39,.012,.09),TRIM,.008)
box("FrontPlate",(0,2.045,.23),(.34,.012,.075),TRIM,.007)
box("Dashboard",(0,.56,.49),(1.48,.31,.15),TRIM,.05)
box("CentreConsole",(0,-.08,.23),(.19,.72,.20),TRIM,.035)
box("RearBench",(0,-.91,.23),(1.34,.42,.17),SEAT,.07)
box("RearSeatBack",(0,-1.14,.48),(1.33,.16,.47),SEAT,.07)
box("FloorPan",(0,-.05,-.015),(1.45,2.84,.065),TRIM,.02)
box("EngineBlock",(0,1.23,.15),(.59,.47,.38),BRAKE,.04)
box("EngineCover",(0,1.22,.36),(.62,.49,.07),TRIM,.035)
box("Radiator",(0,1.74,.15),(.92,.07,.30),BRAKE,.015)
rod("ExhaustPipe",(.33,-1.95,-.08),(.33,.88,-.08),.024,BRAKE)
cylinder("ExhaustTip",(.33,-2.05,-.065),.039,.21,CHROME,"Y",20)
for axle in (-1.30,1.30):
    rod("FixedCrossmember",(-.57,axle,.08),(.57,axle,.08),.025,BRAKE)
for x in (-.46,.46): box("ChassisRail",(x,0,-.065),(.075,3.52,.065),BRAKE,.01)
# Steering wheel is a distinct object for future visual driver-rig animation.
bpy.ops.mesh.primitive_torus_add(major_radius=.145,minor_radius=.018,major_segments=32,
                                minor_segments=10,location=(-.42,.34,.65),rotation=(math.pi/2,0,0))
finish(bpy.context.object,"Steering_Wheel",TRIM)
for dx,dz in [(-.13,0),(.13,0),(0,-.13)]:
    rod("SteeringSpoke",(-.42,.34,.65),(-.42+dx,.34,.65+dz),.014,ALLOY)
rod("GearSelector",(0,.12,.31),(0,.12,.47),.018,BRAKE)
cylinder("GearKnob",(0,.12,.47),.035,.045,TRIM,"Z",16)
# Wipers have proper base pivots represented as separate geometry.
for x in (-.39,.22): rod("Wiper",(x,.83,.49),(x+.29,.73,.58),.008,TRIM)

# A hub-centred wheel asset with true circular tyre cross-section and radial spokes.
current = wheel_objects
bpy.ops.mesh.primitive_torus_add(major_radius=.249,minor_radius=.058,major_segments=48,
                                minor_segments=12,location=(0,0,0),rotation=(0,math.pi/2,0))
tyre=finish(bpy.context.object,"Tyre",RUBBER)
# Scale along the axle for a 205 mm approximate tyre width.
bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
tyre.scale.x=1.76
cylinder("RimBarrel",(0,0,0),.203,.18,ALLOY,"X",48)
cylinder("RimRecess",(.094,0,0),.176,.012,TRIM,"X",40)
cylinder("BrakeDisc",(.061,0,0),.143,.012,BRAKE,"X",40)
caliper=box("BrakeCaliper",(.065,.115,0),(.055,.06,.13),CALIPER,.01)
wheel_objects.remove(caliper)
brake_objects.append(caliper)
for angle in range(0,360,72):
    a=math.radians(angle)
    for offset in (-.055,.055):
        p=(.105, math.sin(a)*.174, math.cos(a)*.174)
        q=(.105, math.sin(a+offset)*.042, math.cos(a+offset)*.042)
        rod("SplitSpoke",q,p,.015,ALLOY)
    cylinder("Lug",(.116,math.sin(a)*.045,math.cos(a)*.045),.009,.008,CHROME,"X",8)
cylinder("HubCap",(.12,0,0),.031,.018,ALLOY,"X",24)
# Shallow tread blocks are geometry, approximately 100 triangles per row.
for i in range(40):
    a=2*math.pi*i/40
    for side in (-1,1):
        block=box("Tread",(side*.045,math.sin(a)*.306,math.cos(a)*.306),(.067,.014,.008),RUBBER,0)
        block.rotation_euler.x=-a

def export_obj(objects, path, combine=False):
    """Export evaluated triangulated geometry; no third-party import package needed."""
    graph=bpy.context.evaluated_depsgraph_get()
    mats={m.name:m for obj in objects for m in obj.data.materials if m}
    mtl_path=path.with_suffix(".mtl")
    with mtl_path.open("w") as out:
        for name,mat in mats.items():
            out.write("newmtl "+name+"\nKd %.6f %.6f %.6f\nKa 0 0 0\nd 1\n\n" % tuple(mat.diffuse_color[:3]))
    vertex_offset=1
    triangles=0
    minimum=[math.inf]*3; maximum=[-math.inf]*3
    with path.open("w") as out:
        out.write("# ImpactDrive original engineering asset; metres; +Z forward, +Y up.\nmtllib "+mtl_path.name+"\n")
        if combine:
            # Merge static pieces into one multi-material mesh; retain column parts
            # as separate named meshes so runtime can animate the steering wheel.
            objects=sorted(objects,key=lambda o: o.name.startswith(("Steering_Wheel","SteeringSpoke")))
            out.write("o "+path.stem+"\n")
        exported_objects=1 if combine else len(objects)
        for obj in objects:
            evaluated=obj.evaluated_get(graph)
            data=evaluated.to_mesh()
            data.calc_loop_triangles()
            moving=obj.name.startswith(("Steering_Wheel","SteeringSpoke"))
            if not combine or moving:
                out.write("o "+obj.name.replace(" ","_")+"\n")
                if combine: exported_objects+=1
            exported_vertices=[]
            for vertex in data.vertices:
                p=obj.matrix_world @ vertex.co
                point=tuple(round(value,6) for value in (p.x,p.z,p.y))
                exported_vertices.append(Vector(point))
                for axis in range(3):
                    minimum[axis]=min(minimum[axis],point[axis])
                    maximum[axis]=max(maximum[axis],point[axis])
                out.write("v %.6f %.6f %.6f\n" % point)
            last=-1
            for triangle in data.loop_triangles:
                index=triangle.material_index
                if index!=last and data.materials:
                    out.write("usemtl "+data.materials[min(index,len(data.materials)-1)].name+"\n")
                    last=index
                a,b,c=triangle.vertices
                # Boolean/bevel intersections occasionally emit collapsed slivers.
                # Remove only geometrically zero-area faces at the export precision.
                if (exported_vertices[b]-exported_vertices[a]).cross(exported_vertices[c]-exported_vertices[a]).length_squared<1e-18:
                    continue
                out.write("f %d %d %d\n" % (a+vertex_offset,c+vertex_offset,b+vertex_offset))
                triangles+=1
            vertex_offset+=len(data.vertices)
            evaluated.to_mesh_clear()
    return {"vertices":vertex_offset-1,"triangles":triangles,"objects":exported_objects,"sourceParts":len(objects),
            "extentM":[maximum[i]-minimum[i] for i in range(3)]}

stats={"body":export_obj(body_objects,OUT/"AsterBody.obj",True),
       "wheel":export_obj(wheel_objects,OUT/"AsterWheel.obj",True),
       "brake":export_obj(brake_objects,OUT/"AsterBrake.obj",True)}
import json
(OUT/"asset_manifest.json").write_text(json.dumps({
    "name":"Aster 1.6", "author":"Original procedural model for ImpactDrive Mobile",
    "units":"metres", "axes":"Unity X right, Y up, Z forward",
    "bodyDimensionsM":{"width":stats['body']['extentM'][0],"length":stats['body']['extentM'][2],"height":stats['body']['extentM'][1]},
    "wheelRadiusM":.31,"wheelWidthM":.205,"wheelbaseM":2.6,"trackM":1.52,
    "status":"Engineering asset; production detailing and runtime inspection pending",
    "geometry":stats},indent=2)+"\n")

# Save editable source outside Assets so Unity does not invoke Blender on import.
# Display all four wheels in the native Blender source; exported wheel stays hub-centred.
for obj in wheel_objects+brake_objects: obj.hide_render=True; obj.hide_viewport=True
for side in (-1,1):
    for axle in (-1.30,1.30):
        for original in wheel_objects+brake_objects:
            copy=original.copy();copy.data=original.data
            bpy.context.collection.objects.link(copy)
            copy.hide_render=False;copy.hide_viewport=False
            copy.location.x+=side*.76;copy.location.y+=axle;copy.location.z-=.15
            if side<0:
                # Mirror the assembly so machined outer rim faces outward on both sides.
                copy.location.x=side*.76-original.location.x
                copy.scale.x*=-1

source_dir=ROOT/"Tools/Blender/Generated"
source_dir.mkdir(exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(source_dir/"Aster.blend"))
print("ASTER_GEOMETRY "+json.dumps(stats))
