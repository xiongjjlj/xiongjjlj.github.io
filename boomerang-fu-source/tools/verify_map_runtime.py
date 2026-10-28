#!/usr/bin/env python3
"""Run bounded, sequential real-scene regressions and record source hashes."""
import argparse
import datetime
import hashlib
import json
import re
import subprocess
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--godot', required=True)
    parser.add_argument('--project', type=Path, default=Path(__file__).resolve().parents[1] / 'godot')
    args = parser.parse_args()
    tests = [('map_behavior_adapter_test', key) for key in ('level3', 'level4', 'level48', 'level55')]
    tests += [('moving_floor_scene_test', key) for key in ('level24','level26','level27','level29','level44','level45','level50')]
    tests += [('moving_floor_native_test', 'level2')]
    tests += [('reactive_floor_native_test', 'level2')]
    tests += [('reactive_floor_scene_test', key) for key in ('level40','level41','level46','level51','level52','level53')]
    tests += [('reactive_floor_game_loop_test', 'level40')]
    tests += [('navigation_scene_test', key) for key in ('level24','level26','level27','level29','level44','level45','level50','level40','level41','level46','level51','level52','level53')]
    tests += [('navigation_platform_ai_test', key) for key in ('level40','level45')]
    tests += [('navigation_selection_test', 'level2'), ('player_ai_game_test', 'level2')]
    tests += [('navigation_updates_test', key) for key in ('level2','level48')]
    tests += [('navigation_updates_scene_test', key) for key in ('level3','level41','level48','level55')]
    tests += [(name, 'level2') for name in ('floor_hazards_test','floor_collapse_test','floor_navigation_test')]
    tests += [('floor_hazards_scene_test', key) for key in ('level14','level18','level19','level40','level42','level46','level49','level51','level54','level57','level58','level59')]
    tests += [('floor_hazards_game_test', key) for key in ('level14','level18','level40')]
    tests += [('level3_behaviors_test', 'level3'), ('ffa_shell_test', 'level4'), ('duel_integration_test', 'level2')]
    tests += [('match_playlist_test','level2'),('playlist_scene_transition_test','level2')]
    paths = [*args.project.glob('*.gd'), *args.project.glob('core/*.gd'), *args.project.glob('assets/original/level*/behaviors.json')]
    paths += list(args.project.glob('assets/original/navigation/*.json'))
    paths += list(args.project.glob('assets/original/floor-*.json')) + list(args.project.glob('shaders/*.gdshader'))
    paths += [args.project/'assets/original/map-catalog.json']
    paths += [args.project / ('tests/' + name + '.gd') for name in sorted({t[0] for t in tests})]
    hashes = {str(p.relative_to(args.project)): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(paths)}
    results = []
    for name, level in tests:
        frame_limit = '1200' if name in ('floor_hazards_game_test','playlist_scene_transition_test') else '120'
        command = [args.godot, '--headless', '--path', str(args.project), '--script', 'res://tests/' + name + '.gd', '--quit-after', frame_limit, '--', '--map=' + level]
        try:
            process = subprocess.run(command, capture_output=True, text=True, timeout=45)
            log = process.stdout + '\n' + process.stderr
            errors = [line for line in log.splitlines() if re.search(r'ERROR:|SCRIPT ERROR|Parse Error|Invalid call', line)]
            reports = []
            for line in log.splitlines():
                if not ('"passed"' in line and '{' in line):
                    continue
                try:
                    report = json.loads(line[line.index('{'):])
                    if isinstance(report.get('passed'), bool):
                        reports.append(report)
                except (ValueError, AttributeError):
                    pass
            passed = process.returncode == 0 and not errors and bool(reports) and all(r['passed'] for r in reports)
            result = {'test': name, 'map': level, 'passed': passed, 'returncode': process.returncode, 'errors': errors, 'reports': reports}
        except subprocess.TimeoutExpired:
            result = {'test': name, 'map': level, 'passed': False, 'errors': ['45 second timeout']}
        results.append(result)
        print(json.dumps({k: v for k, v in result.items() if k != 'reports'}), flush=True)
    report = {'passed': all(r['passed'] for r in results), 'results': results, 'source_sha256': hashes,
              'generated_at': datetime.datetime.now(datetime.timezone.utc).isoformat(),
              'process_limit': 1, 'per_test_timeout_seconds': 45,
              'scope': 'Real-scene native Godot tests; no original Unity runtime, browser, visual or frame-parity claim.'}
    (args.project.parent / 'evidence/godot-map-runtime-check.json').write_text(json.dumps(report, indent=2) + '\n')
    raise SystemExit(0 if report['passed'] else 1)


if __name__ == '__main__':
    main()
