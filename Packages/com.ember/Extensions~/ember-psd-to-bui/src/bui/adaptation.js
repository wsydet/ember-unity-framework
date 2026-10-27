/* global globalThis */
// Copyright (c) 2026 Burner Games. All rights reserved.
(function registerAdaptation(global) {
  "use strict";
  const roles = ["Bg", "Top", "Center", "Bottom"];
  const pageTypes = ["Page", "SubPage", "Popup"];
  const sha = value => typeof value === "string" && /^[a-fA-F0-9]{64}$/.test(value);
  const object = value => value !== null && typeof value === "object" && !Array.isArray(value);
  const text = value => typeof value === "string" && value.trim().length > 0;
  const strings = value => Array.isArray(value) && value.every(text) && new Set(value).size === value.length;
  const rect = value => Array.isArray(value) && value.length === 4 && value.every(Number.isFinite) && value[2] > value[0] && value[3] > value[1];
  const equal = (a, b) => JSON.stringify(a) === JSON.stringify(b);
  const fields = (value, required, optional = []) => object(value) && required.every(key => Object.prototype.hasOwnProperty.call(value, key)) && Object.keys(value).every(key => required.includes(key) || optional.includes(key));
  const close = (a, b) => Array.isArray(a) && a.length === b.length && a.every((v, i) => Number.isFinite(v) && Math.abs(v - b[i]) < 0.01);
  const centered = value => value && ["anchorMin", "anchorMax", "pivot"].every(k => close(value[k], [0.5, 0.5]));
  function allocatedRect(value, bounds, parentBounds) {
    return centered(value) && rect(bounds) && rect(parentBounds) && close(value.sizeDelta, [bounds[2] - bounds[0], bounds[3] - bounds[1]]) &&
      close(value.anchoredPosition, [(bounds[0] + bounds[2] - parentBounds[0] - parentBounds[2]) / 2, (parentBounds[1] + parentBounds[3] - bounds[1] - bounds[3]) / 2]);
  }

  // Rename 使用图层身份；只有实际生成节点后才投影为 BUI 身份，不按名称猜宿主。
  function summary(value) {
    const result = {};
    for (const key of ["targetType", "sourceLayerId", "designRect", "regions", "confirmationRefs", "inputIdentity", "parentTargetId", "hostNodeId", "hostRect"])
      if (Object.prototype.hasOwnProperty.call(value, key)) result[key] = JSON.parse(JSON.stringify(value[key]));
    result.behaviorSources = value.behaviorSources.map(behavior => {
      const item = {};
      for (const key of ["kind", "nodeIds", "value", "inputFingerprint", "confirmationId"])
        if (Object.prototype.hasOwnProperty.call(behavior, key)) item[key] = JSON.parse(JSON.stringify(behavior[key]));
      item.source = Object.fromEntries(["kind", "path", "detail"].map(key => [key, behavior.source[key]]));
      if (behavior.source.evidence !== undefined) item.source.evidence = JSON.parse(JSON.stringify(behavior.source.evidence));
      return item;
    });
    return result;
  }
  function project(input, model) {
    if (!object(input) || input.version !== 1 || !object(input.targets)) throw new Error("BUI_ADAPTATION_INPUT_INVALID");
    const result = { version: 1, targets: {} };
    const targetForLayer = layerId => {
      const matches = model.targets.filter(t => String(model.nodes.find(n => n.id === t.sourceRootNodeId)?.source?.layerId) === String(layerId));
      if (matches.length !== 1) throw new Error("BUI_ADAPTATION_TARGET_IDENTITY_INVALID: " + layerId);
      return matches[0];
    };
    const nodeForLayer = (target, layerId) => {
      const matches = model.nodes.filter(n => target.nodeIds.includes(n.id) && String(n.source?.layerId) === String(layerId));
      if (matches.length !== 1) throw new Error("BUI_ADAPTATION_NODE_IDENTITY_INVALID: " + layerId);
      return matches[0].id;
    };
    for (const [key, value] of Object.entries(input.targets)) {
      if (!object(value) || String(key) !== String(value.sourceLayerId)) throw new Error("BUI_ADAPTATION_SOURCE_KEY_INVALID");
      const target = targetForLayer(value.sourceLayerId);
      const item = summary(value);
      for (const role of roles) item.regions[role] = value.regions[role].map(id => nodeForLayer(target, id));
      item.behaviorSources = item.behaviorSources.map(source => {
        const projected = { ...source, nodeIds: source.nodeIds.map(id => nodeForLayer(target, id)) };
        if (source.kind === "fixedOrScroll" && source.value?.mode === "scroll" && source.value.contentNodeId !== undefined)
          projected.value = { ...source.value, contentNodeId: nodeForLayer(target, source.value.contentNodeId) };
        return projected;
      });
      if (target.targetType === "SubPage") {
        const parent = targetForLayer(value.parentTargetId);
        item.parentTargetId = parent.targetId;
        item.hostNodeId = nodeForLayer(parent, value.hostNodeId);
      }
      result.targets[target.targetId] = item;
    }
    return result;
  }

  function validate(bui, selectedTargetIds) {
    const issues = [];
    const add = (path, message) => issues.push({ code: "BUI_ADAPTATION_INVALID", severity: "Blocking", sourcePath: path, message });
    if (bui?.schemaVersion === 3 || bui?.schemaVersion === 2) {
      if (bui.adaptation !== undefined || bui.targets?.some(t => t.targetType === "SubPage")) add("adaptation", "Adaptation/SubPage requires explicit BUI v4; legacy input cannot carry new required semantics.");
      return issues;
    }
    if (bui?.schemaVersion !== 4) { add("schemaVersion", "Only legacy v3 and adaptation v4 are supported."); return issues; }
    const adaptation = bui.adaptation;
    if (!fields(adaptation, ["version", "targets"]) || adaptation.version !== 1 || !object(adaptation.targets)) {
      add("adaptation", "v4 requires adaptation version 1 and targets."); return issues;
    }
    const nodes = Array.isArray(bui.nodes) ? bui.nodes : [], targets = Array.isArray(bui.targets) ? bui.targets : [];
    const nodeMap = new Map(), targetMap = new Map(), owners = new Map();
    for (const node of nodes) { if (nodeMap.has(node.id)) add("nodes", "Duplicate node identity."); nodeMap.set(node.id, node); }
    for (const target of targets) {
      if (targetMap.has(target.targetId)) add("targets", "Duplicate target identity.");
      targetMap.set(target.targetId, target);
      for (const id of target.nodeIds || []) {
        if (!nodeMap.has(id) || owners.has(id)) add("targets." + target.targetId, "Each node must exist and have exactly one owner.");
        owners.set(id, target.targetId);
      }
      if (!target.nodeIds?.includes(target.sourceRootNodeId) || nodeMap.get(target.sourceRootNodeId)?.parentId != null) add("targets." + target.targetId, "Target root must be owned and parentless.");
    }
    for (const node of nodes) {
      if (!owners.has(node.id)) add("nodes." + node.id, "Unowned node.");
      if (node.parentId && (!nodeMap.has(node.parentId) || owners.get(node.parentId) !== owners.get(node.id))) add("nodes." + node.id, "Parent must belong to the same target.");
      const seen = new Set([node.id]); let parent = node.parentId;
      while (parent && nodeMap.has(parent)) { if (seen.has(parent)) { add("nodes." + node.id, "Node parent cycle."); break; } seen.add(parent); parent = nodeMap.get(parent).parentId; }
      if (!seen.has(targetMap.get(owners.get(node.id))?.sourceRootNodeId)) add("nodes." + node.id, "Node must connect to its owner's declared root.");
    }
    for (const key of Object.keys(adaptation.targets)) if (!targetMap.has(key) || !pageTypes.includes(targetMap.get(key)?.targetType)) add("adaptation.targets." + key, "Unknown or non-page adaptation target.");
    for (const target of targets.filter(t => pageTypes.includes(t.targetType))) {
      const path = "adaptation.targets." + target.targetId, item = adaptation.targets[target.targetId];
      if (!object(item)) { add(path, "Every page target requires adaptation."); continue; }
      if (!fields(item, ["targetType", "sourceLayerId", "designRect", "regions", "behaviorSources", "confirmationRefs", "inputIdentity"], ["parentTargetId", "hostNodeId", "hostRect"])) add(path, "Missing or unknown adaptation fields.");
      if (item.targetType !== target.targetType || !text(item.sourceLayerId) || item.sourceLayerId !== nodeMap.get(target.sourceRootNodeId)?.source?.layerId) add(path, "Target type/sourceLayerId does not match its actual root.");
      if (!rect(item.designRect)) add(path + ".designRect", "Expected positive PSD absolute [left,top,right,bottom].");
      else if (!allocatedRect(nodeMap.get(target.sourceRootNodeId)?.rect, item.designRect, item.designRect)) add(path + ".designRect", "Target root geometry must equal its allocated design rectangle.");
      if (!fields(item.inputIdentity, ["psdSha256", "structureSha256"]) || !sha(item.inputIdentity.psdSha256) || !sha(item.inputIdentity.structureSha256) || item.inputIdentity.psdSha256?.toLowerCase() !== bui.source?.psdSha256?.toLowerCase()) add(path + ".inputIdentity", "Input hash is missing or differs from current PSD.");
      if (!fields(item.regions, roles)) add(path + ".regions", "All four roles must be explicit; unknown region fields are not supported.");
      const regionIds = new Set();
      for (const role of roles) {
        const ids = item.regions?.[role];
        if (!strings(ids)) { add(path + ".regions." + role, "Every region requires an explicit unique nodeId array, including empty roles."); continue; }
        for (const id of ids) {
          const node = nodeMap.get(id);
          if (regionIds.has(id) || owners.get(id) !== target.targetId || node?.type !== role || node?.parentId !== target.sourceRootNodeId) add(path + ".regions." + role, "Region must match one owned, direct region node.");
          regionIds.add(id);
        }
      }
      for (const node of nodes.filter(n => owners.get(n.id) === target.targetId && roles.includes(n.type))) if (!regionIds.has(node.id)) add(path + ".regions", "An explicit region node is absent from adaptation.");
      if (!strings(item.confirmationRefs)) add(path + ".confirmationRefs", "Explicit unique confirmation ID array required.");
      if (!Array.isArray(item.behaviorSources)) add(path + ".behaviorSources", "Explicit behavior source array required.");
      for (const behavior of Array.isArray(item.behaviorSources) ? item.behaviorSources : []) {
        if (!fields(behavior, ["kind", "nodeIds", "value", "source", "inputFingerprint"], ["confirmationId"]) || !["fixedOrScroll", "switchScope", "dynamicVisibility"].includes(behavior.kind) || !strings(behavior.nodeIds) || behavior.nodeIds.length === 0 || behavior.nodeIds.some(id => owners.get(id) !== target.targetId) || !object(behavior.value) || !sha(behavior.inputFingerprint) || !fields(behavior.source, ["kind", "path", "detail"], ["evidence"]) || !["existing-fact", "user-decision"].includes(behavior.source.kind) || !text(behavior.source.path) || !text(behavior.source.detail)) add(path + ".behaviorSources", "Behavior requires exact owned nodes, value, source and input fingerprint.");
        if (behavior.source?.evidence !== undefined && (!Array.isArray(behavior.source.evidence) || !behavior.source.evidence.length || behavior.source.evidence.some(e => !fields(e, ["path", "sha256"]) || !text(e.path) || !sha(e.sha256)))) add(path + ".behaviorSources", "Evidence requires explicit path and SHA256 references.");
        if (behavior?.source?.kind === "user-decision" && (!text(behavior.confirmationId) || !Array.isArray(item.confirmationRefs) || !item.confirmationRefs.includes(behavior.confirmationId))) add(path + ".behaviorSources", "User decision requires a matching confirmation reference; suggestions are not approval.");
      }
      if (target.targetType === "SubPage") {
        const parent = targetMap.get(item.parentTargetId), host = nodeMap.get(item.hostNodeId);
        if (parent?.targetType !== "Page" || owners.get(item.hostNodeId) !== parent?.targetId || !host || host.type === "PrefabRef" || !rect(item.hostRect) || !equal(item.designRect, item.hostRect)) add(path, "SubPage requires a Page-owned host and identical allocated designRect/hostRect.");
        const mounts = nodes.filter(n => n.prefabRef?.kind === "GeneratedTarget" && n.prefabRef.targetId === target.targetId);
        if (mounts.length !== 1 || owners.get(mounts[0]?.id) !== item.parentTargetId || mounts[0]?.parentId !== item.hostNodeId || mounts[0]?.prefabRef?.hostNodeId !== item.hostNodeId || mounts[0]?.prefabRef?.targetType !== "SubPage") add(path, "SubPage requires exactly one explicit parent host mount.");
        else if (!allocatedRect(mounts[0].rect, item.hostRect, adaptation.targets[item.parentTargetId]?.designRect)) add(path, "SubPage mount geometry must equal the host rectangle in parent target coordinates.");
        let ancestor = host; const seen = new Set();
        while (ancestor && ancestor.type !== "Center" && !seen.has(ancestor.id)) { seen.add(ancestor.id); ancestor = nodeMap.get(ancestor.parentId); }
        if (ancestor?.type !== "Center") add(path, "SubPage host must belong to the Page Center region.");
      } else if (["parentTargetId", "hostNodeId", "hostRect"].some(key => key in item)) add(path, "Only SubPage carries parent/host fields.");
    }
    const edges = new Map(targets.map(t => [t.targetId, []]));
    for (const node of nodes.filter(n => n.prefabRef?.kind === "GeneratedTarget")) {
      const child = targetMap.get(node.prefabRef.targetId), owner = owners.get(node.id);
      if (!child) { add("nodes." + node.id, "Missing generated target dependency."); continue; }
      edges.get(owner)?.push(child.targetId);
      if (child.targetType !== "SubPage" && (node.prefabRef.targetType !== undefined || node.prefabRef.hostNodeId !== undefined)) add("nodes." + node.id, "SubPage mount fields cannot annotate another target type.");
      if (selectedTargetIds && selectedTargetIds.includes(owner) && !selectedTargetIds.includes(child.targetId)) add("targets", "Selection must include all generated target dependencies, including hidden pages.");
    }
    const visited = new Set(), visiting = new Set();
    function visit(id) { if (visiting.has(id)) { add("targets", "Generated target dependency cycle."); return; } if (visited.has(id)) return; visiting.add(id); for (const next of edges.get(id) || []) visit(next); visiting.delete(id); visited.add(id); }
    for (const id of edges.keys()) visit(id);
    return issues;
  }
  global.BurnerPsdToBuiAdaptation = { project, validate, summary };
})(globalThis);
