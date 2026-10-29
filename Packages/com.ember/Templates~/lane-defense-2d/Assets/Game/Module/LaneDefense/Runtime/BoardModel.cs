using System;
using UnityEngine;

namespace Game.LaneDefense
{
    public readonly struct Cell : IEquatable<Cell>
    {
        public int Row {get;}
        public int Column {get;}
        public Cell(int row,int column) {Row=row;Column=column;}
        public bool Equals(Cell other)=>Row==other.Row&&Column==other.Column;
        public override bool Equals(object other)=>other is Cell c&&Equals(c);
        public override int GetHashCode()=>HashCode.Combine(Row,Column);
        public override string ToString()=>$"({Row},{Column})";
    }

    /// <summary>只存储逻辑占用；行从上向下，逻辑 x 向右、y 向上。</summary>
    public sealed class BoardModel
    {
        #region 内部参数
        private readonly long[,] _occupancy;
        private readonly bool[,] _blocked;
        public int Rows {get;}
        public int Columns {get;}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public BoardModel(LaneLevelSnapshot level)
        {
            Rows=level.Rows; Columns=level.Columns; _occupancy=new long[Rows,Columns]; _blocked=new bool[Rows,Columns];
            foreach(int i in level.Unbuildable) _blocked[i/Columns,i%Columns]=true;
        }
        public bool Contains(Cell c)=>c.Row>=0&&c.Row<Rows&&c.Column>=0&&c.Column<Columns;
        public bool IsBuildable(Cell c)=>Contains(c)&&!_blocked[c.Row,c.Column];
        public long Occupant(Cell c)=>Contains(c)?_occupancy[c.Row,c.Column]:0;
        public Vector2 Center(Cell c)=>new Vector2(c.Column+.5f,Rows-c.Row-.5f);
        internal void Occupy(Cell c,long id)=>_occupancy[c.Row,c.Column]=id;
        internal void Clear(Cell c,long id) {if(Occupant(c)==id)_occupancy[c.Row,c.Column]=0;}
        #endregion
    }

    public sealed class BoardProjection
    {
        #region 内部参数
        public Vector2 Origin {get;}
        public Vector2 CellSize {get;}
        public int Rows {get;}
        public int Columns {get;}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public BoardProjection(int rows,int columns,Vector2 cellSize,Vector2 origin)
        {Rows=rows;Columns=columns;CellSize=cellSize;Origin=origin;}
        public Vector3 ToWorld(Vector2 logic)=>new Vector3(Origin.x+logic.x*CellSize.x,Origin.y+logic.y*CellSize.y,0);
        public Vector2 ToLogic(Vector3 world)=>new Vector2((world.x-Origin.x)/CellSize.x,(world.y-Origin.y)/CellSize.y);
        public bool TryWorldToCell(Vector3 world,out Cell cell)
        {
            var p=ToLogic(world); cell=default;
            if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||p.x<0||p.y<0||p.x>=Columns||p.y>=Rows)return false;
            cell=new Cell(Rows-1-Mathf.FloorToInt(p.y),Mathf.FloorToInt(p.x));return true;
        }
        public bool TryScreenToCell(Camera camera,Vector2 screen,out Cell cell)
        {
            cell=default; if(camera==null)return false;
            var ray=camera.ScreenPointToRay(screen); var plane=new Plane(Vector3.forward,Vector3.zero);
            return plane.Raycast(ray,out float distance)&&TryWorldToCell(ray.GetPoint(distance),out cell);
        }
        #endregion
    }
}
