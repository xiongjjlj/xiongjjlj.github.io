#!/usr/bin/env python3
"""Decode selected Unity scene behaviours, verifying source bytes before reading.

Refs keep Unity's m_FileID/m_PathID format. resolved_refs only maps same-file
component/GameObject refs; external MonoScripts never alias local object IDs.
"""
import argparse, base64, collections, gzip, hashlib, json
from pathlib import Path, PurePosixPath, PureWindowsPath

FAMILIES = set('MovingObstacle MovingObstacleRollingPin MovingObstacleButter Switch WobblyObstacle DestructableObstacle Debris3D FallingFloor Teleportal MovingFloor MovingFloorReactive MovingFloorRollingPin MovingDoor TrapFloor RotatingObstacleController AutoRotate AnimatedObstacle ExpressiveObstacle GoldenTotem Border ShrubWobbleCollider DestructableObstacleDestroyer'.split())

def main():
    import UnityPy
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--data', required=True, type=Path)
    ap.add_argument('--manifest', required=True, type=Path)
    ap.add_argument('--typetrees', required=True, type=Path)
    ap.add_argument('--output', required=True, type=Path)
    args = ap.parse_args()
    expected = {PureWindowsPath(x['path']).as_posix(): x for x in json.loads(args.manifest.read_text())['files']}
    raw = args.typetrees.read_bytes()
    trees = json.loads(gzip.decompress(raw) if args.typetrees.suffix == '.gz' else raw)
    cache, verified = {}, {}
    def load(name):
        if name not in cache:
            path = args.data / name
            with path.open('rb') as stream:
                digest = hashlib.file_digest(stream, 'sha256').hexdigest()
            entry = expected[name]
            if path.stat().st_size != entry['bytes'] or digest.lower() != entry['sha256'].lower():
                raise ValueError('Unverified source: ' + name)
            verified[name] = digest
            cache[name] = UnityPy.load(str(path))
        return cache[name].file
    scenes = json.loads((args.output / 'manifest.json').read_text())['scenes']
    staged = []
    for scene in scenes:
        name = Path(scene['file']).stem
        source = load(name)
        objects = source.objects
        owners, go_data, transforms = {}, {}, {}
        for obj in objects.values():
            if obj.type.name == 'GameObject':
                go = obj.read_typetree()
                go_data[obj.path_id] = go
                for ref in go['m_Component']:
                    owners[ref['component']['m_PathID']] = obj.path_id
            elif obj.type.name == 'Transform':
                data = obj.read_typetree()
                transforms[data['m_GameObject']['m_PathID']] = data
        def owner(ref):
            if not isinstance(ref, dict) or ref.get('m_FileID', 0) != 0: return None
            pid = ref.get('m_PathID', 0)
            return pid if pid in go_data else owners.get(pid)
        def active(go):
            if not go_data[go]['m_IsActive']: return False
            parent = owner(transforms.get(go, {}).get('m_Father'))
            return active(parent) if parent else True
        records, counts = [], collections.Counter()
        for obj in objects.values():
            if obj.type.name != 'MonoBehaviour': continue
            head = obj.read_typetree(check_read=False)
            script_ref = head['m_Script']
            script_file = name if not script_ref['m_FileID'] else PurePosixPath(source.externals[script_ref['m_FileID']-1].path).name
            script = load(script_file).objects[script_ref['m_PathID']].read_typetree()
            cls = '.'.join(x for x in [script['m_Namespace'], script['m_ClassName']] if x)
            counts[cls] += 1
            if cls not in FAMILIES: continue
            key = script['m_AssemblyName'].removesuffix('.dll') + '::' + cls
            data = obj.read_typetree(nodes=trees[key])  # missing/partial decode is a hard failure
            if any(data[k] != head[k] for k in ('m_GameObject', 'm_Script', 'm_Enabled')):
                raise ValueError(f'{name}:{obj.path_id}: incorrect MonoBehaviour header')
            go = data['m_GameObject']['m_PathID']
            refs = {}
            for field, value in data.items():
                if isinstance(value, list):
                    resolved = [owner(v) for v in value]
                    if any(v is not None for v in resolved): refs[field] = resolved
                else:
                    resolved = owner(value)
                    if resolved is not None: refs[field] = resolved
            if cls == 'WobblyObstacle' and 'spring' in refs:
                children = transforms[refs['spring']]['m_Children']
                targets = [owner(c) for c in children]
                if targets: refs['spring_target'] = targets[0]
            records.append({'component_id': obj.path_id, 'class': cls, 'assembly': script['m_AssemblyName'],
                            'game_object': go, 'game_object_name': go_data[go]['m_Name'],
                            'active_in_hierarchy': active(go), 'data': data, 'resolved_refs': refs})
        result = {'schema': 2, 'source': name, 'source_sha256': verified[name], 'scene_name': scene['name'],
                  'component_owners': owners,
                  'typetrees_sha256': hashlib.sha256(raw).hexdigest(), 'source_class_counts': dict(counts),
                  'class_counts': dict(collections.Counter(r['class'] for r in records)), 'behaviours': records}
        staged.append((name, result))
        # Bounded memory: only shared MonoScript files need to survive the next map.
        cache.pop(name)
    for name, result in staged:
        result['verified_source_files'] = {name: verified[name], **{k:v for k,v in verified.items() if not k.startswith('level')}}
        directory = args.output / name
        directory.mkdir(exist_ok=True)
        (directory / 'behaviors.json').write_text(json.dumps(result, ensure_ascii=False, separators=(',', ':'), default=lambda x: {'base64':base64.b64encode(x).decode()}) + '\n')
    print(json.dumps({'maps': len(staged), 'records': sum(len(d['behaviours']) for _,d in staged), 'verified_files':len(verified)}))

if __name__ == '__main__': main()
