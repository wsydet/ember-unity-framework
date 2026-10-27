/* global globalThis */

(function registerBuiModelBuilder(global) {
  function createBuiModelBuilder(options) {
    const { arrayFrom, makePageClassName, slugify, makeRect, getDefaultVariant, sanitizeId } = options;
    const NAMING_RULES_CONFIG_PATH = options.namingRulesConfigPath;

    function createSyntheticTargetRoot(exportBaseName, canvas, nodes, exportState, namingRules) {
      const index = getNamingRuleIndex(namingRules);
      const fallbackTargetType = namingRules.rootPolicy?.noTargetFallback?.targetType;
      const targetRule = index.targetById[fallbackTargetType] || arrayFrom(namingRules.targetTypes)[0];
      if (!targetRule) {
        throw new Error(`${NAMING_RULES_CONFIG_PATH} must define at least one targetTypes entry.`);
      }
      const targetCanonicalName = makePageClassName(exportBaseName);
      const rootId = makeUniqueNodeId(`target_${slugify(targetCanonicalName)}_root`, nodes);
      const rootNode = {
        id: rootId,
        parentId: null,
        name: targetCanonicalName,
        type: targetRule.nodeType || "Group",
        active: true,
        siblingIndex: 0,
        sourcePath: "",
        source: {
          layerId: null,
          layerType: "syntheticRoot",
          resourceType: "None",
          variant: getDefaultVariant(namingRules),
          zIndex: 0,
          naming: {
            rawName: "",
            displayName: targetCanonicalName,
            main: {
              token: "fallback",
              flag: targetRule.id || fallbackTargetType,
              kind: "target",
              id: targetRule.id || fallbackTargetType,
              name: targetCanonicalName
            },
            options: []
          }
        },
        rect: makeRect(0, 0, canvas.width, canvas.height, canvas)
      };
      nodes.push(rootNode);

      return {
        rootNode,
        target: makeTargetRecord(targetCanonicalName, rootId, targetRule, exportState)
      };
    }


    function makeTargetRecord(targetCanonicalName, rootId, targetRule, exportState) {
      const targetId = makeUniqueTargetId(slugify(targetCanonicalName), exportState);
      return {
        targetId,
        targetType: targetRule.targetType || targetRule.id,
        name: targetCanonicalName,
        sourceRootNodeId: rootId
      };
    }


    function finalizeTargets(targets, nodes) {
      return targets.map((target) => ({
        ...target,
        nodeIds: collectNodeIdsForRoot(nodes, target.sourceRootNodeId)
      }));
    }


    function collectNodeIdsForRoot(nodes, rootId) {
      const childrenByParent = {};
      nodes.forEach((node) => {
        const key = node.parentId || "";
        childrenByParent[key] = childrenByParent[key] || [];
        childrenByParent[key].push(node);
      });

      const ids = [];
      const visit = (nodeId) => {
        ids.push(nodeId);
        arrayFrom(childrenByParent[nodeId]).forEach((child) => visit(child.id));
      };
      visit(rootId);
      return ids;
    }


    function makeUniqueTargetId(base, exportState) {
      const cleanBase = sanitizeId(base || "target");
      let candidate = cleanBase;
      let index = 1;
      exportState.usedTargetIds = exportState.usedTargetIds || {};
      while (exportState.usedTargetIds[candidate]) {
        candidate = `${cleanBase}_${index}`;
        index += 1;
      }
      exportState.usedTargetIds[candidate] = true;
      return candidate;
    }


    function countRootNodes(nodes) {
      return arrayFrom(nodes).filter((node) => !node.parentId).length;
    }


    function makeUniqueNodeId(base, nodes) {
      const used = {};
      nodes.forEach((node) => {
        used[node.id] = true;
      });

      let candidate = sanitizeId(base || "node");
      const cleanBase = candidate;
      let index = 1;
      while (used[candidate]) {
        candidate = `${cleanBase}_${index}`;
        index += 1;
      }
      return candidate;
    }


    function nodeIdPrefix(nodeType, isGroup) {
      if (nodeType === "PrefabRef") {
        return "prefab_ref";
      }
      if (nodeType === "List") {
        return "list";
      }
      if (nodeType === "Button") {
        return "button";
      }
      if (nodeType === "Text") {
        return "text";
      }
      return isGroup ? "group" : "psd_layer";
    }

    return { createSyntheticTargetRoot, makeTargetRecord, finalizeTargets, collectNodeIdsForRoot, makeUniqueTargetId, countRootNodes, makeUniqueNodeId, nodeIdPrefix };
  }

  global.BurnerPsdToBuiModelBuilder = { createBuiModelBuilder };
})(globalThis);
