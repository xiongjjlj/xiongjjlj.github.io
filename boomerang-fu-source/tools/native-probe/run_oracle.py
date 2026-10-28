"""Run one oracle in the existing isolated Unity copy, with a resource preflight.

Does not stage an installation or touch the original game/online services.
The selected assembly must already be registered in the installed NativeProbe.
"""
from pathlib import Path
import argparse,base64,json,subprocess
parser=argparse.ArgumentParser()
parser.add_argument('--assembly',type=Path,required=True)
parser.add_argument('--fixture',required=True)
parser.add_argument('--prefix',required=True)
parser.add_argument('--graphics',action='store_true',help='Offscreen batch-mode D3D11; keep all existing process limits')
parser.add_argument('--graphics-device',type=int,default=1,help='DXGI adapter index; arkena-host adapter 0 is Intel')
parser.add_argument('--artifact',action='append',default=[],help='Additional output basename to retrieve')
args=parser.parse_args()
assert Path(args.fixture).name==args.fixture and "'" not in args.fixture
root=Path(__file__).resolve().parents[2]
remote='arkena-host:D:/arkena/work/codex-boomerang-native-20261027/'
def powershell(source,timeout=25):
    encoded=base64.b64encode(source.encode('utf-16le')).decode()
    result=subprocess.run(['ssh','-o','BatchMode=yes','-o','ConnectTimeout=8','arkena-host','powershell -NoProfile -NonInteractive -EncodedCommand '+encoded],capture_output=True,timeout=timeout)
    assert result.returncode==0,result.stderr.decode('utf8','replace')[-2000:]
    return result.stdout.decode('utf-8-sig')
preflight=powershell("""$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; [Console]::OutputEncoding=[Text.Encoding]::UTF8; $o=Get-CimInstance Win32_OperatingSystem; $c=Get-CimInstance Win32_PerfFormattedData_PerfOS_Processor -Filter "Name='_Total'"; $p=@(Get-CimInstance Win32_PageFileUsage); $d=Get-PSDrive D; $io=Get-CimInstance Win32_PerfFormattedData_PerfDisk_PhysicalDisk -Filter "Name='_Total'"; [pscustomobject]@{freeMiB=[math]::Round($o.FreePhysicalMemory/1024);cpu=$c.PercentProcessorTime;processes=@(Get-Process).Count;pageMiB=($p|Measure-Object CurrentUsage -Sum).Sum;pageAllocatedMiB=($p|Measure-Object AllocatedBaseSize -Sum).Sum;diskFreeGiB=[math]::Round($d.Free/1GB);diskBytesPerSec=$io.DiskBytesPersec} | ConvertTo-Json -Compress""")
resources=json.loads(preflight)
(root.parents[1]/'work'/f'{args.prefix}-native-preflight.json').write_text(json.dumps(resources,indent=2)+'\n')
print(json.dumps(resources),flush=True)
assert resources['freeMiB']>=6144 and resources['cpu']<75 and resources['diskFreeGiB']>=2,'Insufficient resource headroom'
if args.graphics:
    gpu=powershell("$ErrorActionPreference='Stop'; & nvidia-smi --query-gpu=name,memory.free,memory.total,utilization.gpu --format=csv,noheader,nounits; if ($LASTEXITCODE -ne 0) { throw 'GPU query failed' }")
    rows=[line.split(',') for line in gpu.strip().splitlines()]
    assert rows and all(float(row[-3])>=1536 and float(row[-1])<75 for row in rows),'Insufficient GPU headroom'
    (root.parents[1]/'work'/f'{args.prefix}-native-gpu-preflight.json').write_text(json.dumps({'gpus':rows,'max_render_size':[1280,720],'concurrency':1},indent=2)+'\n')
    print(gpu.strip(),flush=True)
assert args.assembly.is_file() and args.assembly.name.endswith('.Native.dll')
subprocess.run(['scp','-q',str(args.assembly),remote+'NativeOracles/'+args.assembly.name],check=True,timeout=25)
script=(Path(__file__).parent/'run.ps1').read_text()
if args.graphics:
    script=script.replace('-batchmode -nographics',f'-batchmode -force-d3d11 -force-device-index {args.graphics_device}')
script=script.replace('$info.Arguments =',"$info.EnvironmentVariables['BOOMERANG_NATIVE_ORACLE']='"+args.assembly.name+"'\n$info.Arguments =")
script=script.replace('$info.Arguments =',"$info.EnvironmentVariables['BOOMERANG_SCENE_OUTPUT']='"+args.fixture+"'\n$info.Arguments =")
print(powershell(script,50),flush=True)
for source,destination in [('native-probe.json',f'{args.prefix}-unity-native-probe.json'),('native-player.log',f'{args.prefix}-unity-native-player.log'),('launch-report.json',f'{args.prefix}-native-launch.json')]:
    subprocess.run(['scp','-q',remote+source,str(root/'evidence'/destination)],check=True,timeout=25)
result=json.loads((root/'evidence'/f'{args.prefix}-unity-native-probe.json').read_text(encoding='utf-8-sig'))
launch_bytes=(root/'evidence'/f'{args.prefix}-native-launch.json').read_bytes()
launch=json.loads(launch_bytes.decode('utf-16' if launch_bytes[:2] in [b'\xff\xfe',b'\xfe\xff'] else 'utf-8-sig'))
assert launch['reason']=='normal exit' and launch['exitCode']==0 and not launch['remaining'],launch
assert launch['resultExists'],'Probe result is missing or stale'
assert not result.get('error'),result.get('error')
assert args.fixture in result['oracles']
for name in [args.fixture]+args.artifact:
    assert Path(name).name==name,'Output must be a basename'
    subprocess.run(['scp','-q',remote+name,str(root/'evidence'/name)],check=True,timeout=25)
print('Verified fixture, clean native exit, no remaining isolated process.')
