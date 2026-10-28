# 先读这里：Boomerang Fu 0.5.1 交接

这是可以放进独立仓库继续开发的 Godot 浏览器项目。线上固定入口：
https://boomerang-fu.arkevo.ai/releases/0.5.1/
原仓库固定 tag：`boomerang-fu-v0.5.1`（`xiongjjlj/xiongjjlj.github.io`）。

## 先运行，再改代码

1. 解压后进入本文件所在目录。直接试玩只需 Python 3：
   `python3 -m http.server 8080 --directory web`，浏览器打开 `http://localhost:8080`。
   Windows 可将 `python3` 换成 `python`。不要直接双击 HTML 使用 file://。
2. 编辑游戏用 **Godot 4.7.2 Standard**，打开 `godot/project.godot`，运行主场景。
   不需要 Unity 账号。不要把 `core/original_scene_materials.gd` 等辅助脚本当成独立程序运行。
3. 重新构建，传入本机 Godot 可执行文件：
   `python3 tools/build_web.py --godot /path/to/godot --output dist/web`。
   包内附带匹配的单线程 Web 导出模板。脚本使用临时工程修正本机路径，导出后补上下载重试修复，
   不会覆盖发布用的 `web/`；输出目录必须为空。一次只运行一个构建，预留 2 GiB 磁盘。
4. 自测新构建：`node tools/test_web_loader.mjs dist/web/index.js`；
   再运行 `python3 -m http.server 8081 --directory dist/web`，浏览器实际开局和结算。

Godot 编辑器可执行文件需由同事在自己的系统安装；包内 Web 导出模板不是编辑器。
`godot/export_presets.cfg` 保留历史开发路径，手动通过 GUI 导出时要改成自己的模板路径；
上面的 `build_web.py` 会自动处理。Python 3.10+，Node.js 22.18+；纯试玩不需要 Node。

## 包内内容

| 路径 | 用途 |
| --- | --- |
| `godot/` | 可编辑工程、GDScript、场景、原始转换后的模型/贴图/动画/地图数据 |
| `web/` | 与线上 0.5.1 一致的 16 个可托管文件，包含引擎、游戏包和音频 |
| `recovered-source/` | 恢复的原版 C# 参考源码；浏览器实际执行的是 GDScript |
| `reference-settings/` | 原安装读取的项目设置，供对照 |
| `research/teleportals/` | 尚未接入运行时的传送门探针源码、输入及原生结果（交接补充） |
| `tools/` | 导出、原生探针、转换器和验证工具的源码 |
| `evidence/` | 原生对照、运行时测试、浏览器记录及已知差异 |
| `vendor/godot-4.7.2/` | 匹配的 Web debug/release 导出模板及哈希说明 |
| `deploy/cloudflare/` | 已验证的托管代码与锁文件；无登录凭据 |
| `INTEGRATION.md` | 固定 URL、iframe 示例、版本和接口边界 |
| `MAP_SCOPE.md` / `WATER_RESTORATION.md` | 地图范围、水面还原及剩余缺口 |
| `VERSION.json` / `SOURCE_REVISION.txt` | 发布源文件清单与来源提交 |
| `KIT_MANIFEST.json` | 交接包逐文件 SHA-256；不包含自身 |

没有打包账号凭据、机器 SSH 配置、node_modules、.godot、bin/obj 或原商业游戏安装目录。
现有 Godot 工程、Web 运行和常规开发不依赖原安装；重新提取原资源或重跑 Unity 原生探针时，
需要自行准备匹配的原安装和原生运行环境。已有提取结果及对照输入/证据已保留。

## 放入同事的新仓库

在解压目录执行 `git init` 后提交源文件即可。包内 `.gitignore` 默认排除 `web/`、`dist/`、
导出模板 ZIP 和缓存：`web/index.pck` 超过 GitHub 单文件限制，不要直接 `git add -f`。
可把可运行包/模板作为新仓库的 Release 附件或对象存储；源工程和转换后的资源随代码提交。
公司 Cloudflare 配置指向 `arkevo.ai`，不会提供身份；同事需要自己的账号、域名和部署配置。
请勿在新仓库直接使用旧公司 profile 覆盖现有 Worker。

## 当前状态和后续开发

稳定是指可冻结依赖和明确接入范围，不代表 100% 还原。默认自由击杀本体地图：
2–4 人 36 张、5–6 人 31 张；可选 Base + DLC 为 57/52 张。59 是资源场景数，
包含教程和重复 Toaster，不应直接作为常规选图数量。

已接入本地战斗、计分、暂停、重赛、换图、机器人与若干地图机关。73 项运行时套件通过；
原版水面/日间光照经过 168 个受控像素样本对照。线上与跨站 iframe 已实测回合结算。
首次公网加载可能很慢，本次冷加载观测约 7 分半；需要继续拆包或优化交付。

优先继续做：地图传送门及 AI 连接；其他 Shader/反射/雾/粒子；原菜单和完整音频；
完整回调、随机流与原版逐帧验证。传送门准备工作仅有探针输入/原生结果，未接入正式运行时。
当前没有联网多人、远程 Agent 控制、比分 postMessage API 或确定性重放保证。
不要让依赖项目把调试日志当作稳定 API。其他项目可先通过 iframe、独立 URL 或固定源码集成。

发布后保留 0.5.1 的文件、固定路径和 tag；新增功能使用新的版本号与开发分支。
