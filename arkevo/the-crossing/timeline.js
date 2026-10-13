export const DURATION=84;
export const phases=[
 {start:0,end:18,label:'尝试',code:'ATTEMPT',title:'先试着穿过去。',description:'路口缺少掩体。直接穿越会暴露在交叉视线中，重复操作没有解决问题。',evidence:'两次穿越失败；记录了暴露位置与行动路径。',tool:'基础控制',version:'BASE',toolDesc:'观察画面 → 移动 → 收集反馈',modules:['observe','act','memory'],flow:'try',caption:'直接穿越，再次失败。问题不在走得够不够快。'},
 {start:18,end:28,label:'设计',code:'DESIGN',title:'把问题交给新工具。',description:'接入 Adaptation。用失败路径与场景几何构造一个可测试的假设：能否让烟幕挡住视线？',evidence:'暴露路段与观察点已对齐；开始构造投掷候选。',tool:'轨迹规划器',version:'V1',toolDesc:'搜索投掷角度，预测落点',modules:['observe','memory','sandbox'],flow:'design',caption:'如果道路上没有掩体，就用烟幕造出掩体。'},
 {start:28,end:40,label:'测试 V1',code:'TEST / V1',title:'第一版，先算落点。',description:'工具把目标位置转成一条抛物线。执行投掷，再检查烟幕是否落在预期位置。',evidence:'烟雾落在目标附近；落点验证通过，穿越尚未验证。',tool:'轨迹规划器',version:'V1',toolDesc:'投掷搜索 → 落点验证',modules:['sandbox','act','verify'],flow:'test',caption:'轨迹算对了，任务就完成了吗？'},
 {start:40,end:50,label:'发现缺口',code:'OBSERVE / FAILURE',title:'落点正确，路仍不通。',description:'烟幕边缘仍然漏出一条视线。只优化落点，无法保证穿越时全程有遮蔽。',evidence:'失败发生在烟幕边缘；验证目标需要从“落点”升级为“路径”。',tool:'轨迹规划器',version:'V1',toolDesc:'暴露缺口 → 保存失败证据',modules:['observe','verify','memory'],flow:'learn',caption:'烟雾到了，危险视线却还在。'},
 {start:50,end:64,label:'修订 V2',code:'ADAPT / V2',title:'修改“怎样才算成功”。',description:'第二版同时检查投掷轨迹、烟幕范围与整条穿越路线，搜索能连续遮蔽路径的组合。',evidence:'新增路径遮蔽检查；从单一落点改为两枚烟雾的空间组合。',tool:'烟幕编译器',version:'V2',toolDesc:'候选搜索 → 视线相交检测 → 路径验证',modules:['memory','sandbox','verify'],flow:'design',caption:'从一个落点，重新设计一整条安全通路。'},
 {start:64,end:84,label:'再尝试',code:'EXECUTE / VERIFY',title:'带着新工具，再来一次。',description:'执行新方案，沿烟幕保护的路径穿过路口。保留工具版本与验证记录，为下一次任务提供起点。',evidence:'演绎关卡完成；路线、投掷方案与工具版本一并留存。',tool:'烟幕编译器',version:'V2',toolDesc:'执行方案 → 检查结果 → 保存可复用工具',modules:['act','verify','memory'],flow:'try',caption:'环境没有变。Agent 与环境交互的方式变了。'}
];
export const phaseAt=t=>{const time=Math.max(0,Math.min(DURATION-.001,Number(t)||0));return phases.findIndex(p=>time>=p.start&&time<p.end)};
export const modules={
 observe:{code:'01 / PERCEPTION',title:'让世界成为可用证据。',desc:'从画面、声音与操作反馈中提取与任务相关的信息，保留不确定性，并把观察结果送入模型上下文。',input:'当前画面、声音、历史观测',output:'对象、位置、事件与置信信息',callout:'本片中的作用：把敌方观察点与暴露路段对应起来。'},
 act:{code:'02 / EXECUTION',title:'让方案真正发生。',desc:'把模型选择的动作变成键鼠、游戏接口或工具调用，并记录执行是否成功、用了什么输入。',input:'动作计划、可用工具及其参数',output:'操作结果、时间戳、执行反馈',callout:'本片中的作用：投掷烟雾、移动到出口，并把操作结果交回观察模块。'},
 memory:{code:'03 / MEMORY',title:'下一次，不必重新摸索。',desc:'保存失败片段、有效策略与工具版本；在相似问题出现时，为模型找回有用的上下文。',input:'观测片段、尝试记录、测试结果',output:'相关经验、可复用工具与版本',callout:'本片中的作用：保留“落点正确却仍然暴露”的失败，避免重复只优化落点。'},
 sandbox:{code:'04 / SANDBOX',title:'给新工具一个试验台。',desc:'在隔离的工作区编写并运行工具。每次修改保留版本，试验失败时可以回到此前可用状态。',input:'工具规格、代码、测试输入',output:'可调用插件、运行日志、版本差异',callout:'本片中的作用：把单点轨迹预测器改为包含烟幕空间与路径检查的工具。'},
 verify:{code:'05 / VERIFICATION',title:'把“看起来对”变成检验。',desc:'将环境结果与任务要求对照。检查目标本身是否合理，把失败证据送回下一轮设计。',input:'任务目标、运行结果、测试条件',output:'通过或失败、误差位置、反例',callout:'本片中的作用：发现烟幕边缘的视线缺口，让评分目标从落点升级为路线。'},
 adaptation:{code:'ADAPTATION',title:'把失败变成下一版工具。',desc:'把本轮的失败证据交回模型，允许它修改工具，再用相同的任务验证修改有没有帮助。',input:'失败画面、工具版本、验证结果',output:'新工具、测试计划、可回滚版本',callout:'本片中的修订：从“烟落在哪里”变为“路径是否真的被遮住”。'}
};
export const worlds={
 cs2:{name:'烟幕编译器',tag:'TACTICAL REASONING / 实验方向',problem:'同一条路，每次都暴露。',text:'让 Agent 自写投掷搜索与空间验证工具：先找落点，再发现烟雾缺口，最后把目标通路转成可执行的烟幕方案。主视角、落点相机与俯视图共同解释因果。',flow:['直接穿越失败','轨迹搜索','发现遮蔽缺口','重写验证目标'],source:'https://www.counter-strike.net/newsentry/532126482488623353',sourceLabel:'CS2 官方：离线练习与地图指南'},
 minecraft:{name:'幽灵试跳器',tag:'PHYSICS & PLANNING / 实验方向',problem:'每一跳，先让分身替你试。',text:'从失败录像估计移动与起跳参数，在落脚之前画出多条幽灵路径。第一版能过固定跳台；遇到移动平台后，再加入时序预测与输入延迟补偿。',flow:['落点失败热图','生成幽灵试跳','移动平台反例','加入时间预测'],source:'https://www.minecraft.net/en-us/article/minecraft-commands',sourceLabel:'Minecraft 官方：命令与环境控制'},
 noita:{name:'反噬预报器',tag:'COMPOSITION & CAUSALITY / 实验方向',problem:'法术很强，先别把自己炸了。',text:'把未知法杖组合送进一个小型试射场，显示爆炸、火焰与液体的连锁范围。漏判一次导电反应，工具就需要新增环境因素并重新检验。',flow:['组合产生反噬','搭建试射场','可视化连锁范围','新增环境变量'],source:'https://noitagame.com/',sourceLabel:'Noita 官方：逐像素物理世界'},
 celeste:{name:'死亡倒带编辑器',tag:'CONTROL & MEMORY / 实验方向',problem:'在失败的那几帧，改写动作。',text:'把多次失败路径叠成幽灵轨迹，搜索冲刺前后的关键输入时机。观众拖动同一条时间轴，对照工具修订前后的动作差异。',flow:['记录死亡前输入','叠加失败轨迹','搜索关键时机','生成动作控制器'],source:'https://www.celestegame.com/',sourceLabel:'Celeste 官方：游戏与动作挑战'}
};

// Observable events become evidence only after the corresponding action occurs.
const events=[
 {at:0,description:'从起点观察交叉视线。目标是在不暴露的情况下，到达拱门后的出口。',title:'目标：穿过路口，到达出口。',evidence:'已设定起点与出口。第一轮尚未开始。',caption:'任务：穿过三条危险视线，抵达拱门后的出口。'},
 {at:.4,description:'开始直接穿越，记录沿途的视线与动作反馈。这一轮的结果仍待观察。',title:'先试着穿过去。',evidence:'第一次穿越进行中；记录移动路径与暴露位置。',caption:'先直接尝试。观察危险视线从哪里出现。'},
 {at:6.7,description:'首次穿越出现暴露。保存失败位置，准备调整操作进行下一次尝试。',title:'第一次尝试失败。',evidence:'首次失败位置已保存；尚未生成新工具。',caption:'第一次失败。把暴露位置与行动路径留下来。'},
 {at:8.7,description:'带着上一轮记录再试一次，继续检查调整后的操作是否能避开危险视线。',title:'换一次操作，再试。',evidence:'保留上一轮记录，开始第二次直接穿越。',caption:'再试一次。操作改变了，危险视线还在。'},
 {at:15.3,title:'重复操作，仍然失败。',evidence:'两次直接穿越失败；共同的暴露路段已记录。',caption:'两次失败指向同一个问题：这条路缺少遮蔽。'},
 {at:18,evidence:'失败路径与观察点送入工具工作台；开始定义投掷搜索。'},
 {at:24,evidence:'工具规格已形成：目标落点 → 投掷候选 → 落点检查。'},
 {at:28,caption:'先把目标位置，变成一条可执行的投掷轨迹。',evidence:'V1 投掷执行中；还没有落点或穿越结果。'},
 {at:34,evidence:'第一枚烟雾落地；正在检查落点与成烟范围。'},
 {at:38,caption:'落点通过。接着检查整条穿越路线。',evidence:'落点检查通过；整条穿越路线仍待测试。'},
 {at:40,description:'正在沿 V1 方案继续移动，实时检查是否出现新的暴露。出口尚未抵达，整条路线仍待验证。',toolDesc:'执行穿越 → 连续记录视线 → 等待路线验证',title:'开始验证整条穿越路线。',code:'TEST / ROUTE',flow:'test',modules:['act','verify','observe'],caption:'落点正确，还要验证走到出口的每一步。',evidence:'开始 V1 穿越测试，持续记录未被截断的视线。'},
 {at:47.25,title:'烟幕边缘，出现缺口。',caption:'观察点 C 的视线，从第一枚烟的边缘穿过。',evidence:'发现烟幕外的残留视线；当前路线仍有暴露。'},
 {at:48,caption:'落点正确，路线仍然失败。把这个反例交回工具工作台。',evidence:'V1 穿越失败。落点正确不能证明整条路线安全。'},
 {at:50,evidence:'把失败视线加入约束；优化目标改为整条路线。'},
 {at:53,evidence:'搜索两枚烟雾的组合，并计算视线与烟幕的相交位置。'},
 {at:59,evidence:'第二枚烟幕正在形成；复查两侧的残留视线。'},
 {at:63,evidence:'V2 路线采样检查通过；真实穿越仍待执行。'},
 {at:64,description:'开始按已检验的方案穿越。视线状态随移动持续更新，出口判定尚未通过。',title:'带着新工具，再来一次。',evidence:'正在沿规划路线移动；持续验证视线，尚未抵达出口。',caption:'带着新工具穿越。每条视线都要经过检验。'},
 {at:78,description:'角色已抵达拱门后的出口区。保存通过检验的路线、投掷方案与工具版本。',flow:'learn',modules:['verify','memory'],title:'抵达出口，验证完成。',evidence:'角色进入出口区域；演绎任务完成，保存工具与验证记录。',caption:'抵达出口。将通过检验的方法，留给下一次任务。'}
];
export function snapshotAt(t){const index=phaseAt(t),phase=phases[index];const event=events.filter(e=>e.at<=t&&e.at>=phase.start).at(-1)||{};return{...phase,...event,index};}
