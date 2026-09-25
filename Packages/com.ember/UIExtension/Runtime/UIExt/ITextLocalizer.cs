// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;

namespace Ember.UIExtension
{
    /// <summary>
    /// 文本多语言解析契约。框架只依赖这个接口，不依赖任何具体配表或语言清单；
    /// 语言表、内容表和语言切换策略都由业务（模板或项目）实现并通过
    /// <see cref="TextLocalization.SetLocalizer"/> 注入。
    /// </summary>
    public interface ITextLocalizer
    {
        /// <summary>当前语言标识，例如 zh_Hans。仅用于编辑器预览显示。</summary>
        string CurrentLanguage { get; }

        /// <summary>可选语言清单，顺序即展示顺序。用于编辑器逐语言预览。</summary>
        IReadOnlyList<string> Languages { get; }

        /// <summary>按当前语言解析 key；查不到返回 false，调用方回退原文。</summary>
        bool TryGet(string key, out string text);

        /// <summary>
        /// 按指定语言解析 key；<paramref name="language"/> 为空时等同按当前语言解析。
        /// 供编辑器预览使用。
        /// </summary>
        bool TryGet(string key, string language, out string text);
    }
}
