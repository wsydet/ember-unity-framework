# GM维护、模板封存与 Ember 0.27.0 交付记录

2026-11-20，完整多聊天模式。本轮承接[GM控制中心已签收任务](../20261119-gm-control-center/README.md)，修复四个region中文标题和制作回执输出目录，正式封存base0.12.0并向六个派生同步，交付框架0.27.0。

[需求与授权](requirements.md)、[主聊天交接](handoff.md)、[R1实施与原阻塞](implementation-r1.md)、[R2实施与恢复](implementation-r2.md)、[RV1独立验收](acceptance-rv1.md)、[证据索引](evidence-index.md)。发布说明见[Ember0.27.0](../../dev/release-0.27.0.md)。

本地候选的八项必需标准经独立验收，无新增阻塞；RC02保留CHECK-N01直接bundle检查的换行限制及独立来源证明。PB01属于实际网络发布核对：以远端main与annotated v0.27.0 peeled commit一致为准。最终机器核对与完整证据包见本机归档publication-final.json和final-archive.json，不能仅以包版本号判断远端发布。

本记录随发行提交封存，不预填尚未发生的网络动作或自引用commit SHA。取得远端v0.27.0后，可用tag解析交付提交；原始阶段记录中的“未发布”描述其当轮历史状态。

不包含消费项目升级、六派生Play或Player/设备/硬件刷新率验收。用户原有两份字体缓存和QualitySettings修改受保护且排除提交。
