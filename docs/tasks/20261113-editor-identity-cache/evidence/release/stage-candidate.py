from pathlib import Path
import json,subprocess,shutil
source=Path(__file__).resolve().parents[5]
candidate=Path('C:/Users/wuyu/.codex/worktrees/release-0-25-3/ember-unity-framework')
task=Path('docs/tasks/20261113-editor-identity-cache')
shutil.copytree(source/task,candidate/task,dirs_exist_ok=True)
entries=json.loads((source/task/'evidence/source-manifest.json').read_text())
paths=[e['path'] for e in entries]+[
 'Packages/com.ember/package.json','Packages/com.ember/CHANGELOG.md',
 'Packages/com.ember/README.md','Packages/com.ember/Dependencies~/README.md',
 'Packages/com.ember/Dependencies~/release-0.25.4.json','Packages/com.ember/Dependencies~/manifest-0.25.4.json',
 'docs/dev/release-0.25.4.md','docs/dev/upm-migration-plan.md','docs/dev/framework-progress.md',str(task)]
subprocess.run(['git','-C',str(candidate),'add','--',*paths],check=True)
subprocess.run(['git','-C',str(candidate),'add','--renormalize','--',str(task)],check=True)
subprocess.run(['git','-C',str(candidate),'diff','--cached','--check'],check=True)
print('Staged only explicit repair files, release metadata/pointers and this task archive.')
