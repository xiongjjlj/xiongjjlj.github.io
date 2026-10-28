import manifest from './manifest.mjs';
import { serve } from './serve.mjs';

export default { fetch: (request, env, ctx) => serve(request, env, ctx, manifest) };
