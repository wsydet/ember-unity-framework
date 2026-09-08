# UnityFarm 改动回流与升级规则

> 适用范围：在 UnityFarm 开发产品功能时，判断一项改动应只保留在 UnityFarm，还是回流 Ember Framework、Ember 模板，或同时更新两者。
> 本文随 `com.ember` 发布，是该问题的唯一正式判定口径；仓库根 `AGENTS.md` 与 `CLAUDE.md` 只维护强制入口。

## 1. 先记住四个结论

| 判定 | 改动归属 | UnityFarm 后续动作 |
|---|---|---|
| 仅当前产品需要 | UnityFarm 项目 | 正常提交 UnityFarm；不发布 Ember |
| 所有项目都应通过 API、运行时或编辑器工具获得 | Ember 框架 | 在框架仓库修改并发布新 `com.ember`；UnityFarm 升级框架 |
| 新项目或选定玩法模板应获得一份可继续修改的 `Assets` 初始内容 | Ember 模板 | 在框架仓库通过模板开发流程封存模板，并发布新 `com.ember`；UnityFarm 升级框架后再处理模板 |
| 通用能力和模板初始接线必须一起变化 | 框架 + 模板 | 同一批次更新两者并发布新 `com.ember`；UnityFarm 先升级框架，再应用模板变化 |

判断依据是“谁应该长期拥有这项能力”，不是当前改动恰好落在哪个文件。UnityFarm 中写出的代码也可能应回流框架；
反过来，调用 Ember API 的代码不等于框架代码。

## 2. 仅保留在 UnityFarm 的改动

满足以下任一条件时，默认只改 UnityFarm：

- 只服务当前游戏的玩法、数值、关卡、剧情、角色、任务、商业化、运营或平台接入。
- 包含 UnityFarm 专用资源、账号/服务配置、产品标识、构建参数或环境差异。
- 是对框架公开 API 的正常调用或组合，没有暴露框架缺陷，也不值得其他项目直接复用。
- 尚未证明可复用，只是为赶当前业务临时验证的实现。

常见例子：具体战斗规则、某个角色控制器、UnityFarm 页面内容、关卡场景、美术与音频、产品专用
`ScriptableObject`、业务存档结构、服务器协议、渠道 SDK 配置、UnityFarm 的 `ProjectSettings`。

不要为了“以后也许能用”过早回流。可复用性不明确时先留在 UnityFarm，并把候选抽象记录下来。

## 3. 需要升级 Ember 框架的改动

下列改动应在 `ember-unity-framework` 的 `Packages/com.ember` 内实现，并发布新的框架版本：

- 修复 Ember Runtime、Editor、代码生成器、UPM Manager 或模板系统本身的缺陷。
- 新增不依赖具体产品内容的通用 API、Manager、引擎能力、编辑器工具或诊断能力。
- 修改框架公共契约、生命周期、序列化格式、程序集边界、依赖或兼容策略。
- 多个项目都应该直接从 Package 获得，而不应该复制到各自 `Assets` 的实现。
- 为现有框架能力补充必须随包交付的测试或使用文档。

常见例子：事件总线、资源 Handle、UI 引擎、SceneUI 投影、通用输入接口、模板部署事务、GUID 冲突预检、
项目中心功能和框架级 bug 修复。

禁止直接修改 UnityFarm 的 `Library/PackageCache`。应在框架仓库复现并修复，验证后发布新 tag，再让
UnityFarm 通过 UPM 升级；PackageCache 中的试验性修改最多用于定位，不能作为最终交付。

## 4. 需要升级 Ember 模板的改动

模板用于交付“部署后归项目所有、允许项目继续修改”的初始 `Assets` 内容。符合以下条件时升级模板：

- 新建项目或选择该模板时就应该存在的业务骨架、场景、Prefab、配置、输入资产或示例接线发生变化。
- 改动属于 `base` 的所有项目共同起点，或只属于某个玩法模板的完整初始形态。
- 只升级 Package 代码无法让已经部署或新部署的项目获得正确的 `Assets` 内容。

当前模板管理范围只有：

- `Assets/Game`
- `Assets/Resources`
- `Assets/Ember/Editor`
- `Assets/Settings`
- `Assets/GameResource`

`Assets/Art`、`Assets/ThirdParty`、`Assets/Editor` 和 `ProjectSettings` 不在模板快照内，不能因为位于项目中
就假定模板系统会发布或更新它们。

常见例子：公共启动场景、模板自带 UI、`GamePages` 初始注册、模板输入资产、示例 Module、玩法模板场景、
模板默认渲染资源和必须复制到项目后再由业务维护的配置。

模板内容必须在 embedded 框架开发仓库中按“加载模板 → 修改项目 `Assets` → 保存模板 → 显式 Bump”处理。
禁止手改 `Templates~/*/Assets`、`ParentSnapshot~`、`template.json` hash，或从 UnityFarm 直接覆盖快照。
修改 `base` 后，还要预览并同步受影响的派生模板；父子冲突必须明确选择并重新验收。

## 5. 必须同时升级框架和模板的情况

只要框架代码与项目初始内容存在配套关系，就应作为同一批次处理。例如：

- 新增框架 API，同时修改模板脚本或场景来调用它。
- 修改序列化字段、生成格式或生命周期，同时更新模板中的 Prefab、配置或业务骨架。
- 修复模板部署/校验逻辑，同时发布依赖该逻辑的新模板内容。
- 移除或重命名框架 API，需要迁移模板中的调用方。

顺序固定为：先确定框架契约，再更新模板接线；先验证框架与模板组合，再发布；UnityFarm 先升级框架，
再决定如何吸收模板内容。不能先把新模板部署到仍运行旧框架的 UnityFarm。

## 6. 版本如何选择

框架版本和模板内容版本彼此独立：

| 变化 | 框架版本 | 模板版本 |
|---|---|---|
| 仅 UnityFarm 业务 | 不变 | 不变 |
| 仅框架文档、兼容修复或向后兼容的小修 | 通常 patch | 内容不变则不 Bump |
| 框架向后兼容的新能力 | 通常 minor | 仅当模板 Assets 改变时 Bump |
| 框架破坏性变更 | major | 按模板实际内容和迁移影响决定 |
| 模板内容修复 | 必须有新的框架发布 tag 才能交付 | patch |
| 模板新增结构或可选能力 | 必须有新的框架发布 tag 才能交付 | minor |
| 模板破坏性重构 | 必须有新的框架发布 tag 才能交付 | major |

模板存放在 `com.ember` 内，因此“升级模板”在交付层面也需要发布一个新的框架 tag。模板 Assets 没有改变时，
可以只把 `frameworkVersion` 兼容声明推进到新框架版本，不应伪造模板内容 Bump，也不应改动内容 hash。

## 7. UnityFarm 如何接收更新

### 7.1 仅框架变化

发布 Ember 后，在 UnityFarm 使用 `Ember/UPM Manager` 或项目 manifest 升级 `com.ember`。让 Unity 完成解析、
编译和回归；不需要执行模板部署。

### 7.2 模板也发生变化

先升级 `com.ember`，再根据项目现状选择：

- **补齐缺失**：只适合取得新增文件；不会刷新已有文件中的旧模板逻辑。
- **完整重新部署**：事务替换五个模板管理目录，会覆盖其中的 UnityFarm 修改；只可在备份和确认差异后使用。
- **人工迁移**：UnityFarm 已在受管目录中持续开发时的默认安全选择。对照新模板移植必要变化，并保留产品代码。

当前尚没有完整的消费端用户区合并向导。模板 patch/minor 只是变化提示，不代表可以安全覆盖 UnityFarm。
不要把“框架已经升级”误认为“已部署模板会自动更新”。

## 8. 从 UnityFarm 发现改动后的标准流程

1. 写清问题、复现条件、期望行为和最小影响范围；保留 UnityFarm 业务上下文，但移除产品私密数据。
2. 按本文先判定“项目 / 框架 / 模板 / 两者”，不确定时优先保持项目改动并单独做回流评估。
3. 框架改动在 `Packages/com.ember` 实现；模板改动在 embedded 框架项目中通过项目中心保存和封存。
4. 执行与改动匹配的 Unity 编译、EditMode、Play Mode、模板 hash/父快照和消费项目验证。
5. 更新 `package.json`、`CHANGELOG`、依赖声明和发布说明，创建不可变版本 tag 并推送。
6. UnityFarm 升级框架；如模板有变化，再按 7.2 节选择补齐、完整重新部署或人工迁移。
7. 在 UnityFarm 重新验证原始问题和受影响链路，不能用框架开发项目通过代替消费项目通过。

## 9. 每次任务结束前检查

- [ ] 已明确改动归属，没有把产品业务收进 Ember。
- [ ] 没有直接修改或依赖 UnityFarm PackageCache 中的临时内容。
- [ ] 框架 API、模板初始内容及调用方的版本关系一致。
- [ ] 模板变更经过保存、Bump；父模板变更已评估派生同步。
- [ ] 已明确 UnityFarm 是只升级框架，还是还要迁移模板。
- [ ] 已记录实际完成的 Unity/消费端验证，未把静态检查写成编译或运行通过。

模板存储、父子同步和部署细节见开发仓库
[`docs/dev/template-upgrade-system.md`](../../../../docs/dev/template-upgrade-system.md)；发布流程见
[`docs/dev/upm-migration-plan.md`](../../../../docs/dev/upm-migration-plan.md)。在只安装 UPM 包的 UnityFarm 中，
仓库级相对链接可能不可用，但本文的判定规则仍完整随包保留。
