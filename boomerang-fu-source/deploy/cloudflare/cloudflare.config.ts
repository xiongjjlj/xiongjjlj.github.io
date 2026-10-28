import { bindings, defineConfig } from 'cf/config';

// Account owning arkevo.ai. Keep company hosting separate from personal sites.
export default defineConfig({
  accountId: 'c0e2717206e00c30b9dcd1d456d744fc',
  worker: {
    name: 'arkevo-boomerang-fu',
    entrypoint: './src/worker.mjs',
    compatibilityDate: '2026-10-28',
    domains: ['boomerang-fu.arkevo.ai'],
    workersDev: false,
    previewUrls: false,
    assets: { runWorkerFirst: true, htmlHandling: 'none', notFoundHandling: 'none' },
    env: { ASSETS: bindings.assets() },
    observability: { enabled: true, traces: { enabled: true, headSamplingRate: 0.01 } }
  }
});
