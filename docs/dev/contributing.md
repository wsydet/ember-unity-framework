# Git 分支与提交规范

最后更新：2026-07-20

---

## 一、分支规范

### 1.1 分支模型

```
main ──────────────────────────────────────────────→ （永远稳定）
  │                │                    │
  └─ 初始搭建期     └─ feature/xxx ─────┘ （开发 → 测试 → 合并）
```

### 1.2 分支定义

| 分支 | 用途 | 说明 |
|------|------|------|
| `main` | 稳定主分支 | 随时可发布，不可直接推送（初始搭建期例外） |
| `feature/<name>` | 功能开发分支 | 从 main 拉出，开发完成后合并回 main |
| `fix/<name>` | 紧急修复分支 | 从 main 拉出，修复线上问题后合并回 main |

### 1.3 分支命名

```
feature/<模块>-<简述>
fix/<模块>-<简述>
```

命名规则：
- 全部小写，单词用连字符 `-` 分隔
- `<模块>` 使用框架模块名，如 `core`、`ui`、`resource`、`scene`、`audio`
- `<简述>` 用 2-4 个词概括改动内容
- 不使用中文、下划线、空格

示例：

```
feature/core-event-bus        # ✅
feature/ui-page-stack         # ✅
fix/resource-load-error       # ✅

feature_ui                    # ❌ 下划线
Feature/Core                  # ❌ 大写
feature/核心事件系统            # ❌ 中文
```

### 1.4 工作流程

#### 当前阶段：初始搭建

当前正在导入依赖包、搭建项目骨架，**允许直接在 main 分支提交**。

#### 后续阶段：功能开发

```
1. 从 main 拉取最新
   git checkout main
   git pull

2. 创建 feature 分支
   git checkout -b feature/<模块>-<简述>

3. 开发 + 频繁提交
   git add ...
   git commit -m "..."

4. 推到远程
   git push -u origin feature/<模块>-<简述>

5. 开发完成，本地测试通过后合并回 main
   git checkout main
   git pull
   git merge feature/<模块>-<简述>
   git push

6. 删除 feature 分支
   git branch -d feature/<模块>-<简述>
   git push origin --delete feature/<模块>-<简述>
```

### 1.5 核心原则

- `main` 分支随时处于**可编译、可运行**状态
- feature 分支合并前必须在 Unity 中编译通过（0 error）
- 合并使用 `git merge`（保留完整提交历史），不使用 squash
- 同一时间只做一条 feature 分支，避免多线并行导致冲突

---

## 二、提交信息规范

### 2.1 格式

```
<type>(<scope>): <subject>
```

### 2.2 type（类型）

| type | 说明 | 示例 |
|------|------|------|
| `feat` | 新功能 | `feat(core): add EventBus system` |
| `fix` | 修复 bug | `fix(ui): fix page stack pop order` |
| `refactor` | 重构（不改变功能） | `refactor(resource): simplify load path` |
| `docs` | 文档变更 | `docs: add git conventions` |
| `style` | 格式调整（空格、缩进等） | `style(core): format code` |
| `chore` | 工程配置、依赖更新 | `chore: add DOTween package` |
| `test` | 测试相关 | `test(core): add EventBus unit test` |
| `build` | 构建/CI 相关 | `build: configure Android build` |

不需要 scope 时可省略括号：

```
docs: update CLAUDE.md
chore: upgrade Unity to 6000.5.4
```

### 2.3 scope（范围）

使用框架模块名：

| scope | 对应 |
|-------|------|
| `core` | 核心：EventBus、Singleton、ServiceLocator |
| `resource` | 资源管理 |
| `ui` | UI 框架 |
| `scene` | 场景管理 |
| `audio` | 音频管理 |
| `input` | 输入系统 |

不涉及具体模块时省略 scope。

### 2.4 subject（描述）

- 使用英文
- 全部小写
- 简洁清晰，一句话说清做了什么
- 结尾不加句号
- 不超过 72 个字符

### 2.5 示例

```
# ✅ 好的提交
feat(core): add EventBus with pub/sub support
fix(ui): fix null reference when closing panel
refactor(resource): extract resource proxy interface
docs: add git branch and commit conventions
chore: add DOTween as embedded package

# ❌ 不好的提交
update code                        # 太模糊
feat: 添加了事件系统                  # 混用中文
feat(core): add EventBus system.   # 结尾有句号
fix bug                            # 缺少 type 和描述
```

### 2.6 提交粒度

- 一次提交只做一件事
- 一个功能拆成多个小提交，比一个大提交更好
- 提交前在 Unity 中确认编译通过
- 不提交 `.meta` 文件以外的意外变更

---

## 三、参考

- [Conventional Commits](https://www.conventionalcommits.org/)
