"""Verify every deployed game file against the browser-tested export, sequentially."""
import argparse
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import quote
from urllib.request import Request, urlopen

parser = argparse.ArgumentParser()
parser.add_argument('--url', default='https://boomerang-fu.arkevo.ai')
parser.add_argument('--report', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
expected = json.loads(args.report.read_text())
result = {'url': args.url, 'version': expected['version'], 'checked_at': datetime.now(timezone.utc).isoformat(), 'files': {}, 'passed': False}
try:
    # Use the same HTTP client identity as the CLI smoke test; the zone rejects
    # Python urllib's default user agent before the request reaches this Worker.
    request_headers = {'User-Agent': 'curl/8.7.1', 'Accept-Encoding': 'identity'}
    with urlopen(Request(args.url + '/version.json', headers=request_headers), timeout=60) as response:
        assert json.load(response)['version'] == expected['version']
    for name, meta in expected['files'].items():
        digest = hashlib.sha256()
        count = 0
        with urlopen(Request(args.url + '/' + quote(name), headers=request_headers), timeout=60) as response:
            assert response.status == 200, name
            headers = {key: response.headers.get(key) for key in ['Content-Type', 'Content-Length', 'ETag', 'X-Game-Version', 'Cross-Origin-Embedder-Policy']}
            while block := response.read(1024 * 1024):
                count += len(block)
                digest.update(block)
        actual = {'bytes': count, 'sha256': digest.hexdigest(), 'headers': headers}
        result['files'][name] = actual
        assert actual['bytes'] == meta['bytes'], (name, 'size mismatch')
        assert actual['sha256'] == meta['sha256'], (name, 'hash mismatch')
        assert headers['X-Game-Version'] == expected['version'], (name, 'version mismatch')
        print(f'PASS {name}: {count} bytes', flush=True)
    result['passed'] = True
finally:
    args.output.write_text(json.dumps(result, indent=2) + '\n')
