# S1 运行、测试与验收

目标：Unity 6000.5.4f1，C:/Users/wuyu/My/ember-unity-framework，embedded com.ember 0.22.0，base 0.8.4。测试范围是相关类，不是全量测试。所有运行截图使用 Game View 的真实 Overlay 输出，完整文件尺寸以 PNG 为准；工具内预览缩到 800px。

| 验收 | 实际结果 |
| --- | --- |
| B01 | 主界面/设置页连续 Export，diff=0，blockers=[]；真实模式、标记、对象 ID、序列化属性和资源保存于契约 |
| B02 | EUIContractEditTests 验证颜色仅 appearance；删除 binding 与 Button→Image 类型变化阻断；空引用、重复语义阻断 |
| B03 | Label 槽位子文字不再被页面自动收集；EnableState 不改变 interactable |
| B04 | 运行示例两份不同实例：Static/Dynamic Init=1、Open=1；页面关闭等待结束后各 Dispose=1；父拥有实例再次工厂包装被拒绝。EditMode 同时覆盖动态重复包装/幂等释放/释放后重建 |
| B05 | Main/Setting 用户文件和 Prefab 摘要保持；生成代码通过实际编译；已有 Framework 元数据保留 |
| B06 | 六 GUID 保持。Main→Settings→Main→Gameplay→Settings→Gameplay→Main 真实 Button PointerClick 事件通过；两种 Settings 文本正确；GM 和 Loading 实测如下 |
| B07 | 创建并重复生成 EUIOwnershipExamplePage，Page 根/注册一致，diff=0；正式六旧名仅历史 .bak 残留 |

导航结果：Main 下设置文本“当前场景：主界面”；Gameplay 下设置文本“当前场景：玩法中”。最终 Main、Fsm.IsTransitioning=false、timeScale=1。事件由 EventSystem PointerClick handler 派发，不冒充物理鼠标或人工试玩。完整遮挡/输入设备组合延续 S0，未声称全设备回归。

GM：Btn_GM 打开/关闭；普通 Toggle 显示时间条；Slider 0.5 → 1.0，Time.timeScale 同步；EUIToggleEx 选中后 Label=“已开启”，随后复原。退出游戏按钮未点击。

Loading：对迁移后的 GamePages.EUILoadingPage 调用 BeginCurtain（0.1s 入/出，SkipFakeProgress/SkipCustomTransition），实测 Covering→Covered→Revealing→Revealed，运行实例名 EUILoadingPage。截图覆盖完成时主菜单内容不可见，GM FPS 依旧位于其 FreePage 排序层；没有改变既有层级。

截图：main.png、settings.png、gm.png、loading.png、ownership-1.png。ownership.png 为最初示例定位与原导航重叠的观察，已通过 Prefab 布局调整解决；最终以 ownership-1.png 为准。示例仅展示所有权文字，不充当 S2 默认组件库。主界面/设置旧视觉与文本换行保持 S0，本轮未改美术。

测试记录：

- 8b93d6e654b248b7b10db59b4aa9cbd7：初批 51/51。
- f83cbe3e82e949ceb047fbebdfdc10eb：加入槽位用例后 52/52。
- 36576b91ba8d4d6f95c543f1400e7159：所有权释放修补后，EUIContractEditTests + EUIDevelopmentCenterEditTests + EUITransitionBlockPreviewTests，52/52，5.275s。
- c28f83ace37f429cb744858abec73a44：契约描述补充后，EUIContractEditTests 7/7，4.126s。

发生过一个 Unity 6000.5 API 编译错误（objectReferenceInstanceIDValue 已废弃），已按实际反射使用 EntityId 并保留低版本条件分支；随后重新编译成功。曾出现目标实例离线，只剩 Unity Farm；一次补测误路由，job=6409f0bf764f42e389154efb0ad064ab，实际 0 用例，不计入验收、未改其源码。目标在第一次 30 秒间隔重试恢复，且用户确认已打开；重新核对 projectRoot 后恢复工作。未盲目重放未知写入。

本轮不是消费部署验收；未运行全量测试、全分辨率/全部字体皮肤、完整 ToggleGroup 动态增删、全部 Image 帧动画或目标平台打包。S2/S3 按其具体变化补验。最终编译、补测及模板普通保存见 validation.json。

最终资源摘要补充后：fd249ffc2c1b4d3f89cbefd9d40a1667，契约相关 7/7，4.3177s。最终 Console error/warning=0；base 普通保存 399 文件，hash d5621e6ed041f733d54e76c3cb5c80c5，版本仍 0.8.4。
