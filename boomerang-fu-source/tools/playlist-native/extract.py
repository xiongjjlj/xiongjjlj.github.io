"""Keep the original playlist method bodies intact for Mono comparisons."""
from pathlib import Path
import hashlib,json,re

root=Path(__file__).resolve().parents[2]
out=Path(__file__).parent
source=(root/'recovered-source/LevelManager.cs').read_text()
names=['BuildMatchPlaylist','FilterLevelsAndAddLevelToPlaylist','AddLevelToPlaylist',
       'IsLevelDisabledInMatchSettings','MoveItemInPlaylist']
methods=[];hashes=[]
for name in names:
 for match in re.finditer(r'\b(?:public|private) (?:void|bool) '+name+r'\([^)]*\)\s*\{',source):
  start=source.index('{',match.start());end=start+1;depth=1
  while depth:depth+=(source[end]=='{')-(source[end]=='}');end+=1
  method=source[match.start():end]
  methods.append(method.replace('private ','public ',1))
  hashes.append({'method':method[:method.index('{')].strip(),'body_sha256':hashlib.sha256(source[start:end].encode()).hexdigest()})
header='''using System;using System.Collections.Generic;using System.Linq;using UnityEngine;
public class LevelManagerReference {
 public List<LevelAsset> levelAssets=new List<LevelAsset>(),switchIncompatibleLevels=new List<LevelAsset>(),matchPlaylist=new List<LevelAsset>();
 public int matchIndex,powerupsCollectedSoFar,lastMatchIndexBeforePowerupsSpawn,powerupBuilds;
 public void BuildPossiblePowerups(){powerupBuilds++;}
'''
start=source.index('\t\tcurrentScene = matchPlaylist[matchIndex % matchPlaylist.Count].path;')
fragment=source[start:source.index('\n\t\tyield return SceneManager.LoadSceneAsync',start)]
consume='\n public string currentScene; public int Consume(){ if(matchPlaylist.Count==0)return -1;int chosen=matchPlaylist[matchIndex%matchPlaylist.Count].id;\n'+fragment+'\nreturn chosen;}\n'
(out/'Reference.cs').write_text(header+'\n'.join(methods)+consume+'\n}\n')
hashes.append({'method':'LoadNewLevel playlist selection/removal fragment','body_sha256':hashlib.sha256(fragment.encode()).hexdigest()})
chance=(root/'recovered-source/Chance.cs').read_text().replace('private static System.Random rng','public static System.Random rng')
(out/'ChanceReference.cs').write_text(chance)
catalog=json.loads((root/'godot/assets/original/map-catalog.json').read_text())
rows=[]
for x in catalog['entries']:
 rows.append('new LevelAsset { id=%d,path=%s,minPlayers=%d,maxPlayers=%d,complexity=(LevelAsset.Complexity)%d,requiredDLC=(DownloadableContentID)%d,supportedMatchTypes=new List<SettingsManager.MatchType>{%s} }'%(
  x['catalog_index'],json.dumps(x['path']),x['min_players'],x['max_players'],x['complexity'],x['required_dlc'],','.join('(SettingsManager.MatchType)'+str(n) for n in x['supported_match_types'])))
manager=next(x['data'] for x in json.loads((root/'evidence/source-level-managers.json').read_text()) if x['class']=='LevelManager')
ids={x['source_id']:x['catalog_index'] for x in catalog['entries']}
switch=[ids[r['m_PathID']] for r in manager['switchIncompatibleLevels']]
(out/'Catalog.cs').write_text('using System.Collections.Generic; public static class Catalog { public static int[] SwitchIncompatible=new int[]{'+','.join(map(str,switch))+'}; public static List<LevelAsset> Create()=>new List<LevelAsset>{\n'+',\n'.join(rows)+'\n};}\n')
(root/'evidence/playlist-extracted-methods.json').write_text(json.dumps({'body_edits':False,'source_sha256':hashlib.sha256(source.encode()).hexdigest(),'chance_source_sha256':hashlib.sha256((root/'recovered-source/Chance.cs').read_bytes()).hexdigest(),'methods':hashes,'catalog_sha256':hashlib.sha256((root/'godot/assets/original/map-catalog.json').read_bytes()).hexdigest(),'scope':'Original LevelManager playlist method bodies and Chance.Shuffle; catalog/settings/entitlement are explicit fixture data, BuildPossiblePowerups is a call counter. Mono System.Random is seeded for repeatability; live seed timing remains separate.'},indent=2)+'\n')
print('Extracted',len(methods),'unchanged playlist bodies; Switch exclusions',switch)
