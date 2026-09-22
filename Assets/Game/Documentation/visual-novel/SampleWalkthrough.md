# 《最后一盏灯》：体验与演出仿写

从 FrameworkScene 的主菜单点“新游戏”，直接进入故事。没有测试序章或说明对白。当前工作区保留 79 句正文、两条路线、两个结局；每条路线 59 句。原 lineId、optionId、endingId 保留，7 句配合动作改写的旁白提高了 textRevision，各对话段提高 contentRevision。改稿后请从新游戏开始，旧档可能被内容指纹检查拒绝。

## 故事与素材

林晚带着信封等在天台，周砚送来录音机。玩家选择先听录音或先写回信，两条路线汇入共同收尾，再根据 heard 进入“有人听见”或“明天继续”。

只使用正式素材：`lastlight_alice`（历史资源键，实际为林晚）、`lastlight_zhou` 和 `lastlight_rooftop`。不再出现 M1 简陋人形或方块背景。E2 新增同构图的林晚微笑 `lastlight_wan_smile` 与屋顶入夜 `lastlight_rooftop_night`，用于真实双图过渡；原三张正式图保留。点头、倾身、转向仍是变换和手势。旧测试键 alice_neutral / alice_smile / lin_neutral / campus / evening 兼容指向这三张正式图，键名不代表新增表情素材。

## 演出如何融入剧情

下表的“句”是节点中的 Say 序号。流程窗口选中节点后，在相邻对白之间查看对应指令；动作 commandId 使用 `last_light_<节点>_stage_<动作>`，可用后缀快速定位。人物实例一直是 `wan` / `zhou`，独立于摆放位置。

| 情节位置 | 画面与叙事目的 | 可参考的编排 |
|---|---|---|
| intro 1–4 | 林晚居中，先交代信封，再说有配音的第一句 | 段首明确背景、音乐、人物和强调默认状态；保留原 Voice |
| intro 5–6 | 林晚让到左侧；周砚从画面外抱着录音机走入右侧，动作期间旁白继续 | `make_room`、`zhou_enter` 分别 Parallel，后接 `arrive` WaitActions；入场先 Show Normalized，再 Move Named |
| intro 9 | 周砚看完手机，轻轻点头 | `zhou_nod` 使用 Nod，强度 0.4，结束自动恢复 |
| intro 11–12 | 林晚犹豫时视线集中在她身上，随后回到说话者强调 | `letter_focus` Manual(wan)，下一句 `listen` Auto；旁白时 Auto 不错误选择人物 |
| intro 13–14 | 风掀起纸角，林晚倾身按住，又收回手 | `catch_paper` Rotate(-3°) 并行，短 Wait 后 `steady_paper` 接管到 0°，带 0.15 秒延迟；`paper_settles` 同时等待旧动作取消及新动作完成 |
| intro 17–18 | 叙述者把目光从两人移向街灯，画面成为空镜 | `wan_out_of_focus`、`zhou_out_of_focus` 分别淡出，后者延迟 0.15 秒；`quiet` 等待全部结束再 Hide，保持资源清理和画面连续 |
| intro 19 | 林晚从栏杆边走回来摊开信封，周砚把录音机转过来 | 同实例退场后重新 Show，再 Move；新姿态为基准状态，无上一段旋转残留 |
| intro 23 | 林晚笑了一下 | `laugh` 使用低强度 Jump，短促起伏后回位 |
| radio 2–3 | 播放键按下，注意力转向录音 | 背景透明度与两个人物透明度分别并行，人物延迟不同；`memory_quiet` 等待组保证节奏 |
| radio 6–7 | 林晚回忆时稍稍倾身，随后坐直 | `lean_in` Scale 与 `listen_tilt` Rotate 并行，不相互接管；`sit_back`、`straighten` 回到基准 |
| radio 12–14 | 风停了，周围安静下来；林晚向老师回应晚安 | `hushed_world` 调整 Stage 透明度，对白 UI 不变；`present_world` 等动作恢复舞台、背景和人物；保留原晚安配音 |
| letter 2–3 | 林晚让位，周砚抱盒子换到左边，她走到右侧铺纸 | `wan_steps_aside` 先到 Normalized 临时位置，再并行 `zhou_brings_box` / `wan_to_paper`。先释放命名位置再交换，避免静默覆盖；`zhou_turn` Mirror 表达转向 |
| letter 6 | 笑声轻轻打断紧张 | `small_laugh`，强度 0.2 的 Jump |
| letter 8–10 | 周砚凑近写字，林晚压住纸角，他退后让出光线 | `zhou_near_paper` 让人物靠近；`zhou_in_front` 与 `zhou_behind` 调整层级，随后 `zhou_back` 返回左侧；层级和位置分别编排 |
| letter 13 | 约定得到回复，周砚点头 | `agreement` Nod，强度 0.45 |
| common 5–6 | 收好东西，林晚走向门边，周砚转身提盒子 | `wan_to_door` Move，`zhou_turn_door` Mirror；对白间等待到位 |
| common 8 | 回应玩笑，林晚轻轻颔首 | `smile_nod` Nod，保持克制幅度 |
| common 12–14 | 两人拉开门走下台阶，天台留下最后一盏灯 | 两条带 ExitAfterMove 的画面外 Move，带错开的延迟；`door_closes` 等待后进入空镜，`last_light` 轻淡舞台，结局前等待完成 |

每个节点先清理命名位置并重建明确状态，因此 radio、letter、common 可独立从编辑器试播。正式验证两种结局仍需走两条完整路线。此处保留 E0/E1 编排；E2 新增片段见下节，原 79 句正文、配音和分支保持不变。

## 参考时关注的规则

- 普通 Show / Hide、背景和资源准备沿用原剧情。要移动已存在人物，用 Move；不要再 Show 一份同人物来代替移动。
- Parallel 允许对白与动作同时推进；后面的 WaitActions 表达叙事上的“到位之后再继续”。时长适合故事节奏，不为验收故意拉长。
- 同实例同属性会接管，不同属性可并行。intro 的纸角动作是接管示例；radio 的倾身是缩放和旋转并行示例。
- Stage / Background / Character 透明度是独立属性，Stage 与局部透明度叠乘；阅读文字和按钮不属于舞台。
- Named 位置被新 Move 立即预占，即使动作带延迟。交换人物先让出一个位置，不能直接对占用目标同时发 Move。
- Gesture 是临时偏移，完成或取消归零；Scale、Rotate、Mirror、Layer 是持续姿态，需显式恢复或退场重入。
- 表情交叉淡化用 Character/wan + `lastlight_wan_smile`；旧 `alice_smile` 是兼容键，仍指向原图，不能用于区别两张表情。

## 阅读中验收，不把提示写进正文

| 检查 | 推荐停留处 | 预期 |
|---|---|---|
| 暂停与倍率 | intro 5 的入场、radio 2 的淡化 | 菜单暂停，恢复继续；2X/3X 加速动作，不改配音音高 |
| 存档与恢复 | 动作仍在进行时先补全文字，再保存 | 只在完整对白/选项稳定点保存；读档恢复最终位置、透明度与姿态，不重播动作过渡 |
| 等待和接管 | intro 13–14 | 接管从当前角度继续，无突然回弹；旧句柄取消不会卡住等待组 |
| 位置和层级 | letter 2–10 | 两人换位仍各自保持身份；靠近、前后层级与返回正确；16:9 / 4:3 目标位置一致 |
| 退出清理 | 任意并行动作期间返回菜单，再新游戏 | 没有旧动作写入新会话，首屏恢复居中林晚与完整背景 |
| 已读快进 | 完成一路后重新阅读 | 已读动作收敛到终态，未读句和选项停止 |
| 阅读与音频 | intro 3、radio 13 | 原有逐字、补全、Voice、历史、隐藏、字号、存读档和双路线继续工作 |

仍保留原 79 句与两条结局的回归。用户确认上一轮测试通过；本次叙事重编与素材迁移是新的修改，静态检查不等于 Unity 演出复验。精确记录见 Implementation。

## E2 正文中的画面演出

当前共 204 条指令、58 个动作 ID（含 13 个 E2 动作），没有新加测试对白。下列后缀都带 `e2_`，完整 commandId 为 `last_light_<节点>_stage_<后缀>`。

| 正文位置 | 演出与可参考步骤 |
|---|---|
| intro 1，门合上 | `e2_door_settle`：Stage Shake，强度 0.16、8 Hz、0.6 秒衰减；对白不动 |
| intro 13，风吹纸角 | `e2_wind`：wan 单人横向抖动，与原旋转动作独立叠加 |
| intro 22–23，林晚笑起来 | `e2_smile`：原立绘 → 微笑，0.75 秒 SmoothStep 双图过渡；等待后继续 |
| radio 2–3，播放键与回忆 | `e2_memory_flash`：0.22 白色闪光，0.7 秒 + 0.08 秒保持，与既有提示音/透明度动作相邻启动；随后等待 |
| radio 8、13–14，回忆到回应 | `e2_memory_haze` 白色薄幕 0.12；`e2_memory_clear` 收回到 0。Cover 保留最终状态，必须显式撤幕 |
| letter 2–3，换位铺纸 | `e2_paper_wind`：wan 边移动边抖，1.6 秒，强度 0.2；与两人 Move 共存，等待后无位移残留 |
| letter 12–13，拍信件照片 | `e2_photo_flash`：透明度 0.16、0.5 秒 + 0.05 秒保持，显式覆盖整个阅读页；闪后恢复，不写入存档 |
| common 开头，时间推移 | `e2_evening_close` 黑幕闭合后重建背景/人物；`e2_evening_reveal` 撤幕，不闪出旧图 |
| common 1–2，夕阳退去 | `e2_nightfall`：屋顶傍晚 → 入夜，5 秒并行 CrossFade；背景旧图直到淡完才释放 |
| common 8–9，三人笑起来 | `e2_relief_smile`：wan 微笑，与原点头并行，保持实例位置与强调 |
| common 14，最后一盏灯 | `e2_last_light_out`：2 秒黑幕，结局前 `e2_last_light_done` 等待，正文仍可读 |

验收沿正文完成：在 letter 换位时开菜单看暂停/恢复；在 common 入夜时切倍率、完整显示对白后存读档，恢复应直接到夜景；在系统设置将震动/闪光设为正常、减弱、关闭后分别重读相关片段，等待时长应一致。用户已确认本轮测试全部通过、无报错。以上操作及黑幕后换景、关闭闪光、快进两分支、退出重开和 16:9/4:3 画面继续作为人工观感复核清单；本次反馈未单独确认逐项人工验收，不能据此关闭平台与画面观感待办。素材生成记录见 [E2Artwork](E2Artwork.md)。

## E3 正文中的环境与声音

本轮保留全部 79 条 Say（包括正文、lineId、textRevision 和 Voice 键），共 218 指令；原选项、变量和连接不变。新增 14 条指令，另将原 BGM 入场改为 2 秒并行淡入、剧情音量 0.6。随后用户反馈“全部成功”；E3 验收通过，未提供逐平台/听音记录。

| 节点 | 新增自然演出 | 配置定位 |
|---|---|---|
| CH01_intro | 天台稀疏暖光点、低音量风声跨对白持续 | `CH01_intro_e3_motes` / `CH01_intro_e3_wind` |
| CH01_radio | 延续天台气氛，按录音键的短促反光；屏住呼吸时风声降到 0.12 | `CH01_radio_e3_motes` / `CH01_radio_e3_wind` / `radio_e3_glint` / `radio_e3_wind_soften` |
| CH01_letter | 写信时继续光点与风声，无新增对白或配音替换 | `CH01_letter_e3_motes` / `CH01_letter_e3_wind` |
| CH01_common | 入夜音乐 3 秒交叉渐变至 0.55；下楼前关闭光点、2 秒淡出风声、3 秒淡出音乐 | `CH01_common_e3_evening` / `common_e3_stop_motes` / `common_e3_stop_wind` / `common_e3_stop_bgm` |

每段起始按同一实例 ID 建立气氛，方便现有指定入口与节点试播；接管不会叠加多个同名实例。光点和风声勾选换场保留，原段内背景初始化不会使其意外遗失，离场显式清理。`rain` 是可复用粒子资源，正文不额外插入雨天或测试章节。

新增原创合成音频 `Audio/Narrative/LastLight/rooftop_wind.wav` 和 `evening.wav`（各 7.5 秒，22,050 Hz 单声道 PCM），配表键分别为 SFX `lastlight_rooftop_wind` 与 BGM `lastlight_evening`。它们是程序合成的环境/音乐演示，不替换原两句 Voice。导出仍走配置表中心；首次导入后检查资源键可选。


## E4 正文中的文字节奏与模式

在 E3 的 218 条指令基础上，新加 1 张开场章节卡和 1 条入夜剧情隐藏，现共 220 条 / 80 条 Say。原 79 句的正文、lineId、textRevision、Voice 和全部旧参数不变；原指令顺序、选项、变量与 Next 保留。没有新加测试段落或按钮。

| 位置 | 自然演出 | 配置定位 |
|---|---|---|
| intro 开场 | “最后一盏灯”居中章节卡，停 0.35 秒后一次显示；后接全屏雨后台词 | `last_light_intro_e4_title` / `last_light_intro_001` |
| intro 手机消息与内心独白 | 引号内消息即时显示；独白句间停顿后减速 | `last_light_intro_009` / `last_light_intro_010` |
| intro 安静片刻 | 独白切全屏，后续普通对白恢复 | `last_light_intro_018` |
| radio 屏住呼吸 | 第一处句号后留出 0.65 秒，再慢速继续 | `last_light_radio_012` |
| letter 落笔与笔迹 | “对不起”后的迟疑用停顿；不愿重抄的心声使用全屏旁白 | `last_light_letter_004` / `last_light_letter_009` |
| common 入夜 | 换景演出前隐藏对白，下一句自动恢复；取名时稍停；末句全屏慢速收尾 | `last_light_common_e4_hide` / `last_light_common_011` / `last_light_common_014` |

共 8 个结构化节奏点、4 句全屏旁白和 1 张标题卡。长文按实际字体与区域分页，不把分页符或节奏控制符塞进正文。沿以上原节点使用“播放节点”或正式新游戏复核；E4 测试随后获用户独立确认通过；逐项观感与平台验证未单独提供，不扩大推定。



## E5 镜头与擦除索引（用户确认通过，纳入 0.6.0）

在 E4 的 220 条指令上增加 10 条普通演出指令，共 **230 条、80 条 Say**（原 79 句正文加 E4 章节卡）；所有 Say 字段、配音指令、选项、跳转与结局保持不变。不添加测试段落或按钮。

| 节点/位置 | 指令 ID 或后缀 | 演出 |
|---|---|---|
| 四个对话节点开头 | `last_light_<节点>_stage_e5_reset` | 中性镜头，为独立试播建立初始构图 |
| intro 第 10 句前 | `intro_stage_e5_memory` / `intro_stage_e5_warm`（共同前缀 `last_light_`） | 回忆氛围：1.12 倍推近、0.08 暖色遮罩，随原独白并行 |
| intro 第 11 句前 | `intro_stage_e5_return` / `intro_stage_e5_clear` | 回到原构图、清除暖色遮罩 |
| letter 第 10 句前 | `letter_stage_e5_letter_close` | 信件段落轻推近到 1.16 倍，右移 0.035 |
| letter 第 11 句前 | `letter_stage_e5_letter_return` | 0.8 秒回到原构图 |
| common 夜色过渡 | `last_light_common_stage_e2_nightfall` | 保留原动作 ID/等待组/背景键，改为右→左擦除 |

从新游戏分别走两条路线，再独立播放 intro、letter 与 common：检查文字不随镜头移动、复位后原构图一致、夜色擦除中间帧有旧图、暂停不跳帧、快进与存读档收敛到目标背景。停止/关窗后不应有粒子、声音或镜头残留。上述入口保留供后续回归；E5 随后已获用户“全部通过”反馈，文档已收束。未提供 XML/复跑总数或逐项观感记录，不扩大推定平台验收。

