// Copyright (c) 2026 Burner Games. All rights reserved.
// This file is part of Burner Unity Packages. Package: com.burner.ui.prefabmaker
(function registerBridgeExport(global) {
  "use strict";
  const SHA=/^[a-f0-9]{64}$/;
  const sha=value=>typeof value==="string"&&SHA.test(value);
  const fail=(code,message=code,details)=>Object.assign(new Error(message),{code,bridgeRejected:true,...(details?{details}:{})});
  function keys(value, expected) {
    return value&&typeof value==="object"&&!Array.isArray(value)&&Object.keys(value).length===expected.length&&expected.every(key=>Object.prototype.hasOwnProperty.call(value,key));
  }
  function validate(params) {
    if(!keys(params,["document","expectedTargetIds","options","outputPolicy"]) ||
       !keys(params.document,["id","path","sha256","saved"]) || !Number.isInteger(params.document.id) || params.document.id<=0 ||
       params.document.saved!==true || typeof params.document.path!=="string" || !sha(params.document.sha256) ||
       !/\.(rename|renamed)\.psd$/i.test(params.document.path) ||
       !Array.isArray(params.expectedTargetIds) || !params.expectedTargetIds.length || params.expectedTargetIds.length>2000 ||
       params.expectedTargetIds.some(id=>typeof id!=="string"||!id||id.length>256)||new Set(params.expectedTargetIds).size!==params.expectedTargetIds.length ||
       !keys(params.options,["mode","exportBaseName","sliceFileNamePrefix","reusePolicy","namingRulesSha256","exportSettingsSha256"]) ||
       params.options.mode!=="bui-png" || !["current-psd","retain"].includes(params.options.reusePolicy) ||
       typeof params.options.exportBaseName!=="string" || !params.options.exportBaseName.trim() ||
       /[\\/:*?"<>|\x00-\x1f]/.test(params.options.exportBaseName)||/[. ]$/.test(params.options.exportBaseName)||params.options.exportBaseName.length>128 ||
       typeof params.options.sliceFileNamePrefix!=="string" || !/^[A-Za-z0-9_]{0,32}$/.test(params.options.sliceFileNamePrefix)||!sha(params.options.namingRulesSha256)||!sha(params.options.exportSettingsSha256)||
       !keys(params.outputPolicy,["kind","overwrite"])||params.outputPolicy.kind!=="psd-directory" ||
       !(params.outputPolicy.overwrite===null || keys(params.outputPolicy.overwrite,["decisionSha256"])&&sha(params.outputPolicy.overwrite.decisionSha256)))
      throw fail("INVALID_PARAMS","Supply the inspected saved working PSD, complete Target IDs, explicit export options and PSD-directory output policy.");
  }
  function create({runner,getActiveDocument,getDocumentPath,normalizePath,folderPath,getFile,getFolder,getRules,getSettings,
    assertSaved,hash,encode,version,isBusy,setBusy,feedback=()=>{}}) {
    async function exportDocument(input, context) {
      const params=JSON.parse(JSON.stringify(input));validate(params);
      if(isBusy()||runner.running)throw fail("BUSY","The shared manual/AI export runner is busy.");
      let writeStarted=false;
      setBusy(true);
      try {
        const result=await runner.runPrepared(async()=>{
          const doc=getActiveDocument();
          const assertTarget=async()=>{
            if(!doc||getActiveDocument()?.id!==params.document.id||doc.id!==params.document.id||
                normalizePath(getDocumentPath(doc))!==normalizePath(params.document.path))
              throw fail("DOCUMENT_MISMATCH","The active Photoshop document no longer matches this request.");
            try {await assertSaved(doc,"bridge-target");} catch(error) {error.bridgeRejected=true;throw error;}
          };
          await assertTarget();
          const [sourcePsdFile,outputFolder,namingRules,exportSettings]=await Promise.all([
            getFile(params.document.path),getFolder(folderPath(params.document.path)),getRules(),getSettings()]);
          if(!sourcePsdFile||!outputFolder)throw fail("INTERACTION_REQUIRED","Authorize the saved PSD folder in Photoshop before retrying with a new request.");
          if(normalizePath(sourcePsdFile.nativePath)!==normalizePath(params.document.path)||normalizePath(outputFolder.nativePath)!==normalizePath(folderPath(params.document.path)))
            throw fail("DOCUMENT_MISMATCH","Resolved PSD or output Entry differs from the request.");
          if(await hash(encode(JSON.stringify(namingRules)))!==params.options.namingRulesSha256||await hash(encode(JSON.stringify(exportSettings)))!==params.options.exportSettingsSha256)
            throw fail("OPTIONS_MISMATCH","Plugin naming/export settings changed; inspect the current settings.");
          const options=params.options;
          return {document:doc,sourcePsdFile,outputFolder,namingRules,exportSettings,
            expectedSourcePsdSha256:params.document.sha256,expectedTargetIds:params.expectedTargetIds,
            exportBaseName:options.exportBaseName,sliceFileNamePrefix:options.sliceFileNamePrefix,reusePolicy:options.reusePolicy,skipAssetsExport:false,
            assertSourceBeforeModal:assertTarget,
            beforeFirstWrite:assertTarget,
            assertCanStart:()=>context.assertCanStart(),
            onWriteStarted:()=>{writeStarted=true;},
            confirmOverwrite:async(conflicts,buiFileName,assetsRootName,{overwrite})=>{
              if(!params.outputPolicy.overwrite)throw fail("OVERWRITE_DECISION_REQUIRED","Review the current output snapshot and submit a new explicitly authorized request.",{conflicts,overwrite});
              if(params.outputPolicy.overwrite.decisionSha256!==overwrite.decisionSha256)throw fail("OVERWRITE_DECISION_STALE","Source, options or outputs changed after the recorded decision.",{conflicts,overwrite});
              return true;
            }};
        });
        feedback(result);
        return {...result,pluginVersion:version,document:params.document,options:params.options,outputDirectory:folderPath(params.document.path),
          requestId:context.requestId,sessionId:context.sessionId,partialOutputPossible:result.productSaved!==true&&writeStarted};
      } catch(error) {
        error.partialOutputPossible=writeStarted;
        error.outputDirectory=folderPath(params.document.path);
        if(!error.code)error.code=String(error.message||error).match(/^([A-Z][A-Z0-9_]+):/)?.[1]||"EXPORT_FAILED";
        throw error;
      } finally {setBusy(false);}
    }
    return {exportDocument};
  }
  const api={create,validate};if(typeof module!=="undefined"&&module.exports)module.exports=api;
  global.BurnerPsdToBuiBridgeExport=api;
})(globalThis);
