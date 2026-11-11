// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.IO;
using System.Text;
using Ember.Basic;
using Ember.UPMManager.Editor;

namespace Ember.Core.Editor
{
    /// <summary>项目内、每次操作独有的暂存目录；只清理本次持有归属凭据的目录。</summary>
    internal static class EmberTemplateTemporaryDirectory
    {
        #region 外部方法
        internal sealed class TestHooks
        {
            internal Action<string> BeforeOwner;
            internal Action<string, FileStream> BeforeOwnerWrite;
            internal Action<string> BeforeCleanup;
            internal Action<string, FileStream> BeforeCleanupLease;
            internal Func<string> Token;
        }

        internal static T Run<T>(string projectRoot, string operation, Func<string, T> action,
            bool committedOnSuccess = false, TestHooks hooks = null)
        {
            if (operation != "EmberPatchPreview" && operation != "EmberPatchApply")
                throw new ArgumentException("未知模板暂存操作。", nameof(operation));
            string parent = Path.GetFullPath(Path.Combine(projectRoot, "Temp"));
            string token = hooks?.Token?.Invoke() ?? Guid.NewGuid().ToString("N");
            if (!Guid.TryParseExact(token, "N", out _)) throw new ArgumentException("无效暂存 token。");
            string root = Path.Combine(parent, operation + "-" + token + "~");
            string owner = Path.Combine(root, ".ember-owner");
            FileStream lease = null;
            Exception primary = null;
            bool owned = false, attempted = false, succeeded = false;
            try
            {
                EmberTemplateTransaction.ValidatePathLinks(parent);
                if (EmberTemplateFileSystem.Directory.Exists(root) || EmberTemplateFileSystem.File.Exists(root))
                    throw new IOException("暂存目录已被占用：" + root);
                attempted = true;
                EmberTemplateFileSystem.Directory.CreateDirectory(root);
                hooks?.BeforeOwner?.Invoke(root);
                EmberTemplateTransaction.ValidatePathLinks(root);
                lease = EmberTemplateFileSystem.File.CreateNewLease(owner);
                hooks?.BeforeOwnerWrite?.Invoke(root, lease);
                byte[] credential = Encoding.UTF8.GetBytes(token);
                lease.Write(credential, 0, credential.Length);
                lease.Flush(true);
                owned = true;
                T result = action(root);
                succeeded = true;
                return result;
            }
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
                    if (!attempted) { /* 未创建，不认领同名占用。 */ }
                    else if (!owned) throw new IOException("未建立完整归属，保留可能残留：" + root);
                    else
                    {
                    hooks?.BeforeCleanup?.Invoke(root);
                    hooks?.BeforeCleanupLease?.Invoke(root, lease);
                    if (Path.GetDirectoryName(root) != parent || Path.GetFileName(root) != operation + "-" + token + "~")
                        throw new IOException("暂存清理边界不匹配：" + root);
                    EmberTemplateTransaction.ValidateDirectorySafety(root);
                    using (var reader = new StreamReader(EmberTemplateFileSystem.File.OpenRead(owner, FileShare.ReadWrite)))
                        if (reader.ReadToEnd() != token)
                            throw new IOException("暂存目录归属凭据不匹配：" + root);
                    lease.Dispose();
                    lease = null;
                    EmberTemplateTransaction.CleanPath(root);
                    }
                }
                catch (Exception cleanup)
                {
                    var failure = new IOException((succeeded && committedOnSuccess ? "模板已提交；" : "")
                        + "暂存目录清理失败，残留需人工检查：[" + root + "]。", cleanup);
                    if (primary != null) primary.Data["EmberStageCleanupException"] = failure;
                    else if (!(succeeded && committedOnSuccess)) throw failure;
                    try { EmberDebug.LogWarning(LogTags.CoreEditor, failure.ToString()); } catch { /* 诊断不能覆盖首要错误。 */ }
                }
                finally { lease?.Dispose(); }
            }
        }
        #endregion
    }
}
