from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parents[5]
target=root.parent/'Windward/docs/design/planning/editor-performance-20261113/implementation.md'
original=target.read_bytes()
marker='## 框架 0.25.4 正式发布记录（2026-11-13）'
if marker not in original.decode('utf-8-sig'):
    note='\n\n'+marker+'\n\nEmber 框架已正式发布 annotated `v0.25.4`，远端 main 与 tag peeled commit 均为 `c5a2fe90b45ea5343a66f086c2663d35c6f6eac6`。本版仅包含方案 B 的 Editor 身份/扩展缓存及直接相关改动；发布轮 0.25.4 编译/域重载、33/33 定向测试、七模板/六父快照、二进制、字体与 bundle 检查通过。\n\n[发布说明](../../../../../ember-unity-framework/docs/dev/release-0.25.4.md)、[远端发布证明](../../../../../ember-unity-framework/docs/tasks/20261113-editor-identity-cache/evidence/release/remote-proof.json)、[消费接续](../../../../../ember-unity-framework/docs/tasks/20261113-editor-identity-cache/consumer-handoff.md)。\n\n本次未升级 Windward。后续另行授权通过 Ember/UPM Manager 接入，必要的扩展 hash 重新绑定先展示实际预览并独立授权，再做同环境前台四条件和直接相关功能复验。原有限基线及样本限制保留，整体低帧率问题仍待消费端验证。\n'
    with target.open('ab') as stream:stream.write(note.encode('utf-8'))
updated=target.read_bytes()
assert updated.startswith(original)
(Path(__file__).parent/'consumer-published-receipt.json').write_text(json.dumps({
 'original_prefix_preserved':True,'original_sha256':hashlib.sha256(original).hexdigest(),
 'appended_bytes':len(updated)-len(original),'windward_upgraded':False},indent=2),encoding='utf-8')
print('Appended framework publication status only; Windward project not upgraded.')
