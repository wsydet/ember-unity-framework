# S8 组件职责补充验收

2026-10-27。范围为 base 页面内尚未归属组件的表现职责，包含 GM；保留页面业务事件、加载门槛与原有生命周期。

- 新增九个真实公共源 Variant：GM 时间倍率、状态、增强 Toggle、圆形进度与图片；设置上下文；库进度；动态列表摘要与行进度。七个带专用逻辑，圆形与图片使用预制体默认值。
- 页面通过组件 API 设置数据，时间倍率格式、状态文本、进度百分比和摘要由专用组件持有。Loading 的专用进度组件见相邻 S8-loading-progress 记录。
- 主菜单/玩法导航、弹窗请求与结果、动态列表租约、Tab 内容切换继续由所属页面管理；BootSplash 自持启动生命周期，不迁移为页面业务。没有新增美术需求，复用现有素材。
- 备份位于 `.utmp/ui-component-responsibilities-20261027/before`，367 文件及 SHA 清单见 backup.json。
- runtime.json 为 12 项运行检查，candidate.json 为 4 项候选继承检查；tests.json 为相关 EditMode 16/16。final-audit.json：71 Prefab、70 Binding、44 库项，绑定和库错误均为 0。
- 最后编译 started 1793068587174 / finished 1793068590711 / reload 1793068597778。GM 截图半透明来自原有 CanvasGroup alpha 0.5，已与备份核对。
- 普通保存 698 文件。后续按用户更新派生模板的要求，经项目中心本地封存 base 0.10.0，内容及封存 hash 为 `61cbe49aa797b1604f293b9756674566`。框架仍 0.22.0；未发布。

没有执行全量测试、构建、真机或消费项目部署。派生模板同步另见 S8-parent-sync，不以 base 测试代替派生验证。
