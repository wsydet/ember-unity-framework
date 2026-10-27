'use strict';
const test=require('node:test'), assert=require('node:assert/strict');
const fs=require('node:fs/promises'),path=require('node:path'),os=require('node:os');
const {validateModel,verify,inside}=require('./verify-ember');
const bridge=require('../src/export/file-bridge');
const repo=path.resolve(__dirname,'../../../../..');
const art=path.join(repo,'art/ui/S4');
const caps=['ember-eui-map-v1','dynamic-list-v1'];
async function input(){return [JSON.parse(await fs.readFile(path.join(art,'EmberPilot.bui'),'utf8')),JSON.parse(await fs.readFile(path.join(art,'EmberPilot.eui-map.json'),'utf8'))];}
test('real Photoshop export passes read-only structural/semantic/resource gate',async()=>assert.equal((await verify(repo,path.join(art,'EmberPilot.eui-map.json'),caps)).status,'passed'));
for(const [name,mutate,error] of [
  ['duplicate node identity',b=>b.nodes.push({...b.nodes[0]}),/DUPLICATE/],
  ['unknown critical flag',b=>b.warnings.push({severity:'Warning',code:'PSD_UNKNOWN_OPTION_FLAG'}),/EXPORT_DIAGNOSTIC/],
  ['lost GeneratedTarget',b=>b.nodes.find(n=>n.prefabRef?.kind==='GeneratedTarget').prefabRef.targetId='missing',/LIST_ITEM_REFERENCE|PREFAB_TARGET_MISSING/],
  ['invalid geometry',b=>b.nodes[0].rect.sizeDelta=[NaN,1],/GEOMETRY/],
  ['hidden state lost',b=>b.nodes.find(n=>n.id==='group_36').active=true,/STATE_VISIBILITY/]
])test(name,async()=>{const [b,c]=await input();mutate(b);assert.throws(()=>validateModel(b,c,caps),error);});
test('unsupported dynamic capability never degrades to Group',async()=>{const [b,c]=await input();assert.throws(()=>validateModel(b,c,['ember-eui-map-v1']),/REQUIRED_CAPABILITY/);});
test('dynamic item cannot be fixed UILogic',async()=>{const [b,c]=await input();c.targets.find(t=>t.buiRole==='ListItem').fixedUILogic=true;assert.throws(()=>validateModel(b,c,caps),/DYNAMIC_ITEM_FIXED/);});
test('variant slot alias must resolve to actual catalog slot',async()=>{const [b,c]=await input();delete c.publicReferences.find(r=>r.prefabName==='EUISecondaryButtonItem').slotMap;assert.throws(()=>validateModel(b,c,caps),/PUBLIC_SLOT_UNKNOWN/);});
test('path escape rejected',()=>assert.throws(()=>inside(repo,'../outside'),/UNSAFE_PATH/));
test('changed input digest rejected',async()=>{const [,c]=await input();c.workingPsd.sha256='0'.repeat(64);const dir=await fs.mkdtemp(path.join(os.tmpdir(),'ember-s4-'));const file=path.join(dir,'map.json');try{await fs.writeFile(file,JSON.stringify(c));await assert.rejects(verify(repo,file,caps),/INPUT_DIGEST_CHANGED/);}finally{await fs.unlink(file);await fs.rmdir(dir);}});
test('fixture: missing referenced PNG stops consumption',async()=>{
  const [b,c]=await input(),dir=await fs.mkdtemp(path.join(repo,'.utmp/ember-s4-missing-'));
  const buiFile=path.join(dir,'missing.bui'),mapFile=path.join(dir,'map.json'),bytes=JSON.stringify(b);
  c.bui={path:path.relative(repo,buiFile),sha256:require('node:crypto').createHash('sha256').update(bytes).digest('hex')};
  try{await fs.writeFile(buiFile,bytes);await fs.writeFile(mapFile,JSON.stringify(c));await assert.rejects(verify(repo,mapFile,caps),{code:'ENOENT'});}
  finally{await fs.unlink(buiFile);await fs.unlink(mapFile);await fs.rmdir(dir);}
});
function host(handler){
  const id='a'.repeat(32),sessionId='b'.repeat(32),requests=new Map(),claims=new Map(),results=new Map();
  const request={version:1,requestId:id,operation:'export',createdAt:new Date().toISOString(),startBefore:new Date(Date.now()+60000).toISOString(),sessionId,params:{}};
  requests.set(id,JSON.stringify(request));
  const store={listRequests:async()=>[...requests.keys()],readRequest:async key=>({bytes:requests.get(key),text:requests.get(key)}),readResult:async key=>results.get(key),readClaim:async key=>claims.get(key),claim:async(key,value)=>{if(claims.has(key))return false;claims.set(key,value);return true;},publishResult:async(key,value)=>results.set(key,value)};
  const runner=bridge.create({store,sessionId,handlers:{export:handler},hash:async bytes=>bytes});
  return {id,runner,results,claims,requests,request};
}
test('fixture: partial failure is terminal and repeated tick never replays',async()=>{let calls=0;const h=host(async()=>{calls++;throw Object.assign(Error('injected after PNG write'),{code:'PNG_WRITE_FAILED',partialOutputPossible:true});});await h.runner.tick();await h.runner.tick();assert.equal(calls,1);assert.equal(h.results.get(h.id).status,'failed');assert.equal(h.results.get(h.id).error.partialOutputPossible,true);});
test('fixture: claim without terminal result reports unknown without execution',async()=>{let calls=0;const h=host(async()=>{calls++;});h.claims.set(h.id,{requestHash:h.requests.get(h.id),operation:'export'});await h.runner.tick();assert.equal(calls,0);assert.equal(h.results.get(h.id).status,'unknown');});
test('fixture: expired request is rejected before write',async()=>{let calls=0;const h=host(async()=>calls++);h.request.createdAt=new Date(Date.now()-120000).toISOString();h.request.startBefore=new Date(Date.now()-60000).toISOString();h.requests.set(h.id,JSON.stringify(h.request));await h.runner.tick();assert.equal(calls,0);assert.equal(h.results.get(h.id).error.code,'START_DEADLINE_EXPIRED');});
