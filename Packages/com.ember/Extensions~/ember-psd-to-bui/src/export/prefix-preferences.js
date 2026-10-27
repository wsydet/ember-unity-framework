/* global globalThis */
// Copyright (c) 2026 Burner Games. All rights reserved.
// This file is part of Burner Unity Packages. Package: com.burner.ui.prefabmaker
(function registerPrefixPreferences(global) {
  const valid = value => typeof value === "string" && /^[A-Za-z0-9_]{0,32}$/.test(value);
  const key = value => String(value || "").replace(/\\/g, "/").replace(/\/+$/, "").toLowerCase();
  function createStore({ fs, formats }) {
    let loaded, folder, state, writes = Promise.resolve();
    async function load() {
      if (!loaded) loaded = (async () => {
        folder = await fs.getDataFolder();
        const file = (await folder.getEntries()).find(e => e.name === "export-prefixes.json");
        state = file ? JSON.parse(await file.read({format:formats.utf8})) : {version:1,byPsd:{}};
        if (state.version !== 1 || !state.byPsd || typeof state.byPsd !== "object" || Array.isArray(state.byPsd) ||
            Object.values(state.byPsd).some(value => !valid(value))) throw Error("PREFIX_SETTINGS_INVALID");
        return state;
      })();
      return loaded;
    }
    async function resolve(context) {
      let warning = null;
      try {
        await writes.catch(() => {});
        await load();
        if (Object.prototype.hasOwnProperty.call(state.byPsd, key(context.psdPath)))
          return {prefix:state.byPsd[key(context.psdPath)],source:"saved"};
      } catch (error) { warning = String(error.message || error); }
      if (context.folder && context.buiName) {
        try {
          const file = (await context.folder.getEntries()).find(e => e.name === context.buiName);
          if (file && !file.isFolder) {
            const bui = JSON.parse((await file.read({format:formats.utf8})).replace(/^\uFEFF/, ""));
            const name = key(context.psdPath).split("/").pop();
            if (key(bui.source?.psdName) === name && valid(bui.source?.sliceFileNamePrefix))
              return {prefix:bui.source.sliceFileNamePrefix,source:"bui",warning};
          }
        } catch (error) { warning = [warning,String(error.message || error)].filter(Boolean).join("; "); }
      }
      return {prefix:valid(context.defaultPrefix)?context.defaultPrefix:"UI_",source:"default",warning};
    }
    function save(psdPath, prefix) {
      const operation = writes.catch(() => {}).then(async () => {
        if (!psdPath || !valid(prefix)) throw Error("PREFIX_INPUT_INVALID");
        await load();
        const next = {version:1,byPsd:{...state.byPsd,[key(psdPath)]:prefix}};
        const json = JSON.stringify(next, null, 2);
        const file = await folder.createFile("export-prefixes.json", {overwrite:true});
        await file.write(json, {format:formats.utf8});
        if (await file.read({format:formats.utf8}) !== json) throw Error("PREFIX_SETTINGS_READBACK_FAILED");
        state = next;
      });
      writes = operation;
      return operation;
    }
    return {resolve,save};
  }

  // Prevent delayed file reads from replacing newer user input or another document's prefix.
  function createController({store,context,read,write,status,changed}) {
    let activeKey = "", editedPsd = "", revision = 0, pending = Promise.resolve();
    const identity = c => [key(c.psdPath),key(c.folder?.nativePath || ""),c.buiName].join("|");
    function update() {
      const c = context(), currentKey = identity(c);
      if (currentKey === activeKey) return pending;
      activeKey = currentKey;
      if (editedPsd && editedPsd === key(c.psdPath)) return Promise.resolve();
      editedPsd = "";
      const captured = ++revision;
      if (!c.psdPath) return Promise.resolve();
      pending = store.resolve(c).then(result => {
        if (captured !== revision || identity(context()) !== currentKey) return;
        write(result.prefix); changed();
        status(result.warning ? "prefixReadFailed" : "prefixRestored" + result.source, result.warning || "");
      }).catch(error => { if(captured===revision)status("prefixReadFailed",String(error.message||error)); });
      return pending;
    }
    function edited() {
      const c=context(), captured=++revision;
      activeKey=identity(c);
      editedPsd=key(c.psdPath);
      const prefix=read();
      if (!valid(prefix)) {status("prefixInvalid","");return Promise.resolve();}
      if (!c.psdPath) {status("prefixUnsavedPsd","");return Promise.resolve();}
      status("prefixSaving","");
      return store.save(c.psdPath,prefix).then(()=>{
        if(captured===revision)status("prefixSaved","");
      }).catch(error=>{if(captured===revision)status("prefixSaveFailed",String(error.message||error));});
    }
    return {update,edited};
  }
  global.BurnerPsdToBuiPrefixPreferences={createStore,createController};
})(globalThis);
