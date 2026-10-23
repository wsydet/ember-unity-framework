// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ember.UPMManager.Editor
{
    /// <summary>同步消费项目根目录规则入口；仅维护带标记的区块，与模板目录无关。</summary>
    internal static class EmberProjectRulesSync
    {
        #region 内部参数
        internal const string BEGIN = "<!-- EMBER:PROJECT-RULES:BEGIN -->";
        internal const string END = "<!-- EMBER:PROJECT-RULES:END -->";
        internal const string RULES_PATH = "Documentation~/maintenance/unity-validation.md";
        internal const string STATUS_KEY = "Ember.ProjectRules.Status";
        internal const string ERROR_KEY = "Ember.ProjectRules.Error";
        private static readonly string[] FILE_NAMES = { "AGENTS.md", "CLAUDE.md" };
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private static string BuildBlock(string projectRoot, string packageRoot, string newline)
        {
            string root = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var target = new Uri(Path.GetFullPath(Path.Combine(packageRoot, RULES_PATH)));
            string link = new Uri(root).MakeRelativeUri(target).ToString();
            string glossary = new Uri(root).MakeRelativeUri(new Uri(Path.GetFullPath(
                Path.Combine(packageRoot, "Documentation~/maintenance/glossary.md")))).ToString();
            return string.Join(newline, new[]
            {
                BEGIN,
                "## Ember 项目公共规则（自动同步）",
                "",
                "需要测试或进行 Unity 验证前，必须完整阅读并遵守[项目测试与 Unity 编译验证规范](<" + link + ">)。",
                "代码修改完成后必须主动通过 Unity MCP 请求重编译，确认本轮编译/域重载完成后读取 Console；不能以刷新请求成功代替编译完成。",
                "理解简化用语先读[通用术语字典](<" + glossary + ">)，再按正式活动模板读取 Assets/Game/Documentation/<template-id>/glossary.md（若存在）；字典不扩展操作授权。",
                "规则适用于所有模板和未部署模板的项目。包路径变化后由框架重新同步；链接暂不可用时，通过 UPM 定位当前 com.ember 包中的 `" + RULES_PATH + "`。",
                "此区块由 Ember 管理，安装、升级和编辑器重载时更新。项目自定义规则请写在区块外；保留已有自定义规则，不以此区块覆盖它们。",
                END
            });
        }

        private static string Merge(string original, string block, string newline)
        {
            int start = original.IndexOf(BEGIN, StringComparison.Ordinal);
            int end = original.IndexOf(END, StringComparison.Ordinal);
            if (start < 0 && end < 0)
                return original + (original.Length == 0 ? "" : (original.EndsWith("\n", StringComparison.Ordinal) ? newline : newline + newline))
                    + block + newline;
            // Never guess ownership when markers were edited, duplicated or pasted into prose.
            if (start < 0 || end < start
                || original.IndexOf(BEGIN, start + BEGIN.Length, StringComparison.Ordinal) >= 0
                || original.IndexOf(END, end + END.Length, StringComparison.Ordinal) >= 0
                || !IsWholeLine(original, start, BEGIN.Length) || !IsWholeLine(original, end, END.Length))
                throw new InvalidDataException("Ember 规则区块标记不完整或重复，已保留原文件；请修复标记后重试。");
            return original.Substring(0, start) + block + original.Substring(end + END.Length);
        }

        private static bool IsWholeLine(string text, int start, int length)
        {
            int after = start + length;
            return (start == 0 || text[start - 1] == '\n')
                && (after == text.Length || text[after] == '\n' || text[after] == '\r');
        }

        private static Encoding DetectEncoding(byte[] bytes, out int preambleLength)
        {
            Encoding[] encodings =
            {
                new UTF32Encoding(false, true, true), new UTF32Encoding(true, true, true),
                new UTF8Encoding(true, true), new UnicodeEncoding(false, true, true), new UnicodeEncoding(true, true, true)
            };
            foreach (var encoding in encodings)
            {
                byte[] preamble = encoding.GetPreamble();
                if (bytes.Take(preamble.Length).SequenceEqual(preamble))
                { preambleLength = preamble.Length; return encoding; }
            }
            preambleLength = 0;
            return new UTF8Encoding(false, true);
        }

        private static bool SyncFile(string path, string projectRoot, string packageRoot)
        {
            if (Directory.Exists(path)) throw new IOException("目标是目录，已保留：" + path);
            // Do not follow links into other projects or silently break user-managed aliases.
            if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("目标是符号链接，已保留：" + path);
            if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0)
                throw new IOException("目标是只读文件，已保留：" + path);
            bool existed = File.Exists(path);
            byte[] before = existed ? File.ReadAllBytes(path) : Array.Empty<byte>();
            Encoding encoding = DetectEncoding(before, out int preambleLength);
            string original = encoding.GetString(before, preambleLength, before.Length - preambleLength);
            if (original.IndexOf('\0') >= 0)
                throw new InvalidDataException("文件包含空字符，可能是无 BOM 的 UTF-16/UTF-32；保留原文件，请先确认编码。");
            string newline = original.Contains("\r\n") ? "\r\n" : "\n";
            string merged = Merge(original, BuildBlock(projectRoot, packageRoot, newline), newline);
            if (merged == original) return false;
            byte[] after = encoding.GetPreamble().Concat(encoding.GetBytes(merged)).ToArray();
            string temp = path + ".ember-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temp, after);
                if (existed)
                {
                    if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0
                        || !File.ReadAllBytes(path).SequenceEqual(before))
                        throw new IOException("同步期间文件已变化，保留当前内容并等待下次重试：" + path);
                    File.Replace(temp, path, null);
                }
                else File.Move(temp, path); // Fails safely if a user created the file in the meantime.
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            return true;
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        /// <summary>返回逐文件状态；某个入口失败不影响另一个入口。开发仓库仍手工维护自己的规则。</summary>
        internal static string Sync(string projectRoot, string packageRoot, out bool hasErrors)
        {
            hasErrors = false;
            if (EmberAISkillInstaller.IsSourceProject(projectRoot)) return "框架开发仓库保留自有规则入口。";
            if (!File.Exists(Path.Combine(packageRoot, RULES_PATH)))
                throw new FileNotFoundException("当前框架包缺少公共规则正文，未修改项目入口。", RULES_PATH);
            var messages = new List<string>();
            foreach (string name in FILE_NAMES)
            {
                try
                {
                    bool changed = SyncFile(Path.Combine(projectRoot, name), projectRoot, packageRoot);
                    messages.Add(name + (changed ? "：已同步公共规则入口。" : "：公共规则入口已是最新。"));
                }
                catch (Exception ex)
                {
                    hasErrors = true;
                    messages.Add(name + "：未同步，" + ex.Message);
                }
            }
            return string.Join("\n", messages);
        }
        #endregion
    }
}
