# 实施定向返工 A1

接续任务20261117-soft-glass-ui-gm，任务目录.utmp/ember-workflow/20261117-soft-glass-ui-gm/。

先读项目规则、requirements.md R4、handoff.md H11、acceptance.md A1，再核对当前源码与evidence/acceptance/final-baseline.json。独立验收发现SG-IA-02（P1，普通模态Popup下层键盘字符/Submit穿透）和SG-IA-01（P2，Blur→UseMask=false后局部玻璃持有disposed外部lease），AC03/04失败。

由实施阶段在原范围已有授权内修复这两项，不重复请求相同修复授权。按acceptance.md各项复验矩阵执行相关短验证；每批产品代码修改后通过Unity MCP显式编译，确认本轮域重载和Console。保留下层场景/UI渲染以支持模糊，不靠隐藏下层规避输入阻挡；保留UseMask=false与逻辑拒绝mask的业务语义及局部玻璃独立配置。补充真正覆盖故障的回归，不只断言mask存在或首射线命中。

R2用户视觉确认仍有效，原manifest覆盖886份文件；验收没有修改产品，5张R2图及产品指纹均无漂移。修复后更新implementation、handoff、证据及受影响文件基线；视觉改变则重出受影响实际预览并按R08更新确认。由另一独立验收聊天定向复验，不能由实施自检代签。

AC11的base正式保存/分发仍未完成；AC09真实设备触摸/安全区/方向/键盘覆盖仍待验证。先准备保护topdown草稿的精确差异与恢复计划，具体模板操作与设备验证按各自门槛承接。不要擅自切换/保存/Bump/同步模板，不提交发布、不全量长测、不归档清理。沿用当前checkout，不创建worktree或子代理，不给其他聊天发送消息。
