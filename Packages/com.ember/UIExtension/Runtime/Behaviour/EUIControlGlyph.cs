using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>控制中心的矢量线形图标，不依赖字体动态补字或新增纹理。</summary>
    [RequireComponent(typeof(CanvasRenderer), typeof(RectTransform))]
    public sealed class EUIControlGlyph : MaskableGraphic
    {
        #region 编辑器面板参数
        [SerializeField] private string _key = "reset";
        [SerializeField, Min(1)] private float _stroke = 2.5f;
        #endregion
        #region 外部方法
        public void SetKey(string key) { if (_key == key) return; _key = key; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var rect=rectTransform.rect;float s=Mathf.Min(rect.width,rect.height)*.38f;var c=rect.center;
            if(_key=="↻"||_key=="reset")
            { Arc(vh,c,s,35,320);Line(vh,c+new Vector2(s*.82f,s*.57f),c+new Vector2(s*.2f,s*.57f));Line(vh,c+new Vector2(s*.82f,s*.57f),c+new Vector2(s*.82f,0)); }
            else if(_key=="▦"||_key=="grid")
            { for(int y=-1;y<=1;y+=2)for(int x=-1;x<=1;x+=2){var p=c+new Vector2(x,y)*s*.5f;Box(vh,p,Vector2.one*s*.65f);} }
            else if(_key=="×"||_key=="exit")
            { Arc(vh,c,s,135,405);Line(vh,c+Vector2.up*s*1.15f,c+Vector2.up*s*.15f); }
            else if(_key=="≡"||_key=="log")
            { for(int y=-1;y<=1;y++)Line(vh,c+new Vector2(-s,y*s*.65f),c+new Vector2(s,y*s*.65f)); }
            else
            { Line(vh,c+new Vector2(0,s),c+new Vector2(s,0));Line(vh,c+new Vector2(s,0),c+new Vector2(0,-s));Line(vh,c+new Vector2(0,-s),c+new Vector2(-s,0));Line(vh,c+new Vector2(-s,0),c+new Vector2(0,s)); }
        }
        #endregion
        #region 内部方法
        private void Box(VertexHelper vh,Vector2 center,Vector2 size)
        { var a=center-size*.5f;var b=center+size*.5f;Line(vh,a,new Vector2(b.x,a.y));Line(vh,new Vector2(b.x,a.y),b);Line(vh,b,new Vector2(a.x,b.y));Line(vh,new Vector2(a.x,b.y),a); }
        private void Arc(VertexHelper vh,Vector2 center,float radius,float start,float end)
        { var prev=center+new Vector2(Mathf.Cos(start*Mathf.Deg2Rad),Mathf.Sin(start*Mathf.Deg2Rad))*radius;for(int i=1;i<=96;i++){float a=Mathf.Lerp(start,end,i/96f)*Mathf.Deg2Rad;var next=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;Line(vh,prev,next);prev=next;} }
        private void Line(VertexHelper vh,Vector2 from,Vector2 to)
        {
            var delta=to-from;if(delta.sqrMagnitude<.001f)return;
            var direction=new Vector2(-delta.y,delta.x).normalized;
            var normal=direction*_stroke*.5f;int index=vh.currentVertCount;
            vh.AddVert(from-normal,color,Vector2.zero);vh.AddVert(from+normal,color,Vector2.zero);vh.AddVert(to+normal,color,Vector2.zero);vh.AddVert(to-normal,color,Vector2.zero);
            vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index+2,index+3,index);
            var outside=normal+direction*.65f;var clear=color;clear.a=0;
            index=vh.currentVertCount;
            vh.AddVert(from-outside,clear,Vector2.zero);vh.AddVert(from-normal,color,Vector2.zero);vh.AddVert(to-normal,color,Vector2.zero);vh.AddVert(to-outside,clear,Vector2.zero);
            vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index+2,index+3,index);
            index=vh.currentVertCount;
            vh.AddVert(from+normal,color,Vector2.zero);vh.AddVert(from+outside,clear,Vector2.zero);vh.AddVert(to+outside,clear,Vector2.zero);vh.AddVert(to+normal,color,Vector2.zero);
            vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index+2,index+3,index);
        }
        #endregion
    }
}
