using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Ember.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    [InitializeOnLoad]
    public static class PlayerFsmParameterEditor
    {
        #region 内部参数
        private sealed class Message { public string Text; }
        private static readonly ConditionalWeakTable<EmberFsmBlueprintWindow, Message> Messages = new();
        private static readonly Dictionary<string, bool> Folds = new();
        private static readonly string[] MovementFields = { "_speed", "_accelerated", "_acceleration", "_braking", "_turning", "_independentAir", "_airSpeed", "_airAccelerated", "_airAcceleration", "_airBraking", "_airTurning", "_enableJumpResponse", "_jumpResponseDuration" };
        private static readonly string[] JumpFields = { "_mode", "_height", "_apexTime", "_launchSpeed", "_gravityScale", "_enableShortJump", "_shortJumpMode", "_releaseSpeed", "_minimumHeight", "_releaseMultiplier", "_enableApexGravity", "_apexSpeed", "_apexMultiplier", "_enableFallGravity", "_fallMultiplier", "_enableFallLimit", "_fallLimit", "_enableEdgeHover", "_edgeHoverDuration", "_airJumpSpeed", "_airJumpHorizontalMode", "_airJumpHorizontalSpeed" };
        private static readonly string[] WallFields = { "_wallJump", "_wallSlideSpeed", "_enableWallFastSlide", "_wallFastSlideSpeed", "_wallJumpX", "_wallJumpY", "_wallProtection", "_wallReturnEnabled", "_wallReturnDuration", "_wallReturnMultiplier" };
        private static readonly string[] InputFields = { "_enableJumpBuffer", "_jumpBufferDuration", "_enableCoyote", "_coyoteDuration" };
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        static PlayerFsmParameterEditor() => EmberFsmBlueprintParameters.Register("platformer-feel", Draw, DrawToolbar,
            ownerTemplateId: "platformer-2d", enabled: () => PlayerEditorAccess.Enabled);
        private static void Draw(EmberFsmBlueprintWindow window)
        {
            if (!PlayerEditorAccess.Available) return;
            var scheme = window.ParameterPreset as PlayerScheme;
            if (scheme == null) { EditorGUILayout.HelpBox("请在上方明确选择完整方案；仅打开图不推断写入目标。", MessageType.Info); return; }
            if (scheme.Graph != window.ParameterGraph) { EditorGUILayout.HelpBox("方案与当前基础图不匹配。", MessageType.Warning); return; }
            PlayerSchemeRuleEditor.DrawBlueprint(window);
            if (GUILayout.Button("完整方案 / 复制 / 比较 / 恢复")) PlayerSchemeWindow.Open(scheme);
            var config = ScriptableObject.CreateInstance<PlayerConfig>();
            try
            {
                PlayerSchemeConversion.SelectScheme(config, scheme, false);
                var feel = scheme.Feel;
                
                if (GUILayout.Button("运动曲线与实测（所选运行实例）")) PlayerMotionTraceWindow.Open(window);
                EditorGUILayout.HelpBox("编辑真实 SO · Ctrl+Z / Ctrl+Y\n修改仅影响下次试跑；保存资产不等于保存模板。共享分组会影响所有引用它的预设。", MessageType.None);
                if (Messages.TryGetValue(window, out var message)) EditorGUILayout.HelpBox(message.Text, MessageType.Info);
                try
                {
                    PlayerStateAvailability.Draw(window, config);
                    var preview = feel.Capture(Physics2D.gravity);
                    EditorGUILayout.LabelField($"基础估算：高度 {preview.EstimatedHeight:F2} u / 到顶 {preview.EstimatedApexTime:F3} s", EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField($"起跳速度 {preview.LaunchSpeed:F2} u/s · 基础重力倍率 {preview.GravityScale:F3}（不含运动修正/碰撞）", EditorStyles.wordWrappedLabel);
                }
                catch (Exception error) { EditorGUILayout.HelpBox(error.Message, MessageType.Error); }
                if (window.ParameterNode != null)
                    EditorGUILayout.LabelField("所选状态：" + window.ParameterNode.DisplayName + "；运动参数由下列共享配置提供。", EditorStyles.wordWrappedLabel);
                DrawGroup("移动 / 空中水平", feel.Movement, MovementFields);
                EditorGUILayout.HelpBox(feel.Movement.IndependentAir
                    ? "空中有效来源：独立空中参数。速度 u/s，加速度、减速度、反向制动力 u/s²；渐变速率为零表示不改变速度。"
                    : "空中有效来源：地面速度及全部地面渐变参数。独立空中值保留但不生效；起跳早期响应开启时按原窗口临时接管。", MessageType.None);
                DrawGroup("单向平台下穿", feel.Movement, new[] { "_dropThroughSpeed" });
                DrawGroup("挂边／翻越 · 几何与分段时长", feel.Movement, new[] { "_ledge" });
                DrawGroup("普通落地／重落地 · 自动运动规则", feel.Movement, new[] { "_landing" });
                DrawGroup("落地翻滚 · 自动运动规则", feel.Movement, new[] { "_rolling" });
                DrawGroup("地面冲刺 / 运动参数", feel.Movement, new[] { "_groundDash" });
                DrawGroup("空中冲刺 / 运动参数", feel.Movement, new[] { "_airDash" });
                DrawGroup("跳跃 / 竖直运动", feel.Jump, JumpFields);
                DrawGroup("踩头反弹 / 启用与解锁由 Powers 管理", feel.Jump, new[] { "_headJumpSpeed" });
                DrawGroup("墙面动作 / 速度与控制窗口", feel.Jump, WallFields);
                DrawGroup("输入窗口", feel.Input, InputFields);
                DrawAirJumpInstallation(config);
                DrawDashInstallation(config);
                DrawSelectedEffects(window);
            }
            catch (Exception error) { EditorGUILayout.HelpBox(error.Message, MessageType.Error); }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }
        private static void DrawDashInstallation(PlayerConfig config)
        {
            if (config.Powers == null) return;
            for (int i = 0; i < config.Powers.SkillSlots.Count; i++)
            {
                var slot = config.Powers.SkillSlots[i];
                if (slot?.Definition == null || (slot.Definition.Id != PlayerGroundDashState.SKILL_ID && slot.Definition.Id != PlayerAirDashState.SKILL_ID)) continue;
                EditorGUILayout.LabelField(slot.Definition.Id + " 装配 · 新会话生效", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || PlayerSchemeOwnership.IsReadOnly(config.Powers) || PlayerSchemeOwnership.IsReadOnly(slot.Definition)))
                {
                    using var serialized = new SerializedObject(config.Powers);
                    serialized.Update();
                    EditorGUILayout.PropertyField(serialized.FindProperty("_skills").GetArrayElementAtIndex(i).FindPropertyRelative("_enabled"), new GUIContent("项目启用此技能"));
                    serialized.ApplyModifiedProperties();
                    EmberFsmBlueprintParameters.DrawProperties(slot.Definition, "_defaultOpen", "_cooldownSeconds", "_limitedUses", "_maximumUses");
                    if (GUILayout.Button("保存此技能装配资产"))
                    { AssetDatabase.SaveAssetIfDirty(config.Powers); AssetDatabase.SaveAssetIfDirty(slot.Definition); }
                }
                EditorGUILayout.HelpBox("地空技能独立 CD；实际 Enter 扣费，打断不返还。动作期间不能保存，恢复不续接动作。解锁与次数修改仅影响新会话；旧档装配不兼容时严格拒绝。", MessageType.None);
            }
        }
        private static void DrawSelectedEffects(EmberFsmBlueprintWindow window)
        {
            var scheme = window.ParameterPreset as PlayerScheme;
            if (scheme == null)
            { EditorGUILayout.HelpBox("未选择效果写入方案；仅查看图结构，不推断默认方案或观察实例为编辑目标。", MessageType.Info); return; }
            EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(scheme), EditorStyles.wordWrappedMiniLabel);
            if (scheme.Graph != window.ParameterGraph)
            { EditorGUILayout.HelpBox("所选方案引用另一基础图，禁止向当前图节点写入。", MessageType.Warning); return; }
            if (scheme.ActionEffects == null)
            { EditorGUILayout.HelpBox("方案尚未迁移。请在该方案 Inspector 执行显式等价迁移。", MessageType.Warning); return; }
            try { scheme.ActionEffects.Validate(scheme.Graph); }
            catch (Exception error) { EditorGUILayout.HelpBox(error.Message, MessageType.Error); }
            if (window.ParameterNode == null) return;
            DrawGroup("所选方案效果容器 / 下次会话生效", scheme, new[] { "_enableEffects" });
            for (int i = 0; i < scheme.ActionEffects.Slots.Count; i++)
            {
                var slot = scheme.ActionEffects.Slots[i];
                if (slot == null || slot.NodeId != window.ParameterNode.Id) continue;
                using var readOnly = new EditorGUI.DisabledScope(PlayerSchemeOwnership.IsReadOnly(scheme.ActionEffects));
                using var data = new SerializedObject(scheme.ActionEffects);
                data.Update();
                var row = data.FindProperty("_slots").GetArrayElementAtIndex(i);
                EditorGUILayout.LabelField("方案动作接线：" + slot.NodeId);
                EditorGUILayout.PropertyField(row.FindPropertyRelative("_actionEffect"));
                EditorGUILayout.PropertyField(row.FindPropertyRelative("_tailEffect"));
                data.ApplyModifiedProperties();
                string[] fields = { "_id", "_lifetime", "_duration", "_damageCategories", "_preventsStagger", "_excludedLayers", "_hasAppearance", "_appearancePriority", "_alpha", "_blinkHz" };
                if (slot.ActionEffect != null) DrawGroup("所选方案动作效果", slot.ActionEffect, fields);
                if (slot.TailEffect != null) DrawGroup("所选方案尾随效果", slot.TailEffect, fields);
            }
            if (GUILayout.Button("显式保存所选方案效果接线"))
                Run(window, () => PlayerActionEffectMigration.Save(scheme), "已保存所选方案效果接线；运行会话保持冻结配置。");
        }
        private static void DrawAirJumpInstallation(PlayerConfig config)
        {
            if (config.Powers == null) return;
            for (int i = 0; i < config.Powers.AbilitySlots.Count; i++)
            {
                var slot = config.Powers.AbilitySlots[i];
                if (slot?.Definition == null || (slot.Definition.Id != PlayerAirJumpState.ABILITY_ID && slot.Definition.Id != PlayerWallJumpState.ABILITY_ID && slot.Definition.Id != PlayerWallSlideState.ABILITY_ID && slot.Definition.Id != PlayerDropThroughState.ABILITY_ID && slot.Definition.Id != PlayerLedgeHangState.ABILITY_ID && slot.Definition.Id != PlayerLedgeClimbState.ABILITY_ID && slot.Definition.Id != PlayerHeadJumpState.ABILITY_ID)) continue;
                EditorGUILayout.LabelField(slot.Definition.Id + " 装配 · 新会话生效", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || PlayerSchemeOwnership.IsReadOnly(config.Powers) || PlayerSchemeOwnership.IsReadOnly(slot.Definition)))
                {
                    using var serialized = new SerializedObject(config.Powers);
                    serialized.Update();
                    EditorGUILayout.PropertyField(serialized.FindProperty("_abilities").GetArrayElementAtIndex(i).FindPropertyRelative("_enabled"), new GUIContent("项目启用此能力"));
                    serialized.ApplyModifiedProperties();
                    EmberFsmBlueprintParameters.DrawProperties(slot.Definition, "_defaultOpen", "_limitedUses", "_maximumUses");
                    if (GUILayout.Button("保存此能力装配资产"))
                    { AssetDatabase.SaveAssetIfDirty(config.Powers); AssetDatabase.SaveAssetIfDirty(slot.Definition); }
                }
                EditorGUILayout.HelpBox("Default Open 仅用于新游戏初始解锁；当前实例资源见下方观察。二段跳仅普通有效起跳补满；墙面能力示例不限次数。修改装配/次数约束可能使旧档不兼容，加载将保留旧档并拒绝补值。", MessageType.None);

            }
        }
        private static void DrawToolbar(EmberFsmBlueprintWindow window)
        {
            if (!PlayerEditorAccess.Available) return;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                window.ParameterPreset = EditorGUILayout.ObjectField("完整方案", window.ParameterPreset as PlayerScheme, typeof(PlayerScheme), false) as PlayerScheme;
                var scheme = window.ParameterPreset as PlayerScheme;
                using (new EditorGUI.DisabledScope(scheme == null || scheme.Graph != window.ParameterGraph))
                {
                    if (GUILayout.Button("应用并白盒试跑", EditorStyles.toolbarButton))
                        Run(window, () => { PlayerSchemeWorkflow.Use(scheme); PlayerDevelopmentEnvironment.UseWhitebox = true; if (EditorApplication.isPlaying) PlayerDevelopmentEnvironment.Restart(); else PlayerDevelopmentEnvironment.Play(); }, "已请求正常 Gameplay 白盒试跑。");
                    using (new EditorGUI.DisabledScope(PlayerSchemeOwnership.IsReadOnly(scheme)))
                        if (GUILayout.Button("保存完整方案", EditorStyles.toolbarButton)) Run(window, () => PlayerSchemeWorkflow.Save(scheme), "已保存完整配置。");
                }
            }
        }
        private static void DrawGroup(string title, ScriptableObject asset, string[] fields)
        {
            string key = title;
            bool open = EditorGUILayout.Foldout(Folds.TryGetValue(key, out var value) && value, title, true);
            Folds[key] = open;
            if (!open) return;
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("实际配置", asset, typeof(ScriptableObject), false);
            if (asset != null) EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(asset), EditorStyles.wordWrappedMiniLabel);
            float width = EditorGUIUtility.labelWidth;
            try { EditorGUIUtility.labelWidth = 190; using (new EditorGUI.DisabledScope(PlayerSchemeOwnership.IsReadOnly(asset))) EmberFsmBlueprintParameters.DrawProperties(asset, fields); }
            finally { EditorGUIUtility.labelWidth = width; }
        }
        private static void Run(EmberFsmBlueprintWindow window, Action action, string success)
        {
            try { action(); Messages.GetValue(window, _ => new Message()).Text = success; }
            catch (Exception error) { Messages.GetValue(window, _ => new Message()).Text = "操作未完成：" + error.Message; }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Apply(PlayerFeelProfile feel)
        {
            PlayerEditorAccess.Require();
            // 复用白盒生命周期；不改正式 PlayerConfig 或恢复目标。
            if (PlayerLabEditorBridge.Active) PlayerFeelLabWindow.Restart(feel, new Vector2(0, 1.05f), Vector2.zero);
            else PlayerFeelLabWindow.Launch(feel, new Vector2(0, 1.05f), Vector2.zero);
        }
        public static void Save(PlayerFeelProfile feel)
        {
            PlayerEditorAccess.Require();
            if (feel == null) throw new ArgumentNullException(nameof(feel));
            _ = feel.Capture(Physics2D.gravity);
            UnityEngine.Object[] assets = { feel, feel.Movement, feel.Jump, feel.Input };
            foreach (var asset in assets)
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset)))
                    throw new InvalidOperationException("请先将预设和分组保存为项目资产。");
            foreach (var asset in assets) PlayerSchemeOwnership.RequireWritable(asset);
            foreach (var asset in assets) AssetDatabase.SaveAssetIfDirty(asset);
        }
        #endregion
    }
}

