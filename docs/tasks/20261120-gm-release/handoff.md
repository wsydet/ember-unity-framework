# 主聊天交接与发布核对契约

任务20261120-gm-release；主聊天01a1f2a7-d1b7-7073-b4d0-4331239ed002，实施01a1f366-9c93-7cc0-b2dd-3f29baa7c3b1，独立验收01a1f39c-e305-7490-93a6-2f8bafb4524c，hostId均local。沿用用户完整模式及发布授权。

主聊天已核对R1/R2实施与RV1正常完成的实际交付。MA01—MA03、TP01/02、RC01、PR01通过；RC02满足候选检查并保留CHECK-N01。R1目录句柄阻塞经重启解除；两子启动融合正式Load→Assets修改→编译→Save/Bump完成。base0.12.0、lane0.1.6、platformer0.13.7、source3d0.6.6、topdown0.1.7、ui-game0.2.6、visual-novel0.22.7，均兼容0.27.0、父级对齐。

当前正式编辑base0.12.0，915受管文件与切换前一致。主动编译/重载证据完整，RV1新鲜状态非Play/非编译/非更新；没有编译错误，最后两条MCP传输警告保留。两字体缓存及QualitySettings用户修改未暂存，EditorSettings原字节保持。无消费升级。

PB01由已授权P1承担：准确选择性提交候选及本正式记录；创建不可变annotated v0.27.0；仅atomic、非force推送origin/main及此tag；随后核对远端main、tag peeled均为发行提交。已存在正确commit/tag时复用，不移动旧tag，不批量推送。网络结果只以实际远端核对为据。

最终发布核对文件为[publication-final.json](../../../.utmp/ember-task-archives/20261120-gm-release/publication-final.json)；该文件只有实际发布后才生成。完整原件与核验包由[归档清单](archive.json)及[证据索引](evidence-index.md)定位。主聊天在PB01满足后核对完整归档、链接及任务所有权，再清理本任务临时目录；不清理原GM归档或任何用户修改。
