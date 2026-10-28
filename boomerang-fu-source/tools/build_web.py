#!/usr/bin/env python3
"""Portable Godot export: one editor process at a time, without editing source presets."""
import argparse,hashlib,json,re,shutil,subprocess,sys,tempfile
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--godot',required=True);p.add_argument('--template',type=Path);p.add_argument('--output',type=Path,default=Path('dist/web'));a=p.parse_args()
root=Path(__file__).resolve().parents[1];out=a.output.resolve();template=(a.template or root/'vendor/godot-4.7.2/web_nothreads_release.zip').resolve()
if not template.is_file():raise SystemExit('Supply the matching Godot 4.7.2 single-threaded Web release template with --template')
exe=shutil.which(a.godot) or str(Path(a.godot).resolve())
version=subprocess.check_output([exe,'--version'],text=True,timeout=10).strip()
if not version.startswith('4.7.2.'):raise SystemExit(f'Expected Godot 4.7.2, got {version}')
if out.exists() and any(out.iterdir()):raise SystemExit('Output must be a new/empty directory; published exports are never overwritten')
out.mkdir(parents=True,exist_ok=True)
if shutil.disk_usage(out).free < 2*1024**3:raise SystemExit('Need 2 GiB free for the isolated build')
log=out.parent/'build.log'
with tempfile.TemporaryDirectory(prefix='bf-build-',dir=out.parent) as temp:
 project=Path(temp)/'godot';shutil.copytree(root/'godot',project,ignore=shutil.ignore_patterns('.godot'))
 preset=project/'export_presets.cfg';text=preset.read_text()
 for key in ['debug','release']:
  text=re.sub(r'custom_template/'+key+r'="[^"\n]*"','custom_template/'+key+'='+json.dumps(template.as_posix()),text)
 preset.write_text(text)
 with log.open('w') as f:
  subprocess.run([exe,'--headless','--editor','--path',str(project),'--import'],stdout=f,stderr=subprocess.STDOUT,check=True,timeout=180)
  subprocess.run([exe,'--headless','--path',str(project),'--export-release','Web',str(out/'index.html')],stdout=f,stderr=subprocess.STDOUT,check=True,timeout=180)
 errors=[s for s in log.read_text().splitlines() if 'ERROR:' in s or 'Parse Error' in s]
 if errors:raise SystemExit('\n'.join(errors))
 # The portable kit contains the exact released FMOD runtime, banks and notices.
 shutil.copytree(root/'web/audio',out/'audio')
 for file in (root/'web').glob('Godot-*.txt'):shutil.copy2(file,out/file.name)
 shutil.copy2(root/'web/README-hosting.txt',out/'README-hosting.txt')
 subprocess.run([sys.executable,str(root/'tools/patch_web_loader.py'),str(out)],check=True,timeout=15)
sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest()
report={'version':'local-development','runtime':version,'export_returncode':0,'browser_checked':False,'files':{f.relative_to(out).as_posix():{'bytes':f.stat().st_size,'sha256':sha(f)} for f in sorted(out.rglob('*')) if f.is_file()}}
(out.parent/'build-report.json').write_text(json.dumps(report,indent=2)+'\n')
print('Exported:',out,'; verify it in a browser before deployment. Report:',out.parent/'build-report.json')
