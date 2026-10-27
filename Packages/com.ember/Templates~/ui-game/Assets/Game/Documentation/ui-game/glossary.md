# UI 游戏术语

| 简称 | 本模板含义 |
| --- | --- |
| 一局 / session | UiGameModule 当前持有的一次三步示例会话 |
| 代次 | 会话 Generation；重开或再次进入产生新代次，旧会话不得影响新局 |
| 完成 / 结果 | 三步完成后的同一 Gameplay 阶段，不是新的框架状态 |
| 暂停 | Gameplay 压入 Settings 后的业务输入锁；恢复只解除暂停来源 |
| 返回 | 正式 RequestTransitionTo Main，等待 Completion，释放 Gameplay 所有权 |
| 步骤行 | 复用 EUIDynamicOptionRowItem 的只读动态展示，key 与显示 index 分离 |
| 保存 / 封存 | 项目中心普通保存与显式 Bump，两者不同 |

框架通用词义以安装包 `Documentation~/maintenance/glossary.md` 为准。
