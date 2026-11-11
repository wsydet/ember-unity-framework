// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.IO;
using System.Text;
using Ember.Basic;

namespace Ember.Core.Editor
{
    /// <summary>项目内、每次操作独有的暂存目录；只清理本次持有归属凭据的目录。</summary>
    internal static class EmberTemplateTemporaryDirectory
    {
        #region 外部方法
        internal static T Run<T>(string projectRoot, string operation, Func<string, T> action)
        {
            string parent = Path.GetFullPath(Path.Combine(projectRoot, "Temp"));
            EmberTemplateTransaction.ValidateDirectorySafety(parent);
            string token = Guid.NewGuid().ToString("N");
            string root = Path.Combine(parent, operation + "-" + token + "~");
            if (EmberTemplateFileSystem.Directory.Exists(root) || EmberTemplateFileSystem.File.Exists(root))
                throw new IOException("暂存目录已被占用：" + root);
            EmberTemplateFileSystem.Directory.CreateDirectory(root);
            string owner = Path.Combine(root, ".ember-owner");
            // CreateNew 防止碰撞时认领另一操作。认领失败不清理尚未归属本次操作的目录。
            using (var stream = new FileStream(EmberTemplateFileSystem.NativePath(owner), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(token);
            Exception primary = null;
            try { return action(root); }
            catch (Exception error)
            {
                primary = error;
                error.Data["EmberTemporaryDirectory"] = root;
                throw;
            }
            finally
            {
                try
                {
                    if (Path.GetDirectoryName(root) != parent || Path.GetFileName(root) != operation + "-" + token + "~")
                        throw new IOException("暂存清理边界不匹配：" + root);
                    EmberTemplateTransaction.ValidateDirectorySafety(root);
                    if (EmberTemplateFileSystem.File.ReadAllText(owner) != token)
                        throw new IOException("暂存目录归属凭据不匹配：" + root);
                    EmberTemplateTransaction.CleanPath(root);
                }
                catch (Exception cleanup)
                {
                    var failure = new IOException("暂存目录清理失败，残留需人工检查：[" + root + "]。", cleanup);
                    if (primary == null) throw failure;
                    primary.Data["EmberStageCleanupException"] = failure;
                    EmberDebug.LogWarning(LogTags.CoreEditor, failure.ToString());
                }
            }
        }
        #endregion
    }
}
