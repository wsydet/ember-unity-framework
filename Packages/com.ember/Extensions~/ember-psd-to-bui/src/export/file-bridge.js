// Copyright (c) 2026 Burner Games. All rights reserved.
// This file is part of Burner Unity Packages. Package: com.burner.ui.prefabmaker
(function registerFileBridge(global) {
  "use strict";
  const VERSION = 1;
  const MAX_REQUEST_BYTES = 65536;
  const ID = /^[a-f0-9]{32}$/;
  const allowed = new Set(["version", "requestId", "operation", "createdAt", "startBefore", "sessionId", "params"]);
  const error = (code, message) => Object.assign(new Error(message || code), {code});
  function validate(request, id, now) {
    if (!request || typeof request !== "object" || Array.isArray(request) ||
        Object.keys(request).some(key => !allowed.has(key))) throw error("INVALID_REQUEST");
    if (request.version !== VERSION) throw error("PROTOCOL_MISMATCH");
    if (!ID.test(id) || request.requestId !== id) throw error("REQUEST_ID_MISMATCH");
    const created = Date.parse(request.createdAt), deadline = Date.parse(request.startBefore);
    if (!Number.isFinite(created) || !Number.isFinite(deadline) || deadline <= created ||
        deadline - created > 600000 || created > now + 120000) throw error("INVALID_DEADLINE");
    if (now >= deadline) throw error("START_DEADLINE_EXPIRED");
    if (!request.params || typeof request.params !== "object" || Array.isArray(request.params)) throw error("INVALID_PARAMS");
  }

  // The store publishes immutable claim/result files. A claim without a terminal
  // result is never executed again, including after a plugin restart.
  function create({store, sessionId, handlers, hash, now = Date.now, onError = () => {}}) {
    if (!ID.test(sessionId) || !store || typeof hash !== "function") throw error("INVALID_BRIDGE_CONFIG");
    let polling = false, stopped = false, cursor = 0;
    async function publish(id, requestHash, operation, status, value, failure) {
      const result = {version: VERSION, requestId: id, requestHash, sessionId, operation,
        status, completedAt: new Date(now()).toISOString(), productSaved: status === "unknown" ? null : value?.productSaved === true,
        ...(value === undefined ? {} : {value}), ...(failure ? {error: failure} : {})};
      try { await store.publishResult(id, result); }
      catch (failure) { throw Object.assign(failure, {bridgePublicationFailure: true}); }
      return result;
    }
    async function process(id) {
      const input = await store.readRequest(id, MAX_REQUEST_BYTES);
      const requestHash = await hash(input.bytes);
      const previous = await store.readResult(id);
      if (previous) {
        if (previous.requestHash !== requestHash) throw error("REQUEST_ID_CONFLICT", id);
        return previous;
      }
      const claim = await store.readClaim(id);
      if (claim) {
        if (claim.requestHash !== requestHash) throw error("REQUEST_ID_CONFLICT", id);
        return publish(id, requestHash, claim.operation, "unknown", undefined,
          {code: "RECOVERY_REQUIRED", message: "A claimed request has no terminal result; query and inspect it, do not replay."});
      }
      let request;
      try {
        request = JSON.parse(input.text);
        validate(request, id, now());
        if (!Object.prototype.hasOwnProperty.call(handlers, request.operation)) throw error("UNSUPPORTED_OPERATION");
        if (request.operation !== "inspect" && request.sessionId !== sessionId) throw error("SESSION_MISMATCH");
      } catch (failure) {
        return publish(id, requestHash, request?.operation || null, "rejected", undefined,
          {code: failure.code || "INVALID_JSON", message: String(failure.message || failure).slice(0, 2048)});
      }
      // The store returns false when another actor has already published this claim.
      // Do not turn a racing claim into a second execution.
      const claimed = await store.claim(id, {version: VERSION, requestId: id, requestHash,
        operation: request.operation, sessionId, claimedAt: new Date(now()).toISOString()});
      if (!claimed) return null;
      try {
        // Recheck the deadline immediately before entering the host handler.
        validate(request, id, now());
        if (stopped) throw error("BRIDGE_STOPPED");
        const value = await handlers[request.operation](request.params, {sessionId, requestId: id,
          assertCanStart: () => {validate(request, id, now()); if (stopped) throw error("BRIDGE_STOPPED");}});
        return await publish(id, requestHash, request.operation, value?.status === "cancelled" ? "cancelled" : "completed", value);
      } catch (failure) {
        // A failed publication must leave the claim unresolved. It must not produce
        // a second terminal result or imply the handler did not run.
        if (failure?.bridgePublicationFailure) throw failure;
        return publish(id, requestHash, request.operation, failure.exportCancelled ? "cancelled" : failure.bridgeRejected ? "rejected" : "failed", undefined,
          {code: failure.code || "HOST_ERROR", message: String(failure.message || failure).slice(0, 65536),
            details: failure.details || null, partialOutputPossible: failure.partialOutputPossible === true, outputDirectory: failure.outputDirectory || null});
      }
    }
    async function tick() {
      if (polling || stopped) return {status: stopped ? "stopped" : "busy"};
      polling = true;
      let processed = 0;
      try {
        const ids = (await store.listRequests()).filter(id => ID.test(id)).sort();
        const start = ids.length ? cursor % ids.length : 0;
        const batch = ids.slice(start).concat(ids.slice(0, start)).slice(0, 32);
        cursor = start + batch.length;
        for (const id of batch) {
          if (stopped) break;
          try { await process(id); processed++; }
          catch (failure) { onError(failure.code || "BRIDGE_IO_ERROR", String(failure.message || failure)); }
        }
        return {status: "ready", processed};
      } finally { polling = false; }
    }
    return {tick, stop: () => {stopped = true;}, get busy() {return polling;}, sessionId};
  }
  const api = {VERSION, MAX_REQUEST_BYTES, ID, validate, create};
  if (typeof module !== "undefined" && module.exports) module.exports = api;
  global.BurnerPsdToBuiFileBridge = api;
})(globalThis);
