#!/usr/bin/env python3
"""Validate behavior data provenance and links, not native runtime parity."""
import collections
import datetime
import hashlib
import json
from pathlib import Path, PureWindowsPath
from export_scene_behaviors import FAMILIES

PROJECT = Path(__file__).resolve().parents[1]
ROOT = PROJECT / 'godot/assets/original'
ADAPTED = {'MovingObstacle', 'Switch', 'WobblyObstacle', 'DestructableObstacle', 'Debris3D'}


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def check_map(scene, installation):
    name = Path(scene['file']).stem
    graph_path = ROOT / scene['file']
    sidecar_path = ROOT / name / 'behaviors.json'
    graph = json.loads(graph_path.read_text())
    data = json.loads(sidecar_path.read_text())
    nodes = {n['id']: n for n in graph['nodes']}
    graph_owners = {c['id']: n['id'] for n in graph['nodes'] for c in n['components']}
    owners = {int(k): v for k, v in data['component_owners'].items()}
    records = data['behaviours']
    failures = []

    def require(condition, label):
        if not condition:
            failures.append(label)

    def owner(ref):
        if not isinstance(ref, dict) or ref.get('m_FileID') != 0:
            return None
        pid = ref.get('m_PathID', 0)
        return pid if pid in nodes else owners.get(pid)

    def active(node_id):
        node = nodes[node_id]
        if not node['active']:
            return False
        transform = next(c['data'] for c in node['components'] if c['type'] == 'Transform')
        parent = owner(transform['m_Father'])
        return active(parent) if parent else True

    require(data['schema'] == 2 and data['source'] == name, 'schema and source identity')
    require(all(owners.get(k) == v for k, v in graph_owners.items()), 'component owners agree with scene graph')
    source_hash = installation[name]['sha256'].lower()
    require(data['source_sha256'].lower() == graph['source_sha256'].lower() == source_hash, 'scene source SHA-256')
    require(len(data['typetrees_sha256']) == 64, 'type tree provenance')
    require(name in data['verified_source_files'], 'scene listed in verified files')
    for filename, value in data['verified_source_files'].items():
        require(value.lower() == installation[filename]['sha256'].lower(), 'source hash: ' + filename)
    actual_counts = dict(collections.Counter(r['class'] for r in records))
    selected_counts = {k: v for k, v in data['source_class_counts'].items() if k in FAMILIES}
    require(actual_counts == data['class_counts'] == selected_counts, 'complete selected-class counts')
    require(sum(data['source_class_counts'].values()) == scene['counts']['MonoBehaviour'], 'all source MonoBehaviour count')
    require(len({r['component_id'] for r in records}) == len(records), 'unique component IDs')
    unresolved = []
    for record in records:
        cid, go, fields = record['component_id'], record['game_object'], record['data']
        require(owners.get(cid) == go, f'{cid}: component owner')
        require(go in nodes and owner(fields['m_GameObject']) == go, f'{cid}: GameObject link')
        require(nodes[go]['name'] == record['game_object_name'], f'{cid}: object name')
        require(active(go) == record['active_in_hierarchy'], f'{cid}: hierarchy activation')
        for field, resolved in record['resolved_refs'].items():
            if field == 'spring_target':
                spring = record['resolved_refs']['spring']
                transform = next(c['data'] for c in nodes[spring]['components'] if c['type'] == 'Transform')
                require(resolved == owner(transform['m_Children'][0]), f'{cid}: spring target')
                continue
            raw = fields[field]
            expected = [owner(x) for x in raw] if isinstance(raw, list) else owner(raw)
            require(resolved == expected, f'{cid}: resolved {field}')
        if record['class'] == 'Switch':
            for field in ('movingObstacles', 'movingDoors', 'movingFloors'):
                for ref in fields[field]:
                    if ref['m_PathID'] == 0:
                        continue  # Unity preserves empty reference array slots.
                    require(ref['m_FileID'] == 0 and ref['m_PathID'] in owners, f'{cid}: switch target')
                    target = next((r for r in records if r['component_id'] == ref['m_PathID']), None)
                    require(target is not None, f'{cid}: decoded switch target')
                    if target and target['class'] not in ADAPTED:
                        unresolved.append({'switch': cid, 'field': field, 'target': target['component_id'], 'class': target['class']})
    active_counts = collections.Counter(r['class'] for r in records if r['active_in_hierarchy'] and r['data'].get('m_Enabled', 1))
    return {'map': name, 'scene': scene['name'], 'passed': not failures, 'failures': failures,
            'records': len(records), 'class_counts': actual_counts, 'active_counts': dict(active_counts),
            'active_classes_without_shared_adapter': {k: v for k, v in active_counts.items() if k not in ADAPTED},
            'switch_targets_without_shared_adapter': unresolved,
            'sidecar_sha256': digest(sidecar_path), 'graph_sha256': digest(graph_path)}


def main():
    installation = {PureWindowsPath(r['path']).as_posix(): r for r in json.loads((PROJECT / 'evidence/original-installation-manifest.json').read_text())['files']}
    results = []
    for scene in json.loads((ROOT / 'manifest.json').read_text())['scenes']:
        try:
            results.append(check_map(scene, installation))
        except (KeyError, ValueError, OSError, StopIteration) as error:
            results.append({'map': Path(scene['file']).stem, 'passed': False, 'failures': [repr(error)]})
    report = {'scope': 'Integrity of 59 imported Unity levels 2-60, including tutorial content. Does not prove FFA eligibility, runtime behavior or frame parity.',
              'generated_at': datetime.datetime.now(datetime.timezone.utc).isoformat(),
              'maps': len(results), 'records': sum(x.get('records', 0) for x in results),
              'passed_maps': sum(x['passed'] for x in results), 'all_passed': all(x['passed'] for x in results),
              'shared_adapter_classes': sorted(ADAPTED), 'results': results}
    (PROJECT / 'evidence/godot-map-behavior-import-check.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({k: v for k, v in report.items() if k != 'results'}))
    for item in results:
        if not item['passed']:
            print(item['map'], item['failures'])
    raise SystemExit(0 if report['all_passed'] else 1)


if __name__ == '__main__':
    main()
