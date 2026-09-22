using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>补充 Resources 资源存在性与类型校验，不执行剧情或播放音频。</summary>
    internal static class NarrativeAssetValidation
    {
        #region 外部方法
        internal static IReadOnlyList<NarrativeError> Validate(NarrativeStorySO story, NarrativeChapterSO chapter,
            NarrativeTableCatalog catalog)
        {
            IReadOnlyList<NarrativeError> issues;
            if (story) story.TryReadDefinition(catalog, out _, out issues);
            else if (chapter) chapter.TryReadDefinition(catalog, out _, out issues);
            else return new NarrativeError[0];
            var errors = new List<NarrativeError>(issues);
            if (catalog?.IsReady != true) return errors;
            var chapters = story ? story.Chapters : new[] { chapter };
            foreach (var current in chapters.Where(c => c))
                foreach (var node in current.Nodes.OfType<NarrativeDialogueSO>())
                    foreach (var command in node.Commands)
                    {
                        if (command == null) continue;
                        var kind = command.Kind == NovelCommandKind.Say ? NovelCommandKind.Voice : NovelScreenRules.ResourceKind(command);
                        if ((kind == NovelCommandKind.Background || kind == NovelCommandKind.Character) && command.VisualAction == NovelVisualAction.Hide) continue;
                        if (!catalog.TryResolve(kind, command.ResourceKey, out var path)) continue;
                        bool visual = kind == NovelCommandKind.Background || kind == NovelCommandKind.Character;
                        Object asset = kind == NovelCommandKind.EffectPlay ? Resources.Load<GameObject>(path) : visual ? Resources.Load<Sprite>(path) : Resources.Load<AudioClip>(path);
                        if (asset is GameObject effect && NovelMediaRules.ValidatePrefab(effect) is string effectError)
                            errors.Add(new NarrativeError("BadEffectPrefab", effectError + "：" + path, current.ChapterId, node.NodeId, command.CommandId, resourceKey: command.ResourceKey));
                        if (!asset) errors.Add(new NarrativeError("MissingResourceAsset", "资源不存在或类型不符：" + path,
                            current.ChapterId, node.NodeId, command.CommandId, resourceKey: command.ResourceKey));
                    }
            return errors;
        }
        #endregion
    }
}
