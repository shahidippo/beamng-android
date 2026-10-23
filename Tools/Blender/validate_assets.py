#!/usr/bin/env python3
"""Validate exported original mesh indices, scale, extent and material references.

These structural checks do not validate Unity import, visual deformation, frame rate
or physical driving behaviour. They are deliberately independent of Blender.
"""
from pathlib import Path
import json
import math

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/"Assets/ImpactDrive/Art/Resources/ImpactDrive"

def inspect(name):
    path=OUT/(name+".obj")
    vertices=[];triangles=[];objects=[];used=set()
    for line in path.read_text().splitlines():
        token=line.split()
        if not token:continue
        if token[0]=='v':
            point=tuple(map(float,token[1:4]))
            assert all(math.isfinite(v) for v in point), (name,'nonfinite vertex')
            vertices.append(point)
        elif token[0]=='f': triangles.append(tuple(int(v.split('/')[0])-1 for v in token[1:]))
        elif token[0]=='o': objects.append(token[1])
        elif token[0]=='usemtl':used.add(token[1])
    assert vertices and triangles and objects,(name,'empty geometry')
    for triangle in triangles:
        assert len(triangle)==3 and len(set(triangle))==3,(name,'degenerate index')
        assert all(0<=i<len(vertices) for i in triangle),(name,'invalid index')
        a,b,c=(vertices[i] for i in triangle)
        u=[b[i]-a[i] for i in range(3)];v=[c[i]-a[i] for i in range(3)]
        normal=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
        assert sum(x*x for x in normal)>=1e-18,(name,'zero-area triangle')
    mats={line.split()[1] for line in path.with_suffix('.mtl').read_text().splitlines() if line.startswith('newmtl ')}
    assert used<=mats,(name,'missing material')
    extents=[max(p[i] for p in vertices)-min(p[i] for p in vertices) for i in range(3)]
    return {'vertices':len(vertices),'triangles':len(triangles),'objects':len(objects),'extentM':extents},vertices,objects

def main():
    body,vertices,names=inspect('AsterBody')
    wheel,wv,wn=inspect('AsterWheel')
    brake,bv,bn=inspect('AsterBrake')
    assert 3.8<body['extentM'][2]<4.4,'Body length must be in metres'
    assert 1.6<body['extentM'][0]<2.3,'Body width must be in metres'
    assert 1.0<body['extentM'][1]<1.5,'Body height must be in metres'
    radial=max(math.hypot(p[1],p[2]) for p in wv)
    assert .309<radial<.311,'Unexpected tyre outside radius'
    assert .19<wheel['extentM'][0]<.24,'Unexpected tyre width'
    assert any('Steering_Wheel' in n for n in names),'Steering wheel not separable'
    assert wheel['objects']==1,'Wheel parts should be merged for rendering'
    spec=json.loads((OUT/'AsterSpec.json').read_text())
    assert abs(spec['WheelbaseM']-2.6)<1e-6,'Axles mismatch original body wheel arches'
    assert 0.28<spec['WheelRadiusM']<.33,'Unsupported tyre size for authored arches'
    assert spec['FrontTrackM']>=spec['RearTrackM']>1.4
    manifest=json.loads((OUT/'asset_manifest.json').read_text())
    for key,actual in [('body',body),('wheel',wheel),('brake',brake)]:
        for metric in ('vertices','triangles','objects'):
            assert manifest['geometry'][key][metric]==actual[metric],(key,metric,'stale manifest')
    result={'status':'pass','checks':['mesh indices','nondegenerate triangles','finite coordinates','material references','SI dimensions',
            'tyre radius','steering separation','merged wheel geometry','spec axle/tyre bounds','manifest counts'],
            'body':body,'wheel':wheel,'brake':brake,'maximumWheelRadiusM':radial}
    print(json.dumps(result,indent=2))

if __name__=='__main__':main()
