#!/usr/bin/env python3
"""Read source graph physics settings in native deserialization order."""
import argparse,hashlib,json,re,zipfile
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--graphs',type=Path,required=True);args=p.parse_args()
root=Path(__file__).resolve().parents[1];ev=root/'evidence'
records=json.loads((ev/'navigation-graph-extraction.json').read_text())['maps']
native={m['key']:m for batch in range(10) for m in json.loads((ev/f'navigation-maps-native-batch{batch:02}.json').read_text())['maps']}
output={}
for item in records:
 path=args.graphs/item['graph'];assert hashlib.sha256(path.read_bytes()).hexdigest()==item['sha256']
 with zipfile.ZipFile(path) as z:
  names=sorted((n for n in z.namelist() if re.fullmatch(r'graph\d+\.json',n)),key=lambda n:int(n[5:-5]))
  assert len(names)==len(native[item['map']]['graphs'])
  settings=[]
  for index,name in enumerate(names):
   data=json.loads(z.read(name));graph=native[item['map']]['graphs'][index]
   assert abs(float(data['nodeSize'])-float(graph['node_size']))<0.000001
   assert data['erodeIterations']==0 and data['neighbours']=='Eight'
   assert data['collision']['use2D'] and not data['penaltyPosition'] and not data['penaltyAngle']
   settings.append({'index':index,'serialized_index':int(name[5:-5]),'collision':data['collision'],'initial_penalty':data['initialPenalty'],'cut_corners':data['cutCorners'],'max_climb':data['maxClimb'],'erosion':data['erodeIterations'],'neighbours':data['neighbours']})
  output[item['map']]=settings
report={'maps':output,'source':'Unmodified graph JSON entries in original serialized Astar ZIP files','source_hashes_verified':True,'runtime_index_order':'Non-null serialized graphs sorted by original index, verified against native graph counts.'}
(ev/'navigation-graph-settings.json').write_text(json.dumps(report,indent=2)+'\n')
print({'maps':len(output),'graphs':sum(map(len,output.values())),'static_diameter_one':[k for k,v in output.items() if v[0]['collision']['diameter']==1]})
