#!/bin/sh
# 激活仓库共享的 git hooks（clone 后执行一次即可）
#
# 用法（在仓库根目录下）：
#   sh scripts/setup-git-hooks.sh
#
# 说明：git 的 core.hooksPath 是本地配置，无法随仓库自动共享，
# 所以 hook 文件版本化在 .githooks/ 里，由本脚本把路径指过去。

set -e

# 切回仓库根目录（脚本位于 scripts/ 下）
cd "$(dirname "$0")/.."

git config core.hooksPath .githooks

echo "[setup-git-hooks] core.hooksPath = $(git config --get core.hooksPath)"
echo "[setup-git-hooks] 已激活共享 hooks。"
echo "[setup-git-hooks] 之后提交会自动校验：新增 Unity 资源必须一并提交对应 .meta。"
echo "[setup-git-hooks] 如仓库启用了 Git LFS，请确认已执行过 'git lfs install'。"
