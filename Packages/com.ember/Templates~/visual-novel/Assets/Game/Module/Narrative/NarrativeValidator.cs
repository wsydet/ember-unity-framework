using System;
using System.Collections.Generic;
using Ember.Basic;

namespace Game.Narrative
{
    /// <summary>角色/资源键查询只消费配表快照，不进行 Unity 资源加载。</summary>
    public interface INarrativeCatalog
    {
        bool IsReady { get; }
        bool HasCharacter(string characterId);
        bool TryResolve(NovelCommandKind kind, string key, out string resourcePath);
    }

    public static class NarrativeValidator
    {
        #region 外部方法
        [HasGC]
        public static IReadOnlyList<NarrativeError> Validate(NovelChapter chapter, INarrativeCatalog catalog,
            IReadOnlyDictionary<string, NovelValue> globals = null, bool allowChapterExit = false)
        {
            var errors = new List<NarrativeError>();
            void Error(string code, string message, string node = null, string command = null,
                string option = null, string key = null)
                => errors.Add(new NarrativeError(code, message, chapter?.Id, node, command, option, key));
            if (chapter == null) { Error("MissingChapter", "章节为空"); return errors.AsReadOnly(); }
            if (string.IsNullOrWhiteSpace(chapter.Id) || chapter.StoryRevision < 1)
                Error("BadChapter", "章节 ID 或修订无效");
            if (catalog == null || !catalog.IsReady) Error("CatalogNotReady", "小说配表尚未就绪");
            var nodes = new HashSet<string>(StringComparer.Ordinal);
            var variables = new Dictionary<string, NovelValue>(StringComparer.Ordinal);
            var commandIds = new HashSet<string>(StringComparer.Ordinal);
            var lineIds = new HashSet<string>(StringComparer.Ordinal);
            var actionIds = new HashSet<string>(StringComparer.Ordinal);
            var optionIds = new HashSet<string>(StringComparer.Ordinal);
            var endingIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (NovelVariable variable in chapter.Variables)
            {
                if (variable == null || string.IsNullOrWhiteSpace(variable.Id) || variables.ContainsKey(variable.Id))
                    Error("BadVariable", "变量 ID 为空或重复");
                else
                {
                    variables.Add(variable.Id, variable.Value);
                    if (!Enum.IsDefined(typeof(NovelValueType), variable.Value.Type)) Error("BadVariable", "变量类型无效");
                }
            }
            foreach (NovelNode node in chapter.Nodes)
                if (node == null || string.IsNullOrWhiteSpace(node.Id) || !nodes.Add(node.Id))
                    Error("BadNodeId", "节点 ID 为空或重复", node?.Id);
            void Target(string id, string node, string option = null)
            { if (string.IsNullOrWhiteSpace(id) || !nodes.Contains(id)) Error("BadTarget", "连接未指向本章有效节点", node, option: option); }
            Target(chapter.EntryId, null);
            foreach (NovelNode node in chapter.Nodes)
            {
                if (node == null) continue;
                if (!Enum.IsDefined(typeof(NovelNodeKind), node.Kind)) Error("BadNodeKind", "节点类型无效", node.Id);
                if (node.Kind == NovelNodeKind.ChapterExit && !allowChapterExit) Error("MissingStory", "章节出口必须在剧情会话中运行", node.Id);
                if (node.Kind == NovelNodeKind.Dialogue || node.Kind == NovelNodeKind.Branch) Target(node.NextId, node.Id);
                if (node.Kind != NovelNodeKind.Dialogue && node.Commands.Count != 0)
                    Error("BadNodeShape", "只有对话段可保存指令", node.Id);
                if (node.Kind != NovelNodeKind.Choice && node.Kind != NovelNodeKind.Branch && node.Routes.Count != 0)
                    Error("BadNodeShape", "此节点不能保存选项或条件分支", node.Id);
                if (node.Kind == NovelNodeKind.Ending && (string.IsNullOrWhiteSpace(node.EndingId) || !endingIds.Add(node.EndingId)))
                    Error("BadEndingId", "结局 ID 为空或重复", node.Id);
                if (node.Kind == NovelNodeKind.Choice && node.Routes.Count == 0) Error("ZeroOptions", "选择没有选项", node.Id);
                foreach (NovelRoute route in node.Routes)
                {
                    if (route == null) { Error("BadRoute", "分支为空", node.Id); continue; }
                    if (string.IsNullOrWhiteSpace(route.Id) || !optionIds.Add(route.Id)) Error("BadOptionId", "选项/分支 ID 为空或重复", node.Id, option: route.Id);
                    if (node.Kind == NovelNodeKind.Choice && string.IsNullOrWhiteSpace(route.Text)) Error("BadOptionText", "选项文字为空", node.Id, option: route.Id);
                    Target(route.TargetId, node.Id, route.Id);
                    if (!Enum.IsDefined(typeof(NovelJunction), route.Condition.Junction)) Error("BadCondition", "条件组合无效", node.Id, option: route.Id);
                    foreach (NovelPredicate p in route.Condition.Predicates)
                    {
                        var source = p?.Scope == NovelVariableScope.Global ? globals : variables;
                        if (p == null || !Enum.IsDefined(typeof(NovelVariableScope), p.Scope) || string.IsNullOrWhiteSpace(p.VariableId) || source == null || !source.TryGetValue(p.VariableId, out NovelValue v) || v.Type != p.Value.Type)
                            Error("BadCondition", "条件变量未声明或类型不符", node.Id, option: route.Id);
                        else if (!Enum.IsDefined(typeof(NovelComparison), p.Comparison) ||
                            (v.Type != NovelValueType.Int && p.Comparison != NovelComparison.Equal && p.Comparison != NovelComparison.NotEqual))
                            Error("BadComparison", "bool/string 仅支持等于和不等于", node.Id, option: route.Id);
                    }
                }
                foreach (NovelCommand c in node.Commands)
                {
                    if (c == null) { Error("BadCommand", "指令为空", node.Id); continue; }
                    if (string.IsNullOrWhiteSpace(c.CommandId) || !commandIds.Add(c.CommandId)) Error("BadCommandId", "指令 ID 为空或重复", node.Id, c.CommandId);
                    if (!Enum.IsDefined(typeof(NovelCommandKind), c.Kind)) Error("BadCommandKind", "指令类型无效", node.Id, c.CommandId);
                    if (float.IsNaN(c.Duration) || float.IsInfinity(c.Duration) || c.Duration < 0) Error("BadDuration", "时长必须是有限非负数", node.Id, c.CommandId);
                    if (c.Kind == NovelCommandKind.Opacity && (!Enum.IsDefined(typeof(NovelTargetKind), c.TargetKind) ||
                        c.TargetKind == NovelTargetKind.Mask || c.TargetKind == NovelTargetKind.Effect ||
                        c.TargetKind == NovelTargetKind.Character && string.IsNullOrWhiteSpace(c.InstanceId) ||
                        string.IsNullOrWhiteSpace(c.ActionId) || !NovelActionHandle.ValidTime(c.Delay) ||
                        !NovelActionHandle.ValidTime(c.Opacity) || c.Opacity > 1))
                        Error("BadAction", "透明度动作需要有效目标、动作 ID、0–1 透明度和非负延迟；遮罩请用 Cover/Flash，粒子实例使用 EffectPlay/EffectStop", node.Id, c.CommandId);
                    if (c.Kind == NovelCommandKind.HideAllCharacters && !Enum.IsDefined(typeof(NovelEase), c.Ease)) Error("BadEase", "隐藏全部立绘的缓动无效", node.Id, c.CommandId);
                    string cameraError = NovelCameraRules.Validate(c);
                    if (cameraError != null) Error("BadCamera", cameraError, node.Id, c.CommandId);
                    string textError = NovelTextRules.Validate(c);
                    if (textError != null) Error("BadText", textError, node.Id, c.CommandId);
                    string mediaError = NovelMediaRules.Validate(c);
                    if (mediaError != null) Error("BadMedia", mediaError, node.Id, c.CommandId);
                    if (NovelMediaRules.IsAudio(c.Kind) && !actionIds.Add(NovelMediaRules.ActionId(c)))
                        Error("BadActionId", "声音动作 ID 在本章重复", node.Id, c.CommandId);
                    string screenError = NovelScreenRules.Validate(c);
                    if (screenError != null) Error("BadScreenAction", screenError, node.Id, c.CommandId);
                    string actorError = NovelActorRules.Validate(c);
                    if (actorError != null) Error("BadActor", actorError, node.Id, c.CommandId);
                    if (NovelActorRules.IsAction(c.Kind) && !actionIds.Add(c.ActionId ?? ""))
                        Error("BadActionId", "动作 ID 在本章重复", node.Id, c.CommandId);
                    if (c.Kind == NovelCommandKind.WaitActions && (c.WaitActions.Count == 0 ||
                        System.Linq.Enumerable.Any(c.WaitActions, string.IsNullOrWhiteSpace)))
                        Error("BadActionWait", "等待列表不能为空，填写已启动的动作 ID", node.Id, c.CommandId);
                    string bindingError = NovelTextBindings.Validate(c, variables, globals);
                    if (bindingError != null) Error("BadTextBinding", bindingError, node.Id, c.CommandId);
                    string variableError = NovelVariableRules.Validate(c, variables, globals);
                    if (variableError != null) Error("BadVariableOperation", variableError, node.Id, c.CommandId);
                    var assignments = c.Scope == NovelVariableScope.Global ? globals : variables;
                    if (c.Kind == NovelCommandKind.SetVariable && (!Enum.IsDefined(typeof(NovelVariableScope), c.Scope) || string.IsNullOrWhiteSpace(c.VariableId) ||
                        assignments == null || !assignments.TryGetValue(c.VariableId, out NovelValue v) || v.Type != c.Value.Type))
                        Error("BadAssignment", "赋值变量未声明或类型不符", node.Id, c.CommandId);
                    if (c.Kind == NovelCommandKind.Say)
                    {
                        if (string.IsNullOrWhiteSpace(c.LineId) || !lineIds.Add(c.LineId) || c.TextRevision < 1)
                            Error("BadLineId", "台词 ID 为空/重复或修订无效", node.Id, c.CommandId);
                        if (string.IsNullOrWhiteSpace(c.Text)) Error("EmptyText", "台词为空", node.Id, c.CommandId);
                        if (!string.IsNullOrEmpty(c.CharacterId) && (catalog == null || !catalog.HasCharacter(c.CharacterId)))
                            Error("MissingCharacter", "角色键不存在", node.Id, c.CommandId, key: c.CharacterId);
                    }
                    bool needsResource = NovelMediaRules.NeedsResource(c.Kind) || (c.Kind == NovelCommandKind.CrossFade || c.Kind == NovelCommandKind.Wipe) || (c.Kind >= NovelCommandKind.Background && c.Kind <= NovelCommandKind.Voice) || (c.Kind == NovelCommandKind.Say && !string.IsNullOrEmpty(c.ResourceKey));
                    if (c.Kind == NovelCommandKind.Character || c.Kind == NovelCommandKind.Background)
                    {
                        if (!Enum.IsDefined(typeof(NovelPortraitSlot), c.Slot) || !Enum.IsDefined(typeof(NovelVisualAction), c.VisualAction))
                            Error("BadPresentation", "槽位或演出操作无效", node.Id, c.CommandId);
                        if (c.VisualAction == NovelVisualAction.Hide) needsResource = false;
                    }
                    if (needsResource && (string.IsNullOrWhiteSpace(c.ResourceKey) || catalog == null ||
                        !catalog.TryResolve(c.Kind == NovelCommandKind.Say ? NovelCommandKind.Voice : NovelScreenRules.ResourceKind(c), c.ResourceKey, out string path) || string.IsNullOrWhiteSpace(path)))
                        Error("MissingResourceKey", "配表资源键不存在或路径为空", node.Id, c.CommandId, key: c.ResourceKey);
                }
            }
            return errors.AsReadOnly();
        }
        #endregion
    }
}
