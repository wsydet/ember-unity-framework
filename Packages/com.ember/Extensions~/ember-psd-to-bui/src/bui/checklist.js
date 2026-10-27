/* global globalThis */

(function registerBuiChecklist(global) {
  function createBuiChecklist(options) {
    const { getWarningPriority, formatText, t } = options;

    function assertExportableBui(bui) {
      if (bui?.schemaVersion === 4 && !bui.source?.adaptationSource) throw new Error("BUI_ADAPTATION_HANDOFF_REQUIRED: production export requires verified saved Rename adaptation.");
      const warnings = Array.isArray(bui?.warnings) ? bui.warnings : [];
      validateBuiChecklist(bui).forEach((warning) => warnings.push(warning));
      const blocking = warnings.filter(isBlockingWarning);
      if (blocking.length > 0) {
        const codes = blocking
          .map((warning) => warning.code)
          .filter(Boolean)
          .slice(0, 6)
          .join(", ");
        throw new Error(formatText(t("buiExportBlocked"), {
          count: blocking.length,
          codes: codes || "-"
        }));
      }
    }


    function validateBuiChecklist(bui) {
      return validateBuiChecklistCore(bui, {
        includeExportMetadata: true,
        requireResourceSha256: true
      });
    }


    function validateBuiPreviewChecklist(bui) {
      return validateBuiChecklistCore(bui, {
        includeExportMetadata: false,
        requireResourceSha256: false
      });
    }


    function validateBuiChecklistCore(bui, validationOptions) {
      const includeExportMetadata = Boolean(validationOptions?.includeExportMetadata);
      const resourceValidationOptions = {
        requireSha256: validationOptions?.requireResourceSha256 !== false
      };
      const warnings = [];
      const validTargetTypes = new Set(bui?.schemaVersion === 4 ? ["Page", "SubPage", "Popup", "Component", "ListItem"] : ["Page", "Popup", "Component", "ListItem"]);
      const validNodeTypes = new Set(["Group", "Bg", "Top", "Center", "Bottom", "Image", "RawImage", "Button", "Text", "PrefabRef", "List"]);
      const validResourceTypes = new Set(["Atlas", "StaticTexture", "DynamicTexture"]);
      const safeAreaRegionNodeTypes = new Set(["Bg", "Top", "Center", "Bottom"]);
      const highRiskNoBindNodeTypes = new Set(["Button", "List", "PrefabRef"]);

      const addIssue = (path, code, severity, message) => {
        warnings.push({
          code,
          severity,
          sourcePath: path || "",
          message
        });
      };
      const add = (path, message) => {
        addIssue(path, "BUI_SCHEMA_CHECKLIST_FAILED", "Blocking", message);
      };

      if (!bui || typeof bui !== "object") {
        add("", "BUI root must be an object.");
        return warnings;
      }

      if (![2, 3, 4].includes(bui.schemaVersion)) {
        add("schemaVersion", "Unsupported BUI schemaVersion.");
      }
      if (includeExportMetadata) {
        if (!isNonEmptyString(bui.assetsRoot)) {
          add("assetsRoot", "assetsRoot must be a non-empty string.");
        }
        validateSourceChecklist(bui.source, add);
      }
      if (!Array.isArray(bui.targets) || bui.targets.length === 0) {
        add("targets", "targets must be a non-empty array.");
      }
      if (!Array.isArray(bui.nodes) || bui.nodes.length === 0) {
        add("nodes", "nodes must be a non-empty array.");
      }

      if (global.BurnerPsdToBuiAdaptation) warnings.push(...global.BurnerPsdToBuiAdaptation.validate(bui));
      else if (bui.schemaVersion === 4 || bui.adaptation) add("adaptation", "Adaptation validator is unavailable.");
      validateCanvasChecklist(bui.canvas, add);
      validateTargetsChecklist(bui.targets, validTargetTypes, add);
      validateNodesChecklist(
        bui.nodes,
        validNodeTypes,
        validResourceTypes,
        highRiskNoBindNodeTypes,
        add,
        addIssue,
        resourceValidationOptions
      );
      validateSafeAreaRegionChecklist(bui.targets, bui.nodes, safeAreaRegionNodeTypes, add);
      validatePrefabRefsChecklist(bui.targets, bui.nodes, add, {
        ...resourceValidationOptions,
        validResourceTypes
      });

      if (bui.resources !== undefined && !Array.isArray(bui.resources)) {
        add("resources", "resources must be an array.");
      }
      const ownerIds = new Set((Array.isArray(bui.targets) ? bui.targets : []).flatMap((target) => Array.isArray(target?.nodeIds) ? target.nodeIds : []));
      const nodeIds = new Set((Array.isArray(bui.nodes) ? bui.nodes : []).map((node) => node?.id));
      const resourceIds = new Set();
      (Array.isArray(bui.resources) ? bui.resources : []).forEach((entry, index) => {
        const path = "resources." + index;
        if (!entry || typeof entry !== "object") {
          add(path, "A standalone resource entry must be an object.");
          return;
        }
        if (!isNonEmptyString(entry.id) || resourceIds.has(entry.id) || nodeIds.has(entry.id)) {
          add(path + ".id", "A standalone resource must have a unique id, distinct from UI node ids.");
        }
        resourceIds.add(entry.id);
        if (!nodeIds.has(entry.ownerNodeId) || !ownerIds.has(entry.ownerNodeId)) {
          add(path + ".ownerNodeId", "A standalone resource owner must be a node belonging to a target.");
        }
        if (!isNonEmptyString(entry.name)) add(path + ".name", "A standalone resource name is required.");
        validateResourceChecklist(entry.resource, path + ".resource", new Set(["Atlas", "StaticTexture"]), add, resourceValidationOptions);
      });
      return warnings;
    }


    function validateSourceChecklist(source, add) {
      if (!source || typeof source !== "object") {
        add("source", "source must be an object.");
        return;
      }

      [
        ["source.type", source.type],
        ["source.psdName", source.psdName],
        ["source.psdRelativePath", source.psdRelativePath],
        ["source.adapter", source.adapter],
        ["source.adapterVersion", source.adapterVersion],
        ["source.exportedAt", source.exportedAt]
      ].forEach(([path, value]) => {
        if (!isNonEmptyString(value)) {
          add(path, `${path} must be a non-empty string.`);
        }
      });

      if (!isSha256Hex(source.psdSha256)) {
        add("source.psdSha256", "source.psdSha256 must be a 64-character sha256 hex string.");
      }
      if (!Number.isInteger(source.psdSize) || source.psdSize < 0) {
        add("source.psdSize", "source.psdSize must be a non-negative integer.");
      }
      if (!isNonEmptyString(source.psdModifiedAt)) {
        add("source.psdModifiedAt", "source.psdModifiedAt must be a non-empty string.");
      }
    }


    function validateCanvasChecklist(canvas, add) {
      if (!canvas || typeof canvas !== "object") {
        add("canvas", "canvas must be an object.");
        return;
      }

      if (!isNumberArray(canvas.referenceResolution, 2) ||
          canvas.referenceResolution.some((value) => value <= 0)) {
        add("canvas.referenceResolution", "canvas.referenceResolution must be a 2-number array containing finite positive width and height values.");
      }
      ["coordinateSystem", "origin", "unit"].forEach((field) => {
        if (!isNonEmptyString(canvas[field])) {
          add(`canvas.${field}`, `canvas.${field} must be a non-empty string.`);
        }
      });
    }


    function validateTargetsChecklist(targets, validTargetTypes, add) {
      if (!Array.isArray(targets)) {
        return;
      }

      targets.forEach((target, index) => {
        const path = `targets[${index}]`;
        if (!target || typeof target !== "object") {
          add(path, `${path} must be an object.`);
          return;
        }
        if (!isNonEmptyString(target.targetId)) {
          add(`${path}.targetId`, `${path}.targetId must be a non-empty string.`);
        }
        if (!validTargetTypes.has(target.targetType)) {
          add(`${path}.targetType`, `${path}.targetType is not a supported target type.`);
        }
        if (!isNonEmptyString(target.name)) {
          add(`${path}.name`, `${path}.name must be a non-empty string.`);
        }
        if (target.targetType === "Page" && isNonEmptyString(target.name) && !isValidPageTargetName(target.name)) {
          add(`${path}.name`, `${path}.name must be XxxPage for Page targets.`);
        }
        if (target.targetType === "Popup" && isNonEmptyString(target.name) && !isValidPopupTargetName(target.name)) {
          add(`${path}.name`, `${path}.name must be XxxPopup for Popup targets.`);
        }
        if (!isNonEmptyString(target.sourceRootNodeId)) {
          add(`${path}.sourceRootNodeId`, `${path}.sourceRootNodeId must be a non-empty string.`);
        }
        if (!Array.isArray(target.nodeIds) || target.nodeIds.length === 0 || target.nodeIds.some((id) => !isNonEmptyString(id))) {
          add(`${path}.nodeIds`, `${path}.nodeIds must be a non-empty string array.`);
        }
      });
    }


    function validateNodesChecklist(nodes, validNodeTypes, validResourceTypes, highRiskNoBindNodeTypes, add, addIssue, resourceValidationOptions = {}) {
      highRiskNoBindNodeTypes = highRiskNoBindNodeTypes || new Set(["Button", "List", "PrefabRef"]);
      addIssue = addIssue || ((path, code, severity, message) => add(path, `${code}: ${message}`));
      if (!Array.isArray(nodes)) {
        return;
      }

      nodes.forEach((node, index) => {
        const path = `nodes[${index}]`;
        if (!node || typeof node !== "object") {
          add(path, `${path} must be an object.`);
          return;
        }
        if (!isNonEmptyString(node.id)) {
          add(`${path}.id`, `${path}.id must be a non-empty string.`);
        }
        if (!isNonEmptyString(node.name)) {
          add(`${path}.name`, `${path}.name must be a non-empty string.`);
        }
        if (!validNodeTypes.has(node.type)) {
          add(`${path}.type`, `${path}.type is not a supported node type.`);
        }
        if (typeof node.active !== "boolean") {
          add(`${path}.active`, `${path}.active must be a boolean.`);
        }
        if (!Number.isInteger(node.siblingIndex)) {
          add(`${path}.siblingIndex`, `${path}.siblingIndex must be an integer.`);
        }
        validateRectChecklist(node.rect, `${path}.rect`, add);
        if (node.resource) {
          validateResourceChecklist(node.resource, `${path}.resource`, validResourceTypes, add, resourceValidationOptions);
        }
        validateBindingChecklist(node, `${path}.binding`, highRiskNoBindNodeTypes, add, addIssue);
        validateTextKeySeedChecklist(node, `${path}.text`, add, addIssue);
      });
    }


    function validateBindingChecklist(node, path, highRiskNoBindNodeTypes, add, addIssue) {
      if (node.binding === undefined || node.binding === null) {
        return;
      }

      if (!node.binding || typeof node.binding !== "object" || Array.isArray(node.binding)) {
        add(path, `${path} must be an object when present.`);
        return;
      }

      if (typeof node.binding.collect !== "boolean") {
        add(`${path}.collect`, `${path}.collect must be a boolean.`);
      }

      if (node.binding.source !== undefined && typeof node.binding.source !== "string") {
        add(`${path}.source`, `${path}.source must be a string when present.`);
      }

      if (node.binding.reason !== undefined && typeof node.binding.reason !== "string") {
        add(`${path}.reason`, `${path}.reason must be a string when present.`);
      }

      if (node.binding.collect === false && highRiskNoBindNodeTypes.has(node.type)) {
        addIssue(
          path,
          "BUI_NOBIND_HIGH_RISK",
          "Warning",
          `${node.type} is marked NoBind. Confirm this node really does not need code binding.`
        );
      }
    }


    function validateTextKeySeedChecklist(node, path, add, addIssue) {
      if (node.type !== "Text") {
        return;
      }

      if (!node.text || typeof node.text !== "object" || Array.isArray(node.text)) {
        add(path, "Text node must contain node.text.text as design/placeholder copy.");
        return;
      }

      if (typeof node.text.text !== "string") {
        add(`${path}.text`, "Text node.text.text must be a string.");
        return;
      }

      if (node.text.key !== undefined && typeof node.text.key !== "string") {
        add(`${path}.key`, "Text node.text.key must be a string when present.");
        return;
      }

      if (node.text.fontSize !== undefined && (!Number.isFinite(node.text.fontSize) || node.text.fontSize <= 0)) {
        add(`${path}.fontSize`, "Text node.text.fontSize must be a positive number when present.");
        return;
      }

      validateTextVisualStyleChecklist(node.text.visualStyle, `${path}.visualStyle`, add);

      if (!isBindingCollectDisabled(node)) {
        return;
      }

      const seed = resolveTextKeySeed(node);
      const result = validateTextKeySeed(seed);
      if (!result.valid) {
        addIssue(
          `${path}.key`,
          "BUI_TEXT_KEY_SEED_REVIEW",
          "Warning",
          `Static NoBind Text could not derive a non-empty normalized LanguageKey. The generated component will keep LanguageKey empty: ${result.reason}`
        );
      }
    }


    function validateTextVisualStyleChecklist(visualStyle, path, add) {
      if (visualStyle === undefined || visualStyle === null) {
        return;
      }

      if (!visualStyle || typeof visualStyle !== "object" || Array.isArray(visualStyle)) {
        add(path, `${path} must be an object when present.`);
        return;
      }

      if (visualStyle.version !== undefined && visualStyle.version !== 1) {
        add(`${path}.version`, `${path}.version must be 1 when present.`);
      }

      const validStatuses = new Set(["Resolved", "Partial", "Mixed", "Unavailable"]);
      if (visualStyle.status !== undefined && !validStatuses.has(visualStyle.status)) {
        add(`${path}.status`, `${path}.status must be Resolved, Partial, Mixed, or Unavailable.`);
      }

      if (visualStyle.key !== undefined && !isNonEmptyString(visualStyle.key)) {
        add(`${path}.key`, `${path}.key must be a non-empty string when present.`);
      }

      if (visualStyle.status !== "Unavailable" && visualStyle.key === undefined) {
        add(`${path}.key`, `${path}.key is required unless visualStyle.status is Unavailable.`);
      }

      if (visualStyle.features !== undefined) {
        if (!visualStyle.features || typeof visualStyle.features !== "object" || Array.isArray(visualStyle.features)) {
          add(`${path}.features`, `${path}.features must be an object when present.`);
        } else {
          validateTextVisualStyleFeaturesChecklist(visualStyle.features, `${path}.features`, add);
        }
      }

      if (visualStyle.missingFields !== undefined && !isStringArray(visualStyle.missingFields)) {
        add(`${path}.missingFields`, `${path}.missingFields must be a string array when present.`);
      }

      if (visualStyle.warnings !== undefined && !isStringArray(visualStyle.warnings)) {
        add(`${path}.warnings`, `${path}.warnings must be a string array when present.`);
      }
    }


    function validateTextVisualStyleFeaturesChecklist(features, path, add) {
      if (features.fontSize !== undefined && (!Number.isFinite(features.fontSize) || features.fontSize <= 0)) {
        add(`${path}.fontSize`, `${path}.fontSize must be a positive number when present.`);
      }

      ["fontIdentity", "fontFamily", "fontStyle", "weight", "strokeBucket", "strokeColor", "fillColor", "fillSource", "fillBlendMode", "alignment"].forEach((field) => {
        if (features[field] !== undefined && typeof features[field] !== "string") {
          add(`${path}.${field}`, `${path}.${field} must be a string when present.`);
        }
      });

      ["syntheticBold", "syntheticItalic", "strokeEnabled"].forEach((field) => {
        if (features[field] !== undefined && typeof features[field] !== "boolean") {
          add(`${path}.${field}`, `${path}.${field} must be a boolean when present.`);
        }
      });

      ["tracking", "leading", "strokeWidth", "fillAlpha", "strokeAlpha"].forEach((field) => {
        if (features[field] !== undefined && !Number.isFinite(features[field])) {
          add(`${path}.${field}`, `${path}.${field} must be a finite number when present.`);
        }
      });
    }


    function isBindingCollectDisabled(node) {
      return node &&
        node.binding &&
        typeof node.binding === "object" &&
        !Array.isArray(node.binding) &&
        node.binding.collect === false;
    }


    function isStringArray(value) {
      return Array.isArray(value) && value.every((item) => typeof item === "string");
    }


    function resolveTextKeySeed(node) {
      if (node.text && typeof node.text.key === "string" && node.text.key.trim()) {
        return node.text.key;
      }

      const fromName = normalizeTextKey(String(node.name || "").replace(/Text$/i, ""));
      if (fromName) {
        return fromName;
      }

      return node.text ? node.text.text : "";
    }


    function validateRectChecklist(rect, path, add) {
      if (!rect || typeof rect !== "object") {
        add(path, `${path} must be an object.`);
        return;
      }

      ["anchorMin", "anchorMax", "pivot", "anchoredPosition", "sizeDelta"].forEach((field) => {
        if (!isNumberArray(rect[field], 2)) {
          add(`${path}.${field}`, `${path}.${field} must be a 2-number array.`);
        }
      });
    }


    function validatePrefabRefOverrideRectChecklist(rect, path, add) {
      validateRectChecklist(rect, path, add);
      if (!rect || typeof rect !== "object") {
        return;
      }

      ["anchorMin", "anchorMax", "pivot"].forEach((field) => {
        if (
          isNumberArray(rect[field], 2) &&
          (rect[field][0] !== 0.5 || rect[field][1] !== 0.5)
        ) {
          add(
            `${path}.${field}`,
            `${path}.${field} must be [0.5, 0.5] for a PrefabRef override.`
          );
        }
      });
    }


    function validateResourceChecklist(resource, path, validResourceTypes, add, options = {}) {
      if (!resource || typeof resource !== "object") {
        add(path, `${path} must be an object.`);
        return;
      }

      if (!isNonEmptyString(resource.asset)) {
        add(`${path}.asset`, `${path}.asset must be a non-empty string.`);
      }
      const requireSha256 = options.requireSha256 !== false;
      const hasPreviewSha256 = resource.sha256 !== undefined && resource.sha256 !== null;
      if ((requireSha256 || hasPreviewSha256) && !isSha256Hex(resource.sha256)) {
        add(`${path}.sha256`, `${path}.sha256 must be a 64-character sha256 hex string.`);
      }
      if (resource.contentHash !== undefined && !isSha256Hex(resource.contentHash)) {
        add(`${path}.contentHash`, `${path}.contentHash must be a 64-character sha256 hex string.`);
      }
      if (resource.contentHash !== undefined && !isNonEmptyString(resource.contentHashAlgorithm)) {
        add(`${path}.contentHashAlgorithm`, `${path}.contentHashAlgorithm must be a non-empty string when contentHash is present.`);
      }
      if (resource.contentHash === undefined && resource.contentHashAlgorithm !== undefined) {
        add(`${path}.contentHash`, `${path}.contentHash must be present when contentHashAlgorithm is present.`);
      }
      if (!Number.isInteger(resource.width) || resource.width < 0) {
        add(`${path}.width`, `${path}.width must be a non-negative integer.`);
      }
      if (!Number.isInteger(resource.height) || resource.height < 0) {
        add(`${path}.height`, `${path}.height must be a non-negative integer.`);
      }
      if (!validResourceTypes.has(resource.resourceType)) {
        add(`${path}.resourceType`, `${path}.resourceType is not a supported resource type.`);
      }
      if (resource.border) {
        ["left", "top", "right", "bottom"].forEach((field) => {
          const value = resource.border[field];
          if (typeof value !== "number" || !Number.isFinite(value) || value < 0) {
            add(`${path}.border.${field}`, `${path}.border.${field} must be a non-negative number.`);
          }
        });
      }
    }

    function validateSafeAreaRegionChecklist(targets, nodes, safeAreaRegionNodeTypes, add) {
      if (!Array.isArray(targets) || !Array.isArray(nodes)) {
        return;
      }

      const nodesById = {};
      nodes.forEach((node) => {
        if (node && isNonEmptyString(node.id)) {
          nodesById[node.id] = node;
        }
      });

      const regionNodeIdsInTargets = new Set();
      targets.forEach((target, targetIndex) => {
        if (!target || typeof target !== "object" || !Array.isArray(target.nodeIds)) {
          return;
        }

        target.nodeIds.forEach((nodeId) => {
          const node = nodesById[nodeId];
          if (!node || !safeAreaRegionNodeTypes.has(node.type)) {
            return;
          }

          regionNodeIdsInTargets.add(node.id);
          if (!["Page", "SubPage"].includes(target.targetType)) {
            add(`targets[${targetIndex}].nodeIds`, `${node.type} Page region nodes are only valid under Page targets.`);
          }
          if (node.parentId !== target.sourceRootNodeId) {
            add(`nodes.${node.id}.parentId`, `${node.type} Page region node must be a direct child of the Page target root.`);
          }
          if (node.layout) {
            add(`nodes.${node.id}.layout`, `${node.type} Page region node cannot use layout options.`);
          }
          if (hasSafeAreaRegionPayload(node)) {
            add(`nodes.${node.id}`, `${node.type} Page region node cannot carry resource, text, or prefabRef payload.`);
          }
        });
      });

      nodes.forEach((node) => {
        if (node && safeAreaRegionNodeTypes.has(node.type) && !regionNodeIdsInTargets.has(node.id)) {
          add(`nodes.${node.id}`, `${node.type} Page region node must belong to a Page target.`);
        }
      });
    }

    function hasSafeAreaRegionPayload(node) {
      return Boolean(node && (node.resource || node.text || node.prefabRef));
    }

    function validatePrefabRefsChecklist(targets, nodes, add, resourceValidationOptions = {}) {
      if (!Array.isArray(targets) || !Array.isArray(nodes)) {
        return;
      }

      const targetTypesById = {};
      const targetsById = {};
      const targetTypesByNodeId = {};
      const nodesById = {};
      const validResourceTypes = resourceValidationOptions.validResourceTypes
        || new Set(["Atlas", "StaticTexture", "DynamicTexture"]);
      nodes.forEach((node) => {
        if (node && isNonEmptyString(node.id)) {
          nodesById[node.id] = node;
        }
      });
      targets.forEach((target) => {
        if (!target || typeof target !== "object") {
          return;
        }

        if (isNonEmptyString(target.targetId)) {
          targetTypesById[target.targetId] = target.targetType || "";
          targetsById[target.targetId] = target;
        }

        if (!Array.isArray(target.nodeIds)) {
          return;
        }

        target.nodeIds.forEach((nodeId) => {
          if (!isNonEmptyString(nodeId)) {
            return;
          }
          targetTypesByNodeId[nodeId] = targetTypesByNodeId[nodeId] || new Set();
          if (isNonEmptyString(target.targetType)) {
            targetTypesByNodeId[nodeId].add(target.targetType);
          }
        });
      });

      nodes.forEach((node) => {
        if (!node || typeof node !== "object") {
          return;
        }

        if (node.type === "PrefabRef") {
          validatePrefabRefIdentity(node, add);
          validatePrefabRefImageOverrides(
            node,
            targetsById,
            nodesById,
            validResourceTypes,
            add,
            resourceValidationOptions
          );
        }

        if (!isGeneratedComponentMount(node)) {
          return;
        }

        const prefabRef = node.prefabRef || {};
        const targetType = targetTypesById[prefabRef.targetId] || "";
        if (isNonEmptyString(prefabRef.targetId) && targetType !== "Component") {
          add(`nodes.${node.id}.prefabRef.targetId`, "Generated Component prefabRef must point to a Component target.");
        }

        const ownerTypes = targetTypesByNodeId[node.id] || new Set();
        if (ownerTypes.size !== 1 || Array.from(ownerTypes).some((type) => !["Page", "SubPage", "Popup", "Component", "ListItem"].includes(type))) {
          add(`nodes.${node.id}`, "Generated Component prefabRef must belong to exactly one supported target subtree.");
        }
      });
    }

    function validatePrefabRefIdentity(node, add) {
      const prefabRef = node?.prefabRef;
      const path = `nodes.${node.id}.prefabRef`;
      if (!prefabRef || typeof prefabRef !== "object") {
        add(path, "PrefabRef node must include prefabRef.");
        return;
      }

      if (prefabRef.kind === "ExternalPrefab") {
        if (!isNonEmptyString(prefabRef.prefabName)) {
          add(`${path}.prefabName`, "ExternalPrefab must include prefabName.");
        } else if (!isPsdExpressiblePrefabName(prefabRef.prefabName)) {
          add(`${path}.prefabName`, "ExternalPrefab prefabName must round-trip through @PrefabRef_<PrefabName>_<VariableName> without whitespace, @, leading/trailing underscores, or consecutive underscores.");
        }
        if (Object.prototype.hasOwnProperty.call(prefabRef, "assetGuid") &&
            (typeof prefabRef.assetGuid !== "string" || !/^[0-9a-fA-F]{32}$/.test(prefabRef.assetGuid))) {
          add(`${path}.assetGuid`, "Explicit assetGuid must contain exactly 32 hexadecimal characters.");
        }
        ["name", "assetPath", "targetId"].forEach((field) => {
          if (prefabRef[field] !== undefined) {
            add(`${path}.${field}`, `ExternalPrefab does not accept legacy ${field}.`);
          }
        });
      } else if (prefabRef.kind === "GeneratedTarget") {
        if (!isNonEmptyString(prefabRef.name)) {
          add(`${path}.name`, "GeneratedTarget must include name.");
        }
        if (!isNonEmptyString(prefabRef.targetId)) {
          add(`${path}.targetId`, "GeneratedTarget must include targetId.");
        }
        ["prefabName", "assetGuid"].forEach((field) => {
          if (prefabRef[field] !== undefined) {
            add(`${path}.${field}`, `GeneratedTarget does not accept ExternalPrefab field ${field}.`);
          }
        });
      } else {
        add(`${path}.kind`, "prefabRef.kind must be ExternalPrefab or GeneratedTarget.");
      }
    }

    function validatePrefabRefImageOverrides(node, targetsById, nodesById, validResourceTypes, add, resourceValidationOptions) {
      const overrides = node?.prefabRef?.overrides;
      if (overrides === undefined || overrides === null) {
        return;
      }

      if (!Array.isArray(overrides)) {
        add(`nodes.${node.id}.prefabRef.overrides`, "PrefabRef overrides must be an array.");
        return;
      }

      const elements = new Set();
      const generatedTarget = node?.prefabRef?.kind === "GeneratedTarget";
      overrides.forEach((elementOverride, index) => {
        const path = `nodes.${node.id}.prefabRef.overrides.${index}`;
        if (!elementOverride || typeof elementOverride !== "object") {
          add(path, "PrefabRef override must be an object.");
          return;
        }

        if (!isNonEmptyString(elementOverride.slot)) {
          add(`${path}.slot`, "PrefabRef override slot must be a non-empty string.");
        }
        if (!isNonEmptyString(elementOverride.element)) {
          add(`${path}.element`, "PrefabRef override element must be a non-empty string.");
        }

        const replacementKey = generatedTarget
          ? (elementOverride.element || "")
          : `${elementOverride.slot || ""}\u0000${elementOverride.element || ""}`;
        if (isNonEmptyString(elementOverride.slot) && isNonEmptyString(elementOverride.element)) {
          if (elements.has(replacementKey)) {
            add(
              `${path}.element`,
              generatedTarget
                ? "GeneratedTarget cannot override the same element more than once, even from different slots."
                : "ExternalPrefab cannot override the same slot + element twice."
            );
          } else {
            elements.add(replacementKey);
          }

          if (node?.prefabRef?.kind === "GeneratedTarget") {
            const target = targetsById[node.prefabRef.targetId];
            const expectedType = elementOverride.resource
              ? "Image"
              : (elementOverride.text ? "Text" : "");
            const matches = (Array.isArray(target?.nodeIds) ? target.nodeIds : [])
              .map((nodeId) => nodesById[nodeId])
              .filter((candidate) => (
                candidate?.type === expectedType
                && candidate?.name === elementOverride.element
              ));
            if (matches.length !== 1) {
              add(
                `${path}.element`,
                `GeneratedTarget override element must match exactly one ${expectedType || "supported"} node name in its Component target; actual matches: ${matches.length}.`
              );
            }
          }
        }

        const hasResource = Boolean(elementOverride.resource && typeof elementOverride.resource === "object" && !Array.isArray(elementOverride.resource));
        const hasText = Boolean(elementOverride.text && typeof elementOverride.text === "object" && !Array.isArray(elementOverride.text));
        if (hasResource === hasText) {
          add(path, "PrefabRef override must contain exactly one resource or text payload.");
        } else if (hasResource) {
          validateResourceChecklist(
            elementOverride.resource,
            `${path}.resource`,
            validResourceTypes,
            add,
            resourceValidationOptions
          );
        } else if (hasText) {
          validateTextKeySeedChecklist(
            {
              type: "Text",
              text: elementOverride.text
            },
            `${path}.text`,
            add
          );
        }
        validatePrefabRefOverrideRectChecklist(elementOverride.rect, `${path}.rect`, add);
      });
    }

    function isGeneratedComponentMount(node) {
      const main = node?.source?.naming?.main;
      return Boolean(
        node?.type === "PrefabRef" &&
        node?.prefabRef?.kind === "GeneratedTarget" &&
        main?.kind === "target" &&
        main?.id === "Component"
      );
    }


    function isBlockingWarning(warning) {
      return getWarningPriority(warning) === "blocking";
    }


    function isNonEmptyString(value) {
      return typeof value === "string" && value.trim().length > 0;
    }


    function isPsdExpressiblePrefabName(value) {
      return isNonEmptyString(value)
        && !/[\s@]/.test(value)
        && !value.startsWith("_")
        && !value.endsWith("_")
        && !value.includes("__");
    }


    function isNumberArray(value, length) {
      return Array.isArray(value)
        && value.length === length
        && value.every((item) => typeof item === "number" && Number.isFinite(item));
    }


    function isSha256Hex(value) {
      return typeof value === "string" && /^[0-9a-fA-F]{64}$/.test(value);
    }

    function isValidPageTargetName(value) {
      return typeof value === "string" && value.length > "Page".length && value.endsWith("Page");
    }

    function isValidPopupTargetName(value) {
      return typeof value === "string" && value.length > "Popup".length && value.endsWith("Popup");
    }

    function validateTextKeySeed(value) {
      const normalized = normalizeTextKey(value);
      if (!normalized) {
        return { valid: false, normalized, reason: "seed normalizes to an empty key" };
      }

      return { valid: true, normalized, reason: "" };
    }


    function normalizeTextKey(value) {
      return String(value || "")
        .trim()
        .replace(/([a-z0-9])([A-Z])/g, "$1_$2")
        .toLowerCase()
        .replace(/[\s\-\\/]+/g, "_")
        .replace(/[^a-z0-9_]/g, "")
        .replace(/_+/g, "_")
        .replace(/^_+|_+$/g, "");
    }


    return { assertExportableBui, validateBuiChecklist, validateBuiPreviewChecklist, validateSourceChecklist, validateCanvasChecklist, validateTargetsChecklist, validateNodesChecklist, validateRectChecklist, validatePrefabRefOverrideRectChecklist, validateResourceChecklist, validateSafeAreaRegionChecklist, validatePrefabRefsChecklist, isBlockingWarning, isNonEmptyString, isNumberArray, isSha256Hex, isValidPageTargetName, isValidPopupTargetName };
  }

  global.BurnerPsdToBuiChecklist = { createBuiChecklist };
})(globalThis);
