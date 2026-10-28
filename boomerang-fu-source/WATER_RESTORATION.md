# 水面与材质输入（0.5.1-dev.8）

本次接入原版 `FX/Water_DepthVignette` 的深浅水颜色、深度渐变、边缘渐变、
直接光照和日间环境光。Shader 从原安装的 D3D11 程序反汇编后移植；
光照系数由原 UnityPlayer 的 `SphericalHarmonicsL2` 和 `Color.linear` 计算。
这里只证明已列出的受控场景，不代表整幅游戏画面一致。

全部 219 条已导入材质记录补齐了 Shader 名称和纹理输入，涉及 116 张纹理；
与现有 PNG 内容相同的纹理复用已有文件。运行时检查确认 182 个纹理槽绑定，
原文件中的 3 个空 Shader 引用也原样保留。输入齐全不等于所有 Shader 已移植。

39 个资源场景中有 44 个水面渲染对象，原始层级实际启用 35 个，分布在 33 个场景。
计数包含教程和重复 Toaster 构建条目，不能用作自由击杀的地图数量。
Turnstile 等场景的备用水面保持关闭；Onsen 的 6 个水面对象中只有 3 个启用。
完整记录见 `evidence/water-source-visibility.json`。

| 验证 | 结果与范围 |
| --- | --- |
| 原 Unity GPU 与 Godot WebGL | 3 种原水面材质 × 4 个深度 × 7 个位置 × 2 种光照，共 168 个像素；最大颜色误差 0.002951，小于 1/255 |
| 光照 | 白色直接光，以及 Turnstile 日间主光、环境光、次要灯的球谐光照；反射和雾关闭 |
| 材质输入 | 219 条记录、182 个纹理槽、3 种水面材质，756 项检查 |
| 实际地图绑定 | Turnstile、Island、Onsen、Big Raft、Donut、Trap Maze 2；检查原始可见性和各图光照 |
| 游戏回归 | 73 个运行时检查套件通过；具体源码哈希见 `godot-map-runtime-check.json` |

浏览器像素证据为 `evidence/godot-water-lighting-web-render-check.json`，原引擎样本为
`evidence/water-lighting-render-native.json`；源码与产物哈希见
`evidence/water-lighting-render-source.json`。此前 84 个白光样本报告保留为历史证据。

重新准备独立 WebGL 验证工程：

```sh
python3 tools/water-render-web/prepare.py \
  --project /absolute/path/to/water-check \
  --template /absolute/path/to/web_nothreads_release.zip
godot --headless --editor --path /absolute/path/to/water-check --import
godot --headless --path /absolute/path/to/water-check \
  --export-release Web /absolute/path/to/web/index.html
python3 tools/serve_preview.py --root /absolute/path/to/web --port 57889 --seconds 600
```

先检查机器资源，每次仅运行一个导出或原生验证进程。打开预览后读取浏览器控制台的
`BF_WATER_RENDER_CHECK`，确认 `passed`、样本数和误差；Headless 加载不能替代 GPU 检查。
验证工程使用公开在本源码内的固定输入，不需要 Unity 编辑器。

反射探针、雾效、季节/夜间变化、其他 Shader 类型、完整水面与粒子效果仍待完成。
受控样本没有覆盖所有观察角度、阴影和动态效果；完整游戏的像素、音频与回调时序尚未证明一致。
