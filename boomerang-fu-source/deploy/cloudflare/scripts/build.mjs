import { createHash } from 'node:crypto';
import { cp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { resolve, extname } from 'node:path';
import { gzipSync } from 'node:zlib';
import config from '../cloudflare.config.ts';
import { cleanBuildOutputDir, writeAssets, writeRootConfig, writeWorkerConfig, getWorkerBundleDir } from '@cloudflare/build-output-utils';

const root = process.cwd();
const source = resolve(process.argv[2] || '../../../../work/web-map-behavior');
const report = JSON.parse(await readFile(resolve(process.argv[3] || '../../evidence/map-behavior-development-build.json'), 'utf8'));
if (!report.browser_checked || report.export_returncode !== 0) throw new Error('A browser-verified successful export is required');
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const chunkBytes = 20 * 1024 * 1024;
const mime = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.wasm': 'application/wasm', '.png': 'image/png', '.json': 'application/json', '.txt': 'text/plain; charset=utf-8' };
const manifest = { version: report.version, files: {} };
await rm('public', { recursive: true, force: true });
await mkdir('public/_parts', { recursive: true });
async function storeParts(bytes) {
  const parts = [];
  for (let offset = 0; offset < bytes.length; offset += chunkBytes) {
    const part = bytes.subarray(offset, offset + chunkBytes);
    const path = `/_parts/${hash(part)}.bin`;
    await writeFile(`public${path}`, part);
    parts.push({ path, bytes: part.length });
  }
  return parts;
}
// One file and one compressor at a time. Compression happens during the build,
// never on a Worker request; the identity representation remains byte-exact.
for (const [name, expected] of Object.entries(report.files)) {
  const bytes = await readFile(resolve(source, name));
  if (bytes.length !== expected.bytes || hash(bytes) !== expected.sha256) throw new Error(`Unverified input: ${name}`);
  const file = { ...expected, type: mime[extname(name)] || 'application/octet-stream', parts: await storeParts(bytes) };
  if (bytes.length >= 1024 * 1024 && ['.pck', '.wasm', '.bank'].includes(extname(name))) {
    const encoded = gzipSync(bytes, { level: 6 });
    if (encoded.length < bytes.length * 0.95) file.gzip = { bytes: encoded.length, sha256: hash(encoded), parts: await storeParts(encoded) };
  }
  manifest.files[`/${name}`] = file;
}
await writeFile('src/manifest.mjs', `// Generated from the verified web export.\nexport default ${JSON.stringify(manifest)};\n`);
await cleanBuildOutputDir(root);
await writeRootConfig(root, { accountId: config.accountId }, { isPreview: false });
await mkdir(getWorkerBundleDir(root), { recursive: true });
await cp('src', getWorkerBundleDir(root), { recursive: true });
await writeAssets({ root, sourceDirectory: 'public' });
await writeWorkerConfig({ root, config: config.worker, manifest: {
  type: 'complete', mainModule: 'worker.mjs', modules: {
    'worker.mjs': { type: 'esm' }, 'serve.mjs': { type: 'esm' }, 'manifest.mjs': { type: 'esm' }
  }
} });
console.log(JSON.stringify({ version: manifest.version, files: Object.keys(manifest.files).length, maxChunkBytes: chunkBytes, accountId: config.accountId, domain: config.worker.domains[0] }));
console.log(JSON.stringify({ compression: Object.fromEntries(Object.entries(manifest.files).filter(([, file]) => file.gzip).map(([path, file]) => [path, { identity: file.bytes, gzip: file.gzip.bytes }])) }));
