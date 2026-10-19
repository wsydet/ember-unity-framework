using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entity;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    public sealed partial class PlayerSchemeWindow
    {
        #region 编辑器面板参数
        [SerializeField] private string _section = "方案与保存", _filter = "";
        [SerializeField] private bool _advanced;
        [SerializeField] private bool _showAvailability;
        [SerializeField] private Vector2 _labScroll, _configScroll;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private OdinMenuTree _navigation;
        private PlayerScheme _panelScheme;
        private readonly List<PlayerSchemePanel> _panels = new();
        private bool _messageIsError;
        private string _validation, _dirtySummary = "", _runtimeSummary = "非 Play · 无运行冻结实例";
        private PlayerScheme _validatedScheme;
        private string _validatedFingerprint;
        private string _configurationIdentity;
        private bool _rebuildPanels;
        private static readonly string[] Sections = { "方案与保存", "动作与机制", "运动参数", "能力与技能", "效果", "动画", "Prefab 表现与碰撞", "攻击与命中" };
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void DisposePanels()
        {
            foreach (var panel in _panels) panel.Dispose();
            _panels.Clear(); _navigation = null; _panelScheme = null;
        }

        // 校验在编辑器更新中执行，绘制只消费结果；不在 getter / Layout 中创建试跑或写资产。
        private void RefreshStatus()
        {
            if (!PlayerEditorAccess.Available) return;
            try
            {
                if (_selected == null) { _validation = null; _dirtySummary = "未选择方案"; _validatedScheme = null; _validatedFingerprint = null; }
                else
                {
                    var assets = PlayerSchemeWorkflow.Configuration(_selected).Values;
                    string identity = string.Join("|", assets.Select(a => a.GetEntityId().ToString()));
                    if (_configurationIdentity != identity) { _configurationIdentity = identity; _rebuildPanels = true; }
                    int dirty = assets.Count(EditorUtility.IsDirty);
                    _dirtySummary = dirty == 0 ? "方案无未保存变化" : $"方案有 {dirty} 个未保存配置";
                    string fingerprint = PlayerSchemeObservationEditor.Fingerprint(_selected);
                    if (_validatedScheme != _selected || fingerprint != _validatedFingerprint)
                    {
                        _validatedScheme = _selected; _validatedFingerprint = fingerprint;
                        try { PlayerSchemeWorkflow.Validate(_selected); _validation = null; }
                        catch (Exception error) { _validation = error.Message; }
                    }
                }
                _runtimeSummary = "非 Play · 无运行冻结实例";
                if (EditorApplication.isPlaying)
                {
                    var players = Resources.FindObjectsOfTypeAll<PlayerController>().Where(p => p.IsPrepared && p.Context != null).ToArray();
                    _runtimeSummary = players.Length == 0 ? "Play · 尚无已准备的 Gameplay 玩家" : string.Join("\n", players.Select(p =>
                        $"冻结实例 {p.name} · 速度 {p.Context.Feel.MoveSpeed:0.###} u/s · " +
                        PlayerSchemeObservationEditor.Describe(p.SchemeObservation, _selected)));
                }
            }
            catch (Exception error) { _validation = error.Message; }
        }

        private void OnGUI()
        {
            if (PlayerEditorAccess.DrawUnavailable()) return;
            // 固定每个 IMGUI 周期的树，避免输入事件重建导致 Layout/Repaint 数量变化。
            if (Event.current.type == EventType.Layout && (_navigation == null || _panelScheme != _selected || _rebuildPanels))
            {
                try { BuildPanels(); _rebuildPanels = false; }
                catch (Exception error) { _validation = error.Message; _validatedScheme = _selected; }
            }
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = position.width < 760 ? 175 : 215;
            try
            {
                DrawContext();
                _tab = GUILayout.Toolbar(_tab, new[] { "手感白盒", "玩家配置" }, GUILayout.Height(27));
                if (_tab == 0)
                {
                    _labScroll = EditorGUILayout.BeginScrollView(_labScroll);
                    DrawLabPage();
                    EditorGUILayout.EndScrollView();
                }
                else DrawConfigurationPage();
            }
            finally { EditorGUIUtility.labelWidth = labelWidth; }
        }

        private void DrawContext()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _selected = (PlayerScheme)EditorGUILayout.ObjectField("编辑方案（两页共享）", _selected, typeof(PlayerScheme), false);
                    using (new EditorGUI.DisabledScope(_selected == null))
                        if (GUILayout.Button("定位", GUILayout.Width(45))) EditorGUIUtility.PingObject(_selected);
                }
                bool readOnly = _selected != null && PlayerSchemeOwnership.IsReadOnly(_selected);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(_selected == null ? "未选择编辑方案" : (readOnly ? "内置只读" : "自定义") + " · " + (_validatedScheme == _selected ? _dirtySummary : "正在检查配置…"), EditorStyles.miniBoldLabel);
                    using (new EditorGUI.DisabledScope(_selected == null))
                        if (GUILayout.Button("复制为自定义…", GUILayout.Width(120))) Run(() => Copy(false));
                }
                EditorGUILayout.LabelField(_runtimeSummary, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("编辑不影响当前冻结实例；应用与保存分开，新会话 / 明确重启后生效。", EditorStyles.wordWrappedMiniLabel);
            }
            if (_selected == null) EditorGUILayout.HelpBox("请选择方案，或在“方案与保存”中创建空白方案。", MessageType.Warning);
            if (_validatedScheme == _selected && !string.IsNullOrEmpty(_validation))
                EditorGUILayout.HelpBox("方案校验失败：" + _validation, MessageType.Error);
            if (!string.IsNullOrEmpty(_message))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(_message, _messageIsError ? MessageType.Error : MessageType.Info);
                    if (GUILayout.Button("×", GUILayout.Width(24))) _message = null;
                }
            }
        }

        private void DrawLabPage()
        {
            EditorGUILayout.LabelField("1  设置下次播放", EditorStyles.boldLabel);
            bool whitebox = PlayerDevelopmentEnvironment.UseWhitebox;
            int target = EditorGUILayout.Popup("Gameplay 目标", whitebox ? 1 : 0, new[] { "正式场景 · 项目默认方案", "白盒场景 · 独立开发库" });
            if ((target == 1) != whitebox) Run(() => PlayerDevelopmentEnvironment.UseWhitebox = target == 1);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField(whitebox ? "下次白盒已应用方案" : "正式项目默认方案", whitebox ? PlayerDevelopmentEnvironment.SelectedScheme : Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH)?.Scheme, typeof(PlayerScheme), false);
            using (new EditorGUI.DisabledScope(!whitebox))
            {
                _position = EditorGUILayout.Vector2Field("白盒出生位置 (u)", _position);
                _velocity = EditorGUILayout.Vector2Field("白盒初速度 (u/s)", _velocity);
            }
            EditorGUILayout.LabelField(whitebox ? "白盒使用独立开发库；同次 Play 的 Continue 复用该库。" : "正式场景不使用上方编辑方案或白盒出生参数。", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("2  应用与试跑", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(_selected == null || !whitebox || PlayerDevelopmentEnvironment.IsCleaning))
                if (GUILayout.Button("应用编辑方案与出生参数 → 下次白盒", GUILayout.Height(26))) Run(ApplyLab);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || PlayerDevelopmentEnvironment.IsCleaning || whitebox && _selected == null))
                if (GUILayout.Button(whitebox ? "应用当前白盒设置并播放" : "播放正式场景（项目默认）", GUILayout.Height(30)))
                    Run(() => { if (PlayerDevelopmentEnvironment.UseWhitebox) ApplyLab(); PlayerDevelopmentEnvironment.Play(); });
            if (EditorApplication.isPlaying)
            {
                using (new EditorGUI.DisabledScope(PlayerDevelopmentEnvironment.IsCleaning || whitebox && _selected == null))
                    if (GUILayout.Button("结束本次 Play，应用设置并重启", GUILayout.Height(28)))
                        Run(() => { if (PlayerDevelopmentEnvironment.UseWhitebox) ApplyLab(); PlayerDevelopmentEnvironment.Restart(); });
                if (GUILayout.Button("停止播放")) Run(() => EditorApplication.isPlaying = false);
            }
            EditorGUILayout.LabelField(PlayerDevelopmentEnvironment.Status, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("3  观察与定位", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_selected == null || _selected.Graph == null))
                    if (GUILayout.Button("状态图")) Run(() => Ember.Core.Editor.EmberFsmBlueprintWindow.Open(_selected.Graph));
                if (GUILayout.Button("运动曲线与实测")) Run(PlayerMotionTraceWindow.ShowWindow);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                    if (GUILayout.Button("编辑白盒场景")) Run(PlayerFeelLabWindow.OpenLab);
            }
            _ruler = EditorGUILayout.Toggle("Scene 世界刻度", _ruler);
            PlayerLabTrail.DrawControls();
            _advanced = EditorGUILayout.Foldout(_advanced, "高级 · 运行诊断 / 旧草稿 / 正式验收", true);
            if (_advanced)
            {
                EditorGUILayout.LabelField("开发存档库", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(PlayerDevelopmentEnvironment.StorageRoot, EditorStyles.wordWrappedLabel, GUILayout.Height(42));
                DrawLegacyDrafts();
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || PlayerDevelopmentEnvironment.IsCleaning))
                    if (GUILayout.Button("正式流程验收（新隔离存档库）")) Run(PlayerFormalAcceptance.Launch);
            }
        }

        private void ApplyLab() => PlayerDevelopmentEnvironment.SetSchemeParameters(_selected, _position, _velocity);

        private void DrawConfigurationPage()
        {
            if (_navigation == null) return;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (position.width >= 820)
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(190))) _navigation.DrawMenuTree();
                    var selected = _navigation.Selection.FirstOrDefault();
                    if (selected?.Value is string section) _section = section;
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    if (position.width < 820)
                    {
                        int index = Mathf.Max(0, Array.IndexOf(Sections, _section));
                        int next = EditorGUILayout.Popup("配置分类", index, Sections);
                        _section = Sections[next];
                        if (next != index)
                        {
                            _navigation.Selection.Clear();
                            _navigation.Selection.Add(_navigation.MenuItems.First(i => (string)i.Value == _section));
                        }
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _filter = EditorGUILayout.TextField(new GUIContent("筛选参数", "支持中文标签、字段名、配置名；跨类别搜索"), _filter);
                        if (GUILayout.Button("清除", GUILayout.Width(45))) _filter = "";
                    }
                    _configScroll = EditorGUILayout.BeginScrollView(_configScroll);
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(_filter)) DrawFilteredPanels();
                        else if (_section == Sections[0]) DrawSchemeHome();
                        else if (_section == Sections[6]) PlayerAppearanceWindow.DrawPanel(_selected != null ? _selected.Feel : null, ref _ruler);
                        else if (_section == Sections[7]) DrawCombatPanel();
                        else
                        {
                            foreach (var panel in _panels.Where(p => p.Category == _section)) panel.Draw(_filter);
                            if (_section == Sections[1])
                            {
                                if (_selected != null && GUILayout.Button("转换附加条件 · 打开蓝图编辑")) PlayerSchemeRuleEditor.Open(_selected);
                                DrawAvailability();
                            }
                        }
                    }
                    finally { EditorGUILayout.EndScrollView(); }
                }
            }
        }

        private void DrawFilteredPanels()
        {
            int count = 0;
            foreach (var panel in _panels.Where(p => p.Matches(_filter))) { panel.Draw(_filter); count++; }
            if (count == 0) EditorGUILayout.HelpBox("没有匹配参数。Prefab 组件请到“Prefab 表现与碰撞”中定位。", MessageType.None);
        }

        private void DrawSchemeHome()
        {
            EditorGUILayout.LabelField("选择方案", EditorStyles.boldLabel);
            foreach (string guid in AssetDatabase.FindAssets("t:PlayerScheme", new[] { "Assets" }))
            {
                var scheme = AssetDatabase.LoadAssetAtPath<PlayerScheme>(AssetDatabase.GUIDToAssetPath(guid));
                if (scheme != null && GUILayout.Button(scheme.name + (PlayerSchemeOwnership.IsReadOnly(scheme) ? "  ·  内置只读" : "  ·  自定义"))) _selected = scheme;
            }
            if (GUILayout.Button("创建空白方案…（保留基础移动、普通跳跃、下落）")) Run(() => Copy(true));
            if (_selected == null) return;
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("方案资产 · 显式保存", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(_selected), EditorStyles.wordWrappedMiniLabel, GUILayout.Height(36));
            using (new EditorGUI.DisabledScope(PlayerSchemeOwnership.IsReadOnly(_selected)))
                if (GUILayout.Button("保存当前完整方案", GUILayout.Height(28))) Run(() => PlayerSchemeWorkflow.Save(_selected));
            if (GUILayout.Button("校验完整方案")) Run(() => PlayerSchemeWorkflow.Validate(_selected));
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("正式项目默认 · 写入 PlayerConfig", EditorStyles.boldLabel);
            var config = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH);
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("当前默认", config?.Scheme, typeof(PlayerScheme), false);
            if (config == null) EditorGUILayout.HelpBox("缺少 PlayerConfig。", MessageType.Error);
            else
            {
                EditorGUILayout.LabelField(EditorUtility.IsDirty(config) ? "PlayerConfig 有未保存变化" : "PlayerConfig 无未保存变化", EditorStyles.miniLabel);
                if (GUILayout.Button("将编辑方案设为默认（Undo，不保存）")) Run(() => PlayerSchemeWorkflow.SetDefault(_selected));
                if (GUILayout.Button("保存项目默认引用（PlayerConfig）")) Run(() => AssetDatabase.SaveAssetIfDirty(config));
            }
            _advanced = EditorGUILayout.Foldout(_advanced, "高级 · 比较 / 恢复 / 素材副本 / 旧草稿", true);
            if (!_advanced) return;
            _reference = (PlayerScheme)EditorGUILayout.ObjectField("比较 / 恢复参考", _reference, typeof(PlayerScheme), false);
            using (new EditorGUI.DisabledScope(_reference == null))
            {
                if (GUILayout.Button("比较完整配置")) Run(() => _differences = PlayerSchemeWorkflow.Compare(_selected, _reference));
                using (new EditorGUI.DisabledScope(PlayerSchemeOwnership.IsReadOnly(_selected)))
                    if (GUILayout.Button("恢复为参考（Undo，不保存）")) Run(() => { PlayerSchemeWorkflow.Restore(_selected, _reference); DisposePanels(); });
            }
            if (_differences != null)
            {
                EditorGUILayout.LabelField("差异数：" + _differences.Count);
                foreach (var line in _differences) EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            }
            _art = EditorGUILayout.ObjectField("待复制的共享素材", _art, typeof(UnityEngine.Object), false);
            using (new EditorGUI.DisabledScope(_art == null))
                if (GUILayout.Button("复制素材为自有文件…")) Run(() =>
                {
                    string extension = System.IO.Path.GetExtension(AssetDatabase.GetAssetPath(_art)).TrimStart('.');
                    string path = EditorUtility.SaveFilePanelInProject("复制自有素材", _art.name + "Custom", extension, "复制后手动替换映射，不修改原件。");
                    if (!string.IsNullOrEmpty(path)) _art = PlayerSchemeWorkflow.CopyArt(_art, path);
                });
            DrawLegacyDrafts();
            foreach (var panel in _panels.Where(p => p.Category == Sections[0])) panel.Draw("");
        }

        private void DrawLegacyDrafts()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("旧白盒方案草稿", _legacyLabScheme, typeof(PlayerScheme), false);
                EditorGUILayout.ObjectField("旧尺度核对草稿", _legacyScaleFeel, typeof(PlayerFeelProfile), false);
            }
            if (_legacyLabScheme != null && GUILayout.Button("选择旧白盒草稿（不应用）")) _selected = _legacyLabScheme;
        }

        private void DrawAvailability()
        {
            if (_selected?.Feel == null || _selected.Graph == null) return;
            _showAvailability = EditorGUILayout.Foldout(_showAvailability, "逐状态装配说明（启用 / 关闭原因 / 依赖）", true);
            if (!_showAvailability) return;
            try
            {
                var feel = _selected.Feel.Capture(Physics2D.gravity);
                foreach (var node in _selected.Graph.Nodes)
                    EditorGUILayout.LabelField(node.DisplayName + " · " + node.Id + "\n" + PlayerStateAvailability.Describe(node.Definition, feel, _selected.Powers), EditorStyles.wordWrappedLabel);
            }
            catch (Exception error) { EditorGUILayout.HelpBox(error.Message, MessageType.Error); }
        }

        private void BuildPanels()
        {
            DisposePanels(); _panelScheme = _selected;
            _navigation = new OdinMenuTree();
            foreach (var section in Sections) _navigation.Add(section, section);
            var item = _navigation.MenuItems.FirstOrDefault(i => (string)i.Value == _section);
            if (item != null) _navigation.Selection.Add(item);
            if (_selected == null) return;
            void Add(string category, string title, ScriptableObject asset, string fields = null, string hint = null)
            { if (asset != null) _panels.Add(new PlayerSchemePanel(category, title, asset, fields?.Split(' '), hint)); }
            var move = _selected.Feel?.Movement; var jump = _selected.Feel?.Jump;
            Add(Sections[0], "方案接线（引用只读）", _selected);
            Add(Sections[1], "输入宽限", _selected.Feel?.Input, null, "缓冲与土狼时间各自独立；关闭仍保留数值。");
            Add(Sections[1], "运动机制开关", move, "_accelerated _independentAir _airAccelerated _enableJumpResponse _landing _rolling", "关闭机制保留所有数值；具体速度、时长见“运动参数”。");
            Add(Sections[1], "跳跃与竖直机制开关", jump, "_enableShortJump _enableApexGravity _enableFallGravity _enableFallLimit _enableEdgeHover _enableWallFastSlide _wallReturnEnabled");
            Add(Sections[1], "碰撞补正（独立开关与参数）", move, "_jumpCorrection _groundDashCorrection _airDashCorrection _stepCorrection", "跳跃向左右补正；空冲向上下补正；地冲和跨台阶只向上且需要前方落脚支撑。按距离由近到远搜索，同距离按方向偏好选择。仅对静止实体障碍启用，路径或空间不足时不补正。");
            Add(Sections[1], "能力动作启用", _selected.Powers, null, "挂边 / 翻越、墙面、二段跳、下穿、反弹及冲刺由 Ability / Skill 装配控制；次数、解锁、CD 见“能力与技能”。");
            Add(Sections[2], "地面水平", move, "_speed _accelerated _acceleration _braking _turning", "渐变关闭时即时响应；速率为零表示该阶段不改变速度。");
            Add(Sections[2], "空中水平", move, "_independentAir _airSpeed _airAccelerated _airAcceleration _airBraking _airTurning _enableJumpResponse _jumpResponseDuration", "独立空中关闭时继承地面；空中值保留但不生效。");
            Add(Sections[2], "普通跳独立竖直策略", jump, "_ordinaryJump", "默认关闭。仅合法普通起跳启用，持续至落地；动作接管取消。共享参数继续用于其他自由飞行。");
            Add(Sections[2], "普通跳跃与短跳", jump, "_mode _height _apexTime _launchSpeed _gravityScale _enableShortJump _shortJumpMode _releaseSpeed _minimumHeight _releaseMultiplier", "高度/到顶与直接速度/重力互斥；短跳关闭时修正参数不生效。");
            Add(Sections[2], "顶点 / 下落加速 / 悬停", jump, "_enableApexGravity _apexSpeed _apexMultiplier _enableFallGravity _fallMultiplier _enableFallLimit _fallLimit _enableEdgeHover _edgeHoverDuration", "下落倍率 > 1 加快下降反馈；顶点区间优先，不叠乘。最终下降时间还受顶点范围、限速和落地高度影响。");
            Add(Sections[2], "墙面 / 二段跳 / 反弹", jump, "_headJumpSpeed _airJumpSpeed _airJumpHorizontalMode _airJumpHorizontalSpeed _wallSlideSpeed _enableWallFastSlide _wallFastSlideSpeed _wallJumpX _wallJumpY _wallProtection _wallReturnEnabled _wallReturnDuration _wallReturnMultiplier", "动作启用、解锁和次数见“能力与技能”；关闭动作不会删除运动参数。");
            Add(Sections[2], "地空冲刺", move, "_groundDash _airDash", "启用、解锁及 CD 见“能力与技能”。");
            Add(Sections[2], "跳跃 / 冲刺 / 台阶补正", move, "_jumpCorrection _groundDashCorrection _airDashCorrection _stepCorrection", "最大距离与搜索步长控制容错范围及精度；所有补正默认关闭。地面补正固定向上，方向偏好仅用于跳跃和空中冲刺。");
            Add(Sections[2], "落地 / 翻滚", move, "_landing _rolling");
            Add(Sections[2], "下穿 / 挂边 / 翻越", move, "_dropThroughSpeed _ledge", "翻越依赖挂边；物理尺寸在 Prefab 中单独编辑。");
            Add(Sections[3], "能力与技能装配", _selected.Powers);
            Add(Sections[4], "效果容器开关", _selected, "_enableEffects");
            Add(Sections[4], "动作效果接线", _selected.ActionEffects);
            Add(Sections[5], "节点动画映射", _selected.Animation, null, "动画仅改变表现，不改变玩法时长；共享片段需先复制再修改。");
            foreach (var pair in PlayerSchemeWorkflow.Configuration(_selected))
            {
                if (pair.Value is EntityAbilityDefinition || pair.Value is EntitySkillDefinition) Add(Sections[3], PlayerSchemePanel.AssetLabel(pair.Value), pair.Value);
                if (pair.Value is EntityEffectDefinition) Add(Sections[4], pair.Value.name, pair.Value);
            }
        }
        #endregion
    }
}
