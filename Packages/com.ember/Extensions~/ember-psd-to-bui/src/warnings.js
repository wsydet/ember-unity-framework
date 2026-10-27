/* global globalThis */

(function registerWarningTools(global) {
  const BLOCKING_CODES = [
    "PSD_NO_EXPORTABLE_ROOT_TARGET",
    "PSD_LISTITEM_TARGET_CONTEXT_MISSING",
    "LAYER_PNG_EXPORT_FAILED",
    "DOCUMENT_PREVIEW_EXPORT_FAILED",
    "PSD_NAMING_RULES_CONFIG_LOAD_FAILED",
    "DOCUMENT_LAYERS_UNAVAILABLE",
    "CANVAS_SIZE_UNAVAILABLE",
    "PSD_FILE_ENTRY_NOT_SELECTED",
    "PSD_METADATA_UNAVAILABLE",
    "PSD_SHA256_UNAVAILABLE",
    "ASSET_SHA256_UNAVAILABLE",
    "BUI_SCHEMA_CHECKLIST_FAILED"
  ];

  const HINT_CODES = [
    "TEXT_STYLE_UNCERTAIN",
    "TEXT_FONT_SIZE_UNAVAILABLE",
    "SMART_OBJECT_UNCERTAIN",
    "LAYER_MASK_UNCERTAIN",
    "LAYER_EFFECTS_UNCERTAIN",
    "BUI_SCHEMA_REFERENCE_ONLY",
    "UXP_FIELD_COVERAGE_FIRST_PASS"
  ];

  function createWarningTools(options) {
    const t = options.t;
    const formatText = options.formatText;
    const capitalizeAscii = options.capitalizeAscii;
    const safeString = options.safeString;
    const arrayFrom = options.arrayFrom;

    function getPriority(warning) {
      const severity = warning?.severity || "Warning";
      const code = warning?.code || "";

      if (severity === "Blocking" || severity === "Error") {
        return "blocking";
      }

      if (BLOCKING_CODES.includes(code)) {
        return "blocking";
      }

      if (HINT_CODES.includes(code)) {
        return "hint";
      }

      if (severity === "Info" || code === "PSD_IGNORE_MARK_APPLIED") {
        return "info";
      }

      return "warning";
    }

    function getPriorityCounts(warnings) {
      const counts = {
        blocking: 0,
        warning: 0,
        hint: 0,
        info: 0
      };

      arrayFrom(warnings).forEach((warning) => {
        const priority = getPriority(warning);
        counts[priority] = (counts[priority] || 0) + 1;
      });

      return counts;
    }

    function formatPrioritySummary(warnings) {
      return formatText(t("warningSummaryByPriority"), getPriorityCounts(warnings));
    }

    function getPriorityRank(warning) {
      const rank = {
        blocking: 0,
        warning: 1,
        hint: 2,
        info: 3
      };
      return rank[getPriority(warning)] ?? 9;
    }

    function sortForDisplay(warnings) {
      return arrayFrom(warnings)
        .map((warning, index) => ({ warning, index }))
        .sort((a, b) => {
          const rankA = getPriorityRank(a.warning);
          const rankB = getPriorityRank(b.warning);
          return rankA === rankB ? a.index - b.index : rankA - rankB;
        })
        .map((item) => item.warning);
    }

    function groupedForDisplay(warnings) {
      const groups = [
        { priority: "blocking", label: t("warningGroupBlocking"), items: [] },
        { priority: "warning", label: t("warningGroupWarning"), items: [] },
        { priority: "hint", label: t("warningGroupHint"), items: [] },
        { priority: "info", label: t("warningGroupInfo"), items: [] }
      ];
      const byPriority = groups.reduce((map, group) => {
        map[group.priority] = group;
        return map;
      }, {});

      arrayFrom(warnings).forEach((warning) => {
        const priority = getPriority(warning);
        (byPriority[priority] || byPriority.warning).items.push(warning);
      });

      return groups.filter((group) => group.items.length > 0);
    }

    function getPriorityLabel(warning) {
      return t(`warningPriority${capitalizeAscii(getPriority(warning))}`);
    }

    function getLocalizedSeverity(severity) {
      const key = capitalizeAscii(safeString(severity, "Warning").toLowerCase());
      const label = t(`severity${key}`);
      return label === `severity${key}` ? key : label;
    }

    function getConfiguredMessage(code) {
      const tables = typeof options.getI18nTable === "function" ? options.getI18nTable() : {};
      const lang = typeof options.getCurrentLang === "function" ? options.getCurrentLang() : "zh";
      const current = tables?.[lang] || {};
      return current.warningMessages?.[code] || "";
    }

    function getLocalizedMessage(warning) {
      if (!warning) {
        return "";
      }

      const code = safeString(warning.code, "");
      const original = safeString(warning.message, "");
      const configured = getConfiguredMessage(code);
      return configured ? formatText(configured, { original }) : original;
    }

    function formatForCopy(warning) {
      if (!warning) {
        return "";
      }

      const message = getLocalizedMessage(warning);

      return [
        `[${getPriorityLabel(warning)}] ${warning.code || ""}`.trim(),
        `${t("warningPathLabel")}: ${warning.sourcePath || t("documentRoot")}`,
        warning.nodeId ? `${t("warningNodeLabel")}: ${warning.nodeId}` : "",
        message ? `${t("warningMessageLabel")}: ${message}` : ""
      ].filter(Boolean).join("\n");
    }

    return {
      formatPrioritySummary,
      getPriority,
      getPriorityCounts,
      getPriorityLabel,
      getPriorityRank,
      getLocalizedMessage,
      getLocalizedSeverity,
      sortForDisplay,
      groupedForDisplay,
      formatForCopy
    };
  }

  global.BurnerPsdToBuiWarningTools = {
    createWarningTools
  };
})(globalThis);
