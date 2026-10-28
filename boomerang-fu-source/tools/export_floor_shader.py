"""Extract the original floor D3D11 shader for the bounded native disassembler."""
from pathlib import Path,PureWindowsPath
import hashlib,json
import UnityPy
from UnityPy.helpers import CompressionHelper
root=Path(__file__).resolve().parents[1];task=root.parents[1]
source=task/'work/reference-complete/Boomerang Fu_Data/sharedassets14.assets'
expected=next(x for x in json.loads((root/'evidence/original-installation-manifest.json').read_text())['files'] if PureWindowsPath(x['path']).name==source.name)
digest=hashlib.file_digest(source.open('rb'),'sha256').hexdigest()
assert digest.lower()==expected['sha256'].lower() and source.stat().st_size==expected['bytes']
output=task/'work/floor-shaders';output.mkdir(exist_ok=True)
reader=UnityPy.load(str(source)).file.objects[29];shader=reader.read();tree=reader.read_typetree()
blob=bytes(shader.compressedBlob);offset=shader.offsets[0][0]
raw=CompressionHelper.decompress_lz4(blob[offset:offset+shader.compressedLengths[0][0]],shader.decompressedLengths[0][0])
(output/'shader-sharedassets14-29-blob.bin').write_bytes(raw)
(output/'sharedassets14-29-parsed.json').write_text(json.dumps(tree['m_ParsedForm'],indent=1)+'\n')
(root/'evidence/floor-shader-extraction.json').write_text(json.dumps({'source':source.name,'source_sha256':digest,'shader_id':29,'shader':shader.m_ParsedForm.m_Name,'bytes':len(raw),'blob_sha256':hashlib.sha256(raw).hexdigest()},indent=2)+'\n')
