"""Prepare the isolated WebGL water comparison using published source inputs.

This does not launch Godot or a browser. Run one export/preview at a time after
checking machine resources. Pass a matching Godot release Web template.
"""
import argparse
import json
import shutil
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--project', type=Path, required=True)
parser.add_argument('--template', type=Path, required=True)
args = parser.parse_args()
source = Path(__file__).resolve().parent
root = source.parents[1]
project = args.project.resolve()
template = args.template.resolve()
assert template.is_file(), template
project.mkdir(parents=True, exist_ok=True)
for name in ('project.godot', 'main.tscn', 'test.gd'):
    shutil.copy2(source / name, project / name)
shutil.copy2(root / 'godot/shaders/original_water.gdshader', project / 'water.gdshader')
shutil.copy2(root / 'evidence/water-lighting-render-native.json', project / 'fixture.json')
shutil.copy2(root / 'godot/assets/original/water-lighting-inputs.json', project / 'lighting.json')
manifest = json.loads((root / 'godot/assets/original/manifest.json').read_text())
inputs = json.loads((root / 'godot/assets/original/map-shader-inputs.json').read_text())
materials = {key: manifest['materials'][key] for key, value in inputs.items()
             if value['shader'] == 'FX/Water_DepthVignette'}
assert len(materials) == 3
(project / 'materials.json').write_text(json.dumps(materials, indent=2) + '\n')
(project / 'export_presets.cfg').write_text('''[preset.0]
name="Web"
platform="Web"
runnable=true
export_filter="all_resources"
include_filter="*.json"
script_export_mode=2
[preset.0.options]
custom_template/release=''' + json.dumps(str(template)) + '''
variant/extensions_support=false
variant/thread_support=false
vram_texture_compression/for_desktop=true
vram_texture_compression/for_mobile=false
html/export_icon=false
html/head_include=""
html/canvas_resize_policy=2
''')
print(json.dumps({'project': str(project), 'materials': len(materials),
                  'fixture': 'water-lighting-render-native.json'}))
