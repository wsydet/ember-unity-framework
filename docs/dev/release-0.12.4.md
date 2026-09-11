# Ember Framework 0.12.4 发布说明

日期：2026-09-11。新 tag：`v0.12.4`。归属：**框架升级**，不改模板、不重新部署 UnityFarm。

## 根因与最小修复

0.12.3 的源 TTF 二进制修复继续有效。钉钉 TTF 长度仍为 2,129,136，SHA256 仍为
`6F6A1E15F33D559FF3D0D7DCC8BD9AB25A6DBD8E9BCD29865DAF7615A17EA284`。

本次静态证据指向独立的容量限制：共享 SDF 使用 Dynamic、1024×1024、采样字号 90、padding 9，
但关闭 Multi Atlas Textures。UnityFarm 已累积 125 个字符，现有碎片无法容纳待加入的大汉字；
“解、水、井、不、消、耗”在源字体中存在，但不在当前图集中。125 是本次消费状态的观测值，不是固定容量上限。

本地 ugui 2.5.0 的 TMP 源码表明，单张图集装箱失败后，多图集开关控制是否调用 `SetupNewAtlasTexture`，
并把新字形指向新 atlasIndex；TMP 再为对应图集生成材质引用和 UGUI 子网格。
因此这次只将 `DingTalk-JinBuTi SDF.asset` 的 `m_IsMultiAtlasTexturesEnabled` 从 0 改为 1。
这是源码与配置支持的根因判断；本会话尚未取得 Unity 内容量复现或视觉验收结果。

保留源 TTF、SDF GUID `a32ba8ab7d4aa814e8b5f0a267b29b42`、源 GUID、材质/纹理 fileID、shader、
字号、padding 和样式。多图集会按需增加纹理与材质绘制开销，不预分配大量图集。
不换字体、不隐藏警告、不以清空已有图集作为修复，也不改农场规则或进入 P4。

## 改动与用户资源保护

- SDF 发布内容只包含一个布尔值的变化；本地原有清空动态缓存的用户改动已按字节备份并保留在工作区，单独暂存开关以避免混入发布。
- 增加 `SceneUI/Tests/PlayMode/SharedFontAtlasPlayModeTests.cs` 与 meta，补齐现有 PlayMode asmdef 的 TMP/UI/SceneUI Integration 引用。
- 增加 `scripts/check-shared-font-config.py` 并接入现有 CI；保留 `.gitattributes`、两份 TTF、二进制哈希清单和既有检查逻辑。
- 对齐 package.json、CHANGELOG、`Dependencies~/manifest-0.12.4.json`、`release-0.12.4.json` 和当前发布文档。
- 模板内容、版本、Hash、ParentSnapshot、第三方依赖均不变。
- UnityFarm 的 GameplayScene、FarmM1、Prefab 美术配置和 Binding 均不由本修复编辑，不写消费端 PackageCache。

## 回归设计与实际结果

### 源码静态检查

`python scripts/check-shared-font-config.py --revision HEAD` 检查工作区及发布 Git blob 的 Dynamic、多图集开关、
尺寸、字号、padding、GUID、源字体和材质引用。可追加 `--package-root "<实际解析的 com.ember 目录>"` 只读检查消费端。
暂存阶段使用 `--revision :`。

继续执行 `python scripts/check-shared-binaries.py --revision HEAD --checkout`，比较工作区、发布 blob 与
core.autocrlf=false/true 两种干净检出的长度、SHA256 和字体结构。静态检查不模拟 FontEngine 装箱或 GPU 渲染。

### Unity 自动测试（本会话未执行）

在框架工程 Test Runner → PlayMode 运行 `Ember.SceneUI.PlayModeTests.SharedFontAtlasPlayModeTests`：

1. `SharedFont_PreservesSourceStyleAndAllowsMultipleAtlases`：检查共享资产导入后的配置、GUID、源字体和材质纹理引用。
2. `Overflow_AllocatesAtlasAndPreservesMaterialsAcrossSceneUIPoolReuse`：从同一源字体和参数创建非持久化临时字体；先关闭多图集，最多尝试 512 个有效 CJK 字符，必须实际遇到单张装箱失败。
3. 不清空或缩放已满图集，在同一字体开启共享配置的多图集，再试同一个失败字符；断言 atlasTextureCount 至少 2、该字形 atlasIndex 为 1，并加入六条验收文字。
4. 用真实 `PrefabSceneUIViewHost`、EUI Item、`EmberSceneUIEngine` 和 TextMeshProUGUI 生成文字。核对每个可见字符所属字体、atlasIndex 对应纹理、shader/颜色/描边参数、TMP_SubMeshUI 活动状态及非空网格。
5. 六轮业务显隐/回收/再获取，确认子网格随 Item 隐藏、复用同一个 TMP 组件、重新显示正常，重复文字不再分配图集。
6. 销毁临时字体、材质、纹理、场景对象；逐字节比较共享 SDF 文件，保证压力测试不持久化动态图集缓存。

TestContext 输出单张失败时的字符数、失败码点、恢复后的图集数，保留测试 XML。这个数量必须来自实际运行，不能把静态估算写成成功结果。
首轮用户报告测试编译错误：`FontEngine.GetGlyphIndex` 不可用、`GetInstanceID` 被禁止；已改为公开的
`TryGetGlyphWithUnicodeValue` 和组件引用比较。修正后编译、单张溢出、材质/子网格和池复用结果仍待用户提供。

当前 Unity MCP 未连接或不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

## UnityFarm 升级与人工验收

必须通过 `Ember/UPM Manager` 选择 **v0.12.4** 升级。禁止直接修改 manifest 或 packages-lock；由升级器和 Unity Package Manager 更新，随后只读核对锁定提交、实际 package.json 和多图集开关。
若当前代理无法操作 Unity，应由用户执行该步骤。规则依据见 [升级规则 §7.1](../../Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md#71-仅框架变化)。

本会话不把准备好新 tag 当成消费升级成功。需要用户完成 UPM Manager 升级后再核对实际安装。

手动触发编译后，从 FrameworkScene 进入农场，按对应游戏状态验收六个正式 SceneUI 标记：

```text
主控中心
无人机 A · 待命
小麦 · 成熟
胡萝卜 · 成熟
锁定田 · 15g · M2 解锁
水井 · M1 不消耗水
```

检查全部中文与标点正常，Console 无缺字警告；重点确认“解、水、井、不、消、耗”。在容量测试已经产生第二张图集后，
检查字体 atlas 数量、对应材质纹理、TMP 子网格，反复切换显隐/离开与重返农场，确认无残留文字、材质丢失、样式变化或对象池复用异常。
通用临时资源测试不能替代用户正式 Prefab 美术、场景接线与肉眼显示验收。
