// Copyright (c) 2026 Burner Games. All rights reserved.
// This file is part of Burner Unity Packages. Package: com.burner.ui.prefabmaker
(function registerBridgeStore(global) {
  "use strict";
  const ID = /^[a-f0-9]{32}$/;
  const fail = (code, message = code) => Object.assign(new Error(message), {code});
  function decode(bytes) {
    return decodeURIComponent(Array.from(bytes, value => "%" + value.toString(16).padStart(2, "0")).join(""));
  }
  async function create({root, formats, nonce, hash, readonly = false}) {
    if (!root?.isFolder || typeof nonce !== "function") throw fail("INVALID_BRIDGE_DIRECTORY");
    // getEntries distinguishes an absent file from permission / I/O errors.
    const entry = async (folder, name) => (await folder.getEntries()).find(item => item.name === name) || null;
    const folder = async name => {
      const found = await entry(root, name);
      if (found && !found.isFolder) throw fail("INVALID_BRIDGE_DIRECTORY", name);
      if (!found && readonly) throw fail("INVALID_BRIDGE_DIRECTORY", name);
      return found || root.createFolder(name);
    };
    const requests = await folder("requests"), claims = await folder("claims"), results = await folder("results");
    async function read(folder, name, maximum) {
      const file = await entry(folder, name);
      if (!file) return null;
      if (!file.isFile) throw fail("BRIDGE_EXPECTED_FILE", name);
      const metadata = await file.getMetadata();
      if (metadata.size > maximum) throw fail("BRIDGE_FILE_TOO_LARGE", name);
      const bytes = new Uint8Array(await file.read({format: formats.binary}));
      if (bytes.byteLength > maximum) throw fail("BRIDGE_FILE_TOO_LARGE", name);
      let text;
      try {text = decode(bytes);} catch (_) {text = "";}
      return {bytes, text};
    }
    async function json(folder, name) {
      const value = await read(folder, name, 64 * 1024 * 1024);
      return value ? JSON.parse(value.text) : null;
    }
    async function publish(folder, name, value, overwrite = false) {
      if (readonly) throw fail("READ_ONLY_BRIDGE");
      const temporaryName = "." + name + "." + nonce() + ".tmp";
      const temporary = await folder.createFile(temporaryName, {overwrite: false});
      let moved = false;
      try {
        await temporary.write(JSON.stringify(value), {format: formats.utf8});
        // UXP Entry publication is exercised in Photoshop; do not assume Node's rename semantics.
        await temporary.moveTo(folder, {newName: name, overwrite});
        moved = true;
        const actual = await json(folder, name);
        if (JSON.stringify(actual) !== JSON.stringify(value)) throw fail("BRIDGE_PUBLICATION_MISMATCH", name);
      } finally {
        if (!moved && await entry(folder, temporaryName)) await temporary.delete();
      }
    }
    const name = id => {
      if (!ID.test(id)) throw fail("INVALID_REQUEST_ID");
      return id + ".json";
    };
    return {
      listRequests: async () => (await requests.getEntries()).filter(item => item.isFile && /^[a-f0-9]{32}\.json$/.test(item.name)).map(item => item.name.slice(0, -5)),
      readRequest: async (id, maximum) => {
        const value = await read(requests, name(id), maximum);
        if (!value) throw fail("REQUEST_NOT_FOUND", id);
        return value;
      },
      readClaim: id => json(claims, name(id)),
      readResult: id => json(results, name(id)),
      pendingWrites: async () => {
        const entries = [...await requests.getEntries(), ...await claims.getEntries()];
        const ids = [...new Set(entries.filter(item => item.isFile && /^[a-f0-9]{32}\.json$/.test(item.name)).map(item => item.name.slice(0, -5)))];
        const pending = [];
        for (const id of ids) {
          const request = await json(requests, name(id)), claim = await json(claims, name(id));
          if (request?.operation !== "export" && claim?.operation !== "export") continue;
          const result = await json(results, name(id));
          if (!request) {pending.push(id); continue;}
          if (typeof hash !== "function") throw fail("INVALID_BRIDGE_CONFIG");
          const requestHash = await hash((await read(requests, name(id), 65536)).bytes);
          if (claim && (claim.requestId !== id || claim.requestHash !== requestHash)) throw fail("REQUEST_ID_CONFLICT", id);
          if (!result || result.status === "unknown") pending.push(id);
          else if (result.requestId !== id || result.operation !== "export" ||
              result.requestHash !== requestHash || !["completed", "cancelled", "rejected", "failed"].includes(result.status)
              || result.status !== "rejected" && (!claim || claim.sessionId !== result.sessionId)) throw fail("INVALID_BRIDGE_RESULT", id);
        }
        return pending;
      },
      claim: async (id, value) => {
        if (await entry(claims, name(id))) return false;
        try {await publish(claims, name(id), value); return true;}
        catch (error) {if (await entry(claims, name(id))) return false; throw error;}
      },
      publishResult: (id, value) => publish(results, name(id), value),
      publishHost: value => publish(root, "host.json", value, true)
    };
  }
  const api = {create, decode};
  if (typeof module !== "undefined" && module.exports) module.exports = api;
  global.BurnerPsdToBuiBridgeStore = api;
})(globalThis);
