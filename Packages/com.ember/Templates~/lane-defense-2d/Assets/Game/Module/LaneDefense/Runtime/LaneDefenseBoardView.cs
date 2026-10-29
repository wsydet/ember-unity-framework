using System;
using System.Collections.Generic;
using Game.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Ember.Core;

namespace Game.LaneDefense
{
    /// <summary>只消费快照。棋盘和实体使用预制体；不根据 Sprite 或 Trigger 推断权威命中。</summary>
    public sealed class LaneDefenseBoardView : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private Camera _camera;
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private GameObject _unitPrefab;
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private GameObject _projectilePrefab;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private BattleSession _battle;
        private BoardProjection _projection;
        private GameObject _root,_preview;
        private readonly Dictionary<long,GameObject> _visuals=new();
        private readonly HashSet<long> _visible=new();
        private readonly List<long> _remove=new();
        private readonly List<BattleEntity> _entities=new();
        private readonly List<ProjectileSnapshot> _projectiles=new();
        private GameInputButton _place,_cancel;
        private GameInputChannel _channel;
        private UniversalAdditionalCameraData _previousStackOwner;
        private Camera _uiCamera;
        private int _previousStackIndex=-1;
        private readonly List<RaycastResult> _uiHits=new();
        public BoardProjection Projection=>_projection;
        public Camera BoardCamera=>_camera;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnDestroy()=>Release();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private bool OverUI(Vector2 position)
        {
            if(EventSystem.current==null)return false;
            var data=new PointerEventData(EventSystem.current){position=position};
            _uiHits.Clear();EventSystem.current.RaycastAll(data,_uiHits);return _uiHits.Count!=0;
        }
        private void Entity(BattleEntity entity,GameObject prefab,Color color)
        {
            _visible.Add(entity.Key.Id);
            if(!_visuals.TryGetValue(entity.Key.Id,out var go))
            {go=Instantiate(prefab,_root.transform);_visuals.Add(entity.Key.Id,go);go.GetComponent<SpriteRenderer>().color=color;}
            go.transform.position=_projection.ToWorld(entity.Position);
            var group=go.GetComponent<SortingGroup>();if(group!=null)group.sortingOrder=100+(int)((_battle.Level.Rows-entity.Position.y)*100);
            var scale=go.transform.localScale;scale.y=Math.Max(.2f,(float)entity.Health/entity.MaxHealth)*.65f;go.transform.localScale=scale;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void ValidateResources()
        {
            if(_camera==null||!_camera.orthographic)throw new InvalidOperationException("An explicit orthographic board camera is required.");
            foreach(var prefab in new[]{_cellPrefab,_unitPrefab,_enemyPrefab,_projectilePrefab})
                if(prefab==null||prefab.GetComponent<SpriteRenderer>()==null||prefab.GetComponent<SpriteRenderer>().sprite==null)throw new InvalidOperationException("Missing lane presentation prefab/SpriteRenderer/Sprite.");
        }
        internal void Bind(BattleSession battle)
        {
            Release();ValidateResources();_battle=battle;
            var level=battle.Level;
            _projection=new BoardProjection(level.Rows,level.Columns,level.CellSize,new Vector2(-level.Columns*level.CellSize.x/2,-level.Rows*level.CellSize.y/2));
            _root=new GameObject("LaneDefenseSession-"+battle.Generation);_root.transform.SetParent(transform,false);
            var main=GameLauncher.Instance.MainCamera;
            _uiCamera=GameLauncher.Instance.UICamera;
            if(main!=null&&_uiCamera!=null)
            {
                _previousStackOwner=main.GetUniversalAdditionalCameraData();
                _previousStackIndex=_previousStackOwner.cameraStack.IndexOf(_uiCamera);
                if(_previousStackIndex>=0)_previousStackOwner.cameraStack.RemoveAt(_previousStackIndex);
                var stack=_camera.GetUniversalAdditionalCameraData();stack.renderType=CameraRenderType.Base;
                if(!stack.cameraStack.Contains(_uiCamera))stack.cameraStack.Add(_uiCamera);
                _camera.depth=main.depth+.1f;
            }
            _camera.enabled=true;
            var baseLine=Instantiate(_cellPrefab,_root.transform);baseLine.name="BaseBoundary";
            baseLine.transform.position=_projection.ToWorld(new Vector2(-.15f,level.Rows/2f));
            baseLine.transform.localScale=new Vector3(.1f,level.Rows*level.CellSize.y,1);
            baseLine.GetComponent<SpriteRenderer>().color=new Color(.15f,.75f,.65f);
            for(int row=0;row<level.Rows;row++)
            {
                var spawn=Instantiate(_cellPrefab,_root.transform);spawn.name="SpawnLane-"+row;
                spawn.transform.position=_projection.ToWorld(new Vector2(level.Columns+.22f,level.Rows-row-.5f));
                spawn.transform.localScale=new Vector3(.12f,level.CellSize.y*.5f,1);
                spawn.GetComponent<SpriteRenderer>().color=new Color(.85f,.4f,.34f);
            }
            for(int row=0;row<level.Rows;row++)for(int col=0;col<level.Columns;col++)
            {
                var cell=new Cell(row,col);var go=Instantiate(_cellPrefab,_root.transform);
                go.name="Cell-"+row+"-"+col;go.transform.position=_projection.ToWorld(battle.Board.Center(cell));
                go.transform.localScale=new Vector3(level.CellSize.x*.95f,level.CellSize.y*.94f,1);
                go.GetComponent<SpriteRenderer>().color=!battle.Board.IsBuildable(cell)?new Color(.12f,.15f,.18f):((row+col)%2==0?new Color(.16f,.24f,.29f):new Color(.19f,.28f,.33f));
            }
            _preview=Instantiate(_cellPrefab,_root.transform);_preview.name="PlacementPreview";
            _preview.GetComponent<SpriteRenderer>().sortingOrder=2000;_preview.SetActive(false);
            _preview.transform.localScale=new Vector3(level.CellSize.x*.88f,level.CellSize.y*.88f,1);
            _channel=GameInputModule.Instance.Service.GetChannel("gameplay");
            _place=new GameInputButton(_channel,"Place");_cancel=new GameInputButton(_channel,"Cancel");
            _camera.transform.position=new Vector3(0,-.25f,-10);_camera.transform.rotation=Quaternion.identity;
            Render();
        }
        internal void ReadInput(bool allowed)
        {
            if(_battle==null)return;
            bool place=_place.ConsumePress(),cancel=_cancel.ConsumePress();
            var module=LaneDefenseModule.Instance;
            _preview.SetActive(false);
            if(!allowed||_battle.Paused){_place.ClearPending();_cancel.ClearPending();return;}
            if(cancel){module.CancelSelection();return;}
            var pointer=_channel.ReadContinuous<Vector2>("Point");
            if(OverUI(pointer)||!_projection.TryScreenToCell(_camera,pointer,out var cell))return;
            if(module.DemolishMode)
            {
                if(place){bool ok=_battle.RemoveUnit(new EntityKey(_battle.Generation,_battle.Board.Occupant(cell)));module.SetFeedback(ok?"Unit dismantled.":"No unit at this cell.");}
                return;
            }
            if(module.SelectedCard==null)return;
            var rejection=_battle.Preview(module.SelectedCard,cell);
            _preview.SetActive(true);_preview.transform.position=_projection.ToWorld(_battle.Board.Center(cell));
            _preview.GetComponent<SpriteRenderer>().color=rejection==PlacementRejection.None?new Color(.3f,1,.7f,.45f):new Color(1,.3f,.3f,.45f);
            if(place){var result=_battle.Place(new PlacementCommand(_battle.Generation,_battle.NextCommandId(),module.SelectedCard,cell));module.SetFeedback(result.Success?"Placed.":result.Rejection.ToString());}
        }
        internal void Render()
        {
            if(_battle==null)return;
            _camera.orthographicSize=Math.Max(_battle.Level.Rows*_battle.Level.CellSize.y/2+1.7f,(_battle.Level.Columns*_battle.Level.CellSize.x/2+1.2f)/Math.Max(.25f,_camera.aspect));
            _visible.Clear();
            _battle.CopyUnits(_entities);foreach(var e in _entities)Entity(e,_unitPrefab,e.DefinitionId=="barrier"?new Color(.75f,.65f,.4f):new Color(.25f,.85f,.8f));
            _battle.CopyEnemies(_entities);foreach(var e in _entities)Entity(e,_enemyPrefab,new Color(.96f,.44f,.38f));
            _battle.CopyProjectiles(_projectiles);foreach(var p in _projectiles)
            {
                _visible.Add(p.Key.Id);
                if(!_visuals.TryGetValue(p.Key.Id,out var go)){go=Instantiate(_projectilePrefab,_root.transform);_visuals.Add(p.Key.Id,go);}
                go.transform.position=_projection.ToWorld(p.Position);
            }
            _remove.Clear();foreach(var entry in _visuals)if(!_visible.Contains(entry.Key))_remove.Add(entry.Key);
            foreach(long id in _remove){Destroy(_visuals[id]);_visuals.Remove(id);}
        }
        internal void Release()
        {
            if(_camera!=null)
            {
                _camera.enabled=false;
                var stack=_camera.GetUniversalAdditionalCameraData();
                if(_uiCamera!=null&&stack.cameraStack!=null)stack.cameraStack.Remove(_uiCamera);
            }
            if(_previousStackOwner!=null&&_uiCamera!=null&&_previousStackIndex>=0&&_previousStackOwner.cameraStack!=null&&!_previousStackOwner.cameraStack.Contains(_uiCamera))
                _previousStackOwner.cameraStack.Insert(Math.Min(_previousStackIndex,_previousStackOwner.cameraStack.Count),_uiCamera);
            _previousStackOwner=null;_uiCamera=null;_previousStackIndex=-1;
            _place?.Dispose();_cancel?.Dispose();_place=null;_cancel=null;_channel=null;
            if(_root!=null){_root.SetActive(false);Destroy(_root);}
            _root=null;_preview=null;_visuals.Clear();_battle=null;_projection=null;
        }
        #endregion
    }
}
