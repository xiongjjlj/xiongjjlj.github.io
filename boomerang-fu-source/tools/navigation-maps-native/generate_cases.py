#!/usr/bin/env python3
import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
records = json.loads((root / 'evidence/navigation-graph-extraction.json').read_text())['maps']

def vector(data, kind):
    keys = 'xyzw'[:int(kind[-1])] if kind.startswith('Vector') else 'xyzw'
    return 'new ' + kind + '(' + ','.join(repr(float(data[k])) + 'f' for k in keys) + ')'

cases = []
for record in records:
    assert record['graph']
    filename = 'boundary-inputs.json' if record['map'] == 'level2' else 'boundary-inputs-' + record['map'] + '.json'
    boundary = json.loads((root / 'godot/assets/original' / filename).read_text())
    pose = boundary['level_transform']
    assert pose['m_Father']['m_PathID'] == 0, record['map']
    cases.append('new MapCase{key=' + json.dumps(record['map']) + ',file=' + json.dumps(record['graph']) +
                 ',position=' + vector(pose['m_LocalPosition'], 'Vector3') +
                 ',rotation=' + vector(pose['m_LocalRotation'], 'Quaternion') +
                 ',scale=' + vector(pose['m_LocalScale'], 'Vector3') +
                 ',boundary=' + vector(boundary['level_box']['m_Size'], 'Vector2') + '}')
Path(__file__).with_name('Cases.cs').write_text('using UnityEngine; public static class MapCases {public static MapCase[] Create()=>new MapCase[]{\n' + ',\n'.join(cases) + '\n};}\n')
print('Generated', len(cases), 'map cases; six maps per native run.')
