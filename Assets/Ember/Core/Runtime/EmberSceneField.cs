using System;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ember.Core
{
    /// <summary>
    /// 场景文件引用 —— 在 Inspector 中拖拽 .unity 文件选择场景，
    /// 代替手写字符串，避免场景名拼写错误。
    ///
    /// 参考旧项目 <c>SceneField</c> 模式，适配 Ember 框架的 Odin 面板。
    ///
    /// <b>用法：</b>
    /// <code>
    /// [SerializeField] private EmberSceneField _mainScene;
    ///
    /// // 运行时获取场景名
    /// EmberSceneManager.Instance.LoadSceneAsync(_mainScene.SceneName);
    ///
    /// // 隐式转换 → 可直接当 string 用
    /// string path = _mainScene;  // 等价于 _mainScene.SceneName
    /// </code>
    ///
    /// <b>未来可视化编辑器：</b>
    /// 状态节点上显示场景选择器，拖入场景文件即可设置 ScenePath，
    /// 生成代码时自动写入 <c>new EmberSceneField("SceneName")</c>。
    /// </summary>
    [Serializable]
    [InlineProperty]
    public struct EmberSceneField : ISerializationCallbackReceiver
    {
        #region 编辑器面板参数

        [HorizontalGroup("Scene"/*, Width = 0.7f*/)]
        [HideLabel]
        [AssetsOnly]
        [Tooltip("拖入 .unity 场景文件")]
        [SerializeField]
        private UnityEngine.Object _sceneAsset;

        //[HorizontalGroup("Scene", Width = 0.3f)]
        //[HideLabel]
        //[ReadOnly]
        //[GUIColor(0.7f, 0.7f, 0.7f)]
        //[Tooltip("场景名（自动从文件名提取）")]
        //[SerializeField]
        private string _sceneName;

        #endregion

        // ============================================================

        /// <summary>场景名（Build Settings 中的名称）</summary>
        public readonly string SceneName => _sceneName;

        /// <summary>是否已赋值（场景名非空）</summary>
        public readonly bool HasValue => !string.IsNullOrEmpty(_sceneName);

        #region 快捷构造

        /// <summary>从场景名字符串构造（用于代码生成）。</summary>
        public EmberSceneField(string sceneName)
        {
            _sceneAsset = null;
            _sceneName = sceneName;
        }

        /// <summary>空场景引用。</summary>
        public static EmberSceneField None => new();

        #endregion

        #region Editor 工厂

#if UNITY_EDITOR
        /// <summary>从资产路径加载 SceneAsset 并构造。</summary>
        public static EmberSceneField FromAssetPath(string assetPath)
        {
            var field = new EmberSceneField();
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(assetPath);
            field._sceneAsset = asset;
            field._sceneName = asset != null
                ? System.IO.Path.GetFileNameWithoutExtension(assetPath)
                : "";
            return field;
        }
#endif

        #endregion

        #region 隐式转换

        /// <summary>可直接当作 string 使用。</summary>
        public static implicit operator string(EmberSceneField field) => field._sceneName;

        /// <summary>从 string 隐式构造。</summary>
        public static implicit operator EmberSceneField(string sceneName) => new(sceneName);

        #endregion

        #region 序列化回调

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (_sceneAsset != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(_sceneAsset);
                string assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

                if (_sceneName != assetName)
                {
                    _sceneName = assetName;
                }
            }
            else
            {
                // 资产置空时不清空名称（保留手动输入的场景名）
            }
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() { }

        #endregion

        #region 相等性

        public override readonly bool Equals(object obj)
        {
            return obj is EmberSceneField other && _sceneName == other._sceneName;
        }

        public override readonly int GetHashCode() => _sceneName?.GetHashCode() ?? 0;

        public static bool operator ==(EmberSceneField a, EmberSceneField b) => a._sceneName == b._sceneName;
        public static bool operator !=(EmberSceneField a, EmberSceneField b) => a._sceneName != b._sceneName;

        #endregion

        #region 格式化

        public override readonly string ToString() => _sceneName ?? "";

        #endregion
    }
}
