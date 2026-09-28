using UnityEngine;
using Sirenix.OdinInspector;

namespace Ember.Table.Editor
{
    /// <summary>项目可继续注册其他图片表；同一表的不同图片列独立登记。</summary>
    [CreateAssetMenu(menuName = "Ember/图片资源来源", fileName = "ImageSource")]
    public sealed class EmberImageSourceAsset : ScriptableObject
    {
        #region 编辑器面板参数
        [InlineProperty, HideLabel] public EmberImageSource Source = new();
        #endregion
    }
}
