/* global globalThis */
// Copyright (c) 2026 Burner Games. All rights reserved.
// This file is part of Burner Unity Packages. Package: com.burner.ui.prefabmaker
(function registerExportRunner(global) {
  function create({ createServices }) {
    let running = false;
    async function withExportLock(operation) {
      if (running) throw new Error("EXPORT_ALREADY_RUNNING: An export is already running.");
      running = true;
      try {
        return await operation();
      } finally {
        running = false;
      }
    }
    function run(request) {
      // Execute synchronously up to its first await so the explicit configuration is frozen.
      return withExportLock(() => execute(request));
    }
    function runPrepared(prepareRequest) {
      // Entry authorization and source/options capture belong to the same exclusive operation.
      return withExportLock(async () => {
        if (typeof prepareRequest !== "function")
          throw new Error("EXPORT_PREPARATION_REQUIRED: Supply an export request factory.");
        return execute(await prepareRequest());
      });
    }
    async function execute(request) {
      if (!request?.document || !request.sourcePsdFile || !request.outputFolder ||
          !request.exportBaseName || typeof request.skipAssetsExport !== "boolean" || typeof request.sliceFileNamePrefix !== "string" ||
          !request.exportSettings || !request.namingRules || typeof request.confirmOverwrite !== "function") {
        throw new Error("EXPORT_INPUT_REQUIRED: Supply the document, authorized entries, export parameters and overwrite decision.");
      }
      // Copy configuration before the first await; Entries and the document retain host identity.
      const input = { ...request,
        exportSettings: JSON.parse(JSON.stringify(request.exportSettings)),
        namingRules: JSON.parse(JSON.stringify(request.namingRules)),
        expectedTargetIds: Array.isArray(request.expectedTargetIds) ? [...request.expectedTargetIds] : request.expectedTargetIds };
      if (!/^[A-Za-z0-9_]{0,32}$/.test(input.sliceFileNamePrefix) ||
          typeof input.exportBaseName !== "string" || /[\\/:*?"<>|]/.test(input.exportBaseName) ||
          input.exportBaseName === "." || input.exportBaseName === ".." || !input.exportBaseName.trim() ||
          !input.exportSettings.output?.assetsSuffix || !input.exportSettings.output?.buiExtension) {
        throw new Error("EXPORT_PARAMETERS_INVALID: Invalid export name, prefix or output settings.");
      }
      const { document: doc, sourcePsdFile, outputFolder, exportBaseName, sliceFileNamePrefix,
        namingRules: activeNamingRules, log = () => {}, onPreview = () => {} } = input;
      const skipAssetsExport = input.skipAssetsExport;
      let assetsRootName = `${exportBaseName}${input.exportSettings.output.assetsSuffix}`;
      let buiFileName = `${exportBaseName}${input.exportSettings.output.buiExtension}`;
      const services = createServices(input);
      const { formats, xmp, sha256Hex, t, formatText, toNumber, getCurrentPsdPath,
        createBaselineWarnings, readPreviewDocument, assertDocumentSaved, buildBuiPreviewModel, assertPreviewHasNoBlockingWarnings,
        buildSourceInfo, assertSourceMetadataComplete, buildCanvasInfo, buildNodes, exportTextReferences,
        assertExportableBui } = services;
      const { prepareExportOutputs, loadReusableBuiResourceState, assertFullBuiPreflightBeforeSlicing,
        appendBuiOnlyResourceReuseWarnings, makeExportTargetKey } = createOutputTools(services, input);
      const warnings = createBaselineWarnings();

      try {
        return await executeWithWarnings();
      } catch (error) {
        // A native slice can fail before Photoshop returns its temporary Document.
        // Preserve that first failure when a later source guard rejects the export.
        // Diagnostics never replace the original code, identity details or outcome.
        try {
          const failures = warnings.filter(item => item.code === "LAYER_PNG_EXPORT_FAILED" || item.code === "PNG_NAME_CONFLICT");
          if (failures.length && error && typeof error === "object") {
            const entries = failures.slice(0, 16).map(item => Object.fromEntries(
              ["code", "severity", "message", "nodeId", "sourcePath"].filter(key => typeof item[key] === "string")
                .map(key => [key, item[key].slice(0, key === "message" ? 2048 : 1024)])));
            error.details = { ...error.details, exportDiagnostics: {
              warningCount: failures.length, truncated: failures.length > entries.length, warnings: entries
            } };
          }
        } catch (diagnosticError) {} // Keep the native failure even if its Error is immutable.
        throw error;
      }

      async function executeWithWarnings() {

      await assertDocumentSaved(doc, "before-read", log);
      const documentSnapshot = await readPreviewDocument(doc);
      let preflightPreview = buildBuiPreviewModel(documentSnapshot);
      log(t("readingDocument"));
      const currentPsdPath = getCurrentPsdPath(doc);
      const source = await buildSourceInfo(doc, sourcePsdFile, warnings, activeNamingRules);
      source.sliceFileNamePrefix = sliceFileNamePrefix;
      assertSourceMetadataComplete(source);
      if (input.expectedSourcePsdSha256 != null && source.psdSha256 !== input.expectedSourcePsdSha256) {
        throw new Error("EXPORT_SOURCE_SHA_MISMATCH: Saved PSD changed since the export input was captured.");
      }
      input.expectedSourcePsdSha256 = source.psdSha256;
      if (sourcePsdFile.nativePath.replace(/\\/g, "/").toLowerCase() !== currentPsdPath.replace(/\\/g, "/").toLowerCase()) {
        throw new Error("RESOURCE_REUSE_INVALID: Selected PSD is not the current document.");
      }
      const reuseExport = global.BurnerPsdToBuiResourceReuse.create({ xmp, formats, sha256Hex, writeOutput: operation => input.writeOutput(operation) });
      const reuseInput = await reuseExport.load(sourcePsdFile, outputFolder, source,
        await assertDocumentSaved(doc, "after-read", log), input.reusePolicy);
      warnings.push(...(reuseInput.warnings || []));
      if (reuseInput.adaptation) source.adaptationSource = reuseInput.adaptationSource;
      else {
        onPreview(preflightPreview);
        assertPreviewHasNoBlockingWarnings(preflightPreview);
      }
      const canvas = buildCanvasInfo(documentSnapshot, warnings);
      const preflightBui = await assertFullBuiPreflightBeforeSlicing(
        documentSnapshot,
        canvas,
        exportBaseName,
        assetsRootName,
        source,
        activeNamingRules,
        reuseInput
      );
      if (reuseInput.adaptation) {
        preflightPreview = {...preflightBui, exportPlan:preflightPreview.exportPlan};
        onPreview(preflightPreview);
      }

      const beforeFirstWrite = input.beforeFirstWrite;
      input.beforeFirstWrite = async () => {
        await reuseExport.verifyUnchanged(reuseInput, sourcePsdFile, source, await assertDocumentSaved(doc, "before-first-write", log));
        if (typeof beforeFirstWrite === "function") await beforeFirstWrite();
      };
      log(formatText(t("overwriteCheck"), {
        bui: buiFileName,
        assets: skipAssetsExport ? t("assetsExportSkippedShort") : `${assetsRootName}/`
      }));
      const preparedOutput = await prepareExportOutputs(outputFolder, buiFileName, assetsRootName, {
        skipAssets: skipAssetsExport, textReferenceRootName: `${exportBaseName}_textrefs`
      });
      if (!preparedOutput) {
        return { status: "cancelled" };
      }

      buiFileName = preparedOutput.existingBui?.name || buiFileName;
      assetsRootName = preparedOutput.existingAssets?.name || assetsRootName;
      const existingBuiSha256 = preparedOutput.snapshot.entries.find(item => item.path === buiFileName && item.kind === "file")?.sha256 || null;
      const assetsFolder = preparedOutput.assetsFolder;
      let previousExport = null;
      try { previousExport = await reuseExport.archiveExisting(outputFolder, preparedOutput.existingBui); }
      catch (error) {
        if (error.firstWriteRejected) throw error;
        warnings.push({ code: "PREVIOUS_EXPORT_UNAVAILABLE", severity: "Warning", message: String(error) });
        log("Previous export history unavailable; old PNG files will be retained: " + error);
      }
      if (previousExport) reuseInput.previousExport = { path: previousExport.path, receiptSha256: previousExport.receiptSha256, buiSha256: previousExport.sha256 };
      const reusableResourceState = skipAssetsExport
        ? await loadReusableBuiResourceState(preparedOutput.existingBui, preparedOutput.existingAssets, assetsRootName)
        : null;
      if (skipAssetsExport) {
        warnings.push({
          code: "BUI_ASSETS_EXPORT_SKIPPED",
          severity: "Info",
          message: "BUI-only export mode skipped PNG slicing and did not modify the assets folder. Existing resource references are reused only when the previous BUI and PNG assets are complete."
        });
      }
      log(skipAssetsExport ? t("creatingBuiOnly") : t("creatingAssets"));
      const {
        adaptation,
        nodes,
        targets,
        resources,
        slicedCount,
        reusedResourceCount = 0,
        missingReusableResourceCount = 0,
        missingReusableResourceSamples = []
      } = await buildNodes(
        documentSnapshot,
        canvas,
        exportBaseName,
        warnings,
        assetsFolder,
        activeNamingRules,
        {
          skipAssets: skipAssetsExport,
          reusableResources: reusableResourceState?.resources || null,
          sharedGroups: reuseInput.declaration?.sharedGroups || [],
          adaptation: reuseInput.adaptation, adaptationSource: reuseInput.adaptationSource, psdSha256: source.psdSha256
        }
      );
      appendBuiOnlyResourceReuseWarnings(warnings, {
        skipAssetsExport,
        reusableResourceState,
        reusedResourceCount,
        missingReusableResourceCount,
        missingReusableResourceSamples
      });

      const plannedSliceCount = toNumber(preflightPreview.exportPlan.sliceCount, 0);
      if (skipAssetsExport) {
        log(t("sliceSkippedForBuiOnly"));
      } else {
        log(formatText(t("sliceDoneDetailed"), {
          success: slicedCount,
          skipped: Math.max(0, plannedSliceCount - slicedCount),
          planned: plannedSliceCount
        }));
      }

      const bui = {
        schemaVersion: adaptation ? 4 : 2,
        ...(adaptation ? {adaptation} : {}),
        source,
        assetsRoot: assetsRootName,
        canvas: {
          referenceResolution: [canvas.width, canvas.height],
          coordinateSystem: "UnityRectTransform",
          origin: "Center",
          unit: "px"
        },
        targets,
        nodes,
        resources,
        warnings
      };
      const retainedTexts = await global.BurnerPsdToBuiResourceReuse.collectRetainedTexts(
        reuseInput.replaceMap, documentSnapshot, bui, services.readTextInfo, warnings, activeNamingRules);
      if (retainedTexts.length) bui.retainedTexts = retainedTexts;
      const designTexts = await global.BurnerPsdToBuiDesignTexts.collect(documentSnapshot, bui, services.readTextInfo, warnings, activeNamingRules);
      bui.designTexts = designTexts;
      await exportTextReferences(documentSnapshot, nodes.concat(retainedTexts, designTexts), outputFolder, `${exportBaseName}_textrefs`, source, warnings);
      assertExportableBui(bui);

      bui.schemaVersion = adaptation ? 4 : 3;
      bui.resourceReuse = reuseExport.finalize(reuseInput, bui, `export-${Date.now()}-${Math.random().toString(16).slice(2)}`);
      await reuseExport.verifyUnchanged(reuseInput, sourcePsdFile, source,
        await assertDocumentSaved(doc, "after-render", log));
      await reuseExport.verifyProducts(outputFolder, bui);
      let buiJson, recordStatus = "saved", recordWarning = null;
      try { buiJson = await reuseExport.persistReceipt(reuseInput, outputFolder, bui, buiFileName); }
      catch (error) {
        if (error.firstWriteRejected) throw error;
        // Relationship validation must still fail before writing the product.
        recordStatus = "failed"; recordWarning = String(error);
        delete bui.source.workflowExport;
        warnings.push({ code: "EXPORT_RECORD_FAILED", severity: "Warning", message: String(error) });
        log("Export record unavailable; saving the current BUI/PNG product: " + error);
        buiJson = JSON.stringify(bui, null, 2);
      }

      await reuseExport.verifyUnchanged(reuseInput, sourcePsdFile, source,
        await assertDocumentSaved(doc, "before-write", log));
      const currentOutput = await global.BurnerPsdToBuiOutputSnapshot.resolve(outputFolder, buiFileName);
      if ((currentOutput ? await sha256Hex(await currentOutput.read({ format: formats.binary })) : null) !== existingBuiSha256)
        throw new Error("EXPORT_OUTPUT_CHANGED: BUI changed after overwrite confirmation; preview again.");
      const buiFile = currentOutput || await input.writeOutput(() => outputFolder.createFile(buiFileName, { overwrite: false }));
      await input.writeOutput(() => buiFile.write(buiJson, { format: formats.utf8 }));
      if (await sha256Hex(await buiFile.read({ format: formats.binary })) !== await sha256Hex(global.BurnerPsdToBuiResourceReuse.encodeUtf8(buiJson))) {
        throw new Error("BUI_READBACK_MISMATCH: Export snapshot was kept; old PNG cleanup was not started.");
      }
      const productFiles = await reuseExport.verifyProducts(outputFolder, bui);
      if (!skipAssetsExport) {
        try {
          const cleanup = await reuseExport.cleanupPrevious(outputFolder, previousExport, bui, buiFileName);
          log(`PNG cleanup: ${JSON.stringify(cleanup)}`);
        } catch (error) { log("Product saved; old PNG cleanup unavailable: " + error); }
      }

      return { status: "completed", productSaved: true, recordStatus, recordWarning, productFiles, buiFileName, assetsRootName, bui,
        targetKey: makeExportTargetKey(outputFolder, buiFileName, assetsRootName) };
      }
    }
    return { run, runPrepared, get running() { return running; } };
  }
  function createOutputTools(services, input) {
    const { formats, t, formatText, formatError, safeString, arrayFrom, normalizePathForCompare,
      getEntryIfExists, createBaselineWarnings, buildNodes, appendUniqueWarnings, isNonEmptyString, isSha256Hex,
      validateBuiChecklist, getWarningPriority } = services;
    const { sliceFileNamePrefix, lastCompletedExportTargetKey, confirmOverwrite,
      log = () => {}, onWarnings = () => {} } = input;
    async function prepareExportOutputs(outputFolder, buiFileName, assetsRootName, options = {}) {
      const skipAssets = Boolean(options.skipAssets);
      const snapshotOptions = {folder: outputFolder, names: [buiFileName, options.textReferenceRootName, assetsRootName],
        formats, hash: services.sha256Hex, encode: global.BurnerPsdToBuiResourceReuse.encodeUtf8};
      const snapshot = await global.BurnerPsdToBuiOutputSnapshot.capture(snapshotOptions);
      const existingBui = snapshot.resolvedEntries.get(buiFileName);
      const existingAssets = snapshot.resolvedEntries.get(assetsRootName);
      const existingTextReferences = snapshot.resolvedEntries.get(options.textReferenceRootName);
      const targetKey = makeExportTargetKey(outputFolder, buiFileName, assetsRootName);
      const matchesLastCompletedExport = Boolean(targetKey && targetKey === lastCompletedExportTargetKey);
      const conflicts = [];

      if (existingBui) {
        if (existingBui.isFolder) {
          throw new Error(formatText(t("buiPathIsFolder"), { name: buiFileName }));
        }
        conflicts.push(existingBui.name);
      }

      if (existingAssets && !skipAssets) {
        conflicts.push(existingAssets.name + (existingAssets.isFolder ? "/" : ""));
      }

      if (existingTextReferences) conflicts.push(existingTextReferences.name + (existingTextReferences.isFolder ? "/" : ""));

      const overwrite = {version: 1, outputDirectory: outputFolder.nativePath, buiFileName, assetsRootName,
        textReferenceRootName: options.textReferenceRootName, skipAssets, snapshot,
        source: {documentId: input.document.id, path: input.sourcePsdFile.nativePath, sha256: input.expectedSourcePsdSha256 || null},
        options: {exportBaseName: input.exportBaseName, sliceFileNamePrefix, reusePolicy: input.reusePolicy || "current-psd",
          namingRules: input.namingRules, exportSettings: input.exportSettings}};
      overwrite.decisionSha256 = await services.sha256Hex(global.BurnerPsdToBuiResourceReuse.encodeUtf8(JSON.stringify(overwrite)));

      if (matchesLastCompletedExport && conflicts.length === 0) {
        conflicts.push(buiFileName);
        if (!skipAssets) {
          conflicts.push(`${assetsRootName}/`);
        }
      }

      if (conflicts.length > 0) {
        log(formatText(t("overwritePromptShown"), { items: conflicts.join(", ") }));
        const decision = await confirmOverwrite(conflicts, buiFileName, assetsRootName, { skipAssets, overwrite });
        if (typeof decision !== "boolean") {
          throw new Error("EXPORT_OVERWRITE_DECISION_REQUIRED: Overwrite decision must be boolean.");
        }
        if (!decision) {
          return null;
        }
      } else {
        log(skipAssets ? t("overwriteNoConflictBuiOnly") : t("overwriteNoConflict"));
      }

      let writing = false;
      input.writeOutput = async operation => {
        if (!writing) {
          try {
            await global.BurnerPsdToBuiOutputSnapshot.unchanged(snapshotOptions, snapshot);
            if (typeof input.beforeFirstWrite === "function") await input.beforeFirstWrite();
            // No asynchronous work between the deadline check and the actual Entry
            // mutation. Archive helpers also enter here at their first write, after IO.
            if (typeof input.assertCanStart === "function") input.assertCanStart();
          } catch (error) {error.firstWriteRejected = true; throw error;}
          writing = true;
          if (typeof input.onWriteStarted === "function") input.onWriteStarted();
        }
        return operation();
      };
      if (skipAssets) {
        return { assetsFolder: null, existingBui, existingAssets, snapshot };
      }

      let assetsFolder = null;
      if (existingAssets) {
        if (existingAssets.isFolder) {
          assetsFolder = existingAssets;
          log(`Assets folder ${assetsRootName}/ will be updated per PNG; obsolete owned PNGs are removed after BUI readback.`);
        } else {
          throw new Error("ASSETS_PATH_NOT_FOLDER: " + assetsRootName);
        }
      } else {
        assetsFolder = await input.writeOutput(() => outputFolder.createFolder(assetsRootName));
      }

      return { assetsFolder, existingBui, existingAssets, snapshot };
    }

    async function loadReusableBuiResourceState(existingBui, existingAssets, assetsRootName) {
      if (!existingBui || existingBui.isFolder) {
        return {
          resources: null,
          reason: "missing-bui",
          reusableCount: 0,
          oldResourceCount: 0,
          invalidResourceCount: 0,
          missingAssetCount: 0
        };
      }

      if (!existingAssets || !existingAssets.isFolder) {
        return {
          resources: null,
          reason: "missing-assets-folder",
          reusableCount: 0,
          oldResourceCount: 0,
          invalidResourceCount: 0,
          missingAssetCount: 0
        };
      }

      let previousBui = null;
      try {
        const text = await existingBui.read({ format: formats.utf8 });
        previousBui = JSON.parse(text);
      } catch (err) {
        return {
          resources: null,
          reason: "bui-read-failed",
          error: formatError(err),
          reusableCount: 0,
          oldResourceCount: 0,
          invalidResourceCount: 0,
          missingAssetCount: 0
        };
      }

      if (!previousBui || typeof previousBui !== "object" || !Array.isArray(previousBui.nodes)) {
        return {
          resources: null,
          reason: "bui-structure-invalid",
          reusableCount: 0,
          oldResourceCount: 0,
          invalidResourceCount: 0,
          missingAssetCount: 0
        };
      }

      // Old BUI files predate this field and used UI_. A changed prefix requires PNG export.
      const previousPrefix = previousBui.source?.sliceFileNamePrefix ?? "UI_";
      if (previousPrefix !== sliceFileNamePrefix) {
        throw new Error(t("slicePrefixRequiresPngExport"));
      }

      const previousAssetsRoot = safeString(previousBui.assetsRoot, "").trim();
      if (previousAssetsRoot && previousAssetsRoot !== assetsRootName) {
        return {
          resources: null,
          reason: "assets-root-mismatch",
          reusableCount: 0,
          oldResourceCount: 0,
          invalidResourceCount: 0,
          missingAssetCount: 0
        };
      }

      const resources = {};
      let oldResourceCount = 0;
      let reusableCount = 0;
      let invalidResourceCount = 0;
      let missingAssetCount = 0;
      const samples = [];

      for (const node of [...previousBui.nodes, ...arrayFrom(previousBui.resources),
        ...previousBui.nodes.flatMap(node => arrayFrom(node.prefabRef?.overrides).filter(item => item.resource).map(item => ({
          id: item.sourceNodeId, source: item.source, sourcePath: item.sourcePath, resource: item.resource
        })))]) {
        const resource = node?.resource;
        if (!resource) {
          continue;
        }

        oldResourceCount += 1;
        if (!isReusableResourcePayload(resource)) {
          invalidResourceCount += 1;
          appendReusableResourceSample(samples, node, "invalid-resource-payload");
          continue;
        }

        const assetEntry = await getDescendantEntryIfExists(existingAssets, resource.asset);
        if (!assetEntry || assetEntry.isFolder) {
          missingAssetCount += 1;
          appendReusableResourceSample(samples, node, "missing-asset-file");
          continue;
        }

        reusableCount += 1;
        for (const key of makeReusableResourceIndexKeys(node)) {
          if (!resources[key]) {
            resources[key] = resource;
          }
        }
      }

      return {
        resources: reusableCount > 0 ? resources : null,
        reason: reusableCount > 0 ? "ok" : "no-reusable-resource",
        reusableCount,
        oldResourceCount,
        invalidResourceCount,
        missingAssetCount,
        samples
      };
    }

    async function assertFullBuiPreflightBeforeSlicing(doc, canvas, exportBaseName, assetsRootName, source, activeNamingRules, reuseInput = {}) {
      const preflightWarnings = createBaselineWarnings();
      const { nodes, targets, adaptation } = await buildNodes(
        doc,
        canvas,
        exportBaseName,
        preflightWarnings,
        null,
        activeNamingRules,
        {
          skipAssets: true,
          reusableResources: null,
          adaptation: reuseInput.adaptation, adaptationReadOnly: true, psdSha256: source.psdSha256
        }
      );
      if (input.expectedTargetIds && JSON.stringify(targets.map(item => item.targetId).sort()) !== JSON.stringify([...input.expectedTargetIds].sort()))
        throw Object.assign(new Error("TARGET_MISMATCH: Complete PSD target set changed; inspect it again."), {code: "TARGET_MISMATCH"});
      const preflightBui = {
        schemaVersion: adaptation ? 4 : 2,
        ...(adaptation ? {adaptation} : {}),
        source,
        assetsRoot: assetsRootName,
        canvas: {
          referenceResolution: [canvas.width, canvas.height],
          coordinateSystem: "UnityRectTransform",
          origin: "Center",
          unit: "px"
        },
        targets,
        nodes,
        warnings: preflightWarnings
      };
      appendUniqueWarnings(preflightWarnings, validateBuiChecklist(preflightBui));
      onWarnings(preflightWarnings);
      const blocking = preflightWarnings.filter((warning) => getWarningPriority(warning) === "blocking");
      if (blocking.length === 0) {
        return preflightBui;
      }

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

    function appendBuiOnlyResourceReuseWarnings(warnings, result) {
      if (!result.skipAssetsExport) {
        return;
      }

      const reused = result.reusedResourceCount || 0;
      const missing = result.missingReusableResourceCount || 0;
      if (reused > 0 && missing === 0) {
        warnings.push({
          code: "BUI_ASSET_REUSE_APPLIED",
          severity: "Info",
          message: `BUI-only export reused ${reused} resource references from the previous complete BUI and existing PNG assets.`
        });
        return;
      }

      if (reused > 0 && missing > 0) {
        warnings.push({
          code: "BUI_ASSET_REUSE_PARTIAL",
          severity: "Blocking",
          message: `BUI-only export reused ${reused} resource references, but ${missing} planned slice nodes had no safe reusable resource. Re-enable PNG slicing before Make Prefab.`,
          details: result.missingReusableResourceSamples || []
        });
        return;
      }

      if (missing > 0) {
        const state = result.reusableResourceState || {};
        warnings.push({
          code: "BUI_ASSET_REUSE_UNAVAILABLE",
          severity: "Blocking",
          message: `BUI-only export found ${missing} planned slice nodes, but previous BUI/assets are not complete enough for reuse (${state.reason || "unknown"}). Re-enable PNG slicing before Make Prefab.`,
          details: {
            oldResourceCount: state.oldResourceCount || 0,
            reusableCount: state.reusableCount || 0,
            invalidResourceCount: state.invalidResourceCount || 0,
            missingAssetCount: state.missingAssetCount || 0,
            samples: (result.missingReusableResourceSamples || []).concat(state.samples || []).slice(0, 8)
          }
        });
      }
    }

    function isReusableResourcePayload(resource) {
      return Boolean(
        resource &&
        typeof resource === "object" &&
        isNonEmptyString(resource.asset) &&
        isSha256Hex(resource.sha256) &&
        Number.isInteger(resource.width) &&
        resource.width > 0 &&
        Number.isInteger(resource.height) &&
        resource.height > 0 &&
        isNonEmptyString(resource.resourceType)
      );
    }

    function makeReusableResourceIndexKeys(node) {
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

    function appendReusableResourceSample(samples, node, reason) {
      if (samples.length >= 8) {
        return;
      }
      samples.push({
        sourcePath: safeString(node?.sourcePath, ""),
        nodeId: safeString(node?.id, ""),
        reason
      });
    }

    async function getDescendantEntryIfExists(folder, relativePath) {
      const parts = safeString(relativePath, "")
        .replace(/\\/g, "/")
        .split("/")
        .map((part) => part.trim())
        .filter(Boolean);
      let current = folder;

      for (const part of parts) {
        current = await getEntryIfExists(current, part);
        if (!current) {
          return null;
        }
      }

      return current;
    }

    function makeExportTargetKey(outputFolder, buiFileName, assetsRootName) {
      const folderPath = normalizePathForCompare(outputFolder.nativePath || "");
      return [folderPath, safeString(buiFileName, ""), safeString(assetsRootName, ""), sliceFileNamePrefix, 4].join("::");
    }

    return { prepareExportOutputs, loadReusableBuiResourceState, assertFullBuiPreflightBeforeSlicing,
      appendBuiOnlyResourceReuseWarnings, makeExportTargetKey };
  }
  global.BurnerPsdToBuiExportRunner = { create, createOutputTools };
})(globalThis);
