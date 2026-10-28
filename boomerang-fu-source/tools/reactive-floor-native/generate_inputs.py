"""Generate real-map MovingFloor cases for the original UnityPlayer oracle.

No Unity editor required. The installed game's MovingFloor methods are invoked
by reflection; only their Start motion fields are initialized by the harness.
"""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
folder = Path(__file__).resolve().parent
cases = []
for number in (40, 41, 46, 51, 52, 53):
    scene = json.loads((root / f'godot/assets/original/scenes/level{number}.json').read_text())
    sidecar = json.loads((root / f'godot/assets/original/level{number}/behaviors.json').read_text())
    nodes = {node['id']: node for node in scene['nodes']}
    transforms = {c['id']: c['data'] for node in nodes.values() for c in node['components'] if c['type'] == 'Transform'}
    for record in sidecar['behaviours']:
        if record['class'] != 'MovingFloorReactive':
            continue
        chain = []
        transform = next(c['data'] for c in nodes[record['game_object']]['components'] if c['type'] == 'Transform')
        while transform:
            chain.insert(0, {'position': transform['m_LocalPosition'], 'rotation': transform['m_LocalRotation'], 'scale': transform['m_LocalScale']})
            transform = transforms.get(transform['m_Father']['m_PathID'])
        cases.append({'key': f'level{number}-{record["component_id"]}', 'seed': 1000 + record['component_id'], 'chain': chain, 'data': record['data'], 'record': record})
inputs = {'cases': cases, 'ticks': 900, 'fixed_delta': 0.016666}
(root / 'evidence/reactive-floor-native-input.json').write_text(json.dumps(inputs, indent=2) + '\n')


def number(value):
    return format(float(value), '.10g') + 'f'


def vector(value):
    return 'new Vector3(' + ','.join(number(value[k]) for k in 'xyz') + ')'


def quaternion(value):
    return 'new Quaternion(' + ','.join(number(value[k]) for k in 'xyzw') + ')'


expressions = []
for case in cases:
    data = case['data']
    fields = [f'{key}={data[key]}' for key in ('isSpring', 'useRotations')]
    fields += [f'{key}={number(data[key])}' for key in ('moveDurMin', 'preMoveDur', 'springPauseDur', 'springWindBackDur')]
    fields += ['positions=new Vector3[]{' + ','.join(vector(p) for p in data['positions']) + '}']
    poses = ['new Pose{position=' + vector(p['position']) + ',rotation=' + quaternion(p['rotation']) + ',scale=' + vector(p['scale']) + '}' for p in case['chain']]
    expressions.append('new Case{key=' + json.dumps(case['key']) + ',seed=' + str(case['seed']) + ',chain=new Pose[]{' + ','.join(poses) + '},data=new Data{' + ','.join(fields) + '}}')
(folder / 'Cases.cs').write_text('using UnityEngine; static class FixtureInput { public static Input Create()=>new Input{ticks=900,fixed_delta=.016666f,cases=new Case[]{' + ','.join(expressions) + '}}; }\n')
print(f'Generated {len(cases)} real-map platform cases.')
