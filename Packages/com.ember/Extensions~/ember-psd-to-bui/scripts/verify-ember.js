#!/usr/bin/env node
'use strict';
// Read-only S4 input gate. This does not create Unity assets or implement S5.
const fs = require('node:fs/promises');
const path = require('node:path');
const crypto = require('node:crypto');
const sha = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const fail = code => { throw Object.assign(new Error(code), {code}); };
function inside(root, relative) {
  if (typeof relative !== 'string' || !relative || path.isAbsolute(relative)) fail('UNSAFE_PATH');
  const result = path.resolve(root, relative), rel = path.relative(root, result);
  if (rel.startsWith('..') || path.isAbsolute(rel)) fail('UNSAFE_PATH');
  return result;
}
async function checked(root, record) {
  const file = inside(root, record.path);
  // Resolve symlinks too: lexical containment alone is insufficient.
  const real = await fs.realpath(file), base = await fs.realpath(root);
  const rel = path.relative(base, real);
  if (rel.startsWith('..') || path.isAbsolute(rel)) fail('UNSAFE_PATH');
  const bytes = await fs.readFile(real);
  if (sha(bytes) !== record.sha256) fail('INPUT_DIGEST_CHANGED:' + record.path);
  return bytes;
}
function unique(items, field) {
  const ids = items.map(x => x[field]);
  if (ids.some(x => typeof x !== 'string' || !x) || new Set(ids).size !== ids.length) fail('DUPLICATE_OR_EMPTY_' + field);
}
function validateModel(bui, companion, capabilities) {
  if (bui.schemaVersion !== 3 || companion.schemaVersion !== 1) fail('SCHEMA_UNSUPPORTED');
  if (!Array.isArray(companion.requiredCapabilities) || !companion.requiredCapabilities.includes('ember-eui-map-v1')) fail('MISSING_CAPABILITY_DECLARATION');
  for (const cap of companion.requiredCapabilities) if (!capabilities.includes(cap)) fail('REQUIRED_CAPABILITY_UNSUPPORTED:' + cap);
  unique(bui.nodes, 'id'); unique(bui.targets, 'targetId'); unique(companion.targets, 'targetId');
  const nodes = new Map(bui.nodes.map(n => [n.id,n])), targets = new Map(bui.targets.map(t => [t.targetId,t]));
  if (bui.targets.length !== companion.targets.length) fail('TARGET_COVERAGE');
  for (const t of bui.targets) {
    if (!['Page','Popup','Component','ListItem'].includes(t.targetType)) fail('TARGET_UNSUPPORTED');
    if (!nodes.has(t.sourceRootNodeId) || !t.nodeIds.includes(t.sourceRootNodeId) || new Set(t.nodeIds).size !== t.nodeIds.length || t.nodeIds.some(id => !nodes.has(id))) fail('TARGET_NODE_REFERENCE');
  }
  for (const t of companion.targets) {
    if (targets.get(t.targetId)?.targetType !== t.buiRole) fail('TARGET_ROLE_MISMATCH');
    if (t.buiRole === 'ListItem' && (t.registration !== 'dynamic-item-only' || t.fixedUILogic === true)) fail('DYNAMIC_ITEM_FIXED_REGISTRATION');
  }
  for (const n of bui.nodes) {
    if (!['Group','Bg','Top','Center','Bottom','Image','RawImage','Button','Text','PrefabRef','List'].includes(n.type)) fail('NODE_UNSUPPORTED');
    if (n.parentId && !nodes.has(n.parentId)) fail('PARENT_MISSING');
    let cursor=n; const visited=new Set();
    while(cursor) { if(visited.has(cursor.id)) fail('PARENT_CYCLE'); visited.add(cursor.id);cursor=nodes.get(cursor.parentId); }
    if (typeof n.active !== 'boolean' || !n.rect || ['anchorMin','anchorMax','pivot','anchoredPosition','sizeDelta'].some(k=>!Array.isArray(n.rect[k])||n.rect[k].length!==2||n.rect[k].some(v=>!Number.isFinite(v)))) fail('INVALID_NODE_GEOMETRY');
    if (n.type === 'PrefabRef' && n.prefabRef?.kind === 'GeneratedTarget' && !targets.has(n.prefabRef.targetId)) fail('PREFAB_TARGET_MISSING');
    if (n.type === 'PrefabRef' && n.prefabRef?.kind === 'ExternalPrefab') {
      const ref=companion.publicReferences.find(r=>r.nodeId===n.id);
      if (!ref || ref.guid !== n.prefabRef.assetGuid || ref.prefabName !== n.prefabRef.prefabName) fail('PREFAB_IDENTITY_MISSING');
      for (const o of n.prefabRef.overrides || []) {
        const slotId=ref.slotMap?.[o.slot] || o.slot;
        if (!ref.slots.some(s=>s.id===slotId)) fail('PUBLIC_SLOT_UNKNOWN');
      }
    }
    if (n.type === 'List') {
      const list=companion.lists.find(l=>l.nodeId===n.id);
      if (!companion.requiredCapabilities.includes('dynamic-list-v1') || !list || !list.contract?.itemGuid || !list.contract?.owner || !list.contract?.dataBinding || list.key !== 'Id' || list.index !== 'display-position') fail('LIST_SEMANTICS_MISSING');
      const ref=nodes.get(list.itemNodeId);
      if(ref?.parentId !== n.id || ref.type!=='PrefabRef' || ref.prefabRef?.kind!=='GeneratedTarget' || ref.prefabRef.targetId!==list.itemTargetId) fail('LIST_ITEM_REFERENCE');
    }
  }
  for(const w of bui.warnings || []) if(w.severity==='Blocking' || /UNKNOWN|DUPLICATE_TARGET|FLAG_CONTEXT_INVALID|PARAMS_INVALID|EXPORT_FAILED/.test(w.code)) fail('EXPORT_DIAGNOSTIC:' + w.code);
  for(const state of companion.states) if(!nodes.has(state.nodeId) || nodes.get(state.nodeId).active!==state.designActive) fail('STATE_VISIBILITY_MISMATCH');
  return {targets:bui.targets.length,nodes:bui.nodes.length,lists:companion.lists.length};
}
async function verify(repo, companionFile, capabilities) {
  const companion=JSON.parse(await fs.readFile(companionFile,'utf8'));
  const buiBytes=await checked(repo,companion.bui), bui=JSON.parse(buiBytes);
  const summary=validateModel(bui,companion,capabilities);
  const psd=await checked(repo,companion.workingPsd);
  if(psd.subarray(0,4).toString()!=='8BPS' || sha(psd)!==bui.source.psdSha256) fail('PSD_SOURCE_MISMATCH');
  await checked(repo,companion.sourcePsd);
  for(const input of companion.inputs) await checked(repo,input);
  for(const input of companion.contracts) {
    const contract=JSON.parse(await checked(repo,input));
    await checked(repo,{path:contract.prefabPath,sha256:contract.assetDigest});
    for(const cap of contract.requiredCapabilities||[]) if(!companion.requiredCapabilities.includes(cap)) fail('CONTRACT_CAPABILITY_DROPPED');
    for(const list of companion.lists.filter(x=>x.contractPath===input.path)) if(JSON.stringify(contract.lists[list.contractIndex])!==JSON.stringify(list.contract)) fail('LIST_CONTRACT_CHANGED');
  }
  const catalogInput=companion.inputs.find(i=>i.path.endsWith('/UILibrary/catalog.json'));
  if(!catalogInput)fail('PUBLIC_CATALOG_MISSING');
  const catalog=JSON.parse(await checked(repo,catalogInput));
  for(const ref of companion.publicReferences) {
    const entry=catalog.entries.find(e=>e.prefabGuid===ref.guid);
    if(!entry || JSON.stringify(entry.slots)!==JSON.stringify(ref.slots))fail('PUBLIC_CATALOG_CHANGED');
    const meta=await fs.readFile(inside(repo,ref.prefabPath+'.meta'),'utf8');
    if(!meta.split(/\r?\n/).includes('guid: '+ref.guid))fail('PREFAB_GUID_CHANGED');
  }
  const directory=path.dirname(inside(repo,companion.bui.path));
  const resources=[];
  for(const n of bui.nodes) {if(n.resource)resources.push(n.resource); for(const o of n.prefabRef?.overrides||[])if(o.resource)resources.push(o.resource);}
  for(const r of bui.resources||[]) resources.push(r.resource);
  const assets=[];
  for(const r of resources) {
    const bytes=await checked(directory,{path:path.join(bui.assetsRoot,r.asset),sha256:r.sha256});
    if(bytes.subarray(0,8).toString('hex')!=='89504e470d0a1a0a')fail('PNG_SIGNATURE');
    if(bytes.readUInt32BE(16)!==r.width || bytes.readUInt32BE(20)!==r.height)fail('PNG_DIMENSIONS');
    if(r.border && (r.border.left+r.border.right>=r.width || r.border.top+r.border.bottom>=r.height))fail('NINE_SLICE_CENTER');
    assets.push({asset:r.asset,width:r.width,height:r.height,sha256:r.sha256});
  }
  const texts=[...bui.nodes.map(n=>n.text),...bui.nodes.flatMap(n=>(n.prefabRef?.overrides||[]).map(o=>o.text)),...(bui.designTexts||[]).map(t=>t.text)].filter(Boolean);
  let textReferences=0;
  for(const t of texts) {
    const r=t.previewReference;
    if(r?.status!=='captured')continue;
    if(r.sourcePsdSha256!==bui.source.psdSha256)fail('TEXT_REFERENCE_SOURCE_CHANGED');
    const bytes=await checked(directory,{path:r.path,sha256:r.sha256});
    if(bytes.readUInt32BE(16)!==r.width || bytes.readUInt32BE(20)!==r.height)fail('TEXT_REFERENCE_DIMENSIONS');
    textReferences++;
  }
  return {status:'passed',scope:'read-only S4 structural and semantic gate; not Unity Maker',...summary,assets,textReferences};
}
if(require.main===module) {
  const [repo, companion, ...capabilities]=process.argv.slice(2);
  verify(path.resolve(repo),path.resolve(companion),capabilities).then(r=>console.log(JSON.stringify(r,null,2))).catch(e=>{console.error(JSON.stringify({status:'failed',code:e.code||e.message}));process.exitCode=1;});
}
module.exports={verify,validateModel,inside};
