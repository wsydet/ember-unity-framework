using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>
    /// EmberDebugConfig 的 Odin 面板。
    /// 自动绘制 SO 的属性（由 Odin 和 SO 上的属性共同控制），
    /// 仅在面板顶部添加三个批量操作按钮。
    /// </summary>
    [CustomEditor(typeof(EmberDebugConfigSO))]
    public class EmberDebugConfigEditor : OdinEditor
    {
        [PropertyOrder(-10)]
        [HorizontalGroup("批量操作")]
        [Button("全部开启", ButtonSizes.Medium)]
        private void EnableAll()
        {
            var config = (EmberDebugConfigSO)target;
            config.EnableAll();
            EditorUtility.SetDirty(config);
        }

        [PropertyOrder(-10)]
        [HorizontalGroup("批量操作")]
        [Button("全部关闭", ButtonSizes.Medium)]
        private void DisableAll()
        {
            var config = (EmberDebugConfigSO)target;
            config.DisableAll();
            EditorUtility.SetDirty(config);
        }

        [PropertyOrder(-10)]
        [HorizontalGroup("批量操作")]
        [Button("清理空项", ButtonSizes.Medium)]
        private void CleanEmpty()
        {
            var config = (EmberDebugConfigSO)target;
            config.CleanEmpty();
            EditorUtility.SetDirty(config);
        }
    }
}
