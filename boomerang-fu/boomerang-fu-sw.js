/* Reassembles the large Godot PCK from GitHub Pages-safe parts. */
const PARTS = ['index.pck.part0', 'index.pck.part1', 'index.pck.part2'];
const TOTAL_BYTES = 136433840;

self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', (event) => event.waitUntil(self.clients.claim()));

async function pckResponse(request) {
  const body = new ReadableStream({
    async start(controller) {
      try {
        for (const part of PARTS) {
          const url = new URL(part, request.url);
          const response = await fetch(url, { cache: 'no-store' });
          if (!response.ok) throw new Error(`PCK part ${part} returned ${response.status}`);
          if (response.body) {
            const reader = response.body.getReader();
            while (true) {
              const item = await reader.read();
              if (item.done) break;
              controller.enqueue(item.value);
            }
          } else {
            controller.enqueue(new Uint8Array(await response.arrayBuffer()));
          }
        }
        controller.close();
      } catch (error) {
        controller.error(error);
      }
    },
  });
  return new Response(body, {
    status: 200,
    headers: {
      'Content-Type': 'application/octet-stream',
      'Content-Length': String(TOTAL_BYTES),
      'Cache-Control': 'public, max-age=31536000, immutable',
    },
  });
}

self.addEventListener('fetch', (event) => {
  const url = new URL(event.request.url);
  if (url.pathname.endsWith('/index.pck')) event.respondWith(pckResponse(event.request));
});
