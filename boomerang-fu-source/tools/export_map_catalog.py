"""Read the original LevelManager catalog; build scenes are not a map pool."""
import collections
import gzip
import hashlib
import json
from pathlib import Path, PureWindowsPath

import UnityPy

root = Path(__file__).resolve().parents[1]
task = root.parents[1]
data = task / 'work/reference-complete/Boomerang Fu_Data'
assets = root / 'godot/assets/original'
expected = {PureWindowsPath(x['path']).as_posix(): x for x in json.loads(
    (root / 'evidence/original-installation-manifest.json').read_text())['files']}
trees = json.loads(gzip.decompress((task / 'work/typetrees.json.gz').read_bytes()))
cache, verified = {}, {}


def load(name):
    if name not in cache:
        path = data / name
        digest = hashlib.file_digest(path.open('rb'), 'sha256').hexdigest()
        assert path.stat().st_size == expected[name]['bytes']
        assert digest.lower() == expected[name]['sha256'].lower(), name
        verified[name] = digest
        cache[name] = UnityPy.load(str(path)).file
    return cache[name]


def resolve(obj, ref):
    file = obj.assets_file
    if ref['m_FileID']:
        file = load(Path(file.externals[ref['m_FileID'] - 1].path).name)
    return file.objects[ref['m_PathID']]


manager = load('level1').objects[8970]
manager_data = manager.read_typetree(nodes=trees['Assembly-CSharp::LevelManager'])
build_scenes = next(o.read_typetree()['scenes'] for o in
                   load('globalgamemanagers').objects.values() if o.type.name == 'BuildSettings')
imported = json.loads((assets / 'manifest.json').read_text())['scenes']
imported_keys = {Path(x['file']).stem for x in imported}
entries = []
for index, ref in enumerate(manager_data['levelAssets']):
    obj = resolve(manager, ref)
    value = obj.read_typetree(nodes=trees['Assembly-CSharp::LevelAsset'])
    scene_keys = [f'level{i}' for i, path in enumerate(build_scenes) if path == value['path']]
    assert scene_keys and set(scene_keys) <= imported_keys, value['path']
    entries.append({
        'catalog_index': index, 'source_file': obj.assets_file.name, 'source_id': obj.path_id,
        'path': value['path'], 'name': Path(value['path']).stem,
        'build_scene_keys': scene_keys, 'supported_match_types': value['supportedMatchTypes'],
        'complexity': value['complexity'], 'required_dlc': value['requiredDLC'],
        'min_players': value['minPlayers'], 'max_players': value['maxPlayers'],
        'force_first': bool(value['forceFirstInPlaylist']), 'solo': bool(value['soloInPlaylist']),
        'tricky_navigation': bool(value['isTrickyToNavigate']),
    })
assert len({x['path'] for x in entries}) == len(entries)
assert not any(x['force_first'] or x['solo'] for x in entries)
ffa = [x for x in entries if 0 in x['supported_match_types'] and x['complexity'] != 0]
counts = []
for count in range(2, 7):
    eligible = [x for x in ffa if x['min_players'] <= count <= x['max_players']]
    counts.append({'players': count, 'base': sum(x['required_dlc'] == 0 for x in eligible),
                   'just_desserts': sum(x['required_dlc'] == 2 for x in eligible),
                   'total': len(eligible)})
ids = {x['source_id']: x['catalog_index'] for x in entries}
catalog = {'switch_incompatible_indices': [ids[x['m_PathID']] for x in manager_data['switchIncompatibleLevels']],
           'match_types': {'DeathMatch': 0, 'GoldenDisc': 1, 'KingOfTheHill': 2, 'HideAndSeek': 3},
           'dlc_ids': {'None': 0, 'FreshFlavorsPack': 1, 'JustDessertsPack': 2},
           'tutorial_complexity': 0, 'entries': entries}
output = assets / 'map-catalog.json'
output.write_text(json.dumps(catalog, indent=2) + '\n')
report = {
    'verified_files': verified, 'catalog_sha256': hashlib.sha256(output.read_bytes()).hexdigest(),
    'source_sha256': {name: hashlib.sha256((root / 'recovered-source' / name).read_bytes()).hexdigest()
                      for name in ['LevelManager.cs', 'LevelAsset.cs', 'SettingsManager.cs',
                                   'UIMatchSettingMapsModule.cs', 'UIMenuMatchSettings.cs']},
    'imported_scene_files': len(imported), 'catalog_entries': len(entries),
    'regular_ffa_maps': len(ffa), 'base_maps': sum(x['required_dlc'] == 0 for x in ffa),
    'just_desserts_maps': sum(x['required_dlc'] == 2 for x in ffa),
    'tutorials': [x['name'] for x in entries if x['complexity'] == 0],
    'duplicate_build_paths': [x for x in entries if len(x['build_scene_keys']) > 1],
    'default_pool_counts_by_players': counts,
    'small_maps_excluded_at_5_or_6': [x['name'] for x in ffa if x['max_players'] < 5],
    'unregistered_imported_scenes': sorted(imported_keys - {s for x in entries for s in x['build_scene_keys']}),
    'rules': [
        'FFA selects DeathMatch with teamMatch=false; every registered map in this installation supports DeathMatch.',
        'Default playlist filters by min/max players and original DLC unlock state.',
        'Custom maps bypass min/max player filtering but still require supported mode and unlocked DLC.',
        'Tutorial is optional on the first match, excluded on rematch and from the regular map picker.',
        'Repeated BuildSettings paths represent one LevelAsset, not additional selectable maps.',
        'This audit does not establish the user account DLC entitlement or implement runtime playlist rotation.',
    ],
}
assert report['regular_ffa_maps'] == 57 and report['base_maps'] == 36 and report['just_desserts_maps'] == 21
assert not report['unregistered_imported_scenes']
(root / 'evidence/ffa-map-pool-audit.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps({k: report[k] for k in ['imported_scene_files', 'catalog_entries', 'regular_ffa_maps',
                                      'base_maps', 'just_desserts_maps', 'default_pool_counts_by_players']}))
