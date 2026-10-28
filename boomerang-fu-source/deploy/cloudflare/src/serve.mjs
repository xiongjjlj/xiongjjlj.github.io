const commonHeaders = {
  'Cross-Origin-Opener-Policy': 'same-origin',
  'Cross-Origin-Embedder-Policy': 'require-corp',
  'Cross-Origin-Resource-Policy': 'cross-origin',
  'X-Content-Type-Options': 'nosniff'
};

export async function serve(request, env, ctx, manifest) {
  if (!['GET', 'HEAD'].includes(request.method)) return new Response('Method not allowed', { status: 405, headers: { Allow: 'GET, HEAD' } });
  let path;
  try { path = decodeURIComponent(new URL(request.url).pathname); }
  catch { return new Response('Bad path', { status: 400 }); }
  if (path === '/') path = '/index.html';
  if (path === '/version.json') {
    const body = JSON.stringify({ version: manifest.version, files: Object.fromEntries(Object.entries(manifest.files).map(([name, file]) => [name, { bytes: file.bytes, sha256: file.sha256 }])) });
    return new Response(request.method === 'HEAD' ? null : body, { headers: { ...commonHeaders, 'Content-Type': 'application/json', 'Cache-Control': 'no-cache' } });
  }
  const file = Object.hasOwn(manifest.files, path) ? manifest.files[path] : null;
  if (!file) return new Response('Not found', { status: 404 });
  const etag = `"${file.sha256}"`;
  const headers = new Headers({ ...commonHeaders, 'Content-Type': file.type, 'ETag': etag, 'Cache-Control': 'public, max-age=0, must-revalidate', 'X-Game-Version': manifest.version, 'Accept-Ranges': 'none' });
  // Keep the verified loader intact; automatic analytics injection changes HTML.
  if (file.type.startsWith('text/html')) headers.set('Cache-Control', 'public, max-age=0, must-revalidate, no-transform');
  // Range requests deliberately receive the full 200 response; the Godot loader
  // fetches whole resources. Never return a misleading partial response.
  if (request.headers.get('If-None-Match')?.split(',').some(value => value.trim().replace(/^W\//, '') === etag || value.trim() === '*')) return new Response(null, { status: 304, headers });
  if (request.method === 'HEAD') {
    headers.set('Content-Length', String(file.bytes));
    return new Response(null, { headers });
  }
  async function fetchPart(part) {
    const response = await env.ASSETS.fetch(`https://assets.local${part.path}`);
    if (response.status !== 200 || !response.body) throw new Error(`Asset part unavailable: ${response.status}`);
    return response;
  }
  try {
    const first = await fetchPart(file.parts[0]);
    if (file.parts.length === 1) return new Response(first.body, { headers });
    const { readable, writable } = new FixedLengthStream(file.bytes);
    ctx.waitUntil((async () => {
      try {
        await first.body.pipeTo(writable, { preventClose: true });
        for (const part of file.parts.slice(1)) {
          const next = await fetchPart(part);
          await next.body.pipeTo(writable, { preventClose: true });
        }
        await writable.close();
      } catch (error) {
        console.error(JSON.stringify({ event: 'asset_stream_failed', path, error: String(error) }));
        await writable.abort(error).catch(() => {});
      }
    })());
    return new Response(readable, { headers });
  } catch (error) {
    console.error(JSON.stringify({ event: 'asset_fetch_failed', path, error: String(error) }));
    return new Response('Asset unavailable', { status: 503, headers: { 'Cache-Control': 'no-store' } });
  }
}
