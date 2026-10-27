/* global globalThis */

(function registerNamingSemantics(global) {
  function createNamingSemantics(options) {
    const { arrayFrom, toNumber, getNamingRuleIndex } = options;
    const NAMING_RULES_CONFIG_PATH = options.namingRulesConfigPath;

    function pushIgnoreWarning(warnings, sourcePath, naming, namingRules) {
      warnings.push({
        code: namingRules.warningCodes?.ignoredSubtree || "PSD_IGNORE_MARK_APPLIED",
        severity: "Info",
        message: `Layer or group starts with ${naming.ignorePrefix || "configured ignore prefix"} and was skipped with its whole subtree.`,
        sourcePath
      });
    }


    function resolveTargetRule(naming, namingRules) {
      if (!naming || !naming.main || naming.main.kind !== "target") {
        return null;
      }

      return naming.main.rule || getNamingRuleIndex(namingRules).targetByFlag[naming.main.flag] || null;
    }


    function resolveNodeSemantics(naming, layerType, isGroup, context, namingRules, warnings, sourcePath) {
      pushNamingParseWarnings(naming, namingRules, warnings, sourcePath);

      const index = getNamingRuleIndex(namingRules);
      const defaultNodeType = namingRules.defaults?.nodeTypeByLayerType?.[layerType] || "Image";
      let nodeRule = null;
      let nodeType = defaultNodeType;
      let nodeName = naming.main?.value || naming.displayName || naming.rawName || "Layer";
      let prefabRef = null;
      let layout = "";
      let nineSlice = null;
      let outputModule = "";
      let exportBounds = "Canvas";
      let exportBoundsRequested = false;
      let binding = null;
      let skipChildren = false;
      let skipWarningCode = "";
      let shouldSlice = false;
      let sliceMode = "None";
      let requestedResourceType = "";
      let exportOnly = false;
      let prefabOverride = false;
      let prefabOverrideSlot = "";

      if (naming.main?.kind === "node") {
        nodeRule = naming.main.rule;
        nodeType = nodeRule.buiNodeType || nodeRule.id || defaultNodeType;
        skipChildren = Boolean(nodeRule.skipChildren);
      } else if (naming.main?.kind === "target") {
        warnings.push({
          code: namingRules.warningCodes?.invalidFlagContext || "PSD_FLAG_CONTEXT_INVALID",
          severity: "Warning",
          message: "TargetType flags are only valid on PSD root groups, except configured list-item placeholders directly under configured list nodes.",
          sourcePath
        });
      }

      if (!nodeRule) {
        nodeRule = index.nodeByBuiType[nodeType] || null;
      }

      if (nodeRule) {
        const invalidNodeSeverity = isPageRegionRule(nodeRule) ? "Blocking" : "Warning";
        if (isGroup && nodeRule.allowedOnGroup === false) {
          warnings.push({
            code: namingRules.warningCodes?.invalidFlagContext || "PSD_FLAG_CONTEXT_INVALID",
            severity: invalidNodeSeverity,
            message: `${naming.main?.token || nodeRule.id} is configured for leaf layers, but this PSD item is a group.`,
            sourcePath
          });
        }

        if (!isGroup && nodeRule.allowedOnLeaf === false) {
          warnings.push({
            code: namingRules.warningCodes?.invalidFlagContext || "PSD_FLAG_CONTEXT_INVALID",
            severity: invalidNodeSeverity,
            message: `${naming.main?.token || nodeRule.id} is configured for groups, but this PSD item is a leaf layer.`,
            sourcePath
          });
        }

        validateSafeAreaRegionNode(nodeRule, context, namingRules, warnings, sourcePath);

        if (nodeRule.id === "PrefabRef") {
          const resolvedNaming = resolvePrefabRefNaming(naming);
          nodeName = resolvedNaming.variableName;
          if (!resolvedNaming.prefabName || !resolvedNaming.variableName || resolvedNaming.paramCount < 2) {
            warnings.push({
              code: "PSD_PREFAB_REF_NAME_OR_VARIABLE_MISSING",
              severity: "Blocking",
              message: "@PrefabRef_<PrefabName>_<VariableName> must include both the Prefab name and Unity binding variable name.",
              sourcePath
            });
          } else if (!resolvedNaming.roundTrips) {
            warnings.push({
              code: "PSD_PREFAB_REF_NAME_INVALID",
              severity: "Blocking",
              message: "PrefabName must round-trip through @PrefabRef_<PrefabName>_<VariableName>; do not use leading, trailing, or consecutive underscores.",
              sourcePath
            });
          }
          prefabRef = {
            kind: "ExternalPrefab",
            prefabName: resolvedNaming.prefabName
          };
          skipChildren = true;
          skipWarningCode = namingRules.warningCodes?.prefabRefChildrenSkipped || "PSD_PREFAB_REF_CHILDREN_SKIPPED";
        }
      }

      for (const option of naming.options) {
        const optionResult = applyNamingOption(option, nodeType, isGroup, namingRules, warnings, sourcePath);
        if (!optionResult) {
          continue;
        }

        if (Object.prototype.hasOwnProperty.call(optionResult, "assetGuid") && prefabRef) prefabRef.assetGuid = optionResult.assetGuid;
        if (optionResult.outputModule) outputModule = optionResult.outputModule;
        if (optionResult.exportBounds) {
          exportBounds = optionResult.exportBounds;
          exportBoundsRequested = true;
        }
        if (optionResult.exportOnly) {
          exportOnly = true;
        }
        if (optionResult.resourceType) {
          requestedResourceType = optionResult.resourceType;
        }
        if (optionResult.sliceMode) {
          sliceMode = optionResult.sliceMode;
        }
        if (optionResult.layout) {
          layout = optionResult.layout;
        }
        if (optionResult.nineSlice) {
          nineSlice = optionResult.nineSlice;
        }
        if (optionResult.binding) {
          binding = optionResult.binding;
        }
        if (optionResult.prefabOverride) {
          if (context?.isPrefabRefOverride) {
            prefabOverride = true;
            prefabOverrideSlot = optionResult.prefabOverrideSlot || "";
          } else {
            warnings.push({
              code: "PSD_PREFAB_OVERRIDE_CONTEXT_INVALID",
              severity: "Blocking",
              message: `${option.token} is only valid on an Image or Text leaf inside a PrefabRef design subtree.`,
              sourcePath
            });
          }
        }
      }

      const isRenderableNode = nodeType === "Image" || nodeType === "RawImage" || nodeType === "Button";
      const compositeRequested = sliceMode === "Composite";
      const dynamicTextureRequested = requestedResourceType === "DynamicTexture";
      const materializationRequired = isGroup && compositeRequested;
      if (exportOnly && (dynamicTextureRequested || prefabOverride || binding)) {
        warnings.push({
          code: "PSD_EXPORT_ONLY_CONFLICT",
          severity: "Blocking",
          message: "@ExportOnly cannot be combined with @DynamicTexture, @PrefabOverride or @NoBind; it exports a PNG without a UI node.",
          sourcePath
        });
      }
      if (compositeRequested && dynamicTextureRequested) {
        warnings.push({
          code: namingRules.warningCodes?.compositeDynamicConflict || "PSD_COMPOSITE_DYNAMIC_CONFLICT",
          severity: "Blocking",
          message: "@Composite cannot be combined with @DynamicTexture because a runtime texture must not export a PSD PNG.",
          sourcePath
        });
      }

      if (isGroup && isRenderableNode) {
        const staticResourceRequested = Boolean(requestedResourceType) && !dynamicTextureRequested;
        const requiresMaterialization = Boolean(nodeRule?.groupRequiresMaterialization) || staticResourceRequested;
        if (!compositeRequested && !dynamicTextureRequested && requiresMaterialization) {
          warnings.push({
            code: namingRules.warningCodes?.groupRenderModeRequired || "PSD_GROUP_RENDER_MODE_REQUIRED",
            severity: "Blocking",
            message: `${naming.main?.token || nodeRule?.id || nodeType} on a group must use @Composite for one merged PSD slice or @DynamicTexture for one runtime resource node.`,
            sourcePath
          });
        }
        if (compositeRequested || dynamicTextureRequested) {
          skipChildren = true;
        }
      }

      if (materializationRequired) {
        warnings.push({
          code: namingRules.warningCodes?.materializationRequired || "PSD_RENAME_MATERIALIZATION_REQUIRED",
          severity: "Blocking",
          message: "@Composite Group must be materialized by Burner PSD AI Rename before BUI export. BUI does not convert Smart Objects, rasterize Groups, or provide legacy Composite compatibility.",
          sourcePath
        });
      }

      shouldSlice = resolveSlicePolicy(nodeType, nodeRule, isGroup, namingRules, sliceMode);
      if (dynamicTextureRequested || materializationRequired) {
        shouldSlice = false;
      }
      if (outputModule && (dynamicTextureRequested || (!shouldSlice && !materializationRequired))) {
        warnings.push({ code: "PSD_OUTPUT_MODULE_CONTEXT_INVALID", severity: "Blocking",
          message: "@Module requires a PNG-producing resource; it cannot route DynamicTexture or a non-rendering container.", sourcePath });
      }
      if (exportBoundsRequested && (!shouldSlice || dynamicTextureRequested)) {
        warnings.push({ code: "PSD_EXPORT_BOUNDS_CONTEXT_INVALID", severity: "Blocking",
          message: "@ExportBounds requires a PNG-producing leaf layer; it cannot apply to DynamicTexture or a container.", sourcePath });
      }
      sliceMode = shouldSlice ? (isGroup ? "Composite" : "Layer") : "None";
      const resourceType = dynamicTextureRequested
        ? "DynamicTexture"
        : shouldSlice
          ? requestedResourceType || nodeRule?.defaultResourceType || getConfiguredDefaultResourceType(namingRules)
          : "None";

      return {
        nodeType,
        nodeName,
        prefabRef,
        layout,
        nineSlice,
        outputModule,
        exportBounds,
        binding,
        skipChildren,
        skipWarningCode,
        shouldSlice,
        sliceMode,
        exportOnly,
        prefabOverride,
        prefabOverrideSlot,
        materializationRequired,
        resourceType,
        namingSource: buildNamingSource(naming, nodeRule)
      };
    }

    function resolvePrefabRefNaming(naming) {
      const params = Array.isArray(naming?.main?.params)
        ? naming.main.params.filter((param) => param !== "")
        : [];
      const fallback = naming?.main?.value || "";
      if ((naming?.main?.rule?.id === "Component" || naming?.main?.id === "Component") && /^UI[A-Z][A-Za-z0-9]{0,15}_[A-Z][A-Za-z0-9]*$/.test(fallback)) {
        return { prefabName: fallback, variableName: fallback.replace("_", "") + (fallback.endsWith("Component") ? "" : "Component"), paramCount: 2, roundTrips: true };
      }
      if (params.length <= 1) {
        return {
          prefabName: fallback,
          variableName: fallback,
          paramCount: params.length,
          roundTrips: false
        };
      }

      const prefabName = params.slice(0, -1).join("_");
      const variableName = params[params.length - 1];
      return {
        prefabName,
        variableName,
        paramCount: params.length,
        roundTrips: fallback === `${prefabName}_${variableName}`
      };
    }


    function pushNamingParseWarnings(naming, namingRules, warnings, sourcePath) {
      if (!naming) {
        return;
      }

      naming.unknownFlags.forEach((flag) => {
        warnings.push({
          code: flag.hasValue
            ? namingRules.warningCodes?.unknownMainFlag || "PSD_UNKNOWN_MAIN_FLAG"
            : namingRules.warningCodes?.unknownOptionFlag || "PSD_UNKNOWN_OPTION_FLAG",
          severity: "Warning",
          message: `Unknown naming flag ${flag.token}. It was kept in sourcePath only and did not affect export semantics.`,
          sourcePath
        });
      });

      if (naming.extraMainFlags.length > 0) {
        const hasSafeAreaRegionMain = isPageRegionRule(naming.main?.rule) ||
          naming.extraMainFlags.some((flag) => isPageRegionRule(flag.rule));
        warnings.push({
          code: hasSafeAreaRegionMain
            ? namingRules.warningCodes?.safeAreaRegionMainConflict || "PSD_SAFE_AREA_REGION_MAIN_CONFLICT"
            : namingRules.warningCodes?.multipleMainFlags || "PSD_MULTIPLE_MAIN_FLAGS",
          severity: hasSafeAreaRegionMain ? "Blocking" : "Warning",
          message: hasSafeAreaRegionMain
            ? `Page region NodeType must be the only main flag. Found ${naming.main.token} and ${naming.extraMainFlags.map((flag) => flag.token).join(", ")}.`
            : `Multiple main flags were found. The exporter used ${naming.main.token} and ignored ${naming.extraMainFlags.map((flag) => flag.token).join(", ")}.`,
          sourcePath
        });
      }

      const uniqueOptions = new Map();
      naming.options.forEach((option) => {
        const ruleId = option.rule?.id || option.flag;
        if (option.rule?.kind !== "binding" && ruleId !== "NoBind" && option.rule?.unique !== true) {
          return;
        }

        if (!uniqueOptions.has(ruleId)) {
          uniqueOptions.set(ruleId, []);
        }
        uniqueOptions.get(ruleId).push(option.token);
      });

      uniqueOptions.forEach((tokens, ruleId) => {
        if (tokens.length <= 1) {
          return;
        }

        warnings.push({
          code: namingRules.warningCodes?.duplicateUniqueOption || "PSD_DUPLICATE_UNIQUE_OPTION",
          severity: "Blocking",
          message: `${ruleId} is a unique option and cannot appear more than once. Found ${tokens.join(", ")}.`,
          sourcePath
        });
      });
    }


    function pushTargetRootNamingWarnings(naming, namingRules, warnings, sourcePath) {
      pushNamingParseWarnings(naming, namingRules, warnings, sourcePath);

      if (naming.options.some((option) => option.rule?.kind === "prefabAsset")) {
        warnings.push({ code: "PSD_PREFAB_ASSET_CONTEXT_INVALID", severity: "Blocking",
          message: "@PrefabAsset is only valid on a PrefabRef node, not a target root.", sourcePath });
      }
      if (naming.options.some((option) => option.rule?.kind === "outputModule")) {
        warnings.push({ code: "PSD_OUTPUT_MODULE_CONTEXT_INVALID", severity: "Blocking",
          message: "@Module is resource-local and cannot mark a target root or inherit to its subtree.", sourcePath });
      }
      if (naming.options.some((option) => option.rule?.kind === "exportOnly")) {
        warnings.push({
          code: "PSD_EXPORT_ONLY_CONTEXT_INVALID",
          severity: "Blocking",
          message: "@ExportOnly must mark an Image or RawImage leaf inside a target, not a target root.",
          sourcePath
        });
      }
      if (naming.options.some(option => option.rule?.kind === "exportBounds")) {
        warnings.push({ code: "PSD_EXPORT_BOUNDS_CONTEXT_INVALID", severity: "Blocking",
          message: "@ExportBounds is resource-local and cannot mark a target root or inherit to its subtree.", sourcePath });
      }
      const noBindTokens = naming.options
        .filter((option) => (option.rule?.id || option.flag) === "NoBind")
        .map((option) => option.token);
      if (noBindTokens.length === 0) {
        return;
      }

      warnings.push({
        code: namingRules.warningCodes?.targetNoBindIgnored || "PSD_TARGET_NOBIND_IGNORED",
        severity: "Warning",
        message: `${noBindTokens.join(", ")} on target root has no binding effect. Mark static child nodes with @NoBind instead.`,
        sourcePath
      });
    }


    function applyNamingOption(option, nodeType, isGroup, namingRules, warnings, sourcePath) {
      const rule = option.rule;
      if (!rule) {
        return null;
      }

      if (!arrayFrom(rule.validOnNodeTypes).includes(nodeType)) {
        const safeAreaRegionLayout = rule.kind === "layout" && isPageRegionNodeType(nodeType, namingRules);
        warnings.push({
          code: safeAreaRegionLayout
            ? namingRules.warningCodes?.safeAreaRegionLayoutInvalid || "PSD_SAFE_AREA_REGION_LAYOUT_INVALID"
            : rule.warningCode || namingRules.warningCodes?.invalidFlagContext || "PSD_FLAG_CONTEXT_INVALID",
          severity: safeAreaRegionLayout ? "Blocking" : rule.invalidSeverity || "Warning",
          message: safeAreaRegionLayout
            ? `${option.token} cannot be combined with Page region node type ${nodeType}.`
            : `${option.token} is not valid on configured node type ${nodeType}.`,
          sourcePath
        });
        return null;
      }

      if ((isGroup && rule.allowedOnGroup === false) || (!isGroup && rule.allowedOnLeaf === false)) {
        warnings.push({
          code: rule.warningCode || namingRules.warningCodes?.invalidFlagContext || "PSD_FLAG_CONTEXT_INVALID",
          severity: rule.invalidSeverity || "Warning",
          message: `${option.token} is not valid on a ${isGroup ? "group" : "leaf"} layer.`,
          sourcePath
        });
        return null;
      }

      if (rule.kind === "prefabAsset") {
        const guid = option.value || "";
        if (!/^[0-9a-fA-F]{32}$/.test(guid)) warnings.push({ code: "PSD_PREFAB_ASSET_GUID_INVALID", severity: "Blocking",
          message: "@PrefabAsset requires exactly one 32-character Unity asset GUID.", sourcePath });
        // Preserve presence even when invalid: explicit references must never fall back to name matching.
        return { assetGuid: guid.toLowerCase() };
      }

      if (rule.kind === "outputModule") {
        // Preserve the entire suffix, including consecutive underscores.
        const module = option.value || "";
        if (!/^[A-Za-z0-9_][A-Za-z0-9_.-]*$/.test(module) || module.endsWith(".") ||
            /^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])($|\.)/i.test(module)) {
          warnings.push({ code: "PSD_OUTPUT_MODULE_INVALID", severity: "Blocking",
            message: `${option.token} requires one valid module folder name.`, sourcePath });
          return null;
        }
        return { outputModule: module };
      }

      if (rule.kind === "exportBounds") {
        if (option.value !== "Canvas" && option.value !== "Layer") {
          warnings.push({ code: "PSD_EXPORT_BOUNDS_INVALID", severity: "Blocking",
            message: "@ExportBounds requires exactly Canvas or Layer.", sourcePath });
          return null;
        }
        return { exportBounds: option.value };
      }

      if (rule.kind === "exportOnly") {
        return { exportOnly: true };
      }

      if (rule.kind === "resourceType") {
        return { resourceType: rule.value };
      }

      if (rule.kind === "sliceMode") {
        return { sliceMode: rule.value };
      }

      if (rule.kind === "layout") {
        return { layout: rule.value };
      }

      if (rule.kind === "binding") {
        return {
          binding: {
            collect: rule.collect !== false,
            source: rule.source || "PsdNoBind",
            reason: rule.reason || "static-control"
          }
        };
      }

      if (rule.kind === "prefabOverride") {
        const slot = option.params.join("_").trim();
        if (!slot) {
          warnings.push({
            code: rule.warningCode || "PSD_PREFAB_OVERRIDE_SLOT_MISSING",
            severity: "Blocking",
            message: `${option.token} must include the confirmed Catalog slot id.`,
            sourcePath
          });
          return null;
        }
        return { prefabOverride: true, prefabOverrideSlot: slot };
      }

      if (rule.kind === "border") {
        const values = option.params.map((param) => toNumber(param, NaN));
        if (values.length !== 4 || values.some((value) => !Number.isFinite(value) || value < 0)) {
          warnings.push({
            code: namingRules.warningCodes?.invalidOptionParams || "PSD_OPTION_PARAMS_INVALID",
            severity: "Warning",
            message: `${option.token} must provide four non-negative numbers: left, top, right, bottom.`,
            sourcePath
          });
          return null;
        }

        return {
          nineSlice: {
            left: values[0],
            top: values[1],
            right: values[2],
            bottom: values[3]
          }
        };
      }

      return null;
    }


    function resolveSlicePolicy(nodeType, nodeRule, isGroup, namingRules, sliceMode) {
      if (isGroup) {
        return sliceMode === "Composite" && nodeRule?.slice !== false;
      }

      if (nodeType === "Text" && namingRules.behaviors?.text?.slice === false) {
        return false;
      }

      if (nodeRule && typeof nodeRule.slice === "boolean") {
        return nodeRule.slice;
      }

      return nodeType === "Image" || nodeType === "RawImage" || nodeType === "Button";
    }

    function validateSafeAreaRegionNode(nodeRule, context, namingRules, warnings, sourcePath) {
      if (!isPageRegionRule(nodeRule)) {
        return;
      }

      const requiredTargetTypes = nodeRule.requiresTargetTypes || [nodeRule.requiresTargetType || "Page"];
      if (!requiredTargetTypes.includes(context?.targetType)) {
        warnings.push({
          code: namingRules.warningCodes?.safeAreaRegionContextInvalid || "PSD_SAFE_AREA_REGION_CONTEXT_INVALID",
          severity: "Blocking",
          message: `${nodeRule.id} Page region is only valid directly under @${requiredTargetTypes.join("_ / @")}_ root groups.`,
          sourcePath
        });
      }

      if (nodeRule.requiresDirectTargetChild !== false && !context?.isTargetRootChild) {
        warnings.push({
          code: namingRules.warningCodes?.safeAreaRegionContextInvalid || "PSD_SAFE_AREA_REGION_CONTEXT_INVALID",
          severity: "Blocking",
          message: `${nodeRule.id} Page region must be a direct child group of the Page target root.`,
          sourcePath
        });
      }
    }

    function isPageRegionNodeType(nodeType, namingRules) {
      const index = getNamingRuleIndex(namingRules);
      return isPageRegionRule(index.nodeByBuiType[nodeType] || index.nodeById[nodeType]);
    }

    function isPageRegionRule(rule) {
      return Boolean(rule && (rule.safeAreaRegion || rule.pageRegion));
    }


    function buildNamingSource(naming, primaryRule) {
      return {
        rawName: naming.rawName,
        displayName: naming.displayName || "",
        main: naming.main
          ? {
            token: naming.main.token,
            flag: naming.main.flag,
            kind: naming.main.kind,
            id: primaryRule?.id || naming.main.rule?.id || "",
            name: naming.main.value || ""
          }
          : null,
        options: naming.options.map((option) => ({
          token: option.token,
          flag: option.flag,
          id: option.rule?.id || "",
          value: option.value || "",
          params: option.params
        }))
      };
    }


    function shouldUseNoTargetFallback(namingRules) {
      return namingRules.rootPolicy?.noTargetFallback?.enabled !== false;
    }


    function getDefaultVariant(namingRules) {
      const value = namingRules.defaults?.variant;
      if (!value) {
        throw new Error(`${NAMING_RULES_CONFIG_PATH} is missing defaults.variant.`);
      }
      return value;
    }


    function getConfiguredDefaultResourceType(namingRules) {
      const value = namingRules.defaults?.resourceType;
      if (!value) {
        throw new Error(`${NAMING_RULES_CONFIG_PATH} is missing defaults.resourceType.`);
      }
      return value;
    }

    return { pushIgnoreWarning, resolveTargetRule, resolveNodeSemantics, resolvePrefabRefNaming, pushNamingParseWarnings, pushTargetRootNamingWarnings, applyNamingOption, resolveSlicePolicy, buildNamingSource, shouldUseNoTargetFallback, getDefaultVariant, getConfiguredDefaultResourceType };
  }

  global.BurnerPsdToBuiNamingSemantics = { createNamingSemantics };
})(globalThis);
