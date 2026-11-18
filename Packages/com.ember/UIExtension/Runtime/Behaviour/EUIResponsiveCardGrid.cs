using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>滚动容器中的响应式卡片网格；支持窗口尺寸、安全区和动态页脚变化。</summary>
    public sealed class EUIResponsiveCardGrid : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private RectTransform[] _cards = Array.Empty<RectTransform>();
        [SerializeField] private RectTransform _footer;
        [SerializeField] private RectTransform _sectionHeading;
        [SerializeField, Min(120)] private float _cardHeight = 300;
        #endregion
        #region 内部参数
        private float _width = -1;
        private int _footerCount = -1;
        #endregion
        #region 生命周期
        private void OnEnable() { _width = -1; }
        private void LateUpdate()
        {
            var content = (RectTransform)transform;
            int count = _footer ? _footer.childCount : 0;
            if (Mathf.Abs(_width - content.rect.width) < .1f && count == _footerCount) return;
            _width = content.rect.width; _footerCount = count; Refresh();
        }
        #endregion
        #region 外部方法
        public void Refresh()
        {
            var content = (RectTransform)transform;
            float width = Mathf.Max(100, content.rect.width);
            int columns = width >= 920 ? 3 : width >= 560 ? 2 : 1;
            float cellWidth = (width - (columns - 1) * 20) / columns;
            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i]; if (!card) continue;
                card.anchorMin = card.anchorMax = new Vector2(0, 1); card.pivot = new Vector2(0, 1);
                bool spanLast=columns==2&&i==_cards.Length-1&&_cards.Length%2==1;
                card.sizeDelta = new Vector2(spanLast?width:cellWidth, spanLast?240:_cardHeight);
                card.anchoredPosition = new Vector2((i % columns) * (cellWidth + 20), -(i / columns) * (_cardHeight+20));
            }
            int rows=Mathf.CeilToInt((float)_cards.Length/columns);
            float cardsHeight=rows*(_cardHeight+20)-20;
            if(columns==2&&_cards.Length%2==1)cardsHeight-=(_cardHeight-240);
            if(_sectionHeading){_sectionHeading.anchorMin=_sectionHeading.anchorMax=new Vector2(0,1);_sectionHeading.pivot=new Vector2(0,1);_sectionHeading.anchoredPosition=new Vector2(0,-cardsHeight-24);_sectionHeading.sizeDelta=new Vector2(width,24);cardsHeight+=60;}
            else cardsHeight+=20;
            float footerHeight = 0;
            if (_footer)
            {
                int actionColumns = Mathf.Clamp(Mathf.FloorToInt((width + 16) / 144),1,Mathf.Max(1,_footer.childCount));
                footerHeight = Mathf.Max(132, Mathf.CeilToInt((float)_footer.childCount / actionColumns) * 144);
                _footer.anchorMin = _footer.anchorMax = new Vector2(0, 1); _footer.pivot = new Vector2(0, 1);
                _footer.anchoredPosition = new Vector2(0, -cardsHeight); _footer.sizeDelta = new Vector2(width, footerHeight);
                var grid = _footer.GetComponent<GridLayoutGroup>();
                if (grid) { grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = actionColumns; grid.cellSize = new Vector2((width - (actionColumns - 1) * 16) / actionColumns, 132); }
            }
            content.sizeDelta = new Vector2(content.sizeDelta.x, cardsHeight + footerHeight + 16);
        }
        #endregion
    }
}
