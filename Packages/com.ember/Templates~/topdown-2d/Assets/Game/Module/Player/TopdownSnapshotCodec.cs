using System;
using System.IO;
using System.Text;
using Game.Entity;
using Game.PlayerData;
using UnityEngine;

namespace Game.Player
{
    /// <summary>独立业务类型与版本；严格读完所有字段，损坏不回退默认新游戏。</summary>
    public sealed class TopdownSnapshotCodec:IProgressCodec<TopdownSnapshot>
    {
        #region 内部参数
        public string DataTypeId=>"ember.topdown2d.progress";
        private readonly UTF8Encoding _utf8=new(false,true);
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Vector(BinaryWriter w,Vector2 v){w.Write(v.x);w.Write(v.y);}
        private Vector2 Vector(BinaryReader r)=>new(r.ReadSingle(),r.ReadSingle());
        private int Count(BinaryReader r,int max){var count=r.ReadInt32();if(count<0||count>max)throw new InvalidDataException("快照片段数量越界。");return count;}
        private bool Flag(BinaryReader r){var b=r.ReadByte();if(b>1)throw new InvalidDataException("布尔字段无效。");return b==1;}
        private string Id(BinaryReader r){var value=r.ReadString();TopdownSnapshot.ValidateId(value);return value;}
        private void Power(BinaryWriter w,EntityPowerSnapshot p,bool skills)
        {
            int count=skills?p.SkillCount:p.AbilityCount;w.Write(count);
            for(var i=0;i<count;i++){var v=skills?p.GetSkill(i):p.GetAbility(i);w.Write(v.Id);w.Write(v.IsOpen);w.Write(v.RemainingUses.HasValue);if(v.RemainingUses.HasValue)w.Write(v.RemainingUses.Value);w.Write(v.RemainingCooldown);}
        }
        private EntityPowerRecord[] Power(BinaryReader r)
        {
            var a=new EntityPowerRecord[Count(r,256)];
            for(var i=0;i<a.Length;i++){var id=Id(r);var open=Flag(r);int? uses=Flag(r)?r.ReadInt32():null;a[i]=new EntityPowerRecord(id,open,uses,r.ReadSingle());}return a;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EncodedProgress Encode(TopdownSnapshot snapshot)
        {
            if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));
            using var stream=new MemoryStream();using(var w=new BinaryWriter(stream,_utf8,true))
            {
                w.Write(TopdownSnapshot.DATA_VERSION);w.Write(snapshot.Scene);w.Write(snapshot.LevelId);w.Write(snapshot.SpawnId);w.Write(snapshot.SchemeId);
                Vector(w,snapshot.Position);Vector(w,snapshot.Facing);Vector(w,snapshot.Checkpoint);w.Write(snapshot.CombatInstalled);w.Write(snapshot.Health);
                Power(w,snapshot.Powers,false);Power(w,snapshot.Powers,true);w.Write(snapshot.EffectCount);
                for(var i=0;i<snapshot.EffectCount;i++)
                {
                    var e=snapshot.GetEffect(i);w.Write(e.EffectId);w.Write(e.SourceKey);w.Write(e.RemainingSeconds);w.Write((int)e.Kind);
                    w.Write(e.Rules.PreventsStagger);w.Write(e.Rules.ExcludedLayers);w.Write(e.Rules.DamageCategoryCount);
                    for(var c=0;c<e.Rules.DamageCategoryCount;c++)w.Write(e.Rules.GetDamageCategory(c));
                    w.Write(e.Appearance.Enabled);w.Write(e.Appearance.Priority);w.Write(e.Appearance.Alpha);w.Write(e.Appearance.BlinkHz);
                }
                w.Write(snapshot.ActorCount);for(var i=0;i<snapshot.ActorCount;i++){var a=snapshot.GetActor(i);w.Write(a.Id);w.Write(a.Health);Vector(w,a.Position);}
                w.Write(snapshot.InteractionCount);for(var i=0;i<snapshot.InteractionCount;i++){var a=snapshot.GetInteraction(i);w.Write(a.Id);w.Write(a.Activated);}
                w.Write(snapshot.Weapons.EquippedId);w.Write(snapshot.Weapons.Count);
                for(var i=0;i<snapshot.Weapons.Count;i++){var weapon=snapshot.Weapons.Get(i);w.Write(weapon.Id);Power(w,weapon.Powers,false);Power(w,weapon.Powers,true);}
            }
            return new EncodedProgress(stream.ToArray(),TopdownSnapshot.DATA_VERSION);
        }
        public DecodeResult<TopdownSnapshot> Decode(ReadOnlyMemory<byte> payload,int dataVersion)
        {
            if(dataVersion!=TopdownSnapshot.DATA_VERSION)return new DecodeResult<TopdownSnapshot>(DecodeStatus.DataVersionUnsupported);
            try
            {
                if(payload.Length>4*1024*1024)throw new InvalidDataException("快照过大。");
                using var stream=new MemoryStream(payload.ToArray(),false);using var r=new BinaryReader(stream,_utf8);
                if(r.ReadInt32()!=dataVersion)throw new InvalidDataException("负载内外版本不一致。");
                var scene=Id(r);var level=Id(r);var spawn=Id(r);var scheme=Id(r);var position=Vector(r);var facing=Vector(r);var checkpoint=Vector(r);var combat=Flag(r);var hp=r.ReadSingle();
                var powers=new EntityPowerSnapshot(Power(r),Power(r));var effects=new EntityTimedEffectSnapshot[Count(r,256)];
                for(var i=0;i<effects.Length;i++)
                {
                    var id=Id(r);var source=Id(r);var remaining=r.ReadSingle();var kind=(EntityEffectKind)r.ReadInt32();
                    var stagger=Flag(r);var layers=r.ReadInt32();var categories=new string[Count(r,64)];for(var c=0;c<categories.Length;c++)categories[c]=Id(r);
                    var appearance=new EntityEffectAppearance(Flag(r),r.ReadInt32(),r.ReadSingle(),r.ReadSingle());
                    effects[i]=new EntityTimedEffectSnapshot(id,source,remaining,new EntityEffectRules(categories,stagger,layers),appearance,kind);
                }
                var actors=new TopdownActorRecord[Count(r,1024)];for(var i=0;i<actors.Length;i++)actors[i]=new TopdownActorRecord(Id(r),r.ReadSingle(),Vector(r));
                var interactions=new TopdownInteractionRecord[Count(r,1024)];for(var i=0;i<interactions.Length;i++)interactions[i]=new TopdownInteractionRecord(Id(r),Flag(r));
                var equipped=r.ReadString();var weapons=new Weapons.WeaponProgress[Count(r,16)];
                for(var i=0;i<weapons.Length;i++)weapons[i]=new Weapons.WeaponProgress(Id(r),new EntityPowerSnapshot(Power(r),Power(r)));
                if(stream.Position!=stream.Length)throw new InvalidDataException("存在尾随字段。");
                return new DecodeResult<TopdownSnapshot>(DecodeStatus.Decoded,new TopdownSnapshot(scene,level,spawn,scheme,position,facing,checkpoint,combat,hp,powers,effects,actors,interactions,new Weapons.WeaponLoadoutProgress(equipped,weapons)));
            }
            catch(Exception e){return new DecodeResult<TopdownSnapshot>(DecodeStatus.DecodeFailed,error:e.Message);}
        }
        #endregion
    }
}
