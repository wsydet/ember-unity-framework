using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ember.LevelAuthoring.Editor
{
    /// <summary>制作视图投影，不是新的房间资产格式。GraphPosition 永远不表示世界原点。</summary>
    public sealed class EmberRoomGraphNode
    {
        #region 内部参数
        public string Id { get; }
        public string Title { get; }
        public Vector2 GraphPosition { get; }
        public Vector2 GraphSize { get; }
        public Texture Preview { get; }
        public UnityEngine.Object Source { get; }
        public IReadOnlyList<EmberRoomGraphPort> Ports { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EmberRoomGraphNode(string id, string title, Vector2 graphPosition, Vector2 graphSize,
            IEnumerable<EmberRoomGraphPort> ports, Texture preview = null, UnityEngine.Object source = null)
        { Id = id; Title = title; GraphPosition = graphPosition; GraphSize = graphSize; Ports = Array.AsReadOnly(ports.ToArray()); Preview = preview; Source = source; }
        #endregion
    }

    public sealed class EmberRoomGraphPort
    {
        #region 内部参数
        public string Id { get; }
        public bool Enabled { get; }
        public bool Entrance { get; }
        public bool Exit { get; }
        public Vector2 Anchor { get; }
        public Vector2 Normal { get; }
        public Vector2? WorldAnchor { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <param name="anchor">画布卡片归一化坐标，左上为(0,0)，右下为(1,1)。与世界坐标无关。</param>
        public EmberRoomGraphPort(string id, Vector2 anchor, Vector2 normal, bool enabled = true, bool entrance = true, bool exit = true, Vector2? worldAnchor = null)
        { Id = id; Anchor = anchor; Normal = normal; Enabled = enabled; Entrance = entrance; Exit = exit; WorldAnchor = worldAnchor; }
        #endregion
    }

    public sealed class EmberRoomGraphConnection
    {
        #region 内部参数
        public string FromRoom { get; }
        public string FromPort { get; }
        public string ToRoom { get; }
        public string ToPort { get; }
        public bool Bidirectional { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EmberRoomGraphConnection(string fromRoom, string fromPort, string toRoom, string toPort, bool bidirectional = true)
        { FromRoom = fromRoom; FromPort = fromPort; ToRoom = toRoom; ToPort = toPort; Bidirectional = bidirectional; }
        #endregion
    }

    public sealed class EmberRoomGraphSnapshot
    {
        #region 内部参数
        public IReadOnlyList<EmberRoomGraphNode> Nodes { get; }
        public IReadOnlyList<EmberRoomGraphConnection> Connections { get; }
        public bool RequireOpposedNormals { get; }
        public bool RequireWorldAlignment { get; }
        public float WorldTolerance { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EmberRoomGraphSnapshot(IEnumerable<EmberRoomGraphNode> nodes, IEnumerable<EmberRoomGraphConnection> connections,
            bool requireOpposedNormals = false, bool requireWorldAlignment = false, float worldTolerance = .01f)
        { Nodes = Array.AsReadOnly(nodes.ToArray()); Connections = Array.AsReadOnly(connections.ToArray()); RequireOpposedNormals = requireOpposedNormals; RequireWorldAlignment = requireWorldAlignment; WorldTolerance = worldTolerance; }
        #endregion
    }

    /// <summary>在原资产上读写；不得重建房间/场景、修改身份/GUID或借排版移动内容。</summary>
    public interface IEmberRoomGraphAdapter
    {
        string Id { get; }
        bool CanRead(UnityEngine.Object graph);
        EmberRoomGraphSnapshot Read(UnityEngine.Object graph);
        // 必须含图与所有参与版本判断/写入的序列化资产。供冲突检测、Undo 和失败回滚使用。
        IEnumerable<UnityEngine.Object> Targets(UnityEngine.Object graph);
        void WriteLayout(UnityEngine.Object graph, IReadOnlyDictionary<string, Vector2> graphPositions);
        void AddConnection(UnityEngine.Object graph, EmberRoomGraphConnection connection);
        void RemoveConnection(UnityEngine.Object graph, EmberRoomGraphConnection connection);
        IEnumerable<EmberLevelDiagnostic> Validate(UnityEngine.Object graph);
    }

    /// <summary>产品提供合法试玩描述符及初态；通用层不推测出生位置、进度或加载策略。</summary>
    public interface IEmberRoomGraphTrialAdapter
    {
        IEnumerable<EmberLevelDescriptor> Trials(UnityEngine.Object graph, string roomId);
        EmberLevelTrialRequest CreateTrial(UnityEngine.Object graph, string roomId, EmberLevelDescriptor level);
    }
}
