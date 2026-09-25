using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Table;
using Game.Narrative;
using Game.Table.Generated;
using UnityEditor;
using UnityEngine;

namespace Game.UI.Editor
{
    /// <summary>Single-node audition; owns its scene, clock, session, data copies and audio.</summary>
    public sealed class NovelNodePlaybackWindow : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private NarrativeDialogueSO _node;
        [SerializeField] private NarrativeChapterSO _chapter;
        [SerializeField] private NarrativeStorySO _story;
        [SerializeField] private bool _automatic = true, _muted, _useContext, _showSetup = true;
        [SerializeField] private string _backgroundKey;
        [SerializeField] private int _multiplier = 1, _resolution;
        [SerializeField] private List<NovelPlaybackActor> _actors = new()
        {
            new() { Slot = NovelPortraitSlot.Left }, new() { Slot = NovelPortraitSlot.Center }, new() { Slot = NovelPortraitSlot.Right }
        };
        [SerializeField] private List<NovelPlaybackVariable> _variables = new();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private PreviewRenderUtility _preview;
        private GameObject _host;
        private NovelPlaybackView _view;
        private NovelPlaybackAudio _audio;
        private NovelPlaybackStory _data;
        private NovelSession _session;
        private EmberTableEngine _engine;
        private NarrativeTableCatalog _catalog;
        private double _lastTime;
        private long _frame;
        private bool _paused, _changed;
        private string _message, _sourceJson;
        private Vector2 _setupScroll, _statusScroll;
        private Action<NarrativeError> _locate;
        private Vector2 Pixels => _resolution == 1 ? new Vector2(1440, 1080) : new Vector2(1920, 1080);
        private string SourceJson => (_node ? EditorJsonUtility.ToJson(_node) : "") +
            (_chapter ? EditorJsonUtility.ToJson(_chapter) : "") + (_story ? EditorJsonUtility.ToJson(_story) : "");
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnEnable()
        {
            titleContent = new GUIContent("节点演出试播"); minSize = new Vector2(920, 620);
            EditorApplication.update += UpdatePlayback;
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.projectChanged += OnProjectChanged;
            AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
            EditorApplication.quitting += Shutdown;
            _lastTime = EditorApplication.timeSinceStartup;
        }
        private void OnDisable()
        {
            EditorApplication.update -= UpdatePlayback;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.projectChanged -= OnProjectChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;
            EditorApplication.quitting -= Shutdown;
            Shutdown();
        }
        private void OnPlayMode(PlayModeStateChange state)
        { if (state == PlayModeStateChange.ExitingEditMode) { Shutdown(); _message = "进入 Play Mode，试播已停止。"; } }
        private void OnProjectChanged()
        {
            if (_session == null) return;
            _changed = true; SetPaused(true);
            _message = "项目资源已改变，试播已暂停。建议从头重播以加载最新资源。";
        }
        private void OnInspectorUpdate()
        { if (_session != null && SourceJson != _sourceJson) { _changed = true; Repaint(); } }
        private void OnGUI()
        {
            if (!NovelGameplayLayoutWindow.CanOpen())
            { EditorGUILayout.HelpBox("请在 visual-novel 工作区退出 Play Mode，并关闭正式阅读页的 Prefab Mode。", MessageType.Info); return; }
            DrawToolbar();
            EditorGUILayout.LabelField(_node ? _node.name : "请在节点编辑器选中对话节点并点击“播放节点”。", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                {
                    DrawStage(GUILayoutUtility.GetRect(320, 280, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)));
                    // Reserve the same height for every state: diagnostics must never resize the stage.
                    using (new EditorGUILayout.VerticalScope(GUILayout.Height(164), GUILayout.ExpandHeight(false)))
                    {
                        _statusScroll = EditorGUILayout.BeginScrollView(_statusScroll, GUILayout.Height(164));
                        if (_changed) EditorGUILayout.HelpBox("源内容或资源已变化，点击从头重播加载修改。", MessageType.Warning);
                        if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, MessageType.Info);
                        DrawStatus();
                        EditorGUILayout.EndScrollView();
                    }
                }
                if (_showSetup)
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(280)))
                    {
                        _setupScroll = EditorGUILayout.BeginScrollView(_setupScroll);
                        DrawSetup();
                        EditorGUILayout.EndScrollView();
                    }
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledScope(!_node || !_chapter))
                    if (GUILayout.Button(_session == null ? "播放" : "从头重播", EditorStyles.toolbarButton)) StartPlayback();
                using (new EditorGUI.DisabledScope(_session == null || _session.Snapshot.State == NarrativeState.Faulted))
                {
                    if (GUILayout.Button(_paused ? "继续" : "暂停", EditorStyles.toolbarButton)) SetPaused(!_paused);
                }
                using (new EditorGUI.DisabledScope(_session == null))
                    if (GUILayout.Button("停止", EditorStyles.toolbarButton)) StopAndReset();
                using (new EditorGUI.DisabledScope(_session == null || _paused ||
                    (_session.Snapshot.State != NarrativeState.Revealing && _session.Snapshot.State != NarrativeState.AwaitingAdvance)))
                    if (GUILayout.Button("推进", EditorStyles.toolbarButton)) Advance();
                _automatic = GUILayout.Toggle(_automatic, "自动对白", EditorStyles.toolbarButton);
                bool mute = GUILayout.Toggle(_muted, "静音", EditorStyles.toolbarButton);
                if (mute != _muted) { _muted = mute; _audio?.SetMuted(mute); }
                _multiplier = EditorGUILayout.IntPopup(_multiplier, new[] { "1X", "2X", "3X" }, new[] { 1, 2, 3 }, GUILayout.Width(48));
                _showSetup = GUILayout.Toggle(_showSetup, "起始设置", EditorStyles.toolbarButton);
            }
        }
        private void DrawSetup()
        {
            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.DisabledScope(_session != null))
            {
                EditorGUILayout.HelpBox("起始设置仅供本窗口试播。先停止，再修改；不会沿前置分支推断状态。", MessageType.None);
                _resolution = EditorGUILayout.Popup("分辨率", _resolution, new[] { "1920 × 1080", "1440 × 1080" });
                _useContext = EditorGUILayout.Toggle("指定起始画面", _useContext);
                if (_useContext)
                {
                    _backgroundKey = DrawResourceKey("背景资源键", _backgroundKey, false);
                    foreach (var actor in _actors)
                    {
                        actor.Enabled = EditorGUILayout.Toggle("人物 · " + actor.Slot, actor.Enabled);
                        if (!actor.Enabled) continue;
                        actor.ResourceKey = DrawResourceKey("立绘资源键", actor.ResourceKey, true);
                        actor.InstanceId = EditorGUILayout.TextField("实例 ID", actor.InstanceId);
                        actor.CustomPosition = EditorGUILayout.Toggle("归一化坐标", actor.CustomPosition);
                        if (actor.CustomPosition) actor.Position = EditorGUILayout.Vector2Field("位置", actor.Position);
                    }
                }
                GUILayout.Space(8); GUILayout.Label("变量初值", EditorStyles.boldLabel);
                if (_variables.Count == 0) GUILayout.Label("当前章节 / 剧情无变量。");
                foreach (var variable in _variables)
                {
                    string label = (variable.Scope == NovelVariableScope.Global ? "全局 · " : "章节 · ") + variable.Id;
                    variable.Value = variable.Value.Type switch
                    {
                        NovelValueType.Bool => new NovelValue(EditorGUILayout.Toggle(label, variable.Value.Bool)),
                        NovelValueType.Int => new NovelValue(EditorGUILayout.IntField(label, variable.Value.Int)),
                        _ => new NovelValue(EditorGUILayout.TextField(label, variable.Value.String))
                    };
                }
                if (GUILayout.Button("恢复变量定义初值")) { ResetVariables(); GUI.changed = true; }
            }
            if (EditorGUI.EndChangeCheck() && _session == null && _view != null)
            {
                try { BuildStage(); ShowInitialFrame(); _message = null; }
                catch (Exception ex) { _message = "起始设置无效：" + ex.Message; }
            }
            EditorGUILayout.HelpBox("只播放当前对话节点。按钮、选项分支和存读档不在此窗口执行。窗口使用已保存的正式阅读页布局。", MessageType.None);
        }
        private string DrawResourceKey(string label, string key, bool portrait)
        {
            var ids = portrait ? _catalog?.Portraits.Select(p => p.Id).ToArray() : _catalog?.Backgrounds.Select(b => b.Id).ToArray();
            if (ids == null) return EditorGUILayout.TextField(label, key);
            var options = new[] { "" }.Concat(ids).ToList();
            if (!string.IsNullOrEmpty(key) && !options.Contains(key)) options.Add(key);
            int selected = Mathf.Max(0, options.IndexOf(key ?? ""));
            return options[EditorGUILayout.Popup(label, selected, options.Select(s => s == "" ? "（无）" : s).ToArray())];
        }
        private void DrawStatus()
        {
            if (_session == null) return;
            var snapshot = _session.Snapshot;
            string commandId = snapshot.Error?.CommandId ?? snapshot.CommandId;
            int index = _node ? _node.Commands.ToList().FindIndex(c => c?.CommandId == commandId) : -1;
            if (snapshot.State == NarrativeState.Revealing || snapshot.State == NarrativeState.AwaitingAdvance)
                EditorGUILayout.LabelField($"正文范围 {_session.TextPageStart + 1}–{_session.TextPageEnd}" +
                    (_session.TextPageComplete || snapshot.State == NarrativeState.AwaitingAdvance ? " · 再次推进翻页 / 下一句" : " · 推进补全本页"));
            string position = commandId == _data?.DrainCommandId ? "等待本节点剩余动作结束" : index >= 0 ? $"指令 {index + 1}/{_node.Commands.Count} · {commandId}" : commandId;
            EditorGUILayout.LabelField(snapshot.State == NarrativeState.Ended ? "本节点播放结束（持续效果与循环音保留，停止后全部清理）" :
                (_paused ? "已暂停 · " : "") + position + " · " + snapshot.State + " / " + snapshot.Wait);
            if (snapshot.Error != null) EditorGUILayout.HelpBox(snapshot.Error + "\n若缺少人物，请检查起始设置中的实例 ID。", MessageType.Error);
            using (new EditorGUI.DisabledScope(_locate == null || index < 0))
                if (GUILayout.Button("定位当前指令", GUILayout.Width(125)))
                    _locate(new NarrativeError("Preview", "试播定位", _chapter.ChapterId, _node.NodeId, commandId));

            foreach (string action in snapshot.Actions) GUILayout.Label(action, EditorStyles.miniLabel);
        }
        private void DrawStage(Rect available)
        {
            if (_preview == null || _view == null) { GUI.Box(available, "点击播放，或停止后查看起始画面"); return; }
            float fit = Mathf.Min(available.width / Pixels.x, available.height / Pixels.y);
            var area = new Rect(available.center - Pixels * fit / 2, Pixels * fit);
            if (Event.current.type == EventType.Repaint)
            {
                _view.Flush();
                _preview.BeginPreview(area, GUIStyle.none);
                Texture texture;
                try
                {
                    var camera = _preview.camera;
                    camera.cameraType = CameraType.Game; camera.rect = new Rect(0, 0, 1, 1);
                    camera.orthographic = true; camera.orthographicSize = _view.CanvasSize.y / 2;
                    camera.transform.position = new Vector3(0, 0, -10); camera.transform.rotation = Quaternion.identity;
                    camera.nearClipPlane = .1f; camera.farClipPlane = 100;
                    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                    _preview.Render(true);
                }
                finally { texture = _preview.EndPreview(); }
                GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, false);
            }
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && area.Contains(Event.current.mousePosition))
            { Advance(); Event.current.Use(); }
        }
        private void ResetVariables()
        {
            _variables.Clear();
            if (_chapter) _variables.AddRange(_chapter.Variables.Select(v => new NovelPlaybackVariable { Id = v.Id, Value = v.Value, Scope = NovelVariableScope.Chapter }));
            if (_story) _variables.AddRange(_story.Globals.Select(v => new NovelPlaybackVariable { Id = v.Id, Value = v.Value, Scope = NovelVariableScope.Global }));
        }
        private void LoadTables()
        {
            _engine?.Dispose(); _engine = new EmberTableEngine();
            var catalog = GameTables.CreateCatalog(); var bytes = new Dictionary<string, byte[]>();
            foreach (var entry in catalog.Entries)
            {
                var data = Resources.Load<TextAsset>(entry.ResourcePath);
                if (!data) throw new InvalidOperationException("缺少导表产物：" + entry.ResourcePath);
                bytes.Add(entry.TableId, data.bytes);
            }
            if (!_engine.Load(catalog, bytes).Succeeded) throw new InvalidOperationException("配表加载失败，请在配置表中心检查导出。");
            _catalog = new NarrativeTableCatalog(_engine.Database);
            // 编辑态试播与运行期保持一致：装上多语言与皮肤后再建预览页面。
            NovelLocalization.Install(_catalog); NovelSkin.Install(_catalog);
        }
        private List<NovelCommand> InitialCommands()
        {
            var result = new List<NovelCommand>();
            if (!_useContext) return result;
            string prefix = "preview-seed-" + Guid.NewGuid().ToString("N");
            if (!string.IsNullOrWhiteSpace(_backgroundKey)) result.Add(new NovelCommand(prefix + "-bg", NovelCommandKind.Background, resourceKey: _backgroundKey));
            var ids = new HashSet<string>();
            foreach (var actor in _actors.Where(a => a.Enabled))
            {
                if (string.IsNullOrWhiteSpace(actor.InstanceId) || !ids.Add(actor.InstanceId))
                    throw new InvalidOperationException("起始人物需要非空且不重复的实例 ID。");
                result.Add(new NovelCommand(prefix + "-" + actor.Slot, NovelCommandKind.Character,
                    resourceKey: actor.ResourceKey, instanceId: actor.InstanceId, slot: actor.Slot,
                    positionMode: actor.CustomPosition ? NovelPositionMode.Normalized : NovelPositionMode.Named, position: actor.Position));
            }
            return result;
        }
        private void BuildStage()
        {
            ReleaseStage();
            _preview = new PreviewRenderUtility();
            _host = new GameObject("NovelNodePlaybackHost") { hideFlags = HideFlags.HideAndDontSave };
            _preview.AddSingleGO(_host);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NovelGameplayLayoutWindow.PrefabPath);
            if (!prefab) throw new InvalidOperationException("找不到正式阅读页 Prefab。");
            var root = Instantiate(prefab, _host.transform, false);
            _view = new NovelPlaybackView(root, _preview.camera, Pixels);
        }
        private void ShowInitialFrame()
        {
            _view.ClearVisuals();
            foreach (var command in InitialCommands())
            {
                string error = NovelActorRules.Validate(command);
                if (error != null) throw new InvalidOperationException(error);
                if (!_catalog.TryResolve(command.Kind, command.ResourceKey, out string path))
                    throw new InvalidOperationException("起始画面资源键不存在：" + command.ResourceKey);
                var sprite = NovelPlaybackStory.LoadAsset<Sprite>(path);
                if (!sprite) throw new InvalidOperationException("起始画面缺少 Sprite：" + path);
                _view.Visual(command, sprite, 1);
                if (command.Kind == NovelCommandKind.Character)
                {
                    var state = new NovelVisualState { Kind = NovelCommandKind.Character, Slot = command.Slot,
                        InstanceId = command.InstanceId, Key = command.ResourceKey, Opacity = 1 };
                    if (command.PositionMode == NovelPositionMode.Normalized) state.Offset = command.Position - _view.NamedPosition(command.Slot);
                    _view.ApplyActor(state, Vector2.zero, 0);
                }
            }
        }
        private void StartPlayback()
        {
            if (!NovelGameplayLayoutWindow.CanOpen()) return;
            try
            {
                ReleaseSession(); LoadTables(); BuildStage();
                var initial = InitialCommands(); ShowInitialFrame();
                _data = new NovelPlaybackStory(_node, _chapter, _story, initial, _variables);
                _audio = new NovelPlaybackAudio(_host.transform); _audio.SetMuted(_muted);
                _session = new NovelSession(new NovelNewGameRequest("editor-preview"), () => _catalog, _data, _audio);
                _session.AttachView(_view); _session.ConfigureReading(null, 32, 1, 1, 1, 1);
                _session.SetReadingMultiplier(_multiplier);
                _frame = 0; _paused = false; _changed = false; _sourceJson = SourceJson;
                _lastTime = EditorApplication.timeSinceStartup; _message = null;
            }
            catch (Exception ex) { ReleaseSession(); ReleaseStage(); _message = "无法开始试播：" + ex.Message; }
            Repaint();
        }
        private void Advance()
        {
            if (_session == null || _paused) return;
            _automatic = false; _session.Advance(++_frame); Repaint();
        }
        private void SetPaused(bool paused)
        {
            if (_session == null) return;
            _paused = paused;
            if (paused) _session.Pause("EditorPreview"); else _session.Resume("EditorPreview");
            if (!paused) _message = null;
            _lastTime = EditorApplication.timeSinceStartup;
        }
        private void UpdatePlayback()
        {
            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - _lastTime); _lastTime = now;
            if ((_session != null || _preview != null) && !NovelGameplayLayoutWindow.CanOpen()) { Shutdown(); Repaint(); return; }
            if (_session == null) return;
            try
            {
                if (_paused) return;
                // Editor compilation/dialog stalls must not jump over a whole effect.
                if (delta > .5f) { SetPaused(true); _message = "编辑器更新中断，试播已暂停；点击继续。"; Repaint(); return; }
                _audio.AdvanceTime(delta);
                if (_session.ReadingMultiplier != _multiplier) _session.SetReadingMultiplier(_multiplier);
                var desired = _automatic ? NarrativeReadMode.Auto : NarrativeReadMode.Manual;
                if (_session.IsReady && _session.Snapshot.HasActiveSession && _session.ReadMode != desired) _session.SetReadMode(desired);
                _session.Tick(delta, ++_frame);
                if (_session.Snapshot.State == NarrativeState.Faulted) { _audio.Dispose(); _paused = true; }
                Repaint();
            }
            catch (Exception ex) { _audio?.Dispose(); SetPaused(true); _message = "试播已暂停：" + ex.Message; Repaint(); }
        }
        private void StopAndReset()
        {
            ReleaseSession();
            try { LoadTables(); BuildStage(); ShowInitialFrame(); _message = "已停止并恢复起始画面。"; }
            catch (Exception ex) { _message = ex.Message; }
            Repaint();
        }
        private void ReleaseSession()
        {
            try { _session?.Dispose(); }
            finally
            {
                _session = null; _audio?.Dispose(); _audio = null;
                _data?.Dispose(); _data = null; _paused = false; _changed = false;
            }
        }
        private void ReleaseStage()
        {
            try { _view?.Dispose(); }
            finally { _view = null; _preview?.Cleanup(); _preview = null; _host = null; }
        }
        private void Shutdown()
        {
            try { ReleaseSession(); }
            finally { try { ReleaseStage(); } finally { _engine?.Dispose(); _engine = null; _catalog = null; } }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Play(NarrativeDialogueSO node, NarrativeChapterSO chapter, NarrativeStorySO story,
            Action<NarrativeError> locate = null)
        {
            if (!NovelGameplayLayoutWindow.CanOpen()) return;
            var window = GetWindow<NovelNodePlaybackWindow>();
            bool contextChanged = window._chapter != chapter || window._story != story;
            window._node = node; window._chapter = chapter; window._story = story; window._locate = locate;
            if (contextChanged) window.ResetVariables();
            window.Show(); window.StartPlayback();
        }
        #endregion
    }
}
