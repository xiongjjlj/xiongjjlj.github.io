import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { resolve } from 'node:path';
import { test } from 'node:test';
import { Miniflare, convertV4MiniflareOptions } from 'miniflare';
import manifest from '../src/manifest.mjs';

test('production Worker streams the complete verified export in workerd', { timeout: 60000 }, async () => {
  const options = convertV4MiniflareOptions({
    name: 'game',
    modules: ['worker.mjs', 'serve.mjs', 'manifest.mjs'].map(file => ({ type: 'ESModule', path: resolve('src', file) })),
    // The cf-pinned local workerd supports dates through October 7.
    // Deployment uses today's date and is additionally verified over live HTTPS.
    compatibilityDate: '2026-10-07',
    assets: { directory: resolve('public'), binding: 'ASSETS', run_worker_first: true },
    port: 0
  });
  options.workers[0].config.exports = { default: { type: 'worker' } };
  options.workers[0].config.assets.hasUserWorker = true;
  const runtime = new Miniflare(options);
  try {
    for (const [path, expected] of Object.entries(manifest.files)) {
      const response = await runtime.dispatchFetch(`https://example.test${encodeURI(path)}`);
      assert.equal(response.status, 200, response.status === 200 ? path : `${path}: ${await response.text()}`);
      assert.equal(response.headers.get('content-type'), expected.type, path);
      const hash = createHash('sha256');
      let bytes = 0;
      for await (const chunk of response.body) { hash.update(chunk); bytes += chunk.length; }
      assert.equal(bytes, expected.bytes, path);
      assert.equal(hash.digest('hex'), expected.sha256, path);
    }
    const head = await runtime.dispatchFetch('https://example.test/index.pck', { method: 'HEAD' });
    assert.equal(head.status, 200);
    assert.equal(await head.text(), '');
    assert.equal(head.headers.get('content-length'), String(manifest.files['/index.pck'].bytes));
    const cached = await runtime.dispatchFetch('https://example.test/index.pck', { headers: { 'If-None-Match': `"${manifest.files['/index.pck'].sha256}"` } });
    assert.equal(cached.status, 304);
    assert.equal((await runtime.dispatchFetch('https://example.test/_parts/private.bin')).status, 404);
    assert.equal((await runtime.dispatchFetch('https://example.test/unknown')).status, 404);
    assert.equal((await runtime.dispatchFetch('https://example.test/', { method: 'POST' })).status, 405);
    const root = await runtime.dispatchFetch('https://example.test/');
    assert.equal(root.status, 200);
    assert.equal(root.headers.get('cross-origin-embedder-policy'), 'require-corp');
    await root.text();
    const info = await runtime.dispatchFetch('https://example.test/version.json');
    assert.equal((await info.json()).version, manifest.version);
  } finally { await runtime.dispose(); }
});
