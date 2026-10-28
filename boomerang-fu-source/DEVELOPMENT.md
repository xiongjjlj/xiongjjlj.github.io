# 开发与固定依赖

稳定浏览器版位于 <https://feixiong.me/boomerang-fu/>。其他项目可固定提交
`ef21423cfcfcf4e1e5a9ff8cb16f35ba883e52d1`，源码目录是 `boomerang-fu-source/`；
`boomerang-fu-v0.5.0` 标签保留在该提交。`boomerang-fu-dev` 接受后续开发。

本次开发版本为 `0.5.1-dev.1`，修复共享地图机关，已部署到 <https://boomerang-fu.arkevo.ai/>。
`feixiong.me` 上的网页及 `boomerang-fu-v0.5.0` 标签仍保持稳定版。
源代码、浏览器包与证据分别有哈希，历史浏览器测试报告仅证明当时记录的构建。

开发 Web 包为 `web-development-0.5.1-dev.1.zip`，解压后通过 HTTP(S) 托管。
浏览器已实际验证 Turnstile 的旋转、投掷、暂停恢复、继续回合，以及 Slide Traps
中用键盘近战命中开关、两根刷子移动与回弹；两个页面的控制台均无错误或警告。
在 Turnstile 观察到机器人走落平台，地图专属寻路仍需修复。
该包使用完整 PCK（超过 100 MiB），需托管端允许相应文件大小；未使用稳定版的分片加载器。

## 运行 Godot

使用 Godot 4.7.2，从本目录执行（把 `godot` 替换为实际可执行文件路径）：

```sh
godot --editor --path godot
godot --path godot -- --map=level4
```

正常游戏入口是 `godot/project.godot` 配置的 `main.tscn`。
`core/original_scene_materials.gd` 等辅助脚本继承 `RefCounted`，不能作为
`--script` 的独立入口；否则会出现“不继承 SceneTree 或 MainLoop”的弹窗。

再次导出 Web 时，需安装匹配的 Godot Web 导出模板，并在导出预设中将
`custom_template/debug`、`custom_template/release` 改为本机模板路径或清空以使用默认模板。
工程保存的路径指向原开发环境；启动和编辑本身不需要这些导出模板。

## 验证

```sh
godot --headless --editor --path godot --import
python3 tools/verify_scene_behaviors.py
python3 tools/verify_map_runtime.py --godot /absolute/path/to/godot
python3 tools/verify_multi_map.py --godot /absolute/path/to/godot \
  --project godot --output evidence/godot-multi-map-check.json
```

验证工具逐个启动 Godot，最多一个实例，每次限时 45 秒。`--quit-after` 在
`--` 之前，避免被当成游戏参数而使检查进程一直运行。运行前检查机器负载。

`verify_scene_behaviors.py` 检查 59 个关卡（含教程）的 6,904 条已选行为记录：
安装文件哈希、组件数量、归属、激活层级、引用及弹簧目标。
它不证明这些机关已全部移植，也不证明所有关卡都适合 FFA。

`verify_map_runtime.py` 在真实场景检查开关、移动与旋转机关、子碰撞体同步、
重置和暂停调度；并检查六人对局与双人跨回合计分。报告记录实际源码哈希。
`verify_multi_map.py` 仅证明关卡加载和共享初始化。

## 本次接入范围

| 范围 | 状态 |
| --- | --- |
| 线性/旋转 MovingObstacle，Switch 近战、缴械和飞镖触发 | 共享适配器已接入；4 个真实关卡验证 |
| WobblyObstacle、可破坏障碍和碎片 | 共享适配器已接入；仍有近似视觉/物理行为 |
| MovingDoor、MovingFloor 及其变体 | 数据已解码；共享机关适配尚未完成 |
| FallingFloor、TrapFloor、Teleportal | 数据已解码；完整关卡行为尚未完成 |
| 原版 TimeKeeper 与 Unity 回调顺序、全局随机数交错 | 尚未证明完整场景一致 |
| 原始材质、Animator、机关音效、粒子及逐帧画面 | 尚未完成 |

各地图未接入的行为数量见 `evidence/godot-map-behavior-import-check.json`。
适配器当前固定步使用原版 `0.016666`，但完整 Unity 时钟/物理回调的对照仍需继续。

## 数据再生成

`tools/export_scene_behaviors.py` 需要 UnityPy、原安装目录、安装哈希清单和从原
程序集得到的 TypeTree 文件。默认不打包原安装和本机工具缓存。已有 JSON 可直接运行。

```sh
python3 tools/export_scene_behaviors.py --data /path/to/Boomerang_Fu_Data \
  --manifest evidence/original-installation-manifest.json \
  --typetrees /path/to/typetrees.json.gz --output godot/assets/original
```

导出前校验安装文件；缺少类型信息或未完整解码会失败，不跳过记录。
`schema: 2` 保留 Unity 的 `m_FileID/m_PathID`、`component_owners`、
`active_in_hierarchy` 与 `resolved_refs`，方便其他项目消费。

## 公司域名部署

Cloudflare 的独立 Worker 配置、构建、验证与回滚说明见 `deploy/cloudflare/README.md`。
<https://boomerang-fu.arkevo.ai/version.json> 提供版本及文件 SHA-256，便于其他项目核对依赖。
部署在服务端把大文件分片流式还原，浏览器收到的仍是完整原文件。
