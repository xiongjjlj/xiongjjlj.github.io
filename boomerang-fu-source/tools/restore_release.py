#!/usr/bin/env python3
"""Restore a pinned release cache, verifying archive and every exported file."""
import argparse,hashlib,json,shutil,subprocess,tempfile,zipfile
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('version');p.add_argument('cache',type=Path);a=p.parse_args()
root=Path(__file__).resolve().parents[1]
lock=json.loads((root/'deploy/cloudflare/releases'/f'{a.version}.json').read_text())
a.cache.mkdir(parents=True,exist_ok=True)
if shutil.disk_usage(a.cache).free < 600*1024*1024:raise SystemExit('Need at least 600 MiB free for one release restore')
archive=lock['archive'];target=a.cache/a.version
with tempfile.TemporaryDirectory(prefix='bf-release-',dir=a.cache) as d:
 tmp=Path(d);z=tmp/'web.zip'
 subprocess.run(['curl','--fail','--location','--max-time','300','--retry','1','--output',str(z),archive['url']],check=True,timeout=620)
 assert z.stat().st_size==archive['bytes'] and hashlib.sha256(z.read_bytes()).hexdigest()==archive['sha256'],'Archive integrity failed'
 with zipfile.ZipFile(z) as f:
  assert sorted(f.namelist())==sorted(lock['files']), 'Unexpected archive paths'
  for name,expected in lock['files'].items():
   assert not Path(name).is_absolute() and '..' not in Path(name).parts
   data=f.read(name);assert len(data)==expected['bytes'] and hashlib.sha256(data).hexdigest()==expected['sha256'],name
   out=tmp/'verified'/name;out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(data)
 if target.exists():
  for name,expected in lock['files'].items():
   assert hashlib.sha256((target/name).read_bytes()).hexdigest()==expected['sha256'],'Existing cache differs; will not overwrite'
 else:(tmp/'verified').rename(target)
print('Verified release cache:',target)
