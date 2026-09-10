// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Basic;
using Ember.Core;
using Ember.Resource;

namespace Ember.Table.Integration
{
    /// <summary>
    /// 项目表 Module 的通用生命周期基类，只连接 Core、Resource 与纯 EmberTableEngine。
    /// 具体项目只提供 Catalog、启用特性和领域钩子。
    /// </summary>
    public abstract class EmberTableModuleBase<TModule> : EmberSingleton<TModule>, IEmberModule
        where TModule : EmberTableModuleBase<TModule>, new()
    {
        #region 内部参数

        private const string TAG = "Table.Module";

        private EmberTableEngine _engine;
        private bool _isReady;
        private bool _isInitializing;

        public EmberTableDatabase Database => _engine?.Database ?? EmberTableDatabase.Empty;
        public bool IsReady => _isReady;
        public EmberTableLoadResult LastLoadResult { get; private set; }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>返回项目生成的只读 Catalog；空项目应返回可编译的空 Catalog。</summary>
        protected abstract IEmberTableCatalog CreateTableCatalog();

        /// <summary>所有 Required 表加载成功后的项目钩子。</summary>
        protected virtual void OnTableModuleInitialized()
        {
        }

        /// <summary>Module 开始关闭时的项目钩子。</summary>
        protected virtual void OnTableModuleDestroying()
        {
        }

        /// <summary>热重启前清理项目 Adapter、事件和缓存的钩子。</summary>
        protected virtual void ResetTableModuleData()
        {
        }

        private void InitializeModule()
        {
            if (_isReady || _isInitializing) return;
            if (!EmberResourceManager.IsValid || !EmberResourceManager.Instance.IsInitialized)
            {
                LastLoadResult = Failure(
                    EmberTableErrorCode.ResourceSystemNotReady,
                    "EmberResourceManager must finish initialization before the table Module starts.");
                throw new InvalidOperationException(LastLoadResult.Diagnostics[0].Message);
            }

            _isInitializing = true;
            _engine = new EmberTableEngine();
            try
            {
                IEmberTableCatalog catalog = CreateTableCatalog();
                if (catalog?.Entries == null)
                {
                    LastLoadResult = Failure(EmberTableErrorCode.CatalogMissing, "The project table Catalog is missing.");
                    throw new InvalidOperationException(LastLoadResult.Diagnostics[0].Message);
                }

                var bytesByTableId = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                for (int i = 0; i < catalog.Entries.Count; i++)
                {
                    EmberTableCatalogEntry entry = catalog.Entries[i];
                    if (entry?.Binding == null || string.IsNullOrEmpty(entry.TableId)) continue;
                    byte[] bytes;
                    try
                    {
                        bytes = EmberResourceManager.Instance.LoadFileSync(entry.ResourcePath);
                    }
                    catch
                    {
                        bytes = null;
                    }
                    bytesByTableId[entry.TableId] = bytes;
                }

                LastLoadResult = _engine.Load(catalog, bytesByTableId);
                if (!LastLoadResult.Succeeded)
                {
                    string detail = FirstError(LastLoadResult);
                    ShutdownModule(false);
                    throw new InvalidOperationException(
                        $"{typeof(TModule).Name} failed to load Required tables: {detail}");
                }

                _isReady = true;
                OnTableModuleInitialized();
                EmberDebug.LogInit(TAG, $"{typeof(TModule).Name} loaded {Database.Count} table(s).");
            }
            catch
            {
                ShutdownModule(false);
                throw;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private void ShutdownModule(bool clearResult)
        {
            bool hadState = _isReady || _isInitializing || _engine != null;
            _isReady = false;
            if (hadState)
            {
                try
                {
                    OnTableModuleDestroying();
                }
                catch (Exception ex)
                {
                    EmberDebug.LogError(TAG, $"{typeof(TModule).Name} destroy hook failed: {ex}");
                }
            }

            _engine?.Dispose();
            _engine = null;
            _isInitializing = false;
            if (clearResult) LastLoadResult = null;
        }

        private static EmberTableLoadResult Failure(EmberTableErrorCode code, string message)
        {
            return new EmberTableLoadResult(
                false,
                new[]
                {
                    new EmberTableDiagnostic(
                        EmberTableDiagnosticSeverity.Error,
                        code,
                        message),
                },
                Array.Empty<EmberTableLoadInfo>());
        }

        private static string FirstError(EmberTableLoadResult result)
        {
            for (int i = 0; i < result.Diagnostics.Count; i++)
                if (result.Diagnostics[i].Severity == EmberTableDiagnosticSeverity.Error)
                    return result.Diagnostics[i].ToString();
            return "unknown table load error";
        }

        void IEmberModule.OnInit()
        {
            InitializeModule();
        }

        void IEmberModule.OnDestroy()
        {
            ShutdownModule(true);
            EmberDebug.LogCleanup(TAG, $"{typeof(TModule).Name} destroyed.");
        }

        void IEmberModule.ResetModuleData()
        {
            ShutdownModule(true);
            ResetTableModuleData();
        }

        protected override void OnDestroy()
        {
            ShutdownModule(true);
            base.OnDestroy();
        }

        #endregion
    }
}
