# 0.24.3 发布门禁

基线 `baf06869256ebe295667875b3f266a93142a4d22`（0.24.2），目标origin/main及annotated tag v0.24.3。用户明确要求发布框架；Windward不升级／重部署。发布不代表三项全部关闭，退出AV仍未修复。

- [编译及模板](compile-and-templates.json)、[编译／域重载时间戳](compile-state.json)、[Console](console.json)：主动请求编译已完成，实际加载程序集并校验五模板hash与四个ParentSnapshot；零模板图问题。Console有8条环境／空程序集警告。
- [实际9例测试](tests.json)；[空过滤回执](tests-empty-filter.json)不计通过。
- [旧兼容测试后的源字体状态](fonts-after-legacy-tests.json)与[差异](source-font-hashes.json)：三源字形缓存落盘，未迁移默认引用不属于字体源保护通过。精确恢复前轮验证基线并MCP导入后，[自有资源专项2例](tests-owned-fonts.json)通过，[最终源哈希](source-font-hashes-final.json)四份一致。生成资源保护不等于所有直接包源引用均已迁移。
- 同源码前轮构建、画面、36格采样、字形及源保护证据见[实施记录](../../b03-player-exit-font-20261028.md)。不是本次新构建，也不是Windward已安装并通过。
- 原生AV稳定对照及失败dump／超时记录见[独立调查](../../exit-native-20261028.md)。有效dump仅本机保留，不随Git发布；校验摘要与栈提交存档。

选择性提交排除FSM、其他UI业务、项目设置、模板快照、技能源和其他既有工作区修改。静态门禁使用隔离候选；主工作区的技能检查因既有未跟踪模板Skill目录拒绝，不能删除或顺带提交它们。技能bundle本身不重建，sourceCommit不改写。

隔离候选严格通过[共享二进制／字体／技能bundle及链接门禁](static-gates.json)。原始20份Player日志按原字节打入[player-logs.zip](player-logs.zip)，[逐文件长度及SHA256](player-logs.json)可核对；原日志仍在本机原位置，没有为通过空白检查改写失败证据。两份超大业务逐帧原始结果留在本机，发布保留相应汇总／退出码／失败记录。

主工作区无关改动的[保护检查](workspace-protection.json)与最终提交／远端身份一起交付。详细覆盖与未覆盖范围见[发布说明](../../../dev/release-0.24.3.md)。
