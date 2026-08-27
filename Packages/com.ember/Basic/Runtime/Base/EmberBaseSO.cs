using Sirenix.OdinInspector;
using UnityEngine;

namespace Ember.Basic
{
    /// <summary>
    /// 框架 SO 基类 —— 提供继承溯源面板。
    ///
    /// 所有框架内的 ScriptableObject 都继承此类。
    /// 在 Inspector 顶部显示类型继承链，方便理解 SO 的层级关系。
    ///
    /// <b>使用方式：</b>
    /// <code>
    /// [CreateAssetMenu(menuName = "Ember/MyConfig")]
    /// public class MyConfigSO : EmberBaseSO { }
    /// </code>
    /// </summary>
    public class EmberBaseSO : ScriptableObject
    {
        private const int L0_ORDER = -1000;
        private const string GROUP_NAME = "L0: BaseSO";

        [PropertyOrder(L0_ORDER)]
        [FoldoutGroup("$GROUP_NAME", Expanded = true)]
        [BoxGroup("$GROUP_NAME/Chain", ShowLabel = false)]
        [Title("Type Hierarchy", "自动化继承溯源")]
        [ShowInInspector, ReadOnly]
        [TextArea(1, 100)]
        private string InheritanceChain
        {
            get
            {
                var currentType = GetType();
                var chain = "";
                var selfType = currentType;
                currentType = currentType.BaseType;

                while (currentType != null && currentType != typeof(EmberBaseSO) && currentType != typeof(ScriptableObject))
                {
                    chain = " ∟ " + currentType.Name + "\n" + chain;
                    currentType = currentType.BaseType;
                }

                if (string.IsNullOrEmpty(chain))
                    return " [Root]: " + selfType.Name;

                return " [Path]:\n" + chain + " ------------------------------------------------\n [Current]: " + selfType.Name;
            }
        }
    }
}
