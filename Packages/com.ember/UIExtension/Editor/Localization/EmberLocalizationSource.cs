using System;
using System.Collections.Generic;
using Ember.Table.Editor;
using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>模块提供的文案表注册；不在框架中硬编码具体玩法的表名。</summary>
    [CreateAssetMenu(menuName = "Ember/多语言/源表注册")]
    public sealed class EmberLocalizationSource : ScriptableObject
    {
        #region 编辑器面板参数
        public string DisplayName;
        public string TableId;
        public TextAsset Source;
        public EmberTableDefinition Definition;
        public string KeyColumn = "key";
        public string SourceLanguage = "zh_Hans";
        public string[] Languages = { "zh_Hans", "zh_Hant", "ja", "en" };
        [TextArea] public string TranslationContext;
        public EmberLocalizationTable Output;
        public List<Review> Reviews = new();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        [Serializable] public sealed class Review
        {
            public string Key;
            public string SourceText;
            public bool Pending;
        }
        #endregion
    }
}
