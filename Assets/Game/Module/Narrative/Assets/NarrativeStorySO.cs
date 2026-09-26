using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Basic;
using UnityEngine;

namespace Game.Narrative
{
    public sealed class NarrativeStorySO : EmberBaseSO
    {
        #region 编辑器面板参数
        [SerializeField] private string _storyId = Guid.NewGuid().ToString("N");
        [SerializeField] private string _displayName;
        /// <summary>剧情显示名的多语言 Key；留空用 _displayName。</summary>
        [SerializeField] private string _displayNameKey;
        [SerializeField] private int _revision = 1;
        [SerializeField] private NarrativeChapterSO _entry;
        [SerializeField] private List<NarrativeChapterSO> _chapters = new();
        [SerializeField] private List<NovelVariable> _globals = new();
        [SerializeField] private List<NarrativeChapterLink> _exits = new();
        /// <summary>
        /// 本剧情登记的自定义节点脚本（<see cref="NovelCustomStepSO"/> 子类资产）。
        /// 剧情步骤只保存 ScriptId 字符串，实际资产实例必须在这里登记，才能随剧情一起加载、
        /// 校验并计入剧情指纹。取消登记会让引用它的步骤在剧情校验阶段报错。
        /// </summary>
        [SerializeField] private List<NovelCustomStepSO> _customSteps = new();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string StoryId => _storyId;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        /// <summary>运行期显示名：填了 Key 就取当前语言，否则用 DisplayName（源语言）。</summary>
        public string LocalizedDisplayName => NovelLocalization.TryGetContent(_displayNameKey, out string localized) ? localized : DisplayName;
        public NarrativeChapterSO Entry => _entry;
        public IReadOnlyList<NarrativeChapterSO> Chapters => _chapters.AsReadOnly();
        public IReadOnlyList<NovelVariable> Globals => _globals.AsReadOnly();
        public IReadOnlyList<NarrativeChapterLink> Exits => _exits.AsReadOnly();
        /// <summary>本剧情登记的自定义节点脚本清单。</summary>
        public IReadOnlyList<NovelCustomStepSO> CustomSteps => _customSteps.AsReadOnly();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public bool TryReadDefinition(INarrativeCatalog catalog, out NovelStory definition, out IReadOnlyList<NarrativeError> errors)
        {
            definition = null; var failures = new List<NarrativeError>();
            if (!_entry || !_chapters.Contains(_entry)) failures.Add(new NarrativeError("BadStory", "入口必须引用本剧情已登记章节", null));
            var globals = new Dictionary<string, NovelValue>(StringComparer.Ordinal);
            foreach (var v in _globals)
                if (v != null && !string.IsNullOrWhiteSpace(v.Id) && !globals.ContainsKey(v.Id)) globals.Add(v.Id, v.Value);
            var customSteps = new Dictionary<string, NovelCustomStepSO>(StringComparer.Ordinal);
            foreach (var step in _customSteps)
            {
                if (!step) { failures.Add(new NarrativeError("BadStory", "自定义节点脚本引用为空", null)); continue; }
                string scriptId = step.ScriptId;
                if (string.IsNullOrWhiteSpace(scriptId))
                { failures.Add(new NarrativeError("BadStory", "自定义节点脚本缺少 ScriptId：" + step.name, null)); continue; }
                if (customSteps.ContainsKey(scriptId))
                { failures.Add(new NarrativeError("BadStory", "自定义节点 ScriptId 重复：" + scriptId, null)); continue; }
                customSteps.Add(scriptId, step);
            }
            var chapters = new List<NovelChapter>();
            foreach (var chapter in _chapters)
            {
                if (!chapter) { failures.Add(new NarrativeError("BadStory", "章节引用为空", null)); continue; }
                if (chapter.TryReadDefinition(catalog, out var data, out var issues, globals, true, customSteps)) chapters.Add(data);
                else failures.AddRange(issues);
            }
            var exits = new List<NovelChapterExit>();
            foreach (var link in _exits)
            {
                if (link == null) { exits.Add(null); continue; }
                if (!link.Source || !_chapters.Contains(link.Source) || !link.Exit || !link.Source.Nodes.Contains(link.Exit))
                    failures.Add(new NarrativeError("BadStory", "章节出口引用不属于源章节", link.Source?.ChapterId, link.Exit?.NodeId));
                if (!link.Fallback || !_chapters.Contains(link.Fallback) || link.Routes.Any(r => r == null || !r.Target || !_chapters.Contains(r.Target)))
                    failures.Add(new NarrativeError("BadStory", "路线必须直接引用本剧情已登记章节", link.Source?.ChapterId, link.Exit?.NodeId));
                exits.Add(new NovelChapterExit(link.Source?.ChapterId, link.Exit?.NodeId, link.Fallback?.ChapterId,
                    link.Routes.Select(r => r == null ? null : new NovelRoute(r.Id, r.LocalizedText, r.Target?.ChapterId,
                        r.Condition == null ? null : JsonUtility.FromJson<NovelCondition>(JsonUtility.ToJson(r.Condition)))).ToList()));
            }
            var story = new NovelStory(_storyId, _revision, _entry?.ChapterId, chapters,
                _globals.Select(v => v == null ? null : new NovelVariable(v.Id, v.Value)).ToList(), exits, customSteps);
            failures.AddRange(NarrativeStoryValidator.Validate(story, catalog));
            errors = failures.AsReadOnly(); if (failures.Count == 0) definition = story;
            return definition != null;
        }
        #endregion
    }

    [Serializable]
    public sealed class NarrativeChapterLink
    {
        #region 编辑器面板参数
        [SerializeField] private NarrativeChapterSO _source;
        [SerializeField] private NarrativeChapterExitSO _exit;
        [SerializeField] private List<NarrativeChapterRoute> _routes = new();
        [SerializeField] private NarrativeChapterSO _fallback;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public NarrativeChapterSO Source => _source;
        public NarrativeChapterExitSO Exit => _exit;
        public IReadOnlyList<NarrativeChapterRoute> Routes => _routes.AsReadOnly();
        public NarrativeChapterSO Fallback => _fallback;
        #endregion
    }

    [Serializable]
    public sealed class NarrativeChapterRoute
    {
        #region 编辑器面板参数
        [SerializeField] private string _id = Guid.NewGuid().ToString("N");
        [SerializeField] private string _text;
        /// <summary>路线文字的多语言 Key；留空用 _text。</summary>
        [SerializeField] private string _textKey;
        [SerializeField] private NovelCondition _condition = new();
        [SerializeField] private NarrativeChapterSO _target;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string Id => _id;
        public string Text => _text;
        public string TextKey => _textKey;
        public string LocalizedText => NovelLocalization.TryGetContent(_textKey, out string localized) ? localized : _text;
        public NovelCondition Condition => _condition;
        public NarrativeChapterSO Target => _target;
        #endregion
    }
}
