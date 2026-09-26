// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using Ember.Basic;
using TMPro;
using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// TMP 的多语言扩展：完整保留 <see cref="TextMeshProUGUI"/> 的能力，额外提供一个可选的本地化 Key。
    /// <para><b>回退链：</b>Key 留空、未注入解析器或查不到条目时，显示原文（作者在 TMP 文本框里写的内容），
    /// 因此未接入多语言的页面行为与普通 TMP 完全一致。</para>
    /// <para><b>从已有 TMP 替换：</b>Inspector 右上角三点菜单 →「替换为 TMPEx」，会保留字体、材质、对齐等全部设置。</para>
    /// <para><b>编辑期不覆盖文本：</b>本组件带 <c>ExecuteAlways</c>，为保证所见即所得，只有运行期才写入多语言文本；
    /// 编辑期预览请用 Inspector 的「多语言」区或小说流程面板。</para>
    /// </summary>
    [AddComponentMenu("Ember/UI/TMPEx（多语言文本）")]
    public class TMPEx : TextMeshProUGUI
    {
        #region 编辑器面板参数

        [SerializeField]
        [Tooltip("多语言 Key。留空时显示原文，行为与普通 TMP 完全相同。")]
        private string _key;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        // 原文在运行时解析，不参与序列化，避免与 TMP 的 m_text 出现两份真源。
        // ExecuteAlways 下 AddComponent 会先于赋值触发 OnEnable，所以除了首次捕获，
        // 外部对 text 的写入也同步更新原文，两种赋值顺序都能得到正确的回退文本。
        private string _source;
        private bool _applying;

        /// <summary>TMP 显示文本。外部写入会同时记为原文；本组件写入多语言文本时不会污染原文。</summary>
        public override string text
        {
            get => base.text;
            set
            {
                if (!_applying) _source = value;
                base.text = value;
            }
        }

        /// <summary>当前使用的多语言 Key。赋值后按当前语言立即刷新。</summary>
        public string Key
        {
            get => _key;
            set
            {
                if (_key == value) return;
                _key = value;
                ApplyLocalizedText();
            }
        }

        /// <summary>Key 留空或解析失败时显示的原文。</summary>
        public string Source => _source ?? m_text;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_source == null) _source = m_text;
            TextLocalization.Register(this);
            ApplyLocalizedText();
        }

        protected override void OnDisable()
        {
            TextLocalization.Unregister(this);
            base.OnDisable();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>按当前语言写入文本；编辑期不动文本，保证所见即所得。</summary>
        internal void ApplyLocalizedText()
        {
            if (!Application.isPlaying || !this) return;
            string target = TextLocalization.TryResolve(_key, out string localized) ? localized : Source;
            if (string.IsNullOrEmpty(target) || m_text == target) return;
            _applying = true;
            try { text = target; }
            finally { _applying = false; }
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 运行期接管这段文本：把 <paramref name="source"/> 写成原文并清空 Key。
        ///
        /// <para>给「本该是静态文案、但运行期会被代码写入」的控件用（标题、选项文字、状态行等）。
        /// 只写 <c>text</c> 而不清 Key 时，下一次 <see cref="TextLocalization.RefreshAll"/>
        /// （例如切语言）会按 Key 把这段文本顶掉，所以这类控件要么不挂 Key，要么用本方法接管。</para>
        ///
        /// <para>反过来，如果希望切语言后回到表里的译文，就不要用本方法，直接写 <c>text</c>。</para>
        /// </summary>
        [HasGC]
        public void SetSource(string source)
        {
            _key = string.Empty;
            text = source;
        }

        /// <summary>把显示文本恢复为原文，供语言切换清理或编辑器调试使用。</summary>
        [HasGC]
        public void RestoreSource()
        {
            string source = Source;
            if (string.IsNullOrEmpty(source) || m_text == source) return;
            _applying = true;
            try { text = source; }
            finally { _applying = false; }
        }

        #endregion
    }
}
