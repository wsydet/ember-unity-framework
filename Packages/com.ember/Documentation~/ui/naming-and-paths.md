# UI 命名、路径与图片分类规范

适用于 Ember 框架、base、玩法模板和消费项目的新增 UI，以及明确授权整理的既有 UI。本文是 UI 命名、目录和图片分类的统一来源；业务归属见[业务目录规范](../maintenance/business-directory-layout.md)，制作 API 见 [BUI 制作](bui-maker.md)，绑定与实例所有权见 [EUI 契约](eui-contract.md)。

本文新增的图片分类是制作和审查约定。当前工具没有完整的分类自动迁移、图集自动归属或尺寸预算强制检查能力；文档分类不能作为这些能力已实现的依据。历史资产按原引用继续工作，迁移规则见文末。

## 1. 目录与模块归属

默认代码根为 `Assets/Game/UI/Runtime/`，默认 UI 资源根为 `Assets/GameResource/Resources/UI/`。下表的资源路径均相对该 UI 资源根；项目采用其他加载方案时，以真实生成配置和资源 Provider 为准，仍保持模块及用途归属。

| 内容 | 标准位置 |
| --- | --- |
| UI 用户逻辑及生成 Binding | `Assets/Game/UI/Runtime/Module/<模块>/`，可按需细分 `Page/`、`Component/` |
| UI 编辑工具 | `Assets/Game/UI/Editor/<模块>/` |
| 模块 UI Prefab | `Module/<模块>/Prefabs/` |
| 模块图片 | `Module/<模块>/Atlas/`、`StaticTexture/`、`DynamicTexture/`，按第 3 节选用 |
| 模块配套动画、材质、特效、视频 | 模块内按需使用 `Animator/`、`Material/`、`EffectTexture/`、`EffectPrefab/`、`Video/` |
| 通用图片 | `Common/Atlas/`、`Common/StaticTexture/`、`Common/DynamicTexture/`，可继续按用途分组 |
| 共用 Prefab、字体 | `Common/Prefabs/`、`Common/Fonts/` |
| 编辑器生成配置 | `Assets/Ember/Editor/SOs/EmberCSharpImplementation.asset` |
| 页面注册 | 默认 `Assets/Game/UI/GamePages.cs` 与 `GamePages.User.cs`，以配置和生成结果为准 |

模块名按职责使用 PascalCase，例如 `Inventory`、`Narrative`，与业务模块归属一致。`Runtime`、`Module` 必须保留，不用模板名称混装各类资源，也不创建无用途的空目录。

所有正式 Page、Item、Prefab 骨架、Binding 与注册均通过 `Ember/UI/UI 开发中心` 创建和生成。`classPath` 相对代码根，例如 `Module/Inventory/Page`，不含文件名；Business 模式按模块路由资源，Framework 模式沿现有生成器路由到 Common。不能为符合文档示意手改生成器结果或切换既有资产的代码模式。

生成配置中的 `codePath`、`pageDefFile`、`uiResourceRoot` 及模板引用决定实际输出。Business 兼容 `Inventory/Page` 与 `Module/Inventory/Page`，都路由到 `UI/Module/Inventory/Prefabs/`，不会重复添加 Module；新业务代码统一采用保留 Module 的写法。GamePages.cs 的框架区由生成器维护，GamePages.User.cs 承接用户注册；旧演示代码目录保持兼容，不作为新业务目录示例。

模块模板以当前 `Module/模板` 的真实内容为准。新增分类目录不会因本文件自动进入模板或传播到现有模块；需要统一初始化目录时另走中心的模块模板预览和同步流程。

## 2. Prefab、类与绑定命名

新增业务 UI 推荐使用 `EUI<模块><功能><角色后缀>`，PascalCase，不以大小写区分两个资源；例如 `EUIInventoryMainPage`、`EUIInventorySlotItem`。已有 `EUIMainPage` 等公共名称继续保留，不因推荐格式批量改名。

| 角色 | 名称与行为 |
| --- | --- |
| Page，含主页面、Popup、FullScreenPopup、SubPage 等 | 使用 `Page` 后缀，例如 `EUIInventoryDetailsPage`；实际种类由 Binding 的 `PageType` 表达 |
| Item，含固定子逻辑、列表项和可复用逻辑组件 | 使用 `Item` 后缀，例如 `EUIInventorySlotItem`；角色为 `EUIBindingRole.Item`，不生成 PageDef、不进入 GamePages |
| 纯结构 Prefab、骨架、公共库组件 | 使用准确的用途名，沿公共库真实条目及 StructureOnly 配置；不凭 `Component`、`Embed` 等名称推断逻辑角色 |

Page 的 Prefab 文件基础名、根节点名、PrefabName、PageName 和类名使用同一名称。新增 Page 不使用 `Panel`、`Popup` 等替代角色后缀；弹窗仍命名为 Page。当前创建器明确拒绝 Page 角色的 `Panel` 后缀，但没有完整强制推荐名称格式；审查时仍需核对一致性。内部布局容器可以继续叫 Panel。

`EUICreationRequest` 默认名称为 `NewPage`，正式创建时替换为实际用途名。已有特殊名称仍可由既有 API 读取，不因本规范自动改名。

Item 的 Prefab、根节点和逻辑类也应同名；固定或动态实例通过各自绑定名区分，不为同一组件的多个实例复制 Prefab。类名必须是有效 C# 标识符，资源名不得包含路径、非法字符或系统保留名。不要搬用其他项目的固定长度限制，也不要用 `New`、`V2` 临时绕过未解决的类名或资源冲突。

`.Binding.cs`、EmberManaged 区域和注册内容由中心生成；修改绑定后重新生成，用户逻辑写在用户文件或用户区。实际字段名称及类型以生成结果为准。

### 绑定节点

自动收集识别 `m_` 或 `m` 后接大写字母的节点。新节点统一推荐 `m_<类型缩写>_<用途>`；例如节点 `m_Btn_Close` 对应变量 `Btn_Close`，以实际 Binding 为准。装饰节点不用此前缀，或通过 `EUIBindingExclude` 排除。绑定名在所属 Binding 内唯一；嵌套 Binding 是子逻辑边界，不重复收集其私有节点。

| 控件 | 推荐节点前缀 |
| --- | --- |
| Button / EUIButtonEx | `m_Btn_` / `m_EUIBtn_` |
| Toggle / EUIToggleEx | `m_Tgl_` / `m_EUITgl_` |
| Text / TMP | `m_Txt_` |
| Image / EUIImageEx | `m_Img_` / `m_EUIImg_` |
| InputField | `m_Inp_` |
| Slider / ProgressBar | `m_Pgb_` |
| ToggleGroup / ScrollRect | `m_Tgp_` / `m_Scr_` |
| Canvas / RawImage | `m_Cvs_` / `m_Raw_` |
| CanvasGroup | `m_Cg_` |

RectTransform、自定义组件等按真实完整类型登记 Extension，不凭前缀推断类型。UIContainer/TabLoader 仍属于保留但未完整启用的类型，不因可命名就作为正式可用容器；所有权及边界见 [EUI 契约](eui-contract.md)。

历史节点中的 `m_Ctn_`、`m_Tab_` 分别对应 UIContainer、TabLoader 的保留标记，仅用于识别和迁移核查。

## 3. 图片分类与选择顺序

先确认图片由 Prefab 固定引用还是由业务按数据选择，再确认是否适合集合加载和图集打包。运行时加载一个 Prefab 并不意味着它的固定配图属于 DynamicTexture；Image/RawImage 也不能单独决定目录分类。

| 分类 | 何时使用 | 常见例子 |
| --- | --- | --- |
| `Atlas` | Prefab 固定引用的小图，适合随所属 UI 一起加载和显示，作为图集候选 | 按钮底、边框、页签、固定装饰、九宫底图 |
| `StaticTexture` | Prefab 固定引用、需要独立存放的图，通常为大图或不适合集合打包的图片 | 整屏背景、固定插画、大装饰图 |
| `DynamicTexture` | 实际内容由业务根据 ID、配置或运行时地址选择，不把整套内容写成 Prefab 静态依赖；图片可大可小 | 装备图标、角色头像、按剧情选择的立绘和背景、活动配置图 |

归类例子：固定背景放 `StaticTexture`；随剧情 ID 切换的背景放 `DynamicTexture`；按钮九宫底图放 `Atlas`。背景、立绘仍归使用它们的业务模块，不因内容类型自动放进 Common。

`Common` 只收职责明确的公共素材。同一套通用控件图标可以整体放 Common，不必因某张图暂时只有一个消费者而拆散；组件私有图片保留模块归属。需要复用时引用准确的现有 Sprite/Texture，避免在多个模块复制相同图片。

### 尺寸、图集与特殊贴图

按最终导出 PNG 的实际宽高评估，先检查九宫、透明留边、拆分和已有公共图复用。尚无项目预算时，建议以 **512×512** 作为 Atlas 单图复核起点；任一轴超过就记录原因，优先优化或按用途改为 StaticTexture/DynamicTexture。这个数值是新增审查建议，不是 Unity 限制，也不是当前工具已配置的硬阈值；项目有明确预算时按项目预算执行。

`Atlas/` 是素材目录，放进去不代表已创建 SpriteAtlas 或完成打包。需单独核对 SpriteAtlas 的收录范围、重叠打包、平台设置及实际依赖；大图不因缩略预览或导入降采样变小就自动适合入集。DynamicTexture 是否另按内容组打图集由真实加载粒度决定，不把整个动态图目录默认收进一个常驻图集。

TMP/SDF 字体图集、Shader 数据纹理、特效纹理、RenderTexture、视频和制作源图按真实用途单独管理，不套普通 UI 图片的分类、裁边或压缩规则。StaticTexture 和 DynamicTexture 都可使用 Sprite 或 Texture2D，选择取决于真实消费者；不能仅凭目录批量改导入类型。

## 4. 图片命名、地址与动态加载

新增图片推荐 `<模块>_<功能或内容ID>_<用途>[_<状态>].png`，例如 `Inventory_Main_ButtonBg_Normal.png`、`Inventory_Item1001_Icon.png`。公共图以 `Common_` 开头；用途和状态保持明确。已有名称和外部导出前缀继续按真实来源处理，不在导入时擅自二次改名。

文件路径、GUID 和运行时地址分别记录：GUID 标识 Unity 资产，地址由实际加载后端生成，二者不能替代。默认 Resources 使用相对 Resources 根、不含扩展名的路径，例如 `UI/Module/Inventory/DynamicTexture/Inventory_Item1001_Icon`；切换到其他后端时依据其真实寻址规则，不假定同名文件必然冲突或必然隔离。页面调用优先用生成的 GamePages 定义。

动态图片交接至少记录模块、类别、来源（已有资源/本次导出）、准确路径或地址、消费者类型、加载与释放所有者、缺图策略。Prefab 可以保留明确的通用占位，但不能把所有可选内容图预先挂成静态依赖后称为动态加载。异步回调应核对当前数据身份及借用状态，防止 Item 复用后旧图片覆盖新数据；关闭、回池和销毁按实际资源所有权释放，参见[动态列表](dynamic-lists.md)。

## 5. PSD/BUI 与工具边界

PSD 标志属于上游导出协议，Prefab 文件名和 EUI 角色属于本项目契约。BUI Target、GeneratedTarget、ExternalPrefab 等必须映射到真实资产、节点和角色；不能直接把其他项目的 `@PrefabRef_`、`Component`、`Embed` 或切图前缀当作 Ember 已支持的命名协议。

当前 EmberBuiService 按请求 `imagePath` 导入、按 BUI 精确引用校验，不根据这三类目录自动规划资源。其图片导入设置和支持能力以 [BUI 制作](bui-maker.md) 为准。动态 Icon 的实际内容由业务更新；新增动态图素材需明确制作源、是否导出 PNG、目标地址及接线，不以“已有动态占位节点”代替素材制作已完成。

## 6. 既有资产与迁移检查

需要对消费项目执行本节整理时，可使用通用技能 `ember-ui-normalize`（UI 规范整理）。框架发布并提供对应技能后，通过 UPM Manager 的 AI Skill 安装/更新；技能整理当前项目自有内容，不要求重新部署业务模板。

已有 `Textures/` 等目录保留来源语义；例如 Common 下的默认背景属于固定独立图片，但不因本规范发布自动移动。整理前逐项确认用途、当前路径/GUID、Prefab/场景引用、动态地址、配置、BUI 来源和生成输出。既有动态图放在 Atlas 中也不能仅为目录一致而立即搬迁。

确需迁移时，通过 Unity AssetDatabase 移动并保留 `.meta` 和 GUID；同步代码及配置中的路径、页面注册和上游制作记录。GUID 保留不保证 Resources 字符串地址不变，须明确兼容或调用方更新。变更绑定通过中心生成，不手改生成文件。模板内容先改 Assets，再走正式项目中心保存；普通保存、Bump、发布和消费升级分别处理。

交付时核对：命名与角色一致、代码和资源归属一致、分类和加载方式一致、引用及地址可解析、动态所有权明确。涉及代码或资产时按[Unity 验证规范](../maintenance/unity-validation.md)确认本轮编译及相关验证；纯文档整理仅做文档与链接检查，不声称已迁移资源、完成图集打包或签收运行结果。
