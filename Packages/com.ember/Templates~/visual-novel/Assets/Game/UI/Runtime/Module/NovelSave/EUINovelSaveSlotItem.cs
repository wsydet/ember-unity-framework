using System;
using Game.NovelSave;

namespace Game.UI
{
    public partial class EUINovelSaveSlotItem
    {
        #region 内部参数
        private Action _write, _read;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Save() => _write?.Invoke();
        private void Load() => _read?.Invoke();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override void OnInit()
        {
            base.OnInit(); Write.onClick.AddListener(Save); Read.onClick.AddListener(Load);
        }
        public void Configure(int slot, NovelSlot entry, bool canSave, bool busy, Action write, Action read)
        {
            _write = write; _read = read;
            string name = slot < 6 ? "手动槽 " + (slot + 1) : slot == 6 ? "快速槽" : "自动槽";
            string preview = entry?.Summary ?? "";
            if (preview.Length > 42) preview = preview.Substring(0, 42) + "…";
            Summary.text = entry == null ? name + "\n尚未保存" : name + "    " + new DateTime(entry.SavedUtcTicks, DateTimeKind.Utc).ToLocalTime().ToString("MM-dd HH:mm:ss")
                + "    " + entry.Chapter + "\n" + preview.Replace('\n', ' ').Replace('\r', ' ');
            Write.gameObject.SetActive(slot < 7); Write.interactable = canSave && !busy; Read.interactable = entry != null && !busy;
        }
        public override void OnDispose()
        {
            Write.onClick.RemoveListener(Save); Read.onClick.RemoveListener(Load); _write = _read = null; base.OnDispose();
        }
        #endregion
    }
}
