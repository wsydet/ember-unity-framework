/* global globalThis */

(function registerPngExporter(global) {
  function createPngExporter(options) {
    const { app, action, core, constants, imaging, fs, formats, arrayFrom, toNumber, safeString, stripExtension, formatText, formatError, t, log, readLayerVisible, parseBoundsQuiet, snapBounds, makeAssetFileName, readExportSetting, sha256Hex } = options;
    const writeOutput = options.writeOutput || (operation => operation());
    const resolveOutputEntry = options.resolveOutputEntry || getEntryIfExists;
    const PNG_CONTENT_HASH_ALGORITHM = "png-normalized-sha256-v1";
    let modalContext = null;
    let colorDiagnosticCount = 0;
    // Read the same native documentID properties as Photoshop's DOM, independently
    // of the returned Document wrapper. Only a newly observed ID may be closed.
    function createSliceIdentityGuard(sourceDoc, layer) {
      const sourceId = sourceDoc.id;
      let exportDoc = null, exportId = null, beforeIds = null, failure = null, operationFailure = null;
      const observations = [];
      const validId = id => Number.isSafeInteger(id) && id > 0;
      function reject(stage, reason, cause) {
        if (!failure) {
          failure = Object.assign(new Error(`PNG document identity rejected at ${stage}: ${reason}`), {
            code: "DOCUMENT_MISMATCH", bridgeRejected: true,
            details: {pngDocumentIdentity: {firstFailedStage: operationFailure?.stage || stage, identityFailedStage: stage,
              reason, sourceId: validId(sourceId) ? sourceId : null, exportId, operationFailure,
              sourceLayerId: validId(layer?.id) ? layer.id : null,
              beforeIds: beforeIds?.slice(0, 32) || [], beforeCount: beforeIds?.length ?? null,
              observations: observations.slice(-12),
              cause: cause ? String(cause.message || cause).slice(0, 240) : null}}
          });
        }
        return failure;
      }
      async function check(stage, requireActive = false, closed = false) {
        if (failure) throw failure;
        const observed = {stage, domId: null, domActiveId: null, nativeActiveId: null,
          domIds: [], nativeIds: []};
        observations.push(observed);
        if (observations.length > 12) observations.shift();
        try {
          observed.domId = closed ? null : exportDoc?.id ?? null;
          observed.domActiveId = app.activeDocument?.id ?? null;
          const domIds = arrayFrom(app.documents).map(doc => doc.id);
          observed.domIds = domIds.slice(0, 32);
          observed.domCount = domIds.length;
          // These descriptors match Adobe's document-property and application-range
          // multiGet contract; never infer a native ID from a DOM wrapper's name.
          const result = await action.batchPlay([
            {_obj: "multiGet", _target: {_ref: "document", _enum: "ordinal", _value: "targetEnum"},
              extendedReference: [["documentID"]]},
            {_obj: "multiGet", _target: {_ref: "application", _enum: "ordinal", _value: "targetEnum"},
              extendedReference: [["documentID"], {_obj: "document", index: 1, count: -1}]}
          ], {});
          observed.nativeActiveId = result?.[0]?.documentID ?? null;
          const nativeIds = Array.isArray(result?.[1]?.list) ? result[1].list.map(doc => doc.documentID) : [];
          observed.nativeIds = nativeIds.slice(0, 32);
          observed.nativeCount = nativeIds.length;
          if (!validId(sourceId) || !validId(observed.nativeActiveId) ||
              result?.some(item => String(item?._obj).toLowerCase() === "error" || (typeof item?.result === "number" && item.result !== 0)) ||
              !nativeIds.length || !nativeIds.every(validId) || new Set(nativeIds).size !== nativeIds.length ||
              !domIds.every(validId) || new Set(domIds).size !== domIds.length ||
              domIds.length !== nativeIds.length || domIds.some(id => !nativeIds.includes(id)) ||
              observed.domActiveId !== observed.nativeActiveId || !nativeIds.includes(observed.nativeActiveId) || !nativeIds.includes(sourceId)) {
            throw reject(stage, "Native and DOM document identities are unavailable or inconsistent.");
          }
          if (!beforeIds) {
            if (observed.nativeActiveId !== sourceId) throw reject(stage, "The source is not the active native document.");
            beforeIds = nativeIds.slice();
          } else {
            const addedIds = nativeIds.filter(id => !beforeIds.includes(id));
            if (closed) {
              if (nativeIds.includes(exportId) || addedIds.length || nativeIds.length !== beforeIds.length) {
                throw reject(stage, "Closing the temporary document did not preserve the original document set.");
              }
            } else {
              if (exportId === null && addedIds.length === 1) exportId = addedIds[0];
              if (!validId(exportId) || exportId === sourceId || beforeIds.includes(exportId) ||
                  observed.domId !== exportId || addedIds.length !== 1 || addedIds[0] !== exportId ||
                  beforeIds.some(id => !nativeIds.includes(id))) {
                throw reject(stage, "The returned Document is not the newly created native document.");
              }
              if (requireActive && observed.nativeActiveId !== exportId) throw reject(stage, "The temporary document is not the active native document.");
            }
          }
        } catch (error) { throw reject(stage, "Document identity read failed.", error); }
      }
      async function activate(stage) {
        await check(stage);
        try { app.activeDocument = exportDoc; }
        catch (error) { throw reject(stage, "Selecting the temporary document failed.", error); }
        await check(`${stage}:selected`, true);
      }
      return {
        check, activate,
        noteOperationFailure: (stage, error) => {
          if (!failure) operationFailure = {stage, message: String(error?.message || error).slice(0, 240)};
        },
        created: async doc => { exportDoc = doc; await check("after-create", true); },
        close: async () => {
          if (failure || !exportDoc) return; // Never close an object after identity uncertainty.
          await activate("before-close");
          try {
            if (typeof exportDoc.close !== "function") throw new Error("Document.close is unavailable.");
            await exportDoc.close(constants && constants.SaveOptions ? constants.SaveOptions.DONOTSAVECHANGES : "dontSave");
          } catch (error) { throw reject("close", "Closing the verified temporary document failed.", error); }
          await check("after-close", false, true);
        }
      };
    }
    // Keep native color-state evidence for the first two slices of this exporter.
    // These reads never assign a profile or change PNG normalization semantics.
    function readColorDiagnosticState(doc) {
      const state = {};
      for (const key of ["id", "saved", "mode", "bitsPerChannel", "colorProfileType", "colorProfileName"]) {
        try {
          const value = doc?.[key];
          state[key] = typeof value === "string" ? value.slice(0, 160) :
            typeof value === "number" || typeof value === "boolean" ? value : null;
        } catch (error) { state[key] = null; }
      }
      return state;
    }
    function readSourceColorDiagnosticState(sourceDoc) {
      let nativeDoc = null;
      try {
        // The builder passes a preview snapshot; resolve the live source by ID,
        // never the active document, which becomes the temporary export document.
        nativeDoc = arrayFrom(app.documents).find(doc => doc.id === sourceDoc.id);
      } catch (error) {}
      return readColorDiagnosticState(nativeDoc);
    }
    function captureColorDiagnostic(diagnostic, stage, doc) {
      if (!diagnostic) return;
      let activeDocumentId = null;
      try { activeDocumentId = app.activeDocument?.id ?? null; } catch (error) {}
      diagnostic.stages.push({stage, activeDocumentId, document: readColorDiagnosticState(doc)});
    }
    async function readPngColorDiagnostic(bytes) {
      try {
        const data = toUint8Array(bytes);
        if (!isPngBytes(data)) return {status: "not-png", chunks: []};
        const chunks = [];
        let offset = 8, scanned = 0, truncated = false;
        while (offset + 12 <= data.length && scanned++ < 4096) {
          const length = readUint32(data, offset), end = offset + 12 + length;
          if (end > data.length) return {status: "invalid-chunk", chunks};
          const type = String.fromCharCode(...data.subarray(offset + 4, offset + 8));
          if (["iCCP", "sRGB", "gAMA", "cHRM", "cICP"].includes(type)) {
            if (chunks.length < 8) {
              let sha256 = null;
              try { sha256 = await sha256Hex(data.subarray(offset + 8, end - 4)); } catch (error) {}
              chunks.push({type, bytes: length, sha256});
            } else { truncated = true; }
          }
          offset = end;
          if (type === "IEND") return {status: "complete", chunks, truncated};
        }
        return {status: "incomplete", chunks, truncated: true};
      } catch (error) { return {status: "unavailable", chunks: []}; }
    }
    function throwIfCancelled() {
      if (modalContext?.isCancelled) throw Object.assign(new Error("EXPORT_CANCELLED: Photoshop modal operation was cancelled."), {code: "EXPORT_CANCELLED", exportCancelled: true});
    }
    // Keep the existing modal scopes, but never swallow native cancellation or
    // failure to acquire that scope as an ordinary missing-layer warning.
    async function executeExportModal(operation, options) {
      try {
        return await core.executeAsModal(async context => {
          modalContext = context;
          try {throwIfCancelled(); const result = await operation(); throwIfCancelled(); return result;}
          catch (error) {throwIfCancelled(); throw error;}
        }, options);
      } catch (error) {
        if (error.number === 9) {error.code = "HOST_BUSY"; error.exportHostRejected = true;}
        throw error;
      } finally {modalContext = null;}
    }


    async function tryExportLayerPng(sourceDoc, layer, assetsFolder, nodeId, sourcePath, warnings, exportState, preferredName, resourceType, nineSlice, resourcePrefix, outputModule, ownerModule, exportBounds = "Canvas") {
      if (!assetsFolder) {
        warnings.push({
          code: "LAYER_PNG_EXPORT_NO_ASSETS_FOLDER",
          severity: "Warning",
          message: "Layer PNG export skipped because assets folder is unavailable.",
          nodeId,
          sourcePath
        });
        return null;
      }

      if (exportBounds !== "Canvas" && exportBounds !== "Layer") throw new Error("PSD_EXPORT_BOUNDS_INVALID: expected Canvas or Layer");
      const sourceBounds = exportBounds === "Layer"
        ? await options.readLayerExportBounds(sourceDoc, layer, null, sourcePath, warnings)
        : parseBoundsQuiet(layer.boundsNoEffects || layer.bounds);
      if (!sourceBounds || sourceBounds.right <= sourceBounds.left || sourceBounds.bottom <= sourceBounds.top) {
        warnings.push({
          code: "EMPTY_LAYER_PNG_SKIPPED",
          severity: "Warning",
          message: "Layer bounds are empty or unavailable, so PNG slicing was skipped.",
          nodeId,
          sourcePath
        });
        return null;
      }

      const shared = (exportState.sharedGroups || []).find(g => g.memberLayerIds.map(String).includes(String(layer.id)));
      const resourceModule = outputModule || ownerModule || exportState.moduleName;
      if (shared && (shared.outputModule !== resourceModule || shared.expected.resourceType !== resourceType ||
          ["left", "bottom", "right", "top"].some(k => (nineSlice?.[k] || 0) !== shared.expected.border[k])))
        throw new Error("SHARED_IMAGE_INVALID: incompatible module/type/border");
      let fileName = makeAssetFileName(sourcePath, nodeId, exportState,
        shared ? "Shared_" + shared.id.slice(0, 24) : preferredName, resourcePrefix, shared ? shared.outputModule : outputModule, ownerModule);
      let outputFile = null;
      let tempOutputFile = null;
      let exportedWidth = 0;
      let exportedHeight = 0;
      const docWidth = Math.max(1, Math.round(toNumber(sourceDoc.width, 1)));
      const docHeight = Math.max(1, Math.round(toNumber(sourceDoc.height, 1)));
      let cropBounds = normalizeCropBounds(sourceBounds, docWidth, docHeight, exportBounds);
      let exportedBytesOverride = null;
      const colorDiagnostic = colorDiagnosticCount++ < 2 ? {
        schemaVersion: 1, outcome: "not-completed", sourceBefore: readSourceColorDiagnosticState(sourceDoc), stages: []
      } : null;

      try {
        if (options.assertSourceBeforeModal) await options.assertSourceBeforeModal();
        tempOutputFile = await createTemporaryPngFile(assetsFolder, fileName);
        outputFile = tempOutputFile;
        log(formatText(t("slicingLayer"), { name: safeString(layer.name, sourcePath) }));

        await executeExportModal(
          async () => {
            if (options.assertSourceBeforeModal) await options.assertSourceBeforeModal();
            let exportDoc = null;
            const identity = createSliceIdentityGuard(sourceDoc, layer);
            await identity.check("before-create");
            let operationStage = "create";
            try {
              exportDoc = await app.createDocument({
                name: stripExtension(fileName),
                width: docWidth,
                height: docHeight,
                resolution: Math.max(1, Math.round(toNumber(sourceDoc.resolution, 72))),
                mode: constants && constants.NewDocumentMode ? constants.NewDocumentMode.RGB : "RGBColorMode",
                fill: constants && constants.DocumentFill ? constants.DocumentFill.TRANSPARENT : "transparent"
              });
              await identity.created(exportDoc);
              captureColorDiagnostic(colorDiagnostic, "created", exportDoc);

              operationStage = "duplicate";
              await duplicateLayerToDocument(sourceDoc, layer, exportDoc, identity);
              captureColorDiagnostic(colorDiagnostic, "duplicated", exportDoc);
              operationStage = "background";
              await removeDefaultBackgroundLayer(exportDoc, identity);
              if (exportBounds === "Layer") {
                operationStage = "expand-canvas";
                cropBounds = await expandSliceCanvas(exportDoc, cropBounds, docWidth, docHeight, identity);
              }
              operationStage = "bake";
              await bakeExportDocumentPixels(exportDoc, identity);
              await identity.check("after-merge", true);
              captureColorDiagnostic(colorDiagnostic, "merged", exportDoc);
              operationStage = "crop";
              await cropDocumentToBounds(exportDoc, cropBounds, identity);
              await identity.check("after-crop", true);
              captureColorDiagnostic(colorDiagnostic, "cropped", exportDoc);

              exportedWidth = Math.max(1, Math.round(toNumber(exportDoc.width, cropBounds.right - cropBounds.left)));
              exportedHeight = Math.max(1, Math.round(toNumber(exportDoc.height, cropBounds.bottom - cropBounds.top)));

              let sharedPixels = null;
              if (shared) {
                if (exportedWidth !== shared.expected.width || exportedHeight !== shared.expected.height)
                  throw new Error("SHARED_IMAGE_INVALID: source dimensions changed");
                const actual = await readDocumentPixelsRgba(exportDoc, exportedWidth, exportedHeight);
                sharedPixels = new Uint8Array(actual.pixels);
                for (let i = 0; i < sharedPixels.length; i += 4) if (!sharedPixels[i + 3]) sharedPixels.fill(0, i, i + 3);
                if (await sha256Hex(sharedPixels) !== shared.expected.visibleRgbaSha256)
                  throw new Error("SHARED_IMAGE_INVALID: visible source pixels changed");
                if (Object.values(shared.expected.border).some(Boolean) && (!shouldCompactNineSlice() || getNineSliceCenterSize() !== 2))
                  throw new Error("SHARED_IMAGE_INVALID: declared nine-slice requires compact export with two center pixels");
                exportedBytesOverride = encodeRgbaPng(exportedWidth, exportedHeight, sharedPixels);
              }
              if (nineSlice && shouldCompactNineSlice()) {
                operationStage = "compact";
                await identity.check("before-compact", true);
                const compacted = await tryCompactNineSliceDocumentPixels(exportDoc, nineSlice, sourcePath, warnings, nodeId, sharedPixels);
                await identity.check("after-compact", true);
                if (compacted) {
                  exportedBytesOverride = compacted.bytes;
                  exportedWidth = compacted.width;
                  exportedHeight = compacted.height;
                }
              }

              if (!exportedBytesOverride) {
                operationStage = "save";
                const token = fs.createSessionToken(outputFile);
                await identity.check("before-save", true);
                captureColorDiagnostic(colorDiagnostic, "before-native-save", exportDoc);
                const saveResults = await action.batchPlay(
                  [
                    {
                      _obj: "save",
                      as: {
                        _obj: "PNGFormat",
                        method: {
                          _enum: "PNGMethod",
                          _value: "quick"
                        },
                        PNGInterlaceType: {
                          _enum: "PNGInterlaceType",
                          _value: "PNGInterlaceNone"
                        },
                        // Preserve this document's profile instead of a previous save's choice.
                        embedIccProfileLastState: {
                          _enum: "embedOff",
                          _value: "embedOn"
                        }
                      },
                      in: {
                        _path: token,
                        _kind: "local"
                      },
                      documentID: exportDoc.id,
                      embedProfiles: true,
                      copy: true,
                      lowerCase: true,
                      _options: {
                        dialogOptions: "dontDisplay"
                      }
                    }
                  ],
                  {}
                );
                // Photoshop can resolve batchPlay with an error instead of rejecting it.
                const saveError = (Array.isArray(saveResults) ? saveResults : [saveResults]).find(result =>
                  result && (String(result._obj).toLowerCase() === "error" ||
                    (typeof result.result === "number" && result.result !== 0)));
                if (saveError) throw new Error(`Photoshop PNG save failed (${saveError.result ?? "error"}): ${saveError.message || "native action failed"}`);
                await identity.check("after-save", true);
                captureColorDiagnostic(colorDiagnostic, "after-native-save", exportDoc);
              }
            } catch (error) {
              identity.noteOperationFailure(operationStage, error);
              throw error;
            } finally {
              await identity.close();
            }
          },
          { commandName: "Burner PSD layer slice" }
        );

        const bytes = exportedBytesOverride || await tempOutputFile.read({ format: formats.binary });
        if (!exportedBytesOverride) assertNativePngBytes(bytes);
        const contentHash = await tryNormalizedPngHash(bytes);
        if (colorDiagnostic) {
          colorDiagnostic.encoder = exportedBytesOverride ? "nine-slice-rgba" : "native-png-quick";
          colorDiagnostic.png = await readPngColorDiagnostic(bytes);
          colorDiagnostic.contentHash = contentHash;
          colorDiagnostic.outcome = "png-read";
        }
        const resourceIdentity = { width: exportedWidth, height: exportedHeight, resourceType, nineSlice, outputModule: resourceModule };
        const requestedFileName = fileName;
        fileName = await reservePngFileName(assetsFolder, fileName, contentHash, resourceIdentity, exportState);
        const deduped = !shared && findDedupedPngResource(
          contentHash,
          exportState,
          {
            fileName,
            width: exportedWidth,
            height: exportedHeight,
            resourceType,
            outputModule: resourceModule,
            nineSlice,
            nodeId,
            sourcePath
          },
          warnings
        );
        if (deduped) {
          await deleteDuplicateOutputFile(tempOutputFile, warnings, nodeId, sourcePath, fileName);
          tempOutputFile = null;
          return deduped;
        }

        const writeResult = await writePngIfContentChanged(assetsFolder, fileName, tempOutputFile, bytes, contentHash, warnings, nodeId, sourcePath);
        outputFile = writeResult.file;
        const actualFileName = outputFile.name || fileName;
        if (actualFileName.toLowerCase() !== requestedFileName.toLowerCase()) {
          warnings.push({ code: "PNG_NAME_COLLISION_RENAMED", severity: "Warning",
            message: "PNG name was occupied by a different resource; kept the existing file and exported " + requestedFileName + " as " + actualFileName + ".",
            nodeId, sourcePath });
        }
        tempOutputFile = null;
        rememberPngDedupeResource(
          contentHash,
          exportState,
          {
            fileName: actualFileName,
            sha256: writeResult.sha256,
            width: exportedWidth,
            height: exportedHeight,
            resourceType,
            outputModule: resourceModule,
            nineSlice
          }
        );

        return withContentHash({
          asset: actualFileName,
          sha256: writeResult.sha256,
          width: exportedWidth,
          height: exportedHeight,
          resourceType
        }, contentHash);
      } catch (err) {
        if (exportBounds === "Layer" || shared || err.firstWriteRejected || err.bridgeRejected || err.exportCancelled || err.exportHostRejected) throw err;
        warnings.push({
          code: String(err?.message || err).includes("PNG_NAME_CONFLICT:") ? "PNG_NAME_CONFLICT" : "LAYER_PNG_EXPORT_FAILED",
          severity: String(err?.message || err).includes("PNG_NAME_CONFLICT:") ? "Blocking" : "Warning",
          message: `UXP could not export this layer PNG: ${formatError(err)}`,
          nodeId,
          sourcePath
        });
        return null;
      } finally {
        if (tempOutputFile) {
          await deleteDuplicateOutputFile(tempOutputFile, warnings, nodeId, sourcePath, tempOutputFile.name || fileName);
        }
        if (colorDiagnostic) {
          try {
            colorDiagnostic.sourceAfter = readSourceColorDiagnosticState(sourceDoc);
            warnings.push({code: "PNG_COLOR_PROFILE_DIAGNOSTIC", severity: "Info",
              message: JSON.stringify(colorDiagnostic), nodeId, sourcePath});
          } catch (error) {} // Diagnostics must not turn an accepted save into a failure.
        }
      }
    }


    // Read the actual document composite: no layer visibility, source pixels or PSD history are edited.
    async function exportTextReferences(doc, nodes, outputFolder, rootName, source, warnings) {
      const layers = new Map();
      function index(items, visible = true) {
        for (const layer of arrayFrom(items)) {
          const shown = visible && readLayerVisible(layer);
          layers.set(String(layer.id), { layer, visible: shown });
          index(layer.layers, shown);
        }
      }
      index(doc.layers);
      const entries = [];
      function collect(value) {
        if (!value || typeof value !== "object") return;
        if (value.text && typeof value.text === "object" && value.text.source?.layerId != null) entries.push(value);
        for (const [key, child] of Object.entries(value)) if (key !== "text") {
          if (Array.isArray(child)) child.forEach(collect); else if (child && typeof child === "object") collect(child);
        }
      }
      nodes.forEach(collect);
      if (!entries.length) return;
      let folder = null;
      await executeExportModal(async () => {
        if (options.assertSourceBeforeModal) await options.assertSourceBeforeModal();
        for (const node of entries) {
          throwIfCancelled();
          const layerId = String(node.text.source.layerId);
          const identity = { version: 1, source: "psd-composite-crop", sourceLayerId: layerId, sourcePsdSha256: source.psdSha256 };
          let image = null;
          try {
            const item = layers.get(layerId);
            if (!item || !item.visible) throw Error("TEXT_REFERENCE_LAYER_NOT_VISIBLE");
            if (!imaging || typeof imaging.getPixels !== "function") throw Error("TEXT_REFERENCE_IMAGING_UNAVAILABLE");
            const bounds = parseBoundsQuiet(item.layer.bounds);
            if (!bounds || ![bounds.left,bounds.top,bounds.right,bounds.bottom].every(Number.isFinite) ||
                bounds.right <= bounds.left || bounds.bottom <= bounds.top) throw Error("TEXT_REFERENCE_BOUNDS_INVALID");
            const crop = { left: Math.max(0, Math.floor(bounds.left) - 8), top: Math.max(0, Math.floor(bounds.top) - 8),
              right: Math.min(doc.width, Math.ceil(bounds.right) + 8), bottom: Math.min(doc.height, Math.ceil(bounds.bottom) + 8) };
            const width = crop.right - crop.left, height = crop.bottom - crop.top;
            if (width < 16 || height < 16 || width > 2048 || height > 2048) throw Error("TEXT_REFERENCE_SIZE_UNSUPPORTED");
            image = await imaging.getPixels({ documentID: doc.id, sourceBounds: crop, componentSize: 8,
              colorSpace: "RGB", colorProfile: "sRGB IEC61966-2.1", applyAlpha: false });
            const data = image?.imageData, actual = image?.sourceBounds;
            if (!data || (image.level != null && image.level !== 0) || data.componentSize !== 8 ||
                (data.components !== 3 && data.components !== 4) || !actual ||
                ![actual.left,actual.top,actual.right,actual.bottom].every(Number.isFinite) ||
                actual.left < crop.left || actual.top < crop.top || actual.right > crop.right || actual.bottom > crop.bottom ||
                data.width !== actual.right - actual.left || data.height !== actual.bottom - actual.top)
              throw Error("TEXT_REFERENCE_PIXELS_INVALID");
            const raw = await data.getData({ chunky: true });
            const pixels = new Uint8Array(raw.buffer || raw, raw.byteOffset || 0, raw.byteLength);
            if (pixels.length !== data.width * data.height * data.components) throw Error("TEXT_REFERENCE_PIXELS_INVALID");
            const rgba = new Uint8Array(width * height * 4);
            copyReturnedPixelsToRgba({sourcePixels:pixels,sourceWidth:data.width,sourceHeight:data.height,components:data.components,
              targetPixels:rgba,targetWidth:width,targetHeight:height,
              bounds:{left:actual.left-crop.left,top:actual.top-crop.top,right:actual.right-crop.left,bottom:actual.bottom-crop.top}});
            const bytes = encodeRgbaPng(width, height, rgba);
            if (bytes.byteLength > 16 * 1024 * 1024) throw Error("TEXT_REFERENCE_SIZE_UNSUPPORTED");
            const digest = await sha256Hex(bytes);
            if (!folder) {
              folder = await resolveOutputEntry(outputFolder, rootName);
              if (folder && !folder.isFolder) throw Error("TEXT_REFERENCE_FOLDER_CONFLICT");
              if (!folder) folder = await writeOutput(() => outputFolder.createFolder(rootName));
            }
            const displayName = String(node.name || node.text.source.sourcePath?.split("/").pop() || "Text")
              .replace(/[<>:"/\\|?*\x00-\x1f]/g, "_").replace(/[. ]+$/g, "").slice(0, 64) || "Text";
            const safeLayerId = layerId.replace(/[^A-Za-z0-9_-]/g, "_");
            const name = displayName + "_" + safeLayerId + "_" + digest.slice(0, 12) + ".png";
            let file = (await folder.getEntries()).find(e => e.name === name);
            if (!file) { file = await writeOutput(() => folder.createFile(name, {overwrite:false})); await file.write(bytes, {format:formats.binary}); }
            if (file.isFolder || await sha256Hex(await file.read({format:formats.binary})) !== digest)
              throw Error("TEXT_REFERENCE_READBACK_MISMATCH");
            node.text.previewReference = { ...identity, status: "captured", path: folder.name + "/" + name,
              sha256: digest, width, height, crop, pixelScale: 1, colorSpace: "sRGB" };
          } catch (error) {
            if (error.firstWriteRejected) throw error;
            throwIfCancelled();
            node.text.previewReference = { ...identity, status: "unavailable", reason: String(error?.message || error) };
            warnings.push({code:"TEXT_REFERENCE_UNAVAILABLE",severity:"Warning",nodeId:node.id,
              sourcePath:node.text.source.sourcePath,message:"PSD text reference unavailable: " + String(error?.message || error)});
          } finally { if (image?.imageData?.dispose) image.imageData.dispose(); }
        }
      }, {commandName:"Burner PSD text references"});
    }


    function normalizeCropBounds(bounds, docWidth, docHeight, exportBounds = "Canvas") {
      return snapBounds(bounds.left, bounds.top, bounds.right, bounds.bottom,
        exportBounds === "Layer" ? undefined : { width: docWidth, height: docHeight });
    }

    async function expandSliceCanvas(doc, bounds, width, height, identity) {
      const left = Math.max(0, -bounds.left), top = Math.max(0, -bounds.top);
      const right = Math.max(0, bounds.right - width), bottom = Math.max(0, bounds.bottom - height);
      // Expand only the isolated document, before merging can discard off-canvas pixels.
      // Corner anchors retain masks and pixels without translating or resampling the layer.
      if (left || top) {
        await identity.activate("before-expand-left-top");
        await doc.resizeCanvas(width + left, height + top, constants.AnchorPosition.BOTTOMRIGHT);
        await identity.check("after-expand-left-top", true);
      }
      if (right || bottom) {
        await identity.activate("before-expand-right-bottom");
        await doc.resizeCanvas(width + left + right, height + top + bottom, constants.AnchorPosition.TOPLEFT);
        await identity.check("after-expand-right-bottom", true);
      }
      return { left: bounds.left + left, top: bounds.top + top,
        right: bounds.right + left, bottom: bounds.bottom + top };
    }


    async function cropDocumentToBounds(doc, bounds, identity) {
      const cropBounds = {
        left: Math.round(toNumber(bounds.left, 0)),
        top: Math.round(toNumber(bounds.top, 0)),
        right: Math.round(toNumber(bounds.right, 1)),
        bottom: Math.round(toNumber(bounds.bottom, 1))
      };

      await identity.activate("before-crop");
      if (typeof doc.crop === "function") {
        await doc.crop(cropBounds);
        return;
      }

      await action.batchPlay(
        [
          {
            _obj: "crop",
            to: {
              _obj: "rectangle",
              top: { _unit: "pixelsUnit", _value: cropBounds.top },
              left: { _unit: "pixelsUnit", _value: cropBounds.left },
              bottom: { _unit: "pixelsUnit", _value: cropBounds.bottom },
              right: { _unit: "pixelsUnit", _value: cropBounds.right }
            },
            angle: { _unit: "angleUnit", _value: 0 },
            delete: true,
            _options: { dialogOptions: "dontDisplay" }
          }
        ],
        {}
      );
    }


    async function removeDefaultBackgroundLayer(doc, identity) {
      const layers = arrayFrom(doc.layers);
      if (layers.length < 2) {
        return;
      }

      const candidate = layers.find((item) => {
        const name = safeString(item?.name, "").toLowerCase();
        return item?.isBackgroundLayer || name === "background" || name === "背景";
      });

      if (!candidate) {
        return;
      }

      await identity.activate("before-background-delete");
      try {
        if (typeof candidate.delete === "function") {
          await candidate.delete();
        } else {
          candidate.visible = false;
        }
      } catch (err) {
        await identity.check("before-background-hide", true);
        try {
          candidate.visible = false;
        } catch (ignoreErr) {}
      }
    }


    async function bakeExportDocumentPixels(doc, identity) {
      await identity.activate("before-bake");

      if (!doc || typeof doc.mergeVisibleLayers !== "function") {
        throw new Error("Photoshop DOM mergeVisibleLayers API is unavailable; refusing to export an unbaked layer PNG.");
      }

      if (typeof doc.createLayer === "function") {
        const helperLayer = await doc.createLayer(constants.LayerKind.NORMAL, { name: "__BUI_EXPORT_TRANSPARENCY_BAKE__" });
        await identity.check("after-helper", true);
        try {
          helperLayer.visible = true;
        } catch (err) {}
      } else if (arrayFrom(doc.layers).filter((item) => readLayerVisible(item)).length < 2) {
        throw new Error("Photoshop DOM createLayer API is unavailable; a transparent helper layer is required before mergeVisibleLayers.");
      }

      await identity.check("before-merge", true);
      await doc.mergeVisibleLayers();
    }


    async function tryCompactNineSliceDocumentPixels(doc, border, sourcePath, warnings, nodeId, sharedPixels = null) {
      try {
        if (!imaging || typeof imaging.getPixels !== "function") {
          throw new Error("Photoshop Imaging API is unavailable.");
        }

        const originalWidth = Math.max(1, Math.round(toNumber(doc.width, 1)));
        const originalHeight = Math.max(1, Math.round(toNumber(doc.height, 1)));
        const borders = normalizeNineSliceBorder(border);
        const horizontalSlice = borders.left > 0 || borders.right > 0;
        const verticalSlice = borders.top > 0 || borders.bottom > 0;

        if (!horizontalSlice && !verticalSlice) {
          return null;
        }

        const centerSourceWidth = originalWidth - borders.left - borders.right;
        const centerSourceHeight = originalHeight - borders.top - borders.bottom;
        if (centerSourceWidth <= 0 || centerSourceHeight <= 0) {
          warnings.push({
            code: "PSD_NINESLICE_COMPACT_SKIPPED",
            severity: "Warning",
            message: "Nine-slice compact export was skipped because the border values are larger than the sliced image.",
            nodeId,
            sourcePath
          });
          return null;
        }

        const centerSize = getNineSliceCenterSize();
        const centerWidth = horizontalSlice ? Math.min(centerSize, centerSourceWidth) : centerSourceWidth;
        const centerHeight = verticalSlice ? Math.min(centerSize, centerSourceHeight) : centerSourceHeight;
        const compactWidth = horizontalSlice ? borders.left + centerWidth + borders.right : originalWidth;
        const compactHeight = verticalSlice ? borders.top + centerHeight + borders.bottom : originalHeight;

        if (compactWidth >= originalWidth && compactHeight >= originalHeight) {
          return null;
        }

        const sourceMidX = horizontalSlice ? borders.left + Math.floor((centerSourceWidth - centerWidth) * 0.5) : 0;
        const sourceMidY = verticalSlice ? borders.top + Math.floor((centerSourceHeight - centerHeight) * 0.5) : 0;
        const source = sharedPixels ? { pixels: sharedPixels } : await readDocumentPixelsRgba(doc, originalWidth, originalHeight);
        const compactPixels = new Uint8Array(compactWidth * compactHeight * 4);

        const xRegions = horizontalSlice
          ? [
              { sx: 0, dx: 0, size: borders.left },
              { sx: sourceMidX, dx: borders.left, size: centerWidth },
              { sx: originalWidth - borders.right, dx: borders.left + centerWidth, size: borders.right }
            ]
          : [{ sx: 0, dx: 0, size: originalWidth }];
        const yRegions = verticalSlice
          ? [
              { sy: 0, dy: 0, size: borders.top },
              { sy: sourceMidY, dy: borders.top, size: centerHeight },
              { sy: originalHeight - borders.bottom, dy: borders.top + centerHeight, size: borders.bottom }
            ]
          : [{ sy: 0, dy: 0, size: originalHeight }];

        for (const y of yRegions) {
          for (const x of xRegions) {
            copyRgbaRect(
              source.pixels,
              originalWidth,
              x.sx,
              y.sy,
              compactPixels,
              compactWidth,
              x.dx,
              y.dy,
              x.size,
              y.size
            );
          }
        }

        return {
          bytes: encodeRgbaPng(compactWidth, compactHeight, compactPixels),
          width: compactWidth,
          height: compactHeight
        };
      } catch (err) {
        if (sharedPixels) throw err;
        warnings.push({
          code: "PSD_NINESLICE_COMPACT_FAILED",
          severity: "Warning",
          message: `Nine-slice pixel compact failed, so the original cropped PNG will be used: ${formatError(err)}`,
          nodeId,
          sourcePath
        });
        return null;
      }
    }


    function normalizeNineSliceBorder(border) {
      return {
        left: Math.max(0, Math.round(toNumber(border?.left, 0))),
        top: Math.max(0, Math.round(toNumber(border?.top, 0))),
        right: Math.max(0, Math.round(toNumber(border?.right, 0))),
        bottom: Math.max(0, Math.round(toNumber(border?.bottom, 0)))
      };
    }


    function getNineSliceCenterSize() {
      return Math.max(1, Math.round(toNumber(readExportSetting("slicing.nineSliceCenterSize"), 2)));
    }


    function shouldCompactNineSlice() {
      return readExportSetting("slicing.compactNineSlice") === true;
    }


    async function readDocumentPixelsRgba(doc, width, height) {
      let imageObj = null;
      try {
        imageObj = await imaging.getPixels({
          documentID: doc.id,
          sourceBounds: {
            left: 0,
            top: 0,
            right: width,
            bottom: height
          },
          componentSize: 8,
          applyAlpha: false
        });

        const imageData = imageObj?.imageData;
        if (!imageData || typeof imageData.getData !== "function") {
          throw new Error("Photoshop Imaging API returned no image data.");
        }

        if (imageObj.level && imageObj.level !== 0) {
          throw new Error(`Photoshop Imaging API returned cache level ${imageObj.level}; full-resolution pixels are required.`);
        }

        if (imageData.componentSize && imageData.componentSize !== 8) {
          throw new Error(`Photoshop Imaging API returned ${imageData.componentSize}-bit components; 8-bit pixels are required.`);
        }

        const sourcePixels = await imageData.getData({ chunky: true });
        const sourceWidth = Math.max(1, Math.round(toNumber(imageData.width, width)));
        const sourceHeight = Math.max(1, Math.round(toNumber(imageData.height, height)));
        const components = Math.max(1, Math.round(toNumber(imageData.components, 4)));
        const bounds = normalizeReturnedPixelBounds(imageObj.sourceBounds, width, height);
        const rgba = new Uint8Array(width * height * 4);
        copyReturnedPixelsToRgba({
          sourcePixels,
          sourceWidth,
          sourceHeight,
          components,
          targetPixels: rgba,
          targetWidth: width,
          targetHeight: height,
          bounds
        });
        return { pixels: rgba, width, height };
      } finally {
        try {
          if (imageObj?.imageData && typeof imageObj.imageData.dispose === "function") {
            imageObj.imageData.dispose();
          }
        } catch (err) {}
      }
    }


    function normalizeReturnedPixelBounds(bounds, fallbackWidth, fallbackHeight) {
      if (!bounds || typeof bounds !== "object") {
        return {
          left: 0,
          top: 0,
          right: fallbackWidth,
          bottom: fallbackHeight
        };
      }

      const left = Math.max(0, Math.round(toNumber(bounds.left, 0)));
      const top = Math.max(0, Math.round(toNumber(bounds.top, 0)));
      const width = Math.round(toNumber(bounds.width, fallbackWidth - left));
      const height = Math.round(toNumber(bounds.height, fallbackHeight - top));
      const right = Math.min(fallbackWidth, Math.round(toNumber(bounds.right, left + width)));
      const bottom = Math.min(fallbackHeight, Math.round(toNumber(bounds.bottom, top + height)));
      return {
        left,
        top,
        right: Math.max(left, right),
        bottom: Math.max(top, bottom)
      };
    }


    function copyReturnedPixelsToRgba(options) {
      const {
        sourcePixels,
        sourceWidth,
        sourceHeight,
        components,
        targetPixels,
        targetWidth,
        targetHeight,
        bounds
      } = options;
      const width = Math.min(sourceWidth, bounds.right - bounds.left, targetWidth - bounds.left);
      const height = Math.min(sourceHeight, bounds.bottom - bounds.top, targetHeight - bounds.top);
      for (let y = 0; y < height; y += 1) {
        for (let x = 0; x < width; x += 1) {
          const sourceIndex = (y * sourceWidth + x) * components;
          const targetIndex = ((bounds.top + y) * targetWidth + bounds.left + x) * 4;
          targetPixels[targetIndex] = sourcePixels[sourceIndex] || 0;
          targetPixels[targetIndex + 1] = components > 1 ? sourcePixels[sourceIndex + 1] || 0 : sourcePixels[sourceIndex] || 0;
          targetPixels[targetIndex + 2] = components > 2 ? sourcePixels[sourceIndex + 2] || 0 : sourcePixels[sourceIndex] || 0;
          targetPixels[targetIndex + 3] = components > 3 ? sourcePixels[sourceIndex + 3] || 0 : 255;
        }
      }
    }


    function copyRgbaRect(sourcePixels, sourceWidth, sourceX, sourceY, targetPixels, targetWidth, targetX, targetY, width, height) {
      for (let y = 0; y < height; y += 1) {
        const sourceOffset = ((sourceY + y) * sourceWidth + sourceX) * 4;
        const targetOffset = ((targetY + y) * targetWidth + targetX) * 4;
        targetPixels.set(
          sourcePixels.subarray(sourceOffset, sourceOffset + width * 4),
          targetOffset
        );
      }
    }


    function encodeRgbaPng(width, height, pixels) {
      const scanlineLength = width * 4 + 1;
      const raw = new Uint8Array(scanlineLength * height);
      for (let y = 0; y < height; y += 1) {
        const rowOffset = y * scanlineLength;
        raw[rowOffset] = 0;
        raw.set(pixels.subarray(y * width * 4, (y + 1) * width * 4), rowOffset + 1);
      }

      const ihdr = new Uint8Array(13);
      writeUint32(ihdr, 0, width);
      writeUint32(ihdr, 4, height);
      ihdr[8] = 8;
      ihdr[9] = 6;
      ihdr[10] = 0;
      ihdr[11] = 0;
      ihdr[12] = 0;

      const idat = zlibStore(raw);
      return concatUint8Arrays([
        new Uint8Array([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
        makePngChunk("IHDR", ihdr),
        makePngChunk("IDAT", idat),
        makePngChunk("IEND", new Uint8Array(0))
      ]);
    }


    function zlibStore(data) {
      const blocks = Math.max(1, Math.ceil(data.length / 65535));
      const outputLength = 2 + data.length + blocks * 5 + 4;
      const output = new Uint8Array(outputLength);
      let offset = 0;
      output[offset++] = 0x78;
      output[offset++] = 0x01;
      let readOffset = 0;
      for (let block = 0; block < blocks; block += 1) {
        const remaining = data.length - readOffset;
        const length = Math.min(65535, Math.max(0, remaining));
        output[offset++] = block === blocks - 1 ? 0x01 : 0x00;
        output[offset++] = length & 0xff;
        output[offset++] = (length >>> 8) & 0xff;
        const nlen = (~length) & 0xffff;
        output[offset++] = nlen & 0xff;
        output[offset++] = (nlen >>> 8) & 0xff;
        output.set(data.subarray(readOffset, readOffset + length), offset);
        offset += length;
        readOffset += length;
      }
      writeUint32(output, offset, adler32(data));
      return output;
    }


    function makePngChunk(type, data) {
      const typeBytes = asciiBytes(type);
      const chunk = new Uint8Array(12 + data.length);
      writeUint32(chunk, 0, data.length);
      chunk.set(typeBytes, 4);
      chunk.set(data, 8);
      writeUint32(chunk, data.length + 8, crc32(chunk.subarray(4, data.length + 8)));
      return chunk;
    }


    function asciiBytes(text) {
      const bytes = new Uint8Array(text.length);
      for (let i = 0; i < text.length; i += 1) {
        bytes[i] = text.charCodeAt(i) & 0xff;
      }
      return bytes;
    }


    function concatUint8Arrays(parts) {
      const total = parts.reduce((sum, part) => sum + part.length, 0);
      const output = new Uint8Array(total);
      let offset = 0;
      parts.forEach((part) => {
        output.set(part, offset);
        offset += part.length;
      });
      return output;
    }


    function writeUint32(data, offset, value) {
      const unsigned = value >>> 0;
      data[offset] = (unsigned >>> 24) & 0xff;
      data[offset + 1] = (unsigned >>> 16) & 0xff;
      data[offset + 2] = (unsigned >>> 8) & 0xff;
      data[offset + 3] = unsigned & 0xff;
    }


    function adler32(data) {
      let a = 1;
      let b = 0;
      for (let i = 0; i < data.length; i += 1) {
        a = (a + data[i]) % 65521;
        b = (b + a) % 65521;
      }
      return ((b << 16) | a) >>> 0;
    }


    function crc32(data) {
      let crc = 0xffffffff;
      for (let i = 0; i < data.length; i += 1) {
        crc = CRC32_TABLE[(crc ^ data[i]) & 0xff] ^ (crc >>> 8);
      }
      return (crc ^ 0xffffffff) >>> 0;
    }


    const CRC32_TABLE = makeCrc32Table();


    function makeCrc32Table() {
      const table = new Uint32Array(256);
      for (let i = 0; i < 256; i += 1) {
        let value = i;
        for (let j = 0; j < 8; j += 1) {
          value = (value & 1) ? (0xedb88320 ^ (value >>> 1)) : (value >>> 1);
        }
        table[i] = value >>> 0;
      }
      return table;
    }


    async function duplicateLayerToDocument(sourceDoc, layer, targetDoc, identity) {
      await identity.check("before-duplicate");
      let duplicateError = null;
      if (layer && typeof layer.duplicate === "function") {
        try {
          const duplicated = await layer.duplicate(targetDoc);
          await identity.check("after-duplicate");
          if (duplicated) {
            // Show only the isolated duplicate root. Its descendants keep their own visibility.
            duplicated.visible = true;
            return;
          }
        } catch (err) {
          if (err.bridgeRejected) throw err;
          duplicateError = err;
        }
      }

      if (!sourceDoc?.duplicateLayers) {
        const suffix = duplicateError ? ` Last duplicate error: ${formatError(duplicateError)}` : "";
        throw new Error(`Photoshop DOM layer duplicate API is unavailable.${suffix}`);
      }

      await identity.check("before-duplicate-layers");
      await sourceDoc.duplicateLayers([layer], targetDoc);
      await identity.check("after-duplicate-layers");
      const roots = arrayFrom(targetDoc.layers).filter(item => !item.isBackgroundLayer);
      if (roots.length !== 1) throw new Error("Cannot identify the isolated duplicate root.");
      roots[0].visible = true;
    }

    async function trySha256Hex(bytes, warnings, nodeId, sourcePath) {
      try {
        return await sha256Hex(bytes);
      } catch (err) {
        warnings.push({
          code: "ASSET_SHA256_UNAVAILABLE",
          severity: "Info",
          message: `PNG asset was exported, but sha256 is unavailable: ${formatError(err)}`,
          nodeId,
          sourcePath
        });
        return null;
      }
    }


    function findDedupedPngResource(contentHash, exportState, resource, warnings) {
      if (!contentHash) {
        return null;
      }

      exportState.pngResourceDedupe = exportState.pngResourceDedupe || {};
      const key = makePngDedupeKey(contentHash, resource);
      const existing = exportState.pngResourceDedupe[key];
      if (!existing) {
        return null;
      }

      warnings.push({
        code: "RESOURCE_DEDUPED_BY_PNG_CONTENT",
        severity: "Info",
        message: `PNG resource was identical after ignoring text metadata and now reuses ${existing.asset}.`,
        nodeId: resource.nodeId,
        sourcePath: resource.sourcePath
      });
      return {
        asset: existing.asset,
        sha256: existing.sha256,
        contentHash: existing.contentHash,
        contentHashAlgorithm: existing.contentHashAlgorithm,
        width: existing.width,
        height: existing.height,
        resourceType: existing.resourceType
      };
    }

    function rememberPngDedupeResource(contentHash, exportState, resource) {
      if (!contentHash) {
        return;
      }

      exportState.pngResourceDedupe = exportState.pngResourceDedupe || {};
      const key = makePngDedupeKey(contentHash, resource);
      if (!exportState.pngResourceDedupe[key]) {
        exportState.pngResourceDedupe[key] = {
          asset: resource.fileName,
          sha256: resource.sha256,
          contentHash,
          contentHashAlgorithm: PNG_CONTENT_HASH_ALGORITHM,
          width: resource.width,
          height: resource.height,
          resourceType: resource.resourceType
        };
      }
    }

    async function reservePngFileName(folder, requestedName, contentHash, resource, exportState) {
      exportState.pngNameClaims = exportState.pngNameClaims || {};
      const claims = exportState.pngNameClaims;
      const identity = contentHash ? makePngDedupeKey(contentHash, resource) : null;
      const base = stripExtension(requestedName);
      for (let index = 0; index < 10000; index += 1) {
        const name = index === 0 ? requestedName : base + "_" + String(index).padStart(2, "0") + ".png";
        const key = name.toLowerCase();
        if (claims[key] && (!identity || claims[key] !== identity)) continue;
        if (resource.outputModule) {
          const existing = await getEntryIfExists(folder, name, true);
          if (existing) {
            if (existing.isFolder) continue;
            const oldBytes = await tryReadBinary(existing);
            const oldHash = oldBytes ? await tryNormalizedPngHash(oldBytes) : null;
            if (!contentHash || !oldHash)
              throw new Error("PNG_NAME_CONFLICT: Cannot verify existing PNG " + name);
            if (contentHash !== oldHash) continue;
          }
        }
        claims[key] = identity || "unverified";
        return name;
      }
      throw new Error("PNG_NAME_CONFLICT: No available numbered PNG name for " + requestedName);
    }

    function makePngDedupeKey(contentHash, resource) {
      return [
        contentHash,
        resource.width,
        resource.height,
        safeString(resource.resourceType, ""),
        safeString(resource.outputModule, "").toLowerCase(),
        formatNineSliceKey(resource.nineSlice)
      ].join("|");
    }

    async function createTemporaryPngFile(assetsFolder, fileName) {
      const baseName = stripExtension(fileName) || "slice";
      for (let i = 0; i < 20; i += 1) {
        const suffix = `${Date.now()}_${Math.floor(Math.random() * 1000000)}_${i}`;
        const tempName = `.__bui_tmp_${baseName}_${suffix}.png`;
        try {
          return await writeOutput(() => assetsFolder.createFile(tempName, { overwrite: false }));
        } catch (err) {if (err.firstWriteRejected) throw err;}
      }

      return await writeOutput(() => assetsFolder.createFile(`.__bui_tmp_${baseName}.png`, { overwrite: true }));
    }

    async function writePngIfContentChanged(assetsFolder, fileName, tempOutputFile, newBytes, newContentHash, warnings, nodeId, sourcePath) {
      const existingFile = await getEntryIfExists(assetsFolder, fileName);
      if (existingFile && !existingFile.isFolder && newContentHash) {
        const oldBytes = await tryReadBinary(existingFile);
        const oldContentHash = oldBytes ? await tryNormalizedPngHash(oldBytes) : null;
        if (oldContentHash && oldContentHash === newContentHash) {
          const oldSha256 = await trySha256Hex(oldBytes, warnings, nodeId, sourcePath);
          await deleteDuplicateOutputFile(tempOutputFile, warnings, nodeId, sourcePath, tempOutputFile.name || fileName);
          warnings.push({
            code: "PNG_WRITE_SKIPPED_SAME_CONTENT",
            severity: "Info",
            message: "Existing PNG file was kept because normalized contentHash is unchanged.",
            nodeId,
            sourcePath
          });
          return { file: existingFile, sha256: oldSha256, contentHash: oldContentHash, reusedExistingFile: true };
        }
      }

      if (existingFile && existingFile.isFolder) {
        await deleteDuplicateOutputFile(existingFile, warnings, nodeId, sourcePath, fileName);
      }

      const targetFile = await assetsFolder.createFile(fileName, { overwrite: true });
      await targetFile.write(newBytes, { format: formats.binary });
      const writtenBytes = await targetFile.read({ format: formats.binary });
      const sha256 = await trySha256Hex(writtenBytes, warnings, nodeId, sourcePath);
      await deleteDuplicateOutputFile(tempOutputFile, warnings, nodeId, sourcePath, tempOutputFile.name || fileName);
      return { file: targetFile, sha256, contentHash: newContentHash, reusedExistingFile: false };
    }

    async function tryReadBinary(entry) {
      try {
        if (!entry || entry.isFolder || typeof entry.read !== "function") {
          return null;
        }

        return await entry.read({ format: formats.binary });
      } catch (err) {
        return null;
      }
    }

    async function getEntryIfExists(folder, name, requireReadable = false) {
      if (!folder || !name) {
        return null;
      }

      try {
        if (typeof folder.getEntry === "function") {
          const entry = await folder.getEntry(name);
          if (entry) {
            return entry;
          }
        }
      } catch (err) {}

      try {
        if (typeof folder.getEntries !== "function") {
          if (requireReadable) throw new Error("Directory enumeration unavailable");
          return null;
        }

        const entries = await folder.getEntries();
        const exact = arrayFrom(entries).find((entry) => entry?.name === name);
        if (exact) {
          return exact;
        }

        const foldedName = name.toLowerCase();
        return arrayFrom(entries).find((entry) => safeString(entry?.name, "").toLowerCase() === foldedName) || null;
      } catch (err) {
        if (requireReadable) throw new Error("PNG_NAME_CONFLICT: Cannot verify destination " + name + "; " + formatError(err));
        return null;
      }
    }


    function withContentHash(resource, contentHash) {
      if (!contentHash) {
        return resource;
      }

      return {
        ...resource,
        contentHash,
        contentHashAlgorithm: PNG_CONTENT_HASH_ALGORITHM
      };
    }


    function assertNativePngBytes(bytes) {
      const data = toUint8Array(bytes);
      const invalid = () => { throw new Error("Photoshop PNG save produced an empty, incomplete or damaged PNG."); };
      if (!isPngBytes(data)) invalid();
      let offset = 8, hasHeader = false, imageBytes = 0;
      while (offset + 12 <= data.length) {
        const length = readUint32(data, offset), end = offset + length + 12;
        if (end > data.length) invalid();
        const type = String.fromCharCode(...data.subarray(offset + 4, offset + 8));
        if (crc32(data.subarray(offset + 4, end - 4)) !== readUint32(data, end - 4)) invalid();
        if (!hasHeader) {
          if (type !== "IHDR" || length !== 13 || !readUint32(data, offset + 8) || !readUint32(data, offset + 12)) invalid();
          hasHeader = true;
        } else if (type === "IHDR") invalid();
        if (type === "IDAT") imageBytes += length;
        if (type === "IEND") {
          if (length !== 0 || imageBytes === 0 || end !== data.length) invalid();
          return;
        }
        offset = end;
      }
      invalid();
    }


    async function tryNormalizedPngHash(bytes) {
      const data = toUint8Array(bytes);
      if (!isPngBytes(data)) {
        return null;
      }

      const ranges = [{ start: 0, end: 8 }];
      let totalLength = 8;
      let offset = 8;
      while (offset + 12 <= data.length) {
        const length = readUint32(data, offset);
        const typeOffset = offset + 4;
        const dataOffset = offset + 8;
        const nextOffset = dataOffset + length + 4;
        if (length < 0 || nextOffset > data.length) {
          return null;
        }

        const type = String.fromCharCode(data[typeOffset], data[typeOffset + 1], data[typeOffset + 2], data[typeOffset + 3]);
        if (!isIgnoredPngMetadataChunk(type)) {
          ranges.push({ start: offset, end: nextOffset });
          totalLength += nextOffset - offset;
        }

        offset = nextOffset;
        if (type === "IEND") {
          break;
        }
      }

      const normalized = new Uint8Array(totalLength);
      let writeOffset = 0;
      ranges.forEach((range) => {
        normalized.set(data.subarray(range.start, range.end), writeOffset);
        writeOffset += range.end - range.start;
      });
      return await sha256Hex(normalized);
    }


    function toUint8Array(bytes) {
      if (bytes instanceof Uint8Array) {
        return bytes;
      }

      if (bytes && bytes.buffer instanceof ArrayBuffer) {
        return new Uint8Array(bytes.buffer, bytes.byteOffset || 0, bytes.byteLength);
      }

      if (bytes instanceof ArrayBuffer) {
        return new Uint8Array(bytes);
      }

      return new Uint8Array(bytes || []);
    }


    function isPngBytes(data) {
      return data &&
        data.length >= 8 &&
        data[0] === 0x89 &&
        data[1] === 0x50 &&
        data[2] === 0x4e &&
        data[3] === 0x47 &&
        data[4] === 0x0d &&
        data[5] === 0x0a &&
        data[6] === 0x1a &&
        data[7] === 0x0a;
    }


    function readUint32(data, offset) {
      return ((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]) >>> 0;
    }


    function isIgnoredPngMetadataChunk(type) {
      return type === "iTXt" || type === "tEXt" || type === "zTXt" || type === "tIME";
    }


    function formatNineSliceKey(nineSlice) {
      return nineSlice
        ? [nineSlice.left, nineSlice.top, nineSlice.right, nineSlice.bottom].map((value) => String(Math.round(toNumber(value, 0)))).join(",")
        : "";
    }


    async function deleteDuplicateOutputFile(outputFile, warnings, nodeId, sourcePath, fileName) {
      try {
        if (outputFile && typeof outputFile.delete === "function") {
          await outputFile.delete();
          return;
        }

        if (outputFile && typeof outputFile.remove === "function") {
          await outputFile.remove();
          return;
        }
      } catch (err) {
        if (isMissingFileError(err)) {
          return;
        }

        warnings.push({
          code: "DEDUPED_PNG_DELETE_FAILED",
          severity: "Info",
          message: `Duplicate PNG resource was deduped in BUI but the temporary duplicate file could not be removed: ${fileName}; ${formatError(err)}`,
          nodeId,
          sourcePath
        });
      }
    }

    function isMissingFileError(err) {
      const message = safeString(err?.message || err, "").toLowerCase();
      return message.includes("no such file") ||
        message.includes("not found") ||
        message.includes("enoent");
    }

    return { tryExportLayerPng, exportTextReferences, normalizeCropBounds, normalizeNineSliceBorder };
  }

  global.BurnerPsdToBuiPngExporter = { createPngExporter };
})(globalThis);
