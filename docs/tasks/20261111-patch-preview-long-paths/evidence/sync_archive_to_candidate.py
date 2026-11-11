from pathlib import Path
import shutil
source=Path('C:/Users/wuyu/My/ember-unity-framework')
dest=Path('C:/Users/wuyu/.codex/worktrees/release-0-25-3/ember-unity-framework')
task='docs/tasks/20261111-patch-preview-long-paths'
shutil.copytree(source/task,dest/task,dirs_exist_ok=True)
shutil.copyfile(source/'docs/dev/release-0.25.3.md',dest/'docs/dev/release-0.25.3.md')
print('Synced formal archive and release notes; candidate source and version preserved')
