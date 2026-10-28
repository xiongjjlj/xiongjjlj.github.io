#!/usr/bin/env python3
import hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
here=Path(__file__).parent
source=(root/'recovered-source/LevelManager.cs').read_text()
start=source.index('\tpublic GraphMask GetBestGraphForTarget(');end=source.index('{',start)+1;depth=1
while depth:depth+=(source[end]=='{')-(source[end]=='}');end+=1
body=source[start:end]
(here/'Reference.cs').write_text('using UnityEngine; using Pathfinding; public partial class LevelManager {\n'+body+'\n}\n')
(root/'evidence/navigation-selection-method.json').write_text(json.dumps({'source':'recovered-source/LevelManager.cs','method':'GetBestGraphForTarget','sha256':hashlib.sha256(body.encode()).hexdigest(),'body_edits':False},indent=2)+'\n')
maps={r['map']:r for r in json.loads((root/'evidence/navigation-graph-extraction.json').read_text())['maps']}
groups={}
for filename in ['moving-floor-native-input.json','reactive-floor-native-input.json']:
 for case in json.loads((root/'evidence'/filename).read_text())['cases']:
  groups.setdefault(case['key'].split('-')[0],[]).append((case,filename.startswith('reactive')))
def v(data,kind):return 'new '+kind+'('+','.join(repr(float(data[k]))+'f' for k in ('xyzw' if kind=='Quaternion' else 'xyz'))+')'
entries=[]
for key,items in groups.items():
 floors=[]
 for case,reactive in items:
  assert all(list(p['scale'].values())==[1.0,1.0,1.0] for p in case['chain']), 'Platform inverse requires source unit-scale hierarchy: '+case['key']
  poses=['new Pose{position='+v(p['position'],'Vector3')+',rotation='+v(p['rotation'],'Quaternion')+',scale='+v(p['scale'],'Vector3')+'}' for p in case['chain']]
  floors.append('new FloorCase{id='+case['key'].split('-')[1]+',mask='+str(case['data']['graphMask']['value'])+',reactive='+str(reactive).lower()+',rotating='+str(bool(case['data']['useRotations'])).lower()+',chain=new Pose[]{'+','.join(poses)+'}}')
 entries.append('new MapCase{key='+json.dumps(key)+',file='+json.dumps(maps[key]['graph'])+',floors=new FloorCase[]{'+','.join(floors)+'}}')
(here/'Cases.cs').write_text('using UnityEngine; public static class MapCases {public static MapCase[] Create()=>new MapCase[]{'+',\n'.join(entries)+'};}\n')
print('Generated',len(entries),'maps and',sum(len(x) for x in groups.values()),'platforms')
