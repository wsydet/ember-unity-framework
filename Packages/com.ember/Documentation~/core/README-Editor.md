# 框架编辑器工具

| 目录 | 说明 |
|---|---|
| [Core/Editor](../../Core/Editor/README.md) | 项目中心、模板开发、场景映射、Build Settings 与 Play 场景管理 |
| [FrameworkTools/Editor](../../FrameworkTools/Editor) | 快速场景打开、Toolbar、Odin 集成检查 |
| [Basic/Editor](../../Basic/Editor) | 日志配置、资源批处理、编码检查等通用工具 |
| [UIExtension/Editor](../../UIExtension/Editor) | UI 开发中心、绑定生成、预制体维护与模块目录模板 |
| [SceneUI/Editor](../../SceneUI/Editor) | 运行时 SceneUI 诊断 |
| [UPMManager/Editor](../../UPMManager/Editor) | UPM 依赖检测与框架升级 |

当前入口为 `Ember/项目中心`、`Ember/UI/UI 开发中心`、`Ember/UPM Manager`。
项目配置仍写入 `Assets/Ember/Editor/SOs` 和 `Assets/Resources`，不写入只读的安装包。
具体文件、模板接口和配置路径见 [Core.Editor 说明](../../Core/Editor/README.md)。
