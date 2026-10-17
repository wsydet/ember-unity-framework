# 非增量构建警告维护

后续：本文发现的UniRx关闭域重载播放检测问题已另行修正，当前状态及实测见[播放生命周期记录](10-unirx-play-lifecycle.md)。本文保留当轮历史证据。

日期：2026-10-17。承接[18条警告复核](08-clean-build-acceptance.md)。不屏蔽诊断，不直接修改PackageCache，不Bump、提交、推送或发布。

## 归属与修正

| 警告 | 正确维护源 | 修正 |
|---|---|---|
| EmberSceneField._sceneAsset，CS0414 | Packages/com.ember/Core/Runtime/EmberSceneField.cs | SceneAsset选择字段及构造赋值仅在UNITY_EDITOR编译；Editor序列化字段名、引用、场景名处理不变 |
| EUITransitionBlock两个预设字段，CS0414 | Packages/com.ember/UIExtension/Runtime/Components/EUITransitionBlock.cs | 仅供Editor应用预设的选择字段归入UNITY_EDITOR；实际运行曲线、排布和时长仍序列化并参与Player |
| UniRx三处查找过时API，CS0618 | Packages/com.neuecc.unirx/Scripts/UnityEngineBridge/MainThreadDispatcher.cs | 单对象FindAnyObjectByType；集合无排序查找，保留默认活动对象范围；单例候选不依赖InstanceID顺序，清理仍遍历所有非当前实例 |
| Plaque隐式向量截断 | Packages/com.ember/UIExtension/Runtime/Resources/Plaque/Plaque.shader | 显式传vertex.xyz |
| NodeBlurEffect逗号表达式 | 同包Resources/UI-NodeBlurEffectShader.shader | 显式fixed4零向量构造 |
| ShowTextInstance八处有符号整数除/模性能提示 | 同包Resources/ShowText/ShowTextInstance.shader | 对2/4/16使用带负数偏置的移位与余数计算，保留向零截断；未将负数直接转uint |
| Feel两个可选PostProcessing资源缺失脚本 | Assets/ThirdParty/Feel及私有镜像com.moremountains.feel/Feel | 默认资源从PostProcessing/Resources移动至PostProcessing/Editor/Resources；唯一Editor使用方改为按GUID经AssetDatabase加载 |

MCP实测UniRx来源是Embedded，路径确在仓库Packages中且受Git管理；它不是注册表缓存。第三方私有镜像没有UniRx目录，不为本次警告另造包或改版本。局部维护说明见该包EmberMaintenance.md；其他工程使用OpenUPM 7.1.0不会自动获得修正。

Feel当前活动来源仍是Assets，私有镜像README明确其来源为该Assets目录，本轮同步相同改动到镜像，未切换安装方式。原资源、文件夹meta和GUID保留；未删除缺失组件。唯一调用位于UNITY_EDITOR && MM_POSTPROCESSING，资源不应作为运行时Resources打入Player。Prefab GUID仍为948e90df3bdde44068685e4ebecfcc25，Profile仍为c1bea42a5942a4c09a3ecd393f6054aa。

这不是模板玩法改动；第三方供应商内容不进入Ember包或platformer模板。保留所有原有修改。

## 验证范围

Unity编译完成后Console错误查询0；Shader.Find确认三个Shader实际路径均为上述维护源，ShaderUtil查询未返回诊断。Editor中EmberSceneField.FromAssetPath仍返回FrameworkScene正确名称和路径，两个预设选择字段及运行曲线字段仍存在；Feel Prefab GUID解析到新Editor目录。

ShowText位运算在CPU上以-1024至1024及int.MinValue/int.MaxValue，对除数2/4/16共6153组除法和余数对照通过。此项是算术语义验证，不冒充GPU视觉回归。旧ShowText运行组件当前为注释内容，未新增激活路径。

当前工程没有旧PostProcessing可选依赖，受MM_POSTPROCESSING条件控制的自动配置分支无法在本工程实操；资源迁移及GUID解析已核对，不能声称已验证安装该依赖后的编辑器自动配置。未安装可选包的Player构建与实际游戏流程由下述发行验证覆盖。

第一轮CleanBuildCache构建Succeeded、0错误、4警告、0 Assert（GUID a1f3f60cfce849fda1c7c1a96bc653fd）。原字段、Shader及Feel警告均消失，但替换使用的FindFirstObjectByType及InstanceID排序接口在Unity6000.5也已废弃。依据当前编辑器反射结果改为FindAnyObjectByType和无排序参数的FindObjectsByType；较早Unity6分支使用SortMode.None。没有关闭CS0618诊断。

临时空场景Play探针：2个活动Dispatcher经CullAll变为1个，投递回调执行，销毁后对象为0，Console错误查询0；原FrameworkScene随后恢复，未保存探针场景。既有ScenePlaybackDetector依赖DidReloadScripts设置播放标记，在本机关闭域重载的Play配置下Initialize提前返回，故探针不算发行初始化验证，且未将此既有独立问题混入本轮警告修正。发行行为以新Player进程为准。记录位于`.utmp/warnings-20261017/unirx-probe.txt`。

## 最终非增量构建及两进程

第二轮CleanBuildCache构建：**Succeeded，0错误、0警告**；原始诊断asserts=0 errors=0。GUID `02c640a1465045f5b790a7bb10a181d5`；Mono2x，options原始值128（CleanBuildCache）、Development=false，耗时1分32.461秒。原18条警告及中间替代API警告均未复现；历史原生Assert也未复现，不因此推断历史Assert的根因。

证据根`.utmp/warnings-20261017/final-player`：build-report.txt、build-verification.txt、build-entry-stack.txt、build-diagnostics.txt、build-messages.txt；第一轮4警告结果保留在同级player目录，不覆盖失败对照。

| 阶段 | 时间 | PID | boot | 断言 | 退出码 |
|---|---|---:|---|---:|---:|
| save | 15:42:41 | 207280 | `557acff0ce54457ebed8add1a870df6d` | 16/16 | 0 |
| resume | 15:43:13 | 24372 | `e2d0525427b947d4acfeeb79ba1014e5` | 19/19 | 0 |

两进程均PASSED、editor=false、development=false、errors=[]，GUID一致，前进程已退出再启动后进程。正式Main→A→B→组合保存退出、磁盘合法检查点续玩、项目/世界恢复、死亡一次提交、相机加载/恢复/卸载均通过。未改运行器或断言。

- evidence/report-save.json SHA256：`e4e56b119796fb63a7c427466924affd1ece8af719aa9ce32a1d54285edaacf7`。
- evidence/report-resume.json SHA256：`bf0a205242d67ce89fb5263647879570ad1a6319a9dbe178d8fe19fba29ffed3`。

验证范围是Windows64 Mono与当前白盒流程；不是IL2CPP/其他平台或三种旧Shader全部视觉效果的验收。Feel旧PostProcessing编辑器配置及UniRx关闭域重载的编辑器播放检测限制如上。

## 状态与消费边界

构建自动改写的URP预筛选、项目批处理设置与Odin两个NoEditor meta已恢复本轮基线；原Odin DLL/PDB修正及其他已有工作区修改保留。15:43:07项目中心检查0错误、0差异、1条已保存未封存提示。

本轮未改模板维护内容，不需要重复普通保存。platformer-2d仍0.4.1，最后普通保存15:27:48，contentHash仍`b0d2b8954453f5eefeb8f094854c24eb`；versionedContentHash仍`373437cccbe9f4ba6e93359da2c8b2fd`，父base0.8.3及parentContentHash `1270ce6dfcae3c01d050c09ecfd60337`不变。Ember实际仍0.19.2（本地源码修正未发版），模板声明frameworkVersion仍0.19.0；UniRx仍7.1.0，Feel仍供应商5.4/镜像5.4.0，Odin仍4.0.2。

没有Bump、提交、推送或发布。消费端不会因普通模板保存自动获得Ember包、embedded UniRx或Feel镜像修正；后续须按各自维护源发布/接入，不能声称注册表旧版本已包含此次补丁。
