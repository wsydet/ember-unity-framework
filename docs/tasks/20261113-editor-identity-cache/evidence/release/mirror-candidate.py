from pathlib import Path
import shutil
source=Path(__file__).resolve().parents[5]
candidate=Path('C:/Users/wuyu/.codex/worktrees/release-0-25-3/ember-unity-framework')
task=Path('docs/tasks/20261113-editor-identity-cache')
shutil.copytree(source/task,candidate/task,dirs_exist_ok=True)
shutil.copyfile(source/'docs/dev/release-0.25.4.md',candidate/'docs/dev/release-0.25.4.md')
print('Mirrored only current task records and 0.25.4 release notes.')
