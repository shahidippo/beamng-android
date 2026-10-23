#!/usr/bin/env python3
"""Repository/asset integrity checks. These do not replace Unity import or Android tests."""
import json
import math
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
errors = []
checks = 0


def check(condition, message):
    global checks
    checks += 1
    if not condition:
        errors.append(message)


required = [
    'Packages/manifest.json', 'ProjectSettings/ProjectVersion.txt',
    'ProjectSettings/ProjectSettings.asset', 'ProjectSettings/EditorBuildSettings.asset',
    'Assets/ImpactDrive/Scenes/ProvingGround.unity',
    'Assets/ImpactDrive/Runtime/Bootstrap.cs',
    'Assets/ImpactDrive/Editor/BuildAndroid.cs',
    'Assets/ImpactDrive/Runtime/World/ProvingGroundBuilder.cs',
    'Assets/ImpactDrive/Runtime/Presentation/GameplayPresentation.cs',
    'Tests/Physics/ImpactDrive.Physics.Tests.csproj',
]
for name in required:
    check((root / name).is_file(), f'Missing required project file: {name}')

manifest = json.loads((root / 'Packages/manifest.json').read_text())
for name, version in manifest['dependencies'].items():
    check(bool(re.fullmatch(r'\d+\.\d+\.\d+', version)), f'Unpinned package: {name}')

guid_paths = {}
for path in (root / 'Assets').rglob('*.meta'):
    match = re.search(r'^guid: ([a-f0-9]{32})$', path.read_text(), re.M)
    check(match is not None, f'Invalid metadata GUID: {path.relative_to(root)}')
    if match:
        guid = match.group(1)
        check(guid not in guid_paths, f'Duplicate GUID: {guid}')
        guid_paths[guid] = path

for path in (root / 'Assets').rglob('*'):
    if not path.is_file() or path.suffix == '.meta' or 'Generated' in path.parts:
        continue
    check(path.with_name(path.name + '.meta').is_file(), f'Missing asset metadata: {path.relative_to(root)}')
    if path.suffix == '.unity':
        for guid in re.findall(r'guid: ([a-f0-9]{32})', path.read_text()):
            check(guid in guid_paths, f'Scene references unknown GUID {guid}')

assemblies = {}
for path in (root / 'Assets').rglob('*.asmdef'):
    definition = json.loads(path.read_text())
    check(definition['name'] not in assemblies, f'Duplicate assembly: {definition["name"]}')
    assemblies[definition['name']] = definition
for name, definition in assemblies.items():
    for reference in definition.get('references', []):
        if reference.startswith('ImpactDrive.'):
            check(reference in assemblies, f'{name} references missing assembly {reference}')

for path in (root / 'Assets/ImpactDrive/Art').rglob('*.obj'):
    vertices = []
    faces = []
    for line in path.read_text().splitlines():
        fields = line.split()
        if fields and fields[0] == 'v':
            vertices.append(tuple(map(float, fields[1:4])))
        elif fields and fields[0] == 'f':
            faces.append([int(field.split('/')[0]) for field in fields[1:]])
    check(len(vertices) >= 3 and len(faces) > 0, f'Empty mesh: {path.name}')
    check(all(math.isfinite(v) for xyz in vertices for v in xyz), f'Nonfinite mesh coordinates: {path.name}')
    check(all(len(face) >= 3 and all(1 <= i <= len(vertices) for i in face) for face in faces), f'Invalid mesh index: {path.name}')
    print(f'{path.name}: {len(vertices)} vertices, {sum(len(f)-2 for f in faces)} triangles')

for error in errors:
    print('FAIL:', error)
print(f'Project integrity: {checks-len(errors)}/{checks} checks passed.')
raise SystemExit(1 if errors else 0)
