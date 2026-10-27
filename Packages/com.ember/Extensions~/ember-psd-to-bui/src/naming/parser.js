/* global globalThis */

(function registerNamingParser(global) {
  function createNamingParser(options) {
    const { safeString, arrayFrom } = options;

    function parsePsdNaming(rawName, namingRules) {
      const text = safeString(rawName, "").trim();
      const prefix = namingRules.syntax?.flagPrefix || "@";
      const index = getNamingRuleIndex(namingRules);
      const ignorePrefix = getIgnorePrefixes(namingRules).find((candidate) => text.startsWith(candidate)) || "";
      const tokens = expandNamingTokens(text.split(/\s+/).filter(Boolean), prefix);
      const displayTokens = [];
      const mainCandidates = [];
      const options = [];
      const unknownFlags = [];

      for (const token of tokens) {
        if (!token.startsWith(prefix) || token.length <= prefix.length) {
          displayTokens.push(token);
          continue;
        }

        const parsedToken = parseFlagToken(token, namingRules);
        const optionRule = index.optionByFlag[parsedToken.flag];
        const nodeRule = index.nodeByFlag[parsedToken.flag];
        const targetRule = index.targetByFlag[parsedToken.flag];

        if (optionRule) {
          options.push({
            ...parsedToken,
            kind: "option",
            rule: optionRule
          });
          continue;
        }

        if (nodeRule) {
          mainCandidates.push({
            ...parsedToken,
            kind: "node",
            rule: nodeRule
          });
          continue;
        }

        if (targetRule) {
          mainCandidates.push({
            ...parsedToken,
            kind: "target",
            rule: targetRule
          });
          continue;
        }

        unknownFlags.push(parsedToken);
      }

      return {
        rawName: text,
        ignored: Boolean(ignorePrefix),
        ignorePrefix,
        displayName: displayTokens.join(" ").trim(),
        main: mainCandidates[0] || null,
        extraMainFlags: mainCandidates.slice(1),
        options,
        unknownFlags
      };
    }


    function parseFlagToken(token, namingRules) {
      const prefix = namingRules.syntax?.flagPrefix || "@";
      const delimiter = namingRules.syntax?.mainDelimiter || "_";
      const body = token.startsWith(prefix) ? token.slice(prefix.length) : token;
      const delimiterIndex = body.indexOf(delimiter);
      const flag = delimiterIndex >= 0 ? body.slice(0, delimiterIndex) : body;
      const value = delimiterIndex >= 0 ? body.slice(delimiterIndex + delimiter.length) : "";
      const params = value ? value.split(delimiter).filter((part) => part.length > 0) : [];

      return {
        token,
        flag,
        value,
        params,
        hasValue: delimiterIndex >= 0
      };
    }


    function expandNamingTokens(tokens, prefix) {
      const result = [];
      for (const token of tokens) {
        if (!token.startsWith(prefix) || token.length <= prefix.length) {
          result.push(token);
          continue;
        }

        let start = 0;
        for (let i = prefix.length; i < token.length; i += 1) {
          if (!token.startsWith(prefix, i)) {
            continue;
          }

          const part = token.slice(start, i);
          if (part) {
            result.push(part);
          }
          start = i;
          i += prefix.length - 1;
        }

        const last = token.slice(start);
        if (last) {
          result.push(last);
        }
      }
      return result;
    }


    function getNamingRuleIndex(namingRules) {
      if (namingRules.__index) {
        return namingRules.__index;
      }

      const index = {
        targetByFlag: {},
        targetById: {},
        nodeByFlag: {},
        nodeById: {},
        nodeByBuiType: {},
        optionByFlag: {}
      };

      arrayFrom(namingRules.targetTypes).forEach((rule) => {
        index.targetById[rule.id] = rule;
        arrayFrom(rule.flags).forEach((flag) => {
          index.targetByFlag[flag] = rule;
        });
      });

      arrayFrom(namingRules.nodeTypes).forEach((rule) => {
        index.nodeById[rule.id] = rule;
        if (rule.buiNodeType) {
          index.nodeByBuiType[rule.buiNodeType] = rule;
        }
        arrayFrom(rule.flags).forEach((flag) => {
          index.nodeByFlag[flag] = rule;
        });
      });

      arrayFrom(namingRules.optionTypes).forEach((rule) => {
        arrayFrom(rule.flags).forEach((flag) => {
          index.optionByFlag[flag] = rule;
        });
      });

      Object.defineProperty(namingRules, "__index", {
        value: index,
        enumerable: false,
        configurable: true
      });
      return index;
    }


    function getIgnorePrefixes(namingRules) {
      return arrayFrom(namingRules.syntax?.ignorePrefixes).filter(Boolean);
    }


    function isIgnoredByNaming(naming) {
      return Boolean(naming && naming.ignored);
    }

    return { parsePsdNaming, parseFlagToken, getNamingRuleIndex, getIgnorePrefixes, isIgnoredByNaming };
  }

  global.BurnerPsdToBuiNamingParser = { createNamingParser };
})(globalThis);
