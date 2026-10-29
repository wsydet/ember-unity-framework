using System;
using System.Collections.Generic;
using Ember.Basic;
using Ember.Core.Fsm;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    public enum PlayerAimMode { Independent, Movement }

    [CreateAssetMenu(menuName="Game/Player/Topdown Scheme",fileName="PlayerScheme")]
    public sealed class PlayerScheme : EmberBaseSO
    {
        #region 编辑器面板参数
        [SerializeField] private string _id="topdown.starter";
        [SerializeField] private EmberFsmGraph _graph;
        [SerializeField,Min(.01f)] private float _speed=6, _acceleration=45, _braking=60;
        [SerializeField] private PlayerAimMode _aimMode;
        [SerializeField,Range(0,1)] private float _aimDeadzone=.2f;
        [SerializeField] private Vector2 _defaultFacing=Vector2.right;
        [SerializeField] private LayerMask _obstacles=1;
        [SerializeField,Min(.001f)] private float _skin=.015f;
        [SerializeField] private PlayerAnimationMapping[] _animations=Array.Empty<PlayerAnimationMapping>();
        [SerializeField,Tooltip("缺少动作映射时明确保留占位身体与方向标记；正式角色可关闭此回退。")] private bool _allowPrimitivePresentation=true;
        [SerializeField] private bool _combatEnabled;
        [SerializeField] private PlayerCombatSettings _combat=new();
        [SerializeField] private EntityPowerConfig _powers;
        [SerializeField] private Weapons.WeaponDefinition[] _weapons=Array.Empty<Weapons.WeaponDefinition>();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string Id=>_id;
        public EmberFsmGraph Graph=>_graph;
        public IReadOnlyList<Weapons.WeaponDefinition> Weapons=>_weapons;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void Configure(EmberFsmGraph graph,float speed=6,float acceleration=45,float braking=60,
            PlayerAimMode aimMode=PlayerAimMode.Independent,string id="topdown.starter")
        { _graph=graph;_speed=speed;_acceleration=acceleration;_braking=braking;_aimMode=aimMode;_id=id; }

        public void ConfigureAnimation(PlayerAnimationMapping[] mappings,bool allowPrimitiveFallback)
        {_animations=mappings==null?Array.Empty<PlayerAnimationMapping>():(PlayerAnimationMapping[])mappings.Clone();_allowPrimitivePresentation=allowPrimitiveFallback;}
        public void ConfigureCombat(bool enabled,EntityPowerConfig powers,PlayerCombatSettings settings=null)
        {_combatEnabled=enabled;_powers=powers;if(settings!=null)_combat=settings;}
        public void ConfigureWeapons(params Weapons.WeaponDefinition[] weapons)=>_weapons=weapons==null?Array.Empty<Weapons.WeaponDefinition>():(Weapons.WeaponDefinition[])weapons.Clone();

        public PlayerSchemeSnapshot Capture()
        {
            if(string.IsNullOrWhiteSpace(_id)||_graph==null||!_graph.Inspect(typeof(PlayerContext)).IsValid||
                !float.IsFinite(_speed)||_speed<=0||!float.IsFinite(_acceleration)||_acceleration<=0||
                !float.IsFinite(_braking)||_braking<=0||!float.IsFinite(_skin)||_skin<=0||_skin>.1f||
                !float.IsFinite(_aimDeadzone)||_aimDeadzone<0||_aimDeadzone>=1||
                !float.IsFinite(_defaultFacing.x)||!float.IsFinite(_defaultFacing.y)||_defaultFacing.sqrMagnitude<.0001f)
                throw new InvalidOperationException("俯视方案、图或运动/朝向数值无效。");
            var animations=new Dictionary<string,PlayerAnimationMapping>(StringComparer.Ordinal);
            foreach(var entry in _animations)
            {if(entry==null||!animations.TryAdd(entry.ActionId,entry.Capture()))throw new InvalidOperationException("动作映射缺失或重复。");}
            if(!_allowPrimitivePresentation)
                foreach(var node in _graph.Nodes)
                    if(string.IsNullOrEmpty(node.DefaultChildId)&&!animations.ContainsKey(node.Id))throw new InvalidOperationException("缺少必要动作映射："+node.Id);
            return new PlayerSchemeSnapshot(_id,_graph,_speed,_acceleration,_braking,_aimMode,_aimDeadzone,_defaultFacing.normalized,_obstacles,_skin,animations,
                _combatEnabled,_combat.Capture(),_powers,_weapons);
        }
        #endregion
    }

    /// <summary>会话捕获的参数与图声明。源方案后续编辑不改变本宿主。</summary>
    public sealed class PlayerSchemeSnapshot:IDisposable
    {
        #region 内部参数
        private readonly List<EmberFsmStateDefinition> _definitions=new();
        private readonly Dictionary<string,PlayerAnimationMapping> _animations;
        private readonly List<UnityEngine.Object> _powerAssets=new();
        private readonly List<Weapons.WeaponSpec> _weapons=new();
        public IReadOnlyList<Weapons.WeaponSpec> Weapons=>_weapons;
        public string Id {get;}
        public EmberFsmGraph Graph {get;}
        public float Speed {get;}
        public float Acceleration {get;}
        public float Braking {get;}
        public PlayerAimMode AimMode {get;}
        public float AimDeadzone {get;}
        public Vector2 DefaultFacing {get;}
        public LayerMask Obstacles {get;}
        public float Skin {get;}
        public bool CombatEnabled {get;}
        public PlayerCombatSpec Combat {get;}
        public EntityPowerConfig Powers {get;}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerSchemeSnapshot(string id,EmberFsmGraph graph,float speed,float acceleration,float braking,
            PlayerAimMode aimMode,float aimDeadzone,Vector2 facing,LayerMask obstacles,float skin,Dictionary<string,PlayerAnimationMapping> animations,
            bool combatEnabled,PlayerCombatSpec combat,EntityPowerConfig powers,Weapons.WeaponDefinition[] weapons)
        {
            Id=id;Speed=speed;Acceleration=acceleration;Braking=braking;AimMode=aimMode;AimDeadzone=aimDeadzone;
            DefaultFacing=facing;Obstacles=obstacles;Skin=skin;
            _animations=animations;
            CombatEnabled=combatEnabled;Combat=combat;
            Graph=ScriptableObject.CreateInstance<EmberFsmGraph>();Graph.hideFlags=HideFlags.HideAndDontSave;
            try
            {
                if(weapons==null||weapons.Length>16||!combatEnabled&&weapons.Length>0)throw new InvalidOperationException("武器装配数量或战斗模块无效。");
                var weaponIds=new HashSet<string>(StringComparer.Ordinal);
                foreach(var weapon in weapons)
                {if(weapon==null||!weaponIds.Add(weapon.Id))throw new InvalidOperationException("武器装配缺失或身份重复。");_weapons.Add(weapon.Capture());}
                Powers=PlayerPowersDefinition.Freeze(powers,_powerAssets);
                if(combatEnabled)
                {
                    using var host=Powers==null?EntityPowerHost.CreateEmpty():EntityPowerHost.Create(Powers);
                    if(!host.Skills.TryGet("dash",out _))
                        throw new InvalidOperationException("角色战斗装配需要 dash Skill；攻击资源属于武器。");
                }
                var nodes=new List<EmberFsmNode>();
                foreach(var node in graph.Nodes)
                {
                    var definition=UnityEngine.Object.Instantiate(node.Definition);definition.hideFlags=HideFlags.HideAndDontSave;_definitions.Add(definition);
                    var bindings=new List<EmberFsmTargetBinding>();
                    foreach(var binding in node.Bindings)bindings.Add(new EmberFsmTargetBinding(binding.Slot,binding.TargetId));
                    nodes.Add(new EmberFsmNode(node.Id,node.DisplayName,definition,node.ParentId,node.DefaultChildId,bindings.ToArray()));
                }
                Graph.Configure(graph.EntryId,nodes.ToArray());
            }
            catch {Dispose();throw;}
        }
        public void Dispose()
        {
            foreach(var definition in _definitions)if(definition!=null)UnityEngine.Object.DestroyImmediate(definition);
            foreach(var asset in _powerAssets)if(asset!=null)UnityEngine.Object.DestroyImmediate(asset);
            _powerAssets.Clear();
            foreach(var weapon in _weapons)weapon.Dispose();_weapons.Clear();
            _definitions.Clear();if(Graph!=null)UnityEngine.Object.DestroyImmediate(Graph);
        }
        internal bool TryAnimation(string actionId,out PlayerAnimationMapping mapping)=>_animations.TryGetValue(actionId,out mapping);
        #endregion
    }
}
