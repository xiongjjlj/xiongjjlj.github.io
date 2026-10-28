# Boomerang Fu on arkevo.ai

Target: https://boomerang-fu.arkevo.ai

This deployment serves the browser-tested **0.5.1-dev.1** build. The game is still a development recreation; bot navigation and some map mechanisms remain incomplete. The stable Git tag `boomerang-fu-v0.5.0` is unchanged.

## Company account

- Account: `c0e2717206e00c30b9dcd1d456d744fc` (owns `arkevo.ai`).
- Worker: `arkevo-boomerang-fu`.
- Authentication: existing local cf profile `arkevo-brand-deploy`, passed explicitly.
- Domain: `boomerang-fu.arkevo.ai`. This project does not configure the apex or the brand website.
- No credentials are included in the repository.

## Repeatable deployment

Use Node 22.18 or newer and Python 3. Unzip the verified development web package to a directory, then:

```sh
npm ci --ignore-scripts
npm run build -- /absolute/path/to/unzipped-web /absolute/path/to/evidence/map-behavior-development-build.json
npm test
npm run deploy:check
npm run deploy
python3 scripts/verify_live.py --report /absolute/path/to/evidence/map-behavior-development-build.json --output /absolute/path/to/https-check.json
```

The build refuses files whose size or SHA-256 differs from the browser-tested evidence. Generated assets and dependencies are ignored by Git. `version.json` publishes the deployed version and original-file SHA-256 values for integration checks.

## Large files

Godot's PCK, WebAssembly engine, and FMOD bank exceed Cloudflare's [25 MiB individual asset limit](https://developers.cloudflare.com/workers/platform/limits/). Packaging splits files into at most 20 MiB parts. The Worker streams these parts in sequence using [FixedLengthStream](https://developers.cloudflare.com/workers/runtime-apis/response/#set-the-content-length-header); browsers receive the exact original files at their original names. No service worker or modified game loader is required.

Responses support GET, HEAD, ETag revalidation and correct MIME types. HTTP Range requests receive the full 200 response, as permitted by HTTP; partial downloads are not implemented. Internal part paths are not public endpoints. Files are revalidated so a subsequent release does not silently use stale assets.

The runtime test uses one local workerd process, hashes all 16 files including the full 142 MB PCK, and disposes the process on completion. The pinned local runtime supports compatibility date 2026-10-07; production uses 2026-10-28 and is separately verified over HTTPS and in the browser.

## Operation and rollback

This is static game hosting. It does not add accounts, a matchmaking service, or internet multiplayer. Player controls and local bots are provided by the game itself. Future updates must pass the same build and verification steps. To roll back, deploy a previously verified package using its matching evidence report; do not move the stable Git tag.
