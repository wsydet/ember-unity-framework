# lane-defense-2d

直接继承 base 的中性横向分路塔防基础模板。正式活动身份为 `lane-defense-2d`；本候选随 com.ember 分发，部署后 Assets 归消费项目所有。当前候选版本及验证状态以项目中心和 [Validation](Validation.md) 为准，未发布内容不能当成已发行包。

从 `Assets/Game/Scenes/FrameworkScene.unity` 启动，经 Main 的开始按钮进入 Gameplay。选择卡牌后点击棋盘空格，右键或 Esc 取消；HUD 提供开始波次、拆除及暂停，暂停和结算弹窗提供重开与返回主菜单。重开按本关初始配置创建新会话，不恢复中途快照。

- [术语](glossary.md)
- [运行契约](RuntimeContracts.md)
- [消费扩展](ExtensionGuide.md)
- [制作与部署](Authoring.md)
- [实际验证与边界](Validation.md)

模板只含棋盘、卡牌事务、基础射手/阻挡物/敌人、波次及会话生命周期。特殊弹道、颜色、反射、分裂、传送、合成、护盾组合、剧情与养成属于消费项目。首版不提供持久化进度或战斗中途存档。
