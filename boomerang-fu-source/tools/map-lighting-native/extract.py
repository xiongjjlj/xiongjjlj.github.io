"""Generate native Unity lighting inputs from the verified imported scene graph."""
from pathlib import Path
import json,hashlib
r=Path(__file__).resolve().parents[2];assets=r/'godot/assets/original';m=json.loads((assets/'manifest.json').read_text());lines=[];hashes={}
f=lambda v:repr(float(v))+'f'
for scene in m['scenes']:
 path=assets/scene['file'];hashes[scene['file']]=hashlib.sha256(path.read_bytes()).hexdigest();s=json.loads(path.read_text());nodes={int(n['id']):n for n in s['nodes']};transforms={int(c['id']):(n,c['data']) for n in s['nodes'] for c in n['components'] if c['type']=='Transform'}
 owners={int(n['id']):(n,c['data']) for n in s['nodes'] for c in n['components'] if c['type']=='Transform'};lights=[]
 for n in s['nodes']:
  for c in n['components']:
   if c['type']!='Light':continue
   d=c['data'];chain=[];owner,transform=owners[int(n['id'])];active=True
   while True:
    active=active and owner['active'];q=transform['m_LocalRotation'];chain.insert(0,'new Quaternion('+','.join(f(q[x]) for x in 'xyzw')+')');parent=int(transform['m_Father']['m_PathID'])
    if not parent:break
    owner,transform=transforms[parent]
   if not active or not d['m_Enabled'] or int(d['m_Type'])!=1:continue
   if not int(d['m_CullingMask']['m_Bits'])&1:continue # All 44 imported FX water renderers use layer 0.
   color=d['m_Color'];lights.append('new Lamp { name='+json.dumps(n['name'])+',color=new Color('+','.join(f(color[x]) for x in 'rgba')+'),intensity='+f(d['m_Intensity'])+',vertex='+str(int(d['m_RenderMode'])==2).lower()+',shadow='+str(int(d['m_Shadows']['m_Type'])!=0).lower()+',rotations=new Quaternion[]{'+','.join(chain)+'} }')
 lines.append('new Map { key='+json.dumps(Path(scene['file']).stem)+',name='+json.dumps(scene['name'])+',lights=new Lamp[]{'+','.join(lights)+'} }')
(Path(__file__).parent/'Catalog.cs').write_text('using UnityEngine;\npublic static class Catalog { public class Lamp { public string name;public Color color;public float intensity;public bool vertex,shadow;public Quaternion[] rotations;} public class Map {public string key,name;public Lamp[] lights;} public static Map[] Maps={\n'+',\n'.join(lines)+'\n};}\n')
(r/'evidence/map-lighting-inputs-source.json').write_text(json.dumps({'scenes':len(lines),'scene_sha256':hashes,'scope':'Serialized active directional light hierarchy affecting source layer 0 (water). Base ambient comes from Level.SetAmbientLight; seasonal/night changes are not included.'},indent=2)+'\n')
print('Generated',len(lines),'map lighting inputs')
