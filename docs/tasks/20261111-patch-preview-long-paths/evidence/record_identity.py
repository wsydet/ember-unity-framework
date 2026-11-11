from pathlib import Path
import hashlib,json,subprocess
task=Path(__file__).resolve().parent.parent
root=Path.cwd(); package=root/'Packages/com.ember'
names=['Core/Editor/EmberProjectSetup.cs','Core/Editor/EmberProjectSetup.Patch.cs','Core/Editor/EmberProjectSetup.AISkills.cs','Core/Editor/EmberTemplateInheritanceEngine.cs','Core/Editor/EmberTemplateTransaction.cs','Core/Editor/EmberTemplateTemporaryDirectory.cs','Core/Editor/EmberTemplateTemporaryDirectory.cs.meta','Core/Editor/EmberExtensionProject.cs','Core/Editor/EmberExtensionProject.Identity.cs','UPMManager/Editor/EmberTemplateFileSystem.cs','UPMManager/Editor/EmberTemplateFileSystem.cs.meta','UPMManager/Editor/EmberAISkillInstaller.cs','UPMManager/Editor/EmberAISkillInstaller.Templates.cs','Tests/EditMode/EmberTemplatePatchEditTests.cs','Tests/EditMode/EmberTemplateFileSystemEditTests.cs','Tests/EditMode/EmberTemplateFileSystemEditTests.cs.meta']
entries=[{'path':p,'sha256':hashlib.sha256((package/p).read_bytes()).hexdigest()} for p in sorted(names)]
aggregate=hashlib.sha256(''.join(e['path']+'\t'+e['sha256']+'\n' for e in entries).encode()).hexdigest()
manifest={'taskId':task.name,'date':'2026-11-11 Asia/Shanghai','status':'local verification incomplete: file symlink safety pending','packageVersion':'0.25.2','published':False,'head':subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip(),'branch':subprocess.check_output(['git','branch','--show-current'],text=True).strip(),'aggregateAlgorithm':'SHA256 of UTF8 ordinal-sorted path TAB raw-file-sha256 LF rows','aggregateSha256':aggregate,'entries':entries}
(task/'evidence/source-identity.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
diff=subprocess.check_output(['git','diff','--binary','--']+['Packages/com.ember/'+p for p in names])
(task/'evidence/delivery-tracked.diff').write_bytes(diff)
(task/'evidence/git-status-final.txt').write_bytes(subprocess.check_output(['git','status','--porcelain=v1']))
print(json.dumps({'aggregate':aggregate,'files':len(entries),'head':manifest['head']}))
