# 动画接入协议

源码相对于项目根：`Assets/Game/Module/Player/PlayerAnimationProfile.cs`、`PlayerAnimationView.cs`、`PlayerAnimationSession.cs`、`PlayerSchemeCapture.cs`；编辑工作流在同模块 `Editor/PlayerSchemeWorkflow.cs`、`PlayerSchemeLibrary.cs`。目录变更时重新发现真实源码，不依赖临时设计文档。

## 所有权与映射

`PlayerSchemeWorkflow.Copy` 深复制可变方案配置，保留图、Definition与美术引用；复制方案不会自动复制Controller/Clip。`PlayerSchemeOwnership.RequireWritable`只说明不是受保护资产，还应查其他方案是否共享目标。单纯替换图片通常只需自有AnimationProfile与新Clip，Controller和Key继续只读共享。

`PlayerAnimationProfile.Controller`要求**单层、无参数的基础AnimatorController**，不接受把AnimatorOverrideController资产塞入该字段。每宿主`PlayerAnimationSession`运行时创建自己的Override，把槽的Key映射到Clip，并在结束时释放。不能另造全局Override或手动替换正在运行实例的Controller。

每个槽：

- `_nodeId`对应真实图节点，包括父节点；集合必须一一对应。
- `_state`是完整Animator状态路径，如`Base Layer.idle`，唯一；对应状态直接引用本槽Key，无自动转换/回调。
- `_key`是Controller实际包含的独占原片段对象；按引用匹配，不按名字查找。不因换图片修改Key。
- `_clip`是播放替换资源；允许经明确选择复用，必须有有效资产引用。
- `_speed`有限正数，`_blendSeconds`有限非负游戏秒；不能靠改播放速度或帧长修改玩法计时。

基础Controller不允许嵌套状态机、AnyState/Entry/State转换、参数或StateMachineBehaviour，状态数量与映射一致；state.speed保持1，不启用speed/time参数。新增状态才需要有意扩充Controller，先复制共享Controller并保留旧键/状态映射，不能修改内置共享素材。

`PlayerAnimationAssets.Install()`是首次安装保护工具，已有动画装配时拒绝。正常换图不能调用它重建现有层级和资产。

## 事务与冻结

写前记录方案闭包JSON/dirty、每个要改动资源的路径/GUID/localID、Prefab层级/碰撞体与图布局。新图片与片段先导入验证，再修改自有AnimationProfile的指定槽；使用SerializedObject和一个Undo组，不清空用户Undo历史。不在绘制/预览时保存或创建资产。

先在候选上运行`PlayerSchemeWorkflow.Validate`，确认全部槽、曲线和依赖合法；只显式保存授权资源，不全局SaveAssets。对已有Clip/TextureImporter改写记录可回退文件与导入设置；AssetDatabase导入和写文件不是普通Undo能完全撤销的操作，失败时只撤回本次创建/修改，不覆盖期间的新草稿。

当前会话的动画映射配置在准备时冻结，但Controller/Clip/Sprite属于共享Unity资源，不宣称运行中重写同一Clip的内容仍绝对不可见。优先新建候选美术资源并在新Play验证；不要在旧会话下原地重写共享美术再把画面称为冻结保证。

## 层级与播放

根物理对象 → Facing持有的Visual → Animation（Animator/PlayerAnimationView）→ Sprite。片段路径相对Animation；不写根Transform或Visual朝向层，不含Collider/Sensors，不写SpriteRenderer颜色（alpha由效果合成）。Animator禁用Root Motion。原物理尺寸示例1×1.75 u仅作参考，保留用户实际碰撞配置；更换图片不自动拟合碰撞体。

玩家动画观察已提交叶子及激活代数；稳定叶子不反复重播，同叶重入应从头播放。层级链中最终叶子表现优先，父节点槽保留后备契约。暂停把Animator.speed置0，恢复原槽速度。动画无权调用转换、扣次数或创建效果。

## 验证与报告

实际用`Ember/Player/玩家开发工具`选择完整候选，应用至下次白盒，再正常Play→Main→新游戏。检查左右朝向脚底、不同动作对齐、循环接缝和非Loop末帧、加速/刹车及本次启用动作、暂停恢复、同叶重入、效果闪烁/透明、多实例独立Override、保存后重载引用。窗口静态预览和实际Game画面分开报告；若缺少可执行场景/素材，则准确列出未验证项。

用户只有部分素材时交付“已替换两项，其余保留占位”等真实结论，不声称整套完成。附资源路径及映射表、PPU/脚底/朝向约定、项目默认是否改变。真实模板切换/消费部署/发行Build只有实际执行才能记为通过。
