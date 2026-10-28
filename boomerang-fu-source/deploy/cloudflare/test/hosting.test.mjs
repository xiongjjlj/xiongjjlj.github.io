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
      const response = await runtime.dispatchFetch(`https://example.test${encodeURI(path)}`, { headers: { 'Accept-Encoding': 'identity' } });
      assert.equal(response.status, 200, response.status === 200 ? path : `${path}: ${await response.text()}`);
      assert.equal(response.headers.get('content-type'), expected.type, path);
      const hash = createHash('sha256');
      let bytes = 0;
      for await (const chunk of response.body) { hash.update(chunk); bytes += chunk.length; }
      assert.equal(bytes, expected.bytes, path);
      assert.equal(hash.digest('hex'), expected.sha256, path);
    }
    const head = await runtime.dispatchFetch('https://example.test/index.pck', { method: 'HEAD', headers: { 'Accept-Encoding': 'identity' } });
    assert.equal(head.status, 200);
    assert.equal(await head.text(), '');
    assert.equal(head.headers.get('content-length'), String(manifest.files['/index.pck'].bytes));
    const cached = await runtime.dispatchFetch('https://example.test/index.pck', { headers: { 'Accept-Encoding': 'identity', 'If-None-Match': `"${manifest.files['/index.pck'].sha256}"` } });
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
    for (const [path, expected] of Object.entries(manifest.files)) {
      if (!expected.gzip) continue;
      const compressed = await runtime.dispatchFetch(`https://example.test${encodeURI(path)}`, { headers: { 'Accept-Encoding': 'gzip' } });
      // Miniflare dispatchFetch decompresses the body and moves the wire header
      // from Content-Encoding to MF-Content-Encoding.
      const digest = createHash('sha256');
      let count = 0;
      for await (const chunk of compressed.body) { digest.update(chunk); count += chunk.length; }
      assert.equal(compressed.headers.get('mf-content-encoding'), 'gzip', path);
      assert.equal(compressed.headers.get('vary'), 'Accept-Encoding', path);
      assert.equal(compressed.headers.get('etag'), `"${expected.gzip.sha256}"`, path);
      assert.equal(compressed.headers.get('content-length'), String(expected.gzip.bytes), path);
      assert.equal(count, expected.bytes, path);
      assert.equal(digest.digest('hex'), expected.sha256, path);
    }
    for (const [version, release] of Object.entries(manifest.releases || {})) {
      const base = `https://example.test/releases/${version}`;
      const redirect = await runtime.dispatchFetch(base + '?map=level15', {redirect:'manual'});
      assert.equal(redirect.status, 308);
      assert.equal(redirect.headers.get('location'), base + '/?map=level15');
      const versionResponse = await runtime.dispatchFetch(base + '/version.json');
      assert.equal(versionResponse.headers.get('access-control-allow-origin'), '*');
      const info = await versionResponse.json();
      assert.equal(info.version, version); assert.equal(info.immutable, true);
      for(const [path, expected] of Object.entries(release.files)) {
        const response = await runtime.dispatchFetch(base + encodeURI(path), {headers:{'Accept-Encoding':'gzip'}});
        const digest = createHash('sha256'); let size = 0;
        for await(const chunk of response.body){digest.update(chunk);size+=chunk.length;}
        assert.equal(response.status,200);assert.equal(size,expected.bytes);assert.equal(digest.digest('hex'),expected.sha256);
        assert.match(response.headers.get('cache-control'), /immutable/);
      }
    }
    assert.equal((await runtime.dispatchFetch('https://example.test/releases/9.9.9/')).status,404);
    const packed = manifest.files['/index.pck'];
    assert.ok(packed.gzip && packed.gzip.bytes < packed.bytes * 0.95);
    for (const [accept, encoding, status] of [
      ['gzip;q=0', null, 200], ['*;q=0, gzip;q=1', 'gzip', 200],
      ['gzip;q=0.5, identity;q=1', null, 200], ['gzip;q=0, *;q=1', null, 200]
    ]) {
      const response = await runtime.dispatchFetch('https://example.test/index.pck', { method: 'HEAD', headers: { 'Accept-Encoding': accept } });
      assert.equal(response.status, status, accept);
      assert.equal(response.headers.get('mf-content-encoding'), encoding, accept);
      assert.equal(await response.text(), '');
    }
    // Keep the local edge transport acceptable while testing the Worker's
    // refusal of a client that excludes both supported representations.
    const excluded = await runtime.dispatchFetch('https://example.test/index.pck', { method: 'HEAD', headers: { 'Accept-Encoding': 'identity' }, cf: { clientAcceptEncoding: 'gzip;q=0, identity;q=0' } });
    assert.equal(excluded.status, 406);
    const compressedCached = await runtime.dispatchFetch('https://example.test/index.pck', { headers: { 'Accept-Encoding': 'gzip', 'If-None-Match': `"${packed.gzip.sha256}"` } });
    assert.equal(compressedCached.status, 304);
    const otherRepresentation = await runtime.dispatchFetch('https://example.test/index.pck', { method: 'HEAD', headers: { 'Accept-Encoding': 'gzip', 'If-None-Match': `"${packed.sha256}"` } });
    assert.equal(otherRepresentation.status, 200);
  } catch (error) {
    console.error(error);
    throw error;
  } finally { await runtime.dispose(); }
});
