# 自由击杀地图范围

核对对象是从 arkena-host 取得、逐文件 SHA-256 校验的原安装。依据是
`LevelManager.levelAssets` 的 58 条地图定义及 `BuildMatchPlaylist`，不是
Unity BuildSettings 中的场景文件数量。自由击杀对应 `DeathMatch` 且 `teamMatch=false`。

| 默认对局人数 | 本体常规地图 | Just Desserts DLC | 全部已解锁时合计 |
| --- | ---: | ---: | ---: |
| 2–4 人 | 36 | 21 | 57 |
| 5–6 人 | 31 | 21 | 52 |

59 个已导入资源场景还包含教学关 `level28`，以及同一路径
`Assets/Scenes/LevelToaster.unity` 的两个构建条目 `level8` / `level33`。
烤面包机在 LevelManager 中只有一个 LevelAsset；不能算成两张可选地图。
这版安装的 58 个 LevelAsset 全部支持 DeathMatch，没有混入捉迷藏专属地图。

5–6 人默认排除 Enclosed、Mini Island、Plants、Wheel、Temple Garden；
它们的原版 `maxPlayers=4`。原版自定义地图选项会绕过人数限制，仍检查模式和 DLC 解锁。
教程只可能在首次对局按设置插入，不属于常规选图列表，也不进入重赛地图池。

开发验收分别统计本体 36 张与 DLC 21 张，优先补齐本体；教程单列。
已经导入的全部 59 个资源场景保留用于核对，`?map=levelN` 目前仍是指定场景的开发入口。
原版地图池筛选、DLC 状态和跨回合地图轮换尚未接入当前运行时，不能用加载成功代替这些功能。
本次审计不推断用户账号的 DLC 所有权。

完整逐图配置：`godot/assets/original/map-catalog.json`。
核验结果与源文件哈希：`evidence/ffa-map-pool-audit.json`。
可复现提取工具：`tools/export_map_catalog.py`。
