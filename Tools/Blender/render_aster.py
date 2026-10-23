"""Render the actual original Blender mesh for offline geometry inspection."""
from pathlib import Path
import bpy
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/"Tools/Blender/Generated"
bpy.ops.wm.open_mainfile(filepath=str(OUT/"Aster.blend"))
bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,-.467))
mat=bpy.data.materials.new("Inspection floor")
mat.diffuse_color=(.16,.18,.20,1)
bpy.context.object.data.materials.append(mat)
bpy.ops.object.camera_add(location=(5.1,6.5,3.25))
cam=bpy.context.object
cam.rotation_euler=(Vector((0,0,.28))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO';cam.data.ortho_scale=5.8
bpy.context.scene.camera=cam
for position,power,size in [((1,3,6),1800,5),((-4,1,3),1000,4),((1,-5,4),1700,4)]:
    bpy.ops.object.light_add(type='AREA',location=position)
    lamp=bpy.context.object
    lamp.data.energy=power;lamp.data.shape='DISK';lamp.data.size=size
    lamp.rotation_euler=(Vector((0,0,.3))-lamp.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.samples=48
scene.cycles.use_denoising=False
scene.render.resolution_x=1200;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.world.color=(.30,.30,.30)
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(OUT/'Aster-preview.png')
bpy.ops.render.render(write_still=True)
