#!/usr/bin/env python3
"""Extract each level's original serialized A* graph after installation hash checks."""
import argparse
import gzip
import hashlib
import json
from pathlib import Path, PurePosixPath, PureWindowsPath


def main():
    import UnityPy
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--data', type=Path, required=True)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--typetrees', type=Path, required=True)
    parser.add_argument('--scenes', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args()
    expected = {PureWindowsPath(e['path']).as_posix(): e for e in json.loads(args.manifest.read_text())['files']}
    trees = json.loads(gzip.decompress(args.typetrees.read_bytes()))
    scenes = json.loads(args.scenes.read_text())['scenes']
    cache, verified, records = {}, {}, []

    def load(name):
        if name not in cache:
            path = args.data / name
            with path.open('rb') as stream:
                digest = hashlib.file_digest(stream, 'sha256').hexdigest()
            entry = expected[name]
            if digest.lower() != entry['sha256'].lower() or path.stat().st_size != entry['bytes']:
                raise ValueError('Unverified original file: ' + name)
            cache[name] = UnityPy.load(str(path)).file
            verified[name] = digest
        return cache[name]

    def resolve(file, pointer):
        if pointer['m_FileID']:
            file = PurePosixPath(load(file).externals[pointer['m_FileID'] - 1].path).name
        return file, load(file).objects[pointer['m_PathID']]

    args.output.mkdir(parents=True, exist_ok=True)
    for scene in scenes:
        name = Path(scene['file']).stem
        source = load(name)
        found = []
        for obj in source.objects.values():
            if obj.type.name != 'MonoBehaviour':
                continue
            head = obj.read_typetree(check_read=False)
            _, script = resolve(name, head['m_Script'])
            script = script.read_typetree()
            if script['m_ClassName'] not in ('Level', 'LevelTutorial', 'LevelProcedural') or script['m_Namespace']:
                continue
            data = obj.read_typetree(nodes=trees['Assembly-CSharp::' + script['m_ClassName']])
            pointer = data['pathfindingGraph']
            if not pointer['m_PathID']:
                found.append({'map': name, 'name': scene['name'], 'graph': None, 'reason': 'Original Level has no serialized graph; runtime scan required.'})
                continue
            file, asset = resolve(name, pointer)
            text = asset.read_typetree()
            content = text['m_Script']
            payload = content.encode('utf-8', 'surrogateescape') if isinstance(content, str) else bytes(content)
            digest = hashlib.sha256(payload).hexdigest()
            output_name = digest + '.bytes'
            (args.output / output_name).write_bytes(payload)
            found.append({'map': name, 'name': scene['name'], 'level_component': obj.path_id,
                          'graph': output_name, 'graph_name': text['m_Name'], 'bytes': len(payload),
                          'sha256': digest, 'source_file': file, 'source_path_id': asset.path_id,
                          'include_teleportals': bool(data['includeTeleportalsInPathfinding'])})
        if len(found) != 1:
            raise ValueError(f'{name}: expected one Level component, found {len(found)}')
        records.extend(found)
        print(json.dumps(found[0]), flush=True)
        # Hold one map and its external files at a time; no growing scene cache.
        cache.clear()
    args.report.write_text(json.dumps({'maps': records, 'verified_source_files': verified,
                                      'typetrees_sha256': hashlib.sha256(args.typetrees.read_bytes()).hexdigest(),
                                      'scope': 'Original serialized graph bytes; runtime scans and teleportal connections are not applied.'}, indent=2) + '\n')


if __name__ == '__main__':
    main()
