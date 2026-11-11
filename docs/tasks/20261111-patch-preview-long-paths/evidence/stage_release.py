from pathlib import Path
import hashlib,json,subprocess
repo=Path.cwd();task=repo/'docs/tasks/20261111-patch-preview-long-paths'
def git(*args): return subprocess.check_output(['git',*args],cwd=repo)
identity=json.loads((task/'evidence/source-identity.json').read_text(encoding='utf-8'))
extra=['Packages/com.ember/package.json','Packages/com.ember/CHANGELOG.md','Packages/com.ember/README.md','Packages/com.ember/Dependencies~/README.md','Packages/com.ember/Dependencies~/release-0.25.3.json','Packages/com.ember/Dependencies~/manifest-0.25.3.json','docs/dev/release-0.25.3.md','docs/dev/framework-progress.md','docs/dev/upm-migration-plan.md']
paths=['Packages/com.ember/'+e['path'] for e in identity['entries']]+extra+['docs/tasks/20261111-patch-preview-long-paths']
subprocess.run(['git','add','--']+paths,cwd=repo,check=True)
entries=[]
for e in identity['entries']:
    p='Packages/com.ember/'+e['path'];blob=git('show',':'+p);work=(repo/p).read_bytes()
    assert blob.replace(b'\r\n',b'\n')==work.replace(b'\r\n',b'\n'),p
    entries.append({'path':e['path'],'gitBlobSha256':hashlib.sha256(blob).hexdigest(),'lfTextSha256':hashlib.sha256(blob.replace(b'\r\n',b'\n')).hexdigest(),'workingTreeSha256':e['sha256']})
release={'version':'0.25.3','tag':'v0.25.3','baseCommit':git('rev-parse','v0.25.2^{}').decode().strip(),'aggregateAlgorithm':'SHA256 UTF8 ordinal-sorted path TAB gitBlobSha256 LF rows','aggregateSha256':hashlib.sha256(''.join(e['path']+'\t'+e['gitBlobSha256']+'\n' for e in entries).encode()).hexdigest(),'entries':entries,'note':'Original task raw-byte identity retained separately; Git text CRLF normalization only, no semantic source changes.'}
(task/'evidence/release-source-identity.json').write_text(json.dumps(release,ensure_ascii=False,indent=2),encoding='utf-8')
p=task/'consumer-validation-prompt.md';s=p.read_text(encoding='utf-8');marker='\n## 正式 v0.25.3 核对身份'
s=s.split(marker)[0]+marker+'\n\n正式接入目标为 `v0.25.3`；通过 `git ls-remote --tags` 核对 annotated tag 的 peeled commit，并通过消费项目实际UPM解析核对。上文0.25.2与逐文件原始指纹是修复验证时工作区历史；以下表为正式候选Git blob指纹，作为发布版本核对依据。聚合SHA256 `'+release['aggregateSha256']+'`（路径排序、TAB、LF算法同上）。\n\nGit可对文本执行CRLF转换；若原始字节不匹配，先保留原始指纹和差异，仅对这些C#/meta文本计算CRLF→LF的SHA256并逐项匹配下表LF列，确认差异仅为换行；不得对资源/二进制套用归一化，也不能只凭版本号跳过身份核对。\n\n| 包内路径 | Git blob SHA256 | 文本LF SHA256 |\n|---|---|---|\n'
for e in entries:s+='| `'+e['path']+'` | `'+e['gitBlobSha256']+'` | `'+e['lfTextSha256']+'` |\n'
p.write_text(s,encoding='utf-8')
subprocess.run(['git','add','--',str(p),str(task/'evidence/release-source-identity.json')],cwd=repo,check=True)
staged=git('diff','--cached','--name-only','-z').decode().strip('\0').split('\0')
allowed=set(paths[:-1])
assert all(p in allowed or p.startswith('docs/tasks/20261111-patch-preview-long-paths/') for p in staged),staged
print(json.dumps({'stagedFiles':len(staged),'releaseSourceAggregate':release['aggregateSha256'],'base':release['baseCommit']}))
