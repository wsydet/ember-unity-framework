# 共享 Git Hooks

本目录存放仓库级共享的 git hooks，通过 `core.hooksPath` 指向这里。

## 激活

clone 后在仓库根目录执行一次：

```sh
sh scripts/setup-git-hooks.sh
```

等价于手动执行：

```sh
git config core.hooksPath .githooks
```

## 为什么需要激活脚本

`core.hooksPath` 是 git 的**本地配置**（存在 `.git/config`），不能通过 `git push/clone` 自动同步到他人机器。因此 hook 脚本本身版本化在本目录，激活脚本负责把每个人的 git 指到这里。

## 各 hook 说明

| Hook | 作用 |
|------|------|
| `pre-commit` | 阻止「新增 Unity 资源却未一并提交对应 `.meta`」的提交 |
| `pre-push` / `post-commit` / `post-checkout` / `post-merge` | Git LFS 官方 hook（由 `git lfs install` 生成，随本目录共享，避免切换 hooksPath 后 LFS 失效） |

## 临时绕过

```sh
git commit --no-verify
```
