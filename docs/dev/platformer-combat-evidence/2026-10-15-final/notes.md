# 2026-10-15 非 T03 最终补验

- 编译：Unity MCP scripts refresh 后 Console error=0，新增测试可执行；结束 Console error=0。
- S03：首次 job `2933486ac23e4761a0ecd2bea6e29a34` 因日志缺失原异常导致预期日志不匹配。PlayerController.Check 保留节点、阶段及 InnerException 后，[故障测试](runtime-fault.json) job `022b3bd2ba51491c900c6e728f833c2e` 1/1 Passed（0.63431 秒）。这是实例 FixedUpdate 回调注入，不修改图资产。
- [相关回归](related-regressions.json)：job `cb2de3571aff49a6ae7a896d69876f01` 2/2 Passed（0.1380329 秒），覆盖候选进入失败恢复旧宿主/效果及进入时禁用不复活旧宿主。本次未重跑历史全量套件。
- 正式入口：PlayFormalWithScheme 内置“通用攻击模板1”，Main 点击新游戏进入 GameplayScene；未修改项目默认方案。临时 timeScale=0.1，独立虚拟键盘操作真实玩家，渲染结束时捕获实际游戏画面。第一刀使用正式六帧资源，未以素材预览替代实播。
- 右向：[trace](slash-right/trace.txt)，Slash_0.png 至 Slash_5.png 六张逐一检查通过；scale=(3.2,3.2,1)，位置 x=2.4，结束 idle。
- 左向：[trace](slash-left-confirmed/trace.txt)，先 A 转身，记录 facing=False 后再攻击；Slash_0.png 至 Slash_5.png 六张逐一检查通过，scale=(-3.2,3.2,1)，位置 x=-3.47，结束 idle。左右展开、朝向及消退正常。本次是降速实时渲染检查，不声称对所有招式做原速逐帧录制。
- `slash-left/` 保留首次未成功转身的采集：实际 scale.x 为正，仍朝右，不作为左向通过证据。
- 清理：捕获回调取消订阅、输入设备限制恢复、虚拟键盘移除、timeScale=1，退出 Play；FrameworkScene dirty=false，Play Mode Options=1/Enabled=true，玩家窗口仍为空。默认方案 GUID `a2d7100fc47f54b4282b5be389d735b8` 及配置 JSON 与开始一致。
- T03 剩余项仍未验证，延期真实项目；本次未切换模板、Bump、提交、发布或重新保存模板快照。新增测试、诊断修复及正式说明在 Assets 中，尚未纳入此前已保存快照。
