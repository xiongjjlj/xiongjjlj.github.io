# Boomerang Fu Godot 浏览器移植工程

目标：在浏览器本地运行、可自行托管的 Boomerang Fu 自由击杀模式；按原版验证游戏逻辑、地图、角色、动画、视效、音频和输入行为。目标仍是用户要求的完整还原。按用户 2026-10-27 的要求改用 Godot。当前 Web 构建可进行 2–6 人混合真人/机器人对战：移动、冲刺、投掷、蓄力、返回、近战、出招位移、基础弹反、死亡、计分与回合流程已接通。空手缴械、掉镖与飞镖互撞也已接入。道具和完整视听表现仍不完整，没有达到逐帧一致。


在线试玩：[feixiong.me/boomerang-fu](https://feixiong.me/boomerang-fu/)。已实测公开地址加载、双人操作、音频及暂停恢复。当前为未完成还原的开发版本。

稳定依赖点已发布到 GitHub：[源码快照 `boomerang-fu-v0.5.0`](https://github.com/xiongjjlj/xiongjjlj.github.io/tree/boomerang-fu-v0.5.0/boomerang-fu-source)，对应提交 `2e96a62`；[可继续开发的 `boomerang-fu-dev` 分支](https://github.com/xiongjjlj/xiongjjlj.github.io/tree/boomerang-fu-dev)从同一版本开始。发布网页文件位于同一 tag 的 [`boomerang-fu/`](https://github.com/xiongjjlj/xiongjjlj.github.io/tree/boomerang-fu-v0.5.0/boomerang-fu)。依赖项目可固定 tag，地图和视觉补全继续在 dev 分支推进。

最新构建（2026-10-28）：已接入正常的角色选择、短/中/长局、开局提示、暂停恢复、回合结算、继续、加赛和重赛。可配置 2–6 个本地玩家，使用两套键盘、接入的手柄或机器人；六个原角色、头像、字体、模型和动画曲线保留。Unity BuildSettings 中的 59 张原始对战地图（`level2`–`level60`）已经导入 Web 资源包，可用 `?map=levelN`（例如 `?map=level38`，`slide` 仍是 `level3` 的别名）选择；逐张 Godot 启动与共享 FFA 初始化检查全部通过。LevelSlideTraps 另有 MovingObstacle、Switch、WobblyObstacle、可破坏障碍、飞镖墙面回调和玩家近战障碍回调适配；其他地图目前共享 FFA 运行时和原始几何/碰撞体，地图专属移动平台、传送门、落水、粒子、音频和 Animator 仍需逐项移植。

浏览器实测双人操作、新角色选择、菜单/对局音乐、暂停滤波、计分和下一回合；无控制台错误。最新整合验证覆盖 37 组检查，包含六人计分、AI 跨回合复用、72 段动画绑定、六人火/冰效果、加赛和反复切换人数后的节点回收；对应报告记录 45 个核心脚本的 SHA-256。13 种普通道具（含伪装、缩小边界和诱饵）和原始 FMOD 音频库已接入，音频内存上限为 128 MiB；最新 Godot 回归套件覆盖道具书、六人 FFA、诱饵、伪装和边界，全部通过。

解压 `web-main.zip` 后通过 HTTP(S) 提供整个目录。进入大厅点击 FIGHT；Esc 暂停，F9 打开调试控件，点击 Sound 开启音频。P1：WASD / Space 冲刺 / J 攻击 / K 按住并松开投掷；P2：方向键 / Enter 冲刺 / Slash 攻击 / Shift 按住并松开投掷。

最新证据：`evidence/ffa-flow-source-contract.json`、`evidence/godot-ffa-browser-check.json`。机器人原生逐帧对照、其余地图和角色、原版菜单动效、完整视效及原生调度仍有缺口。未达到用户要求的 100% 或逐帧一致，原生/浏览器音频运行库也未证明 PCM 一致。

2026-10-28 后续开发：机器人目标选择已完成 256 组原版对照，9,216 项比较完全一致；格挡、近战、投掷和冲刺的即时动作分支已完成 1,024 组对照，命令顺序、随机数状态和离散结果一致。17,773 项动作比较中有四项旋转分量相差 5.96×10⁻⁸，已单独记录，未宣称逐帧一致。按键协程另以 Unity 真实 Update / 协程 / LateUpdate 验证，覆盖变帧时长与时间冻结：96 组各 32 帧，共 3,072 条逐帧记录、82,944 项比较完全一致。最新浏览器验证覆盖 43 个核心脚本、37 组检查，控制台无错误；详见 `evidence/godot-player-ai-buttons-web-check.json`。上述基础模块现已串入机器人行为循环并打入 Web 和 ZIP。最新整局集成检查覆盖四档难度各 1,200 帧，及一名真人加五名机器人的场景；机器人 AI 组件现在跨回合复用并在场景销毁时显式释放。浏览器实测两人和六人混合对局、机器人击杀、计分、继续及暂停恢复，控制台无错误。详见 `evidence/godot-player-ai-game-check.json` 与 `evidence/godot-player-ai-game-web-check.json`。本轮又把完整原版 `PlayerAI` 与 `MonsterLove.StateMachine` 放进隔离 Unity 原生探针，以真实 `Update` 顺序、状态回调、路径回调和 `WaitForSeconds` 跑完九个 FFA 状态、每状态 96 帧；进程正常退出且无残留。详见 `evidence/player-ai-loop-native-contract.json`。仍未宣称 100%：探针使用受控空物理世界，植被遮挡、移动平台/开关/传送门、真实寻路图和寻路池随机种子、全局随机数交错及完整场景视觉/音频仍有缺口。

后续各节记录分阶段的验证结果，其中的构建数量与“当前”均对应当时版本。最新状态以以上说明和 `evidence/progress.json` 为准。

2026-10-27 更新：核心数值基线已升级为原游戏 UnityPlayer / Mono，并在浏览器 WebAssembly 中复跑通过。共 13,200 个运动/时钟时间步、393 组数学/弹反样本；比较没有使用误差容限。记录见 `evidence/native-runtime-contract.json`。测试范围仍有限，圆形扫掠、任意角度旋转、完整功能和逐帧画面仍有缺口。

## 已有实物

- 从用户本地原版程序集重新恢复了 518 个 C# 文件，共 78,205 行。使用原版依赖重新反编译后，`recovered-source/` 已通过 .NET 编译：0 错误、101 警告。这只验证恢复代码，不等于 Godot 或浏览器已还原原版。
- `Assembly-CSharp.dll` SHA-256 为 `7e5a31b96c64150455340067314538af3399b723cd11d9891a065bde2dce2a6f`；与 2026-10-07 保存的副本相同。
- `globalgamemanagers` 确认 Unity 版本为 `2022.3.62f2`，恢复了 ProjectSettings 和 61 个构建场景的原始路径。
- 原 Fixed Timestep 为 `0.016666`；2D 重力为零；Velocity Iterations 为 2，Position Iterations 为 1。这些来自序列化数据，不是按视频估值。
- 模式对应 `SettingsManager.MatchType.DeathMatch`、`teamMatch=false`、`VictoryCondition.Kills`。原作包含击杀加分、自杀扣分、同回合多名赢家及加赛处理。
- 本地自有实机录像可用于外观参考，但它是 30fps 有损录像，不能证明 60Hz 原生帧完全一致。正式验证需要同版本、同场景、同初始状态、同输入的无损捕获。

## 当前缺口

2026-10-27 17:14–17:19（北京时间）已成功通过 SSH 读取新 Windows 的原版安装：`D:\Steam\steamapps\common\Boomerang Fu\Boomerang Fu_Data`，370 个文件，共 655,143,644 字节，并取得逐文件 SHA-256。原版 `Assembly-CSharp.dll` 与上述本地副本一致。远端已生成一个 138,158,199 字节的完整数据归档及一个 11,828,189 字节的 Managed 依赖归档，两个压缩进程均已退出。

2026-10-28：完整数据归档已传回本机，归档 SHA-256 与远端记录一致，解包后的 370 个文件逐一验证大小和 SHA-256 全部通过。共享资源、音频银行和全部安装数据现已在本机可用；它们还需要逐项转换、绑定和实测。原安装只读；远端三个任务临时归档已在校验后清理。清单见 `evidence/original-installation-manifest.json`，完整校验见 `evidence/full-data-verification.json`，归档清理见 `evidence/reference-archive-cleanup.json`。

原版直接依赖已补齐并逐文件校验。编译记录见 `evidence/compile-check.json`。声明元数据对照中，695 个类型及其基类、接口、字段和方法签名一致；六个接口的 BeforeFieldInit 标记有差异，完整原始结果保留在 `evidence/metadata-contract-check.json`，没有标成完全一致。

已校验取得全部 61 个场景文件。Godot 的 `tools/export_godot_scene.py` 保留原始层级、变换、碰撞体与渲染资源引用；其中 59 张对战地图已进入 Web 资源包，节点数和网格引用按源场景保存。该场景的网格与材质引用已补齐（47 个网格、16 个材质、10 张贴图、187 个渲染实例）；SpriteRenderer 与全局运行时场景尚未导入。当前材料只是预览，原始 Shader、灯光与后处理仍需移植。

两名角色已通过 GLB 导入 Godot；15 个蒙皮网格的骨骼与绑定矩阵保留，单骨骼网格的隐式权重已展开。原始动画流直接解码为三次曲线系数、常量和密集采样通道，运行时不将曲线重新烘焙成固定帧。待机与行走检查没有未绑定目标；实际浏览器中已验证切换和慢动作。表情贴图的原始缩放与偏移通过 `KHR_texture_transform` 保留。记录见 `evidence/godot-character-assets.json`、`evidence/godot-character-import-check.json` 和浏览器截图；这不证明 Animator 状态机或逐帧画面一致。

Player、Disc 与两名角色的序列化组件已读取。TypeTreeGenerator 的组件头缺少 `m_Enabled` 对齐，现使用对应 Unity 版本的标准头替换，并检查脚本引用一致。相关转换从原安装只读执行，完整导出与修正补丁均已校验 SHA-256；合计 87 个网格、41 个材质、29 张贴图和 120 个动画相关资源，无未解析引用。当前网页包含两名角色的待机、行走曲线，以及原版飞镖模型和 DiscSpin、DiscFlying、DiscGrounded 曲线。

## Godot 路线

使用官方 Godot 4.7.2 标准版及单线程 Web 模板，Compatibility 渲染器。Godot 4 的 C# 项目目前不能导出到 Web，因此 `recovered-source/` 作为规则参考，浏览器执行逻辑移植为 GDScript。Unity 编辑器下载已停止，不再等待 Unity 授权。

`godot/core/original_timekeeper.gd` 已移植命中停顿、慢动作与暂停。当前基线在原安装的 UnityPlayer 2022.3.62f2 / Mono 中执行恢复的 TimeKeeper 方法及原版数学 API，注入时间与游戏服务；1,800 帧被比较字段逐值一致。Godot WebAssembly 也通过同一批对照。旧 CoreCLR 夹具只作为历史中间结果，不能替代原游戏运行时。

`godot/core/original_ffa_score.gd` 已移植自由击杀计分决策，10 个边界场景检查通过；现已接入普通飞镖命中、角色死亡与回合分数。`original_ffa_round.gd` 保留最后存活者检查的 0.15 秒与 1.3 秒等待阶段，死亡事件会重新开始检查；等待期间仍可发生死后击杀和相互死亡。默认双人 Medium 对局目标为 14 分，来自 `SettingsManager.PrepareMatchLength`，并非使用字段初始化时的 10 分。原版菜单、转场、地图轮换和完整决胜表现尚未移植。

`godot/core/original_actor_motion.gd` 已移植加速、滑行、冲刺、冷却、瞄准后移动恢复、湿地阻力、击退和眩晕。原游戏 Mono 运行时的 17 组、3,060 个时间步，与 Godot 桌面及 WebAssembly 的被比较状态逐值一致。接触时机仍由夹具注入，未证明完整原生碰撞、角色四元数旋转或 Animator 一致。

`godot/core/original_disc_motion.gd` 已移植普通飞镖飞行、返回、自由运动、收回、旋转弧线、追踪与反弹。原游戏 Mono 运行时的 24 组、4,800 个时间步，以及 30 组弹反响应、10 组互撞共 1,200 个时间步，与 Godot 桌面及 WebAssembly 逐值一致。保留原版初始化两次 StartThrow、Free 状态两次加力等调用顺序。源方法在独立程序集内重新编译执行；空间查询、接触时机及协程仍是替身，不是完整原版对局重放。

`original_player_weapons.gd` 接入库存、蓄力、方向采样、投掷、收回、接镖冷却与蓄力接镖后坐；手中模型按库存显示，并使用原角色的飞镖颜色。13 项集成检查通过，包括飞出后接回、取消收回、输入松开不重复投掷，以及模型与曲线绑定。拾取与伤害现在由 `original_combat_world.gd` 处理固定步中的扫掠和固定步后的重叠；保留不同的查询半径与实际触发体半径，但尚未与 Unity 原生查询和回调顺序比较。

伤害路径按 DamageCollider → Player → Actor 的保护条件处理遮挡、冲刺、无敌、护盾、已弹开的飞镖、本人飞镖与正面攻击保护。24 项边界及集成检查通过，覆盖重复回调不重复计分、死后击杀、相互死亡、公共飞镖和冲刺期间接触后的 Stay/Enter 区别。死亡切换使用原 CharacterPlayer 的 bodyNormal/bodySliced 引用及两段 DeathSliced 原始曲线；飞出的 DeathSlice 身体部件、音效与粒子尚未接入。可操作的持械近战、飞镖弹反和双方近战互挡已接入；空手缴械已接入；完整防御和道具组合仍未完成。

近战使用原 Player prefab 的九顶点 PolygonCollider2D 及原始父子变换。攻击有效期 0.2 秒、位移加力 0.1125 秒、移动锁定 0.5 秒和攻击冷却 0.66 秒分别保留。原游戏 Mono 运行时的 13 组、2,340 帧位移和冷却状态与提取的原 C# 方法逐值一致；34 项碰撞边界检查、10 项输入顺序/缓冲检查及实际地图近战/弹反集成检查通过。八段左右手蓄势与挥击动画保留原始曲线，全部目标绑定；原 Animator 的混合和中断语义尚不完整。普通和满蓄力弹反保留停顿、推退及冷却分支；原生碰撞顺序、保护协程边界和四元数运算仍需实机对照。证据及限制见 `evidence/melee-source-contract.json`。

空手缴械沿用原攻击位移，移动锁定乘 0.85；目标有武器或被冻结时眩晕 0.8 秒，空手目标为 0.5 秒。一次只打掉一枚飞镖，保留原掉落初始化、原主人、减速转公共飞镖及拾取顺序，不直接计击杀分。49 项边界检查通过，覆盖护盾、冲刺、无敌、冻结、遮挡、已有眩晕、重复接触和两秒击退归属。Disarm 与 Stunned 四段角色原始曲线已解码且全部绑定。

飞镖互撞按原版显式扫掠触发，先调用被撞飞镖，再调用移动飞镖；相对速度使用对方上一步保存的速度。原版 Physics2D 矩阵关闭 Projectile/Projectile 和 Disarm/Player 原生触发，已保留该区别。恢复代码里，Disc.CollisionEnter2D 在基类登记碰撞后立即返回，因此没有执行后面的 2000 冲量分支；原游戏 Mono 运行时的 10 组、1,200 步对照也保留了这一行为，逐值一致。实际地图测试通过互撞、双方接回、短暂眩晕和重开，节点数回到 993。无窗口 dummy 渲染器在双镖回收时报告一次空材质诊断；真实 WebGL 浏览器验证未复现该诊断，控制台零错误和零警告。完整证据见 `evidence/disarm-source-contract.json`。

飞镖的原始 Z 高度已进入运动状态：投掷为 -0.75，掉落为 actor Z - 0.75。返回、追踪、弹反、主人保护与接镖方向保留 Vector3 运算后再转 Vector2 的顺序，物理形状仍在 XY。363 组原版数学样本在原游戏运行时和 Godot WebAssembly 中逐值一致。射线保留三维距离，再由查询归一化二维方向。当前场景已使用这些计算。边界、道具等尚未实现的路径仍待补齐。

`tests/duel_integration_test.gd` 在实际地图场景中连续投掷并跑完 14 个击杀回合，检查死亡动画绑定、跨回合分数保留、达到目标分数和重新开局，全部通过；重开前后节点数均为 993。这个检查覆盖当前普通飞镖对战流程，不能证明原版完整自由击杀或逐帧还原。报告见 `evidence/godot-duel-integration-check.json`，当前差异列在 `evidence/combat-source-contract.json`。

`godot/core/original_collision_world.gd` 从场景加载 124 个有效碰撞形状，另注册两个玩家圆形碰撞体；连续圆形扫掠、墙面滑动、重叠恢复与玩家接触的 10 项几何／集成检查通过。此查询实现尚未与 Unity 的原生 Box2D 查询逐项比较。碰撞后的换镖、可选的冲刺缴械（原默认关闭）、燃烧传递、完整击杀归属和动态障碍仍待接入。 原生小型探针已确认非单位射线方向的处理，但发现圆形扫掠距离约 0.005 的差异，以及 23.456 度旋转的舍入差异。游戏当前使用的 -45/±90 度样本一致；没有把任意角度或全部空间查询标为一致。

从原始 MonoScript 读出的执行顺序保存在 `evidence/unity-script-order.json`：插值控制器 -100、插值变换 -50、PlayerInput -3、Player -2、RewiredController/TimeKeeper 0、插值更新器 100。预览据此组织固定步、输入与显示插值，并处理冲刺时清空插值缓存；完整原生输入映射、时间调度与四元数一致性仍待验证。

`godot/` 是可编辑源工程；`web/` 是开发预览静态产物。运行 `python3 tools/serve_preview.py` 会启动仅监听 127.0.0.1、默认 15 分钟退出的预览服务器，并打印地址。P1 使用 WASD 移动、空格冲刺、J 近战、K 投掷；P2 使用方向键移动、回车冲刺、/ 近战、Shift 投掷。先移动调整朝向；按住投掷键蓄力，松开投掷；空手时按住同一键收回，按攻击键缴械。回合结束按 Next round 继续，Restart match 清空分数重新开始。这是预览控制方案，未宣称复现 Rewired 的原始键位和手柄映射。浏览器已实际验证双方飞镖及近战击杀、跨回合计分、冲刺躲避和近战弹反。近战按键实测先由 P1 得分到 1:0，再由 P2 得分到 1:1，第三回合 P1 挡回 P2 飞镖；控制台无错误或警告。近战截图与记录为 `evidence/godot-melee-browser-kill.png`、`evidence/godot-melee-browser-parry.png` 和 `evidence/godot-melee-browser-console.json`。最新浏览器实测通过同时投掷互撞后双方接回，以及两次空手缴械、掉镖与眩晕，记录见 `evidence/godot-disarm-browser-console.json`；实拍为 `evidence/godot-disarm-browser.png`。 当前构建的数值验证页为 `web/validation/index.html`，复制相同的 17 个核心脚本并记录 SHA-256；15 组数值检查见 `evidence/godot-dash-web-check.json`。复制文件不表示其全部路径都已覆盖。可部署的主游戏压缩包为 `web-main.zip`，包含静态文件；需要通过 HTTP(S) 服务，不能直接双击 HTML。当前 `index.html` / `index.pck` 已部署到 [feixiong.me/boomerang-fu](https://feixiong.me/boomerang-fu/)，GitHub Pages 工作流已成功完成。

## 道具移植进度

`original_player_powers.gd` 已接入三能力上限与先进先出淘汰、重复拾取、冰火互斥、反向操作计时，以及跨回合保存。9 个恢复的 Player 方法在原版 UnityPlayer / Mono 内执行；14 组流程、3,077 个状态快照与 Godot 桌面和 WebAssembly 被比较字段逐值一致。UI、地图服务和拾取特效等依赖由测试替身提供，这不等于全部道具效果已经移植。

当前网页可通过临时道具面板验证加速、额外飞镖、6 秒反向操作和护盾。护盾保留原始 1.4 × 1.33 碰撞半径、单次承伤、反弹、击退／眩晕、近战中断，以及破碎后的 0.85 秒非缩放无敌时间；33 项边界检查通过。浏览器实测护盾挡镖并反弹接回、近战撞盾后双方眩晕，双方存活且分数不变；控制台零错误和零警告。

原版道具书的 6 个节点、2 个网格及出现／待机／收取三段动画已导入，包含发光材质的四条颜色通道。已接入原始生成间隔、回合门槛、9 点地面检测、排除半径、选择池、Pickup Stay 拾取和 0.2 秒延迟销毁；21 项检查通过，节点数在生命周期前后均为 1,015。浏览器已用键盘移动并拾取道具，得到护盾且书移除，控制台无错误或警告。普通频率从第 5 回合起满足生成门槛，临时面板的 Spawn a test book 可立即验证。当前只开放加速、护盾、额外飞镖和反向操作四种效果。

护盾和书本使用原始几何，但原 Shader、粒子、音效和 Animator 过渡混合仍未还原。自动生成目前针对无旋转的 LevelEnclosed，Unity 随机数序列和完整原生拾取回调时序尚未验证。证据见 `evidence/powerup-source-contract.json`、`evidence/godot-powerup-book-check.json`、`evidence/godot-book-browser-check.json`。最新验证页记录 17 个核心文件哈希和 15 组检查；旧报告保留其当时版本的哈希。

## 镜头移植进度

`original_camera_rig.gd` 已接入原版 16° FOV、相机方向、目标包围盒取景、自动缩放，以及玩家、飞镖和道具书加入／移除目标时的缓动。9 个恢复的 Camera3D 方法保持方法体不变，在原版 UnityPlayer / Mono 中运行；6 组取景场景共 3,600 帧，以及包含缓动重叠、冻结和暂停的 420 帧目标流程，与桌面和 WebAssembly 被比较状态逐值一致，未使用误差容限。浏览器已验证移动跟随、书本收取，以及飞镖反弹后接回，控制台无错误或警告。

镜头噪声、震屏、击杀后的特殊取景和 FOV 转场仍未移植。这里验证的是注入目标／显式协程调度条件下的取景状态，未比较相机矩阵，也不是完整原生对局逐帧回放。证据见 `evidence/camera-source-contract.json`、`evidence/godot-camera-web-check.json`。

## 地面检测

已恢复 LevelManager 的九点危险区域检测，并在原版 Physics2D 中执行盒形／圆形地面、墙边和水域样本。2,904 个采样位置的六项判定在桌面和浏览器中逐项一致。对照发现并修正了南侧采样点的坐标方向错误；道具书生成现在复用这套点检测。证据见 `evidence/ground-source-contract.json`、`evidence/godot-ground-web-check.json`。角色坠落及早期冲刺自救已接入；穿墙冲刺也已接入；重叠陷阱地面的选择顺序及全部场景几何仍需验证。

## 工具

- `tools/inventory.py`：只读 SHA-256 清单、版本和反编译诊断。不会将“存在文件”标记为“游戏已完成”。
- `tools/inspect_unity.py`：逐文件读取原版序列化设置和外部依赖，使用 UnityPy 1.25.4。
- `tools/recover_project.py`：有界 AssetRipper 恢复流程，缺少场景时失败，拒绝覆盖已有目录，最终回收自己启动的进程。
- `tools/export_godot_scene.py`：从已校验安装中转换层级、网格、材质和贴图，记录缺失引用与未恢复部分。
- `tools/prefab_to_glb.py`：转换角色几何、蒙皮绑定与贴图变换，保留原始节点可见性。
- `tools/decode_animation.py`：解码原始动画曲线并核对骨骼路径；流格式参考 [AssetStudio 原始实现](https://github.com/Perfare/AssetStudio/blob/master/AssetStudio/Classes/AnimationClip.cs)。
- `tools/actor-motion-oracle/`：提取原始移动与接触方法，生成 17 组数值轨迹；Godot 的 `tests/actor_motion_test.gd` 逐值比较，报告保留原生引擎尚未验证的边界。
- `tools/disc-motion-oracle/`：提取原始飞镖方法，生成 20 组飞行轨迹；`tests/disc_motion_test.gd` 逐值比较。追加 `clash` 参数生成 10 组碰撞响应轨迹，`tests/disc_clash_motion_test.gd` 逐值比较。库存与模型的集成检查另见 `tests/weapon_integration_test.gd`。
- `tools/compare_frames.py`：严格对比同编号 PNG 帧序列；空序列、缺帧、尺寸不同和任意通道不同均失败。此工具通过自身校验只证明比较器有效，不能证明游戏还原。

## 完成验收

| 要求 | 需要的证据 | 当前 |
|---|---|---|
| 原版自由击杀规则 | 原生与浏览器同输入的逐 tick 状态、死亡原因、得分和结算 | 未验证 |
| 原版地图与角色 | 完整场景、模型、骨骼、动画及材料导入，并在浏览器实际运行 | 原始资源已齐，导入／绑定及运行对照未完成 |
| 回旋镖、近战、冲刺与道具组合 | 按机制逐项和组合场景对照原生状态 | 未验证 |
| 逐帧画面 | 相同随机状态、时间步、分辨率与渲染设置的无损 PNG 对照 | 未验证 |
| 音频 | 音频银行、事件、混音和时序的实测对照 | 原始资源已齐，导入／绑定及运行对照未完成 |
| 浏览器直接运行 | WebGL/WASM 构建在浏览器内执行，完成整场自由击杀 | 飞镖与近战对战已运行；完整原版内容未补齐 |
| 自行 host | 静态产物、正确 MIME/压缩头和实际部署后游玩 | 静态包已生成并部署到 feixiong.me；完整原版内容仍未补齐 |

资源预算：本机最多一个恢复/编译重任务，工具限定 2 个逻辑 CPU；复用一个有明确到期时间的本地预览服务。Windows 验证使用独立副本 `D:\arkena\work\codex-boomerang-native-20261027`，不复制线上插件；单实例、2 个逻辑 CPU、2 GiB 上限、30 秒超时。最近一轮峰值约 197 MiB、约 1.83 秒，已正常退出且无该实例残留。验证副本保留供后续对照，无常驻进程。原安装和线上服务未改动；不进行重启、关机或系统更新。
## 坠落与自救

2026-10-28：已接入地面检测触发坠落、0.66 秒落水／0.74 秒悬崖计时、0.33 秒自救窗口、一次自救限制、落地冲刺恢复自救次数，以及死亡后的隐藏和碰撞关闭。原版 Actor 的 4 个完整方法与 2 段更新分支，在原版 UnityPlayer 中与真实 Physics2D 配合运行；16 个场景、2,880 帧与 Godot 桌面及 Web 的被比较字段逐值一致。牛油果和西瓜的原始 Fall 动画均绑定 116 条轨道。实际浏览器已验证坠落、死亡、不产生无来源击杀分和回合结束。

当前 LevelEnclosed 的原始 FloorCollider 为 100×100，比 43×43 的围墙竞技区更大，因此穿过围墙本身不表示坠落。临时坠落按钮把 P1 放到实际地面边界外；镜头不保证始终拍到这个远处测试角色。移动平台的完整附着、坠落粒子／声音、Animator 过渡和原生逐帧画面仍未完成。详见 `evidence/fall-source-contract.json`。

## 穿墙冲刺与危险区域保护

已接入穿墙能力、墙内延长冲刺、出墙恢复碰撞、墙内出招限制，以及冲刺／近战的地面安全判定。15 个原版 Actor／Player 方法保持方法体不变，在原版 UnityPlayer 中执行；13 个场景共 1,170 帧状态和 430 组空间判定，与桌面及浏览器被比较结果逐值一致。对照补齐了多边形边界射线规则，以及原生圆形重叠检测中的多边形 0.01 外扩和接触阈值；没有给比较结果加误差容限。

实际地图中，不带能力的冲刺停在墙内侧 x=19.6063，带能力的冲刺到达墙外 x=30.4126；均存活，结束后恢复碰撞。浏览器按键也验证了穿墙后继续移动，控制台无错误／警告。临时 Phase 按钮和测试起点入口可用于体验，书本道具池也已加入该能力。`evidence/dash-source-contract.json` 记录范围与缺口，穿墙前后截图为 `evidence/godot-phase-before.png` 和 `evidence/godot-phase-after.png`。这仍不等于完整原生逐帧游戏或画面已还原。

### 时间道具与共享时钟

2026-10-28：TimeImmune 已接入玩家时间补偿、飞镖一秒补偿衰减、冻结帧保护、重复拾取、死亡时对其他持有者的判断，以及结束后的复原。原版的 5 段 Player／Disc 代码和完整 TimeKeeper 在原 UnityPlayer 中运行，12 个场景、4,320 帧与 Godot 桌面及 Web 比较的字段全部一致。实际场景 16 项检查通过；加速飞镖的两次初始化投掷均会立即检查半径 0.5 内的触发器。

浏览器已实测时间道具期间移动、投掷、击杀、接回和到期；控制台无错误或警告，16 组 Web 检查全部通过。原版默认的 13 项随机道具概率列表没有 TimeImmune，因此当前通过临时 Warp 按钮体验，没有擅自加入书本道具池。时间粒子、音频、同时重叠触发器的原生顺序和完整逐帧画面仍未验证。范围见 `evidence/timeimmune-source-contract.json`；游戏截图为 `evidence/godot-timeimmune-active.png` 与 `evidence/godot-timeimmune-throw.png`。

### 分裂飞镖

2026-10-28：MultiDisc 已接入投掷分裂、扇形方向、碎片飞行、部分／完整回收、双飞镖组合，以及持有者死亡和跨局清理。普通分裂产生 5 枚碎片；原版 Fire／Explosive 组合产生 4 枚。54 个原版运行场景、243 个初始子飞镖、1,200 个飞行状态和 12 组回收序列，在 Godot 桌面及 Web 被比较字段上全部一致；实际场景 15 项检查通过，17 组 Web 检查通过。

实际浏览器已验证两次 5 枚分裂、护盾接触、部分回收、绕过障碍召回全部碎片、恢复可投掷飞镖、击杀计分和下一局清理，控制台没有错误或警告。临时 Multi 按钮和书本道具池均可获得这一能力。无窗口 dummy 渲染器仍有既有的清理时空材质诊断，真实 WebGL 未复现。

512 个角度诊断在 ±93° 还保留两个很小的三角函数差异；已有 16 个原生旋转样本已全部相同。当前幽灵飞镖使用临时半透明材料，原版边缘发光着色器尚未还原。火焰／爆炸的完整效果、音频、自动补镖与全局逐帧对照仍未完成。范围与证据见 `evidence/multidisc-source-contract.json`，实际截图见 `evidence/godot-multidisc-partial.png` 和 `evidence/godot-multidisc-complete.png`。

### 遥控飞镖

2026-10-28：Controlled Disc 已接入按住召回后的方向操控、松手和死亡分支、30 单位控制半径及 4 单位边界缓冲、控制资格恢复和操控时间统计。原版 23 个场景、4,140 帧的比较字段，在 Godot 桌面和 Web 全部一致；18 组 Web 检查通过。实际场景 14 项检查通过，覆盖反向输入不反转飞镖控制、分裂后的 5 枚碎片同时操控、跨局保留和新比赛清理，节点数回到 1,025。

浏览器使用 P2 的 Shift 投掷，再按住 Shift + 右方向键 600 毫秒，已确认进入遥控、松手退出并接回，控制台没有错误或警告。临时 Control 按钮和书本道具池均可获得该能力。原版操控粒子、边界粒子、音频和召回动画尚未还原；多碎片无窗口测试仍出现既有 dummy 材质诊断，真实 WebGL 未复现。范围见 `evidence/controlled-source-contract.json`，截图为 `evidence/godot-controlled-complete.png`。

## 传送飞镖（2026-10-28）

已接入原版传送选择、近距离视线判断、危险落点回退、落点击杀、分裂碎片回收、坠落救回和同帧位置切换。冲刺键触发传送；传送本身不额外授予通用无敌。保留了源码中随机上界排除最后一个飞镖、近距离提前返回等细节。普通道具池现有 7 种能力，另有 2 种调试能力；还缺爆炸、火焰、冰地、伪装、缩圈和分身的完整世界效果。

原版 UnityPlayer 中执行未改动的相关方法，35 个场景的被比较字段与 Godot 严格相等；完整 Godot 场景的 15 项检查通过，浏览器 19 组数值对照全部通过。真实按键已验证投掷后传送；临时测试按钮也验证了护盾和无敌状态下的传送击杀，计分 1:0，原版压扁身体与脸部资源可见。实际 WebGL 无错误或警告。见 `evidence/teleport-source-contract.json`、`godot-teleport-browser-check.json` 和 `godot-telefrag-browser.png`。

这些测试注入了拾取、死亡效果、重生调度和视听服务，不能等同于原生完整游戏回放。爆炸组合仍只记录补充飞镖请求；传送粒子、声音、Animator 过渡、原版着色器与逐帧画面对齐尚未完成。无图形场景测试中已有的 dummy renderer 空材质清理诊断仍在，实际浏览器没有复现。

## 地图材质与静态画面对照（2026-10-28）

从原版 D3D11 字节码恢复了地图使用的着色公式，接入顶点颜色、主光方向、自定义阴影色、Gamma 颜色计算、纹理、透明裁剪及纹理采样分支。原版 9 个着色器族的字节码已用 Windows 系统 D3DDisassemble 解码，其中 8 类已用于当前地图；16 种网格材质和 11 个贴地装饰已接入。33 个精灵的顶点、UV、三角索引及纹理名称已与原版运行时核对，22 个带风摆动的精灵仍待接入。

在相同机位、1280×720 的静态场景原始帧缓冲 PNG 对照中，90.2465% 的像素完全相同，95.3451% 的像素各通道误差不超过 1/255，平均通道误差为 0.7659/255。地面采样 RGB 均为 (212,252,134)。系统截图包含缩放、有损编码和显示色彩转换，因此没有用于这些数值。这个结果仅覆盖该静态视角；不代表整款游戏完成度或动态逐帧一致。阴影边缘、摆动植物、飘尘、角色与其他视效仍有差异。

当前可玩 Web 构建已重新导出，19 组 WASM 数值检查全部通过；15 项实际场景检查通过，节点清理返回 1,047。实际浏览器移动和投掷正常、无错误或警告。证据见 `evidence/world-shader-source-contract.json`、`scene-visual-frame-comparison.json`、`godot-visual-web-check.json`；无损画面为 `scene-visual-native-level.png` 与 `scene-visual-godot-frame.png`。

## 动态植物与环境光（2026-10-28 03:04）

已接入 LevelEnclosed 的全部 33 个原始 SpriteRenderer，其中 22 株植物使用恢复的 PK/SpritesDiffuseWave 摆动公式与透明混合。主光和环境光组合经过原版 GPU 的 24 个白色精灵测试，最大通道误差约 2.03e-7。一个固定视角的无损 1280×720 对比帧，平均通道误差为 0.636/255，95.72% 像素的各通道差不超过 1；这不是游戏完成率。植物的完整动画序列、原版粒子、阴影和材质过滤差异仍未全部验证。

当前导出已通过 19 组浏览器数值检查及 15 项传送场景检查，场景重置后节点数保持 1069。实际浏览器移动与投掷无控制台错误。最新静态托管包为 `web-main.zip`，证据见 `evidence/godot-wind-web-check.json`、`evidence/godot-wind-browser-check.json`、`evidence/scene-wind-frame-comparison.json`。爆炸等剩余道具、完整菜单和视听表现仍在继续移植。
