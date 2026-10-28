# Ember 0.24.3

2026-10-28。基于0.24.2的框架补丁，交付B03运行时加载恢复和共享动态字体的项目可写资源机制。**退出0xC0000005仍未修复，不属于本版已解决项。**

## 改动

- `EmberTerrainRuntimeLifecycle` 在 BeforeSceneLoad 幂等订阅 sceneLoaded，仅对本次加载场景中含 EmberTerrainTile 的 Tilemap 刷新一次，在 Start／首次渲染前恢复临时 Sprite。无需消费端GameInit／逐关卡接线，不增加每帧扫描；保留原规则、UV／顶点、旋转镜像、Grid、Anchor、普通Tile、Prefab来源及碰撞。
- 字体皮肤创建／保存调用 Editor-only `EmberWritableFonts.EnsureProjectFonts`，按源GUID共享项目字体，并隔离Atlas、材质、fallback和字重。`MakePrefabsWritable(folder)` 提供带备份的定向引用迁移，保持组件FileID、Binding、输入框引用和材质参数。TMPEx在TMP准备字形前选择映射字体。
- 四份包源保留TTF、GUID和动态能力，关闭源上的ClearDynamicDataOnBuild。相对0.24.2还包含工作区已有的动态缓存归零结果：清除字形缓存／额外Atlas，保留配方和原始字体；不是删除字体字符覆盖。原始TTF与许可证的二进制门禁独立验证。
- 更新共享字体静态门禁：四源必须保持Dynamic且不参与构建清空；继续核对Multi Atlas、源GUID、fallback、材质和许可证。项目生成资源仍启用TMP构建清理。

## 验证与限制

发布准备通过框架Unity MCP主动编译，确认本轮编译完成、真实域重载和已加载程序集；无编译失败。Console保留6条空Editor程序集警告和2条MCP重连警告，不声称零警告。

本轮三组定向EditMode **9/9通过**。首次使用短类名的过滤实际匹配0例，已保留失败选择回执，不计通过；改用完整命名空间后实际执行9例。

旧兼容测试仍直接使用未迁移的默认共享源，运行后使三份源各有2个字形并落盘；这是未迁移路径的已知边界，不能当源保护通过。已备份该状态，并从前轮保护基线逐字节恢复四源，校验预期SHA256后通过MCP重新导入。随后仅重跑项目可写字体用例 **2/2通过**，四源最终磁盘哈希与保护基线完全一致，Font／Atlas／Material dirty均另查验。未将测试产生的缓存混入发布。

与本次发布的运行代码相同的前轮验证证据继续适用：同一打开Variant和混合图连续两次真实域重载；Windows64 Development D3D11构建0错误0警告；首次及卸载再加载共36次占格Sprite采样无null；实际画面、三套中文及Symbols2 fallback、Editor Play/Stop、字体资源重载与源资产保护均有回执。本次发布准备没有重新构建Player，不能把旧构建标成0.24.3新构建。

五个模板实际内容hash与封存metadata一致，四个派生ParentSnapshot均匹配base0.10.0，模板图问题为空；platformer-2d仍0.13.0，其余模板、0.24.0兼容声明、第三方依赖和技能bundle不变。不包含工作区其他FSM／UI制作／模板约束修改。

SpriteAtlas打包、自定义加载器在sceneLoaded之后另行反序列化Prefab／AssetBundle、大图耗时及Windward安装本版后的正式产品验收未覆盖。不改变消费项目SpritePackerMode。

## 必须完成的消费迁移

1. 通过正式 `Ember/UPM Manager` 取得0.24.3；本次发布不替Windward执行升级或部署。
2. 在字体皮肤窗口保存生成项目库；默认字体位置为 `Assets/GameResource/Resources/UI/Common/Fonts/EmberGenerated/<源GUID>/`。审查皮肤ID、字体映射及共享fallback。
3. 对实际UI Prefab目录调用 `EmberWritableFonts.MakePrefabsWritable`，审阅备份与diff。场景内固定文本、TMP Settings全局fallback和自定义字体调用方另行定向迁移。只升级代码／关闭源构建清空不足以消除旧工程的包资产改写。
4. 保持活动模板部署身份，无需全量重部署。分别复验首次／重入规则格可见、真实Variant、动态字形、Editor Play/Stop、资源重载、Windows构建源资产哈希与dirty状态。
5. 真实输入、机关、返回菜单和单一正常退出各自验收；**退出AV仍是独立阻断**。

## 已知退出故障

原生栈确认：无障碍 `StaticDestroy` 在ScreenManager全局指针清零后再次查询窗口，RAX=0读取0x138，在UnityPlayer.dll RVA0x1dd2e9发生AV。该地址有窗口查询／GPUProfiler同址符号，不能仅凭名称归因RenderDoc。现有空闲框架Player无调试器成对运行：无主动UIA查询2次退出0，单次查询后2次AV；查询进程在关闭前已结束。退出0只作为对照。

证据支持这一版本／二进制中的稳定原生故障路径，未追溯全部历史触发客户端，也没有验证完全去框架的纯Unity工程或其他引擎版本。本版不提供未证实的引擎／环境归责、强制退出或禁用无障碍的绕过方案。

详见[三项独立实施记录](../implementation/b03-player-exit-font-20261028.md)、[原生退出调查](../implementation/exit-native-20261028.md)、[发布门禁](../implementation/evidence/release-0.24.3/README.md)、[字体迁移API](../../Packages/com.ember/Documentation~/manual/font-skins.md)与[B03生命周期](../../Packages/com.ember/Documentation~/maintenance/terrain-cache-b03.md)。原生dump保留本机，发布提交只含栈、校验摘要、日志及复现工具。
