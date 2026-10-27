/* global globalThis */
(function registerDesignTexts(global) {
  // Source design data only: no old Prefab, replacement plan or Rename record.
  function isIgnored(layer, namingRules) {
    const name = String(layer.name || "").trim();
    return (namingRules.syntax?.ignorePrefixes || []).filter(Boolean).some(prefix => name.startsWith(prefix));
  }
  async function collect(doc, bui, readTextInfo, warnings, namingRules) {
    const used = new Set(), roots = new Map(), seen = new Set(), result = [];
    function sources(value) {
      if (!value || typeof value !== "object") return;
      if (value.text?.source?.layerId != null) used.add(String(value.text.source.layerId));
      for (const child of Object.values(value)) if (child && typeof child === "object") sources(child);
    }
    sources(bui.nodes); sources(bui.retainedTexts); // Do not duplicate old explicitly-bound exports.
    for (const target of bui.targets || []) {
      const root = (bui.nodes || []).find(n => n.id === target.sourceRootNodeId);
      if (root?.source?.layerId == null) throw Error("DESIGN_TEXT_INVALID: target root layer is missing");
      const id = String(root.source.layerId);
      roots.set(id, [...(roots.get(id) || []), target.targetId]);
    }
    async function walk(layers, parentPath, parentVisible, parentTargets) {
      for (const layer of layers || []) {
        if (isIgnored(layer, namingRules)) continue;
        const id = String(layer.id), path = parentPath ? parentPath + "/" + layer.name : layer.name;
        if (seen.has(id)) throw Error("DESIGN_TEXT_INVALID: duplicate PSD layer " + id);
        seen.add(id);
        const visible = parentVisible && layer.visible !== false;
        const targetIds = roots.get(id) || parentTargets;
        if (visible && !used.has(id) && (layer.textItem || layer.text)) {
          if (!/^[1-9][0-9]*$/.test(id)) throw Error("DESIGN_TEXT_INVALID: invalid source layer");
          const text = await readTextInfo(layer, path, warnings);
          if (!text || String(text.source?.layerId) !== id) throw Error("DESIGN_TEXT_INVALID: native text identity unavailable");
          result.push({id: "design_text_" + id, name: layer.name, purpose: "design-text-style", targetIds, text});
        }
        await walk(layer.layers, path, visible, targetIds);
      }
    }
    await walk(doc.layers, "", true, []);
    return result;
  }
  global.BurnerPsdToBuiDesignTexts = {collect, isIgnored};
})(globalThis);
