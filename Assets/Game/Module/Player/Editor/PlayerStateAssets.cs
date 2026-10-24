using System;
using System.IO;
using System.Linq;
using Ember.Core.Fsm;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>显式安装阶段 4 图装配，保留既有图 GUID、节点 ID 和已配置的 SO 参数。</summary>
    public static class PlayerStateAssets
    {
        private const string DirectoryPath = "Assets/GameResource/Resources/Config/Player";

        public static string Install()
        {
            var graph = AssetDatabase.LoadAssetAtPath<EmberFsmGraph>(DirectoryPath + "/PlayerGraph.asset");
            if (graph == null) throw new InvalidOperationException("请先创建玩家基础资产。");
            Directory.CreateDirectory(DirectoryPath + "/States");
            AssetDatabase.Refresh();
            var locomotion = Get<PlayerLocomotionStateDefinition>("Locomotion");
            var ground = Get<PlayerGroundStateDefinition>("Ground");
            var air = Get<PlayerAirStateDefinition>("Air");
            var idle = Get<PlayerIdleStateDefinition>("Idle");
            var move = Get<PlayerMoveStateDefinition>("Move");
            var jump = Get<PlayerJumpStateDefinition>("Jump");
            var fall = Get<PlayerFallStateDefinition>("Fall");
            var airJump = Get<PlayerAirJumpStateDefinition>("AirJump");
            var wallJump = Get<PlayerWallJumpStateDefinition>("WallJump");
            var wallSlide = Get<PlayerWallSlideStateDefinition>("WallSlide");
            var groundDash = Get<PlayerGroundDashStateDefinition>("GroundDash");
            var airDash = Get<PlayerAirDashStateDefinition>("AirDash");
            var dropThrough = Get<PlayerDropThroughStateDefinition>("DropThrough");
            var hang = Get<PlayerLedgeHangStateDefinition>("LedgeHang");
            var climb = Get<PlayerLedgeClimbStateDefinition>("LedgeClimb");
            var headJump = Get<PlayerHeadJumpStateDefinition>("HeadJump");
            var rolling = Get<PlayerRollingStateDefinition>("Rolling");
            var land = Get<PlayerLandStateDefinition>("Land");
            var hardLand = Get<PlayerHardLandStateDefinition>("HardLand");
            // 保留阶段 3 的 basic-drive 稳定 ID，将其扩展为所有移动状态的公共父节点。
            graph.Configure("basic-drive",
                new EmberFsmNode("basic-drive", "Locomotion", locomotion, defaultChildId: "air", bindings: new[] { new EmberFsmTargetBinding("land", "land"), new EmberFsmTargetBinding("hard-land", "hard-land"), new EmberFsmTargetBinding("rolling", "rolling") }),
                new EmberFsmNode("ground", "Ground", ground, "basic-drive", "idle",
                    new EmberFsmTargetBinding("fall", "fall"), new EmberFsmTargetBinding("jump", "jump"),
                    new EmberFsmTargetBinding("ground-dash", "ground-dash"), new EmberFsmTargetBinding("air-dash", "air-dash"), new EmberFsmTargetBinding("drop-through", "drop-through")),
                new EmberFsmNode("idle", "Idle", idle, "ground", bindings: new[] { new EmberFsmTargetBinding("move", "move") }),
                new EmberFsmNode("move", "Move", move, "ground", bindings: new[] { new EmberFsmTargetBinding("idle", "idle") }),
                new EmberFsmNode("air", "Air", air, "basic-drive", "fall",
                    new EmberFsmTargetBinding("head-jump", "head-jump"), new EmberFsmTargetBinding("ledge-hang", "ledge-hang"),
                    new EmberFsmTargetBinding("ground", "ground"), new EmberFsmTargetBinding("jump", "jump"), new EmberFsmTargetBinding("air-jump", "air-jump"), new EmberFsmTargetBinding("wall-jump", "wall-jump"), new EmberFsmTargetBinding("wall-slide", "wall-slide"),
                    new EmberFsmTargetBinding("ground-dash", "ground-dash"), new EmberFsmTargetBinding("air-dash", "air-dash"), new EmberFsmTargetBinding("drop-through", "drop-through")),
                new EmberFsmNode("head-jump", "HeadJump", headJump, "air", bindings: new[] { new EmberFsmTargetBinding("fall", "fall") }),
                new EmberFsmNode("wall-jump", "WallJump", wallJump, "air", bindings: new[] { new EmberFsmTargetBinding("fall", "fall") }),
                new EmberFsmNode("wall-slide", "WallSlide", wallSlide, "air", bindings: new[] { new EmberFsmTargetBinding("fall", "fall") }),
                new EmberFsmNode("air-jump", "AirJump", airJump, "air", bindings: new[] { new EmberFsmTargetBinding("fall", "fall") }),
                new EmberFsmNode("jump", "Jump", jump, "air", bindings: new[] { new EmberFsmTargetBinding("fall", "fall") }),
                new EmberFsmNode("fall", "Fall", fall, "air", bindings: new[] { new EmberFsmTargetBinding("rising", "jump") }),
                new EmberFsmNode("drop-through", "DropThrough", dropThrough, "basic-drive", bindings: new[] { new EmberFsmTargetBinding("ground", "ground"), new EmberFsmTargetBinding("fall", "fall") }),
                new EmberFsmNode("ledge-hang", "LedgeHang", hang, "basic-drive", bindings: new[] { new EmberFsmTargetBinding("ledge-climb", "ledge-climb"), new EmberFsmTargetBinding("fall", "fall") }),
                new EmberFsmNode("ledge-climb", "LedgeClimb", climb, "basic-drive", bindings: new[] { new EmberFsmTargetBinding("fall", "fall") }),
                new EmberFsmNode("rolling", "Rolling", rolling, "basic-drive", bindings: new[] { new EmberFsmTargetBinding("ground", "ground"), new EmberFsmTargetBinding("fall", "fall"), new EmberFsmTargetBinding("jump", "jump"), new EmberFsmTargetBinding("ground-dash", "ground-dash"), new EmberFsmTargetBinding("drop-through", "drop-through") }),
                new EmberFsmNode("land", "Land", land, "basic-drive", bindings: new[] { new EmberFsmTargetBinding("ground", "ground"), new EmberFsmTargetBinding("fall", "fall"), new EmberFsmTargetBinding("jump", "jump"), new EmberFsmTargetBinding("ground-dash", "ground-dash"), new EmberFsmTargetBinding("drop-through", "drop-through") }),
                new EmberFsmNode("hard-land", "HardLand", hardLand, "basic-drive", bindings: new[] { new EmberFsmTargetBinding("ground", "ground"), new EmberFsmTargetBinding("fall", "fall") }),
                new EmberFsmNode("ground-dash", "GroundDash", groundDash, "basic-drive", bindings: new[] { new EmberFsmTargetBinding("ground", "ground"), new EmberFsmTargetBinding("fall", "fall") }),
                new EmberFsmNode("air-dash", "AirDash", airDash, "basic-drive", bindings: new[] { new EmberFsmTargetBinding("ground", "ground"), new EmberFsmTargetBinding("fall", "fall") }));
            ExtendGroundMotion(graph);
            var report = graph.Inspect(typeof(PlayerContext));
            if (!report.IsValid) throw new InvalidOperationException("玩家状态图无效：" + string.Join("; ", report.Errors));
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssetIfDirty(graph);
            return "Installed 21 state definitions and hierarchical player graph; existing GUID and parameters preserved.";
        }

        /// <summary>显式增量装配；只补地面叶子绑定及两个新节点，不重建其他节点或本机布局。</summary>
        public static void ExtendGroundMotion(EmberFsmGraph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            var nodes = graph.Nodes.ToList();
            string[] ids = { "idle", "move", "accelerate", "decelerate" };
            var accelerate = Get<PlayerAccelerateStateDefinition>("Accelerate");
            var decelerate = Get<PlayerDecelerateStateDefinition>("Decelerate");
            if (!nodes.Any(n => n.Id == "accelerate")) nodes.Add(new EmberFsmNode("accelerate", "Accelerate", accelerate, "ground"));
            if (!nodes.Any(n => n.Id == "decelerate")) nodes.Add(new EmberFsmNode("decelerate", "Decelerate", decelerate, "ground"));
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (!ids.Contains(node.Id)) continue;
                var bindings = node.Bindings.ToList();
                foreach (var target in ids)
                    if (target != node.Id && !bindings.Any(b => b.Slot == target)) bindings.Add(new EmberFsmTargetBinding(target, target));
                nodes[i] = new EmberFsmNode(node.Id, node.DisplayName, node.Definition, node.ParentId, node.DefaultChildId, bindings.ToArray());
            }
            Undo.RecordObject(graph, "扩展地面运动阶段");
            graph.Configure(graph.EntryId, nodes.ToArray());
            EditorUtility.SetDirty(graph);
        }

        private static T Get<T>(string name) where T : ScriptableObject
        {
            string path = DirectoryPath + "/States/Player" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var result = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(result, path);
            return result;
        }
    }
}
