/* global globalThis */
(function registerResourceReuse(global) {
  const NS = "https://burnergames.net/ns/ui-workflow/1.0/";
  const HASH = /^[a-f0-9]{64}$/;
  function check(value, message) { if (!value) throw new Error(`RESOURCE_REUSE_INVALID: ${message}`); }
  function normalizeHash(value) {
    check(typeof value === "string" && /^[a-f0-9]{64}$/i.test(value), "Invalid snapshot SHA256");
    return value.toLowerCase();
  }
  function fields(value, names) {
    const keys = names.split(" ");
    check(value && typeof value === "object" && !Array.isArray(value) &&
      keys.every(k => Object.prototype.hasOwnProperty.call(value, k)) && Object.keys(value).every(k => keys.includes(k)), `Missing/unsupported fields: ${names}`);
  }
  function text(value) { return typeof value === "string" && value.trim().length > 0; }
  function positive(value) { return Number.isSafeInteger(value) && value > 0; }
  function layerId(value) { return positive(value) || (typeof value === "string" && /^[1-9][0-9]*$/.test(value)); }
  function clone(value) { return JSON.parse(JSON.stringify(value)); }
  function key(c) { return JSON.stringify([c.targetId, c.nodeId, c.kind, c.slot, c.element]); }
  function consumer(targetId, nodeId, kind = "node", slot = "", element = "") { return { targetId, nodeId, kind, slot, element }; }
  function isStatic(r) { return r && ["Atlas", "StaticTexture"].includes(r.resourceType); }
  function validateDeclaration(d) {
    fields(d, "schemaVersion kind id revision workflowId renameRecordId renameRevision workingPsd state requirements" + (d?.schemaVersion >= 2 ? " sharedGroups" : "") + (d?.schemaVersion === 3 ? " adaptation" : ""));
    check([1, 2, 3].includes(d.schemaVersion) && d.kind === "burner-resource-reuse", "Unsupported declaration version/kind");
    if (d.schemaVersion === 3) {
      fields(d.adaptation, "version targets");
      check(d.adaptation.version === 1 && d.adaptation.targets && Object.keys(d.adaptation.targets).length > 0, "Saved adaptation is required by declaration v3");
    }
    check([d.id, d.workflowId, d.renameRecordId].every(text) && positive(d.revision) && positive(d.renameRevision), "Invalid declaration identity/revision");
    fields(d.workingPsd, "path sha256");
    check(text(d.workingPsd.path) && !/[:\\]/.test(d.workingPsd.path) && d.workingPsd.path.split("/").every(p => p && p !== "." && p !== "..") && HASH.test(d.workingPsd.sha256), "Invalid PSD path/hash");
    const shared = d.schemaVersion >= 2 ? d.sharedGroups : [];
    check(Array.isArray(shared), "Invalid shared groups");
    check(Array.isArray(d.requirements) && ((d.state === "none" && !d.requirements.length && !shared.length) || (d.state === "required" && (d.requirements.length || shared.length))), "Invalid explicit reuse state");
    const members = new Set(), groupIds = new Set();
    for (const g of shared) {
      fields(g, "id sourceLayerId memberLayerIds targetLayerId outputModule expected");
      check(HASH.test(g.id) && !groupIds.has(g.id), "Invalid/duplicate shared group identity"); groupIds.add(g.id);
      check(layerId(g.sourceLayerId) && layerId(g.targetLayerId) && /^[A-Za-z][A-Za-z0-9]*$/.test(g.outputModule), "Invalid shared scope");
      check(Array.isArray(g.memberLayerIds) && g.memberLayerIds.length >= 2 && g.memberLayerIds.map(String).includes(String(g.sourceLayerId)), "Shared source must belong to its members");
      for (const id of g.memberLayerIds) {
        check(layerId(id) && !members.has(String(id)) && !d.requirements.some(r => String(r.resultLayerId) === String(id)), "Shared member is invalid or claimed twice"); members.add(String(id));
      }
      const e = g.expected; fields(e, "width height visibleRgbaSha256 border resourceType");
      check(positive(e.width) && positive(e.height) && HASH.test(e.visibleRgbaSha256) && isStatic(e), "Invalid shared pixels/type");
      fields(e.border, "left bottom right top");
      check(Object.values(e.border).every(n => Number.isSafeInteger(n) && n >= 0) && e.border.left + e.border.right < e.width && e.border.top + e.border.bottom < e.height, "Invalid shared border");
    }
    const ids = new Set();
    for (const r of d.requirements) {
      fields(r, "id required targetLayerId sourceLayerId resultLayerId kind instanceLayerId slot element candidate");
      check(text(r.id) && !ids.has(r.id), "Duplicate/missing requirement identity"); ids.add(r.id);
      check(r.required === true && [r.targetLayerId, r.sourceLayerId, r.resultLayerId].every(layerId), "Missing actual layer identities or required flag");
      check((r.kind === "node" && r.instanceLayerId === null && r.slot === "" && r.element === "") ||
        (r.kind === "override" && layerId(r.instanceLayerId) && text(r.slot) && text(r.element)), "Invalid consumer scope");
      const c = r.candidate; fields(c, "project assetPath guid resourceType expected");
      fields(c.project, "normalizedProjectPath productGuid");
      check(text(c.project.normalizedProjectPath) && text(c.project.productGuid) && /^Assets\/.+\.png$/i.test(c.assetPath) && /^[a-f0-9]{32}$/i.test(c.guid) && isStatic(c), "Invalid candidate identity/type");
      const e = c.expected; fields(e, "fileSha256 metaSha256 contentHash contentHashAlgorithm width height border importer");
      check([e.fileSha256, e.metaSha256, e.contentHash].every(h => HASH.test(h)) && e.contentHashAlgorithm === "png-normalized-sha256-v1", "Invalid candidate hashes");
      fields(e.border, "left bottom right top");
      check(positive(e.width) && positive(e.height) && Object.values(e.border).every(v => Number.isFinite(v) && v >= 0), "Invalid candidate geometry");
      fields(e.importer, "textureType spriteImportMode mipmapEnabled sRGBTexture alphaIsTransparency filterMode spritePixelsPerUnit");
      check(e.importer.textureType === "Sprite" && e.importer.spriteImportMode === "Single" && e.importer.mipmapEnabled === false &&
        e.importer.sRGBTexture === true && e.importer.alphaIsTransparency === true && e.importer.filterMode === "Bilinear" &&
        Number.isFinite(e.importer.spritePixelsPerUnit) && e.importer.spritePixelsPerUnit > 0, "Unsupported candidate importer");
    }
  }

  function enumerate(bui) {
    const result = [];
    for (const target of bui.targets) {
      for (const node of bui.nodes.filter(n => target.nodeIds.includes(n.id))) {
        if (["Image", "Button"].includes(node.type) && isStatic(node.resource))
          result.push({ consumer: consumer(target.targetId, node.id), resource: node.resource, node, target, layerId: node.source?.layerId });
        for (const o of node.prefabRef?.overrides || []) if (isStatic(o.resource))
          result.push({ consumer: consumer(target.targetId, node.id, "override", o.slot, o.element), resource: o.resource, node, target, layerId: o.source?.layerId });
      }
    }
    check(new Set(result.map(c => key(c.consumer))).size === result.length, "Ambiguous exported consumers");
    return result;
  }

  function replacement(map, bui) {
    const bindings = [], uses = enumerate(bui), ids = new Set();
    for (const relation of map.relations) {
      const fresh = relation.newLayers, old = relation.oldNodes || [];
      check(text(relation.id) && !ids.has(relation.id), "Replacement relation identity is missing/ambiguous");
      ids.add(relation.id);
      check(Array.isArray(fresh) && Array.isArray(old), "Replacement relation sides must be arrays");
      const valid = { add: fresh.length > 0 && old.length === 0, delete: old.length > 0 && fresh.length === 0,
        replace: fresh.length > 0 && old.length > 0, move: fresh.length > 0 && old.length > 0, keep: old.length > 0 };
      check(valid[relation.intent] === true, "Replacement relation sides conflict with intent: " + relation.id);
      check(old.every(n => text(n.globalObjectId)), "Replacement relation has no old object identity: " + relation.id);
      for (const layer of fresh) {
        check(layerId(layer.resultLayerId), "Replacement relation has invalid result layer: " + relation.id);
        const nodes = bui.nodes.filter(n => String(n.source?.layerId) === String(layer.resultLayerId)).map(n => ({
          layerId: n.source.layerId, nodeId: n.id, targetIds: bui.targets.filter(t => t.nodeIds.includes(n.id)).map(t => t.targetId), resource: n.resource || null }));
        const overrides = uses.filter(u => u.consumer.kind === "override" && String(u.layerId) === String(layer.resultLayerId))
          .map(u => ({ layerId: u.layerId, consumer: u.consumer, resource: u.resource }));
        const resources = (bui.resources || []).filter(r => String(r.source?.layerId) === String(layer.resultLayerId));
        check(nodes.length + overrides.length + resources.length > 0, `Replacement relation ${relation.id} has no actual exported result layer ${layer.resultLayerId}`);
        bindings.push({ relationId: relation.id, sourceLayerId: layer.sourceLayerId, resultLayerId: layer.resultLayerId, nodes, overrides, resources });
      }
    }
    return { schemaVersion: 1, replaceMap: clone(map), replacementBindings: clone(bindings),
      layers: bui.nodes.map(n => ({ layerId: n.source?.layerId ?? null, nodeId: n.id,
        targetIds: bui.targets.filter(t => t.nodeIds.includes(n.id)).map(t => t.targetId), resource: n.resource || null })) };
  }

  // Explicit replacement bindings cannot opt ignored subtrees back into export.
  async function collectRetainedTexts(map, doc, bui, readTextInfo, warnings, namingRules) {
    if (!map) return [];
    const layers = new Map(), selectedLayers = new Set(), selectedObjects = new Set(), result = [];
    function index(items, parent = "", visible = true) {
      for (const layer of items || []) {
        if (global.BurnerPsdToBuiDesignTexts.isIgnored(layer, namingRules)) continue;
        const path = parent ? `${parent}/${layer.name}` : layer.name;
        const id = String(layer.id), shown = visible && layer.visible !== false;
        check(!layers.has(id), "Duplicate PSD layer identity: " + id);
        layers.set(id, { layer, path, visible: shown }); index(layer.layers, path, shown);
      }
    }
    index(doc.layers);
    const exported = new Set();
    function collect(value) {
      if (!value || typeof value !== "object") return;
      if (value.text?.source?.layerId != null) exported.add(String(value.text.source.layerId));
      for (const child of Object.values(value)) if (child && typeof child === "object") collect(child);
    }
    collect(bui.nodes);
    for (const relation of map.relations || []) {
      if (relation.textStyleBindings == null) continue;
      check(Array.isArray(relation.textStyleBindings) && ["keep", "replace", "move"].includes(relation.intent), "Invalid retained text relation: " + relation.id);
      const targets = (map.targets || []).filter(t => t.id === relation.targetId);
      check(targets.length === 1 && /^[a-f0-9]{32}$/i.test(targets[0].prefab?.guid), "Retained text target is missing/ambiguous");
      const target = targets[0], guid = target.prefab.guid.toLowerCase();
      const actual = bui.targets.filter(t => String(bui.nodes.find(n => n.id === t.sourceRootNodeId)?.source?.layerId) === String(target.targetLayerId));
      check(actual.length === 1, "Retained text target is not exported: " + relation.id);
      for (const binding of relation.textStyleBindings) {
        fields(binding, "resultLayerId oldGlobalObjectId");
        check(layerId(binding.resultLayerId), "Invalid retained text layer identity");
        const id = String(binding.resultLayerId), old = binding.oldGlobalObjectId;
        check(typeof old === "string" && new RegExp(`^GlobalObjectId_V1-1-${guid}-[0-9]+-[0-9]+$`).test(old) &&
          (relation.oldNodes || []).filter(n => n.globalObjectId === old).length === 1, "Retained text must bind an exact old object in its relation");
        check(!selectedLayers.has(id) && !selectedObjects.has(old) && !exported.has(id), "Duplicate retained text binding or already exported text: " + id);
        const item = layers.get(id);
        check(item && item.visible && (item.layer.textItem || item.layer.text), "Retained text must be an existing visible native text layer: " + id);
        const textInfo = await readTextInfo(item.layer, item.path, warnings);
        check(textInfo && String(textInfo.source?.layerId) === id, "Retained text source identity unavailable: " + id);
        selectedLayers.add(id); selectedObjects.add(old);
        result.push({ id: `retained_text_${id}`, name: item.layer.name, purpose: "existing-text-style",
          binding: { relationId: relation.id, targetId: actual[0].targetId, prefabGuid: guid, globalObjectId: old }, text: textInfo });
      }
    }
    return result;
  }

  function finalize(input, bui, snapshotId) {
    const uses = enumerate(bui);
    const snapshot = { schemaVersion: 1, state: input.state, snapshotId,
      declarationSha256: input.declarationSha256 || "", renameRecordSha256: input.renameRecordSha256 || "",
      declaration: input.declaration ? clone(input.declaration) : null, bindings: [], consumers: uses.map(u => clone(u.consumer)) };
    if (input.replaceMap) bui.source.replacement = replacement(input.replaceMap, bui);
    const used = new Set();
    for (const r of input.declaration?.requirements || []) {
      const matches = uses.filter(u => {
        const root = bui.nodes.find(n => n.id === u.target.sourceRootNodeId);
        return String(root?.source?.layerId) === String(r.targetLayerId) && String(u.layerId) === String(r.resultLayerId) &&
          u.consumer.kind === r.kind && u.consumer.slot === r.slot && u.consumer.element === r.element &&
          (r.kind === "node" || String(u.node.source?.layerId) === String(r.instanceLayerId));
      });
      check(matches.length === 1, `Requirement ${r.id} must map to exactly one actual consumer`);
      const u = matches[0]; const k = key(u.consumer);
      check(!used.has(k), "Multiple requirements claim one consumer"); used.add(k);
      check(u.resource.resourceType === r.candidate.resourceType && HASH.test(u.resource.sha256) && text(u.resource.asset) &&
        positive(u.resource.width) && positive(u.resource.height), "Invalid exported resource metadata/classification");
      snapshot.bindings.push({ requirementId: r.id, consumer: clone(u.consumer), resultLayerId: r.resultLayerId,
        asset: u.resource.asset, sha256: u.resource.sha256, width: u.resource.width, height: u.resource.height, border: clone(u.resource.border || null) });
    }
    for (const g of input.declaration?.sharedGroups || []) {
      let source = null;
      for (const id of g.memberLayerIds) {
        const matches = uses.filter(u => String(u.layerId) === String(id) && u.consumer.kind === "node" &&
          String(bui.nodes.find(n => n.id === u.target.sourceRootNodeId)?.source?.layerId) === String(g.targetLayerId));
        check(matches.length === 1, "Shared member must map to one actual consumer: " + id);
        const r = matches[0].resource, e = g.expected, b = e.border;
        const width = b.left || b.right ? b.left + Math.min(2, e.width-b.left-b.right) + b.right : e.width;
        const height = b.top || b.bottom ? b.top + Math.min(2, e.height-b.top-b.bottom) + b.bottom : e.height;
        check(r.width === width && r.height === height && r.resourceType === e.resourceType &&
          ["left", "bottom", "right", "top"].every(k => (r.border?.[k] || 0) === b[k]), "Shared resource geometry/type differs");
        if (source) check(r.asset === source.asset && r.sha256 === source.sha256, "Shared members exported different resources");
        source = r;
      }
    }
    return snapshot;
  }

  // UXP 宿主不保证提供 TextDecoder/TextEncoder；解析和摘要使用同一份 UTF-8 字节。
  function decodeUtf8(value) {
    const bytes = ArrayBuffer.isView(value)
      ? new Uint8Array(value.buffer, value.byteOffset, value.byteLength) : new Uint8Array(value);
    const escaped = Array.from(bytes, byte => `%${byte.toString(16).padStart(2, "0")}`).join("");
    return decodeURIComponent(escaped).replace(/^\uFEFF/, "");
  }
  function encodeUtf8(value) {
    const bytes = [];
    encodeURIComponent(value).replace(/%([0-9A-F]{2})|([^%])/g, (_, hex, character) => {
      bytes.push(hex ? parseInt(hex, 16) : character.charCodeAt(0));
      return "";
    });
    return new Uint8Array(bytes);
  }

  function create(options) {
    const writeOutput = options.writeOutput || (operation => operation());
    const { xmp, formats, sha256Hex } = options;
    async function relativeEntry(folder, relative) {
      check(text(relative) && !/[\\:\x00]/.test(relative) && relative.split("/").every(p => p && p !== "." && p !== ".."), "Publication path must stay inside its folder");
      let entry = folder;
      for (const part of relative.split("/")) {
        check(typeof entry.getEntries === "function", "Publication parent folder is missing");
        entry = (await entry.getEntries()).find(e => e.name === part);
        check(entry, "Publication file is missing: " + relative);
      }
      return entry;
    }
    function readManifest(psdFile) {
      if (!xmp?.XMPFile) return null;
      xmp.XMPMeta.registerNamespace(NS, "burnerUI");
      let file;
      try {
        file = new xmp.XMPFile(psdFile.nativePath, xmp.XMPConst.FILE_PHOTOSHOP, xmp.XMPConst.OPEN_FOR_READ);
        const metadata = file.getXMP(), property = metadata?.getProperty?.(NS, "manifest");
        return property ? JSON.parse(String(property.value)) : null;
      } finally { if (file) file.closeFile(); }
    }
    const stable = value => JSON.stringify(sort(value));
    function sort(value) {
      if (Array.isArray(value)) return value.map(sort);
      return value && typeof value === "object" ? Object.fromEntries(Object.keys(value).sort().map(k => [k, sort(value[k])])) : value;
    }
    function same(a, b, message) { check(stable(a) === stable(b), message); }
    async function loadAdaptation(psdFile, folder, source, d, declarationSha256, stem) {
      const adaptationInputs = [];
      async function read(path, expected) {
        const entry = await relativeEntry(folder, path), bytes = await entry.read({ format: formats.binary });
        const sha256 = await sha256Hex(bytes);
        if (expected) check(expected.sha256 === sha256 && (expected.size == null || expected.size === bytes.byteLength), "Adaptation publication hash/size mismatch: " + path);
        adaptationInputs.push({ path, entry, sha256 });
        return { path, sha256, value: JSON.parse(decodeUtf8(bytes)) };
      }
      const manifest = readManifest(psdFile);
      check(manifest?.schemaVersion === 1 && manifest.adaptationVersion === 1 && manifest.workflowId === d.workflowId &&
        manifest.renameRecordId === d.renameRecordId && manifest.renameRevision === d.renameRevision && manifest.reuseState === d.state, "Saved adaptation XMP identity mismatch");
      check(d.workingPsd.path === psdFile.name && d.workingPsd.sha256 === source.psdSha256, "Saved adaptation PSD identity changed; run Rename validation again");
      const recordFile = await read(`${stem}.rename.json`), record = recordFile.value;
      check(record.schemaVersion === 1 && record.kind === "burner-rename-record" && record.status === "complete" &&
        record.id === d.renameRecordId && record.workflowId === d.workflowId && record.revision === d.renameRevision, "Adaptation Rename record identity mismatch");
      same(record.workingPsd, d.workingPsd, "Adaptation record PSD differs");
      same(record.resourceReuse, {state:d.state,path:`${stem}.resource-reuse.json`,id:d.id,revision:d.revision,sha256:declarationSha256}, "Adaptation declaration is not bound to the completed Rename record");
      check(record.publication && HASH.test(record.publication.sha256), "Missing adaptation publication");
      const publication = await read(record.publication.path, record.publication), files = publication.value.files;
      check([1, 2].includes(publication.value.schemaVersion) && files && files["review.json"], "Missing formal adaptation review");
      const reviewSpec = files["review.json"], parent = record.publication.path.split("/").slice(0, -1).join("/");
      const reviewPath = publication.value.schemaVersion === 2 ? reviewSpec.storagePath : [parent, "review.json"].filter(Boolean).join("/");
      const review = await read(reviewPath, reviewSpec);
      check(review.value.status === "complete" && review.value.revision === record.revision && review.value.runId === record.runId, "Adaptation review identity mismatch");
      same(review.value.decisions, record.decisions, "Adaptation review decisions differ from record");
      same(review.value.execution, record.execution, "Adaptation review execution differs from record");
      const execution = record.execution, result = execution?.adaptationResult, original = record.decisions?.adaptation;
      check(result?.version === 1 && original?.version === 1 && result.visualVerification?.status === "verified", "Adaptation has no saved structure/pixel verification");
      same(result.adaptation, d.adaptation, "Declaration adaptation differs from verified saved result");
      const mappings = new Map(), resultIds = new Set();
      check(Array.isArray(result.layerMappings), "Missing adaptation layer mappings");
      for (const item of result.layerMappings) {
        check(layerId(item.sourceLayerId) && layerId(item.resultLayerId) && !mappings.has(String(item.sourceLayerId)) && !resultIds.has(String(item.resultLayerId)), "Ambiguous adaptation layer mappings");
        mappings.set(String(item.sourceLayerId), String(item.resultLayerId)); resultIds.add(String(item.resultLayerId));
      }
      const mapped = id => { check(mappings.has(String(id)), "Unverified adaptation source layer: " + id); return mappings.get(String(id)); };
      const confirmedItems = record.decisions.reviewConfirmationItems || [], selections = record.decisions.reviewDecisions?.selectionRecords || {};
      const mappedTargets = {};
      for (const [id, raw] of Object.entries(original.targets || {})) {
        const target = global.BurnerPsdToBuiAdaptation.summary(raw), savedId = mapped(id), actual = d.adaptation.targets[savedId];
        check(actual && id === raw.sourceLayerId, "Missing adaptation target mapping");
        target.sourceLayerId = savedId;
        for (const role of ["Bg", "Top", "Center", "Bottom"]) target.regions[role] = target.regions[role].map(mapped);
        for (const behavior of target.behaviorSources) {
          if (behavior.source.kind === "user-decision") {
            const matches = confirmedItems.filter(item => item.id === behavior.confirmationId && item.kind === "adaptation-behavior" &&
              item.adaptationInputFingerprint === behavior.inputFingerprint && String(item.targetSourceLayerId) === id && stable(item.behaviorNodeIds) === stable(behavior.nodeIds));
            check(matches.length === 1 && raw.confirmationRefs.includes(behavior.confirmationId), "Adaptation user decision lacks its original review item");
            const item = matches[0], byIdentity = item.selectionIdentity ? Object.values(selections).filter(s => stable(s.selectionIdentity) === stable(item.selectionIdentity)) : [];
            const selection = byIdentity.length === 1 ? byIdentity[0] : byIdentity.length > 1 ? null : selections[item.id];
            check(selection?.choiceId === "confirm-adaptation" && HASH.test(selection.itemFingerprint), "Adaptation user decision was not confirmed in the original review");
          }
          behavior.nodeIds = behavior.nodeIds.map(mapped);
        }
        if (target.targetType === "SubPage") { target.parentTargetId = mapped(target.parentTargetId); target.hostNodeId = mapped(target.hostNodeId); }
        for (const operation of record.decisions.structureOperations || []) {
          if (operation.structureKind !== "adaptation-region" || String(operation.targetLayerId) !== id) continue;
          check(operation.approved === true && operation.action === "create-group" && target.regions[operation.region]?.length === 0, "Unapproved or conflicting saved adaptation region");
          const receipts = (execution.appliedStructures || []).filter(r => r.id === operation.id);
          check(receipts.length === 1 && layerId(receipts[0].resultLayerId) && String(receipts[0].parentLayerId) === savedId, "Missing saved adaptation group receipt");
          same(receipts[0].memberLayerIds.map(String), operation.memberLayerIds.map(String), "Saved adaptation group members differ");
          target.regions[operation.region] = [String(receipts[0].resultLayerId)];
        }
        check(actual.inputIdentity?.psdSha256 === source.psdSha256 && HASH.test(actual.inputIdentity.structureSha256), "Saved adaptation input identity differs");
        target.inputIdentity = clone(actual.inputIdentity);
        mappedTargets[savedId] = target;
        const visual = result.visualVerification.items?.filter(v => String(v.targetLayerId) === savedId);
        check(visual?.length === 1 && HASH.test(visual[0].sourceSha256) && HASH.test(visual[0].resultSha256) &&
          visual[0].difference?.visibleChangedPixels === 0 && visual[0].difference?.alphaChangedPixels === 0, "Adaptation target lacks unchanged saved pixel evidence");
      }
      same({version:1,targets:mappedTargets}, d.adaptation, "Saved adaptation changed an unapproved value, source, geometry or behavior fingerprint");
      const adaptationSource = {version:1,renameRecord:{path:recordFile.path,sha256:recordFile.sha256},
        publication:{path:publication.path,sha256:publication.sha256},review:{path:review.path,sha256:review.sha256}};
      return { adaptation:clone(d.adaptation), adaptationSource, adaptationInputs, record, renameRecordSha256:recordFile.sha256 };
    }
    async function load(psdFile, folder, source, saved, policy = "current-psd", inspection = false) {
      check(psdFile && saved === true, "Save the working PSD before export; unsaved/unknown state cannot be verified");
      check(["current-psd", "retain"].includes(policy), "Unknown reusePolicy");
      const stem = psdFile.name.replace(/\.(psd|psb)$/i, "");
      const warnings = [];
      const entries = await folder.getEntries();
      const reuseFile = entries.find(e => e.name === `${stem}.resource-reuse.json`);
      // Adaptation is a required saved input independently of the image-reuse policy.
      let discovered;
      if (reuseFile) {
        try { discovered = JSON.parse(decodeUtf8(await reuseFile.read({ format: formats.binary }))); } catch (_) { /* retain reports the parse failure below */ }
      }
      const requiredAdaptation = discovered?.schemaVersion === 3;
      if (!requiredAdaptation) {
        const manifest = readManifest(psdFile);
        check(manifest?.adaptationVersion == null, "Saved PSD requires adaptation declaration v3; missing/changed input cannot be discarded by reusePolicy");
      }
      if (policy === "current-psd" && !requiredAdaptation) {
        // Shared source identity is an executable requirement, never a silent suggestion.
        const sharedFile = (await folder.getEntries()).find(e => e.name === `${stem}.resource-reuse.json`);
        if (sharedFile) {
          let d;
          try { d = JSON.parse(decodeUtf8(await sharedFile.read({ format: formats.binary }))); } catch (_) { d = null; }
          check(inspection || !(d?.schemaVersion === 2 && d.sharedGroups?.length), "This PSD has required shared image groups; choose retain to preserve and validate them.");
        }
        warnings.push({ code: "CURRENT_PSD_EXPORT", severity: "Info",
          message: "Export uses the current saved PSD. Historical Rename, reuse and replacement relations are not adopted; choose retain to validate and continue those relations." });
        return { state: "direct-export", declaration: null, warnings };
      }
      // Only the explicitly selected relationships are required, not a historical Rename publication.
      check(reuseFile, "Selected reuse declaration is missing; provide it or explicitly choose current-psd export");
      const bytes = await reuseFile.read({ format: formats.binary });
      const declaration = JSON.parse(decodeUtf8(bytes));
      validateDeclaration(declaration);
      const declarationSha256 = await sha256Hex(bytes);
      const handoff = requiredAdaptation ? await loadAdaptation(psdFile, folder, source, declaration, declarationSha256, stem) : {};
      if (declaration.workingPsd.sha256 !== source.psdSha256)
        warnings.push({ code: "HISTORICAL_PSD_CHANGED", severity: "Info", message: "PSD changed since Rename; selected relationships will be matched against current exported layer identities." });
      let replaceMap = null, replaceFile = entries.find(e => e.name === `${stem}.replace-map.json`), replaceSha256 = "";
      if (replaceFile) {
        const data = await replaceFile.read({ format: formats.binary });
        replaceMap = JSON.parse(decodeUtf8(data)); replaceSha256 = await sha256Hex(data);
        check(replaceMap.schemaVersion === 1 && replaceMap.kind === "burner-replace-map" && Array.isArray(replaceMap.relations), "Selected replacement map invalid");
      }
      return { state: declaration.state, declaration, declarationSha256, renameRecordSha256: "",
        reuseFile, replaceMap, replaceFile, replaceSha256, warnings, ...handoff };
    }
    async function verifyUnchanged(input, psdFile, source, saved) {
      check(saved === true && await sha256Hex(await psdFile.read({ format: formats.binary })) === source.psdSha256, "PSD changed during export");
      if (input.reuseFile) check(await sha256Hex(await input.reuseFile.read({ format: formats.binary })) === input.declarationSha256, "Selected declaration changed during export");
      if (input.replaceFile) check(await sha256Hex(await input.replaceFile.read({ format: formats.binary })) === input.replaceSha256, "Selected replacement map changed during export");
      for (const item of input.adaptationInputs || []) check(await sha256Hex(await item.entry.read({ format: formats.binary })) === item.sha256, "Adaptation publication changed during export: " + item.path);
    }
    async function persistReceipt(input, folder, bui, buiName) {
      const snapshotId = bui.resourceReuse.snapshotId;
      const directory = async (parent, name) => {
        const entry = (await parent.getEntries()).find(e => e.name === name);
        if (entry) { check(entry.isFolder, `Export path is not a directory: ${name}`); return entry; }
        return writeOutput(() => parent.createFolder(name));
      };
      const runs = await directory(await directory(folder, "export"), "runs");
      check(!(await runs.getEntries()).some(e => e.name === snapshotId), "Export snapshot already exists");
      const run = await writeOutput(() => runs.createFolder(snapshotId));
      const receiptPath = `export/runs/${snapshotId}/export.json`;
      bui.source.workflowExport = { snapshotId, path: receiptPath };
      const buiJson = JSON.stringify(bui, null, 2);
      const uses = enumerate(bui);
      const layers = bui.nodes.map(n => ({ layerId: n.source?.layerId ?? null, nodeId: n.id,
        targetIds: bui.targets.filter(t => t.nodeIds.includes(n.id)).map(t => t.targetId), resource: n.resource || null }));
      const overrides = uses.filter(u => u.consumer.kind === "override").map(u => ({ layerId: u.layerId, consumer: u.consumer, resource: u.resource }));
      const resources = clone(bui.resources || []);
      const replacementBindings = bui.source.replacement?.replacementBindings || [];
      const receipt = { schemaVersion: 1, snapshotId, exportedAt: bui.source.exportedAt,
        bui: { path: buiName, sha256: await sha256Hex(encodeUtf8(buiJson)) },
        source: clone(bui.source), renameRecordSha256: input.renameRecordSha256 || "", renameRecord: input.record || null,
        replaceMap: input.replaceMap || null, replacementBindings, resourceReuse: clone(bui.resourceReuse), layers, overrides, resources };
      receipt.snapshot = await freezeInputs(folder, run, bui, buiName, encodeUtf8(buiJson));
      const bound = new Set((bui.resourceReuse.bindings || []).map(b => key(b.consumer)));
      const production = new Set(uses.filter(u => !bound.has(key(u.consumer))).map(u => `${bui.assetsRoot}/${u.resource.asset}`));
      for (const r of bui.resources || []) if (r.asset) production.add(`${bui.assetsRoot}/${r.asset}`);
      const dimensions = new Map(uses.map(u => [`${bui.assetsRoot}/${u.resource.asset}`, u.resource]));
      for (const r of bui.resources || []) if (r.asset) dimensions.set(`${bui.assetsRoot}/${r.asset}`, r);
      const measured = Object.entries(receipt.snapshot.files).filter(([path]) => /\.png$/i.test(path)).map(([path, file]) => ({
        path, role: production.has(path) ? "production" : "design-reference", sha256:file.sha256, bytes:file.size,
        pixelArea: dimensions.has(path) ? dimensions.get(path).width * dimensions.get(path).height : null
      }));
      receipt.imageMeasurement = {schemaVersion:1, files:measured, imageInstances:uses.length,
        productionPngBytes:measured.filter(r => r.role === "production").reduce((n,r) => n+r.bytes,0),
        referencePngBytes:measured.filter(r => r.role === "design-reference").reduce((n,r) => n+r.bytes,0),
        productionPixelArea:measured.filter(r => r.role === "production").reduce((n,r) => n+r.pixelArea,0),
        atlasPages:null, runtimeMemoryBytes:null, frameTime:null, outsideScopeRelease:null};
      if (input.previousExport) receipt.previousExport = clone(input.previousExport);
      await publishReceipt(run, receipt);
      return buiJson;
    }
    async function publishReceipt(run, receipt) {
      const json = JSON.stringify(receipt, null, 2);
      const pending = await writeOutput(() => run.createFile(`.__export_${Date.now()}_${Math.random().toString(16).slice(2)}.json`, { overwrite: false }));
      await pending.write(json, { format: formats.utf8 });
      check(await sha256Hex(await pending.read({ format: formats.binary })) === await sha256Hex(encodeUtf8(json)), "Snapshot receipt readback failed");
      await run.renameEntry(pending, "export.json", { overwrite: false });
      return relativeEntry(run, "export.json");
    }
    function resourceFiles(bui) {
      const files = new Map();
      function visit(value) {
        if (!value || typeof value !== "object") return;
        if (Object.prototype.hasOwnProperty.call(value, "asset")) {
          check(text(value.asset) && Number.isSafeInteger(value.width) && value.width >= 0 &&
            Number.isSafeInteger(value.height) && value.height >= 0, "Invalid snapshot resource fields");
          const digest = normalizeHash(value.sha256);
          const name = `${bui.assetsRoot}/${value.asset}`;
          check(/\.png$/i.test(name) && (!files.has(name) || files.get(name) === digest), "Conflicting PNG identity");
          files.set(name, digest);
        }
        Object.values(value).forEach(visit);
      }
      visit(bui);
      function references(value) {
        if (!value || typeof value !== "object") return;
        const reference = value.text?.previewReference;
        if (reference?.status === "captured") {
          const name = reference.path, digest = normalizeHash(reference.sha256);
          check(reference.version === 1 && typeof name === "string" &&
            !/[\\:\x00]/.test(name) && name.split("/").every(p => p && p !== "." && p !== "..") && /\.png$/i.test(name), "Invalid text reference path");
          check(!files.has(name) || files.get(name) === digest, "Conflicting text reference identity");
          files.set(name, digest);
        }
        Object.values(value).forEach(references);
      }
      references(bui);
      return files;
    }
    async function verifyProducts(folder, bui) {
      const files = [];
      for (const [relative, expected] of resourceFiles(bui)) {
        const entry = await relativeEntry(folder, relative);
        const bytes = await entry.read({ format: formats.binary });
        const actual = await sha256Hex(bytes);
        check(actual === expected, "EXPORT_PRODUCT_CHANGED: " + relative);
        files.push({ path: entry.nativePath || folder.nativePath + "/" + relative, sha256: actual, size: bytes.byteLength });
      }
      return files;
    }
    async function mkdir(parent, name) {
      const existing = (await parent.getEntries()).find(e => e.name === name);
      if (existing) { check(existing.isFolder, "Snapshot parent is not a folder"); return existing; }
      return writeOutput(() => parent.createFolder(name));
    }
    async function freezeInputs(folder, run, bui, buiName, buiBytes, resume = false) {
      const files = {};
      const inputs = resourceFiles(bui);
      check(!inputs.has(buiName), "BUI and PNG paths conflict");
      inputs.set(buiName, await sha256Hex(buiBytes));
      for (const [relative, expected] of inputs) {
        check(!/[\\:\x00]/.test(relative) && relative.split("/").every(p => p && p !== "." && p !== ".."), "Snapshot path escapes export folder");
        const bytes = relative === buiName ? buiBytes : await (await relativeEntry(folder, relative)).read({ format: formats.binary });
        check(await sha256Hex(bytes) === expected, "Snapshot input changed: " + relative);
        let parent = run;
        const parts = relative.split("/");
        for (const part of parts.slice(0, -1)) parent = await mkdir(parent, part);
        const name = parts[parts.length - 1];
        let file = (await parent.getEntries()).find(e => e.name === name);
        if (resume) check(!(await run.getEntries()).some(e => e.name === "export.json"), "Snapshot was published during recovery");
        // Only unpublished archives may repair partial payload writes. Completed archives
        // always use the strict verification path below archiveExisting.
        if (file) {
          check(resume && !file.isFolder, "Snapshot file already exists: " + relative);
          if (await sha256Hex(await file.read({ format: formats.binary })) !== expected)
            await writeOutput(() => file.write(bytes, { format: formats.binary }));
        } else {
          file = await writeOutput(() => parent.createFile(name, { overwrite: false }));
          await writeOutput(() => file.write(bytes, { format: formats.binary }));
        }
        const saved = await file.read({ format: formats.binary });
        check(saved.byteLength === bytes.byteLength && await sha256Hex(saved) === expected, "Snapshot readback failed: " + relative);
        files[relative] = { sha256: expected, size: bytes.byteLength };
      }
      return { buiPath: buiName, files };
    }
    async function archiveExisting(folder, existingBui) {
      if (!existingBui) return null;
      const bytes = await existingBui.read({ format: formats.binary });
      const bui = JSON.parse(decodeUtf8(bytes));
      const sha256 = await sha256Hex(bytes);
      const snapshotId = `input-${sha256}`;
      const runs = await mkdir(await mkdir(folder, "export"), "runs");
      const reference = bui.source?.workflowExport?.path;
      let receiptFile = null;
      if (reference) {
        // Legacy receipts are allowed to lack payload; present payload must validate.
        try { receiptFile = await relativeEntry(folder, reference); } catch (_) { /* legacy missing receipt */ }
      }
      if (!receiptFile || !JSON.parse(decodeUtf8(await receiptFile.read({ format: formats.binary }))).snapshot) {
        const existing = (await runs.getEntries()).find(e => e.name === snapshotId);
        if (existing) {
          check(existing.isFolder, "Snapshot path is not a folder");
          receiptFile = (await existing.getEntries()).find(e => e.name === "export.json") || null;
        } else receiptFile = null;
        if (!receiptFile) {
          const run = existing || await writeOutput(() => runs.createFolder(snapshotId));
          const receipt = { schemaVersion: 1, snapshotId, bui: { path: existingBui.name, sha256 }, source: bui.source,
            resourceReuse: bui.resourceReuse, snapshot: await freezeInputs(folder, run, bui, existingBui.name, bytes, true) };
          // Publish the receipt last. Failed receipt writes must not leave a completion
          // marker that makes the next attempt treat a partial archive as immutable.
          receiptFile = await publishReceipt(run, receipt);
        }
      }
      const receipt = JSON.parse(decodeUtf8(await receiptFile.read({ format: formats.binary })));
      check(normalizeHash(receipt.bui.sha256) === sha256 && receipt.snapshot?.files, "Old snapshot identity changed");
      // Resolve from the export root because UXP Entry has no portable parent accessor.
      const path = receipt.snapshotId === snapshotId ? `export/runs/${snapshotId}/export.json` : reference;
      const parent = await relativeEntry(folder, path.split("/").slice(0, -1).join("/"));
      for (const [relative, expected] of Object.entries(receipt.snapshot.files)) {
        const saved = await (await relativeEntry(parent, relative)).read({ format: formats.binary });
        check(saved.byteLength === expected.size && await sha256Hex(saved) === normalizeHash(expected.sha256), "Old snapshot file changed: " + relative);
      }
      check(normalizeHash(receipt.snapshot.files[receipt.snapshot.buiPath]?.sha256) === sha256, "Old snapshot BUI missing");
      for (const [relative, expected] of resourceFiles(bui)) {
        check(normalizeHash(receipt.snapshot.files[relative]?.sha256) === expected, "Old snapshot PNG missing: " + relative);
        check(await sha256Hex(await (await relativeEntry(folder, relative)).read({ format: formats.binary })) === expected, "Old export PNG changed: " + relative);
      }
      return { bui, sha256, path, receiptSha256: await sha256Hex(await receiptFile.read({ format: formats.binary })) };
    }
    async function cleanupPrevious(folder, previous, bui, buiName) {
      const report = { deleted: [], retained: [] };
      if (!previous) return report;
      const oldFiles = resourceFiles(previous.bui), live = resourceFiles(bui);
      // A second BUI can share an assets root. An unreadable BUI blocks cleanup.
      for (const entry of await folder.getEntries()) if (!entry.isFolder && /\.bui$/i.test(entry.name) && entry.name !== buiName) {
        const other = JSON.parse(decodeUtf8(await entry.read({ format: formats.binary })));
        for (const [name, hash] of resourceFiles(other)) live.set(name, hash);
      }
      for (const [relative, expected] of oldFiles) {
        if (live.has(relative) || Array.from(live.keys()).some(p => p.toLowerCase() === relative.toLowerCase())) continue;
        if (!relative.startsWith(previous.bui.assetsRoot + "/")) {
          report.retained.push({path:relative,reason:"reference-history"}); continue;
        }
        check(relative.startsWith(previous.bui.assetsRoot + "/") && previous.bui.assetsRoot === bui.assetsRoot, "Cleanup assets root changed");
        let file;
        try { file = await relativeEntry(folder, relative); } catch (_) { report.retained.push({ path: relative, reason: "missing-or-unreadable" }); continue; }
        if (file.isFolder || await sha256Hex(await file.read({ format: formats.binary })) !== expected) {
          report.retained.push({ path: relative, reason: "identity-changed" }); continue;
        }
        await file.delete(); report.deleted.push(relative);
      }
      return report;
    }
    return { verifyProducts, load, inspect: (psd, folder, source, saved) => load(psd, folder, source, saved, "current-psd", true), finalize, verifyUnchanged, persistReceipt, archiveExisting, cleanupPrevious };
  }
  global.BurnerPsdToBuiResourceReuse = { create, validateDeclaration, enumerate, finalize, consumer, key, encodeUtf8, collectRetainedTexts };
})(globalThis);
