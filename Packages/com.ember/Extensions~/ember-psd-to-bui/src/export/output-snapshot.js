// Copyright (c) 2026 Burner Games. All rights reserved.
// This file is part of Burner Unity Packages. Package: com.burner.ui.prefabmaker
(function registerOutputSnapshot(global) {
  "use strict";
  const fail = message => Object.assign(new Error("EXPORT_OUTPUT_CHANGED: " + message), {code:"EXPORT_OUTPUT_CHANGED"});
  function select(children, name) {
    const matches=children.filter(item=>item.name.toLowerCase()===name.toLowerCase());
    if(matches.length>1) throw fail("Ambiguous case variants for output: " + name);
    return matches[0]||null;
  }
  async function resolve(folder,name) {return select(await folder.getEntries(),name);}
  async function capture({folder, names, formats, hash, encode}) {
    const entries=[], resolved=new Map();
    async function walk(entry, relative, depth, requestedPath) {
      if (depth>32 || entries.length>=20000) throw fail("Output snapshot exceeds supported limits");
      const identity={path:relative,...(requestedPath===undefined?{}:{requestedPath})};
      if (entry.isFolder) {
        entries.push({...identity,kind:"directory"});
        const children=await entry.getEntries(),folded=new Set();
        for(const child of children) {if(folded.has(child.name.toLowerCase()))throw fail("Ambiguous case variants in output directory: "+relative);folded.add(child.name.toLowerCase());}
        for (const child of children.sort((a,b)=>a.name<b.name?-1:a.name>b.name?1:0))
          await walk(child,relative+"/"+child.name,depth+1);
      } else {
        const bytes=await entry.read({format:formats.binary});
        entries.push({...identity,kind:"file",bytes:bytes.byteLength,sha256:await hash(bytes)});
      }
    }
    const children=await folder.getEntries();
    for (const name of [...new Set(names)].sort()) {
      const entry=select(children,name);resolved.set(name,entry);
      if(entry) await walk(entry,entry.name,0,name); else entries.push({path:name,requestedPath:name,kind:"missing"});
    }
    const snapshot={entries,sha256:await hash(encode(JSON.stringify(entries)))};
    // Native Entries are used by the current operation only, never serialized into
    // the immutable decision. Their real names and bytes are in the signed entries.
    Object.defineProperty(snapshot,"resolvedEntries",{value:resolved});
    return snapshot;
  }
  async function unchanged(options, expected) {
    if((await capture(options)).sha256!==expected.sha256) throw fail("BUI, PNG or text-reference output changed after the overwrite decision; inspect it again before a new request.");
  }
  const api={capture,unchanged,resolve};
  if(typeof module!=="undefined"&&module.exports)module.exports=api;
  global.BurnerPsdToBuiOutputSnapshot=api;
})(globalThis);
