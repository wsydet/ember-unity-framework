# Ember Framework 0.12.3 发布说明

发布日期：2026-09-11。新不可变 tag：`v0.12.3`。归属：**框架升级**。

## 根因与恢复范围

旧 `.gitattributes` 的 `Packages/com.ember/SharedAssets/** -filter -diff -merge text` 虽关闭 LFS，却强制把所有共享资源按文本入库。TTF 内合法的 CRLF 字节被转为 LF，偏移和表校验和被破坏；不是 SDF 图集容量、源字体 GUID 或 ugui 版本问题。

修改前已按原始字节备份本地两份完整字体。审计覆盖 SharedAssets 全部 17 个文件：2 个 TTF、1 个文本 SDF asset、10 个 meta、4 个 TXT。两份 TTF 均受影响；旧 blob 与原件的 CRLF→LF 结果完全相同。其他 15 个文件为文本；现有本地 SDF 用户改动已备份并保留，不纳入发布。

| 文件 | v0.12.2 长度 | 恢复长度 | 丢失字节 |
|---|---:|---:|---:|
| `SharedAssets/Fonts/钉钉进步体/DingTalk-JinBuTi.ttf` | 2,128,837 | 2,129,136 | 299 |
| `SharedAssets/Fonts/阿里妈妈东方大楷/AlimamaDongFangDaKai-Regular.ttf` | 5,247,241 | 5,251,184 | 3,943 |

恢复后 SHA256：

```text
DingTalk-JinBuTi.ttf
6F6A1E15F33D559FF3D0D7DCC8BD9AB25A6DBD8E9BCD29865DAF7615A17EA284
AlimamaDongFangDaKai-Regular.ttf
043CEBA922A6EE7A376687F36D27E3D96EB2B6E0784CC32826D8B062E9038214
```

旧钉钉字体 SHA256 为 `60623730FC009E3F0171DA8FC50C6705B11B9249D3C7C6A1A95056F2EC99CA1E`，旧东方大楷为 `D48B457659D91871DC1D28243D0BC58464715610E22AC5176C68A96830F9324B`。

默认共享规则现为 `-filter -diff -merge -text`；meta/TXT 明确按文本处理，asset 使用 `text=auto` 保留文本 YAML 的合并与 diff。没有重新生成 SDF、修改 GUID、扩大图集或隐藏警告。

## 静态回归

`scripts/shared-binaries.json` 保存经过验证的二进制原件长度和 SHA256；`scripts/check-shared-binaries.py` 仅依赖 Python 标准库，使用 subprocess 原始 bytes 读取 Git blob，禁止用 PowerShell 文本管道导出二进制。

```text
python scripts/check-shared-binaries.py --revision : --checkout
python scripts/check-shared-binaries.py --revision v0.12.3 --checkout
python scripts/check-shared-binaries.py --revision v0.12.3 --package-root "<UnityFarm 实际解析的 com.ember 目录>"
```

- 检查完整二进制清单，新增或漏记二进制会失败；检查工作区及指定 Git tree 的有效 attributes。
- 比较工作区、暂存或提交 blob、core.autocrlf=false/true 两种干净检出的长度及 SHA256。临时 index/检出目录不接触用户 index 或消费端 PackageCache。
- TTF 检查表边界、每张表与整个字体的校验和、loca/glyf 字形偏移、目标中文 Unicode cmap。
- 本次原件另经 Pillow/FreeType 实际读取并栅格化目标文字；这不是 Unity/TMP 渲染验证。
- 负向验证：v0.12.2 钉钉字体因 cmap 表校验和失败被拒绝，东方大楷因 DSIG 表越界被拒绝。
- CI 在 push、PR 与 tag 上执行静态检查。远端 CI 状态必须单独读取，不能由本地通过推断。

不要从未经验证的检出重新生成清单以消除失败。新增/更换二进制时应先验证原件，再显式更新受审查的清单。

## 发布与消费边界

版本、CHANGELOG、`Dependencies~/release-0.12.3.json` 与 `manifest-0.12.3.json` 对齐；第三方依赖不变。模板仍为 `base 0.6.0` 与 `source3d-2p5d 0.3.1`，保持原内容、hash、ParentSnapshot 与 0.12.0 兼容声明，**不重新部署模板**。

UnityFarm 只更新现有 manifest 的 com.ember URL，以及 packages-lock 内同一依赖的 URL/commit hash。保留此前所有用户修改与未跟踪文件，包括 GameplayScene、FarmM1、EUI 美术和生成 Binding；不进入 P4，不修改农场规则，不写 PackageCache。

manifest/lock 修改成功只表示依赖声明升级。最终安装必须由 Unity Package Manager 解析后确认实际包版本与文件 SHA256；如果没有解析，仍按“UPM 实际安装待完成”记录，不将旧缓存哈希冒充新包结果。

安装地址：`https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.12.3`。

## Unity 自动验证与手动验收

当前 Unity MCP 未连接或不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

1. 在 UnityFarm 的 Package Manager 解析 com.ember v0.12.3，核对实际 package.json 为 0.12.3，并运行上述 `--package-root` 检查。不要编辑缓存或重新部署模板。
2. 手动触发编译，确认 Console 没有编译/字体导入错误。必要时对已更新的源字体执行 Reimport，再次编译；不替换字体或更改图集配置。
3. 从 **FrameworkScene** 进入农场，检查六个正式 **SceneUI** 标记；按游戏状态覆盖“主控中心、无人机、待命、小麦、胡萝卜、成熟、锁定田、解锁、水井、不消耗水”，确认中文正常且没有缺字警告，特别是“定”(U+5B9A) 与“田”(U+7530)。
4. 保留截图/Console 与实际安装版本作为验收证据。静态字体检查、Unity 编译、运行时人工验收分别记录；本说明不宣称后两项已通过。
