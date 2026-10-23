# Original Aster engineering assets

All meshes, colours and component layouts in this directory were authored for this
project with `Tools/Blender/create_aster.py`; there are no third-party vehicle assets,
textures or sounds. The editable native source is `Tools/Blender/Generated/Aster.blend`.

The resource OBJs use metres with +X right, +Y up and +Z forward. `AsterBody.obj`
contains the shell, interior and separable steering wheel. `AsterWheel.obj` contains
the tyre, rim and rotating disc; `AsterBrake.obj` contains the nonrotating caliper.
Static parts are merged into multi-material meshes to reduce renderer count.
The mesh tyre radius is 0.31 m; the factory scales it to the configured 0.305 m.
Hubs use independent front/rear track values from the editable `AsterSpec.json`.

Regenerate from the repository root:

```sh
blender --background --factory-startup --python Tools/Blender/create_aster.py
python3 Tools/Blender/validate_assets.py
blender --background --factory-startup --python Tools/Blender/render_aster.py
```

The Blender still is a geometry inspection render, **not a game screenshot**.
The current vehicle is an engineering asset. Production detailing, opening body
panels, authored LODs, full suspension hardpoint geometry, UV/texturing work,
detachable components and Android rendering validation remain outstanding.

`ProvingGroundBuilder` creates a finite 640 × 800 m engineering test site: a 600 m
straight, physical end wall, connected test lanes, skidpad, bumps, 10% gradient and
service shelter. This is not one of the completed open-world launch maps. Grass,
wet asphalt, gravel and ice select force-model parameters; terrain sinkage, weather,
world streaming and navigation are not implemented by these assets.
