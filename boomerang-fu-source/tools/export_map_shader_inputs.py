"""Export all scene material inputs from hash-verified installation files.

One process, at most three open asset environments. Null shaders are preserved
as null; unsupported texture types are recorded, never guessed or substituted.
"""
from pathlib import Path, PureWindowsPath
from collections import OrderedDict, Counter
import hashlib, json, time
import UnityPy

root=Path(__file__).resolve().parents[1]; task=root.parents[1]
assets=root/'godot/assets/original'; data=task/'work/reference-complete/Boomerang Fu_Data'
expected={PureWindowsPath(x['path']).as_posix():x for x in json.loads((root/'evidence/original-installation-manifest.json').read_text())['files']}
manifest=json.loads((assets/'manifest.json').read_text()); sprites=json.loads((assets/'sprite-inputs.json').read_text())
cache=OrderedDict(); verified={}; textures={}; result={}; started=time.monotonic()
def sha(path):
 with path.open('rb') as handle:return hashlib.file_digest(handle,'sha256').hexdigest()
def verify(name):
 if name not in verified:
  path=data/name; row=expected[name]; digest=sha(path)
  assert path.stat().st_size==row['bytes'] and digest.lower()==row['sha256'].lower(),name
  verified[name]=digest
 return data/name
def load(name):
 if name not in cache:
  while len(cache)>=3:cache.popitem(last=False)
  cache[name]=UnityPy.load(str(verify(name))).file
 else:cache.move_to_end(name)
 return cache[name]
def resolve(file,ref):
 if not ref['m_PathID']:return None
 if ref['m_FileID']:file=PureWindowsPath(load(file).externals[ref['m_FileID']-1].path).name
 return load(file).objects[ref['m_PathID']]
def texture_record(reader):
 if reader is None:return None
 key=f'{reader.assets_file.name}_{reader.path_id}'
 if key in textures:return textures[key]
 original=reader.read(); record={'source_file':reader.assets_file.name,'source_id':reader.path_id,'type':reader.type.name,'name':original.m_Name}
 if reader.type.name=='Texture2D':
  stream=original.m_StreamData
  if stream and stream.size:verify(PureWindowsPath(stream.path).name)
  target=assets/'shader-textures'/f'{key}.png';target.parent.mkdir(exist_ok=True)
  image=original.image
  import io
  buffer=io.BytesIO();image.save(buffer,format='PNG');encoded=buffer.getvalue()
  existing=manifest['textures'].get(key,{})
  if existing.get('path') and (assets/existing['path']).is_file() and sha(assets/existing['path'])==hashlib.sha256(encoded).hexdigest():target=assets/existing['path']
  assert image.width*image.height<=4096*4096,'Texture exceeds single-image budget'
  if target.exists():
   assert hashlib.sha256(encoded).hexdigest()==sha(target),key
  else:image.save(target)
  image.close(); settings=original.m_TextureSettings
  record.update(path=target.relative_to(assets).as_posix(),sha256=sha(target),width=original.m_Width,height=original.m_Height,filter=settings.m_FilterMode,wrap_u=settings.m_WrapU,wrap_v=settings.m_WrapV,aniso=settings.m_Aniso)
 textures[key]=record
 return record
for key in sorted({**manifest['materials'],**sprites['materials']}):
 assert time.monotonic()-started<180,'Extraction time budget exceeded'
 file,pid=key.rsplit('_',1);file=Path(file).name;reader=load(file).objects[int(pid)];tree=reader.read_typetree()
 shader=resolve(file,tree['m_Shader']);kind=None if shader is None else shader.read_typetree()['m_ParsedForm']['m_Name']
 entry={'source_material':key,'shader':kind,'textures':{},'valid_keywords':tree.get('m_ValidKeywords',[]),'render_queue':tree['m_CustomRenderQueue']}
 for name,env in tree['m_SavedProperties']['m_TexEnvs']:
  entry['textures'][name]={'scale':[env['m_Scale']['x'],env['m_Scale']['y']],'offset':[env['m_Offset']['x'],env['m_Offset']['y']],'texture':texture_record(resolve(file,env['m_Texture']))}
 result[key]=entry
cache.clear()
target=assets/'map-shader-inputs.json';target.write_text(json.dumps(result,indent=2)+'\n')
report={'passed':True,'material_count':len(result),'texture_count':len(textures),'shader_counts':dict(Counter(str(x['shader']) for x in result.values())),'null_shader_materials':[k for k,v in result.items() if v['shader'] is None],'non_2d_textures':[v for v in textures.values() if v['type']!='Texture2D'],'verified_files':verified,'output_sha256':sha(target),'runtime_connected_at_extraction':False,'visual_parity_verified':False,'limits':{'processes':1,'cached_asset_files':3,'timeout_seconds':180,'texture_pixels':4096*4096}}
(root/'evidence/map-shader-inputs-extraction.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='verified_files'},indent=2))
