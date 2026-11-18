using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>把已制作的面板限制在父级范围内，横屏保持紧凑，竖屏提供卡片重排空间。</summary>
    public sealed class EUIConstrainedPanel : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private Vector2 _maximumSize=new(1080,660);
        [SerializeField] private float _portraitHeight=1120;
        [SerializeField] private float _inset=24;
        #endregion
        #region 内部参数
        private Vector2 _lastSize;
        #endregion
        #region 生命周期
        private void OnEnable(){_lastSize=Vector2.zero;Refresh();}
        private void LateUpdate(){var parent=transform.parent as RectTransform;if(parent&&parent.rect.size!=_lastSize)Refresh();}
        #endregion
        #region 外部方法
        public void Refresh()
        {
            var rect=(RectTransform)transform;var parent=rect.parent as RectTransform;if(!parent)return;
            _lastSize=parent.rect.size;bool portrait=_lastSize.y>_lastSize.x;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;
            rect.sizeDelta=new Vector2(Mathf.Min(_maximumSize.x,Mathf.Max(100,_lastSize.x-_inset*2)),Mathf.Min(portrait?_portraitHeight:_maximumSize.y,Mathf.Max(100,_lastSize.y-_inset*2)));
        }
        #endregion
    }
}
