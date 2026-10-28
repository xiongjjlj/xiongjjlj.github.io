# 开发与固定依赖

稳定浏览器版位于 <https://feixiong.me/boomerang-fu/>。其他项目可固定提交
`ef21423cfcfcf4e1e5a9ff8cb16f35ba883e52d1`，源码目录是 `boomerang-fu-source/`；
`boomerang-fu-v0.5.0` 标签保留在该提交。`boomerang-fu-dev` 接受后续开发。

当前源码与开发 Web 包为 `0.5.1-dev.8`。公司预览位于
<https://boomerang-fu.arkevo.ai/>，线上实际版本以 `/version.json` 为准。
稳定依赖 `0.5.0` 保留，后续开发不改变其提交和标签。
源代码、浏览器包与证据分别有哈希，历史报告只证明当时记录的构建。

开发包为 `web-development-0.5.1-dev.8.zip`，解压后通过 HTTP(S) 托管。
完整 PCK 超过 100 MiB；附带的 Cloudflare Worker 会将存储分片合成为原文件响应。
59 个已导入资源场景分别使用自己的原版寻路网格。地图范围已按原版 LevelAsset 核验：本体 36 张、DLC 21 张，另有教程和重复 Toaster 构建条目；详情见 `MAP_SCOPE.md`。运行时障碍重扫已接入；传送门连接仍待完成。

普通 MovingFloor 已接入 Split、Drift、Tracks、Spin、Donut、Four Cogs、Raft Crusher。
角色和落地回旋镖随平台移动，退出接触后解除附着，回合重置恢复初始轨迹。
探针直接调用原安装程序集的 `MovingFloor.ToggleTargetPosition/FixedUpdate`，
比较 38,416 帧。位置最大差约 0.0000022，速度最大差约 0.000363；
显式浮点容差不等于完整游戏逐帧一致。

响应式平台接入 `level40`、`level41`、`level46`、`level51`、`level52`、`level53`，
共 14 个平台，包含开关启动、预备延迟、中途反向、弹簧等待和返回，以及配对滚轴。
探针在原 UnityPlayer 内执行未改写的恢复源码方法体，使用真实 Transform/Mathf；
时钟、导航及呈现服务由探针适配，区别于原游戏完整场景执行。
42 组轨迹共 37,842 个时点通过，位置最大差小于 0.000001，速度差小于 0.000081。

`godot-map-runtime-check.json` 的 73 项检查包括六张响应式地图、平台寻路和完整游戏循环。
Big Raft 浏览器实测了近战开关、平台载人、途中反向、滚轴、暂停恢复与完整单程。
中央区域角色完整往返不掉落；外侧角色会受到固定滚轴阻挡，随后失去脚下平台。
这项行为由 `godot-reactive-floor-game-loop-check.json` 单独覆盖，未据此声称原版回调一致。
对应报告为 `godot-reactive-floor-native-check.json` 和 `godot-reactive-floor-browser-check.json`。

59 个资源场景的 300 个原版网格已解码；992 条原版 ABPath 的节点序列、边缘处理和
平滑坐标完全匹配。发布数据只包含网格输入，不包含用于对照的路径结果。
13 张平台地图的 1,134 次选图结果一致，540 次正反变换最大位置差约 0.0000043。
原版 `GraphTransform` 的近似平移判断、旋转四元数的浮点保留和矩阵计算规则均已接入。
平移和旋转平台上的机器人保留原路径坐标，逐帧更新世界坐标；失败重算保留旧路径，
回合重置恢复平台位置。Circle Maze 的响应式旋转机关按原版保留世界坐标路径。
此处已接入分区重扫队列；完整原版回调调度顺序仍未证明一致。
相关报告为 `godot-navigation-maps-check.json`、`godot-navigation-selection-check.json`，
以及各图的 `godot-navigation-scene-*-check.json` 和 `godot-navigation-platform-ai-*-check.json`。

其他地形机关、传送门寻路连接、完整 Unity 触发顺序、平台音效/闪烁/绳索/拖尾及地图波浪
仍待完成。探针输入生成器在 `tools/moving-floor-native/` 和 `tools/reactive-floor-native/`；
使用已有隔离 Windows 副本，不需要 Unity 编辑器，每次一个进程、30 秒和 2 GiB 上限。

动态寻路使用原版 `GridGraph.UpdateArea` 的受影响范围、物理检测、相邻节点连接、
基础代价和危险标签规则。原 UnityPlayer 中两种扫描半径的 22 组地形变化，合计
6,230,791 项比较和 66 条路径完全匹配。物理检测包含原版圆形与旋转矩形障碍、
地板消失和恢复、可走性覆盖，以及重扫时保留危险标签。

移动墙、连续旋转障碍和 Circle Maze 的响应式旋转机关已连接重扫队列，
可破坏障碍消失后会刷新网格；已有路径上的节点失效时，AI 会请求新路径。
重开一局恢复原始网格，再处理随机起点的移动墙。四张实际地图的机关、销毁和
复位测试通过；Circle Maze 双机器人完整循环运行 360 帧、执行 28 次重扫。
报告为 `navigation-updates-restoration-check.json`、`godot-navigation-updates-game-check.json`
及 `godot-navigation-updates-scene-*-check.json`。原版整体调度和画面一致性仍待验证。

落地更新 `0.5.1-dev.6` 接入 12 个资源场景中的塌陷/踩踏地板：预警、下沉、恢复、危险标签、寻路重扫、书本落水、灭火与回合重置。地板状态 259,752 项、塌陷排程 56,252 项、危险区域 525,720 项原引擎对照均无差异；这些是隔离方法验证，未证明整局回调顺序。浏览器检查 Falling 2 的塌陷/死亡/复位，以及 Trap Maze 2 的踩踏下沉；恢复由原生状态及实际场景检查覆盖，浏览器短局未观察到恢复。地板粒子、碎片服务和投射物附着仍有缺口。报告为 `floor-hazards-restoration-check.json` 与 `floor-hazards-browser-check.json`。

## 地图池与跨回合换图（0.5.1-dev.7）

普通入口默认 Base maps，2–4 人 36 张、5–6 人 31 张；Base + DLC 为 57/52 张。
教程和 Toaster 重复构建条目不进入常规轮换。`?map=levelN` 是固定地图开发入口。
原版筛选、Chance 随机洗牌、首次简单地图优先、DLC 排序、循环及重赛行为已移植；
304 组隔离原版 Mono 对照、53,916 项比较无差异。自定义地图和 Switch 分支也有逻辑对照，
网页只提供本体/DLC 选择。DLC 开关表示内容选择，不查询账号授权。

实际场景切换的 41 项检查覆盖比分、控制器配置、道具、相关 RNG 状态、重赛清零和重复输入。
66 项运行时回归全部通过。浏览器确认地图数、连续三回合换图、比分保留、返回选人保留设置，
以及重新选择本体开局；日志无错误。证据见 `playlist-browser-check.json`、
`godot-match-playlist-check.json`、`godot-playlist-scene-transition-check.json`。
原菜单呈现、过场时间、教程流程和完整游戏随机数交错仍待补齐。

## 水面与材质输入（0.5.1-dev.8）

219 条材质记录的 Shader 身份和纹理输入已补齐，182 个纹理槽经绑定检查。
原版水面的深度/边缘渐变和日间光照已接入，保留各场景原始启用状态。
168 个原引擎 GPU 与 WebGL 受控样本全部通过，最大误差小于 1/255；
六张实际地图检查光照与水面可见性，当前 73 个运行时套件全部通过。
反射、雾效、其他 Shader 类型及完整场景画面仍待完成。重现步骤及证据见 `WATER_RESTORATION.md`。

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
| 普通 MovingFloor | 7 个 FFA 关卡、16 个平台已接入；38,416 帧原版运动对照，仍有显式浮点容差 |
| MovingFloorReactive / RollingPin | 6 张地图、14 个平台已接入；37,842 个源码方法时点对照，完整呈现和回调顺序仍待验证 |
| 各地图网格、平台选图和路径跟随 | 59 个资源场景、300 个网格，992 条原版路径、1,134 次选图对照；动态重扫已接入，传送连接待完成 |
| MovingDoor | 仅教程关卡使用；待实现 |
| FallingFloor、TrapFloor | 状态、运动、导航和回合重置已接入；粒子、碎片服务及完整附着仍待完成 |
| Teleportal | 数据已解码；关卡传送和寻路连接待完成 |
| 原版地图池、人数/DLC 筛选及跨回合地图轮换 | 已接入；默认本体 36/31 张，DLC 开关扩至 57/52 张；教程流程及账号授权检测未接入 |
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
