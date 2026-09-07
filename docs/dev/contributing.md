# Git 分支与提交规范

最后核对：2026-09-07

---

## 一、分支规范

### 1.1 分支模型

```
main ──────────────────────────────────────────────→ （永远稳定）
  │                │                    │
  └─ codex/xxx ───┘ └─ feature/xxx ─────┘ （开发 → 测试 → 合并）
```

### 1.2 分支定义

| 分支 | 用途 | 说明 |
|------|------|------|
| `main` | 稳定主分支 | 随时可发布，合并和推送遵循当前仓库规则及用户明确授权 |
| `codex/<name>` | Codex 默认工作分支 | 从明确的基线创建；用户指定名称时遵从用户 |
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

#### 当前工作流程

框架已进入维护阶段，不再沿用“初始搭建期可直接提交 main”的临时例外。已有工作区改动先确认归属，新增工作默认使用任务分支；不会因文档维护自动提交或推送。

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
- 并行任务应使用独立工作区或明确文件归属，避免覆盖尚未提交的改动

---

## 二、提交信息规范

### 2.1 格式

```
<type>(<scope>): <subject>
```

### 2.2 type（类型）

| type | 说明 | 示例 |
|------|------|------|
| `feat` | 新功能 | `feat(core): 新增 EventBus 发布订阅系统` |
| `fix` | 修复 bug | `fix(ui): 修复界面栈弹出顺序错误` |
| `refactor` | 重构（不改变功能） | `refactor(resource): 简化资源加载路径` |
| `docs` | 文档变更 | `docs: 新增 Git 提交规范文档` |
| `style` | 格式调整（空格、缩进等） | `style(core): 统一代码格式` |
| `chore` | 工程配置、依赖更新 | `chore: 添加 DOTween 嵌入式包` |
| `test` | 测试相关 | `test(core): 新增 EventBus 单元测试` |
| `build` | 构建/CI 相关 | `build: 配置 Android 构建参数` |

不需要 scope 时可省略括号：

```
docs: 更新 CLAUDE.md 项目概述
chore: 升级 Unity 至 6000.5.4
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

- **使用中文**，简洁清晰，一句话说清做了什么
- type 和 scope 保持英文小写（符合 Conventional Commits 规范）
- 结尾不加句号
- 不超过 72 个字符

### 2.5 示例

```
# ✅ 好的提交
feat(core): 新增 EventBus 发布订阅系统
fix(ui): 修复关闭界面时的空引用异常
refactor(resource): 提取资源代理接口
docs: 新增 Git 分支与提交规范
chore: 添加 DOTween 为嵌入式包

# ❌ 不好的提交
update code                        # 太模糊
feat(core): add EventBus system    # subject 应为中文
feat(core): 新增 EventBus 系统。    # 结尾有句号
fix bug                            # 缺少 type 和描述
```

### 2.6 提交粒度

- 一次提交只做一件事
- 一个功能拆成多个小提交，比一个大提交更好
- 代码/资源改动按 [CLAUDE.md](../../CLAUDE.md) 通过 Unity MCP 验证；纯文档改动检查路径、API 一致性和 Markdown
- 源码、资源与配套 `.meta` 一起检查；无关改动先确认归属，不擅自丢弃

---

## 三、参考

- [Conventional Commits](https://www.conventionalcommits.org/)
