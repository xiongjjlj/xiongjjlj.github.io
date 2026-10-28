#!/usr/bin/env python3
"""Smoke-test imported levels through the shared Godot startup path."""
from __future__ import annotations
import argparse,json,re,subprocess,datetime,hashlib
from pathlib import Path

ap=argparse.ArgumentParser();ap.add_argument('--godot',required=True);ap.add_argument('--project',required=True);ap.add_argument('--output',required=True);args=ap.parse_args()
maps=[f'level{i}' for i in range(2,61)]
ready=re.compile(r'BF_GODOT_READY (\{.*\})')
rows=[]
project=Path(args.project)
source_sha256={str(p.relative_to(project)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted([project/'main.gd',*project.glob('core/*.gd'),*project.glob('assets/original/level*/behaviors.json'),*project.glob('assets/original/navigation/*.json')])}
for name in maps:
    try:
        p=subprocess.run([args.godot,'--headless','--path',args.project,'--quit-after','60','--','--map='+name],capture_output=True,text=True,timeout=45)
        log=p.stdout+'\n'+p.stderr
        match=ready.search(log)
        errors=[line.strip() for line in log.splitlines() if re.search(r'ERROR:|SCRIPT ERROR|Parse Error|Invalid call',line)]
        data=json.loads(match.group(1)) if match else {}
        rows.append({'map':name,'returncode':p.returncode,'scene':data.get('scene'),'scene_index':data.get('scene_index'),'nodes':data.get('nodes'),'meshes':data.get('meshes'),'missing':data.get('missing'),'errors':errors,'passed':p.returncode==0 and bool(match) and not errors})
    except subprocess.TimeoutExpired as exc:
        rows.append({'map':name,'returncode':None,'scene':None,'errors':['timeout'],'passed':False})
report={'passed':all(x['passed'] for x in rows),'maps':rows,'map_count':len(rows),'source_sha256':source_sha256,'scope':'Imported Unity build levels 2-60 (including tutorial content) through Godot headless startup; this proves loading and shared FFA initialization, not FFA eligibility, full scene-specific behavior or visual parity.','generated_at':datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=8))).isoformat(timespec='seconds')}
Path(args.output).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(report,ensure_ascii=False))
raise SystemExit(0 if report['passed'] else 1)
