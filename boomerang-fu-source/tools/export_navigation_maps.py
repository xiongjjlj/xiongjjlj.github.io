#!/usr/bin/env python3
"""Package native-decoded graph inputs; exclude all expected path results."""
import base64
import gzip
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
evidence = root / 'evidence'
source = json.loads((evidence / 'navigation-graph-extraction.json').read_text())
settings = json.loads((evidence / 'navigation-graph-settings.json').read_text())['maps']
maps = [m for i in range(10) for m in json.loads((evidence / f'navigation-maps-native-batch{i:02}.json').read_text())['maps']]
assert [m['key'] for m in maps] == [m['map'] for m in source['maps']]
output = root / 'godot/assets/original/navigation'
output.mkdir(exist_ok=True)
records = []
for data, original in zip(maps, source['maps']):
    graphs = [{k: v for k, v in graph.items() if k != 'paths'} for graph in data['graphs']]
    for graph, config in zip(graphs, settings[data['key']]):
        assert graph['index'] == config['index']
        graph['settings'] = config
    assert all(g['extra_connections'] == 0 for g in graphs), data['key']
    payload = json.dumps({'map': data['key'], 'graphs': graphs}, separators=(',', ':')).encode()
    envelope = {'encoding': 'gzip-base64', 'uncompressed_bytes': len(payload),
                'uncompressed_sha256': hashlib.sha256(payload).hexdigest(),
                'source_graph_sha256': original['sha256'],
                'data': base64.b64encode(gzip.compress(payload, compresslevel=9, mtime=0)).decode()}
    path = output / (data['key'] + '.json')
    path.write_text(json.dumps(envelope, separators=(',', ':')) + '\n')
    records.append({'map': data['key'], 'graphs': len(graphs), 'nodes': sum(len(g['nodes']) for g in graphs),
                    'bytes': path.stat().st_size, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
report = {'maps': records, 'total_bytes': sum(r['bytes'] for r in records), 'expected_paths_excluded': True,
          'scope': 'Original serialized grids and original startup boundary tags. Runtime rescans and teleport edges remain separate.'}
(evidence / 'navigation-map-export.json').write_text(json.dumps(report, indent=2) + '\n')
print({'maps': len(records), 'bytes': report['total_bytes']})
