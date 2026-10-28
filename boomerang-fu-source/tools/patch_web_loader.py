#!/usr/bin/env python3
"""Patch the Godot 4.7.2 export without modifying the engine WASM/PCK."""
import argparse, hashlib
from pathlib import Path
p = argparse.ArgumentParser(); p.add_argument('web', type=Path); args=p.parse_args()
path=args.web/'index.js'; text=path.read_text()
start=text.index('\tfunction getTrackedResponse(') if 'BF_LOADER_RELIABILITY_V1' not in text else text.index('\t// BF_LOADER_RELIABILITY_V1')
end=text.index('\tfunction retry(', start)
if 'BF_LOADER_RELIABILITY_V1' not in text:
    expected='41393b8841d6c66366b325ef75fc3bd3ffcb26bfc5511d72a56bcf9f42e9589e'
    actual=hashlib.sha256(text[start:end].encode()).hexdigest()
    if actual!=expected: raise SystemExit(f'Unknown export preloader: {actual}; inspect before updating patch')
patch=Path(__file__).with_name('web_loader_preload.js').read_text()+'\n'
text=text[:start]+patch+text[end:]
old='return retry(loadFetch.bind(null, file, loadingFiles, fileSize, raw), DOWNLOAD_ATTEMPTS_MAX);'
new="""return retry(loadFetch.bind(null, file, loadingFiles, fileSize, raw), DOWNLOAD_ATTEMPTS_MAX)
            .catch(error => { throw new Error(`Unable to download ${file} after ${DOWNLOAD_ATTEMPTS_MAX} attempts. Please reload to retry. ${error.message}`); })
            .finally(() => { loadingFiles[file].done = true; });"""
if old not in text and new not in text: raise SystemExit('Unknown loadPromise wrapper')
path.write_text(text.replace(old,new))
print('Patched bounded fetch/body retries:',path)
