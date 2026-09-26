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

        /// <summary>
        /// 编写提示：<b>不是错误</b>，只有「校验剧情 / 校验章节」按钮会产生，且不参与
        /// <c>TryReadDefinition</c> 的阻断判定（那条路径一旦有 error 就拒绝运行剧情）。
        ///
        /// 目前查两条：同节点里「空实例 ID 的立绘隐藏」落在哪种槽位语义上，
        /// 以及章节卡之前有没有先声明背景。两者运行期都有兜底
        /// （见 NovelSession.Actors 的退化匹配与阅读页的 Backdrop 底板），
        /// 但作者按推荐写法编排才是可维护、可独立试播的写法，所以在编辑期提示一次。
        ///
        /// <para><b>已知边界：</b>只做同节点内的顺序分析。跨节点延续的实例状态
        /// （在上一段显示、在这一段隐藏）不在本提示范围内，避免对分支内容产生误报；
        /// 这类情况由运行期诊断负责。</para>
        /// </summary>
        internal static IReadOnlyList<NarrativeError> ValidateHints(NarrativeStorySO story, NarrativeChapterSO chapter)
        {
            var hints = new List<NarrativeError>();
            var chapters = story ? story.Chapters : new[] { chapter };
            foreach (var current in chapters.Where(c => c))
                foreach (var node in current.Nodes.OfType<NarrativeDialogueSO>())
                { CollectSlotHideHints(current.ChapterId, node, hints); CollectChapterCardHints(current.ChapterId, node, hints); }
            return hints;
        }

        /// <summary>
        /// 章节卡（居中标题）自己就是一整屏底色，退场会把它渐隐掉。
        /// 之前没有背景时，渐隐期间靠阅读页的不透明底板兜底；
        /// 但「先声明背景再放章节卡」才是推荐写法，这里只提示，不阻断。
        /// </summary>
        private static void CollectChapterCardHints(string chapterId, NarrativeDialogueSO node, List<NarrativeError> hints)
        {
            bool backgroundDeclared = false;
            foreach (var command in node.Commands)
            {
                if (command == null) continue;
                if (command.Kind == NovelCommandKind.Background && command.VisualAction != NovelVisualAction.Hide)
                { backgroundDeclared = true; continue; }
                if (command.Kind != NovelCommandKind.Say || command.TextMode != NovelTextMode.Title || backgroundDeclared) continue;
                hints.Add(new NarrativeError("ChapterCardBeforeBackground",
                    "章节卡之前还没有声明背景。卡片退场会把整屏底色渐隐，此时只能靠阅读页底板兜底；" +
                    "推荐像 LastLight 那样在这个节点开头先写一条「背景 → 显示」，章节卡才能落在真正的场景画面上。",
                    chapterId, node.NodeId, command.CommandId));
            }
        }


        /// <summary>节点内已知实例的渲染槽与命名位置；NamedSlot 为 -1 表示没有认领命名位置。</summary>
        private sealed class KnownInstance
        {
            internal string Id;
            internal NovelPortraitSlot Slot;
            internal int NamedSlot;
        }

        private static void CollectSlotHideHints(string chapterId, NarrativeDialogueSO node, List<NarrativeError> hints)
        {
            var live = new List<KnownInstance>();
            foreach (var command in node.Commands)
            {
                if (command == null || command.Kind != NovelCommandKind.Character) continue;
                if (command.VisualAction == NovelVisualAction.Hide)
                {
                    if (!string.IsNullOrEmpty(command.InstanceId))
                    { live.RemoveAll(i => i.Id == command.InstanceId); continue; }
                    var named = live.FirstOrDefault(i => i.NamedSlot == (int)command.Slot);
                    if (named != null) { live.Remove(named); continue; }
                    var unnamed = live.FirstOrDefault(i => i.Slot == command.Slot && i.NamedSlot < 0);
                    if (unnamed != null)
                    {
                        live.Remove(unnamed);
                        hints.Add(new NarrativeError("AmbiguousSlotHide",
                            "空实例 ID 的立绘隐藏会退化为隐藏 " + command.Slot + " 槽上的实例「" + unnamed.Id +
                            "」（该实例是归一化入场的，没有认领命名位置）。请把这条隐藏的实例 ID 写成「" + unnamed.Id + "」，不要依赖槽位语义。",
                            chapterId, node.NodeId, command.CommandId));
                        continue;
                    }
                    var moved = live.FirstOrDefault(i => i.Slot == command.Slot);
                    if (moved != null)
                        hints.Add(new NarrativeError("StaleSlotHide",
                            "空实例 ID 的立绘隐藏指向的 " + command.Slot + " 槽已被移到别处的人物「" + moved.Id +
                            "」占用。按现有语义这条隐藏不会隐藏任何实例；要隐藏它请填写实例 ID。",
                            chapterId, node.NodeId, command.CommandId));
                    continue;
                }
                string id = string.IsNullOrEmpty(command.InstanceId) ? "legacy-" + command.Slot : command.InstanceId;
                live.RemoveAll(i => i.Id == id);
                live.Add(new KnownInstance
                {
                    Id = id,
                    // 空实例 ID 的 Show 没有命名位置时由运行期挑选空闲渲染槽，
                    // 静态分析沿用指令里写的槽位作为近似值。
                    Slot = command.Slot,
                    NamedSlot = command.PositionMode == NovelPositionMode.Named ? (int)command.Slot : -1
                });
            }
        }
        #endregion
    }
}
