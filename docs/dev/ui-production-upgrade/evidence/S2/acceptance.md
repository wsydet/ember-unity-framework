# S2 实际验收

工程 C:/Users/wuyu/My/ember-unity-framework，Unity 6000.5.4f1，目标 ember-unity-framework@b23e9b768e85ed30。活动 base 与 S1 交接一致；没有再次加载模板。HEAD 为 5119adc2c142295f0367fde05c4b01d25b672c91。版本仍 base0.8.4 / framework0.22.0。

## 已实现

公共库配置/服务/面板位于 Packages/com.ember/UIExtension/Editor/Pages/EUILibrary*.cs；创建服务增加真实 Variant、结构源、内容插槽与可选 Binding 继承。绑定验证新增继承链、字段缺失/类型/角色检查，生成器拒绝失效继承。S1 契约增加 libraryEntries 和 library-contract 差异。公开入口和调用规则见包内 ui/prefab-library.md。

8 个库条目及 10 个新 Prefab 的实际 GUID、Variant 源和 Binding 配置见 [assets.json](assets.json)。两个结构 Page、四个基础组件放 Common/Prefabs；OptionRow、OptionRowVariant 和两个预览 Page 放 Module/UIFoundation/Prefabs。结构源不注册业务页面、不查找不存在的逻辑类。真实 Item Variant 继承生成类，字段没有重复声明。

创建和再生成使用 EUI 开发中心公开服务。主菜单两个按钮改为嵌套 ActionButton，根 GUID、Framework 模式、IsFramework、Button 声明类型、绑定职责与原用户代码保持。用户文件 SHA256 见 [main-user-code.json](main-user-code.json)。六个 S1 页面 GUID 均保持。原有大批模板切换删除未批量恢复。

## 已验证

| 编号 | 结果与证据 |
| --- | --- |
| L01 | PreviewPage / PopupPage 保存后 PrefabAssetType=Variant，源分别为 StandardPage/StandardPopupPage。注册为 MainPage/Popup，实际可打开。assets.json 与契约记录真实来源。 |
| L02 | 同一个源创建两个临时实例，通过 ResolveInstanceSlot 分别改文字和颜色，结果独立；源文字仍 Continue、源 Prefab 字节未改。见 independent-overrides.json；相关测试同时覆盖。 |
| L03 | OptionRowVariant 具有真实资源源和 baseBindingUUID，生成 Binding 继承 EUIOptionRowItem；通过 EUIItemFactory 创建后 SetContent/IsSelected/Dispose 成功。继承缺失字段和循环、结构源不可作类基类有专项测试。 |
| L04 | 重复ID、缺失槽位、错误类型、跨边界槽位与过期目录专项通过；正式配置 Validate=[]，目录摘要一致。自动匹配默认关闭，精确引用须验证。 |
| L05 | 实际 Play：Main→Settings（当前场景主界面）→Main→Gameplay→Settings（当前场景玩法中）→Gameplay→Main，转场完成；两个按钮行为保持。main-final.png。 |
| L06 | 配置、目录、所有默认 Prefab、图片、TTF、字体目录处于模板管理范围。普通保存结果见 template-save.json；默认 LoadDefault 是可携带的初始化入口。未做干净消费项目部署，不把保存等同部署验证。 |
| L07 | 实际预览包括页面、弹窗、主/次/禁用按钮、选择/禁用 Toggle、进度、空态、滚动行。最终 preview-final.png / popup-final.png；最后为列表行补图标和右侧状态并实际查看。 |
| L08 | 两张 imagegen 正式图已导入，透明度/尺寸/提示词/导入设置在 materials.md 与 image-inspection.json。基础形状和 TMP 独立可编辑。 |
| L09 | 2560×1440、1024×768 实际 GameView；长文本换两行、圆角缩放正常；PointerDown 显示按下，disabled 点击计数0；Toggle驱动0%/100%；滚动位置0显示后排。见 pressed-zero-scroll-1024x768.png、preview-1024x768.png、interaction.json。触摸无需 hover 才可操作，但未做实体触屏测试。 |

相关 EditMode 56/56，job 360078f706604c7eb952624ebf196901，13.9836762秒；仅 EUIDevelopmentCenterEditTests、EUIContractEditTests、EUILibraryEditTests。原始结果 [tests.json](tests.json)。最后代码编译 started1793013404725、finished1793013407962、domainReloadAfter1793013411822；此后只有资产/文档修改，没有遗漏代码编译。最终状态和 Console 见 validation.json。

弹窗 Close 与 mask 点击均真实派发 PointerClick，关闭后对象消失。实际 Raycast 首项为 EmberBgMask，次项底层 Primary，见 raycast.json。事件派发不是人工鼠标/触屏试玩；ScrollRect 验证设置滚动位置，不冒充实际拖拽手势。

四份最终契约连续导出 diff=0；Main/Preview/Popup/Variant 的 libraryEntries 分别1/6/1/2。正式源由 Prefab 与配置维护，导出 JSON 不反写资产。EUI 开发中心已实际打开 Library tab，读取8条目、验证及摘要一致。

## 修复过的问题与未验证边界

首轮构建脚本出现 VerticalFit 大小写、继承结构体判空与缺少 Linq 编译问题，已修复后重新编译/测试。最初预览使用 FreePage 导致排序盖住 Popup，已改为 MainPage；最初 ClosePageByDef 不关闭 Popup，已改为 ClosePage(Page) 并重验。初始斜体不适合浅色库，已复用 Liberation Sans 动态字体。

preview-initial.png、popup-initial.png、states-zero-scroll.png 为历史失败/中间状态，不作最终通过图。preview-neutral.png 为中间字体调整图；preview-final.png 是最终图。4:3 图拍于列表图标补齐之前，布局/交互尺寸未随后改变。临时 GameView 1024×768 尺寸已移除、原 QHD 索引恢复；退出 Play，场景未保存运行期对象。

未验证：真机触摸/键盘辅助导航、竖屏/刘海安全区实机、干净消费部署/构建、完整多语言/CJK 视觉、派生模板合并、所有异常依赖图组合。EnableState 与 interactable 未合并。S3 池/完整容器、S4 PSD/UXP、S5 BUI、S6 完整换肤、S7 ui-game 均未实现。

保护位置 `.utmp/ui-production-S2-20261026/`：UIExtension、GameUI、CommonPrefabs 为完成的定向副本；全 Assets 复制曾中断，Assets 子目录仅部分备份，不能当完整恢复源。恢复时按本轮实际文件选择，不批量回退其他会话。

Burner 工程仅作只读公共库/槽位参考，没有复制产品业务或定制依赖。默认资源由 base 持有，通用能力在框架包。未提交/推送/Bump/发布，未改派生模板或消费项目。
