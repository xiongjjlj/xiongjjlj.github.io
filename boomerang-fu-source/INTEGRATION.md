# 0.5.1 integration baseline

This release freezes the current local browser game for dependent projects.
It does not claim complete Boomerang Fu parity. Use a versioned URL/tag so
ongoing reconstruction cannot change your dependency without an explicit update.

| Contract | Fixed value |
| --- | --- |
| Game URL | https://boomerang-fu.arkevo.ai/releases/0.5.1/ |
| Version + file hashes | https://boomerang-fu.arkevo.ai/releases/0.5.1/version.json |
| Source tag | `boomerang-fu-v0.5.1` in `xiongjjlj/xiongjjlj.github.io` |
| Source directory | `boomerang-fu-source/` |
| Download | `boomerang-fu-releases/0.5.1/web.zip` in the same tag |
| Development branch | `boomerang-fu-dev` |
| Moving preview | https://boomerang-fu.arkevo.ai/ |

## Supported integration

Open the fixed URL, embed it in an iframe, or unzip and serve the 16 exported
files through HTTP(S). Preserve relative paths and serve `.wasm` with
`application/wasm`. The single-threaded build uses WebGL 2. The initial payload
is large (PCK 154 MB decoded, about 49 MB gzip); hosting must support it.

```html
<iframe
  src="https://boomerang-fu.arkevo.ai/releases/0.5.1/"
  title="Boomerang Fu"
  width="1280" height="720"
  allow="autoplay; fullscreen; gamepad"
  allowfullscreen>
</iframe>
```

Give the iframe keyboard focus by clicking the game. Audio requires interaction.
Cross-origin embedding does not give the host page access to game internals.
The public version endpoint supports cross-origin reads; it has no credentials.

Normal FFA uses the original filtered map pool: base 36 maps for 2–4 players,
31 for 5–6; optional Base + DLC 57/52. `?map=level15` fixes Island without
rotation; `?map=levelN` accepts imported scenes `level2`–`level60`. Those 59
resource scenes include tutorial and duplicate Toaster, not 59 normal arenas.
The DLC switch selects content and does not verify an ownership entitlement.

The current contract is a playable browser surface and pinned source/assets.
There is **no supported postMessage control/score API, network multiplayer,
remote agent protocol, deterministic replay, or native frame/pixel/PCM parity**.
Projects requiring those features must add a versioned adapter rather than
parsing console diagnostics or assuming internal GDScript fields are stable.

## Release scope and limits

Local 2–6 keyboard/gamepad/bot slots, combat, scoring, pause, rematch and map
rotation are integrated. The release reuses the unchanged `.8` game binary:
73 runtime suites passed, and water lighting passed 168 controlled native/WebGL
pixel samples. These tests do not prove every map feature or input device.
A public cold load in the validation environment took 454 seconds; this is
an observed sample, not a performance guarantee. The large initial download
remains a release limitation.
Desktop Chromium is validated; mobile, Safari and gamepad hardware diversity
are not release-certified.

Map teleportals remain incomplete, along with original menus, reflections,
fog, other shader families, particles and full audio/callback scheduling.
This release is suitable for embedding, launcher/UI integration and building
against a frozen code baseline. It is not an authoritative original-game
benchmark, a fully restored map catalogue, or an online multiplayer service.

Downloads retry at most four times, abort after 30 seconds without data or
300 seconds per attempt, and show an error after exhaustion. Reload retries.
A slow/unreliable network can still fail; the loader no longer silently hangs.
WASM is buffered before engine initialization so body errors are retryable;
this adds a temporary buffer of approximately 40 MB.

## Reproduction and compatibility policy

After a Godot Web export, run `python3 tools/patch_web_loader.py WEB_DIR`.
The patch checks the supported original loader block and refuses unknown
versions. Run `node tools/test_web_loader.mjs WEB_DIR/index.js`.

Cloudflare builds verify every current and frozen file hash. Release locks in
`deploy/cloudflare/releases/` must remain present; a missing cache or changed
file fails the build instead of silently removing or replacing a release.
Use `tools/restore_release.py 0.5.1 CACHE_DIR` to restore the published ZIP into
`CACHE_DIR/0.5.1`; then set `BF_RELEASE_CACHE=CACHE_DIR` when building.
The ZIP and metadata are version-pinned in Git; old `0.5.0` remains unchanged.
Bug fixes get a new version. Do not move published tags or overwrite frozen URLs.
