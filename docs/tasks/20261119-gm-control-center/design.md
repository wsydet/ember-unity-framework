> 当前状态更新（2026-11-19，R14/D10/A1）：用户已在主聊天明确批准“GM布局就按现在这版”及Loading接线、设置/公共弹窗通透参数、玻璃初始化与GM解耦，开始Unity实施。见[approval.md](approval.md)及[current-approval-r14.md](current-approval-r14.md)，需求为AC01—AC24。以下旧“未确认/不可Unity”段落是历史状态，已被本更新覆盖。完整模式与原调度授权沿用；I1派发信息以dispatch.json为准。
# GM 控制中心方案 D10

任务 `20261119-gm-control-center`；有效需求R13，轮次D10；2026-11-19，Asia/Shanghai。状态：**入口越过拖动阈值即收起HUD、后续长按再开已更新网页；其他UI玻璃覆盖只读审查已交付**。D9自适应布局只在重开后使用，D6主面板/最多五/原入口拖动FPS和共享动作保持。整体方案及Unity实施仍未确认。

## 方向与本轮交付

将 GM 从状态大卡片、组件演示与快捷按钮混排，收敛为一个可移动的小面板。顶部固定少量开发操作，下面只放消费项目注册的扩展。面板之外不加全屏暗幕，保留游戏可见参照。

实际入口为 [本地网页](http://127.0.0.1:8765/prototype/)，文件 [prototype/index.html](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/prototype/index.html)，运行与操作见 [prototype/README.md](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/prototype/README.md)。统一四列：标题跨两列，拖动/关闭圆钮各一列，FSM跨左2×2格，第三列电源/恢复图标圆钮，最右时间条跨两行；帧率整宽与扩展外缘对齐。GM只快切模式与显示独立FPS/屏幕Hz/生效目标，目标类型和值只在完整设置编辑；无二级浮层或设置跳转入口。

背景是可见的彩色游戏场景示意：河流、树木、屋顶、桥与角色。它由网页绘制，明确标注不是 Unity 运行画面；玻璃确实通过浏览器 backdrop-filter 模糊下层场景，文字/按钮为独立清晰前景。背景可动画或手动平移，方便对照玻璃内外轮廓。没有把不透明贴片或模糊过的背景图片冒充实时模糊。

## 实际基线与所有者

本轮只读核对：分支 `codex/release-0.26.0`，HEAD `65ae680732323d83de9f898f89e9e8cc576ee4c8`；Unity 6000.5.4f1，embedded `Packages/com.ember` 0.26.0（manifest `file:com.ember`、lock embedded）。`Assets/Editor/EmberEditingTemplate.json` 正式编辑 base 0.11.1，hash `9077bd5b5271898e8055d1c7cf72b0a7`。base 专属 glossary 不存在。唯一既有 Git 改动为 `Assets/GameResource/Resources/UI/Common/Fonts/DefaultLight/DefaultLightSans.asset`，保留。

| 所有者 | 实际对象 / 后续归属 |
| --- | --- |
| base 共用业务 | `Assets/Game/UI/Runtime/Module/UIFoundation/` 下 GM Page/Center/Item、GMActionRegistry、EUIAppearanceBootstrap.GM；GM 的布局、过滤和消费扩展接线继续留在 Assets。不是新 Manager。 |
| base 制作维护源 | `Assets/Game/UI/Editor/UIFoundation/SoftGlassUILibraryBuilder.cs`、`SoftGlassUIRevisionTwo.cs`、`SoftGlassUIAuthoringWindow.cs`；已有正式 Prefab 位于 `Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/`。 |
| 框架基础能力 | `Packages/com.ember/UI/Runtime/Backdrop/`、`UIExtension/Runtime/Behaviour/EUIGlassImage.cs` / `EUIGlassStyle.cs`、`UIRendering/Runtime/URP/EUIBackdropRendererFeature.cs`。优先复用，不为 GM 改全局默认。 |
| 框架统计组件 | `Packages/com.ember/UI/Runtime/Components/FPSDisplay.cs`；若定向验证确认统计缺陷，在框架修正，与限帧政策解耦。 |
| 消费项目 | 具体补给、无敌、检查点、解锁关卡、存档等 Provider 与业务实现。本网页只是演示内容，不并入 base 玩法。 |

此任务未从 UnityFarm 提出或验证，不推定任何消费项目已接入。初步变更归属为 **base 模板整理 + 条件性的框架统计/玻璃修复**。模板的改动先落 Assets，框架修复必须有本轮证据；不改 PackageCache。`Templates~`、`ParentSnapshot~` 本轮均未编辑，后续也必须通过项目中心维护。旧文档中的 topdown 身份、版本或历史通过结论不替代本轮基线。

## 布局与交互提案

| 项目 | 紧凑候选 | 舒展比较 | 取舍 |
| --- | --- | --- | --- |
| 横屏 1280×720 | 画面宽 36%、高 86%，约 461×619，占面积 31.0% | 宽 44%、高 92%，约占 40.5% | 默认右上，24 参照单位边距，游戏约 69% 面积仍在面板外。 |
| 竖屏 720×1280 | 宽 88%、高 62%，约 634×794，占面积 54.6% | 宽 92%、高 76%，约占 69.9% | 默认右下、上方保留游戏；提高文字与命中尺寸，不把横屏控件整体缩到难以使用。 |
| 窄横屏 960×720 | 受最小可读宽度约束，实际宽至少 44%，高 86%，占面积约 37.8% | 同宽度安全下限 | 网页显示实际值，不承诺所有比例可机械应用于极小画面。 |

以上是讨论用虚拟画面比例。Unity 实施时按安全区及短边720DU布局换算，正文、按钮命中、刘海和极端宽高比须实际验证；网页 px 与 DU 不直接换算。过窄/过矮时先保证基础区与至少一行扩展可访问，而不是压扁控件。宽高滑杆有可读下限。

固定区沿用R8，标题按R9在左两列居中：四列标题/拖动/收起；左侧只读FSM方框，中间纯图标电源/恢复圆钮，最右0.1—3×单根竖条；其下帧率只快切三模式。扩展自身滚动、原按钮主体样式保持，增加轻量收藏星角标；若真实Unity/Ember时间不同，实施仍须可诊断而不是用假相同值掩盖。

### D10当前：入口开始拖动即关闭HUD

用户在主聊天明确选择“拖动开始就收起，之后长按再展开”，来源[steering-r13.md](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/steering-r13.md)。pointerdown不关闭；首次移动越过原8×预览缩放阈值、进入dragged时调用同一setHUD(false)，清理HUD手势/待放大timer。继续移动、释放及吸边都保持收起，不自动重开、不短按打开主面板；下一次长按才按新位置重新求解D9布局。阈值以内微动保留原短按/长按语义。

这条C20覆盖D9/P18“拖动途中临时无解暂隐并保留展开意图”的旧交互。D9尺寸档位/滞后/登记避让在重开后仍有效，入口大小/保存位置/FPS来源和收藏不改变。正式Unity仍需在FloatingEntry首次进入拖动的可复用接点接入，不复制整份入口逻辑；本轮只网页，未改C#或Prefab。

### D10其他UI玻璃覆盖只读审查

完整结果与逐类文件证据见[glass-ui-audit.md](glass-ui-audit.md)。当前base的主菜单、设置、SoftGlass公共弹窗与控件已有采样接线，不建议全UI改成GM风格。具体优先补核项是Loading进度轨已为玻璃面而宿主链未见Group/显式lease；其余小范围候选为设置旧schema黑0.5遮罩与公共Popup染色声明、GM0.64重Backdrop、安装与ShowGM耦合、控件制作重复Surface幂等性。

Legacy、普通图片/功能前景、显式示例背景和有意保留的World/视觉小说皮肤不自动视为缺陷。对六模板只抽样实际保存配置，未加载运行。审查不等于批准G01—G06实施，也不等于本轮真实采样验收；没有运行探针/Unity编译/Play/测试。

### D9保留：边角紧凑候选与自适应外侧圆钮

本节布局与大小规则保持；其中拖动中保留HUD意图的旧段落由上方D10明确覆盖，现为拖动首次生效即收起、后续长按重开。

来源为[steering-r12.md](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/steering-r12.md)及 `references/corner-fit-r12.png`，已查看并复现近似左下三项。用户允许放不下时缩小外侧圆钮，入口大小保持；常规83%保持。下面的尺寸档位、命中下限与排序是P17/P18设计提案，不是用户指定数值。

1. 优先83%：尝试原轻弧侧列，整体上下纠正不超过当前命中直径45%；超过就不再把整列推远，改尝试围绕入口向内/向上或向下的局部弧线，先调方向/弧度/间距。
2. 局部弧线以入口中心为锚，优先90°，其后100°、75°、60°候选；两项使用55°/70°，单项不强造曲线。根据入口上下位置优先向可用半边，左右均朝内；不是绕入口完整一圈。相邻中心距由命中圆直径和间距推导，不允许互相覆盖。间距优先12/8，局部紧凑候选8/6虚拟单位，圆间最终至少留5。
3. 83%候选不满足安全/登记区域或紧凑质量约束时，再统一尝试76%、69%、62%。同一HUD全部圆钮和图标同比缩小，不分项缩成大小不一，不改变GM入口的尺寸、落点或收藏数。
4. 62%是本轮可审阅视觉下限；命中圆直径取 `max(可见直径, 入口直径×64%, 48虚拟单位)`。横屏76入口时最小可见47.12、命中48.64；竖屏100入口时62/64。圆钮以透明外侧命中层包住可见玻璃圆片，二者同心；拟合与碰撞检查都用完整命中圆，命中层带圆角，不以重叠矩形覆盖邻钮。网页等比预览不能代替真实Unity/设备命中尺寸验收，正式DU下限仍需按项目标准核定。
5. 只有各档都无合法解才明确收起并提示主面板；不静默丢项。拖动/吸边中临时无解时暂隐HUD但保留展开状态，下一可用位置立即恢复，吸边终点重算后才决定明确回退，避免途中一次失败永久关掉HUD。

候选除安全区/入口/登记矩形分离外，还约束自然距离：侧列只允许小幅整体修正；局部弧线最远命中边缘距入口中心的上限为入口直径×(0.6×项数+0.2)，一两项用1.7倍。相同尺寸先按候选形态顺序，局部解再优先较近、方向较自然的方案，不做遍历整屏的大平移。该距离预算也是P17提案，用于拒绝“数学不出界但远离入口”的解。

缩小立即生效保证安全；放大需更大档在边界/登记区域再留4虚拟单位余量，且连续约180ms可用，才恢复。近边界来回移动先维持当前小档，避免逐帧换档；返回宽裕区自动恢复83%，没有尺寸持久化。HUD关闭会清理待放大timer。入口D8对已登记文字的同侧避让继续保留，不因本轮HUD需要扩大其行为。

网页画面外新增“额外UI占用模拟（审阅）”：默认关闭，打开后围绕入口当前高度登记上下占用矩形，显示半透明边界，提供适中/窄/极窄通道，便于实际审阅自适应；不移入口、不写配置。空间测试不是任意场景识别，真实避让范围仍只含安全区、入口和登记矩形。

复现对比：左下三项的入口坐标/尺寸保持相同，最远命中边缘距离由D8约201.89降为D9约121.81虚拟单位，减少约40%。常规左右中部仍为83%；登记窄通道分别实测76%、69%、62%，回到宽裕区恢复83%。

### D9正式Unity增量提案（尚未执行）

在D8位置规划提案上增加同一候选求解和可见/命中占用模型，参数归GM专用配置，不改全局FloatingEntry默认值。入口的拖动/吸边/位置/FPS来源和Activated兼容保持；HUD只能读取入口落点，不能为拟合反向搬入口。尺寸选择由位置/安全区/登记矩形/收藏列表失效驱动，放大滞后使用未缩放时间并在隐藏/销毁清理。当前动态圆片与同心命中层仅网页实现，正式Prefab/Binding须经EUI中心制作。

真实Unity应对角落3/5项、窄安全区、登记UI变更、点击命中边缘、触摸/长按/入口拖动、缩小/恢复滞后及最终无解回退做定向验证。它不是本轮运行结果；D7统一动作/开关/危险确认和D8玻璃生命周期要求继续有效。

### D8历史：最多五项、轻弧、较小圆钮与自动避让（候选与固定大小由D9覆盖）

有效来源为[steering-r11.md](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/steering-r11.md)，合并用户R10/R11。最多五个、轻微弧度、圆钮比GM入口小、保留入口拖动/FPS及自动避让是已明确要求；83%大小、弧度参数、沿边避让策略和无空间回退属于P15/P16可审阅选择。

- 收藏沿用D7同一稳定Key列表和执行分发。最多5个，潜在未注册Key也占名额；第6次收藏只提示上限，不替换、不执行动作。取消后可补一个。载入旧数据先格式校验/去重，再按原顺序保留前5个，专用键回写归一化列表并显示说明；写入受限则保留本页前5个并说明。默认仍不预收藏。
- 视觉直径取入口83%：横屏入口76→HUD63.08，竖屏100→83，均为网页虚拟画面单位。五钮按收藏顺序自上而下；纵向间隔12，中间比首末向屏幕内侧多偏移直径38%，形成轻弧而非整圈。少于三项时无需强造弧线。正式Unity的DU、命中区域、真机可读尺寸另行验证；如扩大命中区域，布局需以其占用矩形一并避让，不能只验可见圆圈。
- HUD左右方向按入口当前所在半屏朝内。先围绕入口高度生成五点，整体上下夹限，再搜索相邻高度/更内侧候选；只接受按钮位于安全边界内、彼此分离、与入口和登记矩形不交叠的解，按位移代价选最接近的解。拖动每次移动、吸边每帧、横竖切换/尺寸变化均重排；不逐帧重建动作或注册项。
- 本网页安全区为整个虚拟画面内缩12，登记矩形为左上场景标题、横屏左下说明文字，以及显式带 `data-hud-avoid` 的区域；测量转为画面本地坐标后外扩10。只对安全区、入口、登记矩形负责，不识别树、房子、角色、未登记UI等任意画面物体。正常画面最多五项完整展开，不再使用D7无限收藏滚动列。
- 入口继续可自由拖动；释放吸边及恢复位置时，若入口会压住登记文字，则沿同一侧选最近可用高度。保存实际落点的归一化高度，左右侧不因避让改变。此为原吸边逻辑上的避让补充提案，非已存在Unity能力；拖动过程中不强行推开用户指针。
- 极小/被遮满画面找不到完整安全解时收起HUD并提示“空间不足，请短按入口在主面板操作收藏”；保留收藏，不缩小到不可点、不堆叠、不越界，也不未经提示改成整圈。若入口同侧亦无可用高度，则保持原安全区夹限入口，使主面板仍可达；不保证在完全覆盖安全区的任意禁入集合中找到无交叠解。空态提示也经过同样的边界/登记矩形校验。

### D8入口行为、位置与FPS来源

只读核查的 `EUIFloatingEntry.cs` 原有8×Canvas.scaleFactor屏幕阈值、半尺寸+12安全边距、左右半屏吸边、底部起算归一化Y、0.18秒三次缓出及Right/Y位置记录，未改源码。网页用8×预览缩放因子CSS像素作为入口拖动阈值，移动距离换算回本地画面；这不同于D7扩展收藏手势的8 CSS像素阈值，不能混为同一数值。

入口是单指/主鼠标的等待→短按/长按/拖动状态机：550ms之前越过拖动阈值就取消长按，拖动释放只吸边不打开面板；长按成功后消费短按，并锁定本次手势不再转成拖动，下一次按下仍可正常拖。pointercancel、失焦、lost-capture清理timer，指针捕获保证离开圆钮后的释放可达；键盘激活可短开主面板。主面板自己的⠿黄态拖动逻辑保持。

网页位置独立键为 `ember.prototype.20261119-gm-control-center.entry.position.v1`，schema `{version:1,Right:boolean,Y:number}`；Y从底部起算、限制0—1，默认右侧Y=.72与源码一致。保存实际吸边落点，尺寸变化按归一化高度恢复再避让；坏数据回默认，存储受限仅本页记忆。桌面/手机共用当前来源的该演示位置键，不写Unity PlayerPrefs，不声称已完成真实项目/用户隔离。

入口显示“—”和小号“FPS”，aria-label与画面外说明明确尚未采样；独立于屏幕Hz、目标FPS，不从网页动画或目标制造数字。正式Unity继续保留 `FPSDisplay` 与现有入口接线，初始化统计窗口问题仍按AC06待实际验证，不能用网页占位当统计修复。

### D8正式Unity接入补充（尚未执行）

保留 `EUIFloatingEntry` 原拖动/吸边/恢复职责及公开Activated兼容；需要先核对可复用的长按/拖动抑制接点，在不重复Activated的前提下接HUD。无合适接点时设计有界的通用手势扩展，不能在GM用户逻辑中复制整份FloatingEntry或在手势释放后两条路径都激活。

HUD位置规划优先留base UIFoundation；接入Canvas安全区与画面本地坐标、入口/命中矩形及业务显式登记的UI禁入矩形，布局失效由位置/安全区/登记版本驱动。业务自行声明需要保护的UI，不把场景图像识别并入框架。入口同侧最近可用高度的补充应可配置、默认不影响其他使用FloatingEntry的页面；不修改全局入口默认行为来满足单GM。位置原Right/Y应继续使用或有明确迁移，新增收藏存储继续按项目/用户隔离提案。

玻璃生命周期、稳定Key重新解析、同源开关及统一危险确认沿用D7正式提案；HUD开着即有消费者，五个圆钮共享组而非各自RT。实际Unity需补鼠标/触摸/多指拒绝、拖动与长按冲突、位置恢复、登记矩形变化、安全区/旋转、极小回退、FPS来源与危险确认输入域的定向验证，本轮均未运行。

### D7历史原型：收藏、入口长按与共享动作HUD（数量/形态由D8覆盖）

来源为[steering-r9.md](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/steering-r9.md)及 `references/favorite-hud-r9.jpg`。用户明确要求标题居中、长按或右键收藏自定义按钮、收起时长按GM入口显示圆形快捷HUD；以下时长、阈值、关闭方式与存储细节属于P13/P14可审阅提案。

- 扩展右键或长按切换收藏，星角标表达状态，不改两列按钮主体。初始没有预收藏；普通短按仍执行原动作。550ms判定前移动超过8 CSS像素、滚动、移出或取消即撤销；长按后消费释放点击。触摸contextmenu与长按去重，不两次切换；网页保留原生滚动，不在按下时抢占触摸。
- 主面板关闭时，入口短按开主面板，长按只展开/收起HUD，释放不额外开面板。HUD按收藏先后在入口内侧单列展开，圆钮透明玻璃，按可用空间夹限并内部滚动；没有收藏/当前无可用项时显示简洁提示。点外部、ESC、再次长按收起；开主面板先关HUD；执行后保持HUD。title与aria-label提供动作名称。
- 一个 `registeredActions` Map、一个开关状态源、一个 `executeAction(key)` 同时服务主面板和HUD。按钮回调只保存稳定Key；执行前重新解析注册项、检查可用性，再进入同一普通动作/开关/危险分支。HUD不复制业务逻辑，也不捕获旧描述对象。3/24/0选择会注销/重建演示注册项并释放旧手势监听，收藏顺序保留；恢复注册后重新出现。当前演示可用性以是否注册为准，未新增独立禁用状态控制器。
- 网页专用localStorage Key为 `ember.prototype.20261119-gm-control-center.favorites.v1`，值为 `{version:1,keys:[稳定演示Key...]}`，只保存收藏Key/顺序。读入校验schema、字符串格式、去重及最多64项；无效JSON回空，存储不可用则仅本页记忆。未知或未注册Key不提供可执行按钮，也不执行任意字符串；合法但暂未注册的Key可保留，恢复后再显示。首次载入不自动写存储、不自动收藏。此处是原型浏览器记忆，不是Unity配置。

手势状态为等待→短按执行/达到时长后长按消费/移动滚动取消。取消路径不会执行动作或收藏；重建视图时清理timer与监听。550ms和8 CSS像素仅是网页候选，物理触摸、操作系统原生菜单与真实入口拖动的协调尚待后续验证。

### D7正式Unity实施提案（尚未执行）

收藏状态与接线归base `UIFoundation`，不新建Manager、不把消费动作移入框架。优先核对项目现有设置/存储服务，再在同一业务范围实现小型收藏存储辅助；按稳定项目身份、用户或本地档案、schema版本隔离。只存Key及顺序，不存回调/业务对象或业务开关状态；匿名用户的档案选择、迁移和清理政策需实施前确定，本轮没有写PlayerPrefs。

注册版本变化时重建主列表/HUD；未注册或不可用项不提供失效执行入口，潜在收藏记录保留；同Key重注册后重新解析当前Provider。两个入口必须调用同一UI执行分发：检查Key/IsAvailable，危险动作使用既有 `EUICommonDialogs.ShowConfirm`，确认回调再解析/校验；不能只在主面板确认而让HUD绕过。当前网页危险示例只显示“需消费端确认，未执行存档修改”，不代表正式确认流程已完成。

通过EUI开发中心制作/绑定正式HUD Item及Page接线，复用同一页面玻璃组并在动态列表变化后RefreshSurfaces；不为每个圆钮创建RT。面板关闭但HUD可见时仍有真实玻璃消费者，应继续捕获；只有主面板与HUD均关闭后才验证GM零消费者/清退。HUD隐藏、销毁、注册重建与恢复须配对释放/恢复自身表面，不影响其他Popup。

入口复用现有EUIFloatingEntry吸边/归一化位置与拖动机制，D8补充详见上节。D7当时入口固定，D8网页已模拟拖动/吸边，真实Unity入口仍未运行验证。ESC应先交上层模态Popup；HUD关闭优先于GM收起，不越过危险确认输入域。

后续最小相关Unity回归包括实际鼠标/触摸手势、滚动取消、入口拖动冲突、注销/重注册/可用性变化、收藏隔离/坏数据、危险确认焦点与回调重校验、两入口开关一致，以及HUD玻璃生命周期。此处列计划，不声称已运行。

### D6保留布局：四列对齐、按住黄态、完整设置才能调整目标

两次连续用户原话与约束见[steering-r8.md](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/steering-r8.md)，参考 `references/alignment-r7.png`。本轮一次合并R7/R8，未另开聊天或增加业务功能。

- 标题简化为“控制中心”，跨左两列；右上拖动/关闭各占一列，均为完整单元圆钮。下方FSM方卡跨2列2行，电源/恢复箭头各占第三列一行，时间条占第四列两行。四圆钮同直径、同列中心线，方卡/条上下边缘对齐；整宽帧率和扩展两列继续共用24边距、12间隔。外部面板尺寸候选不变。
- 时间钮移除外部“未开启/恢复”和内部1×文字，保留电源、恢复箭头、aria-label及开关高亮；1.00×仅在条内。功能沿用：未启用拖条无响应、恢复1×始终有效，P10关闭保值仍未独立确认。
- 只有拖动圆钮处理pointerdown/move，按住立即黄色 `#ffd63f`，移动继续夹限；pointerup/cancel/lost-capture/window-blur清理黄态与按压状态。标题文本、关闭钮、时间与模式控件不启动拖面板；没有扩大成按钮排序或位置持久化。
- GM帧率区只保留“— FPS”“屏幕144 Hz”“目标147 FPS”及三模式快捷按钮。删除GM自动/自定义按钮和数值框，不新增设置入口/二级浮层。完整设置仍持有原目标类型/数值，GM模式切换只改mode，不暗改auto/custom或customText；同步档的生效目标由屏幕分频，切回高响应恢复保留选择。
- 用户所感“固定144”已实际检查：该字段为默认模拟屏幕Hz，不是目标。完整设置把144改120后自动目标为124；高响应选择自定义后180、200均可改并同步GM；同步模式禁用自定义属既有规则。没有发现目标输入被锁成144的运行行为；本轮把字段标签写清，不把网页默认值当实际显示器测量。
- 整页继续明确模拟、未连接Unity；FPS保持独立空读数，不从请求目标伪造。未新增状态切换、日志诊断、目标覆盖或持久化。该四列具体视觉仍待主聊天审阅。

### D5历史形态：方卡＋简洁时间条，GM目标编辑已被R8收敛

R6原话及两张用户标注截图见[steering-r6.md](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/steering-r6.md)，附件为 `references/time-strip-r6.png`、`references/state-frame-r6.png`，结合参考图音乐方卡与右侧长条布局完成本轮原型。

- FSM卡采用1:1方框，标签只保留“顶层游戏状态”和当前Name，长名完整换行；当前Name仍来自已核查的GameLauncher.Fsm.Current.Name契约，网页没有连接Unity。画面外统一说明只读模拟，并可选择长名压力样例；不新增状态切换能力。
- 原两圆钮移到时间条左侧，最右仅一根输入轨道/填充。删去条内外标题、模拟字样、套壳和条外倍率；倍率作为清晰前景放条内下方，pointer-events:none，避免遮挡拖动。保留输入aria-label。用户已明确未启用时拖动无响应；P10关闭保留倍率尚未独立确认，1×随时可用且不改变开关的提案保持。
- 帧率长条作为上下组之间的自然版块：第一行独立FPS占位/屏幕Hz与节能/平衡/高响应分段按钮，第二行自动/自定义切换、按需数值框和当前目标/同步数值。常用值以两行完成，容器允许合理换行，不摆大型表单。同步模式的目标类型按钮和自定义输入禁用，显示实际同步目标而非保留的自定义值。
- 两入口继续共用frameState和一个计算验证函数。高响应默认自动、144→147、自定义180严格180、刷新率变化自动重算/自定义不改。FPS永远保留“— FPS”，不从目标推导。Hz只读、由外部模拟控件提供；反复的“模拟/只读/编辑”说明放画面外统一呈现。
- 已删除二级编辑区DOM及开关/inert/焦点管理，直接控件不经过任何弹层；ESC按既有GM收起。收起重开不暗改策略或时间。移除扩展前分割线、项目扩展标题及纯演示数量，扩展按钮网格/外观/3或24或空态和独立滚动继续保持。
- 外部面板尺寸候选沿用，GM标题区略收紧以保护扩展空间。动作反馈移到画面外当前单条反馈，不增加日志列表或诊断。未加入状态业务操作、全局重置、持久化或其他功能。

网页仍是缩放的虚拟屏幕比较，不是原生设备触摸验收；浏览器玻璃像素与UnityDU/GPU成本不等价。当前顶部及两行内联控件的具体排版仍待主聊天确认。

### D4历史：R4参考图与R5即时补充的顶部整合（形态由R6覆盖）

R4用户认可下方扩展并要求帧率/策略/顶层FSM集中顶部，新图为 `references/top-control-center-r4.png`。R5原话：“时间控制感觉可以做到音量条和亮度条的位置，包括一个竖着的时间条和两个圆形按钮，一个是开启时间控制，一个是恢复到1x”；即时来源 [steering-r5.md](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/steering-r5.md) 已合并到requirements，不另开轮次。

采用左侧两张信息卡上下排列、右侧控制区的紧凑组合，借鉴参考图宽圆角卡与音量/亮度条的位置语言，不复制其音乐、通信或亮度功能。横屏两卡各120参照单位高，右侧竖条与卡组同高252；竖屏两卡各156高，竖条与卡组同高324。面板外部尺寸、位置候选和已认可扩展样式保持；竖屏针对动作反馈占用做了16单位收紧，确保3个扩展仍完整可见。

帧率卡集中独立“— FPS · 模拟”、当前模式/自动或自定义目标、只读模拟Hz及编辑入口。当前请求180不表示实测180。状态卡没有按钮或切换状态操作，长名采用较小字号/自动换行，不截断为不可读省略号；外部审阅控件可以选择已核查Name或显式“非实际注册状态”的长名压力样例。底部不重复放帧率或FPS。

**顶层FSM源码核查**：`Assets/Game/UI/Runtime/Module/UIFoundation/EUISoftGlassGMStateItem.cs` 已读 `GameLauncher.Instance.Fsm.Current.Name`；`Packages/com.ember/Core/Runtime/GameLauncher.cs` 创建自己的EmberStateMachine并注册CreateInit/Main/Gameplay/SettingsState；框架对应Name为 `Init`、`Main`、`Gameplay`、`Settings`，当前base的 `Assets/Game/State/Game*State.cs` 继承这些Name。未就绪沿用“—”。这不是玩家动作/子FSM，网页没有读取运行中的Unity。证据见 `evidence/d4/top-fsm-source.json`。

**R5时间语义提案P10**：开关只控制倍率条能否编辑，关闭保持当前倍率，独立1×恢复随时有效且不改变开关状态；网页初始未启用、倍率1×。关闭不等于暂停，绝不作为TimeScale=0按钮。这个关闭语义尚未得到用户独立确认，网页画面下方和说明已标明提案。倍率条支持指针及键盘方向，关闭后原生disabled且输入处理守卫保留值；没有额外重置按钮、第三个时间动作或持久化。

R2/R3共享frameState和计算规则不变。D4当时曾迁顶部卡打开浮层；D5/R6已删除浮层、改为长条直接编辑，旧底部/顶部弹层入口与相关ESC二级流程均不再作为当前实施形态。

拖动语义分别为：

1. **推荐实施**：扩展区纵向 ScrollRect 浏览，空状态可读；整个背景/标题不被滚动带走。
2. **R8已明确、待Unity实施**：只按住右上拖动圆钮移动整面板，标题文本不拖；限定安全区并在旋转后夹限，其他按钮/滑杆/滚动不启动拖动。按住黄态，释放/取消/失焦恢复。优先会话保留位置，不新加持久化。
3. **未确认可选，不纳入本轮默认范围**：按钮拖动排序。沿用注册描述的 Group / Order / Key 顺序，不新增排序持久化和编辑模式。

收起与展开保持既有时间倍率，ESC只在GM实际收到对应输入时收起，不能越过上层模态Popup的输入作用域。入口网页行为以D8为准，真实Unity入口拖动/吸边/恢复仍须实施验证。

## 基础功能保留、移出与删除清单

| 现有内容 | 提案 | 兼容与实施说明 |
| --- | --- | --- |
| 真实顶层 FSM 状态、Unity/Ember 时间值 | R4顶部只读状态卡；时间值在倍率条下 | 不硬编码Gameplay；真实来源为GameLauncher.Fsm.Current.Name，非玩家FSM；两时间不同仍须可诊断。 |
| 时间调节、1×复位 | R5竖向倍率条＋独立圆形1× | 后续真实应用继续同步Unity Time与EmberTimeManager；收起不复位，恢复1×不暗改启用状态。 |
| 时间 Enable 开关 | R5保留为圆形启用/关闭按钮 | 关闭只禁用调节并保留倍率为当前提案P10，待独立确认；不是暂停、不是归零。 |
| 暂停/恢复快捷卡 | 被R5取代，从当前方案移除 | 不做第三个时间动作或重复横向滑杆，原D1暂停提案不再作为有效实施内容。 |
| 浮动 FPS 入口 | 保留并定向核查统计 | 网页使用 GM 圆钮，避免把模拟 FPS 看成实测；最终入口可继续显示真实 FPS。 |
| Exit game | 移到次级“更多/危险操作”，确认后调用既有 Quit | 不在小面板主视区占格；网页说明了候选，尚未制作次级菜单。确认式退出不应使用绕过框架清退的 Application.Quit。 |
| CoreActions 的 UI library / Enhanced | 移出正式 GM 的默认注册和 UI | UI library 仍由独立展示入口打开，Enhanced 是测试演示，不是业务能力。不要全局删除玻璃库。 |
| GMTest 四类 Image 样本、Enhanced Toggle | 从正式 GM 树/绑定移出 | 需要的样本留独立展示页/夹具。原 Legacy 资源先保留，不批量删除带GUID资产。 |
| sample.background / sample.verbose | 删除 ShowGM 自动 RegisterSamples 接线 | 示例 Provider 保留为显式示例入口；业务项目自己的 Provider 不受影响。 |
| 多张状态大卡、重复诊断标题、全屏深色 Backdrop | 删除正式 GM 中相应绘制/冗余层级 | 页面背景不盖住全部游戏。玻璃表面与输入拦截只覆盖面板；视外输入按既有 FreePage 契约明确验证。 |
| 旧 GMPage、Legacy 选择、GamePages / 原 GUID / 用户钩子 | 保留兼容 | 当前方案默认整理 SoftGlass 正式入口，Legacy 作为回退，不手改 EmberManaged 生成区。若要求 Legacy 同步清理需主聊天明确扩大范围。 |

“删除”均指用户确认后的实施建议，本轮零产品删除。已知 Core Key `ember.gm.reset-time` 可在中心打开期间继续保留语义；`ember.gm.components` / `ember.gm.showcase` 退出默认 GM 列表时，实施前检查代码与配置调用方，必要时保留展示入口或短兼容转发。不要把移出 UI 误当作删除公开注册接口。消费端自有稳定 Key 一律保留。

## 消费端扩展接入契约

复用现有 `IGMActionProvider.Actions` 和 `GMActionRegistry.Register(provider) -> IDisposable`。不把注册接口搬进 UPM，不创建新 GMManager，不要求业务修改 GMPage 的受管区。

- 每项稳定唯一 Key（建议 `项目.模块.动作`，避开 `ember.gm.*` / `sample.*`）；Group、Order、Title、Icon；动作使用 Execute，开关成对提供 ReadToggle / WriteToggle。重复 Key 当前会抛错，不以重复注册实现更新。
- Provider/模块持有句柄，生效期间注册，模块退出/销毁先 Dispose；句柄幂等。页面收起不得 Dispose 消费模块自己的句柄。
- 按钮只存 Key，点击前重新 TryFind / 检查 IsAvailable，防止闭包捕获失效业务对象。动态注销、同 Key 重新注册后，旧视图刷新/重建与最新解析保持一致。
- 沿用 Snapshot 的 Group 字符序、Order、Key 排序；Group 作为可选小节文字，不增加一套拖动排序。界面固定基础区与消费扩展列表分离，避免 CoreActions 全部重复到扩展区。
- 注册项列表在持有句柄期间稳定；新增/删除先注销再注册，以 Version 使面板刷新；单纯状态变化由现有低频刷新读取，不每帧 Snapshot 分配。
- **现存缺口**：`Dangerous` 现在只改变颜色，`TryExecute` 不确认；实施需在 UI 动作执行前走既有 `EUICommonDialogs.ShowConfirm` 并在确认回调重新解析 Key / IsAvailable。业务副作用仍应由消费端自行兜底，不把红色当安全确认。
- 显示/启用范围沿用项目现有 GM 发布策略，本轮不额外新增发行开关体系。24按钮是网页滚动用例，不在正式 base 自动注入测试 Provider。

## 玻璃配置、复用与性能

已存在 `EUIBackdropSettings`（半径DU、降采样、迭代、更新频率、回退色）、`EUIBackdropGraphic`（color alpha、radius、edgeLight、gloss、frost）、`EUIGlassImage` 与 `EUIGlassStyle`。URP后端在下层段透明绘制后采集，再供下一段前景采样。优先检查真实就绪 lease、捕获相机/排序和表面 alpha，不把“不透明”直接认定是某一个参数。

静态事实：当前 settings 16DU、downsample4、iterations2、Throttled30；Neutral tint alpha0.18；但 GMPage 的 `SoftGlassUIRevisionTwo.Page()` 又在全屏 CenterHost 制作了 alpha0.64 的 Backdrop。该组合可能影响观感，仍需实际检查 Group/边界/叠层，不能仅凭这段源码认定根因。

建议配置入口：GM Page 的 EUIBackdropGroup 引用一份 **GM 专用 settings**；框架现有默认保持其他页面行为。半径/降采样/更新频率在这份SO编辑，表面不透明度/圆角/边缘高光由GM表面和专用样式配置。若中心现有 slot 可表达，则直接复用；需要新配置只在 base UIFoundation 内，不做第二套框架参数。网页右侧控件是审阅工具，不是正式GM新增一排美术设置。

网页候选18px与20%不透明度仅表达视觉方向，**不代表Unity18DU或任何GPU预算**。Unity 首轮可从现有16DU/4/2/30Hz作为对照基线，逐项调整，不在没有画面比较前预定最终数值。回退色沿用可读策略，后端不支持时诚实显示 Solid，不标记为模糊通过。

性能：一组共享一个下层边界，按钮不逐个申请RT；动态创建后 RefreshSurfaces；扩展滚动不改变捕获边界数；关闭/隐藏按生命周期释放GM自己的消费者，不影响其他Popup；从无GM→展开→滚动→收起比较RT/边界/相机段。缩小可见面板并不必然按面积同比降低当前全屏捕获成本。默认先不引入虚拟列表；24个按钮足够检验现有Factory/句柄与刷新逻辑，需要更大规模再基于实测决定。

## FPS 解释、事实与短排查

FPS 是游戏每秒完成帧数，Hz 是显示刷新次数，帧时间是每帧耗时。约16.67ms/帧对应60FPS，1.67ms/帧对应600FPS；吞吐量高不保证每帧间隔稳定，也不表示60Hz屏幕每秒展示600个完整新帧。

桌面 VSync>0 时由刷新率分频控制，targetFrameRate不主导；移动端忽略vSyncCount，使用targetFrameRate和平台可用档位；-1在桌面/移动语义不同。Editor 的 targetFrameRate 仅影响Game视图，不能从Editor吞吐量推断Player功耗。网页模拟明确展示这些差异，不测量浏览器或Unity性能。[Unity targetFrameRate](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/application/targetframerate)、[Unity vSyncCount](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/qualitysettings/vsynccount)。

当前PC/Mobile画质配置都为vSyncCount=0。FPSDisplay在Awake将60格都填当前时间，Update在窗口未满时仍以60除已过时长，**静态推断**可能启动读数放大；有效窗口满后可能是真实不限帧，也可能受其他运行因素影响。本轮未读运行时targetFrameRate、未测实际刷新率、未启动Play，不认定根因。

主聊天确认后建议仅作以下短定向排查，不先写全局60帧：

1. 经Unity MCP编译/域重载完成后，记录平台、Editor/Player、聚焦/后台、质量档、实际vSyncCount/targetFrameRate/refreshRateRatio；排查自有启动及相关插件是否运行时改值。
2. 记录组件首次启用后前120帧：realtime/unscaled时间、有效样本数、现有显示值、独立按实际样本数与累计未缩放时长算的FPS。检查首帧、未满窗口、满窗口和暂停后行为，注意初始化时刻与计数对应。
3. 在相同场景稳定采3秒，与Game视图Stats/Profiler帧时间对照；GM开/关各短测一轮，分清显示器刷新、CPU吞吐与玻璃成本。仅启动期虚高才修统计，持续1.6ms左右且不限帧则保留真实吞吐结论。
4. 条件允许时对桌面Development Player做同范围对照；未构建则明确Editor范围。移动政策在实际目标设备另验刷新率/分频/热与功耗，不用桌面模拟替代。

R2已确认下列桌面策略，取代D1“桌面政策完全待定”的表述。短定向排查仍用于判断统计是否失真、真实配置是否按策略应用，不能把网页模拟视为600FPS根因证据。移动映射仍待定，当前Unity全局政策尚未修改。

### D2：已确认桌面策略的网页交互

用户在主聊天原话：“可以，帧率就按这个方案，默认高响应、自动跟随屏幕，自定义目标不加余量。先更新到网页原型里，我们再继续确认 GM 布局，暂时不要改 Unity。”

| 模式 | 桌面控制 | 网页输入语义 |
| --- | --- | --- |
| 节能 | vSyncCount=2，以半刷新率同步 | 高响应目标选择禁用；若之前选自定义，保留数值但禁用并明确不生效。 |
| 平衡 | vSyncCount=1，跟随刷新率同步 | 同上；targetFrameRate不参与控制，不能把保留值显示为当前目标。 |
| 高响应 · 自动（默认） | vSyncCount=0，按自动余量表请求targetFrameRate | 模拟刷新率改变即重算，默认144Hz→147FPS。表为原型参数，非性能保证。 |
| 高响应 · 自定义 | vSyncCount=0，targetFrameRate严格为填写正整数 | 180就是180，不加余量；刷新率变化、切同步再切回均保留180。小数/0/负值拒绝，显示待修正而不悄悄取整。 |

自动候选表：60→66、90→95、120→124、144→147、165→168、180→182、240→242、360→361。**未列表边界为D2暂定设计选择，尚未获用户确认**：使用更高相邻表项的余量；低于60Hz加6，高于360Hz加1；自动结果向上取整成整数。例如75→80、59.94→66。没有杜撰已确认数学公式，也不把余量表当防抖保证。

没有增加运行时覆盖演示。玩家选择在网页内直接作为“模拟当前控制”，GM默认展示“模拟：高响应·自动147 / 144Hz”等精简行，按R3允许点开编辑（详见下节）；独立“模拟FPS —”保持无采样占位，不从目标反推出读数。屏幕刷新率、策略和生效控制均明确标为模拟，没有读取Unity、本机屏幕或浏览器FPS。移动端只保留平台差异与待定说明，不套用桌面VSync档位或刷新率加余量。

D2保持D1画面比例、玻璃、固定功能和扩展交互。底部原提示行替换为摘要；按钮点击反馈按需显示。D3沿用尺寸，提供按需浮层，避免将多组设置常驻展开挤占扩展区；画面外仍保留玩家设置预览供比较与模拟刷新率控制。

后续Unity获准实施时，玩家设置与选择保存在base业务接线，优先核对已有设置服务；通用帧率策略机制按可复用程度与实际接口确定归属，不为本次模拟新建Manager或承诺已有持久化API。自动模式的运行时刷新率变化、同步模式忽略自定义、配置应用与实测FPS独立属于AC06相关验证；桌面确认不等于移动策略或GM布局确认。

### D3历史位置与共享状态契约（浮层交互已由R6取代）

来源为主聊天用户R3原话：“我建议在gm中也可以修改这个”。这确认GM需要编辑能力，替代D2仅显示摘要的限制；**具体浮层形态仍是本轮待审阅提案**。

- D3曾点击底部摘要；D4/R4已迁到顶部帧率卡，打开局部玻璃编辑浮层，包含模式、自动/自定义、自定义正整数目标；初始关闭，不额外占用扩展区布局。浮层高度受GM范围约束，必要时自身纵向滚动，不溢出画面。
- GM中的屏幕刷新率是文字只读参照；只有网页外部审阅控件能调整模拟Hz。FPS始终是独立模拟占位，不从目标计算实测值。没有新增持久化、重置、运行时覆盖或移动策略。
- 两入口共用一个 `frameState`（模式、目标类型、保留的自定义原始输入、模拟Hz）和一个渲染/验证函数，输入事件更新同一状态并同步两处控件、禁用/错误提示、摘要及生效控制；没有两套余量计算或相互触发的循环事件。
- 规则沿用R2：默认高响应+自动；自定义180严格180，不加余量；同步模式禁用自定义但保留原值；任一入口中的无效正整数输入在两处同时提示，不暗中取整；切自动后按最新模拟Hz重新计算。
- ×关闭浮层；ESC优先关浮层并将焦点还给当前顶部帧率卡，再按既有GM行为收起。关闭/重开编辑区、GM收起/展开不改变策略。键盘可从模式到目标类型再到自定义输入，未加入额外快捷键。
- 浮层打开时，被覆盖的GM标题拖动、基础控件及扩展区暂时inert；编辑控件不启动面板拖动或触发底层扩展动作。浮层标题不选中文本，关闭后恢复原有交互与扩展滚动。网页外部设置仍可交互，便于双向比较，浮层采用非全页模态语义。

后续Unity帧率实现沿用D6范围，通过EUI中心制作GM三模式快切、只读摘要与完整设置控件，复用同一策略应用接口；D7另加本节收藏/HUD。旧D3浮层与D5 GM目标输入不作为当前形态。本轮仍只列方案，不制作Unity资源。

## 确认后的实施文件与中心操作

| 工作包 | 计划涉及文件 / 操作 |
| --- | --- |
| GM布局 | 保留GMPage/Center/Entry/Action等原Prefab GUID；通过EUI开发中心调整Center为固定基础区+仅扩展ScrollRect，收缩背景到面板，重新绑定/生成对应Binding。 |
| 用户逻辑 | `EUISoftGlassGMCenterItem.cs`、`EUISoftGlassGMStateItem.cs`、`EUISoftGlassGMTimeItem.cs`、`EUISoftGlassGMActionItem.cs`、`EUISoftGlassGMPage.cs`；按最终确认实现图标启用/恢复、顶层状态、GM模式快切/目标只读、专用拖动钮黄态、危险确认与清退，不做GM目标输入/浮层/旧暂停卡。 |
| 接线清理 | `EUIAppearanceBootstrap.GM.cs` 移除默认RegisterSamples；保留显式示例入口；Registry接口优先不变。删除GMTest引用要与中心生成同时完成。 |
| 可重复制作用途 | 更新/新增GM专用的中心维护操作，基于既有authoring source，但不能再次批量Apply旧RevisionTwo覆盖24个不相关资产。窗口当前仅有首次制作/22Binding重生成，并无本轮整理按钮；需要有界、可预检的GM范围中心操作，不能谎称现成一键可用。 |
| 框架条件修复 | FPSDisplay确认统计问题再改；玻璃基础后端只有定向证据证明缺陷才改。若拖面板需要通用组件先核对现有拖拽机制；GM专有位置逻辑优先留base，不改通用EUIConstrainedPanel影响别的控件。 |
| 正式文档 | 更新 `Assets/Game/Documentation/base/soft-glass-ui.md` 的本轮接线/GM说明；公共统计/玻璃API确有变动才更对应包文档。旧文档历史版本文字不作为当前发布状态。 |
| 模板交付 | 完成Assets与验证后按授权由项目中心普通保存base；保存、Bump、子模板父同步、包发布、消费升级分开记录与授权。未执行的环节保持待完成；不手改快照/hash。 |

实施阶段进入时先读编码/API、Unity验证规范和适用EUI制作技能；本轮没有执行这些产品操作。回退按实际实施前备份恢复本任务Assets/用户逻辑与生成结果，不碰既有字体修改；Legacy路径和原注册接口保持可回退。若本次变动涉及生成模板源，核对工具维护源，不能手改生成受管块。

## 必要回归与验收安排

| AC | 确认后最小相关验证 | 当前状态 |
| --- | --- | --- |
| AC01 | 彩色动态场景与低层UI在面板内真实模糊；半径/表面alpha独立；前景清晰；Solid回退可读且不冒充Blur；关闭清退 | 网页方向已自检，Unity待实施验证 |
| AC02 | 横屏1280×720、窄横屏、竖屏720×1280；安全区、极端宽高、旋转后夹限；面板外游戏继续可见 | 网页尺寸/320和390页面宽度自检，Unity/设备待验 |
| AC03 | 24扩展滚动时顶部固定；时间同步/差异可诊断；关闭与重开不暗改倍率，时间动作以R5/AC10为准 | D4相关网页空间/滚动检查通过，不是Unity行为验收 |
| AC04 | Provider注册/重复Key/注销/同Key重注册；IsAvailable；禁用/危险动作确认后再解析；只拖圆钮，标题/滑杆/滚动不抢手势 | D6相关网页交互通过，真实Provider与输入待实施 |
| AC05 | 正式GM无组件样例、自动示例Provider、全屏暗板；展示库独立可达；Legacy/原GUID/消费Key引用保持 | 仅静态清单，待主聊天确认及实施 |
| AC06 | R2三模式/默认高响应自动、余量表与自定义原值、动态刷新率、同步忽略自定义、真实配置与独立FPS；首120帧+稳定3秒统计对照，区分Editor/Player | D2定向网页自检通过，Unity配置/统计排查未开始 |
| AC07 | 主聊天明确确认design D1/后续修订，记录确认原话/范围/时间后才派实施 | **待用户确认** |
| AC08 | GM快切mode与完整设置同步，目标类型/数值仅完整设置；GM不暗改保留值，无二级浮层 | D6相关网页自检通过，Unity待整体方案确认后实施 |
| AC09 | 顶部集中独立FPS/策略/目标/Hz与只读顶层FSM；真实来源说明、长名可读；无底部重复帧率；认可扩展区保持 | D4网页检查通过，顶部形态待用户审阅 |
| AC10 | 右侧一竖条/两圆钮，启用禁用、指针/方向键、关闭保值、两种启用状态下恢复1×一致，无旧暂停卡 | D4/R5网页检查通过；P10关闭语义仍待用户确认，Unity待实施 |
| AC11 | FSM1:1方卡、最右单根轨道且倍率条内；帧率两行轻量直接编辑，无旧弹层/扩展标题/数量/分割线；下方样式/滚动及横竖窄屏保持 | D5/R6定向网页检查通过，具体形态待用户审阅 |
| AC12 | 四列边缘/中心对齐；时间纯图标；GM只三模式/只读屏幕Hz目标FPS；完整设置目标可改且同步，保留类型和值 | D6/R8定向网页检查通过，实际Unity待实施 |
| AC13 | 标题两列，右上同尺寸拖动/关闭圆钮；只拖动钮移动，按住黄色，松开/cancel/blur清理，关闭不拖 | D6/R8定向网页检查通过，具体视觉待用户审阅 |
| AC14 | 标题在两格内居中；扩展长按/右键收藏取消，无误执行；短按正常、移动/滚动/取消不收藏 | D7/R9网页自检通过；真实触摸和Unity待实施 |
| AC15 | 主面板关闭时入口长按显示收藏圆形HUD；同Key共享执行/状态/可用性/危险确认，空态/滚动/边界/关闭/本地记录健壮 | D7/R9网页自检通过；Unity存储隔离、注册生命周期与确认流程待实施 |
| AC16 | 最多五收藏，第6次拒绝不替换不执行；旧列表按顺序前5；小于入口的轻弧圆钮互不重叠 | D8/R11定向网页自检通过，83%/弧度仍待视觉审阅 |
| AC17 | 原入口单指拖动/阈值/左右吸边/归一化位置恢复与FPS来源保留，短按/长按/拖动互斥；主面板黄态不回退 | D8网页相关自检通过；FPS占位不采样，真实Unity/触摸待验 |
| AC18 | HUD随入口/吸边/尺寸变化向内重排，避边/入口/登记矩形，无法容纳明确回退 | D8网页左右/四角/手机/320宽及遮满回退自检通过；真实安全区/登记接线待验 |
| AC19 | 左下三项与四角3/5项先紧凑布局再自适应统一缩小，命中不重叠；入口不腾位；宽裕恢复且不抖档；最小仍无解才回退 | D9定向网页自检通过，实际76/69/62档及恢复已验证；数值/排序待审阅，Unity待实施 |
| AC20 | pointerdown/阈值内微动不关；越阈值首次拖动即收起HUD，松开/吸边不重开，后续长按在新位按D9展开 | D10网页12项定向检查通过，Unity未实施 |
| AC21 | 只读盘点正式base入口/配置/Prefabs/Group/普通Image/采样安装，区分已接入/小范围建议/保持/待验证 | D10审查完成，见glass-ui-audit.md；真实采样未运行，未授权产品同步 |

实施代码/资源变动后必须走Unity MCP请求编译、确认本轮编译/域重载、读取Console，再做相关回归；不默认全量测试，不用历史报告当本轮通过。触摸、安全区与移动平台策略需要实际设备才签收，消费项目尚未指定，若主聊天选择消费端验收需补项目/基线/范围。旧双Popup隐藏/返回链不合并成独立修复目标，但危险确认使用到的模态输入链必须做针对性回归；发现同源失败明确记录并交主聊天，不默默宣布已解决历史任务。

## 待主聊天确认的范围

1. **布局/玻璃方向**：以紧凑横屏约31%、竖屏约55%为起点，右上/右下锚点、通透暗色玻璃；或在网页选定其他比例/视觉值。Unity精确DU与成本仍须实施实测。
2. **D9形态与时间语义**：五收藏、轻弧、受限允许缩小HUD、保留入口拖动FPS均已明确；P17具体档位/命中下限/候选排序与距离预算、P18放大滞后/临时无解和最终回退、P14正式存储仍待审阅，P10关闭保留倍率仍待确认。D6主面板和D7共享动作保持，不重复询问已明确能力。
3. **帧率剩余边界**：R2桌面三模式、默认高响应自动与自定义无余量已经确认，不重复投票；本轮未列刷新率的D2暂定边界可在主聊天补定，移动端映射仍待讨论。真实运行统计短排查仍留到整体方案获准实施后。

## D10/R13本轮网页与静态审查

仅运行 `tooling/check-prototype-d10.cjs`，**12项通过、0JS错误**：[网页结果](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d10/web-checks.json)。只覆盖pointerdown/微动保留、首次越阈值收起、继续拖动不延迟长按、松开吸边保持关、收藏/保存位置保留、后续长按新位置D9重开与短按/FPS占位。未重复D9全套。截图[拖动后保持收起](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d10/after-drag-collapsed.png)、[新位置长按重开](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d10/long-press-reopened.png)已查看。

静态审查的Prefab/资产、38个源码配置文件证据及模板五文件抽样分别保存在evidence/d10/glass-assets.json、glass-source-evidence.json、template-samples.json；结论在glass-ui-audit.md。不把旧历史图、本网页CSS或静态数量当Unity真实lease就绪/模糊验证通过。

## D9/R12历史网页定向自检

仅运行 `tooling/check-prototype-d9.cjs`，**41项通过、0JS错误**，结果[evidence/d9/web-checks.json](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d9/web-checks.json)。范围包括用户左下三项D8/D9对比、四角3/5项、左右正常83%、登记文字避让、手机/320宽、实际76/69/62档及命中圆、安全/尺寸滞后/恢复、画面外受限模拟控件和受影响的HUD点击/入口短长按。真实Chrome鼠标及布局测量，不是物理触摸或Unity验收；未重跑原收藏上限、帧率/时间全套。

保留[D8近似复现](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d9/before-d8-left-bottom-three.png)和[旧几何](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d9/before-d8-geometry.json)，当前对比为[左下三项](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d9/after-left-bottom-three.png)、[常规83%](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d9/normal-right-three.png)、[左上五项](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d9/left-top-five.png)、[右下五项](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d9/right-bottom-five.png)、[手机右上五项](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d9/phone-top-right-five.png)、[手机左下三项](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d9/phone-left-bottom-three.png)、[实际最小档](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d9/restricted-minimum-three.png)、[审阅控件最小档](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d9/review-control-minimum.png)、[恢复常规](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d9/restored-normal-three.png)，D8对比与D9九张最终图已全部查看。首轮发现拖动中临时无解提前关闭HUD，已改保留展开意图、吸边终点复判并定向复测；未更改入口落点来让HUD通过。

## D8/R11历史网页定向自检

仅运行 `tooling/check-prototype-d8.cjs`，**38项通过、0JS错误**，结果[evidence/d8/web-checks.json](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d8/web-checks.json)。定向覆盖五项上限/第6次右键和长按拒绝/取消后补项/旧数据前5归一化、83%大小与轻弧、左右内向/四角/手机/320宽边界与登记文字避让、入口拖动吸边/位置恢复/阈值取消/长按锁定/取消失焦/FPS占位、主面板黄态及无空间回退。仅网页真实鼠标和合成取消/失焦事件，不是物理触摸或Unity验收；未重跑D7动作共享及旧帧率/时间全套。

最终图为[右侧轻弧](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d8/right-arc.png)、[左侧轻弧](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d8/left-arc.png)、[左上避标题](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d8/top-left-avoid.png)、[左下避说明](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d8/bottom-left-avoid.png)、[右上角](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d8/top-right-avoid.png)、[右下角](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d8/bottom-right-avoid.png)、[手机右下](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d8/phone-bottom-right.png)、[手机左上](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d8/phone-top-left.png)，已全部查看。初轮截图发现入口自身遮标题，已补同侧最近高度避让并增加入口与登记文字分离检查；这是D8提案补正，不声称原Unity已具备。

## D7/R9历史网页定向自检

仅运行 `tooling/check-prototype-d7.cjs`，32项通过、0JS错误，结果[evidence/d7/web-checks.json](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d7/web-checks.json)。覆盖标题居中/拖钮黄态、右键和长按收藏取消不执行、随后短按执行、移动/取消/滚动撤销、触摸式长按与contextmenu去重、入口短长按区分、HUD与主面板共用开关/动作/危险提示、各关闭方式、默认空态/多项滚动/320宽夹限、刷新记忆、注销/恢复、未知Key与坏JSON。实际鼠标操作和合成触摸式事件分别记录，未进行物理触摸设备验收；未重跑旧帧率/时间全套。

最终四图[主面板收藏](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d7/main-favorites.png)、[空收藏HUD](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d7/hud-empty.png)、[收起时快捷HUD](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d7/hud-favorites.png)、[竖屏多项滚动](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d7/hud-many-portrait.png)已全部查看。初次截图发现GM入口继承全局hover浅底导致文字对比不足，已改为暗色玻璃悬停态并重新截图/检查。D1—D6不持久化的历史叙述仍指当轮状态；当前仅收藏演示增加专用浏览器记忆，帧率、时间、位置、业务开关没有新增持久化。

## D6/R8历史网页定向自检

只运行 `tooling/check-prototype-d6.cjs`，28项通过、0JS错误。覆盖四列对齐/圆直径/标题跨度/默认Gameplay单行、纯图标时间动作、GM无目标控件、完整设置模拟Hz与180/200实际可改、GM模式双向同步/保留类型和值、专用拖钮黄色/移动/释放/cancel/blur路径、标题不拖/关闭不拖、受影响扩展滚动和横竖窄屏。未重跑旧全套。

最终截图 [默认网格](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d6/desktop-grid.png)、[按住拖动黄态](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d6/desktop-drag-yellow.png)、[竖屏长名](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d6/portrait390-long.png)、[窄屏滚动](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d6/compact-many.png)，已查看；结果[evidence/d6/web-checks.json](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d6/web-checks.json)。首次截图发现默认Gameplay被拆成最后一字母，已调整正常字号并补单行检查；拖动色变改为即时以准确表达按住状态。测试中修正grid跨度读取端与页面滚动定位，不作为产品缺陷。

## D5/R6历史网页定向自检

D5当轮运行 `tooling/check-prototype-d5.cjs`：28项通过、0JS错误。覆盖无二级浮层/旧标题/分割线、直接控件双向同步/同步禁用/自定义180/刷新率变化/无效输入/键盘、独立FPS、单根时间条无标题/倍率位置/禁用不响应/开启指针/关闭保值/恢复1×，以及FSM方框长名、原按钮外观/3、24、空列表/滚动及横竖窄屏空间。未重跑D1—D4全套玻璃、余量表或业务Provider检查。

初次新布局核查发现百分比高度的竖条输入参与grid内在高度计算，把固定区撑高；已将输入绝对定位在轨道区域内，让方卡与圆钮决定顶行高度，定向复测通过。原预览服务退出后已同端口重启，地址保持不变，现终端会话6912；不是持久托管。

证据[evidence/d5/web-checks.json](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d5/web-checks.json)，最终截图[横屏自动](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d5/desktop-auto.png)、[横屏自定义180](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d5/desktop-custom180.png)、[竖屏长名](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d5/portrait390-long.png)、[窄横屏多按钮](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d5/compact-many.png)。已查看四张最终截图。无Unity产品/编译/Play/Unity测试操作。

## D4/R5历史网页定向自检

D4当轮运行 `tooling/check-prototype-d4.cjs`：29项通过、0JS运行错误。范围为顶部整合、源说明/真实Name模拟/长名、编辑入口迁移、竖向条与两个圆钮的禁用/开启/指针/ArrowUp/保值/1×、以及受影响3按钮容纳/24滚动固定/空状态/原按钮样式。覆盖横屏、窄横屏、390与320网页宽度，未重跑旧全套玻璃、帧率余量表、拖动或Provider检查。

初次竖屏检查发现动作反馈可使第三个扩展超出12参照单位，已将竖屏两卡各收紧8、竖条同步收紧16，并保持字体换行可读；定向复测29项通过。测试脚本中的一处括号和多元素locator调用已修正，不作为产品故障记录。

证据 [evidence/d4/web-checks.json](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d4/web-checks.json)；最终截图 [默认横屏](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d4/desktop-top.png)、[顶部帧率编辑](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d4/desktop-frame-editor.png)、[竖屏长名](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d4/portrait390-long.png)、[窄横屏长名](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d4/compact-long.png)。已查看四张最终截图。来源核查为只读源码，不是运行时采样；无Unity产品/编译/Play/Unity测试操作。

## D3历史网页定向自检

D3当轮运行 `tooling/check-prototype-d3.cjs`，23项检查通过，0JS运行错误。覆盖GM点击编辑、只读Hz、键盘Tab/选择/输入、双向共享状态、自定义180与无效小数、同步禁用、外部Hz联动、ESC优先级/焦点归还、关闭重开保值、编辑不拖GM/不触底层、受影响扩展滚动及横/竖/320/390宽可见性。未重复D1/D2全套背景、玻璃或余量表测试。

结果 [evidence/d3/web-checks.json](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d3/web-checks.json)，最终截图 [默认收起](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d3/desktop-collapsed.png)、[横屏编辑180](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d3/desktop-editor180.png)、[竖屏390宽编辑](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d3/portrait390-editor180.png)。已查看最终横屏与竖屏编辑截图。键盘检查中修正了自动脚本对原生select的Enter使用方式（Enter会打开列表，Tab先关闭列表）；网页标题拖动时的文本选中已禁用并定向复核。无Unity产品、编译、Play或Unity测试操作。

## D2历史网页定向自检

D2当轮运行 `tooling/check-prototype-d2.cjs`：20项检查通过，0 JS运行错误。覆盖默认高响应+自动、144→147、自定义180原值、换刷新率自定义保持/自动重算、同步禁用且保留输入、候选表及声明边界、无效输入、GM独立模拟FPS、移动待定提示及横/竖/320/390宽适配。因GM footer变化补核少量按钮可容纳与24按钮滚动固定区，未重复D1拖动/玻璃等大范围检查。

结果 [evidence/d2/web-checks.json](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d2/web-checks.json)，最终截图 [默认144自动147](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d2/desktop-auto147.png)、[自定义180](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d2/desktop-custom180.png)、[设置局部](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d2/settings-custom180.png)、[竖屏390宽](../../../.utmp/ember-task-archives/20261119-gm-control-center/materials/evidence/d2/portrait390-custom180.png)。已视觉查看默认、设置局部与竖屏截图。D1证据保留为历史，不代表D2更新后全部重新验收。无Unity产品、编译、Play或Unity测试操作。

任何阶段自检或网页交付都不替代主聊天用户确认。当前应回主聊天审阅/返工方案，不派实施，不执行模板保存/Bump/提交/发布。
