using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative
{
    public enum NovelValueType { Bool, Int, String }
    public enum NovelComparison { Equal, NotEqual, Greater, GreaterOrEqual, Less, LessOrEqual }
    public enum NovelJunction { All, Any }
    public enum NovelCommandKind { Say, SetVariable, Wait, Background, Character, BGM, SFX, Voice, Opacity, WaitActions, Move, Scale, Rotate, Mirror, Layer, Gesture, Emphasis, Shake, Cover, Flash, CrossFade, EffectPlay, EffectStop, BGMStop, AmbientPlay, AmbientStop, AmbientVolume, DialogueVisibility, Camera, Wipe }
    public enum NovelNodeKind { Dialogue, Choice, Branch, Ending, ChapterExit }
    public enum NovelVariableScope { Chapter, Global }
    public enum NovelPortraitSlot { Left, Center, Right }
    public enum NovelVisualAction { Show, Replace, Hide }

    /// <summary>值类型用于定义和运行快照；没有可共享的可变内部状态。</summary>
    [Serializable]
    public struct NovelValue
    {
        #region 编辑器面板参数
        [SerializeField] private NovelValueType _type;
        [SerializeField] private bool _bool;
        [SerializeField] private int _int;
        [SerializeField] private string _string;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public NovelValueType Type => _type;
        public bool Bool => _bool;
        public int Int => _int;
        public string String => _string ?? string.Empty;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelValue(bool value) { _type = NovelValueType.Bool; _bool = value; _int = 0; _string = null; }
        public NovelValue(int value) { _type = NovelValueType.Int; _int = value; _bool = false; _string = null; }
        public NovelValue(string value) { _type = NovelValueType.String; _string = value; _int = 0; _bool = false; }
        public override string ToString() => Type == NovelValueType.Bool ? Bool.ToString()
            : Type == NovelValueType.Int ? Int.ToString(System.Globalization.CultureInfo.InvariantCulture) : String;
        #endregion
    }

    [Serializable]
    public sealed class NovelVariable
    {
        #region 编辑器面板参数
        [SerializeField] private string _id;
        [SerializeField] private NovelValue _value;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string Id => _id;
        public NovelValue Value => _value;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelVariable(string id, NovelValue value) { _id = id; _value = value; }
        #endregion
    }

    [Serializable]
    public sealed class NovelPredicate
    {
        #region 编辑器面板参数
        [SerializeField] private string _variableId;
        [SerializeField] private NovelVariableScope _scope;
        [SerializeField] private NovelComparison _comparison;
        [SerializeField] private NovelValue _value;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string VariableId => _variableId;
        public NovelVariableScope Scope => _scope;
        public NovelComparison Comparison => _comparison;
        public NovelValue Value => _value;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelPredicate(string variableId, NovelComparison comparison, NovelValue value, NovelVariableScope scope = NovelVariableScope.Chapter)
        { _variableId = variableId; _comparison = comparison; _value = value; _scope = scope; }
        #endregion
    }

    /// <summary>空条件表示无条件；非空条件为平铺 AND 或 OR，不解释代码。</summary>
    [Serializable]
    public sealed class NovelCondition
    {
        #region 编辑器面板参数
        [SerializeField] private NovelJunction _junction;
        [SerializeField] private List<NovelPredicate> _predicates = new();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public NovelJunction Junction => _junction;
        public IReadOnlyList<NovelPredicate> Predicates => _predicates.AsReadOnly();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelCondition(NovelJunction junction = NovelJunction.All, params NovelPredicate[] predicates)
        { _junction = junction; _predicates = new List<NovelPredicate>(predicates ?? Array.Empty<NovelPredicate>()); }

        public bool Evaluate(IReadOnlyDictionary<string, NovelValue> variables, IReadOnlyDictionary<string, NovelValue> globals = null)
        {
            if (_predicates.Count == 0) return true;
            foreach (NovelPredicate p in _predicates)
            {
                var source = p.Scope == NovelVariableScope.Global ? globals : variables;
                if (source == null || !source.TryGetValue(p.VariableId, out NovelValue actual) || actual.Type != p.Value.Type)
                    throw new InvalidOperationException("条件变量缺失或类型不一致：" + p.VariableId);
                int order = actual.Type == NovelValueType.Int ? actual.Int.CompareTo(p.Value.Int)
                    : actual.Type == NovelValueType.Bool ? actual.Bool.CompareTo(p.Value.Bool)
                    : string.Compare(actual.String, p.Value.String, StringComparison.Ordinal);
                bool match = p.Comparison switch
                {
                    NovelComparison.Equal => order == 0, NovelComparison.NotEqual => order != 0,
                    NovelComparison.Greater => order > 0, NovelComparison.GreaterOrEqual => order >= 0,
                    NovelComparison.Less => order < 0, NovelComparison.LessOrEqual => order <= 0,
                    _ => throw new InvalidOperationException("未知比较类型")
                };
                if (Junction == NovelJunction.All && !match) return false;
                if (Junction == NovelJunction.Any && match) return true;
            }
            return Junction == NovelJunction.All;
        }
        #endregion
    }

    /// <summary>M1 仅执行 Say、SetVariable 和 Wait；演出命令交由后续演出层完成并回报。</summary>
    [Serializable]
    public sealed class NovelCommand
    {
        #region 编辑器面板参数
        [SerializeField] private float _cameraZoom = 1;
        [SerializeField] private NovelWipeDirection _wipeDirection;
        [SerializeField] private NovelTextMode _textMode;
        [SerializeField] private List<NovelTextBeat> _textBeats = new();
        [SerializeField] private bool _dialogueVisible = true;
        [SerializeField] private string _commandId;
        [SerializeField] private NovelCommandKind _kind;
        [SerializeField] private string _lineId;
        [SerializeField] private int _textRevision = 1;
        [SerializeField] private string _characterId;
        [SerializeField, TextArea] private string _text;
        [SerializeField] private string _resourceKey;
        [SerializeField] private string _variableId;
        [SerializeField] private NovelVariableScope _scope;
        [SerializeField] private NovelValue _value;
        [SerializeField] private float _duration;
        [SerializeField] private NovelPortraitSlot _slot;
        [SerializeField] private NovelVisualAction _visualAction;
        [SerializeField] private string _instanceId;
        [SerializeField] private NovelTargetKind _targetKind;
        [SerializeField] private string _actionId;
        [SerializeField] private bool _parallel;
        [SerializeField] private float _delay;
        [SerializeField] private float _opacity = 1;
        [SerializeField] private List<string> _waitActions = new();
        [SerializeField] private NovelPositionMode _positionMode;
        [SerializeField] private Vector2 _position;
        [SerializeField] private Vector2 _scale = Vector2.one;
        [SerializeField] private float _rotation;
        [SerializeField] private bool _mirror;
        [SerializeField] private int _layer;
        [SerializeField] private NovelEase _ease;
        [SerializeField] private NovelGesture _gesture;
        [SerializeField] private float _strength = 1;
        [SerializeField] private bool _exitAfterMove;
        [SerializeField] private NovelEmphasisMode _emphasisMode;
        [SerializeField] private float _dimFactor = .55f;
        [SerializeField] private Vector2 _direction = Vector2.right;
        [SerializeField] private float _frequency = 12;
        [SerializeField] private bool _decay = true;
        [SerializeField] private Color _color = UnityEngine.Color.black;
        [SerializeField] private float _hold;
        [SerializeField] private bool _wholeReader;
        [SerializeField] private bool _persistent;
        [SerializeField] private bool _keepOnSceneChange;
        [SerializeField] private string _bindingId;
        [SerializeField] private float _volume = 1;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public float CameraZoom => _cameraZoom;
        public NovelWipeDirection WipeDirection => _wipeDirection;
        public NovelTextMode TextMode => _textMode;
        public IReadOnlyList<NovelTextBeat> TextBeats => _textBeats ?? (IReadOnlyList<NovelTextBeat>)Array.Empty<NovelTextBeat>();
        public bool DialogueVisible => _dialogueVisible;
        public bool Persistent => _persistent;
        public bool KeepOnSceneChange => _keepOnSceneChange;
        public string BindingId => _bindingId;
        public float Volume => _volume;
        public Vector2 Direction => _direction;
        public float Frequency => _frequency;
        public bool Decay => _decay;
        public Color Color => _color;
        public float Hold => _hold;
        public bool WholeReader => _wholeReader;
        public string CommandId => _commandId;
        public NovelCommandKind Kind => _kind;
        public string LineId => _lineId;
        public int TextRevision => _textRevision;
        public string CharacterId => _characterId;
        public string Text => _text;
        public string ResourceKey => _resourceKey;
        public string VariableId => _variableId;
        public NovelVariableScope Scope => _scope;
        public NovelValue Value => _value;
        public float Duration => _duration;
        public NovelPortraitSlot Slot => _slot;
        public NovelVisualAction VisualAction => _visualAction;
        public string InstanceId => _instanceId;
        public NovelPositionMode PositionMode => _positionMode;
        public Vector2 Position => _position;
        public Vector2 Scale => _scale;
        public float Rotation => _rotation;
        public bool Mirror => _mirror;
        public int Layer => _layer;
        public NovelEase Ease => _ease;
        public NovelGesture Gesture => _gesture;
        public float Strength => _strength;
        public bool ExitAfterMove => _exitAfterMove;
        public NovelEmphasisMode EmphasisMode => _emphasisMode;
        public float DimFactor => _dimFactor;
        public NovelTargetKind TargetKind => _targetKind;
        public string ActionId => _actionId;
        public bool Parallel => _parallel;
        public float Delay => _delay;
        public float Opacity => _opacity;
        public IReadOnlyList<string> WaitActions => _waitActions ?? (IReadOnlyList<string>)Array.Empty<string>();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelCommand(string commandId, NovelCommandKind kind, string text = null, string lineId = null,
            string characterId = null, string resourceKey = null, string variableId = null,
            NovelValue value = default, float duration = 0, int textRevision = 1, NovelVariableScope scope = NovelVariableScope.Chapter,
            NovelPortraitSlot slot = NovelPortraitSlot.Left, NovelVisualAction visualAction = NovelVisualAction.Show,
            string instanceId = null, NovelTargetKind targetKind = NovelTargetKind.Stage, string actionId = null,
            bool parallel = false, float delay = 0, float opacity = 1, string[] waitActions = null,
            NovelPositionMode positionMode = NovelPositionMode.Named, Vector2 position = default,
            Vector2? scale = null, float rotation = 0, bool mirror = false, int layer = 0,
            NovelEase ease = NovelEase.Linear, NovelGesture gesture = NovelGesture.Jump, float strength = 1,
            bool exitAfterMove = false, NovelEmphasisMode emphasisMode = NovelEmphasisMode.Off, float dimFactor = .55f, Vector2? direction = null, float frequency = 12, bool decay = true,
            Color? color = null, float hold = 0, bool wholeReader = false, bool persistent = false, bool keepOnSceneChange = false, string bindingId = null, float volume = 1, NovelTextMode textMode = NovelTextMode.Dialogue, NovelTextBeat[] textBeats = null, bool dialogueVisible = true, float cameraZoom = 1, NovelWipeDirection wipeDirection = NovelWipeDirection.LeftToRight)
        {
            _cameraZoom = cameraZoom; _wipeDirection = wipeDirection;
            _textMode = textMode; _textBeats = new List<NovelTextBeat>(textBeats ?? Array.Empty<NovelTextBeat>()); _dialogueVisible = dialogueVisible;
            _commandId = commandId; _kind = kind; _text = text; _lineId = lineId;
            _characterId = characterId; _resourceKey = resourceKey; _variableId = variableId;
            _value = value; _duration = duration; _textRevision = textRevision; _scope = scope;
            _slot = slot; _visualAction = visualAction;
            _instanceId = instanceId; _targetKind = targetKind; _actionId = actionId;
            _parallel = parallel; _delay = delay; _opacity = opacity;
            _waitActions = new List<string>(waitActions ?? Array.Empty<string>());
            _positionMode = positionMode; _position = position; _scale = scale ?? Vector2.one;
            _rotation = rotation; _mirror = mirror; _layer = layer; _ease = ease;
            _gesture = gesture; _strength = strength; _exitAfterMove = exitAfterMove;
            _emphasisMode = emphasisMode; _dimFactor = dimFactor;
            _direction = direction ?? Vector2.right; _frequency = frequency; _decay = decay;
            _color = color ?? UnityEngine.Color.black; _hold = hold; _wholeReader = wholeReader;
            _persistent = persistent; _keepOnSceneChange = keepOnSceneChange; _bindingId = bindingId; _volume = volume;
        }
        #endregion
    }

    public sealed class NovelRoute
    {
        #region 内部参数
        public string Id { get; }
        public string Text { get; }
        public string TargetId { get; }
        public NovelCondition Condition { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelRoute(string id, string text, string targetId, NovelCondition condition = null)
        { Id = id; Text = text; TargetId = targetId; Condition = condition ?? new NovelCondition(); }
        #endregion
    }

    /// <summary>SO 编译得到的会话定义；连接只转换为 ID，不持久化第二套分支数据。</summary>
    public sealed class NovelNode
    {
        #region 内部参数
        public string Id { get; }
        public NovelNodeKind Kind { get; }
        public string NextId { get; }
        public string EndingId { get; }
        public string Prompt { get; }
        public IReadOnlyList<NovelCommand> Commands { get; }
        public IReadOnlyList<NovelRoute> Routes { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelNode(string id, NovelNodeKind kind, string nextId = null, string endingId = null,
            IList<NovelCommand> commands = null, IList<NovelRoute> routes = null, string prompt = null)
        {
            Id = id; Kind = kind; NextId = nextId; EndingId = endingId; Prompt = prompt;
            Commands = new List<NovelCommand>(commands ?? Array.Empty<NovelCommand>()).AsReadOnly();
            Routes = new List<NovelRoute>(routes ?? Array.Empty<NovelRoute>()).AsReadOnly();
        }
        #endregion
    }

    public sealed class NovelChapter
    {
        #region 内部参数
        public string Id { get; }
        public int StoryRevision { get; }
        public string EntryId { get; }
        public IReadOnlyList<NovelNode> Nodes { get; }
        public IReadOnlyList<NovelVariable> Variables { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelChapter(string id, int storyRevision, string entryId, IList<NovelNode> nodes,
            IList<NovelVariable> variables = null)
        {
            Id = id; StoryRevision = storyRevision; EntryId = entryId;
            Nodes = new List<NovelNode>(nodes ?? Array.Empty<NovelNode>()).AsReadOnly();
            Variables = new List<NovelVariable>(variables ?? Array.Empty<NovelVariable>()).AsReadOnly();
        }
        #endregion
    }
}
