"""Preserve recovered floor state-machine bodies for isolated native tests."""
from pathlib import Path
import hashlib,json,re
root=Path(__file__).resolve().parents[2]
records={}
for name in ['TrapFloor','FallingFloor']:
 source=(root/'recovered-source'/f'{name}.cs').read_text()
 generated=source.replace(f'class {name} :',f'class {name}Reference :')
 generated=re.sub(r'\bprivate\b','public',generated)
 (Path(__file__).parent/f'{name}Reference.cs').write_text(generated)
 methods={}
 for method in ['Start','Update','StartFalling','OnDisable']:
  signature=re.search(r'\bvoid '+method+r'\([^)]*\)\s*\{',source)
  start=source.index('{',signature.start());end=start+1;depth=1
  while depth:depth+=(source[end]=='{')-(source[end]=='}');end+=1
  body=source[start:end]
  assert body in generated
  methods[method]=hashlib.sha256(body.encode()).hexdigest()
 records[name]={'source_sha256':hashlib.sha256(source.encode()).hexdigest(),'method_bodies':methods}
(root/'evidence/floor-hazards-extracted-methods.json').write_text(json.dumps({'body_edits':False,'classes':records,'adapters':'Explicit frame delta; material, sound, navigation and secondary effect collaborators. Unity Transform, Physics2D, Random and Mathf remain native.'},indent=2)+'\n')

level=(root/'recovered-source/Level.cs').read_text()
start=level.index('\t\tif (isFallingApart && allFallingFloors.Length')
init=level[start:level.index('\n\t\tif (isDarkLevel)',start)]
start=level.index('\t\tif (Singleton<GameManager>.Instance.gameState',level.index('private void Update()'))
update=level[start:level.index('\n\t\tif (Singleton<GameManager>.Instance.gameState == GameManager.GameState.Playing && isDarkLevel)',start)]
header='''using UnityEngine;using System.Collections.Generic;
public class FallingFloor:FallingFloorReference {}
public class LevelCollapseReference {
 public bool isFallingApart=true;
 public FallingFloor[] allFallingFloors;
 public List<FallingFloor> fallingFloorPlaylist=new List<FallingFloor>();
 public float fallingApartDuration=.5f,fallingApartTimer;
 public BoxCollider2D box;
 public void Init(){
'''
(Path(__file__).parent/'LevelCollapseReference.cs').write_text(header+init+'\n }\n public void Update(){\n'+update+'\n }\n}\n')
(root/'evidence/floor-collapse-extracted-methods.json').write_text(json.dumps({'source_sha256':hashlib.sha256(level.encode()).hexdigest(),'init_fragment_sha256':hashlib.sha256(init.encode()).hexdigest(),'update_fragment_sha256':hashlib.sha256(update.encode()).hexdigest(),'body_edits':False,'scope':'Original Level.Init floor playlist block and first two Level.Update collapse blocks; original FallingFloor type name retained by a subclass of its extracted implementation.'},indent=2)+'\n')
