"""Extract verified level collapse settings, hierarchy order and floor materials."""
import gzip,hashlib,json
from pathlib import Path,PureWindowsPath
import UnityPy

root=Path(__file__).resolve().parents[1];task=root.parents[1]
assets=root/'godot/assets/original';data=task/'work/reference-complete/Boomerang Fu_Data'
expected={PureWindowsPath(x['path']).as_posix():x for x in json.loads((root/'evidence/original-installation-manifest.json').read_text())['files']}
trees=json.loads(gzip.decompress((task/'work/typetrees.json.gz').read_bytes()))
manifest=json.loads((assets/'manifest.json').read_text());result={};verified={};floor_materials={}
for scene in manifest['scenes']:
 name=Path(scene['file']).stem;side=json.loads((assets/name/'behaviors.json').read_text())
 records=[x for x in side['behaviours'] if x['class'] in ['FallingFloor','TrapFloor']]
 if not records:continue
 cache={}
 def load(file):
  if file not in cache:
   path=data/file;digest=hashlib.file_digest(path.open('rb'),'sha256').hexdigest()
   assert path.stat().st_size==expected[file]['bytes'] and digest.lower()==expected[file]['sha256'].lower(),file
   verified[file]=digest;cache[file]=UnityPy.load(str(path)).file
  return cache[file]
 def resolve(file,ref):
  if ref['m_FileID']:file=Path(load(file).externals[ref['m_FileID']-1].path).name
  return load(file).objects[ref['m_PathID']]
 source=load(name);level=None
 for obj in source.objects.values():
  if obj.type.name!='MonoBehaviour':continue
  head=obj.read_typetree(check_read=False);script=resolve(name,head['m_Script']).read_typetree()
  if script['m_ClassName']=='Level':
   assert level is None
   level=obj.read_typetree(nodes=trees['Assembly-CSharp::Level'])
 assert level is not None,name
 box=resolve(name,level['box']).read_typetree();go=level['m_GameObject']['m_PathID']
 owners={int(k):v for k,v in side['component_owners'].items()};transforms={}
 for obj in source.objects.values():
  if obj.type.name=='Transform':
   value=obj.read_typetree();transforms[value['m_GameObject']['m_PathID']]=value
 by_go={r['game_object']:r for r in records};order=[]
 def visit(owner):
  r=by_go.get(owner)
  if r and r['class']=='FallingFloor' and r['active_in_hierarchy']:order.append(r['component_id'])
  for child in transforms.get(owner,{}).get('m_Children',[]):visit(owners[child['m_PathID']])
 visit(go)
 assert len(order)==sum(r['class']=='FallingFloor' and r['active_in_hierarchy'] for r in records)
 materials={}
 for rec in records:
  renderer=resolve(name,rec['data']['floorRenderer']).read_typetree()
  material=resolve(name,renderer['m_Materials'][0]);md=material.read();shader=md.m_Shader.read()
  colors={k:{c:getattr(v,c) for c in 'rgba'} for k,v in md.m_SavedProperties.m_Colors}
  material_id=f'{material.assets_file.name}_{material.path_id}'
  materials[str(rec['component_id'])]={'flash':colors.get('_FlashColor',{'r':1,'g':1,'b':1,'a':0}),'shader':shader.m_ParsedForm.m_Name,'material':material_id}
  if material_id not in floor_materials:
   textures={}
   for key,tex in md.m_SavedProperties.m_TexEnvs:
    entry={'scale':[tex.m_Scale.x,tex.m_Scale.y],'offset':[tex.m_Offset.x,tex.m_Offset.y],'texture':None}
    if tex.m_Texture.path_id:
     reader=resolve(material.assets_file.name,{'m_FileID':tex.m_Texture.file_id,'m_PathID':tex.m_Texture.path_id})
     target=assets/'shader-textures'/f'{reader.assets_file.name}_{reader.path_id}.png';reader.read().image.save(target)
     entry['texture']={'path':str(target.relative_to(assets)),'sha256':hashlib.sha256(target.read_bytes()).hexdigest()}
    textures[key]=entry
   floor_materials[material_id]={'shader':shader.m_ParsedForm.m_Name,'textures':textures}
 result[name]={'collapsing':level['isFallingApart'],'duration':level['fallingApartDuration'],'box_size':box['m_Size'],'falling_order':order,'materials':materials}
 print(name,'falling',len(order),'traps',sum(r['class']=='TrapFloor' for r in records),flush=True)
 cache.clear()
(assets/'floor-inputs.json').write_text(json.dumps(result,separators=(',',':'))+'\n')
(assets/'floor-materials.json').write_text(json.dumps(floor_materials,indent=2)+'\n')
(root/'evidence/floor-inputs-extraction.json').write_text(json.dumps({'maps':len(result),'verified_files':verified,'output_sha256':hashlib.sha256((assets/'floor-inputs.json').read_bytes()).hexdigest()},indent=2)+'\n')
