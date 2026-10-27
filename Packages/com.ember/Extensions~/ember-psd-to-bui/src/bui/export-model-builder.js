/* global globalThis */

(function registerExportModelBuilder(global) {
  function createExportModelBuilder(options) {
    const {
      arrayFrom,
      maybePromise,
      toNumber,
      safeString,
      stripExtension,
      makePageClassName,
      slugify,
      hashText,
      deriveResourceModuleName,
      parseTargetIdentity,
      getResourceNamingKey,
      parsePsdNaming,
      isIgnoredByNaming,
      pushIgnoreWarning,
      pushTargetRootNamingWarnings,
      resolveTargetRule,
      resolvePrefabRefNaming,
      shouldUseNoTargetFallback,
      createSyntheticTargetRoot,
      makeTargetRecord,
      finalizeTargets,
      countRootNodes,
      makeUniqueNodeId,
      nodeIdPrefix,
      sanitizeNodeName,
      sanitizeId,
      getDefaultVariant,
      buildNamingSource,
      isLayerGroup,
      isArtboardLayer,
      readLayerId,
      readLayerVisible,
      detectLayerType,
      resolveNodeSemantics,
      readLayerBounds,
      readLayerExportBounds,
      parseBoundsQuiet,
      makeRect,
      snapBounds,
      makeRectFromSnappedBounds,
      readTextInfo,
      appendLayerUncertaintyWarnings,
      tryExportLayerPng
    } = options;

    function buildCanvasInfo(doc, warnings) {
      const width = Math.round(toNumber(doc.width, 0));
      const height = Math.round(toNumber(doc.height, 0));

      if (width <= 0 || height <= 0) {
        warnings.push({
          code: "CANVAS_SIZE_UNAVAILABLE",
          severity: "Blocking",
          message: "Photoshop DOM did not expose a finite positive canvas width and height. Reopen or repair the PSD document before exporting BUI."
        });
      }

      return {
        width,
        height
      };
    }


    function collectExportRootItems(layers, namingRules, warnings) {
      const items = [];
      for (let i = 0; i < layers.length; i += 1) {
        const layer = layers[i];
        const name = safeString(layer.name, `Layer_${i}`);
        if (!isArtboardLayer(layer)) {
          items.push({
            layer,
            siblingIndex: items.length,
            sourcePath: name
          });
          continue;
        }

        const naming = parsePsdNaming(name, namingRules);
        if (isIgnoredByNaming(naming)) {
          pushIgnoreWarning(warnings, name, naming, namingRules);
          continue;
        }

        const childLayers = arrayFrom(layer.layers);
        for (let childIndex = 0; childIndex < childLayers.length; childIndex += 1) {
          const child = childLayers[childIndex];
          const childName = safeString(child.name, `Layer_${childIndex}`);
          items.push({
            layer: child,
            siblingIndex: items.length,
            sourcePath: `${name}/${childName}`
          });
        }
      }
      return items;
    }


    function appendDuplicateTargetWarnings(targets, warnings, namingRules) {
      const counts = new Map();
      for (const target of targets) {
        const name = safeString(target?.name, "").trim();
        if (!name) {
          continue;
        }
        counts.set(name, (counts.get(name) || 0) + 1);
      }

      for (const [name, count] of counts) {
        if (count <= 1) {
          continue;
        }
        warnings.push({
          code: namingRules.warningCodes?.duplicateTargetName || "PSD_DUPLICATE_TARGET_NAME",
          severity: "Blocking",
          message: `Duplicate target name "${name}" appears ${count} times. Target names must be unique before BUI export.`,
          sourcePath: name
        });
      }
    }

    function finalizeAndResolvePrefabRefs(targetDrafts, nodes, warnings) {
      const targets = finalizeTargets(targetDrafts, nodes);
      const componentTargets = targets.filter((target) => target?.targetType === "Component");
      for (const node of nodes) {
        if (node?.type !== "PrefabRef" || node?.prefabRef?.kind !== "ExternalPrefab" || Object.prototype.hasOwnProperty.call(node.prefabRef, "assetGuid")) {
          continue;
        }

        const prefabName = safeString(node.prefabRef.prefabName, "").trim();
        const matches = componentTargets.filter((target) => safeString(target?.name, "") === parseTargetIdentity(prefabName).name);
        if (matches.length !== 1) {
          continue;
        }

        const overrides = Array.isArray(node.prefabRef.overrides)
          ? node.prefabRef.overrides
          : [];
        node.prefabRef = {
          kind: "GeneratedTarget",
          name: matches[0].name,
          targetId: matches[0].targetId
        };
        if (overrides.length > 0) {
          const targetNodeIds = new Set(arrayFrom(matches[0].nodeIds));
          const generatedElements = new Set();
          for (const elementOverride of overrides) {
            const slot = safeString(elementOverride?.slot, "");
            const element = safeString(elementOverride?.element, "");
            if (element && generatedElements.has(element)) {
              warnings.push({
                code: "PSD_PREFAB_OVERRIDE_ELEMENT_DUPLICATE",
                severity: "Blocking",
                message: `GeneratedTarget element "${element}" is overridden more than once, including from different slots.`,
                nodeId: node.id,
                sourcePath: node.sourcePath
              });
            } else if (element) {
              generatedElements.add(element);
            }
            const expectedType = elementOverride?.resource
              ? "Image"
              : (elementOverride?.text ? "Text" : "");
            const slotMatches = nodes.filter((candidate) => (
              targetNodeIds.has(candidate?.id)
              && candidate?.type === expectedType
              && safeString(candidate?.name, "") === element
            ));
            if (slotMatches.length !== 1) {
              warnings.push({
                code: slotMatches.length === 0
                  ? "PSD_GENERATED_TARGET_OVERRIDE_SLOT_NOT_FOUND"
                  : "PSD_GENERATED_TARGET_OVERRIDE_SLOT_AMBIGUOUS",
                severity: "Blocking",
                message: `GeneratedTarget replacement "${slot}.${element}" must match exactly one ${expectedType || "supported"} element in Component target ${matches[0].name}; actual matches: ${slotMatches.length}.`,
                nodeId: node.id,
                sourcePath: node.sourcePath
              });
            }
          }
          node.prefabRef.overrides = overrides;
        }
      }
      return targets;
    }


    async function buildNodes(doc, canvas, exportBaseName, warnings, assetsFolder, namingRules, options = {}) {
      const skipAssets = Boolean(options.skipAssets);
      if (options.adaptation && !(options.adaptationReadOnly && skipAssets) && !options.adaptationSource) throw new Error("BUI_ADAPTATION_HANDOFF_REQUIRED: production export requires verified saved Rename adaptation.");
      function checkSubPageGate(layers) {
        for (const layer of arrayFrom(layers)) {
          const name = safeString(layer.name, ""), parsed = parsePsdNaming(name, namingRules);
          if (isIgnoredByNaming(parsed)) continue;
          if (!options.adaptation && /(^|\s)@(SubPage|子页)(_|\s|$)/.test(name)) throw new Error("BUI_V4_PRODUCTION_NOT_READY: SubPage production export awaits template/layout consumption.");
          checkSubPageGate(layer.layers);
        }
      }
      checkSubPageGate(doc.layers);
      if (skipAssets && options.sharedGroups?.length) throw new Error("Shared image requirements need a full pixel export; BUI-only cannot verify them.");
      const exportState = {
        resources: [],
        slicedCount: 0,
        reusedResourceCount: 0,
        missingReusableResourceCount: 0,
        missingReusableResourceSamples: [],
        usedAssetNames: {},
        moduleName: doc.moduleName || "",
        usedTargetIds: {},
        adaptation: options.adaptation || null,
        skipAssets,
        reusableResources: options.reusableResources || null,
        sharedGroups: options.sharedGroups || []
      };
      const nodes = [];
      const targetDrafts = [];
      const unmarkedRootLayers = [];

      const layers = arrayFrom(doc.layers);
      const rootItems = collectExportRootItems(layers, namingRules, warnings);
      if (layers.length === 0) {
        warnings.push({
          code: "DOCUMENT_LAYERS_UNAVAILABLE",
          severity: "Warning",
          message: "Photoshop DOM did not expose document layers."
        });
        const fallback = createSyntheticTargetRoot(exportBaseName, canvas, nodes, exportState, namingRules);
        return {
          nodes,
          resources: exportState.resources,
          targets: finalizeAndResolvePrefabRefs([fallback.target], nodes, warnings),
          previewRootNode: fallback.rootNode,
          slicedCount: exportState.slicedCount,
          reusedResourceCount: exportState.reusedResourceCount,
          missingReusableResourceCount: exportState.missingReusableResourceCount,
          missingReusableResourceSamples: exportState.missingReusableResourceSamples
        };
      }

      for (const item of rootItems) {
        const layer = item.layer;
        const name = safeString(layer.name, `Layer_${item.siblingIndex}`);
        const sourcePath = item.sourcePath;
        const naming = parsePsdNaming(name, namingRules);

        if (isIgnoredByNaming(naming)) {
          pushIgnoreWarning(warnings, sourcePath, naming, namingRules);
          continue;
        }

        const targetRule = resolveTargetRule(naming, namingRules);
        if (targetRule && targetRule.allowedAtRoot !== false) {
          const built = await buildTargetFromRootLayer(
            doc,
            layer,
            item.siblingIndex,
            canvas,
            nodes,
            warnings,
            assetsFolder,
            exportState,
            namingRules,
            targetRule,
            naming,
            targetDrafts,
            sourcePath
          );
          targetDrafts.push(built.target);
          continue;
        }

        unmarkedRootLayers.push({ layer, siblingIndex: item.siblingIndex, naming, sourcePath });
      }

      appendDuplicateTargetWarnings(targetDrafts, warnings, namingRules);

      if (targetDrafts.length === 0 && shouldUseNoTargetFallback(namingRules)) {
        const fallback = createSyntheticTargetRoot(exportBaseName, canvas, nodes, exportState, namingRules);
        warnings.push({
          code: namingRules.rootPolicy?.noTargetFallback?.warningCode || "PSD_NO_TARGET_ROOT_FALLBACK",
          severity: "Warning",
          message: "No configured root target group was found. The exporter used the rootPolicy.noTargetFallback target.",
          nodeId: fallback.rootNode.id,
          sourcePath: ""
        });

        for (let i = 0; i < unmarkedRootLayers.length; i += 1) {
          const item = unmarkedRootLayers[i];
          await visitLayer(
            doc,
            item.layer,
            fallback.rootNode.id,
            "",
            item.siblingIndex,
            canvas,
            nodes,
            warnings,
            assetsFolder,
            exportState,
            namingRules,
            {
              parentNodeType: fallback.rootNode.type,
              targetDrafts,
              targetType: fallback.target.targetType,
              isTargetRootChild: true,
              resourcePrefix: deriveResourceModuleName(fallback.target.name || exportBaseName, exportState.moduleName)
            }
          );
        }

        return {
          nodes,
          resources: exportState.resources,
          targets: finalizeAndResolvePrefabRefs([fallback.target], nodes, warnings),
          previewRootNode: fallback.rootNode,
          slicedCount: exportState.slicedCount,
          reusedResourceCount: exportState.reusedResourceCount,
          missingReusableResourceCount: exportState.missingReusableResourceCount,
          missingReusableResourceSamples: exportState.missingReusableResourceSamples
        };
      }

      for (const item of unmarkedRootLayers) {
        warnings.push({
          code: namingRules.warningCodes?.unmarkedRootIgnored || "PSD_UNMARKED_ROOT_IGNORED",
          severity: "Info",
          message: "Root layer/group has no configured TargetType flag and was ignored by the configured root policy.",
          sourcePath: item.sourcePath
        });
      }

      if (targetDrafts.length === 0) {
        throw new Error("No exportable root target was found. Add a configured TargetType flag to a top-level PSD group.");
      }

      const finalizedTargets = finalizeAndResolvePrefabRefs(targetDrafts, nodes, warnings);
      const adaptation = exportState.adaptation ? global.BurnerPsdToBuiAdaptation.project(exportState.adaptation, {targets: finalizedTargets, nodes}) : undefined;
      if (adaptation) {
        const issues = global.BurnerPsdToBuiAdaptation.validate({schemaVersion: 4, adaptation, targets: finalizedTargets, nodes, source: {psdSha256: options.psdSha256}});
        if (issues.length) throw new Error(issues.map(issue => issue.sourcePath + ": " + issue.message).join("\n"));
      }
      return {
        ...(adaptation ? {schemaVersion: 4, adaptation} : {}),
        nodes,
        resources: exportState.resources,
        targets: finalizedTargets,
        previewRootNode: nodes[0],
        slicedCount: exportState.slicedCount,
        reusedResourceCount: exportState.reusedResourceCount,
        missingReusableResourceCount: exportState.missingReusableResourceCount,
        missingReusableResourceSamples: exportState.missingReusableResourceSamples
      };
    }


    async function buildTargetFromRootLayer(sourceDoc, layer, siblingIndex, canvas, nodes, warnings, assetsFolder, exportState, namingRules, targetRule, naming, targetDrafts, sourcePathOverride, resourcePrefixOverride) {
      const name = safeString(layer.name, `Target_${siblingIndex}`);
      const sourcePath = sourcePathOverride || name;
      const childLayers = arrayFrom(layer.layers);
      const layerId = readLayerId(layer, sourcePath, warnings);
      const targetValue = (targetRule.targetType || targetRule.id) === "Component" ? resolvePrefabRefNaming(naming).prefabName : naming.main?.value;
      const identity = parseTargetIdentity(targetValue || makePageClassName(stripExtension(name)), targetRule.targetType || targetRule.id);
      const targetCanonicalName = sanitizeNodeName(identity.name);
      const resourceModule = identity.module || exportState.moduleName;
      const resourcePrefix = (!identity.module && resourcePrefixOverride) || deriveResourceModuleName(targetCanonicalName, resourceModule);
      pushTargetRootNamingWarnings(naming, namingRules, warnings, sourcePath);
      validateTargetNamingConvention(targetCanonicalName, targetRule, namingRules, warnings, sourcePath);
      const adaptationItem = exportState.adaptation?.targets?.[String(layerId)];
      if ((targetRule.targetType || targetRule.id) === "SubPage" && !adaptationItem) throw new Error("BUI_V4_PRODUCTION_NOT_READY: SubPage requires explicit adaptation and downstream template/layout support.");
      const explicitRect = adaptationItem?.designRect;
      const targetBounds = explicitRect ? {left: explicitRect[0], top: explicitRect[1], right: explicitRect[2], bottom: explicitRect[3]} : await readTargetContentBounds(layer, canvas, namingRules, sourcePath);
      const targetCoordinateSpace = createCoordinateSpace(targetBounds, canvas, Boolean(explicitRect));
      const rootId = makeUniqueNodeId(`target_${slugify(targetCanonicalName)}_root`, nodes);
      const rootNode = {
        id: rootId,
        parentId: null,
        name: targetCanonicalName,
        type: targetRule.nodeType || "Group",
        active: readLayerVisible(layer),
        siblingIndex,
        sourcePath,
        source: {
          layerId: layerId === null ? null : String(layerId),
          layerType: "group",
          resourceType: "None",
          variant: getDefaultVariant(namingRules),
          zIndex: siblingIndex,
          naming: buildNamingSource(naming, targetRule, null)
        },
        rect: makeRectInCoordinateSpace(targetBounds, targetCoordinateSpace, canvas)
      };
      nodes.push(rootNode);
      const target = makeTargetRecord(targetCanonicalName, rootId, targetRule, exportState);

      if (childLayers.length === 0) {
        warnings.push({
          code: namingRules.warningCodes?.invalidFlagContext || "PSD_FLAG_CONTEXT_INVALID",
          severity: "Warning",
          message: "A root TargetType flag should be placed on a PSD group. This target has no child layers.",
          nodeId: rootId,
          sourcePath
        });
      }

      for (let i = 0; i < childLayers.length; i += 1) {
        await visitLayer(
          sourceDoc,
          childLayers[i],
          rootId,
          sourcePath,
          i,
          canvas,
          nodes,
          warnings,
          assetsFolder,
          exportState,
          namingRules,
          {
            parentNodeType: rootNode.type,
            targetDrafts,
            targetType: targetRule.targetType || targetRule.id,
            targetId: target.targetId,
            isTargetRootChild: true,
            coordinateSpace: targetCoordinateSpace,
            resourcePrefix,
            resourceModule
          }
        );
      }

      return {
        rootNode,
        target,
        targetBounds
      };
    }

    async function exportResource(sourceDoc, layer, assetsFolder, nodeId, sourcePath, warnings, exportState, preferredName, semantics, resourcePrefix, resourceModule) {
      const resource = await tryExportLayerPng(sourceDoc, layer, assetsFolder, nodeId, sourcePath, warnings,
        exportState, preferredName, semantics.resourceType, semantics.nineSlice, resourcePrefix, semantics.outputModule, resourceModule, semantics.exportBounds);
      if (resource) resource.namingKey = exportResourceNamingKey(sourcePath, nodeId, preferredName, resourcePrefix, semantics);
      return resource;
    }

    function exportResourceNamingKey(sourcePath, nodeId, name, prefix, semantics) {
      const key = getResourceNamingKey(sourcePath, nodeId, name, prefix, semantics.outputModule);
      return semantics.exportBounds === "Layer" ? key + "|ExportBounds:Layer" : key;
    }

    function applyOutputModule(resource, semantics, moduleName) {
      if (!resource) return resource;
      if (semantics.outputModule || moduleName) resource.outputModule = semantics.outputModule || moduleName;
      else delete resource.outputModule; // BUI-only export must not retain a removed PSD marker.
      return resource;
    }

    function resolveReusableResource(exportState, node, semantics, sourcePath, resourcePrefix, resourceModule) {
      const index = exportState.reusableResources;
      const resource = index ? findReusableResource(index, node) : null;
      if (!resource) {
        recordMissingReusableResource(exportState, sourcePath || node.sourcePath, "missing");
        return null;
      }

      if (
        resource.resourceType &&
        semantics.resourceType &&
        !stringsEqualIgnoreCase(resource.resourceType, semantics.resourceType)
      ) {
        recordMissingReusableResource(exportState, sourcePath || node.sourcePath, "type-mismatch");
        return null;
      }

      const namingKey = exportResourceNamingKey(sourcePath, node.id, node.name || semantics.nodeName, resourcePrefix, semantics);
      if (resource.namingKey !== namingKey) {
        throw new Error("切图命名策略已变化或旧 BUI 缺少命名记录，请重新导出 PNG / Re-export PNG: " + sourcePath);
      }
      const cloned = cloneJsonObject(resource);
      if (semantics.nineSlice) {
        cloned.border = semantics.nineSlice;
      } else if (cloned.border) {
        delete cloned.border;
      }
      applyOutputModule(cloned, semantics, resourceModule || exportState.moduleName);
      exportState.reusedResourceCount += 1;
      return cloned;
    }


    function findReusableResource(index, node) {
      for (const key of makeReusableResourceKeys(node)) {
        const resource = index[key];
        if (resource) {
          return resource;
        }
      }
      return null;
    }


    function makeReusableResourceKeys(node) {
      const keys = [];
      if (node?.id) {
        keys.push(`id:${node.id}`);
      }
      const layerId = safeString(node?.source?.layerId, "").trim();
      if (layerId) {
        keys.push(`layer:${layerId}`);
      }
      const sourcePath = safeString(node?.sourcePath, "").trim();
      if (sourcePath) {
        keys.push(`path:${sourcePath}`);
      }
      return keys;
    }


    function recordMissingReusableResource(exportState, sourcePath, reason) {
      exportState.missingReusableResourceCount += 1;
      if (exportState.missingReusableResourceSamples.length >= 8) {
        return;
      }
      exportState.missingReusableResourceSamples.push({
        sourcePath: sourcePath || "",
        reason
      });
    }


    function cloneJsonObject(value) {
      return value && typeof value === "object" ? JSON.parse(JSON.stringify(value)) : value;
    }


    function stringsEqualIgnoreCase(left, right) {
      return safeString(left, "").toLowerCase() === safeString(right, "").toLowerCase();
    }


    function validateTargetNamingConvention(targetName, targetRule, namingRules, warnings, sourcePath) {
      const targetType = targetRule.targetType || targetRule.id;
      if (targetType === "Page" && !isValidPageTargetName(targetName)) {
        warnings.push({
          code: namingRules.warningCodes?.pageTargetNameInvalid || "PSD_PAGE_TARGET_NAME_SUFFIX_INVALID",
          severity: "Blocking",
          message: "Page target name must be XxxPage.",
          sourcePath
        });
      }
    }

    function isValidPageTargetName(value) {
      return typeof value === "string" && value.length > "Page".length && value.endsWith("Page");
    }


    async function visitLayer(sourceDoc, layer, parentId, parentPath, siblingIndex, canvas, nodes, warnings, assetsFolder, exportState, namingRules, context) {
      const name = safeString(layer.name, `Layer_${siblingIndex}`);
      const sourcePath = parentPath ? `${parentPath}/${name}` : name;
      const naming = parsePsdNaming(name, namingRules);

      if (isIgnoredByNaming(naming)) {
        pushIgnoreWarning(warnings, sourcePath, naming, namingRules);
        return;
      }

      const childLayers = arrayFrom(layer.layers);
      const isGroup = childLayers.length > 0 || isLayerGroup(layer);
      const layerId = readLayerId(layer, sourcePath, warnings);
      const layerType = detectLayerType(layer, isGroup);
      const targetRule = resolveTargetRule(naming, namingRules);
      if ((targetRule?.targetType || targetRule?.id) === "SubPage") {
        await buildGeneratedSubPageMount(sourceDoc, layer, parentId, sourcePath, siblingIndex, canvas, nodes, warnings, assetsFolder, exportState, namingRules, context, targetRule, naming, layerId);
        return;
      }
      if (targetRule && context?.parentNodeType === "List" && targetRule.allowedAsListPlaceholder) {
        await buildGeneratedListItemMount(
          sourceDoc,
          layer,
          parentId,
          parentPath,
          siblingIndex,
          canvas,
          nodes,
          warnings,
          assetsFolder,
          exportState,
          namingRules,
          context,
          targetRule,
          naming,
          layerId,
          layerType
        );
        return;
      }
      if (targetRule && isComponentTargetRule(targetRule)) {
        if (canGenerateComponentTargetMount(context, namingRules)) {
          await buildGeneratedComponentMount(
            sourceDoc,
            layer,
            parentId,
            parentPath,
            siblingIndex,
            canvas,
            nodes,
            warnings,
            assetsFolder,
            exportState,
            namingRules,
            context,
            targetRule,
            naming,
            layerId,
            layerType
          );
        } else {
          pushInvalidComponentTargetWarning(warnings, namingRules, sourcePath, context);
        }
        return;
      }

      const semantics = resolveNodeSemantics(naming, layerType, isGroup, context || {}, namingRules, warnings, sourcePath);
      const nodeId = layerId
        ? `${nodeIdPrefix(semantics.nodeType, isGroup)}_${sanitizeId(layerId)}`
        : `${nodeIdPrefix(semantics.nodeType, isGroup)}_${hashText(sourcePath)}_${siblingIndex}`;

      const bounds = semantics.nodeType === "List"
        ? await readListDisplayBounds(layer, canvas, sourcePath, warnings, namingRules)
        : semantics.exportBounds === "Layer"
          ? await readLayerExportBounds(sourceDoc, layer, canvas, sourcePath, warnings)
          : await readLayerBounds(layer, canvas, sourcePath, warnings);
      const node = {
        id: nodeId,
        parentId,
        name: sanitizeNodeName(semantics.nodeName || name),
        type: semantics.nodeType,
        active: readLayerVisible(layer),
        siblingIndex,
        sourcePath,
        source: {
          layerId: layerId === null ? null : String(layerId),
          layerType,
          resourceType: semantics.resourceType || "None",
          variant: getDefaultVariant(namingRules),
          zIndex: siblingIndex,
          naming: semantics.namingSource
        },
        rect: makeRectInCoordinateSpace(bounds, context?.coordinateSpace, canvas)
      };

      if (semantics.layout) {
        node.layout = {
          direction: semantics.layout
        };
      }

      if (semantics.prefabRef) {
        node.prefabRef = semantics.prefabRef;
      }

      if (semantics.binding) {
        node.binding = semantics.binding;
      }

      const text = semantics.nodeType === "Text" ? await readTextInfo(layer, sourcePath, warnings) : null;
      if (text) {
        text.key = deriveTextKeySeed(node.name, text.text);
        node.text = text;
      }

      appendLayerUncertaintyWarnings(layer, sourcePath, warnings);
      if (semantics.shouldSlice) {
        if (exportState.skipAssets) {
          const resource = resolveReusableResource(exportState, node, semantics, sourcePath, context?.resourcePrefix, context?.resourceModule);
          if (resource) {
            node.resource = applyOutputModule(resource, semantics, context?.resourceModule || exportState.moduleName);
          }
        } else {
          const resource = await exportResource(sourceDoc, layer, assetsFolder, nodeId, sourcePath, warnings, exportState, node.name, semantics, context?.resourcePrefix, context?.resourceModule);
          if (resource) {
            if (semantics.nineSlice) {
              resource.border = semantics.nineSlice;
            }
            node.resource = applyOutputModule(resource, semantics, context?.resourceModule || exportState.moduleName);
            exportState.slicedCount += 1;
          }
        }
      }

      if (semantics.exportOnly) {
        appendStandaloneResource(node, parentId, exportState, warnings);
        return;
      }
      nodes.push(node);

      if (semantics.skipChildren) {
        if (semantics.prefabRef && childLayers.length > 0) {
          await collectPrefabRefOverrides(
            sourceDoc,
            childLayers,
            sourcePath,
            canvas,
            bounds,
            node,
            warnings,
            assetsFolder,
            exportState,
            namingRules,
            context
          );
        }
        return;
      }

      for (let i = 0; i < childLayers.length; i += 1) {
        await visitLayer(
          sourceDoc,
          childLayers[i],
          nodeId,
          sourcePath,
          i,
          canvas,
          nodes,
          warnings,
          assetsFolder,
          exportState,
          namingRules,
          {
            parentNodeType: node.type,
            targetDrafts: context?.targetDrafts,
            targetType: context?.targetType,
            targetId: context?.targetId,
            isTargetRootChild: false,
            coordinateSpace: context?.coordinateSpace,
            resourceModule: context?.resourceModule,
            resourcePrefix: context?.resourcePrefix
          }
        );
      }
    }


    async function collectPrefabRefOverrides(
      sourceDoc,
      layers,
      parentSourcePath,
      canvas,
      prefabRefBounds,
      prefabRefNode,
      warnings,
      assetsFolder,
      exportState,
      namingRules,
      context
    ) {
      for (let index = 0; index < layers.length; index += 1) {
        const layer = layers[index];
        const name = safeString(layer.name, `Layer_${index}`);
        const sourcePath = parentSourcePath ? `${parentSourcePath}/${name}` : name;
        const naming = parsePsdNaming(name, namingRules);
        if (isIgnoredByNaming(naming)) {
          pushIgnoreWarning(warnings, sourcePath, naming, namingRules);
          continue;
        }

        const childLayers = arrayFrom(layer.layers);
        const isGroup = childLayers.length > 0 || isLayerGroup(layer);
        const layerType = detectLayerType(layer, isGroup);
        const semantics = resolveNodeSemantics(
          naming,
          layerType,
          isGroup,
          {
            parentNodeType: "PrefabRef",
            targetType: context?.targetType,
            isPrefabRefOverride: true,
            coordinateSpace: context?.coordinateSpace,
            resourceModule: context?.resourceModule,
            resourcePrefix: context?.resourcePrefix
          },
          namingRules,
          warnings,
          sourcePath
        );
        const targetName = sanitizeNodeName(semantics.nodeName || name);

        if (semantics.exportOnly) {
          const layerId = readLayerId(layer, sourcePath, warnings);
          const resourceNode = {
            id: layerId ? "image_" + sanitizeId(layerId) : "image_" + hashText(sourcePath) + "_" + index,
            name: targetName,
            sourcePath,
            source: { layerId: layerId === null ? null : String(layerId), resourceType: semantics.resourceType }
          };
          resourceNode.resource = exportState.skipAssets
            ? resolveReusableResource(exportState, resourceNode, semantics, sourcePath, context?.resourcePrefix, context?.resourceModule)
            : await exportResource(sourceDoc, layer, assetsFolder, resourceNode.id, sourcePath, warnings, exportState, targetName, semantics, context?.resourcePrefix, context?.resourceModule);
          if (resourceNode.resource && !exportState.skipAssets) {
            exportState.slicedCount += 1;
            if (semantics.nineSlice) resourceNode.resource.border = semantics.nineSlice;
          }
          applyOutputModule(resourceNode.resource, semantics, context?.resourceModule || exportState.moduleName);
          appendStandaloneResource(resourceNode, prefabRefNode.id, exportState, warnings);
          continue;
        }

        if (semantics.prefabOverride) {
          const isImageOverride = !isGroup
            && stringEquals(semantics.nodeType, "Image")
            && semantics.shouldSlice
            && !stringEquals(semantics.resourceType, "DynamicTexture");
          const isTextOverride = !isGroup
            && stringEquals(semantics.nodeType, "Text");
          if (!isImageOverride && !isTextOverride) {
            warnings.push({
              code: "PSD_PREFAB_OVERRIDE_ELEMENT_TYPE_INVALID",
              severity: "Blocking",
              message: "@PrefabOverride is only valid on a static sliced @Image_ leaf or @Text_ leaf inside a PrefabRef design subtree.",
              nodeId: prefabRefNode.id,
              sourcePath
            });
          } else {
            const layerId = readLayerId(layer, sourcePath, warnings);
            const overrideId = layerId
              ? `prefab_override_${sanitizeId(layerId)}`
              : `prefab_override_${hashText(sourcePath)}_${index}`;
            const overrideBounds = isImageOverride && semantics.exportBounds === "Layer"
              ? await readLayerExportBounds(sourceDoc, layer, canvas, sourcePath, warnings)
              : await readLayerBounds(layer, canvas, sourcePath, warnings);
            const override = {
              slot: semantics.prefabOverrideSlot,
              element: targetName,
              rect: makePrefabRefOverrideRect(
                prefabRefBounds,
                overrideBounds,
                canvas
              )
            };
            let hasPayload = false;
            if (isImageOverride) {
              const resourceName = makePrefabOverrideAssetName(prefabRefNode.name, semantics.prefabOverrideSlot, targetName, semantics.outputModule);
              const resourceNode = {
                id: overrideId,
                name: resourceName,
                sourcePath,
                source: {
                  layerId: layerId === null ? null : String(layerId)
                }
              };
              let resource = null;
              if (exportState.skipAssets) {
                resource = resolveReusableResource(exportState, resourceNode, semantics, sourcePath, context?.resourcePrefix, context?.resourceModule);
              } else {
                resource = await exportResource(
                  sourceDoc,
                  layer,
                  assetsFolder,
                  overrideId,
                  sourcePath,
                  warnings,
                  exportState,
                  resourceName,
                  semantics,
                  context?.resourcePrefix,
                  context?.resourceModule
                );
                if (resource) {
                  if (semantics.nineSlice) {
                    resource.border = semantics.nineSlice;
                  }
                  exportState.slicedCount += 1;
                }
              }
              if (resource) {
                override.sourceNodeId = overrideId;
                override.sourcePath = sourcePath;
                override.source = resourceNode.source;
                override.resource = applyOutputModule(resource, semantics, context?.resourceModule || exportState.moduleName);
                hasPayload = true;
              }
            } else {
              const text = await readTextInfo(layer, sourcePath, warnings);
              if (text) {
                override.text = text;
                hasPayload = true;
              }
            }

            if (hasPayload) {
              if (!Array.isArray(prefabRefNode.prefabRef.overrides)) {
                prefabRefNode.prefabRef.overrides = [];
              }
              if (prefabRefNode.prefabRef.overrides.some((item) => (
                item.slot === semantics.prefabOverrideSlot
                && item.element === targetName
              ))) {
                warnings.push({
                  code: "PSD_PREFAB_OVERRIDE_ELEMENT_DUPLICATE",
                  severity: "Blocking",
                  message: `PrefabRef replacement ${semantics.prefabOverrideSlot}.${targetName} is overridden more than once.`,
                  nodeId: prefabRefNode.id,
                  sourcePath
                });
              } else {
                prefabRefNode.prefabRef.overrides.push(override);
              }
            }
          }
        }

        if (semantics.skipChildren) {
          continue;
        }

        if (childLayers.length > 0) {
          await collectPrefabRefOverrides(
            sourceDoc,
            childLayers,
            sourcePath,
            canvas,
            prefabRefBounds,
            prefabRefNode,
            warnings,
            assetsFolder,
            exportState,
            namingRules,
            context
          );
        }
      }
    }

    function makePrefabOverrideAssetName(instanceName, slotName, elementName, outputModule) {
      // Slot/element remain unchanged in BUI; only the PNG name gets instance scope.
      if (outputModule) return elementName;
      const parts = [instanceName];
      if (slotName && slotName !== elementName) parts.push(slotName);
      parts.push(elementName);
      return parts.filter(Boolean).join("_");
    }

    function appendStandaloneResource(node, ownerNodeId, exportState, warnings) {
      if (node.resource) {
        exportState.resources.push({
          id: node.id,
          ownerNodeId,
          name: node.name,
          sourcePath: node.sourcePath,
          source: node.source,
          resource: node.resource
        });
      } else if (!exportState.skipAssets) {
        warnings.push({
          code: "PSD_EXPORT_ONLY_RESOURCE_MISSING",
          severity: "Blocking",
          message: "The @ExportOnly layer did not produce a PNG. Make the source layer visible and resolve its export error before retrying.",
          sourcePath: node.sourcePath
        });
      }
    }

    function makePrefabRefCoordinateSpace(bounds, fallbackCanvas) {
      const snappedBounds = snapBounds(
        bounds.left,
        bounds.top,
        bounds.right,
        bounds.bottom,
        fallbackCanvas
      );
      return {
        left: snappedBounds.left,
        top: snappedBounds.top,
        canvas: {
          width: snappedBounds.width,
          height: snappedBounds.height
        }
      };
    }

    function makePrefabRefOverrideRect(prefabRefBounds, overrideBounds, fallbackCanvas) {
      return makeRectInCoordinateSpace(
        overrideBounds,
        makePrefabRefCoordinateSpace(prefabRefBounds, fallbackCanvas),
        fallbackCanvas
      );
    }


    function stringEquals(left, right) {
      return safeString(left, "") === right;
    }


    async function buildGeneratedComponentMount(sourceDoc, layer, parentId, parentPath, siblingIndex, canvas, nodes, warnings, assetsFolder, exportState, namingRules, context, targetRule, naming, layerId, layerType) {
      const name = safeString(layer.name, `Component_${siblingIndex}`);
      const sourcePath = parentPath ? `${parentPath}/${name}` : name;
      const componentNaming = resolvePrefabRefNaming(naming);
      const targetNaming = withTargetName(naming, componentNaming.prefabName);
      const targetDrafts = Array.isArray(context?.targetDrafts) ? context.targetDrafts : null;
      if (!targetDrafts) {
        warnings.push({
          code: "PSD_COMPONENT_TARGET_CONTEXT_MISSING",
          severity: "Blocking",
          message: "Component child Prefab requires target collection context, but the exporter did not provide it.",
          sourcePath
        });
        return;
      }

      const rootSiblingIndex = countRootNodes(nodes);
      const built = await buildTargetFromRootLayer(
        sourceDoc,
        layer,
        rootSiblingIndex,
        canvas,
        nodes,
        warnings,
        assetsFolder,
        exportState,
        namingRules,
        targetRule,
        targetNaming,
        targetDrafts,
        sourcePath,
        context?.resourcePrefix
      );
      targetDrafts.push(built.target);

      const bounds = built.targetBounds || await readLayerBounds(layer, canvas, sourcePath, warnings);
      const mountId = layerId
        ? makeUniqueNodeId(`prefab_ref_${sanitizeId(layerId)}`, nodes)
        : makeUniqueNodeId(`prefab_ref_${hashText(sourcePath)}_${siblingIndex}`, nodes);
      const mountKind = namingRules.behaviors?.component?.mountPrefabRefKind || "GeneratedTarget";
      const mountNode = {
        id: mountId,
        parentId,
        name: sanitizeNodeName(componentNaming.variableName || built.target.name),
        type: "PrefabRef",
        active: readLayerVisible(layer),
        siblingIndex,
        sourcePath,
        source: {
          layerId: layerId === null ? null : String(layerId),
          layerType,
          resourceType: "None",
          variant: getDefaultVariant(namingRules),
          zIndex: siblingIndex,
          naming: buildNamingSource(naming, targetRule),
          mountAs: {
            nodeType: "PrefabRef",
            prefabRefKind: mountKind,
            targetId: built.target.targetId,
            name: built.target.name
          }
        },
        prefabRef: {
          kind: mountKind,
          targetId: built.target.targetId,
          name: built.target.name
        },
        rect: makeRectInCoordinateSpace(bounds, context?.coordinateSpace, canvas)
      };
      nodes.push(mountNode);

      warnings.push({
        code: namingRules.warningCodes?.componentTargetGenerated || "PSD_COMPONENT_TARGET_GENERATED",
        severity: "Info",
        message: `Component child Prefab under ${context?.targetType || "Target"} was generated as an independent target plus a PrefabRef mount.`,
        nodeId: mountId,
        sourcePath
      });
    }


    async function buildGeneratedListItemMount(sourceDoc, layer, parentId, parentPath, siblingIndex, canvas, nodes, warnings, assetsFolder, exportState, namingRules, context, targetRule, naming, layerId, layerType) {
      const name = safeString(layer.name, `ListItem_${siblingIndex}`);
      const sourcePath = parentPath ? `${parentPath}/${name}` : name;
      const targetDrafts = Array.isArray(context?.targetDrafts) ? context.targetDrafts : null;
      if (!targetDrafts) {
        warnings.push({
          code: "PSD_LISTITEM_TARGET_CONTEXT_MISSING",
          severity: "Blocking",
          message: "Configured list-item placeholder requires target collection context, but the exporter did not provide it.",
          sourcePath
        });
        return;
      }

      const rootSiblingIndex = countRootNodes(nodes);
      const built = await buildTargetFromRootLayer(
        sourceDoc,
        layer,
        rootSiblingIndex,
        canvas,
        nodes,
        warnings,
        assetsFolder,
        exportState,
        namingRules,
        targetRule,
        naming,
        targetDrafts,
        sourcePath,
        context?.resourcePrefix
      );
      targetDrafts.push(built.target);

      const bounds = built.targetBounds || await readLayerBounds(layer, canvas, sourcePath, warnings);
      const mountId = layerId
        ? makeUniqueNodeId(`prefab_ref_${sanitizeId(layerId)}`, nodes)
        : makeUniqueNodeId(`prefab_ref_${hashText(sourcePath)}_${siblingIndex}`, nodes);
      const mountKind = namingRules.behaviors?.list?.mountPrefabRefKind || "GeneratedTarget";
      const mountNode = {
        id: mountId,
        parentId,
        name: built.target.name,
        type: "PrefabRef",
        active: readLayerVisible(layer),
        siblingIndex,
        sourcePath,
        source: {
          layerId: layerId === null ? null : String(layerId),
          layerType,
          resourceType: "None",
          variant: getDefaultVariant(namingRules),
          zIndex: siblingIndex,
          naming: buildNamingSource(naming, targetRule),
          mountAs: {
            nodeType: "PrefabRef",
            prefabRefKind: mountKind,
            targetId: built.target.targetId,
            name: built.target.name
          }
        },
        prefabRef: {
          kind: mountKind,
          targetId: built.target.targetId,
          name: built.target.name
        },
        rect: makeRectInCoordinateSpace(bounds, context?.coordinateSpace, canvas)
      };
      nodes.push(mountNode);

    }

    async function buildGeneratedSubPageMount(sourceDoc, layer, parentId, sourcePath, siblingIndex, canvas, nodes, warnings, assetsFolder, exportState, namingRules, context, targetRule, naming, layerId) {
      const item = exportState.adaptation?.targets?.[String(layerId)];
      if (!item) throw new Error("BUI_V4_PRODUCTION_NOT_READY: SubPage requires explicit adaptation; production export awaits downstream template/layout support.");
      const host = nodes.find(node => node.id === parentId);
      if (context?.targetType !== "Page" || String(host?.source?.layerId) !== String(item.hostNodeId)) throw new Error("BUI_SUBPAGE_HOST_INVALID: SubPage must be directly inside its confirmed Page host.");
      if (!Array.isArray(item.hostRect) || JSON.stringify(item.designRect) !== JSON.stringify(item.hostRect)) throw new Error("BUI_SUBPAGE_RECT_INVALID: designRect must equal the allocated hostRect.");
      const built = await buildTargetFromRootLayer(sourceDoc, layer, countRootNodes(nodes), canvas, nodes, warnings, assetsFolder, exportState, namingRules, targetRule, naming, context.targetDrafts, sourcePath, context.resourcePrefix);
      context.targetDrafts.push(built.target);
      nodes.push({
        id: makeUniqueNodeId(`prefab_ref_${sanitizeId(layerId)}`, nodes), parentId, name: built.target.name,
        type: "PrefabRef", active: readLayerVisible(layer), siblingIndex, sourcePath,
        source: {layerId: String(layerId), layerType: "group", resourceType: "None", variant: getDefaultVariant(namingRules), zIndex: siblingIndex, naming: buildNamingSource(naming, targetRule)},
        prefabRef: {kind: "GeneratedTarget", targetId: built.target.targetId, name: built.target.name, targetType: "SubPage", hostNodeId: parentId},
        rect: makeRectInCoordinateSpace(built.targetBounds, context.coordinateSpace, canvas)
      });
    }

    function isComponentTargetRule(targetRule) {
      return Boolean(targetRule && (targetRule.targetType || targetRule.id) === "Component");
    }

    function withTargetName(naming, targetName) {
      return {
        ...naming,
        main: {
          ...naming.main,
          value: targetName
        }
      };
    }

    function canGenerateComponentTargetMount(context, namingRules) {
      return getComponentTargetParentTypes(namingRules).includes(context?.targetType);
    }

    function getComponentTargetParentTypes(namingRules) {
      const configured = arrayFrom(namingRules.behaviors?.component?.allowedParentTargetTypes)
        .filter((targetType) => typeof targetType === "string" && targetType.length > 0);
      return configured.length > 0 ? configured : ["Page"];
    }

    function pushInvalidComponentTargetWarning(warnings, namingRules, sourcePath, context) {
      const allowedTargetTypes = getComponentTargetParentTypes(namingRules);
      warnings.push({
        code: namingRules.warningCodes?.invalidFlagContext || "PSD_FLAG_CONTEXT_INVALID",
        severity: "Blocking",
        message: `@Component_ is only valid at PSD root or inside configured target subtrees (${allowedTargetTypes.join(", ")}). Current target: ${context?.targetType || "none"}.`,
        sourcePath
      });
    }

    function createCoordinateSpace(bounds, fallbackCanvas, preserveLayout = false) {
      const snappedBounds = preserveLayout ? {...bounds, width: bounds.right - bounds.left, height: bounds.bottom - bounds.top} : snapBounds(bounds.left, bounds.top, bounds.right, bounds.bottom);
      const width = Math.max(1, snappedBounds.width);
      const height = Math.max(1, snappedBounds.height);
      return {
        preserveLayout,
        left: snappedBounds.left,
        top: snappedBounds.top,
        canvas: {
          width,
          height
        }
      };
    }

    async function readTargetContentBounds(layer, canvas, namingRules, sourcePath) {
      const childLayers = arrayFrom(layer.layers);
      let bounds = null;
      for (let i = 0; i < childLayers.length; i += 1) {
        const child = childLayers[i];
        const childName = safeString(child.name, `Layer_${i}`);
        const childPath = sourcePath ? `${sourcePath}/${childName}` : childName;
        bounds = mergeBounds(bounds, await readExportableSubtreeBounds(child, canvas, namingRules, childPath));
      }

      return bounds || await readLayerBoundsQuiet(layer) || await readLayerBounds(layer, canvas, sourcePath, []);
    }

    async function readListDisplayBounds(layer, canvas, sourcePath, warnings, namingRules) {
      // Ignored subtrees cannot contribute export geometry.
      const childLayers = arrayFrom(layer.layers);
      let bounds = null;
      for (let i = 0; i < childLayers.length; i += 1) {
        bounds = mergeBounds(bounds, await readExportableSubtreeBounds(childLayers[i], canvas, namingRules, sourcePath));
      }

      if (!bounds) throw new Error("LIST_DISPLAY_BOUNDS_MISSING: List needs an effective design extent; ignored examples cannot define it.");
      return bounds;
    }

    async function readNonEmptySubtreeBounds(layer) {
      const childLayers = arrayFrom(layer.layers);
      let bounds = null;
      for (let i = 0; i < childLayers.length; i += 1) {
        bounds = mergeBounds(bounds, await readNonEmptySubtreeBounds(childLayers[i]));
      }

      if (bounds) {
        return bounds;
      }

      const layerBounds = await readLayerBoundsQuiet(layer);
      return hasPositiveArea(layerBounds) ? layerBounds : null;
    }

    function hasPositiveArea(bounds) {
      return Boolean(
        bounds
        && Number.isFinite(bounds.left)
        && Number.isFinite(bounds.top)
        && Number.isFinite(bounds.right)
        && Number.isFinite(bounds.bottom)
        && bounds.right > bounds.left
        && bounds.bottom > bounds.top
      );
    }

    async function readExportableSubtreeBounds(layer, canvas, namingRules, sourcePath) {
      const name = safeString(layer.name, sourcePath || "Layer");
      const naming = parsePsdNaming(name, namingRules);
      if (isIgnoredByNaming(naming) || naming.options.some((option) => option.rule?.kind === "exportOnly")) {
        return null;
      }

      const childLayers = arrayFrom(layer.layers);
      let bounds = null;
      for (let i = 0; i < childLayers.length; i += 1) {
        const child = childLayers[i];
        const childName = safeString(child.name, `Layer_${i}`);
        const childPath = sourcePath ? `${sourcePath}/${childName}` : childName;
        bounds = mergeBounds(bounds, await readExportableSubtreeBounds(child, canvas, namingRules, childPath));
      }

      const result = childLayers.length ? bounds : await readLayerBoundsQuiet(layer);
      if (result && naming.options.some(option => option.rule?.kind === "exportBounds" && option.value === "Layer")) {
        // Overflow belongs to this image, not to the target's design frame.
        const clipped = { left: Math.max(0, result.left), top: Math.max(0, result.top),
          right: Math.min(canvas.width, result.right), bottom: Math.min(canvas.height, result.bottom) };
        if (!hasPositiveArea(clipped)) throw new Error("PSD_EXPORT_BOUNDS_DESIGN_EXTENT_MISSING: use @ExportOnly for an image entirely outside its design canvas.");
        return clipped;
      }
      return result;
    }

    async function readLayerBoundsQuiet(layer) {
      if (typeof parseBoundsQuiet !== "function") {
        return null;
      }

      try {
        const bounds = await maybePromise(layer.boundsNoEffects || layer.bounds);
        return parseBoundsQuiet(bounds);
      } catch (err) {
        return null;
      }
    }

    function mergeBounds(a, b) {
      if (!b) {
        return a;
      }

      if (!a) {
        return {
          left: b.left,
          top: b.top,
          right: b.right,
          bottom: b.bottom
        };
      }

      return {
        left: Math.min(a.left, b.left),
        top: Math.min(a.top, b.top),
        right: Math.max(a.right, b.right),
        bottom: Math.max(a.bottom, b.bottom)
      };
    }

    function makeRectInCoordinateSpace(bounds, coordinateSpace, fallbackCanvas) {
      if (coordinateSpace?.preserveLayout) {
        const width = bounds.right - bounds.left, height = bounds.bottom - bounds.top;
        return {anchorMin: [0.5, 0.5], anchorMax: [0.5, 0.5], pivot: [0.5, 0.5],
          anchoredPosition: [bounds.left - coordinateSpace.left + (width - coordinateSpace.canvas.width) / 2,
            coordinateSpace.canvas.height / 2 - (bounds.top - coordinateSpace.top + height / 2)],
          sizeDelta: [width, height]};
      }
      if (!coordinateSpace) {
        return makeRectFromSnappedBounds(snapBounds(bounds.left, bounds.top, bounds.right, bounds.bottom), fallbackCanvas);
      }

      const snappedBounds = snapBounds(bounds.left, bounds.top, bounds.right, bounds.bottom);
      const left = snappedBounds.left - coordinateSpace.left;
      const top = snappedBounds.top - coordinateSpace.top;
      return makeRectFromSnappedBounds({
        left,
        top,
        right: left + snappedBounds.width,
        bottom: top + snappedBounds.height,
        width: snappedBounds.width,
        height: snappedBounds.height
      }, coordinateSpace.canvas);
    }

    function deriveTextKeySeed(nodeName, textValue) {
      const fromName = normalizeTextKeySeed(String(nodeName || "").replace(/Text$/i, ""));
      if (fromName) {
        return fromName;
      }

      return normalizeTextKeySeed(textValue);
    }

    function normalizeTextKeySeed(value) {
      return String(value || "")
        .trim()
        .replace(/([a-z0-9])([A-Z])/g, "$1_$2")
        .toLowerCase()
        .replace(/[\s\-\\/]+/g, "_")
        .replace(/[^a-z0-9_]/g, "")
        .replace(/_+/g, "_")
        .replace(/^_+|_+$/g, "");
    }

    return {
      buildCanvasInfo,
      buildNodes,
      buildTargetFromRootLayer,
      visitLayer,
      buildGeneratedComponentMount,
      buildGeneratedListItemMount,
      makePrefabRefOverrideRect,
      makePrefabOverrideAssetName
    };
  }

  global.BurnerPsdToBuiExportModelBuilder = { createExportModelBuilder };
})(globalThis);
