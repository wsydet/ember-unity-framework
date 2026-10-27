/* global globalThis */

(function registerLayerReader(global) {
  function createLayerReader(options) {
    const { maybePromise, parseBounds, formatError, safeString, toNumber, round2, action } = options;

    async function readLayerBounds(layer, canvas, sourcePath, warnings) {
      try {
        const bounds = await maybePromise(layer.boundsNoEffects || layer.bounds);
        const parsed = parseBounds(bounds);
        if (parsed) {
          return parsed;
        }
      } catch (err) {
        warnings.push({
          code: "LAYER_BOUNDS_UNAVAILABLE",
          severity: "Warning",
          message: `UXP could not read layer bounds: ${formatError(err)}`,
          sourcePath
        });
      }

      throw new Error(`DESIGN_BOUNDS_UNAVAILABLE: ${sourcePath}. PSD layer bounds cannot be replaced with canvas dimensions.`);
    }


    async function readLayerExportBounds(sourceDoc, layer, canvas, sourcePath, warnings) {
      const visible = await readLayerBounds(layer, canvas, sourcePath, warnings);
      if (detectLayerType(layer, false) !== "smartObject") return visible;
      // Smart-object DOM bounds trim transparent padding. The placed quad keeps it.
      const layerId = readRawLayerId(layer);
      if (!Number.isInteger(Number(sourceDoc?.id)) || layerId === null || !action?.batchPlay)
        throw new Error("SMART_OBJECT_EXPORT_BOUNDS_UNAVAILABLE: " + sourcePath);
      const result = await action.batchPlay([{
        _obj: "get", _target: [{ _property: "smartObjectMore" },
          { _ref: "layer", _id: layerId }, { _ref: "document", _id: Number(sourceDoc.id) }],
        _options: { dialogOptions: "dontDisplay" }
      }], { synchronousExecution: false, modalBehavior: "fail" });
      const more = result?.[0]?.smartObjectMore;
      const quad = more?.nonAffineTransform || more?.transform;
      if (!Array.isArray(quad) || quad.length !== 8 || !quad.every(Number.isFinite))
        throw new Error("SMART_OBJECT_EXPORT_BOUNDS_UNAVAILABLE: " + sourcePath);
      const xs = quad.filter((_, index) => index % 2 === 0), ys = quad.filter((_, index) => index % 2 === 1);
      if (Math.max(...xs) <= Math.min(...xs) || Math.max(...ys) <= Math.min(...ys))
        throw new Error("SMART_OBJECT_EXPORT_BOUNDS_UNAVAILABLE: " + sourcePath);
      return { left: Math.min(visible.left, ...xs), top: Math.min(visible.top, ...ys),
        right: Math.max(visible.right, ...xs), bottom: Math.max(visible.bottom, ...ys) };
    }

    function readLayerId(layer, sourcePath, warnings) {
      const id = layer.id ?? layer._id ?? layer.layerID ?? null;
      if (id === null || id === undefined || id === "") {
        warnings.push({
          code: "LAYER_ID_UNAVAILABLE",
          severity: "Warning",
          message: "Photoshop DOM did not expose a layer id. Exporter used a sourcePath hash for node id fallback.",
          sourcePath
        });
        return null;
      }

      return id;
    }


    function readLayerVisible(layer) {
      if (typeof layer.visible === "boolean") {
        return layer.visible;
      }
      if (typeof layer.enabled === "boolean") {
        return layer.enabled;
      }
      return true;
    }


    function detectLayerType(layer, isGroup) {
      if (isGroup) {
        return "group";
      }

      const kind = safeString(layer.kind || layer.type || layer.layerKind, "").toLowerCase();
      if (kind.includes("text") || layer.textItem || layer.text) {
        return "text";
      }
      if (kind.includes("smart")) {
        return "smartObject";
      }
      if (kind.includes("shape") || kind.includes("solid")) {
        return "shape";
      }
      return "image";
    }


    function mapNodeType(layerType) {
      if (layerType === "text") {
        return "Text";
      }
      return "Image";
    }


    function readTextInfoSync(layer, sourcePath, warnings) {
      return readTextInfoFromDescriptor(layer, sourcePath, warnings, null);
    }


    async function readTextInfo(layer, sourcePath, warnings) {
      const descriptor = await readTextDescriptor(layer, sourcePath, warnings);
      return readTextInfoFromDescriptor(layer, sourcePath, warnings, descriptor);
    }


    function readTextInfoFromDescriptor(layer, sourcePath, warnings, descriptor) {
      const textItem = layer.textItem || layer.text || descriptor;
      if (!textItem) {
        warnings.push({ code: "TEXT_INPUT_UNAVAILABLE", severity: "Warning",
          message: "Neither native text DOM nor textKey descriptor is available. Font, size and color must remain unresolved.", sourcePath });
        return null;
      }

      let value = "";
      try {
        value = safeString(textItem.contents ?? textItem.text ?? descriptor?.textKey ?? "", "");
      } catch (err) {
        value = "";
      }

      let fontSize = null;
      let fontSizeError = null;
      try {
        fontSize = readTextFontSize(layer, textItem, descriptor);
      } catch (err) {
        fontSizeError = err;
      }

      if (fontSize === null) {
        warnings.push({
          code: "TEXT_FONT_SIZE_UNAVAILABLE",
          severity: "Warning",
          message: fontSizeError
            ? `Photoshop DOM did not expose a usable Text font size. Original message: ${formatError(fontSizeError)}`
            : "Photoshop DOM did not expose a usable Text font size. Design font size is unresolved; Maker must not substitute a default.",
          sourcePath
        });
      }

      const visualStyle = buildTextVisualStyle(layer, textItem, descriptor, fontSize);
      if (!visualStyle || visualStyle.status !== "Resolved") {
        warnings.push({
          code: "TEXT_STYLE_UNCERTAIN",
          severity: "Warning",
          message: visualStyle
            ? `Text visual style export is ${visualStyle.status}. Missing or uncertain fields: ${(visualStyle.missingFields || []).join(", ") || "none"}.`
            : "Text visual style export is unavailable. TMP style mapping will need manual confirmation.",
          sourcePath
        });
      }
      pushTextFillColorDiagnostic(layer, sourcePath, descriptor, visualStyle, warnings);

      const textInfo = {
        text: value,
        valuePolicy: "preserve",
        styleStatus: visualStyle?.status === "Resolved" ? "Resolved" : "Partial",
        tmpStatus: "Placeholder",
        source: { kind: "photoshop_native_text", layerId: readRawLayerId(layer), sourcePath,
          readMode: layer.textItem || layer.text ? (descriptor ? "dom_and_descriptor" : "dom_only") : "descriptor_only" }
      };
      if (fontSize !== null) {
        textInfo.fontSize = fontSize;
      }
      if (visualStyle) {
        textInfo.visualStyle = visualStyle;
      }

      return textInfo;
    }


    async function readTextDescriptor(layer, sourcePath, warnings) {
      if (!action || typeof action.batchPlay !== "function") {
        return null;
      }

      const layerId = readRawLayerId(layer);
      if (layerId === null) {
        return null;
      }

      const descriptor = await readLayerDescriptorProperty(layerId, "textKey", sourcePath, warnings, true);
      if (!descriptor) {
        return null;
      }

      const layerEffects = await readLayerDescriptorProperty(layerId, "layerEffects", sourcePath, warnings, false);
      if (layerEffects) {
        descriptor.rawLayerEffects = layerEffects.layerEffects || layerEffects;
        const visibility = await readLayerDescriptorProperty(layerId, "layerFXVisible", sourcePath, warnings, false);
        const visible = typeof visibility === "boolean" ? visibility : readFirstBoolean([visibility], ["layerFXVisible"]);
        const recorded = readFirstBoolean([descriptor.rawLayerEffects], ["masterFXSwitch"]);
        descriptor.layerEffectsEnabled = visible !== null && recorded !== null && visible !== recorded
          ? null : visible ?? recorded;
        // Effect parameters survive Photoshop's master switch being turned off.
        // Only effective effects go to existing consumers; retain the raw descriptor separately.
        descriptor.layerEffects = descriptor.layerEffectsEnabled === true ? descriptor.rawLayerEffects : null;
      }
      descriptor.globalLightingAngle = null;
      const effects = descriptor.layerEffects || {};
      const shadows = [effects.dropShadow, ...(Array.isArray(effects.dropShadowMulti) ? effects.dropShadowMulti : [])];
      if (shadows.some(shadow => shadow?.enabled === true && shadow?.useGlobalAngle === true)) {
        descriptor.globalLightingAngle = await readGlobalLightingAngle(sourcePath, warnings);
      }

      const strokeStyle = await readLayerDescriptorProperty(layerId, "strokeStyle", sourcePath, warnings, false);
      if (strokeStyle) {
        descriptor.strokeStyle = strokeStyle.strokeStyle || strokeStyle;
      }

      descriptor.effectsReadStatus = layerEffects || strokeStyle ? "recorded" : "unavailable_or_absent";
      return descriptor;
    }

    async function readGlobalLightingAngle(sourcePath, warnings) {
      try {
        const result = await action.batchPlay([{
          _obj: "get",
          _target: [{ _property: "globalAngle" }, { _ref: "document", _enum: "ordinal", _value: "targetEnum" }]
        }], { synchronousExecution: false, modalBehavior: "fail" });
        const raw = result?.[0]?.globalAngle;
        const angle = typeof raw === "number" ? raw : raw?._value;
        if (Number.isFinite(angle)) return angle;
      } catch (err) {
        warnings.push({ code: "TEXT_SHADOW_GLOBAL_ANGLE_UNAVAILABLE", severity: "Warning",
          message: `Could not read document global lighting angle: ${formatError(err)}`, sourcePath });
        return null;
      }
      warnings.push({ code: "TEXT_SHADOW_GLOBAL_ANGLE_UNAVAILABLE", severity: "Warning",
        message: "Document global lighting angle is missing. A global-light shadow cannot use its local angle as a substitute.", sourcePath });
      return null;
    }


    async function readLayerDescriptorProperty(layerId, property, sourcePath, warnings, required) {
      try {
        const result = await action.batchPlay([
          {
            _obj: "get",
            _target: [
              { _property: property },
              { _ref: "layer", _id: layerId }
            ]
          }
        ], {
          synchronousExecution: false,
          modalBehavior: "fail"
        });

        const first = Array.isArray(result) ? result[0] : null;
        if (!first || typeof first !== "object") {
          return null;
        }

        if (first._obj === "error") {
          if (required) {
            warnings.push({
              code: "TEXT_STYLE_UNCERTAIN",
              severity: "Warning",
              message: `UXP batchPlay returned an error descriptor while reading Text ${property}.`,
              sourcePath
            });
          }
          return null;
        }

        if (first[property] !== undefined && first[property] !== null) {
          return first[property];
        }

        return Object.keys(first).length > 0 ? first : null;
      } catch (err) {
        if (required) {
          warnings.push({
            code: "TEXT_STYLE_UNCERTAIN",
            severity: "Warning",
            message: `UXP batchPlay could not read Text ${property} descriptor: ${formatError(err)}`,
            sourcePath
          });
        }
        return null;
      }
    }


    function readRawLayerId(layer) {
      const id = layer?.id ?? layer?._id ?? layer?.layerID ?? null;
      if (id === null || id === undefined || id === "") {
        return null;
      }

      const numericId = typeof id === "number" ? id : Number(id);
      return Number.isFinite(numericId) ? numericId : id;
    }


    function readTextFontSize(layer, textItem, descriptor) {
      const transformScaleY = readTextTransformScaleY(layer, textItem, descriptor);
      const directCandidates = [
        textItem?.characterStyle?.size,
        textItem?.characterStyle?.fontSize,
        textItem?.size,
        textItem?.fontSize,
        textItem?.textStyle?.size,
        textItem?.textStyle?.fontSize,
        descriptor?.textStyle?.size,
        descriptor?.textStyle?.fontSize,
        descriptor?.textStyle?.fontSize?._value
      ];

      for (const candidate of directCandidates) {
        const fontSize = normalizeFontSize(candidate, transformScaleY);
        if (fontSize !== null) {
          return fontSize;
        }
      }

      const rangeFontSize = readTextStyleRangeFontSize(
        textItem?.textStyleRange || textItem?.textStyleRanges || descriptor?.textStyleRange || descriptor?.textStyleRanges,
        transformScaleY
      );
      return rangeFontSize;
    }


    function buildTextVisualStyle(layer, textItem, descriptor, fontSize) {
      const ranges = normalizeArray(
        textItem?.textStyleRange ||
        textItem?.textStyleRanges ||
        descriptor?.textStyleRange ||
        descriptor?.textStyleRanges
      );
      const primaryRangeStyle = ranges.length > 0
        ? (ranges[0]?.textStyle || ranges[0]?.characterStyle || null)
        : null;
      const styleCandidates = [
        primaryRangeStyle,
        textItem?.characterStyle,
        textItem?.textStyle,
        descriptor?.textStyle,
        descriptor?.paragraphStyle,
        textItem?.paragraphStyle,
        descriptor,
        textItem
      ].filter(Boolean);

      const paragraphCandidates = [
        firstRangeStyle(textItem?.paragraphStyleRange || textItem?.paragraphStyleRanges),
        firstRangeStyle(descriptor?.paragraphStyleRange || descriptor?.paragraphStyleRanges),
        textItem?.paragraphStyle,
        descriptor?.paragraphStyle
      ].filter(Boolean);

      const features = {};
      const missingFields = [];
      const styleWarnings = [];
      if (descriptor?.rawLayerEffects && descriptor.layerEffectsEnabled === null) {
        missingFields.push("layerEffectsEnabled");
      }

      if (fontSize !== null) {
        features.fontSize = fontSize;
      }

      const fontIdentity = readFirstString(styleCandidates, [
        "fontPostScriptName",
        "postScriptName",
        "fontName",
        "font",
        "fontFamilyName",
        "fontFamily"
      ]);
      if (fontIdentity) {
        features.fontIdentity = fontIdentity;
      } else {
        missingFields.push("fontIdentity");
      }

      const fontPostScriptName = readFirstString(styleCandidates, ["fontPostScriptName", "postScriptName"]);
      if (fontPostScriptName) features.fontPostScriptName = fontPostScriptName;

      const fontFamily = readFirstString(styleCandidates, ["fontFamily", "fontFamilyName", "family"]);
      if (fontFamily) {
        features.fontFamily = fontFamily;
      }

      const fontStyleName = readFirstString(styleCandidates, ["fontStyleName", "fontStyle", "styleName"]);
      if (fontStyleName) {
        features.fontStyle = fontStyleName;
      }

      const syntheticBold = readFirstBoolean(styleCandidates, ["syntheticBold", "fauxBold", "bold"]);
      if (syntheticBold !== null) {
        features.syntheticBold = syntheticBold;
      }

      const syntheticItalic = readFirstBoolean(styleCandidates, ["syntheticItalic", "fauxItalic", "italic"]);
      if (syntheticItalic !== null) {
        features.syntheticItalic = syntheticItalic;
      }

      const weight = normalizeFontWeight(
        readFirstValue(styleCandidates, ["fontWeight", "weight"]) ||
        fontStyleName ||
        fontIdentity,
        syntheticBold
      );
      features.weight = weight;

      const layerEffectFill = readTextLayerEffectFillColor(descriptor);
      const textFillColor = readFirstColor(styleCandidates, [
        "color",
        "fillColor",
        "textFillColor",
        "solidColor"
      ]);
      const fillColor = layerEffectFill?.color || textFillColor;
      if (fillColor) {
        features.fillColor = fillColor.hex;
        const fillAlpha = layerEffectFill?.opacity ?? fillColor.alpha;
        if (fillAlpha !== null) {
          features.fillAlpha = fillAlpha;
        }
        features.fillSource = layerEffectFill ? layerEffectFill.source : "textStyle";
        if (layerEffectFill?.blendMode) {
          features.fillBlendMode = layerEffectFill.blendMode;
        }
      } else {
        missingFields.push("fillColor");
      }

      const tracking = readFirstNumeric(styleCandidates, ["tracking", "track", "characterSpacing"]);
      if (tracking !== null) {
        features.tracking = roundStyleNumber(tracking);
      }

      const leading = readFirstNumeric(styleCandidates, ["leading", "lineSpacing", "autoLeading"]);
      if (leading !== null) {
        features.leading = roundStyleNumber(leading);
      }

      const alignment = normalizeTextAlignment(
        readFirstValue(paragraphCandidates, ["justification", "align", "alignment"]) ||
        readFirstValue(styleCandidates, ["justification", "align", "alignment"])
      );
      if (alignment) {
        features.alignment = alignment;
      }

      const transformScaleX = readTextTransformScaleX(layer, textItem, descriptor);
      const transformScaleY = readTextTransformScaleY(layer, textItem, descriptor);
      if (transformScaleX !== 1 || transformScaleY !== 1) {
        features.transformScale = {
          x: roundStyleNumber(transformScaleX),
          y: roundStyleNumber(transformScaleY)
        };
      }

      const stroke = readTextStrokeStyle(styleCandidates, descriptor);
      if (stroke.found) {
        features.strokeEnabled = stroke.enabled;
        features.strokeBucket = stroke.enabled ? stroke.bucket : "none";
        if (stroke.enabled) {
          features.strokeWidth = stroke.width;
          if (stroke.color) {
            features.strokeColor = stroke.color.hex;
            if (stroke.color.alpha !== null) {
              features.strokeAlpha = stroke.color.alpha;
            }
          }
        }
        if (stroke.position) {
          features.strokePosition = stroke.position;
        }
        if (stroke.source) {
          features.strokeSource = stroke.source;
        }
        if (stroke.enabled && stroke.widthMissing) {
          missingFields.push("strokeWidth");
        }
        if (stroke.enabled && stroke.colorMissing) {
          missingFields.push("strokeColor");
        }
      } else {
        features.strokeEnabled = false;
        features.strokeBucket = "none";
        features.strokeSource = descriptor?.layerEffectsEnabled === false ? "psdLayerEffectsDisabled" : "assumedNone";
        if (hasLayerEffectHint(layer) && descriptor?.layerEffectsEnabled !== false) {
          missingFields.push("strokeStyle");
        }
      }

      if (ranges.length > 1) {
        styleWarnings.push("Multiple text style ranges detected; visualStyle uses the first range as the primary style.");
      }

      const key = buildTextVisualStyleKey(features);
      const status = resolveTextVisualStyleStatus(features, missingFields, ranges.length);
      return {
        version: 1,
        status,
        key,
        features,
        effects: {
          status: descriptor?.effectsReadStatus || "unavailable",
          source: "photoshop_batchPlay",
          application: "record_only",
          layerEffects: descriptor?.layerEffects || null,
          layerEffectsEnabled: descriptor?.layerEffectsEnabled ?? null,
          rawLayerEffects: descriptor?.layerEffectsEnabled === true ? null : descriptor?.rawLayerEffects || null,
          globalLightingAngle: descriptor?.globalLightingAngle ?? null,
          strokeStyle: descriptor?.strokeStyle || null
        },
        textStyleRanges: ranges,
        missingFields,
        warnings: styleWarnings
      };
    }


    function firstRangeStyle(ranges) {
      const normalized = normalizeArray(ranges);
      if (normalized.length === 0) {
        return null;
      }

      return normalized[0]?.paragraphStyle || normalized[0]?.textStyle || normalized[0]?.characterStyle || normalized[0];
    }


    function normalizeArray(value) {
      if (!value) {
        return [];
      }
      return Array.isArray(value) ? value : [value];
    }


    function readFirstString(owners, keys) {
      const value = readFirstValue(owners, keys);
      const text = safeString(value, "").trim();
      return text || null;
    }


    function readFirstValue(owners, keys) {
      for (const owner of owners) {
        for (const key of keys) {
          const value = safeReadPath(owner, key);
          if (value !== null && value !== undefined && value !== "") {
            return value;
          }
        }
      }

      return null;
    }


    function readFirstNumeric(owners, keys) {
      const value = readFirstValue(owners, keys);
      const numeric = readNumericValue(value);
      return numeric !== null && Number.isFinite(numeric) ? numeric : null;
    }


    function readFirstBoolean(owners, keys) {
      const value = readFirstValue(owners, keys);
      if (typeof value === "boolean") {
        return value;
      }
      if (typeof value === "string") {
        const normalized = value.trim().toLowerCase();
        if (normalized === "true") {
          return true;
        }
        if (normalized === "false") {
          return false;
        }
      }

      return null;
    }


    function readFirstColor(owners, keys) {
      for (const owner of owners) {
        for (const key of keys) {
          const color = readColorValue(safeReadPath(owner, key));
          if (color) {
            return color;
          }
        }
      }

      return null;
    }


    function readColorValue(value) {
      if (!value) {
        return null;
      }

      if (typeof value === "string") {
        const normalized = value.trim();
        const match = normalized.match(/^#?([0-9a-fA-F]{6})([0-9a-fA-F]{2})?$/);
        if (!match) {
          return null;
        }
        const hex = `#${match[1].toUpperCase()}`;
        const alpha = match[2] ? parseInt(match[2], 16) / 255 : null;
        return { hex, alpha: alpha === null ? null : roundStyleNumber(alpha) };
      }

      if (typeof value !== "object") {
        return null;
      }

      const colorOwner = value.color && typeof value.color === "object" ? value.color : value;
      const red = readColorChannel(colorOwner, ["red", "r", "Rd  ", "Rd", "R"], ["redFloat", "redF", "Rf"]);
      const green = readColorChannel(colorOwner, ["green", "grain", "g", "Grn ", "Grn", "G"], ["greenFloat", "grainFloat", "greenF", "gf"]);
      const blue = readColorChannel(colorOwner, ["blue", "b", "Bl  ", "Bl", "B"], ["blueFloat", "blueF", "bf"]);
      if (red === null || green === null || blue === null) {
        return null;
      }

      const alpha = readFirstNumericProperty(colorOwner, ["alpha", "opacity", "A"]);
      return {
        hex: `#${toHexByte(red)}${toHexByte(green)}${toHexByte(blue)}`,
        alpha: alpha === null ? null : roundStyleNumber(alpha > 1 ? alpha / 100 : alpha)
      };
    }


    function readColorChannel(owner, byteKeys, floatKeys) {
      const byteValue = readFirstNumericProperty(owner, byteKeys);
      if (byteValue !== null) {
        return byteValue;
      }

      const floatValue = readFirstNumericProperty(owner, floatKeys);
      if (floatValue === null) {
        return null;
      }

      return floatValue * 255;
    }


    function readTextLayerEffectFillColor(descriptor) {
      const effectOwners = [
        descriptor?.layerEffects,
        descriptor?.effects,
        descriptor
      ].filter(Boolean);
      const fillCandidates = [];

      for (const owner of effectOwners) {
        fillCandidates.push(
          ...normalizeArray(owner?.solidFill),
          ...normalizeArray(owner?.solidFillMulti),
          ...normalizeArray(owner?.colorOverlay),
          ...normalizeArray(owner?.colorOverlayMulti),
          ...normalizeArray(owner?.SoFi)
        );
      }

      for (const candidate of fillCandidates) {
        if (!candidate || typeof candidate !== "object" || Object.keys(candidate).length === 0) {
          continue;
        }

        if (!readLayerEffectVisible(candidate)) {
          continue;
        }

        const blendMode = normalizeIdentifier(readFirstValue([candidate], ["mode", "blendMode", "Md  "]));
        if (blendMode && blendMode !== "normal" && blendMode !== "norm") {
          continue;
        }

        const color = readFirstColor([candidate, candidate.solidFill, candidate.solidFillContent], [
          "color",
          "Clr ",
          "fillColor",
          "solidFill.color",
          "solidFillContent.color"
        ]);
        if (!color) {
          continue;
        }

        const opacity = normalizeOpacity(readFirstNumeric([candidate], [
          "opacity",
          "Opct",
          "fillOpacity",
          "solidFillOpacity"
        ]));

        return {
          color,
          opacity: opacity === null ? color.alpha : opacity,
          blendMode: blendMode || null,
          source: "layerEffectSolidFill"
        };
      }

      return null;
    }


    function pushTextFillColorDiagnostic(layer, sourcePath, descriptor, visualStyle, warnings) {
      if (!descriptor || !visualStyle || !Array.isArray(visualStyle.missingFields) || !visualStyle.missingFields.includes("fillColor")) {
        return;
      }

      warnings.push({
        code: "TEXT_FILL_COLOR_DESCRIPTOR_DIAGNOSTIC",
        severity: "Info",
        message: "Text fillColor 缺失。诊断信息记录了 UXP descriptor 中可能相关的颜色字段路径，用于补充解析规则。",
        sourcePath,
        layerId: readRawLayerId(layer),
        details: buildTextFillColorDescriptorSummary(descriptor)
      });
    }


    function buildTextFillColorDescriptorSummary(descriptor) {
      const ranges = normalizeArray(descriptor?.textStyleRange || descriptor?.textStyleRanges);
      const paragraphRanges = normalizeArray(descriptor?.paragraphStyleRange || descriptor?.paragraphStyleRanges);
      return {
        descriptorKeys: sortedObjectKeys(descriptor),
        textStyleKeys: sortedObjectKeys(descriptor?.textStyle),
        layerEffectsKeys: sortedObjectKeys(descriptor?.layerEffects),
        strokeStyleKeys: sortedObjectKeys(descriptor?.strokeStyle),
        textStyleRangeCount: ranges.length,
        textStyleRangeSamples: ranges.slice(0, 5).map((range) => ({
          keys: sortedObjectKeys(range),
          textStyleKeys: sortedObjectKeys(range?.textStyle),
          characterStyleKeys: sortedObjectKeys(range?.characterStyle),
          styleSheetDataKeys: sortedObjectKeys(range?.textStyle?.styleSheetData || range?.styleSheetData),
          colors: findDescriptorColorCandidates(range, 12),
          rawColorValues: findDescriptorRawColorValues(range, 12)
        })),
        paragraphStyleRangeCount: paragraphRanges.length,
        paragraphStyleRangeSamples: paragraphRanges.slice(0, 3).map((range) => ({
          keys: sortedObjectKeys(range),
          paragraphStyleKeys: sortedObjectKeys(range?.paragraphStyle)
        })),
        rootColors: findDescriptorColorCandidates(descriptor, 40),
        rootRawColorValues: findDescriptorRawColorValues(descriptor, 40)
      };
    }


    function sortedObjectKeys(value) {
      if (!value || typeof value !== "object" || Array.isArray(value)) {
        return [];
      }

      return Object.keys(value).sort();
    }


    function findDescriptorColorCandidates(root, limit) {
      const results = [];
      const seen = new Set();
      walkDescriptorForColors(root, "", 0, results, seen, limit);
      return results;
    }


    function findDescriptorRawColorValues(root, limit) {
      const results = [];
      const seen = new Set();
      walkDescriptorForRawColors(root, "", 0, results, seen, limit);
      return results;
    }


    function walkDescriptorForRawColors(value, path, depth, results, seen, limit) {
      if (results.length >= limit || depth > 8 || !value || typeof value !== "object") {
        return;
      }
      if (seen.has(value)) {
        return;
      }
      seen.add(value);

      if (Array.isArray(value)) {
        for (let i = 0; i < value.length; i += 1) {
          walkDescriptorForRawColors(value[i], `${path}[${i}]`, depth + 1, results, seen, limit);
          if (results.length >= limit) {
            return;
          }
        }
        return;
      }

      for (const key of Object.keys(value)) {
        const child = value[key];
        const childPath = path ? `${path}.${key}` : key;
        const normalizedKey = normalizeIdentifier(key);
        if (normalizedKey.includes("color") || normalizedKey === "clr") {
          results.push({
            path: childPath,
            parsed: Boolean(readColorValue(child)),
            summary: summarizeDescriptorValue(child, 3)
          });
          if (results.length >= limit) {
            return;
          }
        }

        if (
          normalizedKey.includes("style") ||
          normalizedKey.includes("range") ||
          normalizedKey.includes("effect") ||
          normalizedKey.includes("sheet") ||
          normalizedKey.includes("color") ||
          normalizedKey.includes("clr") ||
          normalizedKey.includes("fill")
        ) {
          walkDescriptorForRawColors(child, childPath, depth + 1, results, seen, limit);
          if (results.length >= limit) {
            return;
          }
        }
      }
    }


    function summarizeDescriptorValue(value, depth) {
      if (depth <= 0 || value === null || value === undefined || typeof value !== "object") {
        return value;
      }
      if (Array.isArray(value)) {
        return value.slice(0, 4).map((item) => summarizeDescriptorValue(item, depth - 1));
      }

      const summary = {};
      for (const key of Object.keys(value).sort().slice(0, 24)) {
        const child = value[key];
        if (child && typeof child === "object") {
          summary[key] = summarizeDescriptorValue(child, depth - 1);
        } else {
          summary[key] = child;
        }
      }
      return summary;
    }


    function walkDescriptorForColors(value, path, depth, results, seen, limit) {
      if (results.length >= limit || depth > 8 || !value || typeof value !== "object") {
        return;
      }
      if (seen.has(value)) {
        return;
      }
      seen.add(value);

      const color = readColorValue(value);
      if (color && path) {
        results.push({
          path,
          hex: color.hex,
          alpha: color.alpha
        });
        if (results.length >= limit) {
          return;
        }
      }

      if (Array.isArray(value)) {
        for (let i = 0; i < value.length; i += 1) {
          walkDescriptorForColors(value[i], `${path}[${i}]`, depth + 1, results, seen, limit);
          if (results.length >= limit) {
            return;
          }
        }
        return;
      }

      for (const key of Object.keys(value)) {
        const child = value[key];
        const childPath = path ? `${path}.${key}` : key;
        const normalizedKey = normalizeIdentifier(key);
        if (
          normalizedKey.includes("color") ||
          normalizedKey.includes("clr") ||
          normalizedKey.includes("fill") ||
          normalizedKey.includes("style") ||
          normalizedKey.includes("range") ||
          normalizedKey.includes("effect") ||
          normalizedKey.includes("sheet") ||
          canContainRgbColor(child)
        ) {
          walkDescriptorForColors(child, childPath, depth + 1, results, seen, limit);
          if (results.length >= limit) {
            return;
          }
        }
      }
    }


    function canContainRgbColor(value) {
      if (!value || typeof value !== "object") {
        return false;
      }

      const owner = value.color && typeof value.color === "object" ? value.color : value;
      return (
        hasAnyOwnProperty(owner, ["red", "r", "Rd  ", "Rd", "R", "redFloat", "redF", "Rf"]) &&
        hasAnyOwnProperty(owner, ["green", "grain", "g", "Grn ", "Grn", "G", "greenFloat", "grainFloat", "greenF", "gf"]) &&
        hasAnyOwnProperty(owner, ["blue", "b", "Bl  ", "Bl", "B", "blueFloat", "blueF", "bf"])
      );
    }


    function hasAnyOwnProperty(owner, keys) {
      if (!owner || typeof owner !== "object") {
        return false;
      }

      return keys.some((key) => Object.prototype.hasOwnProperty.call(owner, key));
    }


    function readLayerEffectVisible(candidate) {
      const enabled = readFirstBoolean([candidate], [
        "enabled",
        "present",
        "showInDialog"
      ]);
      if (enabled === false) {
        return false;
      }

      const opacity = normalizeOpacity(readFirstNumeric([candidate], [
        "opacity",
        "Opct",
        "fillOpacity",
        "solidFillOpacity"
      ]));
      if (opacity !== null && opacity <= 0) {
        return false;
      }

      return true;
    }


    function normalizeOpacity(value) {
      if (!Number.isFinite(value)) {
        return null;
      }

      return roundStyleNumber(value > 1 ? value / 100 : value);
    }


    function toHexByte(value) {
      const clamped = Math.max(0, Math.min(255, Math.round(value)));
      return clamped.toString(16).padStart(2, "0").toUpperCase();
    }


    function readTextStrokeStyle(styleCandidates, descriptor) {
      const strokeCandidates = [
        descriptor?.strokeStyle,
        descriptor?.layerEffects?.frameFX,
        descriptor?.frameFX,
        descriptor?.textStyle?.strokeStyle,
        descriptor?.textStyle?.stroke,
        ...styleCandidates.map((style) => style?.strokeStyle),
        ...styleCandidates.map((style) => style?.stroke),
        ...styleCandidates.map((style) => style?.frameFX)
      ].filter(Boolean);

      for (const candidate of strokeCandidates) {
        if (!candidate || typeof candidate !== "object" || Object.keys(candidate).length === 0) {
          continue;
        }

        const enabled = readStrokeEnabled(candidate);
        const width = readFirstNumeric([candidate], [
          "strokeStyleLineWidth",
          "size",
          "width",
          "lineWidth",
          "strokeWidth"
        ]);
        const color = readFirstColor([candidate, candidate.strokeStyleContent], [
          "color",
          "Clr ",
          "strokeStyleContent.color"
        ]);
        const bucket = bucketStrokeWidth(enabled ? width : 0);
        const position = normalizeStrokePosition(readFirstValue([candidate], [
          "strokeStyleLineAlignment",
          "position",
          "alignment"
        ]));

        return {
          found: true,
          enabled,
          width: width === null ? 0 : roundStyleNumber(width),
          color,
          bucket,
          position,
          source: "psdStrokeStyle",
          widthMissing: enabled && width === null,
          colorMissing: enabled && !color
        };
      }

      return { found: false };
    }


    function hasLayerEffectHint(layer) {
      return Boolean(layer && (layer.effects || layer.layerEffects));
    }


    function readStrokeEnabled(candidate) {
      const enabled = readFirstBoolean([candidate], [
        "strokeEnabled",
        "enabled",
        "present",
        "showInDialog"
      ]);
      if (enabled !== null) {
        return enabled;
      }

      const width = readFirstNumeric([candidate], [
        "strokeStyleLineWidth",
        "size",
        "width",
        "lineWidth",
        "strokeWidth"
      ]);
      return width !== null && width > 0;
    }


    function bucketStrokeWidth(width) {
      if (!Number.isFinite(width) || width <= 0) {
        return "none";
      }
      if (width <= 1.5) {
        return "thin";
      }
      if (width <= 3) {
        return "normal";
      }
      if (width <= 5) {
        return "thick";
      }
      return "heavy";
    }


    function normalizeStrokePosition(value) {
      const normalized = normalizeIdentifier(value);
      if (!normalized) {
        return null;
      }
      if (normalized.includes("outside") || normalized.includes("out")) {
        return "outside";
      }
      if (normalized.includes("inside") || normalized.includes("in")) {
        return "inside";
      }
      if (normalized.includes("center")) {
        return "center";
      }
      return normalized;
    }


    function normalizeFontWeight(value, syntheticBold) {
      const normalized = normalizeIdentifier(value);
      if (syntheticBold === true) {
        return normalized.includes("bold") || normalized.includes("heavy") || normalized.includes("black")
          ? "bold"
          : "synthetic_bold";
      }
      if (!normalized) {
        return "regular";
      }
      if (normalized.includes("bold") || normalized.includes("heavy") || normalized.includes("black")) {
        return "bold";
      }
      if (normalized.includes("medium")) {
        return "medium";
      }
      if (normalized.includes("light") || normalized.includes("thin")) {
        return "light";
      }
      return "regular";
    }


    function normalizeTextAlignment(value) {
      const normalized = normalizeIdentifier(value);
      if (!normalized) {
        return null;
      }
      if (normalized.includes("left")) {
        return "left";
      }
      if (normalized.includes("right")) {
        return "right";
      }
      if (normalized.includes("center")) {
        return "center";
      }
      if (normalized.includes("justify")) {
        return "justified";
      }
      return normalized;
    }


    function buildTextVisualStyleKey(features) {
      const font = normalizeIdentifier(features.fontIdentity || features.fontFamily || "unknown-font");
      const weight = normalizeIdentifier(features.weight || "regular");
      const strokeBucket = normalizeIdentifier(features.strokeBucket || "none");
      const strokeColor = features.strokeEnabled !== false && strokeBucket !== "none"
        ? normalizeIdentifier(features.strokeColor || "unknown")
        : "none";
      const source = [
        font,
        weight,
        strokeBucket,
        strokeColor
      ].join("|");
      return `psdTextStyle:v1:${font}:${weight}:${strokeBucket}-${strokeColor}:${hashStyleKey(source)}`;
    }


    function resolveTextVisualStyleStatus(features, missingFields, rangeCount) {
      if (!features || !features.fontIdentity) {
        return missingFields.length > 0 ? "Partial" : "Unavailable";
      }
      if (rangeCount > 1) {
        return "Mixed";
      }
      return missingFields.length > 0 ? "Partial" : "Resolved";
    }


    function normalizeIdentifier(value) {
      const text = safeString(unwrapDescriptorScalar(value), "").trim().toLowerCase();
      if (!text) {
        return "";
      }
      return text
        .replace(/^#/, "")
        .replace(/[^a-z0-9]+/g, "_")
        .replace(/^_+|_+$/g, "") || "";
    }


    function unwrapDescriptorScalar(value) {
      if (!value || typeof value !== "object") {
        return value;
      }

      const candidates = ["_value", "value", "name", "_enum", "_obj"];
      for (const key of candidates) {
        const candidate = value[key];
        if (candidate !== null && candidate !== undefined && typeof candidate !== "object") {
          return candidate;
        }
      }

      return value;
    }


    function hashStyleKey(value) {
      let hash = 2166136261;
      for (let i = 0; i < value.length; i += 1) {
        hash ^= value.charCodeAt(i);
        hash = Math.imul(hash, 16777619);
      }
      return (hash >>> 0).toString(16).padStart(8, "0");
    }


    function roundStyleNumber(value) {
      return typeof round2 === "function" ? round2(value) : Math.round(value * 100) / 100;
    }


    function readTextStyleRangeFontSize(ranges, transformScaleY) {
      if (!Array.isArray(ranges)) {
        return null;
      }

      for (const range of ranges) {
        const candidates = [
          range?.textStyle?.size,
          range?.textStyle?.fontSize,
          range?.characterStyle?.size,
          range?.characterStyle?.fontSize
        ];
        for (const candidate of candidates) {
          const fontSize = normalizeFontSize(candidate, transformScaleY);
          if (fontSize !== null) {
            return fontSize;
          }
        }
      }

      return null;
    }


    function normalizeFontSize(value, transformScaleY) {
      const raw = readNumericValue(value);
      if (raw === null || Number.isNaN(raw) || !Number.isFinite(raw) || raw <= 0) {
        return null;
      }

      const scaleY = Number.isFinite(transformScaleY) && transformScaleY > 0 ? transformScaleY : 1;
      const effectiveSize = raw * scaleY;
      return typeof round2 === "function" ? round2(effectiveSize) : Math.round(effectiveSize * 100) / 100;
    }


    function readTextTransformScaleY(layer, textItem, descriptor) {
      const candidates = [
        safeReadPath(layer, "transform"),
        safeReadPath(textItem, "transform"),
        safeReadPath(layer, "textTransform"),
        safeReadPath(textItem, "textTransform"),
        safeReadPath(textItem, "textStyle.transform"),
        safeReadPath(textItem, "characterStyle.transform"),
        safeReadPath(descriptor, "transform"),
        safeReadPath(descriptor, "textStyle.transform")
      ];

      for (const candidate of candidates) {
        const scaleY = readTransformScaleY(candidate);
        if (scaleY !== null) {
          return scaleY;
        }
      }

      return 1;
    }


    function readTextTransformScaleX(layer, textItem, descriptor) {
      const candidates = [
        safeReadPath(layer, "transform"),
        safeReadPath(textItem, "transform"),
        safeReadPath(layer, "textTransform"),
        safeReadPath(textItem, "textTransform"),
        safeReadPath(textItem, "textStyle.transform"),
        safeReadPath(textItem, "characterStyle.transform"),
        safeReadPath(descriptor, "transform"),
        safeReadPath(descriptor, "textStyle.transform")
      ];

      for (const candidate of candidates) {
        const scaleX = readTransformScaleX(candidate);
        if (scaleX !== null) {
          return scaleX;
        }
      }

      return 1;
    }


    function safeReadPath(owner, path) {
      try {
        if (!owner || !path) {
          return null;
        }

        return path.split(".").reduce((value, key) => (value == null ? null : value[key]), owner);
      } catch (err) {
        return null;
      }
    }


    function readTransformScaleY(transform) {
      if (!transform) {
        return null;
      }

      if (Array.isArray(transform) && transform.length >= 4) {
        return normalizeScaleY(readNumericValue(transform[2]), readNumericValue(transform[3]));
      }

      if (typeof transform !== "object") {
        return null;
      }

      const directScale = readFirstNumericProperty(transform, ["scaleY", "yScale", "verticalScale"]);
      if (directScale !== null && Number.isFinite(directScale) && directScale > 0) {
        return directScale;
      }

      const objectKeyPairs = [
        ["c", "d"],
        ["yx", "yy"],
        ["m21", "m22"],
        ["skewY", "scaleY"]
      ];
      for (const [xKey, yKey] of objectKeyPairs) {
        if (!Object.prototype.hasOwnProperty.call(transform, xKey) || !Object.prototype.hasOwnProperty.call(transform, yKey)) {
          continue;
        }

        const scaleY = normalizeScaleY(readNumericValue(transform[xKey]), readNumericValue(transform[yKey]));
        if (scaleY !== null) {
          return scaleY;
        }
      }

      return null;
    }


    function readTransformScaleX(transform) {
      if (!transform) {
        return null;
      }

      if (Array.isArray(transform) && transform.length >= 2) {
        return normalizeScaleX(readNumericValue(transform[0]), readNumericValue(transform[1]));
      }

      if (typeof transform !== "object") {
        return null;
      }

      const directScale = readFirstNumericProperty(transform, ["scaleX", "xScale", "horizontalScale"]);
      if (directScale !== null && Number.isFinite(directScale) && directScale > 0) {
        return directScale;
      }

      const objectKeyPairs = [
        ["a", "b"],
        ["xx", "xy"],
        ["m11", "m12"],
        ["scaleX", "skewX"]
      ];
      for (const [xKey, yKey] of objectKeyPairs) {
        if (!Object.prototype.hasOwnProperty.call(transform, xKey) || !Object.prototype.hasOwnProperty.call(transform, yKey)) {
          continue;
        }

        const scaleX = normalizeScaleX(readNumericValue(transform[xKey]), readNumericValue(transform[yKey]));
        if (scaleX !== null) {
          return scaleX;
        }
      }

      return null;
    }


    function readFirstNumericProperty(owner, keys) {
      for (const key of keys) {
        if (!Object.prototype.hasOwnProperty.call(owner, key)) {
          continue;
        }

        const value = readNumericValue(owner[key]);
        if (value !== null) {
          return value;
        }
      }

      return null;
    }


    function normalizeScaleY(xComponent, yComponent) {
      const x = Number.isFinite(xComponent) ? xComponent : 0;
      const y = Number.isFinite(yComponent) ? yComponent : null;
      if (y === null) {
        return null;
      }

      const scaleY = Math.sqrt(x * x + y * y);
      return Number.isFinite(scaleY) && scaleY > 0 ? scaleY : null;
    }


    function normalizeScaleX(xComponent, yComponent) {
      const x = Number.isFinite(xComponent) ? xComponent : null;
      const y = Number.isFinite(yComponent) ? yComponent : 0;
      if (x === null) {
        return null;
      }

      const scaleX = Math.sqrt(x * x + y * y);
      return Number.isFinite(scaleX) && scaleX > 0 ? scaleX : null;
    }


    function readNumericValue(value) {
      if (value === null || value === undefined) {
        return null;
      }

      if (typeof value === "number") {
        return value;
      }

      if (typeof value === "string") {
        return typeof toNumber === "function" ? toNumber(value, null) : Number.parseFloat(value);
      }

      if (typeof value === "object") {
        const keys = ["_value", "value", "amount", "pixels", "px"];
        for (const key of keys) {
          if (Object.prototype.hasOwnProperty.call(value, key)) {
            const nested = readNumericValue(value[key]);
            if (nested !== null) {
              return nested;
            }
          }
        }
      }

      return null;
    }


    function appendLayerUncertaintyWarnings(layer, sourcePath, warnings) {
      const kind = safeString(layer.kind || layer.type || layer.layerKind, "").toLowerCase();

      if (kind.includes("smart")) {
        warnings.push({
          code: "SMART_OBJECT_UNCERTAIN",
          severity: "Warning",
          message: "Smart object contents are not opened or inspected for native text. Font/style data inside it is unavailable; raster pixels cannot establish original font identity.",
          sourcePath
        });
      }

      if (kind.includes("pixel") || kind.includes("raster") || kind.includes("shape") || kind.includes("solid")) {
        warnings.push({ code: "NON_NATIVE_TEXT_SOURCE", severity: "Info",
          message: "Pixel/shape data is not native editable text. Rasterized or outlined lettering cannot provide a reliable font identity.", sourcePath });
      }

      if (layer.mask || layer.vectorMask) {
        warnings.push({
          code: "LAYER_MASK_UNCERTAIN",
          severity: "Warning",
          message: "Layer mask or vector mask presence was detected, but mask geometry is not serialized in the first pass.",
          sourcePath
        });
      }

      if (layer.effects || layer.layerEffects) {
        warnings.push({
          code: "LAYER_EFFECTS_UNCERTAIN",
          severity: "Warning",
          message: "Photoshop layer effects were detected, but Unity-ready effect data is not serialized in the first pass.",
          sourcePath
        });
      }
    }


    function isLayerGroup(layer) {
      const kind = safeString(layer.kind || layer.type || layer.layerKind, "").toLowerCase();
      return kind.includes("group") || kind.includes("section");
    }

    function hasChildLayers(layer) {
      try {
        if (!layer || !layer.layers) {
          return false;
        }
        if (typeof layer.layers.length === "number") {
          return true;
        }
        return typeof layer.layers[Symbol.iterator] === "function";
      } catch (err) {
        return false;
      }
    }

    function isArtboardLayer(layer) {
      if (!layer) {
        return false;
      }
      if (layer.isArtboard === true || layer.artboardEnabled === true) {
        return true;
      }
      if (layer.artboard && typeof layer.artboard === "object") {
        return true;
      }

      const kind = safeString(layer.kind || layer.type || layer.layerKind, "").toLowerCase();
      if (kind.includes("artboard")) {
        return true;
      }

      const name = safeString(layer.name, "").trim();
      return (isLayerGroup(layer) || hasChildLayers(layer)) && /^(artboard|画板)\s*\d*$/i.test(name);
    }

    return { readLayerBounds, readLayerExportBounds, readLayerId, readLayerVisible, detectLayerType, mapNodeType, readTextInfo, readTextInfoSync, appendLayerUncertaintyWarnings, isLayerGroup, isArtboardLayer };
  }

  global.BurnerPsdToBuiLayerReader = { createLayerReader };
})(globalThis);
