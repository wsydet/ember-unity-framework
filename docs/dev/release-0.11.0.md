# 0.11.0 声明与上传命令

## 已准备的声明

- 框架：`Packages/com.ember/package.json` 为 0.11.0。
- 模板：`base 0.5.5` 与 `source3d-2p5d 0.2.6` 均声明兼容框架 0.11.0；内容、版本、hash、父基线和当前编辑记录不变。声明前已逐树复核内容 hash。
- 消费端：`Packages/com.ember/Dependencies~/manifest-0.11.0.json` 包含当前工程 55 项直接依赖加 Feel，共 56 项；不含开发机 `file:` 路径，MCP 固定到当前 lock 的 commit。
- 第三方：`ember-thirdparty-upm/release-ember-0.11.0.json` 列出 7 个包及其版本，消费端统一引用新 tag `ember-v0.11.0`，旧插件 tag 保持不变。

当前未执行提交、打 tag 或推送，也未完成 Unity 编译、Feel 的 UPM 安装切换和消费端功能验证。声明不等于验收通过。
现有升级器不自动执行该依赖清单，消费端须按随包 Dependencies~/README.md 合并配置；不能仅升级 com.ember 就认定所有插件已同步。

## 执行前检查

1. 先确认 GitHub 的 `wsydet/ember-thirdparty-upm` 仓库为 **Private**，且所有接收者的插件使用在适用许可范围内。下列命令不修改仓库可见性。
2. 请在 Unity 中手动触发编译，完成模板正常保存/部署与 Feel 独立安装、Inspector/运行时回归；仍有错误时不要发布。
3. 两个仓库目前均为 `main`，远程均为 `origin`。下列脚本拒绝已有暂存内容和已存在的新 tag，不使用 force，也不推送无关历史 tag。
4. 框架脚本将提交 `Packages/com.ember` 与 `docs` 下的全部现有改动（包含这轮之前的 0.11.0 工作），不包含项目 `Assets`、`.agents`、`.claude` 和其他目录。必须先审阅暂存清单；若其中有不应发布的改动，停止并先拆分提交。
5. 如果 push 因远程分支变化或 tag 已存在而失败，停止核对，不覆盖 tag、不强制推送；本地成功创建的提交和 tag 保留，不重复执行创建命令。

## 1. 第三方仓库（先上传）

在 PowerShell 中执行：

```powershell
& {
Set-Location -LiteralPath 'C:\Users\wuyu\My\ember-thirdparty-upm' -ErrorAction Stop
function Invoke-ReleaseGit { git @args; if ($LASTEXITCODE -ne 0) { throw 'Git 命令失败，已停止。' } }
if ((git branch --show-current) -ne 'main') { throw '当前分支不是 main。' }
if ((git remote get-url origin) -ne 'https://github.com/wsydet/ember-thirdparty-upm.git') { throw 'origin 地址不匹配。' }
if (git tag --list 'ember-v0.11.0' 'feel-v5.4.0') { throw '目标 tag 已存在，请先核对，禁止覆盖。' }
Invoke-ReleaseGit diff --cached --quiet
Invoke-ReleaseGit add -- README.md com.moremountains.feel release-ember-0.11.0.json
Invoke-ReleaseGit diff --cached --stat
if ((Read-Host '确认仓库为 Private、授权及 Unity 验收完成、暂存内容正确后输入 RELEASE') -cne 'RELEASE') { throw '已停止，暂存内容保留。' }
Invoke-ReleaseGit commit -m 'release: third-party bundle for Ember 0.11.0'
Invoke-ReleaseGit tag -a ember-v0.11.0 -m 'Third-party dependency baseline for Ember 0.11.0'
Invoke-ReleaseGit tag -a feel-v5.4.0 -m 'Feel 5.4.0 team UPM package'
Invoke-ReleaseGit push --atomic origin main refs/tags/ember-v0.11.0 refs/tags/feel-v5.4.0
}
```

该 bundle tag 所指提交包含整个第三方仓库，Rainbow 两包、Console Pro、InputDeviceDetector、Odin、DOTween 不需要重新创建旧版本 tag。

## 2. 框架仓库（第三方上传成功后）

```powershell
& {
Set-Location -LiteralPath 'C:\Users\wuyu\My\ember-unity-framework' -ErrorAction Stop
function Invoke-ReleaseGit { git @args; if ($LASTEXITCODE -ne 0) { throw 'Git 命令失败，已停止。' } }
if ((git branch --show-current) -ne 'main') { throw '当前分支不是 main。' }
if ((git remote get-url origin) -ne 'https://github.com/wsydet/ember-unity-framework.git') { throw 'origin 地址不匹配。' }
if (git tag --list v0.11.0) { throw 'v0.11.0 已存在，请先核对，禁止覆盖。' }
Invoke-ReleaseGit diff --cached --quiet
Invoke-ReleaseGit ls-remote --exit-code --tags 'https://github.com/wsydet/ember-thirdparty-upm.git' refs/tags/ember-v0.11.0
Invoke-ReleaseGit add -- Packages/com.ember docs
Invoke-ReleaseGit diff --cached --stat
if ((Read-Host '确认 Unity/消费端验收完成、框架包和 docs 的全部暂存改动均应发布后输入 RELEASE') -cne 'RELEASE') { throw '已停止，暂存内容保留。' }
Invoke-ReleaseGit commit -m 'release: Ember Framework 0.11.0'
Invoke-ReleaseGit tag -a v0.11.0 -m 'Ember Framework 0.11.0'
Invoke-ReleaseGit push --atomic origin main refs/tags/v0.11.0
}
```

上传后消费端框架 URL：

```text
https://github.com/wsydet/ember-unity-framework.git?path=/Packages/com.ember#v0.11.0
```

完整依赖的安装另需合并随包清单，特别是私有 Git 包和 OpenUPM registry；该 URL 本身只安装框架及其 package.json 已声明的依赖。
