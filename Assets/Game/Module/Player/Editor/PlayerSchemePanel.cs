using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entity;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>Odin 分类页中的真实 Unity 序列化资产编辑器；不持有运行配置副本。</summary>
    internal sealed class PlayerSchemePanel : IDisposable
    {
        #region 内部参数
        internal readonly string Category;
        private readonly string _title, _hint;
        private readonly ScriptableObject _asset;
        private readonly string[] _fields;
        private readonly SerializedObject _serialized;
        private readonly HashSet<string> _inactiveReasons = new();
        private bool _open;
        private static readonly Dictionary<string, string> Labels = new()
        {
            ["_speed"] = "速度 (u/s)", ["_accelerated"] = "启用地面渐变", ["_acceleration"] = "起步加速度 (u/s²)",
            ["_braking"] = "刹车减速度 (u/s²)", ["_turning"] = "转向加速度 (u/s²)", ["_independentAir"] = "独立空中控制",
            ["_airSpeed"] = "空中速度 (u/s)", ["_airAccelerated"] = "启用空中渐变", ["_airAcceleration"] = "空中起步 (u/s²)",
            ["_airBraking"] = "空中刹车 (u/s²)", ["_airTurning"] = "空中转向 (u/s²)", ["_enableJumpResponse"] = "启用起跳直接响应",
            ["_jumpResponseDuration"] = "起跳响应窗口 (s)", ["_mode"] = "跳跃参数模式", ["_height"] = "基础跳高 (u)",
            ["_apexTime"] = "基础到顶时间 (s)", ["_launchSpeed"] = "起跳速度 (u/s)", ["_gravityScale"] = "基础重力倍率",
            ["_enableShortJump"] = "启用短跳", ["_shortJumpMode"] = "短跳策略", ["_releaseSpeed"] = "松键上升限速 (u/s)",
            ["_minimumHeight"] = "短跳最低高度 (u)", ["_releaseMultiplier"] = "松键速度倍率", ["_enableApexGravity"] = "启用顶点重力",
            ["_apexSpeed"] = "顶点速度阈值 (u/s)", ["_apexMultiplier"] = "顶点重力倍率", ["_enableFallGravity"] = "启用下落加速／重力修正",
            ["_fallMultiplier"] = "下落重力倍率", ["_enableFallLimit"] = "启用下落限速", ["_fallLimit"] = "下落限速 (u/s)",
            ["_enableEdgeHover"] = "启用离地悬停", ["_edgeHoverDuration"] = "悬停时长 (s)", ["_headJumpSpeed"] = "踩头反弹速度 (u/s)",
            ["_airJumpSpeed"] = "二段跳速度 (u/s)", ["_airJumpHorizontalMode"] = "二段跳水平策略", ["_airJumpHorizontalSpeed"] = "二段跳水平速度 (u/s)",
            ["_wallSlideSpeed"] = "墙滑速度 (u/s)", ["_enableWallFastSlide"] = "启用快速墙滑", ["_wallFastSlideSpeed"] = "快速墙滑速度 (u/s)",
            ["_wallJumpX"] = "踢墙水平速度 (u/s)", ["_wallJumpY"] = "踢墙竖直速度 (u/s)", ["_wallProtection"] = "踢墙保护窗口 (s)",
            ["_wallReturnEnabled"] = "启用回墙修正", ["_wallReturnDuration"] = "回墙窗口 (s)", ["_wallReturnMultiplier"] = "回墙速度倍率",
            ["_enableJumpBuffer"] = "启用跳跃缓冲", ["_jumpBufferDuration"] = "跳跃缓冲 (s)", ["_enableCoyote"] = "启用土狼时间",
            ["_coyoteDuration"] = "土狼时间 (s)", ["_dropThroughSpeed"] = "下穿初速度 (u/s)", ["_groundDash"] = "地面冲刺", ["_airDash"] = "空中冲刺",
            ["_duration"] = "时长 (s)", ["_exitSpeed"] = "结束速度 (u/s)", ["_landing"] = "落地", ["_rolling"] = "翻滚", ["_ledge"] = "挂边与翻越",
            ["_normalEnabled"] = "启用普通落地", ["_normalDuration"] = "普通落地停顿 (s)", ["_hardEnabled"] = "启用重落地",
            ["_hardSpeed"] = "重落地阈值 (u/s)", ["_hardDuration"] = "重落地停顿 (s)", ["_enabled"] = "项目启用", ["_defaultOpen"] = "新进度默认解锁",
            ["_limitedUses"] = "限制使用次数", ["_maximumUses"] = "最大使用次数", ["_cooldownSeconds"] = "冷却 (s)", ["_abilities"] = "能力装配 (Ability)",
            ["_skills"] = "技能装配 (Skill)", ["_definition"] = "配置定义", ["_id"] = "稳定 ID", ["_slots"] = "节点映射",
            ["_enableEffects"] = "启用效果容器", ["_actionEffect"] = "动作效果", ["_tailEffect"] = "尾随效果", ["_nodeId"] = "节点 ID",
            ["_lifetime"] = "效果生命周期", ["_damageCategories"] = "伤害类别", ["_preventsStagger"] = "阻止硬直", ["_excludedLayers"] = "排除碰撞层",
            ["_hasAppearance"] = "启用外观效果", ["_appearancePriority"] = "外观优先级", ["_alpha"] = "透明度", ["_blinkHz"] = "闪烁频率 (Hz)",
            ["_controller"] = "动画 Controller", ["_clip"] = "播放片段", ["_key"] = "覆盖键片段", ["_state"] = "Animator 状态路径",
            ["_blendSeconds"] = "混合时长 (s)", ["_graph"] = "共享基础图", ["_feel"] = "手感配置", ["_powers"] = "能力装配",
            ["_animation"] = "动画配置", ["_actionEffects"] = "动作效果接线", ["_movement"] = "运动配置", ["_jump"] = "跳跃配置", ["_input"] = "输入窗口",
            ["_inputChannel"] = "输入通道", ["_moveAction"] = "移动 Action", ["_jumpAction"] = "跳跃 Action", ["_dashAction"] = "冲刺 Action"
            , ["_layers"] = "探测碰撞层", ["_reach"] = "水平探测距离 (u)", ["_verticalReach"] = "竖直探测距离 (u)",
            ["_gripInset"] = "抓握内缩 (u)", ["_skin"] = "碰撞间隙 (u)", ["_riseDuration"] = "上升时长 (s)",
            ["_inwardDuration"] = "向内移动时长 (s)", ["_speedMode"] = "冲刺速度模式", ["_startSpeed"] = "起始速度 (u/s)",
            ["_endSpeed"] = "结束速度 (u/s)", ["_kind"] = "效果分类",
            ["m_Size"] = "碰撞尺寸 (u)", ["m_Offset"] = "碰撞偏移 (u)", ["_groundDistance"] = "地面探测距离 (u)",
            ["_wallDistance"] = "墙面探测距离 (u)", ["m_LocalPosition"] = "表现偏移 (u)", ["m_LocalScale"] = "表现缩放倍率",
            ["m_Sprite"] = "精灵图", ["m_Color"] = "表现颜色",
            ["_jumpCorrection"] = "跳跃顶角补正", ["_groundDashCorrection"] = "地面冲刺补正",
            ["_airDashCorrection"] = "空中冲刺补正", ["_stepCorrection"] = "普通移动跨小台阶",
            ["_maximumDistance"] = "最大补正距离 (u)", ["_increment"] = "搜索步长 (u)",
            ["_lookAhead"] = "前方探测距离 (u)",
            ["_direction"] = "补正方向偏好", ["_obstacleLayers"] = "可补正障碍层", ["_supportDistance"] = "落脚支撑探测 (u)"
        };
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        internal static string AssetLabel(UnityEngine.Object asset)
        {
            string id = asset is EntityAbilityDefinition ability ? ability.Id : asset is EntitySkillDefinition skill ? skill.Id : null;
            string label = id switch
            {
                PlayerAirJumpState.ABILITY_ID => "二段跳", PlayerWallSlideState.ABILITY_ID => "墙滑",
                PlayerWallJumpState.ABILITY_ID => "踢墙", PlayerDropThroughState.ABILITY_ID => "单向平台下穿",
                PlayerLedgeHangState.ABILITY_ID => "挂边", PlayerLedgeClimbState.ABILITY_ID => "翻越",
                PlayerHeadJumpState.ABILITY_ID => "踩头反弹", PlayerGroundDashState.SKILL_ID => "地面冲刺",
                PlayerAirDashState.SKILL_ID => "空中冲刺", _ => null
            };
            return label != null ? label + " · " + id : asset != null ? asset.name : "未配置";
        }

        private string Label(SerializedProperty property) => FieldLabel(property, _asset);
        internal static string FieldLabel(SerializedProperty property, UnityEngine.Object asset)
        {
            if (asset is PlayerMoveFeel && property.name == "_enabled" && property.propertyPath.Contains("Correction.")) return "启用本组补正";
            if (asset is PlayerAnimationProfile && property.name == "_speed") return "播放速度倍率";
            if (property.propertyType == SerializedPropertyType.ArraySize) return "元素数量";
            if (property.propertyPath.Contains(".Array.data["))
            {
                if (property.propertyType == SerializedPropertyType.Generic)
                {
                    var node = property.FindPropertyRelative("_nodeId");
                    var definition = property.FindPropertyRelative("_definition");
                    return node != null ? node.stringValue : definition?.objectReferenceValue != null ? AssetLabel(definition.objectReferenceValue) : property.displayName;
                }
            }
            return Labels.TryGetValue(property.name, out var label) ? label : property.displayName;
        }

        private bool Contains(string text, string filter) => string.IsNullOrWhiteSpace(filter) || (text ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        private bool MatchesProperty(SerializedProperty property, string filter) => Contains(Label(property) + " " + property.propertyPath + " " + property.tooltip, filter);

        private string InactiveReason(SerializedProperty property)
        {
            bool Off(string path) => _serialized.FindProperty(path)?.boolValue == false;
            string field = property.propertyPath;
            if (_asset is PlayerMoveFeel)
            {
                if (new[] { "_acceleration", "_braking", "_turning" }.Contains(field) && Off("_accelerated")) return "地面渐变关闭；当前即时响应";
                if (new[] { "_airSpeed", "_airAccelerated", "_airAcceleration", "_airBraking", "_airTurning" }.Contains(field) && Off("_independentAir")) return "独立空中关闭；继承地面设置";
                if (new[] { "_airAcceleration", "_airBraking", "_airTurning" }.Contains(field) && Off("_airAccelerated")) return "空中渐变关闭；当前即时响应";
                if (field == "_jumpResponseDuration" && Off("_enableJumpResponse")) return "起跳响应关闭";
                foreach (string correction in new[] { "_jumpCorrection", "_groundDashCorrection", "_airDashCorrection", "_stepCorrection" })
                {
                    if (!field.StartsWith(correction + ".", StringComparison.Ordinal)) continue;
                    if (property.name != "_enabled" && Off(correction + "._enabled")) return "该组碰撞补正关闭";
                    if (property.name == "_direction" && (correction == "_groundDashCorrection" || correction == "_stepCorrection")) return "地面补正固定向上";
                    if (property.name == "_supportDistance" && (correction == "_jumpCorrection" || correction == "_airDashCorrection")) return "空中补正无需地面支撑";
                }
                if (field.StartsWith("_rolling.") && field != "_rolling._enabled" && Off("_rolling._enabled")) return "翻滚关闭";
                if (field.StartsWith("_landing._normal") && field != "_landing._normalEnabled" && Off("_landing._normalEnabled")) return "普通落地停顿关闭";
                if (field.StartsWith("_landing._hard") && field != "_landing._hardEnabled" && Off("_landing._hardEnabled")) return "重落地关闭";
                if (field.StartsWith("_groundDash.") || field.StartsWith("_airDash."))
                {
                    string root = field.Substring(0, field.IndexOf('.'));
                    bool constant = _serialized.FindProperty(root + "._speedMode").enumValueIndex == 0;
                    if (constant && (property.name == "_startSpeed" || property.name == "_endSpeed")) return "恒速模式；使用恒定速度";
                    if (!constant && property.name == "_speed") return "线性模式；使用起始与结束速度";
                }
            }
            if (_asset is PlayerJumpFeel)
            {
                bool direct = _serialized.FindProperty("_mode").enumValueIndex == (int)PlayerJumpParameterMode.Direct;
                if (direct && (field == "_height" || field == "_apexTime")) return "直接模式；使用起跳速度和重力";
                if (!direct && (field == "_launchSpeed" || field == "_gravityScale")) return "高度/到顶模式；运行值由高度和时间推导";
                if (new[] { "_shortJumpMode", "_releaseSpeed", "_minimumHeight", "_releaseMultiplier" }.Contains(field) && Off("_enableShortJump")) return "短跳关闭";
                bool limit = _serialized.FindProperty("_shortJumpMode").enumValueIndex == (int)PlayerShortJumpMode.ReleaseSpeedLimit;
                if (limit && (field == "_minimumHeight" || field == "_releaseMultiplier")) return "短跳限速策略；此参数不参与计算";
                if (!limit && field == "_releaseSpeed") return "最低高度削速策略；此限速不参与计算";
                foreach (var pair in new[] {
                    ("_apexSpeed _apexMultiplier", "_enableApexGravity", "顶点重力关闭"),
                    ("_fallMultiplier", "_enableFallGravity", "下落重力修正关闭"),
                    ("_fallLimit", "_enableFallLimit", "下落限速关闭"),
                    ("_edgeHoverDuration", "_enableEdgeHover", "离地悬停关闭"),
                    ("_wallFastSlideSpeed", "_enableWallFastSlide", "快速墙滑关闭"),
                    ("_wallReturnDuration _wallReturnMultiplier", "_wallReturnEnabled", "回墙修正关闭") })
                    if (pair.Item1.Split(' ').Contains(field) && Off(pair.Item2)) return pair.Item3;
                if (field == "_airJumpHorizontalSpeed" && _serialized.FindProperty("_airJumpHorizontalMode").enumValueIndex == 0) return "保留水平速度；此设定速度不参与计算";
            }
            if (_asset is PlayerInputFeel)
            {
                if (field == "_jumpBufferDuration" && Off("_enableJumpBuffer")) return "跳跃缓冲关闭";
                if (field == "_coyoteDuration" && Off("_enableCoyote")) return "土狼时间关闭";
            }
            return null;
        }

        private void DrawProperty(SerializedProperty property, bool readOnly, string filter, bool all)
        {
            bool match = all || MatchesProperty(property, filter);
            if (property.propertyType == SerializedPropertyType.Generic && property.hasVisibleChildren)
            {
                // 折叠是显示状态；只读资产仍可展开数组和嵌套项。
                if (!string.IsNullOrWhiteSpace(filter)) property.isExpanded = true;
                property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, Label(property), true);
                if (!property.isExpanded) return;
                EditorGUI.indentLevel++;
                var child = property.Copy(); var end = property.GetEndProperty();
                if (child.NextVisible(true))
                    do { DrawProperty(child, readOnly, filter, match); } while (child.NextVisible(false) && !SerializedProperty.EqualContents(child, end));
                EditorGUI.indentLevel--;
                return;
            }
            if (!match) return;
            bool referenceLock = property.propertyType == SerializedPropertyType.ObjectReference && (_asset is PlayerScheme || _asset is PlayerFeelProfile);
            using (new EditorGUI.DisabledScope(readOnly || referenceLock))
            {
                var label = new GUIContent(Label(property), property.tooltip + "\n" + property.propertyPath);
                EditorGUILayout.PropertyField(property, label, false);
            }
            string reason = InactiveReason(property);
            if (reason != null) _inactiveReasons.Add(reason);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerSchemePanel(string category, string title, ScriptableObject asset, string[] fields, string hint)
        {
            Category = category; _title = title; _asset = asset; _hint = hint;
            _serialized = new SerializedObject(asset);
            if (fields != null) _fields = fields;
            else
            {
                var names = new List<string>(); var p = _serialized.GetIterator(); bool enter = true;
                while (p.NextVisible(enter)) { enter = false; if (p.name != "m_Script") names.Add(p.propertyPath); }
                _fields = names.ToArray();
            }
            _open = title == "地面水平" || title == "输入宽限" || title == "能力与技能装配" || title == "效果容器开关" || title == "节点动画映射";
        }

        internal bool Matches(string filter)
        {
            if (Contains(Category + " " + _title + " " + _asset.name, filter)) return true;
            _serialized.UpdateIfRequiredOrScript();
            var p = _serialized.GetIterator();
            while (p.NextVisible(true))
                if (_fields.Any(f => p.propertyPath == f || p.propertyPath.StartsWith(f + ".", StringComparison.Ordinal)) && MatchesProperty(p, filter)) return true;
            return false;
        }

        internal void Draw(string filter)
        {
            if (_asset == null) return;
            bool searching = !string.IsNullOrWhiteSpace(filter);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _open = EditorGUILayout.Foldout(_open || searching, (searching ? Category + " / " : "") + _title, true, EditorStyles.foldoutHeader);
                if (!_open) return;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("写入：" + _asset.name + (PlayerSchemeOwnership.IsReadOnly(_asset) ? " · 只读" : EditorUtility.IsDirty(_asset) ? " · 未保存" : ""), EditorStyles.wordWrappedMiniLabel);
                    if (GUILayout.Button("定位资产", GUILayout.Width(68))) { Selection.activeObject = _asset; EditorGUIUtility.PingObject(_asset); }
                }
                EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(_asset), EditorStyles.wordWrappedMiniLabel, GUILayout.Height(32));
                if (!string.IsNullOrEmpty(_hint)) EditorGUILayout.LabelField(_hint, EditorStyles.wordWrappedMiniLabel);
                _serialized.UpdateIfRequiredOrScript();
                _inactiveReasons.Clear();
                {
                    bool all = !searching || Contains(Category + " " + _title + " " + _asset.name, filter);
                    foreach (string field in _fields)
                    {
                        var property = _serialized.FindProperty(field);
                        if (property != null) DrawProperty(property, PlayerSchemeOwnership.IsReadOnly(_asset), filter, all);
                    }
                }
                if (_serialized.hasModifiedProperties) _serialized.ApplyModifiedProperties();
                foreach (string reason in _inactiveReasons)
                    EditorGUILayout.LabelField("未生效 · " + reason + "；数值保留。", EditorStyles.wordWrappedMiniLabel);
            }
        }

        public void Dispose() => _serialized.Dispose();
        #endregion
    }
}
