using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.LaneDefense
{
    [Serializable]
    public sealed class UnitDefinition
    {
        #region 编辑器面板参数
        [SerializeField] private string _id = "sentry";
        [SerializeField] private string _label = "Sentry";
        [SerializeField] private int _cost = 50;
        [SerializeField] private float _cooldown = 1;
        [SerializeField] private int _health = 100;
        [SerializeField] private int _limit = 20;
        [SerializeField] private bool _shooter = true;
        [SerializeField] private float _range = 12;
        [SerializeField] private float _attackInterval = 1;
        [SerializeField] private int _damage = 25;
        [SerializeField] private float _projectileSpeed = 9;
        [SerializeField] private float _projectileLifetime = 4;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string Id => _id;
        public string Label => _label;
        public int Cost => _cost;
        public float Cooldown => _cooldown;
        public int Health => _health;
        public int Limit => _limit;
        public bool Shooter => _shooter;
        public float Range => _range;
        public float AttackInterval => _attackInterval;
        public int Damage => _damage;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileLifetime => _projectileLifetime;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public UnitDefinition(string id, string label, int cost, int health, bool shooter = true,
            float cooldown = 1, float interval = 1, int damage = 25, float speed = 9,
            float range = 12, int limit = 20, float lifetime = 4)
        { _id=id; _label=label; _cost=cost; _health=health; _shooter=shooter; _cooldown=cooldown;
          _attackInterval=interval; _damage=damage; _projectileSpeed=speed; _range=range;
          _limit=limit; _projectileLifetime=lifetime; }
        internal UnitDefinition Freeze() => (UnitDefinition)MemberwiseClone();
        #endregion
    }

    [Serializable]
    public sealed class EnemyDefinition
    {
        #region 编辑器面板参数
        [SerializeField] private string _id = "walker";
        [SerializeField] private int _health = 100;
        [SerializeField] private float _speed = .45f;
        [SerializeField] private int _damage = 15;
        [SerializeField] private float _attackInterval = 1;
        [SerializeField] private int _baseDamage = 1;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string Id => _id;
        public int Health => _health;
        public float Speed => _speed;
        public int Damage => _damage;
        public float AttackInterval => _attackInterval;
        public int BaseDamage => _baseDamage;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EnemyDefinition(string id, int health=100, float speed=.45f, int damage=15,
            float interval=1, int baseDamage=1)
        { _id=id; _health=health; _speed=speed; _damage=damage; _attackInterval=interval; _baseDamage=baseDamage; }
        internal EnemyDefinition Freeze() => (EnemyDefinition)MemberwiseClone();
        #endregion
    }

    [Serializable]
    public sealed class SpawnEntry
    {
        #region 编辑器面板参数
        [SerializeField] private string _enemyId = "walker";
        [SerializeField] private int _lane;
        [SerializeField] private float _time;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string EnemyId => _enemyId;
        public int Lane => _lane;
        public float Time => _time;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public SpawnEntry(string enemyId, int lane, float time) { _enemyId=enemyId; _lane=lane; _time=time; }
        internal SpawnEntry Freeze() => (SpawnEntry)MemberwiseClone();
        #endregion
    }

    [Serializable]
    public sealed class WaveDefinition
    {
        #region 编辑器面板参数
        [SerializeField] private string _id = "wave-1";
        [SerializeField] private SpawnEntry[] _spawns = Array.Empty<SpawnEntry>();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string Id => _id;
        public IReadOnlyList<SpawnEntry> Spawns => _spawns==null?null:Array.AsReadOnly(_spawns);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public WaveDefinition(string id, params SpawnEntry[] spawns) { _id=id; _spawns=spawns; }
        internal WaveDefinition Freeze()
        {
            var entries = new SpawnEntry[_spawns.Length];
            for(int i=0;i<entries.Length;i++) entries[i]=_spawns[i].Freeze();
            return new WaveDefinition(_id,entries);
        }
        #endregion
    }

    /// <summary>一关的制作源。会话只读取 Freeze 后的独立快照，不修改共享资产。</summary>
    public sealed class LaneDefenseLevel : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private string _id = "training-yard";
        [SerializeField] private int _rows = 5;
        [SerializeField] private int _columns = 9;
        [SerializeField] private Vector2 _cellSize = new Vector2(1.2f,1);
        [SerializeField] private int _initialResource = 500;
        [SerializeField] private int _resourceCap = 9999;
        [SerializeField] private int _baseHealth = 5;
        [SerializeField] private bool _buildDuringWave = true;
        [SerializeField,Range(0,1)] private float _demolishRefund = .5f;
        [SerializeField] private int[] _unbuildable = Array.Empty<int>();
        [SerializeField] private UnitDefinition[] _units = Array.Empty<UnitDefinition>();
        [SerializeField] private EnemyDefinition[] _enemies = Array.Empty<EnemyDefinition>();
        [SerializeField] private WaveDefinition[] _waves = Array.Empty<WaveDefinition>();
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string Id => _id;
        public int Rows => _rows;
        public int Columns => _columns;
        public Vector2 CellSize => _cellSize;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void Configure(string id,int rows,int columns,Vector2 cellSize,int resource,int baseHealth,
            UnitDefinition[] units,EnemyDefinition[] enemies,WaveDefinition[] waves,
            int[] unbuildable=null,bool buildDuringWave=true,int resourceCap=9999,float refund=.5f)
        {
            _id=id; _rows=rows; _columns=columns; _cellSize=cellSize; _initialResource=resource;
            _baseHealth=baseHealth; _units=units; _enemies=enemies; _waves=waves;
            _unbuildable=unbuildable??Array.Empty<int>(); _buildDuringWave=buildDuringWave;
            _resourceCap=resourceCap; _demolishRefund=refund;
        }

        public IReadOnlyList<string> Validate()
        {
            var errors=new List<string>();
            if(string.IsNullOrWhiteSpace(_id)) errors.Add("Missing level ID.");
            if(_rows<=0||_columns<=0||_rows>64||_columns>64) errors.Add("Board must be 1..64 rows/columns.");
            if(!float.IsFinite(_cellSize.x)||!float.IsFinite(_cellSize.y)||_cellSize.x<=0||_cellSize.y<=0) errors.Add("Invalid cell size.");
            if(_baseHealth<=0||_initialResource<0||_resourceCap<_initialResource||!float.IsFinite(_demolishRefund)||_demolishRefund<0||_demolishRefund>1) errors.Add("Invalid economy/base values.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            if(_units==null||_units.Length==0) errors.Add("No cards.");
            else foreach(var u in _units)
            {
                if(u==null) {errors.Add("Null unit.");continue;}
                if(string.IsNullOrWhiteSpace(u.Id)||!ids.Add(u.Id)) errors.Add("Duplicate/missing unit ID: "+u.Id);
                if(u.Cost<0||u.Health<=0||u.Limit<=0||!float.IsFinite(u.Cooldown)||u.Cooldown<0||!float.IsFinite(u.AttackInterval)||u.AttackInterval<=0||!float.IsFinite(u.Range)||u.Range<=0||u.Damage<0||!float.IsFinite(u.ProjectileSpeed)||u.ProjectileSpeed<=0||!float.IsFinite(u.ProjectileLifetime)||u.ProjectileLifetime<=0) errors.Add("Invalid unit: "+u.Id);
            }
            ids.Clear();
            if(_enemies==null) errors.Add("Missing enemies.");
            else foreach(var e in _enemies)
            {
                if(e==null) {errors.Add("Null enemy.");continue;}
                if(string.IsNullOrWhiteSpace(e.Id)||!ids.Add(e.Id)) errors.Add("Duplicate/missing enemy ID: "+e.Id);
                if(e.Health<=0||e.Damage<0||e.BaseDamage<=0||!float.IsFinite(e.Speed)||e.Speed<=0||!float.IsFinite(e.AttackInterval)||e.AttackInterval<=0) errors.Add("Invalid enemy: "+e.Id);
            }
            var waveIds=new HashSet<string>(StringComparer.Ordinal);
            if(_waves==null) errors.Add("Missing waves.");
            else foreach(var w in _waves)
            {
                if(w==null) {errors.Add("Null wave.");continue;}
                if(string.IsNullOrWhiteSpace(w.Id)||!waveIds.Add(w.Id)) errors.Add("Duplicate/missing wave ID.");
                if(w.Spawns==null) {errors.Add("Missing spawn list.");continue;}
                float previous=-1;
                foreach(var s in w.Spawns)
                {
                    if(s==null) {errors.Add("Null spawn.");continue;}
                    if(!ids.Contains(s.EnemyId)||s.Lane<0||s.Lane>=_rows||!float.IsFinite(s.Time)||s.Time<0||s.Time<previous) errors.Add("Invalid/unsorted spawn in "+w.Id);
                    previous=s.Time;
                }
            }
            var blocked=new HashSet<int>();
            if(_unbuildable==null) errors.Add("Missing terrain.");
            else foreach(int cell in _unbuildable) if(cell<0||cell>=_rows*_columns||!blocked.Add(cell)) errors.Add("Invalid/duplicate terrain cell.");
            return errors;
        }

        public LaneLevelSnapshot Freeze()
        {
            var errors=Validate();
            if(errors.Count!=0) throw new InvalidOperationException(string.Join("\n",errors));
            var units=new UnitDefinition[_units.Length]; for(int i=0;i<units.Length;i++) units[i]=_units[i].Freeze();
            var enemies=new EnemyDefinition[_enemies.Length]; for(int i=0;i<enemies.Length;i++) enemies[i]=_enemies[i].Freeze();
            var waves=new WaveDefinition[_waves.Length]; for(int i=0;i<waves.Length;i++) waves[i]=_waves[i].Freeze();
            return new LaneLevelSnapshot(_id,_rows,_columns,_cellSize,_initialResource,_resourceCap,_baseHealth,
                _buildDuringWave,_demolishRefund,(int[])_unbuildable.Clone(),units,enemies,waves);
        }
        #endregion
    }

    public sealed class LaneLevelSnapshot
    {
        #region 内部参数
        public string Id {get;}
        public int Rows {get;}
        public int Columns {get;}
        public Vector2 CellSize {get;}
        public int InitialResource {get;}
        public int ResourceCap {get;}
        public int BaseHealth {get;}
        public bool BuildDuringWave {get;}
        public float DemolishRefund {get;}
        public IReadOnlyList<int> Unbuildable {get;}
        public IReadOnlyList<UnitDefinition> Units {get;}
        public IReadOnlyList<EnemyDefinition> Enemies {get;}
        public IReadOnlyList<WaveDefinition> Waves {get;}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal LaneLevelSnapshot(string id,int rows,int columns,Vector2 size,int resource,int cap,int health,
            bool build,float refund,int[] blocked,UnitDefinition[] units,EnemyDefinition[] enemies,WaveDefinition[] waves)
        { Id=id; Rows=rows; Columns=columns; CellSize=size; InitialResource=resource; ResourceCap=cap;
          BaseHealth=health; BuildDuringWave=build; DemolishRefund=refund; Unbuildable=Array.AsReadOnly(blocked);
          Units=Array.AsReadOnly(units); Enemies=Array.AsReadOnly(enemies); Waves=Array.AsReadOnly(waves); }
        #endregion
    }
}
