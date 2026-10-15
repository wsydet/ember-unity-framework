# 基础模板文档

归属：`base`。这里记录随基础业务骨架交付的模块说明，派生模板会继承本目录。

- [Guide 模块设计](guide-module-design.md)：配置、运行流程、扩展点与待办。
- [引导编辑器使用](guide-editor-usage.md)：编辑步骤、条件、页面选择与验收。
- [配置表接入](../../Table/README.md)：项目 Row、Definition、源数据和生成产物约定。
- [GameInput 输入模块](../GameInput.md)：通道配置、所有权仲裁、就绪结果与输入恢复协议。
- [PlayerData 存档模块](../PlayerDataStorage.md)：配置自加载、显式快照、槽位读写、可靠文件协议、队列、Drain 与生命周期；阶段 2.1—2.3 已实现，按需启用。

Guide、PlayerData、GameTable、GameInput 等可选模块是否启用，以对应 EmberModule 特性为准；文档存在不代表已启用。
框架 API、发布记录和模板管理机制由框架文档维护，不复制到本目录。
