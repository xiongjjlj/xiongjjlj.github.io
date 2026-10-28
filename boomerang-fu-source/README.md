# Boomerang Fu Web — stable source snapshot

This directory is the pinned `v0.1.0-stable` source snapshot for the browser-hosted scoped FFA build. Open `godot/project.godot` with Godot 4.7, export Web, and select the optional scene with `?map=slide`. The published build is in `../boomerang-fu/`.

The default scene is `LevelEnclosed`. `LevelSlideTraps` is runtime-selectable and passes the local FFA shell, while its trap scheduling and full visual/frame parity remain unfinished. `recovered-source/` contains the recovered C# reference used during the port.

This snapshot is intended as a stable dependency point. Continue feature work from a branch or copy the directory; the `v0.1.0-stable` tag remains immutable.
