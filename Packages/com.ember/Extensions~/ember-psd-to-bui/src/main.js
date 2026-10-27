/* global document, crypto */

const photoshop = require("photoshop");
const uxp = require("uxp");

const { app, action, core, constants, imaging } = photoshop;
const { entrypoints, versions: uxpVersions } = uxp;
const fs = uxp.storage.localFileSystem;
const formats = uxp.storage.formats;

const ADAPTER_NAME = "ember-psd-to-bui";
const ADAPTER_VERSION = uxpVersions.plugin;
const STORAGE_LANG_KEY = "ember-psd-to-bui.lang";
const STORAGE_EXPORT_SETTINGS_KEY = "ember-psd-to-bui.export-settings";
const NAMING_RULES_CONFIG_PATH = "config/psd-naming-rules.json";
const I18N_CONFIG_PATH = "config/i18n.json";
const EXPORT_SETTINGS_CONFIG_PATH = "config/export-settings.json";
const BUI_PREVIEW_NODE_LIMIT = 160;
const PREVIEW_SLICE_LIMIT = 80;
const PREVIEW_STRUCTURE_LIMIT = 120;
const LOG_ENTRY_LIMIT = 120;
const ENTRY_LOOKUP_TIMEOUT_MS = 2500;
const FOLDER_PICKER_INITIAL_TIMEOUT_MS = 1000;

const common = globalThis.BurnerPsdToBuiCommon;
if (!common) {
  throw new Error("BurnerPsdToBuiCommon is not loaded.");
}

const {
  formatText,
  withTimeout,
  capitalizeAscii,
  arrayFrom,
  maybePromise,
  hashText,
  toNumber,
  round2,
  stripExtension,
  sanitizeFileBase,
  slugify,
  makePageClassName,
  safeString,
  formatError,
  escapeHtml,
  toUint8Array,
  bytesToHex,
  sha256HexFallback
} = common;

const configLoaderFactory = globalThis.BurnerPsdToBuiConfigLoader;
if (!configLoaderFactory || typeof configLoaderFactory.createConfigLoader !== "function") {
  throw new Error("BurnerPsdToBuiConfigLoader is not loaded.");
}

const configLoader = configLoaderFactory.createConfigLoader({
  fs,
  formats,
  safeString
});

const warningToolsFactory = globalThis.BurnerPsdToBuiWarningTools;
if (!warningToolsFactory || typeof warningToolsFactory.createWarningTools !== "function") {
  throw new Error("BurnerPsdToBuiWarningTools is not loaded.");
}

const warningTools = warningToolsFactory.createWarningTools({
  t,
  getI18nTable: () => i18nTable,
  getCurrentLang: () => currentLang,
  formatText,
  capitalizeAscii,
  safeString,
  arrayFrom
});

const {
  formatPrioritySummary: formatWarningPrioritySummary,
  getPriority: getWarningPriority,
  getPriorityLabel: getWarningPriorityLabel,
  getLocalizedMessage: getLocalizedWarningMessage,
  sortForDisplay: sortWarningsForDisplay,
  groupedForDisplay: groupedWarningsForDisplay,
  formatForCopy: formatWarningForCopy
} = warningTools;

const namingParserFactory = globalThis.BurnerPsdToBuiNamingParser;
if (!namingParserFactory || typeof namingParserFactory.createNamingParser !== "function") {
  throw new Error("BurnerPsdToBuiNamingParser is not loaded.");
}

const {
  parsePsdNaming,
  parseFlagToken,
  getNamingRuleIndex,
  getIgnorePrefixes,
  isIgnoredByNaming
} = namingParserFactory.createNamingParser({ safeString, arrayFrom });

const namingSemanticsFactory = globalThis.BurnerPsdToBuiNamingSemantics;
if (!namingSemanticsFactory || typeof namingSemanticsFactory.createNamingSemantics !== "function") {
  throw new Error("BurnerPsdToBuiNamingSemantics is not loaded.");
}

const {
  pushIgnoreWarning,
  resolveTargetRule,
  resolveNodeSemantics,
  resolvePrefabRefNaming,
  pushNamingParseWarnings,
  pushTargetRootNamingWarnings,
  applyNamingOption,
  resolveSlicePolicy,
  buildNamingSource,
  shouldUseNoTargetFallback,
  getDefaultVariant,
  getConfiguredDefaultResourceType
} = namingSemanticsFactory.createNamingSemantics({
  arrayFrom,
  toNumber,
  getNamingRuleIndex,
  namingRulesConfigPath: NAMING_RULES_CONFIG_PATH
});

const buiRectFactory = globalThis.BurnerPsdToBuiRect;
if (!buiRectFactory || typeof buiRectFactory.createBuiRect !== "function") {
  throw new Error("BurnerPsdToBuiRect is not loaded.");
}

const { parseBounds, makeRect, snapBounds, makeRectFromSnappedBounds } = buiRectFactory.createBuiRect({ toNumber, round2 });

const buiModelBuilderFactory = globalThis.BurnerPsdToBuiModelBuilder;
if (!buiModelBuilderFactory || typeof buiModelBuilderFactory.createBuiModelBuilder !== "function") {
  throw new Error("BurnerPsdToBuiModelBuilder is not loaded.");
}

const {
  createSyntheticTargetRoot,
  makeTargetRecord,
  finalizeTargets,
  collectNodeIdsForRoot,
  makeUniqueTargetId,
  countRootNodes,
  makeUniqueNodeId,
  nodeIdPrefix
} = buiModelBuilderFactory.createBuiModelBuilder({
  arrayFrom,
  makePageClassName,
  slugify,
  makeRect,
  getDefaultVariant,
  sanitizeId,
  namingRulesConfigPath: NAMING_RULES_CONFIG_PATH
});

const buiChecklistFactory = globalThis.BurnerPsdToBuiChecklist;
if (!buiChecklistFactory || typeof buiChecklistFactory.createBuiChecklist !== "function") {
  throw new Error("BurnerPsdToBuiChecklist is not loaded.");
}

const {
  assertExportableBui,
  validateBuiChecklist,
  validateBuiPreviewChecklist,
  isNonEmptyString,
  isSha256Hex
} = buiChecklistFactory.createBuiChecklist({
  getWarningPriority,
  formatText,
  t
});

const sourceInfoFactory = globalThis.BurnerPsdToBuiSourceInfo;
if (!sourceInfoFactory || typeof sourceInfoFactory.createSourceInfo !== "function") {
  throw new Error("BurnerPsdToBuiSourceInfo is not loaded.");
}

const { buildSourceInfo, assertSourceMetadataComplete } = createSourceInfoTools(getSchemaPathHint);

function createSourceInfoTools(getSchemaPathHint) {
  const { buildSourceInfo, assertSourceMetadataComplete } = sourceInfoFactory.createSourceInfo({
    adapterName: ADAPTER_NAME,
    adapterVersion: ADAPTER_VERSION,
    namingRulesConfigPath: NAMING_RULES_CONFIG_PATH,
    formats,
    formatText,
    t,
    formatError,
    toNumber,
    sha256Hex,
    getSchemaPathHint,
    isSha256Hex,
    isNonEmptyString
  });

  return { buildSourceInfo, assertSourceMetadataComplete };
}

const layerReaderFactory = globalThis.BurnerPsdToBuiLayerReader;
if (!layerReaderFactory || typeof layerReaderFactory.createLayerReader !== "function") {
  throw new Error("BurnerPsdToBuiLayerReader is not loaded.");
}

const {
  readLayerBounds,
  readLayerExportBounds,
  readLayerId,
  readLayerVisible,
  detectLayerType,
  mapNodeType,
  readTextInfo,
  readTextInfoSync,
  appendLayerUncertaintyWarnings,
  isLayerGroup,
  isArtboardLayer
} = layerReaderFactory.createLayerReader({
  maybePromise,
  parseBounds,
  formatError,
  safeString,
  toNumber,
  round2,
  action
});

const { buildCanvasInfo, buildNodes, makePrefabRefOverrideRect, makePrefabOverrideAssetName } = createExportBuilders({ readExportSetting, makeAssetFileName, getResourceNamingKey, log });

function createExportBuilders({ readExportSetting, makeAssetFileName, getResourceNamingKey, log, assertSourceBeforeModal, writeOutput }) {
  const pngExporterFactory = globalThis.BurnerPsdToBuiPngExporter;
  if (!pngExporterFactory || typeof pngExporterFactory.createPngExporter !== "function") {
    throw new Error("BurnerPsdToBuiPngExporter is not loaded.");
  }

  const { tryExportLayerPng, exportTextReferences } = pngExporterFactory.createPngExporter({
    app,
    action,
    core,
    constants,
    imaging,
    fs,
    formats,
    arrayFrom,
    toNumber,
    safeString,
    stripExtension,
    formatText,
    formatError,
    t,
    log,
    readLayerVisible,
    readLayerExportBounds,
    parseBoundsQuiet,
    snapBounds,
    makeAssetFileName,
    readExportSetting,
    sha256Hex, assertSourceBeforeModal, writeOutput,
    resolveOutputEntry: globalThis.BurnerPsdToBuiOutputSnapshot.resolve
  });

  const exportModelBuilderFactory = globalThis.BurnerPsdToBuiExportModelBuilder;
  if (!exportModelBuilderFactory || typeof exportModelBuilderFactory.createExportModelBuilder !== "function") {
    throw new Error("BurnerPsdToBuiExportModelBuilder is not loaded.");
  }

  const {
    buildCanvasInfo,
    buildNodes,
    makePrefabRefOverrideRect,
    makePrefabOverrideAssetName
  } = exportModelBuilderFactory.createExportModelBuilder({
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
  });

  return { buildCanvasInfo, buildNodes, makePrefabRefOverrideRect, makePrefabOverrideAssetName, exportTextReferences };
}

let selectedPsdFile = null;
let selectedOutputFolder = null;
let derivedOutputFolderPath = "";
let outputFolderSource = "";
let initialized = false;
let initPromise = null;
let els = null;
let currentLang = readInitialLanguage();
let i18nTable = null;
let i18nPromise = null;
let exportSettings = null;
let exportSettingsPromise = null;
let prefixController = null;
let namingRules = null;
let namingRulesPromise = null;
let statusKey = "ready";
let statusKind = "";
let refreshTimer = null;
let previewRenderTimer = null;
let previewRenderRevision = 0;
const previewSnapshotRequests = new Map();
let busy = false;
let lastPreviewJson = "";
let lastPreviewWarningsText = "";
let lastPreviewWarnings = [];
let logEntries = [];
let autoOutputFolderPathInFlight = "";
let lastAutoOutputFolderFailure = "";
let lastActiveDocumentKey = "";
let lastDefaultExportBaseName = "";
let lastCompletedExportTargetKey = "";
let collapsedPrefabGroups = {};
let summaryState = {
  titleKey: "summaryWaitingTitle",
  bodyKey: "summaryWaitingBody",
  titleText: "",
  bodyText: "",
  kind: ""
};

registerEntrypoints();

function registerEntrypoints() {
  if (!entrypoints || typeof entrypoints.setup !== "function") {
    initializeWhenReady();
    return;
  }

  try {
    entrypoints.setup({
      panels: {
        "ember-psd-to-bui-panel": {
          create: initializeWhenReady,
          show: initializeWhenReady
        }
      }
    });
  } catch (err) {
    console.error("Failed to register UXP entrypoints.", err);
    initializeWhenReady();
  }
}

function initializeWhenReady() {
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", () => {
      void init();
    }, { once: true });
    return;
  }

  void init();
}

function init() {
  if (!initPromise) {
    initPromise = initPanel().catch((err) => {
      initPromise = null;
      console.error("Failed to initialize ember-psd-to-bui panel.", err);
      throw err;
    });
  }

  return initPromise;
}

async function initPanel() {
  if (initialized) {
    refreshPanelState(false);
    return;
  }

  const pluginVersion = document.getElementById("pluginVersion");
  if (pluginVersion) {
    pluginVersion.textContent = `v${ADAPTER_VERSION}`;
  }

  [i18nTable, exportSettings, namingRules] = await Promise.all([
    getI18n(),
    getExportSettings(),
    getNamingRules()
  ]);

  els = {
    pluginVersion,
    documentTitle: document.getElementById("documentTitle"),
    documentMeta: document.getElementById("documentMeta"),
    documentPath: document.getElementById("documentPath"),
    previewCanvas: document.getElementById("previewCanvas"),
    previewLayerCount: document.getElementById("previewLayerCount"),
    previewWarnings: document.getElementById("previewWarnings"),
    layerPreviewList: document.getElementById("layerPreviewList"),
    exportBaseName: document.getElementById("exportBaseName"),
    sliceFileNamePrefix: document.getElementById("sliceFileNamePrefix"),
    outputFolder: document.getElementById("outputFolder"),
    plannedBuiFile: document.getElementById("plannedBuiFile"),
    plannedAssetsFolder: document.getElementById("plannedAssetsFolder"),
    sliceAssetsExport: document.getElementById("sliceAssetsExport"),
    retainReuse: document.getElementById("retainReuse"),
    exportSummary: document.getElementById("exportSummary"),
    refreshBtn: document.getElementById("refreshBtn"),
    copyPreviewBtn: document.getElementById("copyPreviewBtn"),
    copyWarningsBtn: document.getElementById("copyWarningsBtn"),
    copyLogBtn: document.getElementById("copyLogBtn"),
    clearLogBtn: document.getElementById("clearLogBtn"),
    exportBtn: document.getElementById("exportBtn"),
    langToggleBtn: document.getElementById("langToggleBtn"),
    namingRulesBtn: document.getElementById("namingRulesBtn"),
    namingRulesCloseBtn: document.getElementById("namingRulesCloseBtn"),
    namingPopover: document.getElementById("namingPopover"),
    log: document.getElementById("log")
  };

  const missing = Object.keys(els).filter((key) => !els[key]);
  if (missing.length > 0) {
    throw new Error(`Missing panel DOM elements: ${missing.join(", ")}`);
  }

  els.sliceFileNamePrefix.value = loadSliceFileNamePrefix();
  prefixController = globalThis.BurnerPsdToBuiPrefixPreferences.createController({
    store: globalThis.BurnerPsdToBuiPrefixPreferences.createStore({fs,formats}),
    context: () => ({psdPath:getCurrentPsdPath(getActiveDocument()),folder:selectedOutputFolder,
      buiName:sanitizeFileBase(els.exportBaseName.value)+getBuiExtension(),defaultPrefix:loadSliceFileNamePrefix()}),
    read: getSliceFileNamePrefix,
    write: value => {els.sliceFileNamePrefix.value=value;},
    status: (key,error) => {
      const element=document.getElementById("slicePrefixStatus");
      if(element)element.textContent=formatText(t(key),{error});
    },
    changed: () => {scheduleBuiPreviewRender();updateExportAvailability();}
  });
  const onPrefixEdited = () => {
    void prefixController.edited();
    scheduleBuiPreviewRender();
    updateExportAvailability();
  };
  els.sliceFileNamePrefix.addEventListener("input", onPrefixEdited);
  els.sliceFileNamePrefix.addEventListener("change", onPrefixEdited);

  els.refreshBtn.addEventListener("click", () => {
    refreshPanelState(true);
  });
  els.exportBtn.addEventListener("click", exportCurrentDocument);
  els.copyPreviewBtn.addEventListener("click", copyPreviewJson);
  els.copyWarningsBtn.addEventListener("click", copyWarnings);
  els.copyLogBtn.addEventListener("click", copyLog);
  els.clearLogBtn.addEventListener("click", clearLog);
  els.layerPreviewList.addEventListener("click", togglePrefabGroup);
  els.previewWarnings.addEventListener("click", (evt) => copyRowText(evt, "warningLineCopied"));
  els.log.addEventListener("click", (evt) => copyRowText(evt, "logLineCopied"));
  els.exportBaseName.addEventListener("input", () => {
    void prefixController.update();
    renderBuiPreview();
    updateExportAvailability();
  });
  ["change", "input", "click"].forEach((eventName) => {
    els.sliceAssetsExport.addEventListener(eventName, handleAssetsSlicingChange);
  });
  els.langToggleBtn.addEventListener("click", () => applyLanguage(currentLang === "zh" ? "en" : "zh"));
  els.namingRulesBtn.addEventListener("click", toggleNamingPopover);
  els.namingRulesCloseBtn.addEventListener("click", hideNamingPopover);

  document.getElementById("bridgeToggleBtn").addEventListener("click", () => {
    void aiBridgeHost.toggle().catch(error => renderBridgeStatus({state: "error", error: error.message}));
  });
  initialized = true;
  applyLanguage(currentLang);
  refreshPanelState(false);
  setSummaryFromKeys("summaryWaitingTitle", "summaryWaitingBody");

  window.addEventListener("focus", () => refreshPanelState(false));
  document.addEventListener("visibilitychange", () => refreshPanelState(false));
}

function isSkipAssetsExportEnabled() {
  return !isAssetsSlicingEnabled();
}

function isAssetsSlicingEnabled() {
  const control = els?.sliceAssetsExport;
  if (!control) {
    return true;
  }
  if (typeof control.checked === "boolean") {
    return control.checked;
  }
  const ariaChecked = control.getAttribute?.("aria-checked");
  if (ariaChecked === "true") {
    return true;
  }
  const checkedAttribute = control.getAttribute?.("checked");
  return checkedAttribute === null || checkedAttribute === "" || checkedAttribute === "checked" || checkedAttribute === "true";
}

function handleAssetsSlicingChange() {
  updateExportAvailability();
  scheduleBuiPreviewRender();
}

function scheduleBuiPreviewRender() {
  previewRenderRevision += 1;
  if (previewRenderTimer !== null) {
    clearTimeout(previewRenderTimer);
  }
  previewRenderTimer = setTimeout(() => {
    previewRenderTimer = null;
    void renderBuiPreviewNow();
  }, 0);
}

function log(message) {
  const stamp = new Date().toLocaleTimeString();
  if (!els || !els.log) {
    return;
  }
  logEntries.unshift(`[${stamp}] ${message}`);
  if (logEntries.length > LOG_ENTRY_LIMIT) {
    logEntries = logEntries.slice(0, LOG_ENTRY_LIMIT);
  }
  renderLogEntries();
}

function setBusy(isBusy) {
  busy = Boolean(isBusy);
  els.refreshBtn.disabled = isBusy;
  els.sliceFileNamePrefix.disabled = isBusy;
  els.copyPreviewBtn.disabled = isBusy || !lastPreviewJson;
  els.copyWarningsBtn.disabled = !lastPreviewWarningsText;
  els.copyLogBtn.disabled = logEntries.length === 0;
  els.clearLogBtn.disabled = logEntries.length === 0;
  updateExportAvailability();
}

function refreshPanelState(shouldLog) {
  if (busy) {
    if (shouldLog) {
      log(t("refreshSkippedDuringExport"));
    }
    return;
  }

  refreshDocumentState();
  void syncOutputFolderFromActiveDocument().then(() => prefixController?.update());
  void prefixController?.update();
  renderBuiPreview();
  updateExportAvailability();
  if (shouldLog) {
    log(t("documentRefreshed"));
  }
}

function readInitialLanguage() {
  try {
    const saved = localStorage.getItem(STORAGE_LANG_KEY);
    return saved === "en" ? "en" : "zh";
  } catch (err) {
    return "zh";
  }
}

function applyLanguage(lang) {
  currentLang = lang === "en" ? "en" : "zh";

  try {
    localStorage.setItem(STORAGE_LANG_KEY, currentLang);
  } catch (err) {
    // localStorage may be unavailable in some UXP debug states.
  }

  document.querySelectorAll("[data-i18n]").forEach((node) => {
    const key = node.getAttribute("data-i18n");
    node.textContent = t(key);
  });

  document.querySelectorAll("[data-i18n-placeholder]").forEach((node) => {
    const key = node.getAttribute("data-i18n-placeholder");
    node.setAttribute("placeholder", t(key));
  });

  els.langToggleBtn.textContent = currentLang === "zh" ? t("langSwitchTargetEn") : t("langSwitchTargetZh");
  els.langToggleBtn.title = currentLang === "zh" ? t("langSwitchToEnglish") : t("langSwitchToChinese");
  renderBridgeStatus(bridgeUiState);

  setStatus(statusKey, statusKind);
  renderSummary();
  updatePathField(els.outputFolder, getOutputFolderDisplayPath(), t("outputPlaceholder"));
  refreshDocumentState();
  void syncOutputFolderFromActiveDocument().then(() => prefixController?.update());
  void prefixController?.update();
  renderBuiPreview();
  updateExportAvailability();
}

function toggleNamingPopover() {
  if (!els || !els.namingPopover) {
    return;
  }
  els.namingPopover.classList.toggle("is-hidden");
}

function hideNamingPopover() {
  if (!els || !els.namingPopover) {
    return;
  }
  els.namingPopover.classList.add("is-hidden");
}

function t(key) {
  const tables = i18nTable || {};
  const table = tables[currentLang] || {};
  return table[key] || key;
}

function setStatus(key, kind) {
  statusKey = key || "ready";
  statusKind = kind || "";
}

function setSummary(title, body, kind) {
  summaryState = {
    titleKey: "",
    bodyKey: "",
    titleText: title,
    bodyText: body,
    kind: kind || ""
  };
  renderSummary();
}

function setSummaryFromKeys(titleKey, bodyKey, kind) {
  summaryState = {
    titleKey,
    bodyKey,
    titleText: "",
    bodyText: "",
    kind: kind || ""
  };
  renderSummary();
}

function setSummaryWithTitleKey(titleKey, body, kind) {
  summaryState = {
    titleKey,
    bodyKey: "",
    titleText: "",
    bodyText: body,
    kind: kind || ""
  };
  renderSummary();
}

function renderSummary() {
  if (!els || !els.exportSummary) {
    return;
  }

  const title = summaryState.titleKey ? t(summaryState.titleKey) : summaryState.titleText;
  const body = summaryState.bodyKey ? t(summaryState.bodyKey) : summaryState.bodyText;
  els.exportSummary.classList.toggle("is-error", summaryState.kind === "error");
  els.exportSummary.innerHTML = `<strong>${escapeHtml(title)}</strong><span>${escapeHtml(body)}</span>`;
}

function updatePathField(node, value, placeholder) {
  const text = value || placeholder || "";
  node.textContent = text;
  node.title = value || "";
  node.classList.toggle("is-empty", !value);
}

function getOutputFolderDisplayPath() {
  return selectedOutputFolder ? displayEntry(selectedOutputFolder) : derivedOutputFolderPath;
}

function displayEntry(entry) {
  if (!entry) {
    return "";
  }
  return entry.nativePath || entry.name || "";
}

function getCurrentPsdPath(doc) {
  return readDocumentNativePath(doc);
}

function readDocumentNativePath(doc) {
  try {
    const path = safeString(doc?.path, "").trim();
    return path && !path.startsWith("cloud:") ? path : "";
  } catch (err) {
    return "";
  }
}

function getDocumentFolderPath(doc) {
  return getFolderPathFromNativePath(readDocumentNativePath(doc));
}

function getFolderPathFromNativePath(path) {
  if (!path) {
    return "";
  }

  const normalized = path.replace(/\\/g, "/");
  const index = normalized.lastIndexOf("/");
  if (index <= 0) {
    return "";
  }
  return normalized.slice(0, index);
}

function updateExportAvailability() {
  if (!els || !els.exportBtn) {
    return;
  }

  const validation = getExportValidation();
  els.exportBtn.disabled = busy || !validation.canExport;
  els.exportBtn.title = validation.canExport ? "" : t(validation.reasonKey);
  els.exportBtn.textContent = t("startProcess");
}

function getExportValidation() {
  const doc = getActiveDocument();
  if (!doc) {
    return { canExport: false, reasonKey: "noActiveDocument" };
  }

  if (!isValidSliceFileNamePrefix(getSliceFileNamePrefix())) {
    return { canExport: false, reasonKey: "invalidSliceFileNamePrefix" };
  }

  if (!safeString(els?.exportBaseName?.value, "").trim()) {
    return { canExport: false, reasonKey: "noExportBaseName" };
  }

  if (!selectedOutputFolder && !derivedOutputFolderPath && !getDocumentFolderPath(doc)) {
    return { canExport: false, reasonKey: "noDocumentFolderPath" };
  }

  return { canExport: true, reasonKey: "" };
}

async function syncOutputFolderFromActiveDocument() {
  if (busy) {
    return;
  }

  const doc = getActiveDocument();
  const docPath = readDocumentNativePath(doc);
  const folderPath = getFolderPathFromNativePath(docPath);
  if (!folderPath || folderPath === autoOutputFolderPathInFlight) {
    return;
  }

  const selectedMatches =
    selectedOutputFolder &&
    normalizePathForCompare(displayEntry(selectedOutputFolder)) === normalizePathForCompare(folderPath);
  if (!selectedMatches) {
    selectedOutputFolder = null;
  }

  derivedOutputFolderPath = folderPath;
  outputFolderSource = "documentPath";
  updatePathField(els.outputFolder, selectedMatches ? displayEntry(selectedOutputFolder) : folderPath, t("outputPlaceholder"));
  renderBuiPreview();
  updateExportAvailability();

  if (selectedMatches) {
    return;
  }

  autoOutputFolderPathInFlight = folderPath;
  try {
    const folder = await withTimeout(
      getFolderEntryFromDocumentPath(docPath),
      ENTRY_LOOKUP_TIMEOUT_MS,
      t("documentFolderAutoTimeout")
    );
    if (!folder) {
      return;
    }

    selectedOutputFolder = folder;
    outputFolderSource = "documentPath";
    derivedOutputFolderPath = displayEntry(folder) || folderPath;
    updatePathField(els.outputFolder, displayEntry(folder), t("outputPlaceholder"));
    renderBuiPreview();
    updateExportAvailability();
  } catch (err) {
    if (lastAutoOutputFolderFailure !== folderPath) {
      lastAutoOutputFolderFailure = folderPath;
      log(formatText(t("documentFolderAutoFailed"), { error: formatError(err) }));
    }
  } finally {
    autoOutputFolderPathInFlight = "";
  }
}

async function getEntryFromNativePath(nativePath) {
  if (!fs || typeof fs.getEntryWithUrl !== "function") {
    return null;
  }

  const urls = makeFileUrlCandidates(nativePath);
  for (const url of urls) {
    try {
      const entry = await fs.getEntryWithUrl(url);
      if (entry) {
        return entry;
      }
    } catch (err) {
      // Try the next URL form. UXP file URL behavior differs slightly across hosts.
    }
  }

  return null;
}

async function getFolderEntryFromNativePath(nativePath) {
  const entry = await getEntryFromNativePath(nativePath);
  return entry && entry.isFolder ? entry : null;
}

async function getFileEntryFromNativePath(nativePath) {
  const entry = await getEntryFromNativePath(nativePath);
  return entry && !entry.isFolder ? entry : null;
}

async function getFolderEntryFromDocumentPath(nativePath) {
  const folderPath = getFolderPathFromNativePath(nativePath);
  const folder = await getFolderEntryFromNativePath(folderPath);
  if (folder) {
    return folder;
  }

  const fileEntry = await getEntryFromNativePath(nativePath);
  if (fileEntry && !fileEntry.isFolder) {
    return tryGetParentFolder(fileEntry);
  }

  return null;
}

function makeFileUrlCandidates(nativePath) {
  const normalized = safeString(nativePath, "").replace(/\\/g, "/");
  const withoutTrailingSlash = normalized.replace(/\/+$/, "");
  const encoded = encodeURI(withoutTrailingSlash);
  const urls = [];

  if (!withoutTrailingSlash) {
    return urls;
  }

  if (withoutTrailingSlash.startsWith("file:")) {
    urls.push(withoutTrailingSlash);
  } else if (/^[A-Za-z]:\//.test(withoutTrailingSlash)) {
    urls.push(`file:///${withoutTrailingSlash}`);
    urls.push(`file:///${encoded}`);
    urls.push(`file:/${withoutTrailingSlash}`);
    urls.push(`file:/${encoded}`);
    urls.push(`file://${withoutTrailingSlash}`);
    urls.push(`file://${encoded}`);
  } else if (withoutTrailingSlash.startsWith("/")) {
    urls.push(`file://${withoutTrailingSlash}`);
    urls.push(`file://${encoded}`);
    urls.push(`file:${withoutTrailingSlash}`);
    urls.push(`file:${encoded}`);
  }

  return urls.filter((url, index) => url && urls.indexOf(url) === index);
}

function normalizePathForCompare(path) {
  return safeString(path, "").replace(/\\/g, "/").replace(/\/+$/, "").toLowerCase();
}

function refreshDocumentState() {
  const doc = getActiveDocument();

  if (!doc) {
    selectedOutputFolder = null;
    derivedOutputFolderPath = "";
    outputFolderSource = "";
    lastActiveDocumentKey = "";
    els.documentTitle.textContent = t("docNoneTitle");
    els.documentMeta.textContent = t("docNoneMeta");
    els.documentPath.textContent = t("docPathMissing");
    els.documentPath.title = "";
    els.previewCanvas.textContent = formatText(t("canvasLabel"), { width: "-", height: "-" });
    els.previewLayerCount.textContent = formatText(t("buiNodesLabel"), { targets: "-", nodes: "-", prefabRefs: "-" });
    updatePathField(els.outputFolder, "", t("outputPlaceholder"));
    return;
  }

  const docName = doc.title || doc.name || "Untitled";
  const canvasWidth = Math.round(toNumber(doc.width, 0));
  const canvasHeight = Math.round(toNumber(doc.height, 0));
  const layerCount = arrayFrom(doc.layers).length;
  const psdPath = getCurrentPsdPath(doc);
  const folderPath = getDocumentFolderPath(doc);
  const documentKey = psdPath || docName;

  els.documentTitle.textContent = formatText(t("docTitle"), { name: docName });
  els.documentTitle.title = docName;
  els.documentMeta.textContent = canvasWidth > 0 && canvasHeight > 0
    ? formatText(t("docMeta"), { width: canvasWidth, height: canvasHeight, count: layerCount })
    : t("docMetaUnknown");
  els.documentPath.textContent = psdPath || t("docPathMissing");
  els.documentPath.title = psdPath;

  if (documentKey !== lastActiveDocumentKey) {
    selectedPsdFile = null;
    selectedOutputFolder = null;
    derivedOutputFolderPath = folderPath;
    outputFolderSource = folderPath ? "documentPath" : "";
    const nextDefaultName = defaultExportBaseName(docName);
    const currentName = safeString(els.exportBaseName.value, "");
    if (!currentName || currentName === lastDefaultExportBaseName) {
      els.exportBaseName.value = nextDefaultName;
    }
    lastDefaultExportBaseName = nextDefaultName;
    lastActiveDocumentKey = documentKey;
  } else if (!els.exportBaseName.value) {
    const nextDefaultName = defaultExportBaseName(docName);
    els.exportBaseName.value = nextDefaultName;
    lastDefaultExportBaseName = nextDefaultName;
  }

  updatePathField(els.outputFolder, derivedOutputFolderPath || folderPath, t("outputPlaceholder"));
}

function renderBuiPreview() {
  scheduleBuiPreviewRender();
}

async function assertDocumentSaved(doc, phase, log = () => {}) {
  // Read the same native property used by Photoshop's Document.saved getter,
  // but keep a missing/error response distinct from a real dirty document.
  const documentId = doc?.id;
  const detail = `documentId=${documentId}, phase=${phase}`;
  let result;
  try {
    if (!Number.isInteger(documentId) || documentId <= 0) throw new Error("Invalid document ID");
    result = await action.batchPlay([{
      _obj: "get",
      _target: [{ _property: "isDirty" }, { _ref: "document", _id: documentId }],
      _options: { dialogOptions: "dontDisplay" }
    }], { synchronousExecution: false });
    if (result?.[0]?._obj === "error" || typeof result?.[0]?.isDirty !== "boolean") {
      throw new Error(result?.[0]?.message || "Photoshop did not return boolean isDirty");
    }
  } catch (error) {
    throw new Error(`PSD_SAVE_STATE_UNAVAILABLE: 无法读取 Photoshop 保存状态（${detail}）：${error.message || error}`);
  }
  const dirty = result[0].isDirty;
  log(`PSD_SAVE_STATE: ${detail}, isDirty=${dirty}`);
  if (dirty) {
    throw new Error(`PSD_UNSAVED: Photoshop 报告当前 PSD 有未保存状态（${detail}）。这不代表画面一定有改动；请检查文档标签的 * 标记，手动保存完成后重试。`);
  }
  return true;
}

// Read one asynchronous document descriptor. Walking live Layer getters here
// calls batchPlaySync repeatedly and prevents UXP from dispatching panel clicks.
async function readPreviewDocument(doc) {
  const documentId = doc.id;
  if (previewSnapshotRequests.has(documentId)) {
    return previewSnapshotRequests.get(documentId);
  }
  const request = (async () => {
    const result = await action.batchPlay([{
      _obj: "get",
      _target: [{ _property: "json" }, { _ref: "document", _id: documentId }],
      getTextStyles: true,
      getFullTextStyles: true,
      getLayerEffects: true,
      getPathData: false
    }], { synchronousExecution: false });
    if (!result?.[0]?.json) {
      throw new Error(result?.[0]?.message || "Photoshop did not return the document preview data.");
    }
    const descriptor = JSON.parse(result[0].json);
    if (descriptor.id !== documentId || !Array.isArray(descriptor.layers)) {
      throw new Error("Photoshop returned incomplete or mismatched document preview data.");
    }
    const bounds = parseBoundsQuiet(descriptor.bounds);
    const name = safeString(descriptor.file, "untitled.psd").replace(/\\/g, "/").split("/").pop();
    return {
      id: documentId,
      name,
      title: name,
      width: bounds ? bounds.right - bounds.left : 0,
      height: bounds ? bounds.bottom - bounds.top : 0,
      resolution: descriptor.resolution,
      moduleName: readPsdDirectoryModule(getCurrentPsdPath(doc) || descriptor.file),
      layers: makePreviewLayerSnapshots(descriptor.layers, doc)
    };
  })();
  previewSnapshotRequests.set(documentId, request);
  try {
    return await request;
  } finally {
    previewSnapshotRequests.delete(documentId);
  }
}

function makePreviewLayerSnapshots(layers, sourceDoc, parentIndexes = []) {
  return layers.slice().sort((a, b) => b.index - a.index).map((layer, index) => ({
    ...layer,
    // Generator bounds exclude effects; the existing preview prefers these.
    boundsNoEffects: layer.bounds,
    bounds: layer.boundsWithFX || layer.bounds,
    kind: layer.smartObject ? "smartObject" : layer.type,
    textItem: layer.text ? {
      ...layer.text,
      contents: layer.text.textKey,
      transform: layer.text.transform || layer.text.textShape?.[0]?.transform
    } : undefined,
    // Resolve only the layer being sliced; geometry traversal stays in memory.
    duplicate: sourceDoc ? async (targetDoc) => {
      let liveLayer = sourceDoc;
      for (const layerIndex of parentIndexes.concat(index)) {
        liveLayer = liveLayer.layers[layerIndex];
      }
      if (!liveLayer || liveLayer.id !== layer.id) {
        throw new Error("PSD layers changed during export. Refresh the preview and export again.");
      }
      return liveLayer.duplicate(targetDoc);
    } : undefined,
    layers: makePreviewLayerSnapshots(layer.layers || [], sourceDoc, parentIndexes.concat(index))
  }));
}

async function renderBuiPreviewNow() {
  if (!els || !els.layerPreviewList) {
    return;
  }

  const doc = getActiveDocument();
  if (!doc) {
    lastPreviewJson = "";
    updatePreviewToolbarStatus(null);
    renderPreviewPlan(null);
    renderPreviewWarnings([]);
    updateCopyPreviewAvailability();
    els.layerPreviewList.innerHTML = `<div class="hint-row">${escapeHtml(t("previewEmpty"))}</div>`;
    return;
  }

  if (!namingRules) {
    lastPreviewJson = "";
    updatePreviewToolbarStatus(null);
    renderPreviewPlan(null);
    renderPreviewWarnings([]);
    updateCopyPreviewAvailability();
    els.layerPreviewList.innerHTML = `<div class="hint-row">${escapeHtml(t("previewRulesLoading"))}</div>`;
    return;
  }

  const revision = previewRenderRevision;
  lastPreviewJson = "";
  updateCopyPreviewAvailability();
  let preview;
  try {
    const snapshot = await readPreviewDocument(doc);
    if (revision !== previewRenderRevision || getActiveDocument()?.id !== snapshot.id) {
      return;
    }
    preview = buildBuiPreviewModel(snapshot);
    const path = getCurrentPsdPath(doc);
    if (path) {
      let saved = false;
      try { saved = await assertDocumentSaved(doc, "adaptation-preview"); }
      catch (error) { if (!String(error.message).startsWith("PSD_UNSAVED:")) throw error; }
      if (saved) {
        const file = await getFileEntryFromNativePath(path), folder = await getFolderEntryFromNativePath(getFolderPathFromNativePath(path));
        const source = {psdSha256:await sha256Hex(await file.read({format:formats.binary}))};
        const reuse = globalThis.BurnerPsdToBuiResourceReuse.create({xmp:uxp.xmp,formats,sha256Hex});
        const input = await reuse.inspect(file, folder, source, true);
        if (input.adaptation) {
          const options = capturePreviewInput(), settings = await getExportSettings();
          const services = createExportServices({...options,document:doc,outputFolder:folder,exportSettings:settings});
          const warnings = services.createBaselineWarnings(), canvas = services.buildCanvasInfo(snapshot,warnings);
          const built = await services.buildNodes(snapshot,canvas,options.exportBaseName,warnings,null,options.namingRules,
            {skipAssets:true,adaptationReadOnly:true,adaptation:input.adaptation,psdSha256:source.psdSha256});
          await reuse.verifyUnchanged(input,file,source,await assertDocumentSaved(doc,"adaptation-preview-complete"));
          preview = {...preview,...built,source:{...preview.source,...source,adaptationSource:input.adaptationSource},warnings};
        }
      }
    }
    if (revision !== previewRenderRevision || getActiveDocument()?.id !== snapshot.id) return;
  } catch (err) {
    if (revision !== previewRenderRevision || getActiveDocument()?.id !== doc.id) {
      return;
    }
    const message = formatError(err);
    renderPreviewPlan(null);
    renderPreviewWarnings([{ code: "PREVIEW_READ_FAILED", severity: "Blocking", message }]);
    els.layerPreviewList.innerHTML = `<div class="hint-row">${escapeHtml(message)}</div>`;
    log(message);
    return;
  }
  const [canvasWidth, canvasHeight] = preview.canvas.referenceResolution;
  els.previewCanvas.textContent = formatText(t("canvasLabel"), { width: canvasWidth || "-", height: canvasHeight || "-" });
  els.previewLayerCount.textContent = formatText(t("buiNodesLabel"), {
    targets: preview.targets.length,
    nodes: preview.nodes.length,
    prefabRefs: countPrefabRefs(preview.nodes)
  });

  const json = JSON.stringify(preview, null, 2);
  lastPreviewJson = json;
  updatePreviewToolbarStatus(preview);
  renderPreviewPlan(preview);
  renderPreviewWarnings(preview.warnings);
  updateCopyPreviewAvailability();
  renderStructurePreview(preview);
  updateExportAvailability();
}

function updatePreviewToolbarStatus(preview) {
  void preview;
}

function renderPreviewPlan(preview) {
  if (!els || !els.plannedBuiFile || !els.plannedAssetsFolder) {
    return;
  }

  if (!preview || !preview.exportPlan) {
    updatePathField(els.plannedBuiFile, "", t("noExportBaseNameShort"));
    updatePathField(els.plannedAssetsFolder, "", t("noExportBaseNameShort"));
    return;
  }

  const plan = preview.exportPlan;
  const buiFileName = plan.buiFileName || t("noExportBaseNameShort");
  const assetsRoot = plan.assetsRoot || t("noExportBaseNameShort");
  const assetsSkipped = Boolean(plan.assetsSkipped);

  updatePathField(els.plannedBuiFile, plan.buiFileName ? buiFileName : "", t("noExportBaseNameShort"));
  updatePathField(
    els.plannedAssetsFolder,
    assetsSkipped ? t("assetsExportSkippedShort") : (plan.assetsRoot ? assetsRoot : ""),
    t("noExportBaseNameShort")
  );
}

function buildSliceRowsFromNodes(nodes) {
  return arrayFrom(nodes)
    .filter((node) => node.resource && node.resource.asset)
    .map((node) => ({
      sourcePath: node.sourcePath || node.name,
      asset: node.resource.asset,
      meta: `${node.resource.width || 0}x${node.resource.height || 0} · ${node.source?.resourceType || node.resource.resourceType || "Asset"}${node.resource.outputModule ? " · @Module_" + node.resource.outputModule : ""}`
    }));
}

function renderStructurePreview(preview) {
  if (!els || !els.layerPreviewList) {
    return;
  }

  if (!preview || arrayFrom(preview.nodes).length === 0) {
    els.layerPreviewList.innerHTML = `<div class="hint-row">${escapeHtml(t("previewNoLayers"))}</div>`;
    return;
  }

  const rows = buildStructureRows(preview);
  const resources = buildSliceRowsFromNodes(preview.nodes).slice(0, PREVIEW_SLICE_LIMIT);
  const resourceCount = buildSliceRowsFromNodes(preview.nodes).length;
  const prefabRefCount = countPrefabRefs(preview.nodes);
  const structureHtml = rows.html || arrayFrom(preview.nodes)
    .slice(0, PREVIEW_STRUCTURE_LIMIT)
    .map((node) => renderStructureNode(node, getNodeDepth(node, {})))
    .join("");
  els.layerPreviewList.innerHTML = `
    <div class="structure-overview-row">
      <span>${escapeHtml(formatText(t("structurePreviewSummary"), {
        targets: arrayFrom(preview.targets).length,
        nodes: arrayFrom(preview.nodes).length,
        prefabRefs: prefabRefCount,
        resources: resourceCount
      }))}</span>
    </div>
    <div class="structure-preview-grid">
      <div class="structure-section">
        <div class="structure-section-title">${escapeHtml(formatText(t("structureTreeTitleWithCounts"), {
          targets: arrayFrom(preview.targets).length,
          prefabRefs: prefabRefCount
        }))}</div>
        <div class="structure-tree">
          ${structureHtml || `<div class="hint-row">${escapeHtml(t("previewNoLayers"))}</div>`}
          ${rows.omitted > 0 ? `<div class="preview-slice-more">${escapeHtml(formatText(t("previewMore"), { count: rows.omitted }))}</div>` : ""}
        </div>
      </div>
      <div class="structure-section">
        <div class="structure-section-title">${escapeHtml(formatText(t("resourcePlanTitleWithCount"), {
          count: resourceCount
        }))}</div>
        <div class="resource-hint">${escapeHtml(t("resourceFileNameHint"))}</div>
        <div class="resource-tree">
          ${resources.length > 0
            ? resources.map(renderResourcePreviewRow).join("")
            : `<div class="hint-row">${escapeHtml(t("previewResourceEmpty"))}</div>`}
        </div>
      </div>
    </div>
  `;
  els.layerPreviewList.scrollLeft = 0;
}

function togglePrefabGroup(evt) {
  const button = evt.target?.closest?.("[data-prefab-toggle]");
  if (!button) {
    return;
  }

  const group = button.closest(".prefab-group");
  const groupId = button.getAttribute("data-prefab-toggle") || "";
  const isCollapsed = !group?.classList.contains("is-collapsed");
  if (group) {
    group.classList.toggle("is-collapsed", isCollapsed);
  }
  button.setAttribute("aria-expanded", isCollapsed ? "false" : "true");
  if (groupId) {
    collapsedPrefabGroups[groupId] = isCollapsed;
  }
}

function buildStructureRows(preview) {
  const nodes = arrayFrom(preview.nodes);
  const nodeById = {};
  const childrenByParent = {};
  nodes.forEach((node) => {
    nodeById[node.id] = node;
    const parentKey = node.parentId || "";
    if (!childrenByParent[parentKey]) {
      childrenByParent[parentKey] = [];
    }
    childrenByParent[parentKey].push(node);
  });
  Object.keys(childrenByParent).forEach((key) => {
    childrenByParent[key].sort((a, b) => toNumber(a.siblingIndex, 0) - toNumber(b.siblingIndex, 0));
  });

  const parts = [];
  let omitted = 0;
  let rendered = 0;
  const visit = (node, depth, output) => {
    if (!node || rendered >= PREVIEW_STRUCTURE_LIMIT) {
      omitted += 1;
      return;
    }

    rendered += 1;
    output.push(renderStructureNode(node, depth));
    arrayFrom(childrenByParent[node.id]).forEach((child) => visit(child, depth + 1, output));
  };

  const targetGroups = arrayFrom(preview.targets)
    .map((target) => ({
      target,
      root: nodeById[target.sourceRootNodeId]
    }))
    .filter((item) => item.root);

  if (targetGroups.length > 0) {
    targetGroups.forEach((item, index) => {
      const groupRows = [];
      const stats = getNodeSubtreeStats(item.root, childrenByParent);
      visit(item.root, 0, groupRows);
      const groupId = item.target.targetId || item.root.id || `prefab_${index}`;
      const collapsed = Boolean(collapsedPrefabGroups[groupId]);
      parts.push(`
        <div class="prefab-group prefab-tone-${index % 4}${collapsed ? " is-collapsed" : ""}">
          <button class="prefab-group-title" type="button" data-prefab-toggle="${escapeHtml(groupId)}" aria-expanded="${collapsed ? "false" : "true"}">
            <span>${escapeHtml(item.target.name || item.root.name || `Prefab ${index + 1}`)}</span>
            <em>${escapeHtml(item.target.targetType || item.root.type || "Target")} · ${escapeHtml(formatText(t("prefabGroupCounts"), {
              nodes: stats.nodes,
              prefabRefs: stats.prefabRefs
            }))}</em>
          </button>
          <div class="prefab-group-body">
            ${groupRows.join("")}
          </div>
        </div>
      `);
    });
  }

  if (parts.length === 0 && nodes.length > 0) {
    nodes.slice(0, PREVIEW_STRUCTURE_LIMIT).forEach((node) => {
      rendered += 1;
      parts.push(renderStructureNode(node, getNodeDepth(node, nodeById)));
    });
  }
  return { html: parts.join(""), omitted: Math.max(omitted, nodes.length - rendered, 0) };
}

function getNodeSubtreeStats(root, childrenByParent) {
  const stats = {
    nodes: 0,
    prefabRefs: 0
  };

  const visit = (node) => {
    if (!node) {
      return;
    }
    stats.nodes += 1;
    if (node.type === "PrefabRef") {
      stats.prefabRefs += 1;
    }
    arrayFrom(childrenByParent[node.id]).forEach(visit);
  };

  visit(root);
  return stats;
}

function countPrefabRefs(nodes) {
  return arrayFrom(nodes).filter((node) => node.type === "PrefabRef").length;
}

function getNodeDepth(node, nodeById) {
  let depth = 0;
  let cursor = node;
  const visited = {};
  while (cursor && cursor.parentId && nodeById[cursor.parentId] && !visited[cursor.parentId]) {
    visited[cursor.parentId] = true;
    depth += 1;
    cursor = nodeById[cursor.parentId];
  }
  return depth;
}

function renderStructureNode(node, depth) {
  const depthValue = Math.min(toNumber(depth, 0), 10);
  const type = node.type || "Node";
  const rect = node.rect || {};
  const size = Number.isFinite(toNumber(rect.width, NaN)) && Number.isFinite(toNumber(rect.height, NaN))
    ? `${Math.round(toNumber(rect.width, 0))}x${Math.round(toNumber(rect.height, 0))}`
    : "";
  const resource = node.resource?.asset
    ? `${node.resource.asset} · ${node.source?.resourceType || node.resource.resourceType || "Asset"}${node.resource.outputModule ? " · @Module_" + node.resource.outputModule : ""}`
    : "";
  const extra = [node.sourcePath || "", resource, size].filter(Boolean).join(" · ");

  return `
    <div class="structure-row" style="padding-left: ${8 + depthValue * 16}px">
      <span class="structure-type">${escapeHtml(type)}</span>
      <strong>${escapeHtml(node.name || node.id || "-")}</strong>
      <em title="${escapeHtml(extra)}">${escapeHtml(extra || "-")}</em>
    </div>
  `;
}

function renderResourcePreviewRow(resource) {
  return `
    <div class="resource-row" title="${escapeHtml(resource.sourcePath || "")}">
      <strong>${escapeHtml(resource.asset || "-")}</strong>
      <span>${escapeHtml(resource.meta || "-")}</span>
      <em>${escapeHtml(resource.sourcePath || "-")}</em>
    </div>
  `;
}

function renderPreviewWarnings(warnings) {
  if (!els || !els.previewWarnings) {
    return;
  }

  const oldList = els.previewWarnings.querySelector(".preview-warning-list");
  const previousScrollTop = oldList ? oldList.scrollTop : 0;
  const items = arrayFrom(warnings);
  const sortedItems = sortWarningsForDisplay(items);
  const blocking = sortedItems.filter((item) => item.severity === "Blocking");
  lastPreviewWarnings = sortedItems;
  lastPreviewWarningsText = sortedItems.map(formatWarningForCopy).join("\n\n");
  els.previewWarnings.classList.toggle("is-error", blocking.length > 0);

  if (sortedItems.length === 0) {
    els.previewWarnings.innerHTML = `
      <div class="preview-warning-header">
        <strong>${escapeHtml(t("previewWarningsTitle"))}</strong>
      </div>
      <div class="preview-warning-list">
        <div class="preview-warning-empty">${escapeHtml(t("previewWarningsEmpty"))}</div>
      </div>
    `;
    updateCopyPreviewAvailability();
    return;
  }

  const rows = groupedWarningsForDisplay(sortedItems).map((group) => `
    <div class="warning-group-title">${escapeHtml(group.label)} · ${group.items.length}</div>
    ${group.items.map((warning) => `
    <div class="preview-warning-row" data-warning-index="${lastPreviewWarnings.indexOf(warning)}" title="${escapeHtml(t("clickToCopy"))}">
      <div class="warning-row-main">
        <b class="is-${escapeHtml(getWarningPriority(warning))}">${escapeHtml(getWarningPriorityLabel(warning))}</b>
        <span>${escapeHtml(warning.code || "")}</span>
      </div>
      <div class="warning-row-path" title="${escapeHtml(warning.sourcePath || t("documentRoot"))}">${escapeHtml(warning.sourcePath || t("documentRoot"))}</div>
      <div class="warning-row-message">${escapeHtml(getLocalizedWarningMessage(warning))}</div>
    </div>
    `).join("")}
  `);

  els.previewWarnings.innerHTML = `
    <div class="preview-warning-header">
      <strong>${escapeHtml(t("previewWarningsTitle"))}</strong>
      <span>${escapeHtml(formatWarningPrioritySummary(sortedItems))}</span>
    </div>
    <div class="preview-warning-list">
      ${rows.join("")}
    </div>
  `;
  const list = els.previewWarnings.querySelector(".preview-warning-list");
  if (list) {
    list.scrollTop = previousScrollTop;
  }
  updateCopyPreviewAvailability();
}

function updateCopyPreviewAvailability() {
  if (!els || !els.copyPreviewBtn) {
    return;
  }

  els.copyPreviewBtn.disabled = busy || !lastPreviewJson;
  els.copyWarningsBtn.disabled = !lastPreviewWarningsText;
  els.copyLogBtn.disabled = logEntries.length === 0;
  els.clearLogBtn.disabled = logEntries.length === 0;
}

async function copyPreviewJson() {
  if (!lastPreviewJson) {
    log(t("previewCopyEmpty"));
    return;
  }

  await copyTextToClipboard(lastPreviewJson, "previewCopied", "previewCopyFailed");
}

async function copyWarnings() {
  if (!lastPreviewWarningsText) {
    log(t("warningsCopyEmpty"));
    return;
  }

  await copyTextToClipboard(lastPreviewWarningsText, "warningsCopied", "warningsCopyFailed");
}

async function copyLog() {
  if (logEntries.length === 0) {
    return;
  }

  await copyTextToClipboard(logEntries.join("\n"), "logCopied", "logCopyFailed");
}

function clearLog() {
  logEntries = [];
  renderLogEntries();
}

async function copyRowText(evt, copiedKey) {
  const warningRow = evt.target?.closest?.("[data-warning-index]");
  if (warningRow) {
    const index = Number.parseInt(warningRow.getAttribute("data-warning-index") || "-1", 10);
    const text = formatWarningForCopy(lastPreviewWarnings[index]);
    if (!text.trim()) {
      return;
    }

    await copyTextToClipboard(text, copiedKey, "rowCopyFailed");
    return;
  }

  const row = evt.target?.closest?.("[data-copy-text]");
  if (!row) {
    return;
  }

  const text = row.getAttribute("data-copy-text") || row.textContent || "";
  if (!text.trim()) {
    return;
  }

  await copyTextToClipboard(text, copiedKey, "rowCopyFailed");
}

async function copyTextToClipboard(text, copiedKey, failedKey) {
  try {
    if (!navigator?.clipboard?.writeText) {
      throw new Error(t("clipboardUnavailable"));
    }
    await navigator.clipboard.writeText(text);
    if (copiedKey) {
      log(t(copiedKey));
    }
  } catch (err) {
    log(formatText(t(failedKey || "rowCopyFailed"), { error: formatError(err) }));
  }
}

function renderLogEntries() {
  if (!els || !els.log) {
    return;
  }

  els.log.innerHTML = logEntries.map((entry) => `
    <div class="log-entry" data-copy-text="${escapeHtml(entry)}" title="${escapeHtml(t("clickToCopy"))}">
      ${escapeHtml(entry)}
    </div>
  `).join("");
  updateCopyPreviewAvailability();
}

function buildBuiPreviewModel(doc, input = capturePreviewInput()) {
  const namingRules = input.namingRules;
  const makeAssetFileName = (...args) => makeAssetFileNameWithPrefix(input.sliceFileNamePrefix, ...args);
  return build(doc);

  function collectPreviewRootItems(layers, namingRules, warnings) {
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

      const parsed = parsePsdNaming(name, namingRules);
      if (isIgnoredByNaming(parsed)) {
        pushIgnoreWarning(warnings, name, parsed, namingRules);
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

  function appendPreviewDuplicateTargetWarnings(targets, warnings) {
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

  function build(doc) {
    const warnings = [];
    const canvas = buildCanvasInfo(doc, warnings);
    const docName = doc.title || doc.name || input.sourceName || "untitled.psd";
    const rawExportBaseName = safeString(input.exportBaseName, "").trim();
    const exportBaseName = rawExportBaseName ? sanitizeFileBase(rawExportBaseName) : "";
    const assetsRoot = exportBaseName ? `${exportBaseName}${input.assetsSuffix}` : "";
    const skipAssetsExport = input.skipAssetsExport;
    const model = {
      schemaVersion: 2,
      previewOnly: true,
      source: {
        type: "psd",
        psdName: docName,
        adapter: ADAPTER_NAME,
        adapterVersion: ADAPTER_VERSION,
        namingRules: {
          configPath: namingRules?.configPath || NAMING_RULES_CONFIG_PATH,
          version: namingRules?.version || "unknown"
        }
      },
      assetsRoot,
      exportPlan: {
        outputFolder: input.outputFolderPath,
        buiFileName: exportBaseName ? `${exportBaseName}${input.buiExtension}` : "",
        assetsRoot,
        assetsSkipped: skipAssetsExport,
        targetCount: 0,
        nodeCount: 0,
        sliceCount: 0,
        warningCount: 0,
        blockingCount: 0,
        slices: []
      },
      canvas: {
        referenceResolution: [canvas.width, canvas.height],
        coordinateSystem: "UnityRectTransform",
        origin: "Center",
        unit: "px"
      },
      targets: [],
      nodes: [],
      resources: [],
      warnings
    };
    const exportState = {
      usedAssetNames: {},
      usedTargetIds: {},
      previewTruncated: false,
      previewNodeLimit: Number.isInteger(input.nodeLimit) && input.nodeLimit > 0 ? input.nodeLimit : BUI_PREVIEW_NODE_LIMIT,
      moduleName: doc.moduleName || "",
      skipAssets: skipAssetsExport
    };
    const unmarkedRootLayers = [];
    const layers = arrayFrom(doc.layers);
    const rootItems = collectPreviewRootItems(layers, namingRules, warnings);
    function findUnsupportedSubPages(items) {
      for (const layer of items) {
        const parsed = parsePsdNaming(safeString(layer.name, ""), namingRules);
        if (isIgnoredByNaming(parsed)) continue;
        if ((resolveTargetRule(parsed, namingRules)?.targetType) === "SubPage" || /(^|\s)@(SubPage|子页)(_|\s|$)/.test(safeString(layer.name, ""))) warnings.push({code: "BUI_V4_PRODUCTION_NOT_READY", severity: "Blocking", sourcePath: safeString(layer.name, ""), message: "SubPage requires BUI v4 adaptation. Production export awaits template/layout consumption; use dedicated read-only analysis."});
        findUnsupportedSubPages(arrayFrom(layer.layers));
      }
    }
    findUnsupportedSubPages(layers);

    if (layers.length === 0) {
      warnings.push({
        code: "DOCUMENT_LAYERS_UNAVAILABLE",
        severity: "Warning",
        message: "Photoshop DOM did not expose document layers."
      });
      return finalizeBuiPreviewModel(model);
    }

    for (const item of rootItems) {
      const layer = item.layer;
      const name = safeString(layer.name, `Layer_${item.siblingIndex}`);
      const sourcePath = item.sourcePath;
      const parsed = parsePsdNaming(name, namingRules);

      if (isIgnoredByNaming(parsed)) {
        pushIgnoreWarning(warnings, sourcePath, parsed, namingRules);
        continue;
      }

      const targetRule = resolveTargetRule(parsed, namingRules);
      if (targetRule && targetRule.allowedAtRoot !== false) {
        appendPreviewTargetFromRootLayer(layer, item.siblingIndex, canvas, model, exportState, targetRule, parsed, sourcePath);
        continue;
      }

      unmarkedRootLayers.push({ layer, siblingIndex: item.siblingIndex, sourcePath });
    }

    appendPreviewDuplicateTargetWarnings(model.targets, warnings);

    if (model.targets.length === 0 && shouldUseNoTargetFallback(namingRules)) {
      const fallback = createSyntheticTargetRoot(exportBaseName, canvas, model.nodes, exportState, namingRules);
      model.targets.push(fallback.target);
      warnings.push({
        code: namingRules.rootPolicy?.noTargetFallback?.warningCode || "PSD_NO_TARGET_ROOT_FALLBACK",
        severity: "Warning",
        message: "No configured root target group was found. The preview used the rootPolicy.noTargetFallback target.",
        nodeId: fallback.rootNode.id,
        sourcePath: ""
      });

      for (const item of unmarkedRootLayers) {
        appendPreviewLayer(
          item.layer,
          fallback.rootNode.id,
          "",
          item.siblingIndex,
          canvas,
          model,
          exportState,
          {
            parentNodeType: fallback.rootNode.type,
            targetType: fallback.target.targetType,
            isTargetRootChild: true,
            resourcePrefix: deriveResourceModuleName(fallback.target.name || exportBaseName, exportState.moduleName)
          }
        );
      }

      return finalizeBuiPreviewModel(model);
    }

    for (const item of unmarkedRootLayers) {
      warnings.push({
        code: namingRules.warningCodes?.unmarkedRootIgnored || "PSD_UNMARKED_ROOT_IGNORED",
        severity: "Info",
        message: "Root layer/group has no configured TargetType flag and was ignored by the configured root policy.",
        sourcePath: item.sourcePath
      });
    }

    if (model.targets.length === 0) {
      warnings.push({
        code: "PSD_NO_EXPORTABLE_ROOT_TARGET",
        severity: "Blocking",
        message: "No exportable root target was found. Add a configured TargetType flag to a top-level PSD group."
      });
    }

    return finalizeBuiPreviewModel(model);
  }

  function appendPreviewTargetFromRootLayer(layer, siblingIndex, canvas, model, exportState, targetRule, parsed, sourcePathOverride, resourcePrefixOverride) {
    if (!canAppendPreviewNode(model, exportState)) {
      return null;
    }

    const name = safeString(layer.name, `Target_${siblingIndex}`);
    const sourcePath = sourcePathOverride || name;
    pushTargetRootNamingWarnings(parsed, namingRules, model.warnings, sourcePath);
    const childLayers = arrayFrom(layer.layers);
    const targetValue = (targetRule.targetType || targetRule.id) === "Component" ? resolvePrefabRefNaming(parsed).prefabName : parsed.main?.value;
    const identity = parseTargetIdentity(targetValue || makePageClassName(stripExtension(name)), targetRule.targetType || targetRule.id);
    const targetCanonicalName = sanitizeNodeName(identity.name);
    const resourceModule = identity.module || exportState.moduleName;
    const resourcePrefix = (!identity.module && resourcePrefixOverride) || deriveResourceModuleName(targetCanonicalName, resourceModule);
    validatePreviewTargetNamingConvention(targetCanonicalName, targetRule, model.warnings, sourcePath);
    const targetBounds = readPreviewTargetContentBounds(layer, canvas, sourcePath);
    const targetCoordinateSpace = createPreviewCoordinateSpace(targetBounds, canvas);
    const rootId = makePreviewNodeId(`target_${slugify(targetCanonicalName)}_root`, sourcePath, siblingIndex, model.nodes);
    const rootNode = {
      id: rootId,
      parentId: null,
      name: targetCanonicalName,
      type: targetRule.nodeType || "Group",
      active: readLayerVisible(layer),
      siblingIndex,
      sourcePath,
      source: {
        layerId: readLayerIdQuiet(layer),
        layerType: "group",
        resourceType: "None",
        variant: getDefaultVariant(namingRules),
        zIndex: siblingIndex,
        naming: buildNamingSource(parsed, targetRule)
      },
      rect: makeRect(0, 0, targetCoordinateSpace.canvas.width, targetCoordinateSpace.canvas.height, targetCoordinateSpace.canvas)
    };
    model.nodes.push(rootNode);

    if (childLayers.length === 0) {
      model.warnings.push({
        code: namingRules.warningCodes?.invalidFlagContext || "PSD_FLAG_CONTEXT_INVALID",
        severity: "Warning",
        message: "A root TargetType flag should be placed on a PSD group. This target has no child layers.",
        nodeId: rootId,
        sourcePath
      });
    }

    for (let i = 0; i < childLayers.length; i += 1) {
      appendPreviewLayer(
        childLayers[i],
        rootId,
        sourcePath,
        i,
        canvas,
        model,
        exportState,
        {
          parentNodeType: rootNode.type,
          targetType: targetRule.targetType || targetRule.id,
          isTargetRootChild: true,
          coordinateSpace: targetCoordinateSpace,
          resourcePrefix,
          resourceModule
        }
      );
    }

    const target = makeTargetRecord(targetCanonicalName, rootId, targetRule, exportState);
    model.targets.push(target);
    return { rootNode, target, targetBounds };
  }

  function validatePreviewTargetNamingConvention(targetName, targetRule, warnings, sourcePath) {
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

  function appendPreviewLayer(layer, parentId, parentPath, siblingIndex, canvas, model, exportState, context) {
    if (!canAppendPreviewNode(model, exportState)) {
      return;
    }

    const name = safeString(layer.name, `Layer_${siblingIndex}`);
    const sourcePath = parentPath ? `${parentPath}/${name}` : name;
    const parsed = parsePsdNaming(name, namingRules);

    if (isIgnoredByNaming(parsed)) {
      pushIgnoreWarning(model.warnings, sourcePath, parsed, namingRules);
      return;
    }

    const childLayers = arrayFrom(layer.layers);
    const isGroup = childLayers.length > 0 || isLayerGroup(layer);
    const layerType = detectLayerType(layer, isGroup);
    const targetRule = resolveTargetRule(parsed, namingRules);
    if (targetRule && context?.parentNodeType === "List" && targetRule.allowedAsListPlaceholder) {
      appendPreviewListItemMount(layer, parentId, parentPath, siblingIndex, canvas, model, exportState, context, targetRule, parsed, layerType);
      return;
    }
    if (targetRule && isComponentTargetRule(targetRule)) {
      if (canGenerateComponentTargetMount(context, namingRules)) {
        appendPreviewComponentMount(layer, parentId, parentPath, siblingIndex, canvas, model, exportState, context, targetRule, parsed, layerType);
      } else {
        pushInvalidComponentTargetPreviewWarning(model.warnings, sourcePath, context);
      }
      return;
    }

    const semantics = resolveNodeSemantics(parsed, layerType, isGroup, context || {}, namingRules, model.warnings, sourcePath);
    const nodeId = makePreviewNodeId(`${nodeIdPrefix(semantics.nodeType, isGroup)}_${slugify(semantics.nodeName || name)}`, sourcePath, siblingIndex, model.nodes);
    const bounds = semantics.nodeType === "List"
      ? readPreviewListDisplayBounds(layer, canvas)
      : readPreviewBounds(layer, canvas);
    const node = {
      id: nodeId,
      parentId,
      name: sanitizeNodeName(semantics.nodeName || name),
      type: semantics.nodeType,
      active: readLayerVisible(layer),
      siblingIndex,
      sourcePath,
      source: {
        layerId: readLayerIdQuiet(layer),
        layerType,
        resourceType: semantics.resourceType || "None",
        variant: getDefaultVariant(namingRules),
        zIndex: siblingIndex,
        naming: semantics.namingSource
      },
      rect: makePreviewRectInCoordinateSpace(bounds, context?.coordinateSpace, canvas)
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

    if (semantics.nodeType === "Text") {
      node.text = readTextInfoSync(layer, sourcePath, model.warnings) || {
        text: "",
        styleStatus: "PreviewOnly",
        tmpStatus: "Placeholder"
      };
      node.text.key = derivePreviewTextKeySeed(node.name, node.text.text);
    }

    appendLayerUncertaintyWarnings(layer, sourcePath, model.warnings);
    if (semantics.shouldSlice && !exportState.skipAssets) {
      node.resource = makePreviewResource(sourcePath, nodeId, bounds, semantics, exportState, context?.resourcePrefix, canvas, context?.resourceModule);
    }

    if (semantics.exportOnly) {
      if (node.resource) {
        model.resources.push({ id: node.id, ownerNodeId: parentId, name: node.name, sourcePath, source: node.source, resource: node.resource });
      }
      return;
    }
    model.nodes.push(node);

    if (semantics.skipChildren) {
      if (semantics.prefabRef && childLayers.length > 0) {
        appendPreviewPrefabOverrides(
          childLayers,
          sourcePath,
          bounds,
          node,
          canvas,
          model,
          exportState,
          context
        );
      }
      return;
    }

    for (let i = 0; i < childLayers.length; i += 1) {
      appendPreviewLayer(
        childLayers[i],
        nodeId,
        sourcePath,
        i,
        canvas,
        model,
        exportState,
        {
          parentNodeType: node.type,
          targetType: context?.targetType,
          isTargetRootChild: false,
          coordinateSpace: context?.coordinateSpace,
          resourceModule: context?.resourceModule,
          resourcePrefix: context?.resourcePrefix
        }
      );
    }
  }

  function appendPreviewPrefabOverrides(layers, parentSourcePath, prefabRefBounds, prefabRefNode, canvas, model, exportState, context) {
    arrayFrom(layers).forEach((layer, index) => {
      const name = safeString(layer.name, `Layer_${index}`);
      const sourcePath = parentSourcePath ? `${parentSourcePath}/${name}` : name;
      const parsed = parsePsdNaming(name, namingRules);
      if (isIgnoredByNaming(parsed)) {
        pushIgnoreWarning(model.warnings, sourcePath, parsed, namingRules);
        return;
      }

      const childLayers = arrayFrom(layer.layers);
      const isGroup = childLayers.length > 0 || isLayerGroup(layer);
      const layerType = detectLayerType(layer, isGroup);
      const semantics = resolveNodeSemantics(parsed, layerType, isGroup, {
        parentNodeType: "PrefabRef",
        targetType: context?.targetType,
        isPrefabRefOverride: true,
        coordinateSpace: context?.coordinateSpace,
        resourceModule: context?.resourceModule,
          resourcePrefix: context?.resourcePrefix
      }, namingRules, model.warnings, sourcePath);

      if (semantics.exportOnly) {
        if (semantics.shouldSlice && !exportState.skipAssets) {
          const bounds = readPreviewBounds(layer, canvas);
          const id = "resource_" + hashText(sourcePath) + "_" + index;
          model.resources.push({
            id, ownerNodeId: prefabRefNode.id, name: sanitizeNodeName(semantics.nodeName || name), sourcePath,
            source: { layerId: readLayerIdQuiet(layer), resourceType: semantics.resourceType },
            resource: makePreviewResource(sourcePath, id, bounds, semantics, exportState, context?.resourcePrefix, canvas, context?.resourceModule)
          });
        }
        return;
      }
      if (semantics.prefabOverride) {
        const isImageOverride = !isGroup
          && semantics.nodeType === "Image"
          && semantics.shouldSlice
          && semantics.resourceType !== "DynamicTexture";
        const isTextOverride = !isGroup
          && semantics.nodeType === "Text";
        const element = sanitizeNodeName(semantics.nodeName || name);
        if (!isImageOverride && !isTextOverride) {
          model.warnings.push({
            code: "PSD_PREFAB_OVERRIDE_ELEMENT_TYPE_INVALID",
            severity: "Blocking",
            message: "@PrefabOverride is only valid on a static sliced @Image_ leaf or @Text_ leaf inside a PrefabRef design subtree.",
            nodeId: prefabRefNode.id,
            sourcePath
          });
        } else {
          const existing = arrayFrom(prefabRefNode.prefabRef.overrides).some((item) => (
            item.slot === semantics.prefabOverrideSlot
            && item.element === element
          ));
          if (existing) {
            model.warnings.push({
              code: "PSD_PREFAB_OVERRIDE_ELEMENT_DUPLICATE",
              severity: "Blocking",
              message: `PrefabRef replacement ${semantics.prefabOverrideSlot}.${element} is overridden more than once.`,
              nodeId: prefabRefNode.id,
              sourcePath
            });
          } else if (!exportState.skipAssets || isTextOverride) {
            const bounds = readPreviewBounds(layer, canvas);
            const override = {
              slot: semantics.prefabOverrideSlot,
              element,
              rect: makePrefabRefOverrideRect(
                prefabRefBounds,
                bounds,
                canvas
              )
            };
            if (isImageOverride) {
              const overrideId = `prefab_override_${readLayerIdQuiet(layer) || hashText(sourcePath)}`;
              override.resource = makePreviewResource(sourcePath, overrideId, bounds, semantics, exportState, context?.resourcePrefix, canvas, context?.resourceModule, makePrefabOverrideAssetName(prefabRefNode.name, semantics.prefabOverrideSlot, element, semantics.outputModule));
            } else {
              override.text = readTextInfoSync(layer, sourcePath, model.warnings) || {
                text: "",
                styleStatus: "PreviewOnly",
                tmpStatus: "Placeholder"
              };
            }
            prefabRefNode.prefabRef.overrides = arrayFrom(prefabRefNode.prefabRef.overrides);
            prefabRefNode.prefabRef.overrides.push(override);
          }
        }
      }

      if (!semantics.skipChildren && childLayers.length > 0) {
        appendPreviewPrefabOverrides(
          childLayers,
          sourcePath,
          prefabRefBounds,
          prefabRefNode,
          canvas,
          model,
          exportState,
          context
        );
      }
    });
  }

  function derivePreviewTextKeySeed(nodeName, textValue) {
    const fromName = normalizePreviewTextKeySeed(String(nodeName || "").replace(/Text$/i, ""));
    if (fromName) {
      return fromName;
    }

    return normalizePreviewTextKeySeed(textValue);
  }

  function normalizePreviewTextKeySeed(value) {
    return String(value || "")
      .trim()
      .replace(/([a-z0-9])([A-Z])/g, "$1_$2")
      .toLowerCase()
      .replace(/[\s\-\\/]+/g, "_")
      .replace(/[^a-z0-9_]/g, "")
      .replace(/_+/g, "_")
      .replace(/^_+|_+$/g, "");
  }

  function appendPreviewComponentMount(layer, parentId, parentPath, siblingIndex, canvas, model, exportState, context, targetRule, parsed, layerType) {
    const name = safeString(layer.name, `Component_${siblingIndex}`);
    const sourcePath = parentPath ? `${parentPath}/${name}` : name;
    const componentNaming = resolvePrefabRefNaming(parsed);
    const targetParsed = withPreviewTargetName(parsed, componentNaming.prefabName);
    const built = appendPreviewTargetFromRootLayer(layer, model.nodes.filter((node) => !node.parentId).length, canvas, model, exportState, targetRule, targetParsed, sourcePath, context?.resourcePrefix);
    if (!built || !canAppendPreviewNode(model, exportState)) {
      return;
    }

    const bounds = built.targetBounds || readPreviewBounds(layer, canvas);
    const mountId = makePreviewNodeId(`prefab_ref_${slugify(built.target.name)}`, sourcePath, siblingIndex, model.nodes);
    const mountKind = namingRules.behaviors?.component?.mountPrefabRefKind || "GeneratedTarget";
    model.nodes.push({
      id: mountId,
      parentId,
      name: sanitizeNodeName(componentNaming.variableName || built.target.name),
      type: "PrefabRef",
      active: readLayerVisible(layer),
      siblingIndex,
      sourcePath,
      source: {
        layerId: readLayerIdQuiet(layer),
        layerType,
        resourceType: "None",
        variant: getDefaultVariant(namingRules),
        zIndex: siblingIndex,
        naming: buildNamingSource(parsed, targetRule),
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
      rect: makePreviewRectInCoordinateSpace(bounds, context?.coordinateSpace, canvas)
    });

    model.warnings.push({
      code: namingRules.warningCodes?.componentTargetGenerated || "PSD_COMPONENT_TARGET_GENERATED",
      severity: "Info",
      message: `Component child Prefab under ${context?.targetType || "Target"} is shown as a generated target plus a PrefabRef mount.`,
      nodeId: mountId,
      sourcePath
    });
  }

  function appendPreviewListItemMount(layer, parentId, parentPath, siblingIndex, canvas, model, exportState, context, targetRule, parsed, layerType) {
    const name = safeString(layer.name, `ListItem_${siblingIndex}`);
    const sourcePath = parentPath ? `${parentPath}/${name}` : name;
    const built = appendPreviewTargetFromRootLayer(layer, model.nodes.filter((node) => !node.parentId).length, canvas, model, exportState, targetRule, parsed, sourcePath, context?.resourcePrefix);
    if (!built || !canAppendPreviewNode(model, exportState)) {
      return;
    }

    const bounds = built.targetBounds || readPreviewBounds(layer, canvas);
    const mountId = makePreviewNodeId(`prefab_ref_${slugify(built.target.name)}`, sourcePath, siblingIndex, model.nodes);
    const mountKind = namingRules.behaviors?.list?.mountPrefabRefKind || "GeneratedTarget";
    model.nodes.push({
      id: mountId,
      parentId,
      name: built.target.name,
      type: "PrefabRef",
      active: readLayerVisible(layer),
      siblingIndex,
      sourcePath,
      source: {
        layerId: readLayerIdQuiet(layer),
        layerType,
        resourceType: "None",
        variant: getDefaultVariant(namingRules),
        zIndex: siblingIndex,
        naming: buildNamingSource(parsed, targetRule),
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
      rect: makePreviewRectInCoordinateSpace(bounds, context?.coordinateSpace, canvas)
    });

    model.warnings.push({
      code: namingRules.warningCodes?.listItemTargetGenerated || "PSD_LISTITEM_TARGET_GENERATED",
      severity: "Info",
      message: "Configured ListItem placeholder is shown as a generated target plus a PrefabRef mount in the parent List.",
      nodeId: mountId,
      sourcePath
    });
  }

  function isComponentTargetRule(targetRule) {
    return Boolean(targetRule && (targetRule.targetType || targetRule.id) === "Component");
  }

  function withPreviewTargetName(parsed, targetName) {
    return {
      ...parsed,
      main: {
        ...parsed.main,
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

  function pushInvalidComponentTargetPreviewWarning(warnings, sourcePath, context) {
    const allowedTargetTypes = getComponentTargetParentTypes(namingRules);
    warnings.push({
      code: namingRules.warningCodes?.invalidFlagContext || "PSD_FLAG_CONTEXT_INVALID",
      severity: "Blocking",
      message: `@Component_ is only valid at PSD root or inside configured target subtrees (${allowedTargetTypes.join(", ")}). Current target: ${context?.targetType || "none"}.`,
      sourcePath
    });
  }

  function createPreviewCoordinateSpace(bounds, fallbackCanvas) {
    const snappedBounds = snapBounds(bounds.left, bounds.top, bounds.right, bounds.bottom, fallbackCanvas);
    return {
      left: snappedBounds.left,
      top: snappedBounds.top,
      canvas: {
        width: Math.max(1, snappedBounds.width),
        height: Math.max(1, snappedBounds.height)
      }
    };
  }

  function readPreviewTargetContentBounds(layer, canvas, sourcePath) {
    const childLayers = arrayFrom(layer.layers);
    let bounds = null;
    for (let i = 0; i < childLayers.length; i += 1) {
      const child = childLayers[i];
      const childName = safeString(child.name, `Layer_${i}`);
      const childPath = sourcePath ? `${sourcePath}/${childName}` : childName;
      bounds = mergePreviewBounds(bounds, readPreviewExportableSubtreeBounds(child, canvas, childPath));
    }

    return bounds || readPreviewBounds(layer, canvas);
  }

  function readPreviewListDisplayBounds(layer, canvas) {
    // Match export: ignored subtrees cannot contribute List geometry.
    const childLayers = arrayFrom(layer.layers);
    let bounds = null;
    for (let i = 0; i < childLayers.length; i += 1) {
      bounds = mergePreviewBounds(bounds, readPreviewExportableSubtreeBounds(childLayers[i], canvas, layer.name));
    }

    return bounds || { left: 0, top: 0, right: 0, bottom: 0 };
  }

  function readPreviewNonEmptySubtreeBounds(layer) {
    const childLayers = arrayFrom(layer.layers);
    let bounds = null;
    for (let i = 0; i < childLayers.length; i += 1) {
      bounds = mergePreviewBounds(bounds, readPreviewNonEmptySubtreeBounds(childLayers[i]));
    }

    if (bounds) {
      return bounds;
    }

    const layerBounds = parseBoundsQuiet(layer.boundsNoEffects || layer.bounds);
    return hasPositivePreviewArea(layerBounds) ? layerBounds : null;
  }

  function hasPositivePreviewArea(bounds) {
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

  function readPreviewExportableSubtreeBounds(layer, canvas, sourcePath) {
    const name = safeString(layer.name, sourcePath || "Layer");
    const parsed = parsePsdNaming(name, namingRules);
    if (isIgnoredByNaming(parsed) || parsed.options.some((option) => option.rule?.kind === "exportOnly")) {
      return null;
    }

    const childLayers = arrayFrom(layer.layers);
    let bounds = null;
    for (let i = 0; i < childLayers.length; i += 1) {
      const child = childLayers[i];
      const childName = safeString(child.name, `Layer_${i}`);
      const childPath = sourcePath ? `${sourcePath}/${childName}` : childName;
      bounds = mergePreviewBounds(bounds, readPreviewExportableSubtreeBounds(child, canvas, childPath));
    }

    return childLayers.length ? bounds : parseBoundsQuiet(layer.boundsNoEffects || layer.bounds);
  }

  function mergePreviewBounds(a, b) {
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

  function makePreviewRectInCoordinateSpace(bounds, coordinateSpace, fallbackCanvas) {
    if (!coordinateSpace) {
      return makeRect(bounds.left, bounds.top, bounds.right, bounds.bottom, fallbackCanvas);
    }

    const snappedBounds = snapBounds(bounds.left, bounds.top, bounds.right, bounds.bottom, fallbackCanvas);
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

  function finalizeBuiPreviewModel(model) {
    model.targets = finalizeTargets(model.targets, model.nodes);
    resolvePreviewPrefabRefs(model);
    appendUniqueWarnings(model.warnings, validateBuiPreviewChecklist(model));
    const assetsSkipped = Boolean(model.exportPlan?.assetsSkipped);
    const slices = assetsSkipped
      ? []
      : [...model.nodes, ...arrayFrom(model.resources)]
        .filter((node) => node.resource && node.resource.asset)
        .map((node) => ({
          sourcePath: node.sourcePath || node.name,
          asset: node.resource.asset,
          meta: `${node.resource.width || 0}x${node.resource.height || 0} · ${node.source?.resourceType || node.resource.resourceType || "Asset"}${node.resource.outputModule ? " · @Module_" + node.resource.outputModule : ""}`
        }));
    const blockingCount = model.warnings.filter((warning) => getWarningPriority(warning) === "blocking").length;
    model.exportPlan = {
      ...(model.exportPlan || {}),
      targetCount: model.targets.length,
      nodeCount: model.nodes.length,
      sliceCount: slices.length,
      warningCount: model.warnings.length,
      blockingCount,
      slices: slices.slice(0, PREVIEW_SLICE_LIMIT)
    };

    if (model.warnings.length > 80) {
      const omitted = model.warnings.length - 80;
      model.warnings = model.warnings.slice(0, 80);
      model.warnings.push({
        code: "PREVIEW_WARNINGS_TRUNCATED",
        severity: "Info",
        message: `${omitted} preview warnings were omitted from the panel preview. The real export will keep full warnings.`
      });
    }
    return model;
  }



  function resolvePreviewPrefabRefs(model) {
    const componentTargets = arrayFrom(model.targets).filter((target) => target?.targetType === "Component");
    for (const node of arrayFrom(model.nodes)) {
      if (node?.type !== "PrefabRef" || node?.prefabRef?.kind !== "ExternalPrefab" || Object.prototype.hasOwnProperty.call(node.prefabRef, "assetGuid")) {
        continue;
      }

      const prefabName = safeString(node.prefabRef.prefabName, "").trim();
      const matches = componentTargets.filter((target) => safeString(target?.name, "") === parseTargetIdentity(prefabName).name);
      if (matches.length !== 1) {
        continue;
      }

      const overrides = arrayFrom(node.prefabRef.overrides);
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
            model.warnings.push({
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
          const slotMatches = arrayFrom(model.nodes).filter((candidate) => (
            targetNodeIds.has(candidate?.id)
            && candidate?.type === expectedType
            && safeString(candidate?.name, "") === element
          ));
          if (slotMatches.length !== 1) {
            model.warnings.push({
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
  }


  function canAppendPreviewNode(model, exportState) {
    if (model.nodes.length < exportState.previewNodeLimit) {
      return true;
    }

    if (!exportState.previewTruncated) {
      exportState.previewTruncated = true;
      model.warnings.push({
        code: "PREVIEW_NODE_LIMIT_REACHED",
        severity: "Info",
        message: `BUI preview is limited to ${exportState.previewNodeLimit} nodes. The real export will continue traversing the full PSD.`
      });
    }
    return false;
  }

  function readPreviewBounds(layer, canvas) {
    return parseBoundsQuiet(layer.boundsNoEffects || layer.bounds) || {
      left: 0,
      top: 0,
      right: canvas.width,
      bottom: canvas.height
    };
  }

  function makePreviewResource(sourcePath, nodeId, bounds, semantics, exportState, resourcePrefix, fallbackCanvas, resourceModule, preferredName) {
    const snappedBounds = snapBounds(bounds.left, bounds.top, bounds.right, bounds.bottom, fallbackCanvas);
    const width = snappedBounds.width;
    const height = snappedBounds.height;
    const resource = {
      asset: makeAssetFileName(sourcePath, nodeId, exportState, preferredName || semantics.nodeName, resourcePrefix, semantics.outputModule, resourceModule),
      sha256: null,
      width,
      height,
      resourceType: semantics.resourceType || "Atlas",
      previewStatus: "planned"
    };

    if (semantics.nineSlice) {
      resource.border = semantics.nineSlice;
    }

    if (semantics.outputModule || resourceModule || exportState.moduleName) resource.outputModule = semantics.outputModule || resourceModule || exportState.moduleName;
    return resource;
  }

  function makePreviewNodeId(base, sourcePath, siblingIndex, nodes) {
    return makeUniqueNodeId(`${base}_${hashText(sourcePath).slice(0, 8)}_${siblingIndex}`, nodes);
  }

  function readLayerIdQuiet(layer) {
    const id = layer?.id ?? layer?._id ?? layer?.layerID ?? null;
    return id === null || id === undefined || id === "" ? null : String(id);
  }

}

function appendUniqueWarnings(target, additions) {
  const existing = new Set(arrayFrom(target).map(warningIdentity));
  for (const warning of arrayFrom(additions)) {
    const identity = warningIdentity(warning);
    if (existing.has(identity)) {
      continue;
    }
    existing.add(identity);
    target.push(warning);
  }
}

function warningIdentity(warning) {
  return [
    safeString(warning?.code, ""),
    safeString(warning?.sourcePath, ""),
    safeString(warning?.nodeId, ""),
    safeString(warning?.message, "")
  ].join("\n");
}

function assertPreviewHasNoBlockingWarnings(preview) {
  const blockingCount = toNumber(preview?.exportPlan?.blockingCount, 0);
  if (blockingCount <= 0) {
    return;
  }

  const codes = arrayFrom(preview?.warnings)
    .filter((warning) => getWarningPriority(warning) === "blocking")
    .map((warning) => warning.code)
    .filter(Boolean)
    .slice(0, 6)
    .join(", ");
  throw new Error(formatText(t("buiExportBlocked"), {
    count: blockingCount,
    codes: codes || "-"
  }));
}

function parseBoundsQuiet(bounds) {
  try {
    return parseBounds(bounds);
  } catch (err) {
    return null;
  }
}



async function selectSourcePsd() {
  try {
    const file = await fs.getFileForOpening({
      types: ["psd", "psb"]
    });

    if (!file) {
      return;
    }

    selectedPsdFile = file;
    const opened = await tryOpenPsdFile(file);
    if (!els.exportBaseName.value || hasDefaultExportBaseOverride(file.name)) {
      els.exportBaseName.value = defaultExportBaseName(file.name);
    }

    const parentFolder = await tryGetParentFolder(file);
    if (parentFolder) {
      selectedOutputFolder = parentFolder;
      derivedOutputFolderPath = displayEntry(parentFolder);
      outputFolderSource = "psd";
      updatePathField(els.outputFolder, displayEntry(parentFolder), t("outputPlaceholder"));
    } else {
      selectedOutputFolder = null;
      derivedOutputFolderPath = getFolderPathFromNativePath(displayEntry(file));
      outputFolderSource = "";
      updatePathField(els.outputFolder, derivedOutputFolderPath, t("outputPlaceholder"));
    }

    refreshPanelState(false);
    setStatus("ready");
    setSummaryFromKeys("summaryReadyTitle", "summaryReadyBody");
    log(formatText(t(opened ? "selectedAndOpenedPsd" : "selectedPsd"), { name: file.name }));
  } catch (err) {
    setStatus("failed", "error");
    setSummaryWithTitleKey("summaryFailedTitle", formatText(t("psdSelectionFailed"), { error: formatError(err) }), "error");
    log(formatText(t("psdSelectionFailed"), { error: formatError(err) }));
  }
}

async function tryOpenPsdFile(file) {
  if (!file || typeof app.open !== "function") {
    log(t("psdOpenUnavailable"));
    return false;
  }

  try {
    if (core && typeof core.executeAsModal === "function") {
      await core.executeAsModal(
        async () => {
          await app.open(file);
        },
        { commandName: "Burner PSD open" }
      );
    } else {
      await app.open(file);
    }
    return true;
  } catch (err) {
    log(formatText(t("psdOpenFailed"), { error: formatError(err) }));
    return false;
  }
}

async function selectOutputFolder() {
  try {
    const preferredPath = getOutputFolderDisplayPath() || getDocumentFolderPath(getActiveDocument());
    const initialFolder = await resolveFolderPickerInitialLocation();
    const initialPath = displayEntry(initialFolder);
    if (preferredPath && !initialFolder) {
      log(formatText(t("folderPickerInitialLocationUnavailable"), { path: preferredPath }));
    }
    log(initialPath ? formatText(t("folderPickerOpeningAt"), { path: initialPath }) : t("folderPickerOpening"));
    const folder = await openOutputFolderPicker(initialFolder);
    if (!folder) {
      log(t("folderSelectionCancelled"));
      return;
    }

    selectedOutputFolder = folder;
    derivedOutputFolderPath = displayEntry(folder);
    outputFolderSource = "manual";
    updatePathField(els.outputFolder, displayEntry(folder), t("outputPlaceholder"));
    renderBuiPreview();
    updateExportAvailability();
    setStatus("ready");
    setSummaryFromKeys("summaryReadyTitle", "summaryReadyBody");
    log(formatText(t("selectedFolder"), { name: displayEntry(folder) }));
  } catch (err) {
    setStatus("failed", "error");
    setSummaryWithTitleKey("summaryFailedTitle", formatText(t("folderSelectionFailed"), { error: formatError(err) }), "error");
    log(formatText(t("folderSelectionFailed"), { error: formatError(err) }));
  }
}

async function resolveFolderPickerInitialLocation() {
  if (selectedOutputFolder) {
    return selectedOutputFolder;
  }

  const path = derivedOutputFolderPath || getDocumentFolderPath(getActiveDocument());
  if (!path) {
    return null;
  }

  try {
    return await withTimeout(
      getFolderEntryFromNativePath(path),
      FOLDER_PICKER_INITIAL_TIMEOUT_MS,
      t("documentFolderAutoTimeout")
    );
  } catch (err) {
    return null;
  }
}

async function openOutputFolderPicker(initialFolder) {
  if (initialFolder) {
    try {
      return await fs.getFolder({ initialLocation: initialFolder });
    } catch (err) {
      log(formatText(t("folderPickerInitialLocationFailed"), { error: formatError(err) }));
    }
  }

  return fs.getFolder();
}

function capturePreviewInput() {
  return {
    exportBaseName: els.exportBaseName.value,
    sourceName: selectedPsdFile?.name,
    assetsSuffix: getAssetsSuffix(), buiExtension: getBuiExtension(),
    skipAssetsExport: isSkipAssetsExportEnabled(),
    outputFolderPath: getOutputFolderDisplayPath(),
    sliceFileNamePrefix: getSliceFileNamePrefix(), namingRules
  };
}

function createExportServices(input) {
  const readSetting = path => readExportSetting(path, input.exportSettings);
  const builders = createExportBuilders({
    readExportSetting: readSetting,
    assertSourceBeforeModal: input.assertSourceBeforeModal,
    writeOutput: operation => input.writeOutput(operation),
    log: input.log || (() => {}),
    makeAssetFileName: (...args) => makeAssetFileNameWithPrefix(input.sliceFileNamePrefix, ...args),
    getResourceNamingKey: (...args) => getResourceNamingKeyWithPrefix(input.sliceFileNamePrefix, ...args)
  });
  return {
    ...builders, ...createSourceInfoTools(() => readSetting("schemaPathHint")),
    formats, xmp: uxp.xmp, sha256Hex, t, formatText, formatError, toNumber, safeString,
    arrayFrom, normalizePathForCompare, getEntryIfExists, getCurrentPsdPath,
    readPreviewDocument, assertDocumentSaved, readTextInfo, assertPreviewHasNoBlockingWarnings, assertExportableBui,
    isNonEmptyString, isSha256Hex,
    appendUniqueWarnings, validateBuiChecklist, getWarningPriority,
    createBaselineWarnings: () => createBaselineWarnings(readSetting("schemaPathHint")),
    buildBuiPreviewModel: doc => buildBuiPreviewModel(doc, {
      ...input, assetsSuffix: readSetting("output.assetsSuffix"),
      buiExtension: readSetting("output.buiExtension"), outputFolderPath: input.outputFolder.nativePath
    })
  };
}

const exportRunner = globalThis.BurnerPsdToBuiExportRunner.create({ createServices: createExportServices });

async function exportCurrentDocument() {
  if (busy || exportRunner.running) {
    log(t("exportAlreadyRunning"));
    return;
  }

  setBusy(true);
  setStatus("running", "running");
  refreshDocumentState();
  log(t("exportPreparing"));

  try {
    let skipAssetsExport;
    const result = await exportRunner.runPrepared(async () => {
      const doc = getActiveDocument();
      if (!doc) {
        throw new Error(t("noActiveDocument"));
      }

      const documentFolderPath = getDocumentFolderPath(doc);
      if (!documentFolderPath) {
        throw new Error(t("noDocumentFolderPath"));
      }

      if (
        selectedOutputFolder &&
        normalizePathForCompare(displayEntry(selectedOutputFolder)) !== normalizePathForCompare(documentFolderPath)
      ) {
        selectedOutputFolder = null;
      }

      derivedOutputFolderPath = documentFolderPath;
      outputFolderSource = "documentPath";
      updatePathField(els.outputFolder, documentFolderPath, t("outputPlaceholder"));

      if (!selectedOutputFolder) {
        log(formatText(t("outputFolderAuthorizing"), { path: documentFolderPath }));
        selectedOutputFolder = await withTimeout(
          getFolderEntryFromNativePath(documentFolderPath),
          ENTRY_LOOKUP_TIMEOUT_MS,
          t("outputFolderAutoTimeout")
        );
        if (selectedOutputFolder) {
          updatePathField(els.outputFolder, displayEntry(selectedOutputFolder), t("outputPlaceholder"));
        }
      }

      await prefixController?.update();
      const validation = getExportValidation();
      if (!validation.canExport) {
        throw new Error(t(validation.reasonKey));
      }

      if (!selectedOutputFolder) {
        throw new Error(t("outputFolderAuthorizationRequired"));
      }

      const exportBaseName = sanitizeFileBase(
        els.exportBaseName.value
      );
      if (!exportBaseName) {
        throw new Error(t("noExportBaseName"));
      }
      skipAssetsExport = isSkipAssetsExportEnabled();
      const activeNamingRules = await getNamingRules();
      namingRules = activeNamingRules;
      const currentPsdPath = getCurrentPsdPath(doc);
      let sourcePsdFile = selectedPsdFile;
      if (!sourcePsdFile && currentPsdPath) {
        try {
          sourcePsdFile = await withTimeout(getFileEntryFromNativePath(currentPsdPath), ENTRY_LOOKUP_TIMEOUT_MS, t("psdFileAutoTimeout"));
        } catch (err) {
          log(formatText(t("psdFileAutoFailed"), { error: formatError(err) }));
        }
      }
      return {
        document: doc, sourcePsdFile, outputFolder: selectedOutputFolder,
        reusePolicy: els.retainReuse?.checked ? "retain" : "current-psd",
        exportBaseName, skipAssetsExport, sliceFileNamePrefix: getSliceFileNamePrefix(),
        exportSettings, namingRules: activeNamingRules, lastCompletedExportTargetKey,
        confirmOverwrite, log, onPreview: () => renderBuiPreview(), onWarnings: renderPreviewWarnings
      };
    });
    if (result.status === "cancelled") {
      setStatus("ready");
      setSummaryFromKeys("summaryReadyTitle", "summaryReadyBody");
      log(t("exportCancelled"));
      return result;
    }
    const { buiFileName, assetsRootName } = result;
    renderBuiPreview();
    setStatus("done", "done");
    setSummaryWithTitleKey(
      "summaryExportedTitle",
      skipAssetsExport ? t("summaryExportedBuiOnlyBody") : `${buiFileName} / ${assetsRootName}`
    );
    log(formatText(t("exportedFile"), { name: buiFileName }));
    if (skipAssetsExport) {
      log(t("assetsExportSkipped"));
    } else {
      log(formatText(t("assetsFolder"), { name: assetsRootName }));
    }
    lastCompletedExportTargetKey = result.targetKey;
    return result;
  } catch (err) {
    setStatus("failed", "error");
    setSummaryWithTitleKey("summaryFailedTitle", formatText(t("exportFailed"), { error: formatError(err) }), "error");
    log(formatText(t("exportFailed"), { error: formatError(err) }));
  } finally {
    setBusy(false);
  }
}

function confirmOverwrite(conflicts, buiFileName, assetsRootName, options = {}) {
  const skipAssets = Boolean(options.skipAssets);
  const title = t("overwriteTitle");
  const body = skipAssets ? t("overwriteBodyBuiOnly") : t("overwriteBody");
  const meta = skipAssets ? `${buiFileName} / ${t("assetsExportSkippedShort")}` : `${buiFileName} / ${assetsRootName}`;
  return showConfirmDialog({
    title,
    body,
    details: conflicts,
    confirmLabel: skipAssets ? t("overwriteConfirmButtonBuiOnly") : t("overwriteConfirmButton"),
    cancelLabel: t("cancel"),
    meta
  });
}

function showConfirmDialog(options) {
  return new Promise((resolve) => {
    document.querySelectorAll(".modal-confirm-dialog, .confirm-overlay").forEach((node) => {
      if (typeof node.close === "function") {
        try {
          node.close("cancel");
        } catch (err) {}
      }
      node.remove();
    });

    const dialog = document.createElement("dialog");
    dialog.className = "modal-confirm-dialog";
    dialog.setAttribute("aria-label", options.title || "");

    const usesUxpDialog = typeof dialog.uxpShowModal === "function";
    const panel = document.createElement("div");
    panel.className = "modal-confirm-panel";

    if (!usesUxpDialog) {
      const title = document.createElement("div");
      title.className = "confirm-title";
      title.textContent = options.title || "";
      panel.appendChild(title);
    }

    const body = document.createElement("div");
    body.className = "confirm-body";
    body.textContent = options.body || "";
    panel.appendChild(body);

    const meta = document.createElement("div");
    meta.className = "confirm-meta";
    meta.textContent = options.meta || "";
    if (options.meta) {
      panel.appendChild(meta);
    }

    const list = document.createElement("div");
    list.className = "confirm-list";
    arrayFrom(options.details).forEach((item) => {
      const row = document.createElement("div");
      row.textContent = item;
      list.appendChild(row);
    });
    panel.appendChild(list);

    const actions = document.createElement("div");
    actions.className = "confirm-actions";

    const cancelButton = document.createElement("button");
    cancelButton.className = "confirm-control confirm-cancel";
    cancelButton.type = "button";
    cancelButton.textContent = options.cancelLabel || "Cancel";

    const okButton = document.createElement("button");
    okButton.className = "confirm-control confirm-ok";
    okButton.type = "button";
    okButton.textContent = options.confirmLabel || "OK";

    actions.appendChild(cancelButton);
    actions.appendChild(okButton);
    panel.appendChild(actions);
    dialog.appendChild(panel);
    document.body.appendChild(dialog);

    let resolved = false;
    const cleanup = (value) => {
      if (resolved) {
        return;
      }
      resolved = true;
      document.removeEventListener("keydown", onConfirmKeydown, true);
      cancelButton.removeEventListener("click", onCancelClick);
      okButton.removeEventListener("click", onOkClick);
      dialog.removeEventListener("cancel", onDialogCancel);
      dialog.removeEventListener("close", onDialogClose);
      if (typeof dialog.close === "function") {
        try {
          dialog.close(value ? "ok" : "cancel");
        } catch (err) {}
      }
      dialog.remove();
      resolve(value);
    };

    cancelButton.addEventListener("click", onCancelClick);
    okButton.addEventListener("click", onOkClick);
    dialog.addEventListener("cancel", onDialogCancel);
    dialog.addEventListener("close", onDialogClose);

    document.addEventListener("keydown", onConfirmKeydown, true);
    try {
      if (usesUxpDialog) {
        Promise.resolve(
          dialog.uxpShowModal({
            title: options.title || "",
            resize: "none",
            size: { width: 640, height: 340 }
          })
        ).then(() => {
          cleanup(dialog.returnValue === "ok");
        }, () => cleanup(false));
      } else if (typeof dialog.showModal === "function") {
        dialog.showModal();
      } else {
        dialog.setAttribute("open", "open");
        dialog.classList.add("is-fallback");
      }
    } catch (err) {
      cleanup(false);
      return;
    }
    try {
      cancelButton.focus();
    } catch (err) {}
    log(t("overwriteConfirmVisible"));

    function onConfirmKeydown(event) {
      if (!document.body.contains(dialog)) {
        document.removeEventListener("keydown", onConfirmKeydown, true);
        return;
      }
      if (event.key === "Escape") {
        event.preventDefault();
        document.removeEventListener("keydown", onConfirmKeydown, true);
        cleanup(false);
      }
    }

    function onCancelClick() {
      cleanup(false);
    }

    function onOkClick() {
      cleanup(true);
    }

    function onDialogCancel(event) {
      event.preventDefault();
      cleanup(false);
    }

    function onDialogClose() {
      cleanup(dialog.returnValue === "ok");
    }
  });
}

async function getEntryIfExists(folder, name) {
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
  } catch (err) {
    // UXP hosts differ here; fall back to a directory scan.
  }

  try {
    if (typeof folder.getEntries !== "function") {
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
    return null;
  }
}

async function clearFolderContents(folder) {
  const entries = typeof folder.getEntries === "function" ? await folder.getEntries() : [];
  for (const entry of entries) {
    await deleteEntry(entry);
  }
}

async function deleteEntry(entry) {
  if (!entry) {
    return;
  }

  if (entry.isFolder && typeof entry.getEntries === "function") {
    await clearFolderContents(entry);
  }

  if (typeof entry.delete === "function") {
    await entry.delete();
    return;
  }

  if (typeof entry.remove === "function") {
    await entry.remove();
    return;
  }

  throw new Error(formatText(t("deleteEntryUnavailable"), { name: entry.name || "" }));
}

function getActiveDocument() {
  try {
    if (app.documents && app.documents.length > 0) {
      return app.activeDocument || app.documents[0] || null;
    }
  } catch (err) {
    // Fall through to the direct activeDocument probe.
  }

  try {
    return app.activeDocument || null;
  } catch (err) {
    return null;
  }
}

function createBaselineWarnings(schemaPath = getSchemaPathHint()) {
  return [
    {
      code: "BUI_SCHEMA_REFERENCE_ONLY",
      severity: "Warning",
      message: `Schema reference is ${schemaPath}. This plugin runs a lightweight required-field checklist, but full JSON Schema validation remains outside the UXP sandbox.`
    },
    {
      code: "UXP_FIELD_COVERAGE_FIRST_PASS",
      severity: "Warning",
      message: "layer id, text style, masks, layer effects and smart objects are best-effort or unknown in this first pass and are recorded as warnings instead of trusted facts."
    }
  ];
}

async function getNamingRules() {
  if (!namingRulesPromise) {
    namingRulesPromise = configLoader.loadPluginJsonConfig(NAMING_RULES_CONFIG_PATH).then((loaded) => ({
      ...loaded,
      loadStatus: "plugin-package",
      configPath: NAMING_RULES_CONFIG_PATH
    }));
  }

  return namingRulesPromise;
}

async function getI18n() {
  if (!i18nPromise) {
    i18nPromise = configLoader.loadI18nConfig(I18N_CONFIG_PATH);
  }

  return i18nPromise;
}

async function getExportSettings() {
  if (!exportSettingsPromise) {
    exportSettingsPromise = configLoader.loadExportSettingsConfig(EXPORT_SETTINGS_CONFIG_PATH);
  }

  return exportSettingsPromise;
}

async function ensureFolder(parentFolder, name) {
  try {
    const existing = await parentFolder.getEntry(name);
    if (existing && existing.isFolder) {
      return existing;
    }
  } catch (err) {
    // Missing folder is the normal creation path.
  }

  return parentFolder.createFolder(name);
}

async function tryGetParentFolder(file) {
  try {
    if (file.parent) {
      return file.parent;
    }
  } catch (err) {
    // Continue with method-based parent lookup.
  }

  try {
    if (typeof file.getParent === "function") {
      return await file.getParent();
    }
  } catch (err) {
    // Some host entries do not expose a parent folder.
  }

  return null;
}

async function sha256Hex(bytes) {
  if (!bytes) {
    return null;
  }

  const input = toUint8Array(bytes);
  const digestFn = globalThis.crypto?.subtle?.digest;
  if (!digestFn) {
    return sha256HexFallback(input);
  }

  const buffer = input.buffer.slice(input.byteOffset, input.byteOffset + input.byteLength);
  const digest = await digestFn.call(globalThis.crypto.subtle, "SHA-256", buffer);
  return bytesToHex(new Uint8Array(digest));
}

function defaultExportBaseName(fileName, settings = exportSettings) {
  const override = getDefaultExportBaseOverride(fileName, settings);
  return override?.exportBaseName || stripExtension(fileName);
}

function hasDefaultExportBaseOverride(fileName) {
  return Boolean(getDefaultExportBaseOverride(fileName));
}

function getDefaultExportBaseOverride(fileName, settings = exportSettings) {
  const normalized = safeString(fileName, "").toLowerCase();
  return arrayFrom(settings?.defaultExportBaseOverrides)
    .find((item) => safeString(item.psdName, "").toLowerCase() === normalized) || null;
}

function getBuiExtension() {
  return readExportSetting("output.buiExtension");
}

function getAssetsSuffix() {
  return readExportSetting("output.assetsSuffix");
}

function getSchemaPathHint() {
  return readExportSetting("schemaPathHint");
}

function readExportSetting(path, settings = exportSettings) {
  const parts = safeString(path, "").split(".").filter(Boolean);
  let value = settings;
  for (const part of parts) {
    value = value?.[part];
  }

  if (value === undefined || value === null || value === "") {
    throw new Error(`${EXPORT_SETTINGS_CONFIG_PATH} is missing required setting: ${path}`);
  }

  return value;
}

function parseTargetIdentity(value, targetType = "Component") {
  const name = safeString(value, "");
  const match = /^UI([A-Z][A-Za-z0-9]{0,15})_([A-Z][A-Za-z0-9]*)$/.exec(name);
  if (!match) return { name, module: "" };
  const suffix = targetType === "ListItem" ? "Item" : targetType;
  let functionName = match[2];
  if (functionName.endsWith(suffix) && functionName.length > suffix.length) functionName = functionName.slice(0, -suffix.length);
  return { name: "UI" + match[1] + functionName + suffix, module: match[1] };
}

function readPsdDirectoryModule(psdPath) {
  const parts = safeString(psdPath, "").replace(/\\/g, "/").split("/").filter(Boolean);
  const moduleName = parts.length > 1 ? parts[parts.length - 2] : "";
  if (!/^[A-Za-z0-9_][A-Za-z0-9_.-]*$/.test(moduleName) || moduleName.endsWith(".") ||
      /^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])($|\.)/i.test(moduleName))
    return ""; // Explicit Target modules work even for an unsaved document; legacy names retain their old fallback.
  return moduleName;
}

function deriveResourceModuleName(targetName, moduleName) {
  let name = safeString(targetName, "").trim();
  if (!name) {
    return "";
  }

  if (name.startsWith("UI") && name.length > 2 && /[A-Z]/.test(name.charAt(2))) {
    name = name.slice(2);
  }

  const suffixes = moduleName
    ? ["ItemAuto", "ListItem", "Component", "Popup", "PopUp", "Page", "Panel", "View", "Embed", "Item", "Cell"]
    : ["MainPage", "Page", "Popup", "PopUp", "Panel", "View", "ItemAuto", "Item"];
  for (const suffix of suffixes) {
    if (name.endsWith(suffix) && name.length > suffix.length) {
      name = name.slice(0, name.length - suffix.length);
      break;
    }
  }

  if (moduleName) {
    if (!name.toLowerCase().startsWith(moduleName.toLowerCase()) ||
        (name.length > moduleName.length && !/^[_A-Z0-9]/.test(name.slice(moduleName.length))))
      throw new Error("目标名必须以 PSD 所属模块开头 / Target module does not match PSD directory: " + targetName + " / " + moduleName);
    const functionName = name.slice(moduleName.length).replace(/^_+/, "");
    const words = functionName.match(/[A-Z]+(?=[A-Z][a-z]|[0-9]|$)|[A-Z]?[a-z]+|[0-9]+/g) || [];
    const abbreviation = words.map(word => /^[0-9]+$/.test(word) ? word : word.charAt(0).toUpperCase()).join("");
    return toUiAssetNamePart(moduleName) + (abbreviation ? "_" + abbreviation : "");
  }
  return toUiAssetNamePart(name);
}

function makeAssetFileName(...args) {
  return makeAssetFileNameWithPrefix(getSliceFileNamePrefix(), ...args);
}

function getResourceNamingKey(...args) {
  return getResourceNamingKeyWithPrefix(getSliceFileNamePrefix(), ...args);
}

function makeAssetFileNameWithPrefix(filePrefix, sourcePath, nodeId, exportState, preferredName, resourcePrefix, outputModule, ownerModule) {
  const parts = safeString(sourcePath, nodeId).split("/");
  const rawName = preferredName || parts[parts.length - 1] || nodeId;
  const candidate = makeUiAssetFileBase(rawName, resourcePrefix, outputModule, filePrefix).slice(0, 96) || "Layer";
  // Explicit modules share a stable name; the PNG writer verifies content before reuse.
  if (outputModule || ownerModule || exportState.moduleName) {
    exportState.usedAssetNames = exportState.usedAssetNames || {};
    exportState.usedAssetNames[(candidate + ".png").toLowerCase()] = true;
    return candidate + ".png";
  }
  let unique = candidate;
  let index = 1;
  exportState.usedAssetNames = exportState.usedAssetNames || {};
  while (exportState.usedAssetNames[`${unique}.png`.toLowerCase()]) {
    unique = `${candidate}_${String(index).padStart(2, "0")}`;
    index += 1;
  }
  exportState.usedAssetNames[`${unique}.png`.toLowerCase()] = true;
  return `${unique}.png`;
}

function getResourceNamingKeyWithPrefix(filePrefix, sourcePath, nodeId, preferredName, resourcePrefix, outputModule) {
  const rawName = preferredName || safeString(sourcePath, nodeId).split("/").pop() || nodeId;
  return JSON.stringify([4, filePrefix, outputModule || "",
    makeUiAssetFileBase(rawName, resourcePrefix, outputModule, filePrefix).slice(0, 96)]);
}

function loadSliceFileNamePrefix() {
  try {
    const saved = JSON.parse(localStorage.getItem(STORAGE_EXPORT_SETTINGS_KEY) || "{}");
    if (typeof saved.sliceFileNamePrefix === "string") return saved.sliceFileNamePrefix;
  } catch (err) { /* Keep the bundled default when no local preference exists. */ }
  return exportSettings?.output?.sliceFileNamePrefix ?? "UI_";
}

function getSliceFileNamePrefix() {
  return els?.sliceFileNamePrefix?.value ?? exportSettings?.output?.sliceFileNamePrefix ?? "UI_";
}

function isValidSliceFileNamePrefix(prefix) {
  return typeof prefix === "string" && /^[A-Za-z0-9_]{0,32}$/.test(prefix);
}

function makeUiAssetFileBase(rawName, resourcePrefix, outputModule, filePrefix = getSliceFileNamePrefix()) {
  if (!isValidSliceFileNamePrefix(filePrefix)) throw new Error(t("invalidSliceFileNamePrefix"));
  const rawBase = sanitizeFileBase(rawName).replace(/_+/g, "_").replace(/^_+|_+$/g, "");
  const moduleName = toUiAssetNamePart(outputModule || resourcePrefix);
  const module = moduleName;
  if (outputModule) {
    let semanticName = rawBase;
    if (filePrefix && semanticName.toLowerCase().startsWith(filePrefix.toLowerCase()))
      semanticName = semanticName.slice(filePrefix.length);
    else if (/^UI_/i.test(semanticName)) semanticName = semanticName.slice(3);
    // Strip exactly one leading module, only at a word boundary (Commonwealth stays intact).
    for (const prefix of [moduleName]) {
      if (!prefix || !semanticName.toLowerCase().startsWith(prefix.toLowerCase())) continue;
      const tail = semanticName.slice(prefix.length);
      if (!tail || /^[_A-Z0-9]/.test(tail)) {
        semanticName = tail.replace(/^_+/, "");
        break;
      }
    }
    return filePrefix + module + (semanticName ? "_" + toUiAssetNamePart(semanticName) : "");
  }
  // Preserve the literal configured header, including its trailing underscore.
  if (filePrefix && rawBase.toLowerCase().startsWith(filePrefix.toLowerCase()))
    return filePrefix + (toUiAssetNamePart(rawBase.slice(filePrefix.length)) || "Layer");
  const base = toUiAssetNamePart(rawBase) || "Layer";
  const normalizedBase = base.replace(/^Ui_/, "UI_");
  // Existing explicitly named UI resources retain their tail when selecting a new header.
  if (/^UI_/.test(normalizedBase)) return filePrefix + normalizedBase.slice(3);
  return module ? `${filePrefix}${module}_${base}` : (filePrefix === "UI_" ? base : filePrefix + base);
}

function toUiAssetNamePart(value) {
  const text = safeString(value, "")
    .trim()
    .replace(/[^A-Za-z0-9]+/g, "_")
    .replace(/_+/g, "_")
    .replace(/^_+|_+$/g, "");
  if (!text) {
    return "";
  }

  return text
    .split("_")
    .filter(Boolean)
    .map(formatUiAssetWord)
    .join("_");
}

function formatUiAssetWord(word) {
  const lower = word.toLowerCase();
  if (lower === "ui") {
    return "UI";
  }
  if (lower === "hp") {
    return "HP";
  }
  if (/^[A-Z0-9]{2,}$/.test(word)) {
    return word;
  }
  return word.charAt(0).toUpperCase() + word.slice(1);
}

function sanitizeNodeName(name) {
  return safeString(name, "Layer")
    .trim()
    .replace(/[\\/:*?"<>|]+/g, "_") || "Layer";
}

function sanitizeId(value) {
  return safeString(value, "")
    .replace(/[^A-Za-z0-9_]+/g, "_")
    .replace(/^_+|_+$/g, "") || "unknown";
}

// Optional file bridge: startup and storage errors do not block the manual panel.
function bridgeNonce() {
  return Array.from({length: 32}, () => Math.floor(Math.random() * 16).toString(16)).join("");
}
let bridgeUiState = {enabled: true, state: "initializing"};
function renderBridgeStatus(value) {
  bridgeUiState = {...bridgeUiState, ...value};
  const node = typeof document === "undefined" ? null : document.getElementById("bridgeStatus");
  if (node) node.textContent = [value.state, value.directory, value.error].filter(Boolean).join(" · ");
  const button = typeof document === "undefined" ? null : document.getElementById("bridgeToggleBtn");
  if (button) {
    button.textContent = t(bridgeUiState.enabled ? "bridgeDisable" : "bridgeEnable");
    button.disabled = bridgeUiState.state === "busy";
  }
}
async function inspectForBridge(params) {
  if (Object.keys(params).length) throw Object.assign(new Error("inspect takes no parameters"), {code: "INVALID_PARAMS"});
  if (busy || exportRunner.running) throw Object.assign(new Error("Export is running"), {code: "BUSY"});
  const doc = getActiveDocument();
  const result = {protocolVersion: 1, pluginVersion: ADAPTER_VERSION, operations: ["inspect", "export"],
    parameters: {inspect: [], export: ["document", "expectedTargetIds", "options", "outputPolicy"]}, document: null, targets: []};
  if (!doc) return result;
  const documentId = doc.id, documentPath = getCurrentPsdPath(doc);
  let saved;
  try {saved = await assertDocumentSaved(doc, "bridge-inspect");}
  catch (error) {
    if (!String(error.message).startsWith("PSD_UNSAVED:")) throw error;
    saved = false;
  }
  const [snapshot, rules, settings] = await Promise.all([readPreviewDocument(doc), getNamingRules(), getExportSettings()]);
  if (getActiveDocument()?.id !== documentId || getCurrentPsdPath(doc) !== documentPath)
    throw Object.assign(new Error("Active document changed during inspect"), {code: "DOCUMENT_MISMATCH"});
  const exportBaseName = defaultExportBaseName(snapshot.name, settings);
  let model = buildBuiPreviewModel(snapshot, {exportBaseName, sourceName: snapshot.name, nodeLimit: 20000,
    assetsSuffix: readExportSetting("output.assetsSuffix", settings), buiExtension: readExportSetting("output.buiExtension", settings),
    skipAssetsExport: false, outputFolderPath: getFolderPathFromNativePath(documentPath), sliceFileNamePrefix: "", namingRules: rules});
  if (model.warnings.some(item => item.code === "PREVIEW_NODE_LIMIT_REACHED"))
    throw Object.assign(new Error("Target inspection exceeded 20000 nodes; do not export a partial Target set."), {code: "TARGET_INSPECTION_LIMIT"});
  const sourceFile = documentPath ? await getFileEntryFromNativePath(documentPath) : null;
  const sourceHash = sourceFile ? await sha256Hex(await sourceFile.read({format: formats.binary})) : null;
  let adaptationInput = null;
  if (sourceFile && saved) {
    const folder = await getFolderEntryFromNativePath(getFolderPathFromNativePath(documentPath));
    const reuse = globalThis.BurnerPsdToBuiResourceReuse.create({xmp:uxp.xmp,formats,sha256Hex});
    adaptationInput = await reuse.inspect(sourceFile, folder, {psdSha256:sourceHash}, true);
    if (adaptationInput.adaptation) {
      const services = createExportServices({document:doc,outputFolder:folder,exportBaseName,exportSettings:settings,namingRules:rules,sliceFileNamePrefix:""});
      const warnings = services.createBaselineWarnings(), canvas = services.buildCanvasInfo(snapshot, warnings);
      const built = await services.buildNodes(snapshot, canvas, exportBaseName, warnings, null, rules,
        {skipAssets:true,adaptationReadOnly:true,adaptation:adaptationInput.adaptation,psdSha256:sourceHash});
      model = {...built,warnings};
      await reuse.verifyUnchanged(adaptationInput, sourceFile, {psdSha256:sourceHash}, true);
    }
  }
  if (model.warnings.some(item => item.code === "BUI_V4_PRODUCTION_NOT_READY"))
    throw Object.assign(new Error("SubPage inspection requires a valid saved Rename adaptation handoff; no partial target set is returned."), {code:"ADAPTATION_HANDOFF_REQUIRED"});
  if (getActiveDocument()?.id !== documentId || getCurrentPsdPath(doc) !== documentPath)
    throw Object.assign(new Error("Document changed while reading source bytes"), {code: "DOCUMENT_MISMATCH"});
  try {saved = await assertDocumentSaved(doc, "bridge-inspect-complete");}
  catch (error) {if (!String(error.message).startsWith("PSD_UNSAVED:")) throw error; saved = false;}
  result.document = {id: documentId, path: documentPath, saved, name: snapshot.name, sha256: sourceHash};
  result.exportDefaults = {mode: "bui-png", exportBaseName, sliceFileNamePrefix: settings.output.sliceFileNamePrefix, reusePolicy: adaptationInput?.adaptation ? "retain" : "current-psd"};
  result.configuration = {namingRulesSha256: await sha256Hex(globalThis.BurnerPsdToBuiResourceReuse.encodeUtf8(JSON.stringify(rules))),
    exportSettingsSha256: await sha256Hex(globalThis.BurnerPsdToBuiResourceReuse.encodeUtf8(JSON.stringify(settings))),
    namingRules: rules, exportSettings: settings};
  result.targets = model.targets;
  result.warnings = model.warnings.map(item => ({code: item.code, severity: item.severity}));
  result.previewTruncated = model.warnings.some(item => item.code === "PREVIEW_NODE_LIMIT_REACHED");
  return result;
}
const aiExport = globalThis.BurnerPsdToBuiBridgeExport.create({runner: exportRunner, getActiveDocument, getDocumentPath: getCurrentPsdPath,
  normalizePath: normalizePathForCompare, folderPath: getFolderPathFromNativePath,
  getFile: getFileEntryFromNativePath, getFolder: getFolderEntryFromNativePath, getRules: getNamingRules, getSettings: getExportSettings,
  assertSaved: assertDocumentSaved, hash: sha256Hex, encode: globalThis.BurnerPsdToBuiResourceReuse.encodeUtf8, version: ADAPTER_VERSION,
  isBusy: () => busy, setBusy: value => {if (initialized) setBusy(value); else busy = Boolean(value);},
  feedback: result => {if (result.productSaved) lastCompletedExportTargetKey = result.targetKey;}});
const aiBridgeHost = globalThis.BurnerPsdToBuiBridgeHost.create({fs, formats, version: ADAPTER_VERSION,
  handlers: {inspect: inspectForBridge, export: aiExport.exportDocument}, hash: sha256Hex, nonce: bridgeNonce, status: renderBridgeStatus,
  getFolder: getFolderEntryFromNativePath, isBusy: () => busy || exportRunner.running,
  getWorkingPaths: async () => arrayFrom(app.documents).map(getCurrentPsdPath).filter(Boolean),
  getWorkingDocument: async () => {
    const doc = getActiveDocument(), path = doc ? getCurrentPsdPath(doc) : null;
    if (!path) return null;
    try {await assertDocumentSaved(doc, "bridge-route");}
    catch (error) {if (String(error.message).startsWith("PSD_UNSAVED:")) return {path, saved: false}; throw error;}
    return {path, saved: true};
  }});
void aiBridgeHost.initialize();
