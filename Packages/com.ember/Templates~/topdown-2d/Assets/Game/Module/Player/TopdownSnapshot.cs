using System;
using System.Collections.Generic;
using Ember.Basic;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>俯视 v2 完整冻结记录。武器分别保存资源，动作只从稳定状态恢复。</summary>
    public sealed class TopdownSnapshot
    {
        #region 内部参数
        public const int DATA_VERSION=2;
        public Weapons.WeaponLoadoutProgress Weapons {get;}
        public string Scene {get;}
        public string LevelId {get;}
        public string SpawnId {get;}
        public string SchemeId {get;}
        public Vector2 Position {get;}
        public Vector2 Facing {get;}
        public Vector2 Checkpoint {get;}
        public bool CombatInstalled {get;}
        public float Health {get;}
        public EntityPowerSnapshot Powers {get;}
        private readonly EntityTimedEffectSnapshot[] _effects;
        private readonly TopdownActorRecord[] _actors;
        private readonly TopdownInteractionRecord[] _interactions;
        public int EffectCount=>_effects.Length;
        public int ActorCount=>_actors.Length;
        public int InteractionCount=>_interactions.Length;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public TopdownSnapshot(string scene,string levelId,string spawnId,string schemeId,Vector2 position,Vector2 facing,
            Vector2 checkpoint,bool combatInstalled,float health,EntityPowerSnapshot powers,EntityTimedEffectSnapshot[] effects,
            TopdownActorRecord[] actors,TopdownInteractionRecord[] interactions,Weapons.WeaponLoadoutProgress weapons=null)
        {
            ValidateId(scene);ValidateId(levelId);ValidateId(spawnId);ValidateId(schemeId);
            ValidateVector(position);ValidateVector(facing);ValidateVector(checkpoint);
            if(!scene.StartsWith("Assets/",StringComparison.Ordinal)||!scene.EndsWith(".unity",StringComparison.Ordinal)||scene.Contains("..")||
                Mathf.Abs(facing.magnitude-1)>.001f||!float.IsFinite(health)||health<0||!combatInstalled&&health!=0)
                throw new ArgumentException("快照场景、单位朝向或生命装配无效。");
            if(powers==null||effects==null||actors==null||interactions==null)throw new ArgumentNullException("完整进度不能缺少玩家或世界片段。");
            if(powers.AbilityCount>256||powers.SkillCount>256||effects.Length>256||actors.Length>1024||interactions.Length>1024||
                !combatInstalled&&(effects.Length!=0||actors.Length!=0))throw new ArgumentException("快照片段装配或数量无效。");
            for(var i=0;i<powers.AbilityCount;i++)ValidateId(powers.GetAbility(i).Id);
            for(var i=0;i<powers.SkillCount;i++)ValidateId(powers.GetSkill(i).Id);
            foreach(var e in effects)
            {
                ValidateId(e.EffectId);ValidateId(e.SourceKey);
                _=new EntityTimedEffectSnapshot(e.EffectId,e.SourceKey,e.RemainingSeconds,e.Rules,e.Appearance,e.Kind);
                if(e.Rules.DamageCategoryCount>64)throw new ArgumentException("效果类别过多。");
                for(var i=0;i<e.Rules.DamageCategoryCount;i++)ValidateId(e.Rules.GetDamageCategory(i));
            }
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var actor in actors){actor.Validate();if(!ids.Add(actor.Id))throw new ArgumentException("世界角色身份重复。");}
            ids.Clear();foreach(var item in interactions){ValidateId(item.Id);if(!ids.Add(item.Id))throw new ArgumentException("交互身份重复。");}
            Scene=scene;LevelId=levelId;SpawnId=spawnId;SchemeId=schemeId;Position=position;Facing=facing;Checkpoint=checkpoint;
            CombatInstalled=combatInstalled;Health=health;Powers=powers;
            Weapons=weapons??new Weapons.WeaponLoadoutProgress("",Array.Empty<Weapons.WeaponProgress>());
            if(!combatInstalled&&Weapons.Count!=0)throw new ArgumentException("无战斗玩家不能装配武器进度。");
            _effects=(EntityTimedEffectSnapshot[])effects.Clone();_actors=(TopdownActorRecord[])actors.Clone();_interactions=(TopdownInteractionRecord[])interactions.Clone();
        }
        public EntityTimedEffectSnapshot GetEffect(int index)=>_effects[index];
        public TopdownActorRecord GetActor(int index)=>_actors[index];
        public TopdownInteractionRecord GetInteraction(int index)=>_interactions[index];
        public EntityEffectContainer RestoreEffects()=>EntityEffectContainer.RestoreTimed(_effects);
        internal static void ValidateId(string value)
        {if(string.IsNullOrWhiteSpace(value)||value!=value.Trim()||value.Length>256)throw new ArgumentException("进度身份必须为 1—256 字符的稳定 ID。");}
        internal static void ValidateVector(Vector2 value)
        {if(!float.IsFinite(value.x)||!float.IsFinite(value.y))throw new ArgumentException("进度平面向量必须有限。");}
        /// <summary>重试明确保留此检查点的世界，玩家回到检查点并恢复生命；不重置世界对象。</summary>
        public TopdownSnapshot ForRetry(float maximumHealth)=>new(Scene,LevelId,SpawnId,SchemeId,Checkpoint,Facing,Checkpoint,
            CombatInstalled,CombatInstalled?maximumHealth:0,Powers,Array.Empty<EntityTimedEffectSnapshot>(),_actors,_interactions,Weapons);
        #endregion
    }

    public readonly struct TopdownActorRecord
    {
        public string Id {get;}
        public float Health {get;}
        public Vector2 Position {get;}
        public TopdownActorRecord(string id,float health,Vector2 position){Id=id;Health=health;Position=position;Validate();}
        internal void Validate(){TopdownSnapshot.ValidateId(Id);TopdownSnapshot.ValidateVector(Position);if(!float.IsFinite(Health)||Health<0)throw new ArgumentException("世界角色生命无效。");}
    }
    public readonly struct TopdownInteractionRecord
    {
        public string Id {get;}
        public bool Activated {get;}
        public TopdownInteractionRecord(string id,bool activated){TopdownSnapshot.ValidateId(id);Id=id;Activated=activated;}
    }
}
