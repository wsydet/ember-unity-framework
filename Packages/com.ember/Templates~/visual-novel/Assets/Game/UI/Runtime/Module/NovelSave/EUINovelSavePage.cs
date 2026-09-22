using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core;
using Ember.UI;
using Ember.UIExtension;
using Game.Narrative;
using Game.NovelSave;
using UnityEngine;

namespace Game.UI
{
    public partial class EUINovelSavePage
    {
        #region 内部参数
        private readonly List<EUIItem> _items = new();
        private NovelSaveModule _save;
        private NovelSession _paused;
        private int _confirmSlot = -1;
        private IDisposable _pauseLease, _confirmLease;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void ClosePage() { NovelSaveUI.CancelLoad(); EUIManager.Instance.ClosePage(Page); }
        private void SaveSlot(int slot)
        {
            if (_save == null || NovelSaveUI.IsLoading || _save.IsBusy) return;
            if (_save.Store.Slots.Any(s => s.Slot == slot) && _confirmSlot != slot)
            { _confirmSlot = slot; _confirmLease ??= _paused?.AcquirePause("SaveOverwriteConfirm"); Feedback.text = "再次点击此槽位的保存按钮确认覆盖；关闭可取消。"; return; }
            _confirmSlot = -1; _confirmLease?.Dispose(); _confirmLease = null; _save.Save(slot);
        }
        private void LoadSlot(int slot) { _confirmSlot = -1; _confirmLease?.Dispose(); _confirmLease = null; NovelSaveUI.Load(slot); }
        private void Refresh()
        {
            if (_save == null) return;
            bool canSave = _paused != null && !_paused.IsDisposed && _paused.IsReady &&
                (_paused.Snapshot.State == NarrativeState.AwaitingAdvance || _paused.Snapshot.State == NarrativeState.AwaitingChoice);
            var slots = _save.Store.Slots;
            for (int i = 0; i < _items.Count; i++)
            {
                int slot = i;
                ((EUINovelSaveSlotItem)_items[i].Logic).Configure(i, slots.FirstOrDefault(s => s.Slot == i), canSave, NovelSaveUI.IsLoading || _save.IsBusy,
                    () => SaveSlot(slot), () => LoadSlot(slot));
            }
            Feedback.text = _save.Message ?? "6 个手动槽 · 1 个快速槽 · 1 个自动槽。当前句完整显示或选项就绪后可保存。";
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override void OnInit() { base.OnInit(); Close.onClick.AddListener(ClosePage); }
        public override void OnOpen(object param)
        {
            base.OnOpen(param); _confirmSlot = -1; _save = NovelSaveUI.Module;
            if (EmberModuleCollector.Instance.TryGetModule(out NarrativeModule narrative)) _paused = narrative.Session;
            _pauseLease?.Dispose(); _pauseLease = _paused?.AcquirePause("SavePage");
            for (int i = _items.Count; i < 8; i++)
            {
                var root = UnityEngine.Object.Instantiate(SlotTemplate.gameObject, Rows);
                if (!EUIItemFactory.TryCreate(root, out var item, out var error)) { UnityEngine.Object.Destroy(root); throw new InvalidOperationException(error); }
                _items.Add(item);
            }
            foreach (var item in _items) item.Show();
            if (_save != null) _save.Changed += Refresh; Refresh();
        }
        public override void OnClose()
        {
            if (_save != null) { _save.Changed -= Refresh; NovelSaveUI.CancelLoad(); }
            _confirmLease?.Dispose(); _confirmLease = null; _pauseLease?.Dispose(); _pauseLease = null; _paused = null; _save = null;
            foreach (var item in _items) item.Hide();
            NovelSaveUI.Closed(); base.OnClose();
        }
        public override void OnDispose()
        {
            OnClose(); Close.onClick.RemoveListener(ClosePage);
            foreach (var item in _items) { var root = item.GameObject; item.Dispose(); UnityEngine.Object.Destroy(root); }
            _items.Clear(); base.OnDispose();
        }
        #endregion
    }
}
