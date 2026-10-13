using System;
using System.Linq;
using Ember.Core.Editor;
using Ember.Core.Fsm;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    internal static class PlayerSchemeRuleEditor
    {
        private static string _message;
        private static void Run(Action action)
        { try { action(); _message = null; } catch (Exception ex) { _message = ex.Message; } }
        internal static void Open(PlayerScheme scheme, string transition = "wall-slide")
        {
            PlayerEditorAccess.Require();
            if (scheme == null) return;
            var window = EmberFsmBlueprintWindow.Open(scheme.Graph);
            window.ParameterPreset = scheme; window.SelectDeclaration("air", false, transition); window.Repaint();
        }
        internal static void Draw(PlayerScheme scheme, string selected = null, EmberFsmObservation observation = null)
        {
            if (!PlayerEditorAccess.Available || scheme == null) return;
            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("转换附加条件 · 入场许可", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("写入方案：" + scheme.name + "\n" + AssetDatabase.GetAssetPath(scheme), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField(EditorUtility.IsDirty(scheme) || scheme.Rules != null && EditorUtility.IsDirty(scheme.Rules)
                ? "规则 / 方案有未保存更改" : "规则 / 方案已保存", EditorStyles.miniLabel);
            if (EditorApplication.isPlaying)
                EditorGUILayout.HelpBox(observation == null ? "Play · 尚未选择观察实例；当前编辑只用于下次新会话。"
                    : "Play · 观察实例 " + observation.InstanceId + "；当前编辑不会替换运行冻结规则。", MessageType.Info);
            bool readOnly = PlayerSchemeOwnership.IsReadOnly(scheme) || PlayerSchemeOwnership.IsReadOnly(scheme.Rules);
            string error = null; EmberFsmRulePlan<PlayerContext> plan = null;
            try { PlayerSchemeRuleWorkflow.ValidateOwnership(scheme); plan = PlayerSchemeRules.Capture(scheme); }
            catch (Exception ex) { error = ex.Message; }
            if (readOnly) EditorGUILayout.HelpBox("内置只读；请在玩家开发工具复制为自定义。", MessageType.Info);
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Error);
            if (_message != null) EditorGUILayout.HelpBox(_message, MessageType.Error);
            if (scheme.RuleMode == PlayerRuleMode.LegacyCode)
            {
                EditorGUILayout.HelpBox("旧版代码规则：继续E01行为。显式等价迁移后可编辑三个入口的附加条件。", MessageType.Info);
                if (observation?.Rules.Count > 0)
                    EditorGUILayout.HelpBox("所选实例仍使用已捕获的附加条件；当前编辑方案为旧版代码模式，两者不同。", MessageType.Info);
                using (new EditorGUI.DisabledScope(readOnly || EditorApplication.isPlayingOrWillChangePlaymode))
                    if (GUILayout.Button("启用规则编辑（等价迁移）")) Run(() => PlayerSchemeRuleWorkflow.Enable(scheme));
                return;
            }
            if (scheme.Rules == null || scheme.Rules.SchemaVersion != 1 || scheme.Rules.Owner != scheme) return;
            EditorGUILayout.LabelField("最终许可 = 原代码许可 AND 下列条件；只影响下次新会话 / 明确重启。", EditorStyles.wordWrappedLabel);
            using (new EditorGUI.DisabledScope(readOnly || error != null))
                if (GUILayout.Button("保存当前完整方案")) Run(() => PlayerSchemeWorkflow.Save(scheme));
            using var data = new SerializedObject(scheme.Rules); data.Update();
            var rules = data.FindProperty("_rules");
            var catalog = PlayerRuleConditions.CreateCatalog(scheme);
            for (int i = 0; i < rules.arraySize; i++)
            {
                var rule = rules.GetArrayElementAtIndex(i); string id = rule.FindPropertyRelative("_transitionId").stringValue;
                if (selected != null && selected != id) continue;
                var handle = new EmberFsmTransition[] { PlayerAirState.AirJump, PlayerAirState.Hang, PlayerAirState.WallSlide }.FirstOrDefault(t => t.Id == id);
                EditorGUILayout.LabelField("air / " + id + " / fixed", EditorStyles.boldLabel);
                if (handle != null)
                {
                    EditorGUILayout.LabelField("不可绕过：" + handle.Sites[0].Condition, EditorStyles.wordWrappedLabel);
                }
                using (new EditorGUI.DisabledScope(readOnly)) EmberConditionEditor.Draw(rule.FindPropertyRelative("_condition"), catalog);
                if (handle != null)
                {
                    var site = handle.Sites[0];
                    EditorGUILayout.LabelField("只读 · 目标槽 " + handle.TargetSlot + " · " + handle.Mode + "\n" + site.Phase + " / " + site.Segment + " / " + site.Order
                        + " · " + site.Continuation + "\n提交：" + site.CommitDescription, EditorStyles.wordWrappedMiniLabel);
                }
                var frozen = observation?.Rules.FirstOrDefault(r => r.NodeId == "air" && r.TransitionId == id && r.SiteId == "fixed");
                if (observation != null)
                {
                    var current = plan?.Descriptions.FirstOrDefault(r => r.TransitionId == id);
                    EditorGUILayout.HelpBox(frozen == null ? "所选运行实例此入口为旧版代码规则。"
                        : current?.Identity == frozen.Identity ? "所选实例冻结条件与当前树一致。"
                        : "编辑树与所选实例不同；运行记录仍属于下面的冻结树，不映射到当前编辑节点。", MessageType.Info);
                    if (frozen != null) EditorGUILayout.LabelField("运行冻结条件\n" + frozen.Condition, EditorStyles.wordWrappedLabel);
                }
            }
            data.ApplyModifiedProperties();
        }
        internal static void DrawBlueprint(EmberFsmBlueprintWindow window)
        {
            var scheme = window.ParameterPreset as PlayerScheme;
            if (scheme == null || scheme.Graph != window.ParameterGraph) return;
            if (window.SelectedRuleNodeId != "air" || window.SelectedRuleId is not ("air-jump" or "wall-slide" or "ledge-hang"))
            {
                EditorGUILayout.LabelField("有限规则编辑：选择Air的三个动作入口。", EditorStyles.wordWrappedMiniLabel);
                using (new EditorGUILayout.HorizontalScope())
                    foreach (string id in new[] { "air-jump", "ledge-hang", "wall-slide" })
                        if (GUILayout.Button(id)) window.SelectDeclaration("air", false, id);
                return;
            }
            Draw(scheme, window.SelectedRuleId, window.ObservedSnapshot);
        }
    }

    [CustomEditor(typeof(PlayerSchemeRules))]
    internal sealed class PlayerSchemeRulesInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (!PlayerEditorAccess.Available) return;
            var rules = (PlayerSchemeRules)target;
            if (rules.Owner == null || rules.Owner.Rules != rules)
            { EditorGUILayout.HelpBox("规则归属缺失或未被所属方案引用。", MessageType.Error); return; }
            PlayerSchemeRuleEditor.Draw(rules.Owner);
        }
    }
}
