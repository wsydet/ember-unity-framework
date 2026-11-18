# A2独立复验后的实施返工

接续20261117-soft-glass-ui-gm，项目C:/Users/wuyu/My/ember-unity-framework，任务目录.utmp/ember-workflow/20261117-soft-glass-ui-gm/。

负责实施定向修复，不接管独立验收或归档。先读项目规则、requirements.md R4、handoff.md H13、acceptance.md顶部A2及evidence/reacceptance-a1/的独立证据。沿用checkout，不创建worktree/子代理，保护其他改动。

SG-IA-01已通过独立复验。SG-IA-02的原始字符/Submit/Move穿透已修复，但双Popup隐藏/重显还有P2子项，保留稳定ID，不改为已关闭：

1. 选择Showcase的BackgroundMode，依次开下层Confirm、上层Alert，HidePageViewOnly上层后等30帧。下层仍Paused、alpha1、interactable=false、blocksRaycasts=false，三个按钮均不可交互，selection=null。证据hide-chain-repro.json和03-top-hidden-dead-focus.png。
2. 明确选择BackgroundMode，开两层；隐藏上层→重显上层→隐藏下层→重显下层；依次关闭上层和下层。主页面可交互但原selection丢失为null。证据cross-hide-return-repro.json。未经过隐藏的对照正常。

修复实际可见模态顶层的交互所有权和隐藏/重显返回链，检查PageContext暂停/恢复与EUIModalInputScope联动。不要无条件OnResume或重新启用业务本来禁用的根；不抢更高GM焦点，不破坏UseMask=false/逻辑拒绝门禁，不隐藏下层规避背景采样。补充包含真实页面状态的双层隐藏回归；现有单层scope测试和40项矩阵未覆盖此组合。

独立15项探针的另3个false为原焦点已丢失后的连锁结果；孤立外部停用/重启、隐藏下层销毁均通过，不误修额外不存在的问题。

原范围修复已有授权，直接实施并通过MCP主动编译、确认域重载/Console后执行相关短验证。覆盖上述两条路径，以及原字符/Submit/Move、单/双关闭与销毁、业务禁用根、GM焦点、FullScreen和租约/资源归零；不要全量或长测。更新实施记录、证据和新manifest，再交原独立聊天复验，不代签通过。

当前890份产品文件零漂移，交付manifest SHA256 816A14A2C661BBF226BCC3515C67765A41AEB9569CC1B808B3248507733DDA33；R2五图无漂移，用户视觉确认有效。若改动影响视觉，按R08更新对应预览与确认。

AC09真实设备体验和AC11base正式保存/分发仍待完成；不得擅自模板切换/保存/Bump/同步/部署、提交发布或归档清理。
