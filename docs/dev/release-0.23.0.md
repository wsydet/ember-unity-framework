# Ember 0.23.0 发布记录

本版交付 S1–S8 UI 制作流程、公共组件库、base 全量组件化及派生模板同步，并改进项目初始化页的模板身份与更新状态布局。

## 发布内容

- 公共 Prefab 库、动态列表与实例所有权、组件契约、PSD/BUI 制作及换肤流程；技能源先提交再生成可追溯 bundle。
- base 的 GM、Loading 和其他页面复用公共组件及专用 Variant。Loading 数字与填充、GM 倍率与状态、动态摘要等表现职责由组件管理，保留页面业务流程和绑定。
- 模板内容版本：base 0.10.0、ui-game 0.2.0、platformer-2d 0.12.1、source3d-2p5d 0.6.0、visual-novel 0.22.1。四个派生均继承 base 0.10.0。
- 项目初始化页显示当前模板、当前版本、包内版本与更新状态，左侧模板列表、右侧详情；开发仓库导航至模板开发页。
- 保留技能同步和场景确认弹窗。代理须提前告知模板切换，出现等待或超时立即请用户检查确认；用户处理后核对实际结果再继续，不无限等待或重复发起请求。见[公共规则](../../Packages/com.ember/Documentation~/maintenance/unity-validation.md#模板切换弹窗与人工确认)。

## 验证与边界

沿用对应最终内容的增量证据：[S8 组件职责](ui-production-upgrade/evidence/S8-component-responsibilities/README.md)、[Loading](ui-production-upgrade/evidence/S8-loading-progress/README.md)、[四模板同步](ui-production-upgrade/evidence/S8-parent-sync/README.md)、[初始化布局](project-setup-layout/README.md)。base 组件相关 EditMode 16/16；四模板直接相关测试分别 4、13、13、20 项通过。这些记录不是全量玩法验收。

发布候选共享二进制完整性（含两种换行配置检出）及字体配置检查通过。技能 bundle 从源码提交 `5868837542df759baf6ffd9412c7ae10cf7ee66b` 在干净副本生成，11 项技能、33 个文件校验通过；原工作区模板切换产生的技能删除保留。

初始化页 5 项状态检查通过，已完成相应编译；面板视觉与消费项目实机验收未完成。未进行发行构建、真机、全语言或消费项目升级。未将工作区其他 FSM 开发和共享字体动态缓存改动纳入发布。

## 发布进度

五个模板均已通过正式 API 声明兼容 0.23.0；四个派生为 Synced，实际内容与封存 hash、父快照一致，继承图无错误。visual-novel 同步曾返回空结果；用户确认无弹窗后查询证明原事务已完成，没有重复提交。

发布前显式编译 started1793078081750 / finished1793078085267 / reload1793078093086，Console 错误和警告均为 0。正式项目校验 base 差异 0、错误 0；8 条既有提示为测试状态未关联场景。发布前检查通过后创建 v0.23.0 并推送 main 与单个版本标签，最终结果以远端引用为准。

开发证据目录保留阶段说明和关键测试/校验结果；大型契约快照、截图及制作中间产物留在原本地备份，不作为本版包内容交付。

消费项目通过 Ember/UPM Manager 升级框架；模板升级是独立操作，跨模板 minor 先保护定制内容，再按正式迁移流程处理。
