# 业务代码与资源目录规范

适用于框架的基础模板、玩法模板和使用框架的业务项目。新增功能先明确所属模块，再按下面的目录归档；模板名称不作为代码、UI、图片、配置和音频的混合收纳目录。

## 三个主要归属

| 内容 | 标准位置 | 说明 |
| --- | --- | --- |
| 主要业务代码 | `Assets/Game/Module/<模块名>/` | 模块入口、业务规则、运行状态、数据类型及业务服务 |
| UI 逻辑 | `Assets/Game/UI/Runtime/Module/<模块名>/` | 页面、Item、Binding 及 UI 协调逻辑；`Runtime` 是必需层级，由 UI 中心生成 |
| 业务资源 | `Assets/GameResource/<资源类别及所属模块>/` | 根据用途放到对应位置，加载目录遵守实际资源方案 |

模块名按职责命名，如 `Narrative`、`NovelSave`、`Inventory`。UI 与业务模块使用相同的归属名称，便于从功能找到代码及资源；模块内部按需要细分，避免建立大量空目录。业务规则留在业务模块中，UI 负责展示和交互。

框架通用实现仍归 `Packages/com.ember/<子系统>/`。项目启动、状态切换、场景和集中配表等已有公共接线继续使用各自专门目录，例如 `Game/State`、`Game/Scenes`、`Game/Table`，不因引用某模块就迁入该模块。

## UI 必须通过 UI 中心生成

**所有正式 UI 的创建及代码生成必须通过 UI 中心完成。** 页面、弹窗、Item、Prefab 骨架、Binding 和配套注册信息均使用现有 UI 中心（EUI 开发中心）的创建、绑定及生成流程。

先在 UI 中心选择正确的模块归属，再按实际生成的目录和 Binding 编写用户逻辑。新增或调整控件绑定后，通过 UI 中心重新生成；不手工拼造生成文件、绑定字段或另起一套 UI 骨架绕开中心。

运行时 UI 的标准路径是 `Assets/Game/UI/Runtime/Module/<模块名>/`，`Runtime` 必须保留，不使用省略该层的 `Game/UI/Module` 路径。UI 编辑器工具放在 `Game/UI/Editor/<模块名>/`。通过 UI 中心生成后，Prefab 的 ClassPath、生成文件和页面注册应与实际生成结果保持一致。

## UI 与资源对应关系

当前 Resources 加载方案采用以下位置；路径大小写统一使用示例中的形式：

| 资源 | 位置 |
| --- | --- |
| 模块 UI Prefab | `Assets/GameResource/Resources/UI/Module/<模块名>/Prefabs/` |
| 模块 UI 专用图片 | `Assets/GameResource/Resources/UI/Module/<模块名>/Atlas/` |
| 模块 UI 动画、材质等 | 对应 UI 模块目录内的 `Animator/`、`Material/` 等目录 |
| 通用 UI 图片及可复用控件图标 | `Assets/GameResource/Resources/UI/Common/Atlas/` |
| 共用 UI Prefab、字体等 | `Assets/GameResource/Resources/UI/Common/Prefabs/`、`Fonts/` 等目录 |
| 模块配置及运行内容数据 | `Assets/GameResource/Resources/Config/<模块名>/` |
| 模块音频 | `Assets/GameResource/Resources/Audio/<模块名>/` |
| 集中配表源文件 | `Assets/GameResource/TableSources/` |
| 配表生成的运行数据 | 沿用配表系统声明的输出位置，例如 `Resources/Config/Tables/` |

图片跟随使用它的 UI 放入 `Atlas`。通用素材放 `Common/Atlas`，可以按用途或素材组继续细分，例如 `Common/Atlas/Novel`。同一套主对话框、阅读菜单的通用按钮图标可以整体放 Common，不必因为某张图目前只被一个按钮引用而拆散。剧情背景和人物立绘等内容图片仍归使用它们的模块 UI。

`Common` 只收纳职责明确的公共资源，不作为无法判断归属时的默认目录。不按图片文件名猜模块归属，应以实际用途为准；也不为多个 UI 各复制一份相同公共素材。

资源是否进入 `Resources` 由加载方案决定，不要求所有资源都进入 Resources。原始素材、编辑器数据和测试夹具不作为运行时加载内容混入其中；切换加载方案时，仍保持按资源类别、模块及 UI 归属组织。

## 编辑器、测试与生成代码

- 模块编辑器代码放 `Game/Module/<模块名>/Editor/`；UI 编辑工具放 `Game/UI/Editor/<模块名>/`，保持 Editor 程序集隔离。
- 模块测试放 `Game/Module/<模块名>/Tests/`，测试专用资产放其 `Fixtures/` 下；已经退出正式示例的剧情若仍承担回归覆盖，可以保留为夹具，移出运行资源目录。
- UI 用户逻辑与对应 Binding 生成文件放在 UI 中心生成的同一模块目录；生成内容由中心维护，用户逻辑写在对应用户文件或用户区。
- 配表的 Row、Definition、Binding/Catalog 和导出产物遵守配表系统已有规则；修改源数据后重新生成，不手工修改生成内容。

## 示例：视觉小说

```text
Assets/
├── Game/
│   ├── Module/
│   │   ├── Narrative/             # 剧情、运行器、编辑器、测试
│   │   └── NovelSave/             # 存档业务
│   └── UI/
│       ├── Runtime/Module/        # 运行时 UI；Runtime 必需，通过 UI 中心生成
│       │   ├── Narrative/         # 阅读页面及演出 Item
│       │   └── NovelSave/         # 存档页面及槽位 Item
│       └── Editor/Narrative/      # 阅读布局编辑器
└── GameResource/
    ├── TableSources/
    └── Resources/
        ├── Config/Narrative/
        ├── Audio/Narrative/
        └── UI/
            ├── Common/Atlas/Novel/
            ├── Common/Fonts/
            └── Module/
                ├── Narrative/    # Prefabs、Atlas 等
                └── NovelSave/    # Prefabs 等
```

## 现有目录与迁移

以上是新增内容及后续统一整理的归属标准。现有 `Assets/Game/UI/Runtime/Module` 符合规范，必须保留 `Runtime`；后续新增模块 UI 继续通过 UI 中心生成到该结构下。

整理已有内容时，成套调整实际文件与生成配置，不在两个位置各留一份同名脚本。UI 创建、绑定和重新生成仍必须走 UI 中心。通过 Unity 资源移动保留 `.meta` 和 GUID，并核对 Prefab/场景引用、页面注册、Resources 路径、配表、编辑器默认路径和测试。持久化数据中保存了路径的，还要明确兼容处理。

模板修改先落项目 `Assets`，再使用模板开发面板的保存流程；不得直接修改 `Templates~` 或 `ParentSnapshot~` 快照。普通保存、版本 Bump 和发布是独立步骤。新功能或目录迁移的完成检查应包含目录归属、引用与生成路径一致性。
