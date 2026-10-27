/* global globalThis */

(function registerConfigLoader(global) {
  function createConfigLoader(options) {
    const fs = options.fs;
    const formats = options.formats;
    const safeString = options.safeString;

    async function loadI18nConfig(relativePath) {
      const config = await loadPluginJsonConfig(relativePath);
      const languages = config.languages || config;
      if (!languages.zh && !languages.en) {
        throw new Error(`${relativePath} does not contain languages.zh or languages.en.`);
      }
      return languages;
    }

    async function loadExportSettingsConfig(relativePath) {
      const config = await loadPluginJsonConfig(relativePath);
      const requiredPaths = [
        "schemaPathHint",
        "output.buiExtension",
        "output.assetsSuffix"
      ];

      for (const path of requiredPaths) {
        const parts = path.split(".");
        let value = config;
        for (const part of parts) {
          value = value?.[part];
        }
        if (value === undefined || value === null || value === "") {
          throw new Error(`${relativePath} is missing required setting: ${path}`);
        }
      }

      return config;
    }

    async function loadPluginJsonConfig(relativePath) {
      if (fs && typeof fs.getPluginFolder === "function") {
        let entry = await fs.getPluginFolder();
        const parts = safeString(relativePath, "").split(/[\\/]+/).filter(Boolean);
        for (const part of parts) {
          entry = await entry.getEntry(part);
        }
        const text = await entry.read({ format: formats.utf8 });
        return JSON.parse(text);
      }

      if (typeof fetch === "function") {
        const response = await fetch(relativePath);
        if (!response || !response.ok) {
          throw new Error(`fetch ${relativePath} returned ${response ? response.status : "no response"}`);
        }

        return response.json();
      }

      throw new Error("No UXP plugin-folder reader or fetch API is available.");
    }

    return {
      loadI18nConfig,
      loadExportSettingsConfig,
      loadPluginJsonConfig
    };
  }

  global.BurnerPsdToBuiConfigLoader = {
    createConfigLoader
  };
})(globalThis);
