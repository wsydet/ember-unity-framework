using System.Collections.Generic;
using Ember.UI;
using Ember.UIExtension;
using UnityEngine;

namespace Game.UI
{
    public partial class EUIDynamicListPage
    {
        #region 内部参数
        private EUIItemList<EUIOptionRowData> _rows;
        private readonly List<EUIOptionRowData> _data = new();
        private Sprite _firstIcon, _secondIcon;
        public EUIItemList<EUIOptionRowData> Rows => _rows;
        public int ClickCount { get; private set; }
        public string LastKey { get; private set; }
        public int LastIndex { get; private set; }
        #endregion
        #region 生命周期
        public override void OnInit()
        {
            base.OnInit();
            Empty.onClick.AddListener(ClearRows); One.onClick.AddListener(ShowOne);
            Three.onClick.AddListener(ShowThree); Twenty.onClick.AddListener(ShowTwenty);
            Reverse.onClick.AddListener(ReverseRows); Remove.onClick.AddListener(RemoveLast); Back.onClick.AddListener(ReturnMain);
            // 直接复用页面已加载依赖中的正式图标；不引入独立加载句柄。
            _firstIcon = List.ItemTemplate.transform.Find("Icon").GetComponent<UnityEngine.UI.Image>().sprite;
            _secondIcon = List.ItemTemplate.transform.Find("AlternateIcon").GetComponent<UnityEngine.UI.Image>().sprite;
        }
        public override void OnOpen(object param)
        {
            base.OnOpen(param);
            _rows?.Dispose();
            _rows = List.Create<EUIOptionRowData>(data => data.Id,
                lease => ((EUIDynamicOptionRowItem)lease.Item.Logic).Bind(lease, OnSelected));
            ShowThree();
        }
        public override void OnHide() { _rows?.Hide(); base.OnHide(); }
        public override void OnShow() { base.OnShow(); _rows?.Show(); }
        public override void OnClose() { _rows?.Dispose(); _data.Clear(); base.OnClose(); }
        public override void OnDispose()
        {
            _rows?.Dispose(); _data.Clear();
            Empty.onClick.RemoveListener(ClearRows); One.onClick.RemoveListener(ShowOne);
            Three.onClick.RemoveListener(ShowThree); Twenty.onClick.RemoveListener(ShowTwenty);
            Reverse.onClick.RemoveListener(ReverseRows); Remove.onClick.RemoveListener(RemoveLast); Back.onClick.RemoveListener(ReturnMain);
            _firstIcon = _secondIcon = null;
            base.OnDispose();
        }
        #endregion
        #region 内部方法
        private void ShowOne() => SetCount(1);
        private void ShowThree() => SetCount(3);
        private void ShowTwenty() => SetCount(20);
        private void ReturnMain() => EUIManager.Instance.ClosePage(Page);
        private void OnSelected(EUIOptionRowData data, int index)
        { ClickCount++; LastKey = data.Id; LastIndex = index; Title.SetSelected(data.Id, index); }
        #endregion
        #region 外部方法
        public void SetCount(int count)
        {
            _data.Clear();
            for (var i=0;i<count;i++) _data.Add(new EUIOptionRowData {
                Id = "option-" + i, Title = i == 0 ? "A longer description wraps naturally across several lines. Each row keeps its own data identity when the collection is reordered, refreshed, or reused. Scroll to explore the complete collection." : "Option " + (i+1),
                Icon = i % 2 == 0 ? _firstIcon : _secondIcon, Interactable = i != 1, Selected = i == 2, Progress = (i % 5) / 4f });
            _rows.Replace(_data); Title.SetCount(count);
        }
        public void ClearRows() { _data.Clear(); _rows.Clear(); Title.SetCount(0); }
        public void ReverseRows() { _data.Reverse(); _rows.Replace(_data); }
        public void RemoveLast() { if (_data.Count == 0) return; _data.RemoveAt(_data.Count-1); _rows.Replace(_data); }
        #endregion
    }
}
