/* global globalThis */

(function registerSourceInfo(global) {
  function createSourceInfo(options) {
    const { formats, formatText, t, formatError, toNumber, sha256Hex, getSchemaPathHint, isSha256Hex, isNonEmptyString } = options;
    const ADAPTER_NAME = options.adapterName;
    const ADAPTER_VERSION = options.adapterVersion;
    const NAMING_RULES_CONFIG_PATH = options.namingRulesConfigPath;

    async function buildSourceInfo(doc, psdFile, warnings, namingRules) {
      const exportedAt = new Date().toISOString();
      const namingRulesConfigPath = namingRules?.configPath || NAMING_RULES_CONFIG_PATH;
      const psdName = psdFile?.name || doc.title || doc.name || "untitled.psd";
      const source = {
        type: "psd",
        psdName,
        psdRelativePath: psdName,
        psdSha256: null,
        psdSize: null,
        psdModifiedAt: null,
        adapter: ADAPTER_NAME,
        adapterVersion: ADAPTER_VERSION,
        exportedAt,
        uxp: {
          host: "Photoshop",
          plugin: ADAPTER_NAME,
          schemaPathHint: getSchemaPathHint(),
          schemaReadStatus: "not-read-from-uxp-sandbox",
          namingRules: {
            configPath: namingRulesConfigPath,
            version: namingRules?.version || "unknown",
            loadStatus: namingRules?.loadStatus || "unknown"
          }
        }
      };

      if (!psdFile) {
        warnings.push({
          code: "PSD_FILE_ENTRY_NOT_SELECTED",
          severity: "Blocking",
          message: "The active document cannot be exported until UXP authorizes the current PSD file entry, so psdSha256, psdSize and psdModifiedAt can be recorded."
        });
        return source;
      }

      try {
        const metadata = await readEntryMetadata(psdFile);
        source.psdSize = metadata.size;
        source.psdModifiedAt = metadata.modifiedAt;
      } catch (err) {
        warnings.push({
          code: "PSD_METADATA_UNAVAILABLE",
          severity: "Blocking",
          message: `UXP could not read PSD metadata: ${formatError(err)}`
        });
      }

      try {
        const bytes = await psdFile.read({ format: formats.binary });
        source.psdSha256 = await sha256Hex(bytes);
      } catch (err) {
        warnings.push({
          code: "PSD_SHA256_UNAVAILABLE",
          severity: "Blocking",
          message: `UXP could not read PSD bytes for sha256: ${formatError(err)}`
        });
      }

      return source;
    }


    function assertSourceMetadataComplete(source) {
      const missing = [];
      if (!isSha256Hex(source?.psdSha256)) {
        missing.push("psdSha256");
      }
      if (!Number.isInteger(source?.psdSize) || source.psdSize < 0) {
        missing.push("psdSize");
      }
      if (!isNonEmptyString(source?.psdModifiedAt)) {
        missing.push("psdModifiedAt");
      }

      if (missing.length > 0) {
        throw new Error(formatText(t("sourceMetadataRequired"), { fields: missing.join(", ") }));
      }
    }


    async function readEntryMetadata(entry) {
      let metadata = {};
      if (entry.getMetadata) {
        metadata = await entry.getMetadata();
      }

      const size = toNumber(metadata.size ?? entry.size, null);
      const modified = metadata.dateModified || metadata.modified || entry.dateModified || entry.modified;
      return {
        size,
        modifiedAt: modified ? new Date(modified).toISOString() : null
      };
    }

    return { buildSourceInfo, assertSourceMetadataComplete, readEntryMetadata };
  }

  global.BurnerPsdToBuiSourceInfo = { createSourceInfo };
})(globalThis);
