using System;
using System.Collections.Generic;
using System.Text;
using Ember.Basic;
using Ember.Core;

namespace Game.Module.Guide
{
    /// <summary>
    /// 新手引导模块 —— 引导系统主入口。
    ///
    /// <b>定位：</b>业务模块（<see cref="IEmberModule"/>，Phase = <see cref="ModulePhase.Global"/>），
    /// 登录后常驻，跨场景驱动引导。同时实现 <see cref="IEmberUpdate"/> 逐帧驱动当前引导组与覆盖层。
    ///
    /// <b>启用：</b>默认 <see cref="Enabled"/> = false（关闭）。启用时改为返回 true。
    ///
    /// <b>使用示例：</b>
    /// <code>
    /// GuideModule.Instance.Initialize(config);   // 传入 GuideConfig
    /// GuideModule.Instance.Start();              // 装载并开始引导
    ///
    /// // 业务触发事件推进引导
    /// GuideModule.Instance.NotifyButtonClick("MainMenu", "m_Btn_Start");
    /// </code>
    /// </summary>
    public class GuideModule : EmberSingleton<GuideModule>, IEmberModule, IEmberUpdate, IGuideGroupManager
    {
        private const string TAG = LogTags.Game + "." + nameof(GuideModule);
        private const string PROGRESS_FILE = "guide_progress.json";

        #region 内部参数

        private GuideConfig _config;
        private GuideProgress _progress;
        private bool _progressLoaded;
        private bool _isStarted;

        private readonly List<int> _sequentialIds = new();
        private readonly List<int> _unsequentialIds = new();
        private readonly Dictionary<int, GuideGroup> _groups = new();

        private int _curGuideId;
        private string _lastSkipReason;

        /// <summary>测试引导 id（&gt;0 时优先装载该引导，完成后不落盘）。</summary>
        public int TestGuideId;

        /// <summary>模块是否启用。默认关闭，需要时改为返回 true。</summary>
        public bool Enabled => false;

        /// <summary>所属初始化阶段（全局业务，常驻）。</summary>
        public int Phase => ModulePhase.Global;

        /// <summary>当前正在执行的引导 id（0 = 无）。</summary>
        public int CurGuideId => _curGuideId;

        /// <summary>当前正在执行的引导组。</summary>
        public GuideGroup CurGuide
            => _curGuideId != 0 && _groups.TryGetValue(_curGuideId, out var g) ? g : null;

        /// <summary>是否正在引导。</summary>
        public bool IsGuiding => _curGuideId != 0;

        /// <summary>上次跳过原因（诊断用）。</summary>
        public string LastSkipReason => _lastSkipReason;

        #endregion

        // ============================================================

        #region 生命周期

        void IEmberModule.OnInit()
        {
            LoadProgress();
        }

        void IEmberModule.OnDestroy()
        {
            ClearAll();
        }

        void IEmberModule.ResetModuleData()
        {
            ClearAll();
            _progress = null;
            _progressLoaded = false;
        }

        void IEmberUpdate.Update()
        {
            if (_curGuideId != 0 && _groups.TryGetValue(_curGuideId, out var group))
                group.OnTick();
            GuideOverlay.Instance.Update();
        }

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>用配置资产初始化引导系统（构建顺序 / 非顺序 id 列表）。可重复调用。</summary>
        public void Initialize(GuideConfig config)
        {
            _config = config;
            if (!_progressLoaded) LoadProgress();
            RebuildIdLists();
            EmberDebug.LogInit(TAG,
                $"GuideModule 初始化：{_sequentialIds.Count} 条顺序引导，{_unsequentialIds.Count} 条非顺序引导");
        }

        /// <summary>开启引导并装载。</summary>
        public void Start()
        {
            _isStarted = true;
            TrySetupAll();
        }

        /// <summary>关闭引导并清空所有待执行队列。</summary>
        public void Stop()
        {
            _isStarted = false;
            ClearAll();
        }

        /// <summary>
        /// 装载引导：顺序引导装载下一个，非顺序引导装载所有未完成的。
        /// 全局同时只允许一个引导进入执行态。
        /// </summary>
        public void TrySetupAll()
        {
            if (!_isStarted) return;
            if (_progress == null) return;
            if (_curGuideId != 0) return;

            if (TestGuideId > 0)
            {
                SetupSingle(TestGuideId, true);
                return;
            }

            int index = _sequentialIds.IndexOf(_progress.finishedSequentialId);
            int next = index + 1;
            if (next < _sequentialIds.Count)
            {
                SetupSingle(_sequentialIds[next]);
                return;
            }

            foreach (var id in _unsequentialIds)
            {
                if (IsGuideFinished(id)) continue;
                SetupSingle(id);
            }
        }

        /// <summary>清空所有引导（停止当前引导 + 释放所有引导组）。</summary>
        public void ClearAll()
        {
            if (_curGuideId != 0 && _groups.TryGetValue(_curGuideId, out var cur))
            {
                cur.OnExit();
                _curGuideId = 0;
            }

            foreach (var g in _groups.Values)
                g.Dispose();
            _groups.Clear();

            GuideOverlay.Instance.HideAll();
        }

        /// <summary>取消当前引导（不记录完成，稍后会重新触发）。</summary>
        public void StopCurrentGuide()
        {
            if (_curGuideId == 0) return;
            OnGuideFinish(CurGuide, false, true);
        }

        /// <summary>跳过当前引导（记录完成，直接进入下一条）。用于跳过卡住的引导。</summary>
        public void SkipCurrentGuide()
        {
            if (_curGuideId == 0) return;
            OnGuideFinish(CurGuide, true, true);
        }

        /// <summary>查询指定引导是否已完成。</summary>
        public bool IsGuideFinished(int guideId)
        {
            if (_progress == null) return false;
            var entry = GetEntry(guideId);
            if (entry == null) return false;

            if (entry.sequenceOrder > 0)
            {
                int finishedIndex = _sequentialIds.IndexOf(_progress.finishedSequentialId);
                int curIndex = _sequentialIds.IndexOf(guideId);
                return curIndex <= finishedIndex;
            }

            return _progress.finishedOtherId != null && _progress.finishedOtherId.Contains(guideId);
        }

        /// <summary>重置所有引导进度并重新装载。</summary>
        public void ResetProgress()
        {
            _progress = new GuideProgress();
            _progressLoaded = true;
            SaveProgress();
            ClearAll();
            TrySetupAll();
        }

        /// <summary>诊断：引导进度概览。</summary>
        public string GetProgressSummary()
        {
            var sb = new StringBuilder();

            int finishedIndex = _progress != null ? _sequentialIds.IndexOf(_progress.finishedSequentialId) : -1;
            int completedSeq = finishedIndex + 1;
            sb.AppendLine($"=== 顺序引导: {completedSeq}/{_sequentialIds.Count} ===");
            if (completedSeq < _sequentialIds.Count)
                sb.AppendLine($"  下一步: id={_sequentialIds[completedSeq]}");
            else
                sb.AppendLine("  顺序引导已全部完成");

            int completedUnseq = 0;
            if (_progress?.finishedOtherId != null)
            {
                foreach (var id in _unsequentialIds)
                    if (_progress.finishedOtherId.Contains(id)) completedUnseq++;
            }
            sb.AppendLine($"=== 非顺序引导: {completedUnseq}/{_unsequentialIds.Count} ===");

            if (CurGuide != null)
                sb.AppendLine($"=== 当前执行: id={CurGuide.ConfId} 步骤{CurGuide.CurStepIndex} {CurGuide.CurStepState} ===");
            else
                sb.AppendLine("=== 当前没有引导在执行 ===");

            return sb.ToString();
        }

        /// <summary>广播「延时结束」事件（Delay 执行器内部使用，业务也可直接调用）。</summary>
        public void NotifyDelayFinish() => EmberEventBus.OnNext(GuideEventKey.DelayFinish);

        /// <summary>广播「引导遮罩被点击」事件。</summary>
        public void NotifyMaskClick() => EmberEventBus.OnNext(GuideEventKey.MaskClick);

        /// <summary>广播「UI 按钮被点击」事件，供 <see cref="GuideEventType.OnClickUIButton"/> 匹配。</summary>
        public void NotifyButtonClick(string pagePath, string ctrlName)
            => EmberEventBus.OnNext(GuideEventKey.ClickUIButton, pagePath, ctrlName);

        /// <summary>广播「自定义」事件，供 <see cref="GuideEventType.OnCustom"/> 按 key 匹配。</summary>
        public void NotifyCustom(int key) => EmberEventBus.OnNext(GuideEventKey.Custom, key);

        /// <summary>引导步骤通过条件、准备执行时回调（设置当前引导 id）。</summary>
        public void OnGuideExecute(GuideGroup guideGroup, GuideStartCheckResult checkResult)
        {
            if (_curGuideId != 0 && _curGuideId != guideGroup.ConfId)
            {
                EmberDebug.LogError(TAG, $"引导执行冲突! oldId={_curGuideId}, newId={guideGroup.ConfId}");
                return;
            }

            _curGuideId = guideGroup.ConfId;
            GuideUtils.Log($"引导 {guideGroup.ConfId} 执行 (result={checkResult})", guideGroup.Blackboard);
        }

        /// <summary>引导完全终止（完成 / 取消）时回调：落盘进度并装载下一条。</summary>
        public void OnGuideFinish(GuideGroup guideGroup, bool isFinish, bool isForceStop)
        {
            if (_curGuideId != guideGroup.ConfId) return;
            _curGuideId = 0;

            bool isTest = guideGroup.IsTest;

            if (isFinish && !isTest)
            {
                var entry = GetEntry(guideGroup.ConfId);
                if (entry != null)
                {
                    if (entry.sequenceOrder > 0)
                    {
                        _progress.finishedSequentialId = guideGroup.ConfId;
                    }
                    else
                    {
                        _progress.finishedOtherId ??= new List<int>();
                        if (!_progress.finishedOtherId.Contains(guideGroup.ConfId))
                            _progress.finishedOtherId.Add(guideGroup.ConfId);
                    }
                    SaveProgress();
                }
            }

            if (!string.IsNullOrEmpty(guideGroup.SkipReason))
                _lastSkipReason = guideGroup.SkipReason;

            guideGroup.OnExit();
            _groups.Remove(guideGroup.ConfId);
            guideGroup.Dispose();

            if (isTest)
                TestGuideId = 0;

            TrySetupAll();
        }

        #endregion

        // ============================================================

        #region 内部方法

        /// <summary>加载引导进度（幂等，仅首次）。</summary>
        private void LoadProgress()
        {
            if (_progressLoaded) return;
            _progressLoaded = true;

            if (!DataSaver.TryLoad(PROGRESS_FILE, out _progress))
                _progress = new GuideProgress();
            _progress.finishedOtherId ??= new List<int>();

            EmberDebug.LogInit(TAG, $"引导进度已加载：顺序完成 id={_progress.finishedSequentialId}");
        }

        /// <summary>落盘引导进度。</summary>
        private void SaveProgress()
        {
            if (_progress == null) return;
            DataSaver.Save(PROGRESS_FILE, _progress);
        }

        /// <summary>根据配置重建顺序 / 非顺序 id 列表。</summary>
        private void RebuildIdLists()
        {
            _sequentialIds.Clear();
            _unsequentialIds.Clear();
            if (_config == null) return;

            foreach (var e in _config.entries)
            {
                if (e == null) continue;
                if (e.sequenceOrder > 0) _sequentialIds.Add(e.id);
                else _unsequentialIds.Add(e.id);
            }

            _sequentialIds.Sort((a, b) => GetEntry(a).sequenceOrder.CompareTo(GetEntry(b).sequenceOrder));
        }

        /// <summary>按 id 查引导条目。找不到返回 null。</summary>
        private GuideEntry GetEntry(int id)
        {
            if (_config == null) return null;
            foreach (var e in _config.entries)
            {
                if (e != null && e.id == id) return e;
            }
            return null;
        }

        /// <summary>创建并装载单个引导组（已存在则重新评估）。</summary>
        private void SetupSingle(int id, bool isTest = false)
        {
            if (_groups.TryGetValue(id, out var existing))
            {
                // 已装载的引导（上次可能被互斥挡在 NotStart）：重新评估一次
                existing.IsTest = isTest;
                existing.OnEnter();
                return;
            }

            var entry = GetEntry(id);
            if (entry == null || entry.define == null)
            {
                EmberDebug.LogError(TAG, $"引导 {id} 配置缺失或未引用 GuideDefine，跳过");
                return;
            }

            var group = new GuideGroup(id, entry.define, entry.stringParams, entry.intParams, this);
            group.IsTest = isTest;
            _groups.Add(id, group);
            group.OnEnter();
        }

        #endregion
    }
}
