using System;
using Ember.Core;
using Ember.UI;
using UnityEngine;

namespace Game.UI
{
    /// <summary>示例页把已制作的背景Canvas作为独立绘制根借出，释放时归还原有树和布局。</summary>
    internal sealed class SoftGlassBackdropAttachment : IDisposable
    {
        private readonly RectTransform _rect;
        private readonly Transform _parent;
        private readonly int _sibling;
        private readonly Vector2 _min,_max,_pivot,_position,_size;
        private readonly Canvas _canvas;
        private readonly EUIPage _owner;
        private readonly int _order;
        private bool _disposed;

        public SoftGlassBackdropAttachment(EUIPage owner,RectTransform rect)
        {
            _owner=owner;
            _rect=rect;_parent=rect.parent;_sibling=rect.GetSiblingIndex();
            _min=rect.anchorMin;_max=rect.anchorMax;_pivot=rect.pivot;_position=rect.anchoredPosition;_size=rect.sizeDelta;
            _canvas=rect.GetComponent<Canvas>();_order=_canvas.sortingOrder;
            rect.SetParent(owner.Transform.parent,false);
            _canvas.renderMode=RenderMode.ScreenSpaceCamera;_canvas.worldCamera=GameLauncher.Instance.UICamera;
            _canvas.sortingOrder=owner.Canvas.sortingOrder-2;_canvas.planeDistance=owner.Canvas.planeDistance;
            rect.gameObject.SetActive(true);EUIBackdrop.MarkOrderDirty();
        }

        public void SetVisible(bool visible) { if(_rect)_rect.gameObject.SetActive(visible); }
        public void SyncVisibility()
        {
            if(!_canvas||_disposed)return;
            var source=_owner.Canvas;
            _canvas.enabled=source&&source.enabled&&(!_owner.CanvasGroup||_owner.CanvasGroup.alpha>0)&&
                (!source.worldCamera||source.planeDistance<source.worldCamera.farClipPlane);
        }
        public void Dispose()
        {
            if(_disposed)return;_disposed=true;
            if(!_rect)return;
            _rect.SetParent(_parent,false);_rect.SetSiblingIndex(_sibling);
            _rect.anchorMin=_min;_rect.anchorMax=_max;_rect.pivot=_pivot;_rect.anchoredPosition=_position;_rect.sizeDelta=_size;
            if(_canvas)_canvas.sortingOrder=_order;
            EUIBackdrop.MarkOrderDirty();
        }
    }
}
