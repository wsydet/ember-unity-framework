using System;
using System.Linq;
using UnityEditor;

namespace Ember.Core.Editor
{
    public static partial class EmberExtensionProject
    {
        #region 内部参数
        private static long _resolutionGeneration = -1, _hashGeneration = -1;
        private static EmberExtensionDefinition[] _prepared;
        private static string[] _requested;
        private static int _batchDepth;
        private static EmberExtensionResolution _batchResolution;
        internal static int ResolutionBuilds { get; private set; }
        internal static int ContentHashBuilds { get; private set; }
        private sealed class CheckBatch : IDisposable
        {
            private bool _disposed;
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (--_batchDepth == 0) _batchResolution = null;
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static Configuration CloneConfiguration(Configuration source) => new()
        {
            schemaVersion = source.schemaVersion, ownerTemplateId = source.ownerTemplateId,
            ownerTemplateVersion = source.ownerTemplateVersion, enabled = source.enabled.ToArray(),
            bindings = source.bindings?.Select(value => value == null ? null : new Ember.UPMManager.Editor.EmberAISkillInstaller.ExtensionIdentity
            { id = value.id, contractVersion = value.contractVersion, contentHash = value.contentHash }).ToArray()
        };

        private static void InvalidateResolution()
        {
            EmberEditorMetadataCache.Invalidate();
            _resolutionGeneration = -1; _hashGeneration = -1;
            _prepared = null; _batchResolution = null;
        }

        private static EmberExtensionResolution ResolveCurrent()
        {
            long generation = EmberEditorMetadataCache.Generation;
            if (_resolutionGeneration != generation)
            {
                _prepared = null; _batchResolution = null;
                // 失败也按代次保存；损坏配置不会在每帧重新解析，更不会保留旧 true。
                var prepared = EmberEditorMetadataCache.Get("extension-prepared", () =>
                {
                    var config = LoadCurrent();
                    var definitions = PrepareConfiguration(config, EmberFsmBlueprintAccess.CurrentTemplateId, CurrentTemplateVersion);
                    ResolutionBuilds++;
                    return (definitions, requested: config.enabled.ToArray());
                });
                _prepared = prepared.definitions; _requested = prepared.requested;
                _resolutionGeneration = generation;
            }
            // 模块和兼容回调只在当前调用/显式批次内求值，不把未知回调缓存到下一帧。
            if (_batchDepth > 0 && _batchResolution != null) return _batchResolution;
            var result = EmberExtensionCatalog.Resolve(_prepared, _requested);
            if (_batchDepth > 0) _batchResolution = result;
            return result;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>同步只读检查批次。不要跨帧持有；动态模块条件仍按调用实时求值。</summary>
        public static IDisposable BeginCheckBatch()
        {
            if (_batchDepth++ == 0) _batchResolution = null;
            return new CheckBatch();
        }

        /// <summary>执行、Start/Retry 或写入前重新校验当前输入；旧窗口快照不授予权限。</summary>
        public static void ValidateCurrent()
        {
            EmberEditorMetadataCache.ValidateNow();
            _batchResolution = null;
        }

        /// <summary>登记/释放模块或适配器后，下次检查立即重算动态条件。</summary>
        public static void InvalidateChecks() => _batchResolution = null;
        #endregion
    }
}
