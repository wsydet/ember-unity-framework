// Copyright (c) 2026 Burner Games. All rights reserved.
// This file is part of Burner Unity Packages. Package: com.burner.ui.prefabmaker
(function registerBridgeHost(global) {
  "use strict";
  function create({fs, formats, version, handlers, hash, nonce, getWorkingDocument, getFolder,
      getWorkingPaths = async () => [], isBusy = () => false, status = () => {}, schedule = setTimeout, cancel = clearTimeout}) {
    let settings = {version: 2, enabled: true}, bridge = null, store = null, timer = null;
    let directory = "", routing = false, closed = false, legacyChecked = false;
    const sessionId = nonce();
    const fail = code => Object.assign(new Error(code), {code});
    function report(state, error, pending = []) {
      status({state, enabled: settings?.enabled === true, directory, sessionId, pendingRequestIds: pending, error: error?.message || ""});
    }
    async function save() {
      const data = await fs.getDataFolder();
      const temporary = await data.createFile(".bridge-settings-" + nonce() + ".tmp", {overwrite: false});
      await temporary.write(JSON.stringify(settings), {format: formats.utf8});
      await temporary.moveTo(data, {newName: "bridge-settings.json", overwrite: true});
    }
    async function heartbeat(state, error) {
      if (store) await store.publishHost({version: 1, pluginVersion: version, sessionId, state,
        observedAt: new Date().toISOString(), operations: Object.keys(handlers), error: error?.message || null});
    }
    async function detach({preservePending = false} = {}) {
      if (bridge?.busy || isBusy()) throw fail("BUSY");
      // Disabling polling must not forget a site already known to this host.
      // No route is persisted in PluginData; enable rechecks the retained store.
      if (preservePending && store && (await store.pendingWrites()).length) {
        await heartbeat("disabled");
        return;
      }
      if (bridge) bridge.stop();
      bridge = null;
      try {await heartbeat("disabled");} finally {store = null; directory = "";}
      legacyChecked = false;
    }
    async function connect(root) {
      if (!root?.isFolder) throw fail("INVALID_BRIDGE_DIRECTORY");
      store = await global.BurnerPsdToBuiBridgeStore.create({root, formats, nonce, hash});
      directory = root.nativePath;
      bridge = global.BurnerPsdToBuiFileBridge.create({store, sessionId, handlers, hash,
        onError: (code, message) => report("error", new Error(code + ": " + message))});
      await heartbeat("ready");
      legacyChecked = true;
    }
    async function child(parent, name) {
      const found = (await parent.getEntries()).find(item => item.name === name);
      if (found && !found.isFolder) throw fail("INVALID_BRIDGE_DIRECTORY");
      return found || parent.createFolder(name);
    }
    async function originalOpenSite() {
      // Only inspect directories of documents already open in Photoshop. No saved route or disk search.
      const parents = new Set((await getWorkingPaths()).filter(path => /\.ps[db]$/i.test(path))
        .map(path => path.replace(/\\/g, "/")).map(path => path.slice(0, path.lastIndexOf("/"))));
      for (const parent of parents) {
        let root = await getFolder(parent);
        for (const name of [".local", "maker", "bridge"]) {
          root = (await root.getEntries()).find(item => item.name === name);
          if (!root) break;
          if (!root.isFolder) throw fail("INVALID_BRIDGE_DIRECTORY");
        }
        if (!root) continue;
        const existing = await global.BurnerPsdToBuiBridgeStore.create({root, formats, nonce, hash, readonly: true});
        if ((await existing.pendingWrites()).length) return root;
      }
      return null;
    }
    async function route() {
      if (!settings.enabled || closed) return;
      if (isBusy() || bridge?.busy) {report("busy"); return;}
      // Legacy settings only keep the original unresolved site reachable.
      if (!legacyChecked) {
        const old = settings.version === 1 && (settings.token || settings.directoryUrl)
          ? settings.token ? await fs.getEntryForPersistentToken(settings.token) : await fs.getEntryWithUrl(settings.directoryUrl)
          : await originalOpenSite();
        if (old) await connect(old);
        legacyChecked = true;
      }
      const pending = store ? await store.pendingWrites() : [];
      if (pending.length) {report("waiting-original-request", null, pending); return;}
      if (settings.version === 1) {
        settings = {version: 2, enabled: settings.enabled};
        await save();
      }
      const document = await getWorkingDocument();
      if (!document?.path || document.saved !== true) {
        if (bridge) await detach();
        report(document?.path ? "waiting-saved-document" : "waiting-document");
        return;
      }
      const normalized = document.path.replace(/\\/g, "/");
      const parent = normalized.slice(0, normalized.lastIndexOf("/"));
      if (!parent || !/\.ps[db]$/i.test(normalized)) throw fail("INVALID_WORKING_PSD_PATH");
      const target = parent + "/.local/maker/bridge";
      if (directory.replace(/\\/g, "/") === target) {report("ready"); return;}
      if (bridge) await detach();
      const root = await child(await child(await child(await getFolder(parent), ".local"), "maker"), "bridge");
      // Do not route to an export-created temporary document.
      if (isBusy()) throw fail("BUSY");
      const current = await getWorkingDocument();
      if (current?.path !== document.path || current.saved !== true) throw fail("DOCUMENT_MISMATCH");
      await connect(root);
      report("ready");
    }
    function later() {
      if (timer) cancel(timer);
      if (settings.enabled && !closed) timer = schedule(() => {void poll();}, 1000);
    }
    async function poll() {
      if (routing || closed || !settings.enabled) return;
      routing = true;
      try {
        await route();
        if (bridge && !isBusy()) {await bridge.tick(); await heartbeat("ready");}
      } catch (error) {
        report("error", error);
        try {await heartbeat("error", error);} catch (_) { /* Manual export remains independent. */ }
      } finally {routing = false; later();}
    }
    async function initialize() {
      try {
        const data = await fs.getDataFolder();
        const file = (await data.getEntries()).find(item => item.name === "bridge-settings.json");
        if (file) settings = JSON.parse(await file.read({format: formats.utf8}));
        if (![1, 2].includes(settings?.version) || typeof settings.enabled !== "boolean")
          throw fail("INVALID_BRIDGE_SETTINGS");
        if (settings.enabled) await poll(); else report("disabled");
      } catch (error) {report("error", error);} // Optional entry cannot block the manual panel.
    }
    async function enable() {
      if (routing || bridge?.busy || isBusy()) throw fail("BUSY");
      closed = false;
      settings.enabled = true;
      await save();
      await poll();
    }
    async function disable() {
      if (routing || bridge?.busy || isBusy()) throw fail("BUSY");
      if (timer) cancel(timer);
      timer = null;
      await detach({preservePending: true});
      settings.enabled = false;
      await save();
      report("disabled");
    }
    async function stop() {
      if (routing || bridge?.busy || isBusy()) throw fail("BUSY");
      closed = true;
      if (timer) cancel(timer);
      timer = null;
      await detach({preservePending: true});
    }
    async function toggle() {return settings.enabled ? disable() : enable();}
    return {initialize, enable, disable, toggle, stop, poll, sessionId};
  }
  const api = {create};
  if (typeof module !== "undefined" && module.exports) module.exports = api;
  global.BurnerPsdToBuiBridgeHost = api;
})(globalThis);
