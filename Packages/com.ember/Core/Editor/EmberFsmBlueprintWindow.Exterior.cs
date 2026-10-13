using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ember.Core.Editor
{
    public sealed partial class EmberFsmBlueprintWindow
    {
        #region 内部参数
        private const float EXTENT=170, CARD_WIDTH=130, ROW=62;
        private readonly List<GraphElement> _peripherals=new();
        private sealed class ReferenceCard : GraphElement { }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private Relation[] Sources(string id)=>_relations.Where(r=>!r.Structural&&!r.Redirect&&r.Target==id).ToArray();
        private Relation[] Exits(string id)=>_relations.Where(r=>!r.Structural&&r.Source==id&&(!r.Redirect||RedirectVisible(id))).ToArray();
        private float ExteriorHeader(string id)=>Math.Max(HeaderHeight(id),52+ROW*Math.Max(Sources(id).Length,Exits(id).Length));
        private Rect Envelope(string id)
        {
            var position=_positions[id]; var size=_sizes.TryGetValue(id,out var value)?value:new Vector2(200,ExteriorHeader(id));
            float left=Sources(id).Length>0?EXTENT:20;
            float right=Exits(id).Length>0?EXTENT:20;
            return new Rect(position.x-left,position.y,size.x+left+right,Math.Max(size.y,ExteriorHeader(id)));
        }
        private void AddPeripheral(GraphElement element,Rect rect)
        {
            element.style.marginLeft=element.style.marginRight=element.style.marginTop=element.style.marginBottom=0;
            element.style.position=Position.Absolute; element.style.left=rect.x; element.style.top=rect.y;
            element.style.width=rect.width; element.style.height=rect.height; element.layer=1;
            _canvas.AddElement(element); _peripherals.Add(element);
        }
        private void ExteriorCaption(string text,Vector2 point)
        {
            var item=new ReferenceCard { capabilities=(Capabilities)0,pickingMode=PickingMode.Ignore };
            var label=new Label(text); label.style.fontSize=10; label.style.color=new Color(.42f,.48f,.56f); item.Add(label);
            AddPeripheral(item,new Rect(point,new Vector2(CARD_WIDTH,20)));
        }
        private void AddReference(Relation relation,bool incoming,Rect rect)
        {
            string id=incoming?relation.Source:relation.Target;
            var card=new ReferenceCard { capabilities=Capabilities.Selectable,userData=relation.Key,
                name=incoming?"source-reference":"target-reference", tooltip=StatePath(id)+"\n"+relation.Description+"\n"+relation.Key };
            card.AddToClassList("fsm-reference");
            card.style.backgroundColor=new Color(.16f,.18f,.21f);
            card.style.borderBottomWidth=card.style.borderTopWidth=card.style.borderLeftWidth=card.style.borderRightWidth=1;
            var color=relation.Redirect?new Color(1,.65f,.2f):new Color(.29f,.38f,.48f);
            card.style.borderBottomColor=card.style.borderTopColor=card.style.borderLeftColor=card.style.borderRightColor=color;
            var reference=new Button(()=>{ NavigateToState(id); SelectDeclaration(relation.Source,relation.Redirect,relation.Id); })
                { text=Display(id)+"  ↗",name="locate-reference" };
            reference.style.fontSize=13; card.Add(reference);
            var description=string.IsNullOrWhiteSpace(relation.Description)?relation.Id:relation.Description;
            var label=new Label((relation.Redirect?"◇ 预检 · ":"")+description) { tooltip=description };
            label.style.whiteSpace=WhiteSpace.Normal; label.style.fontSize=11; label.style.maxHeight=30;
            label.style.overflow=Overflow.Hidden; card.Add(label);
            card.RegisterCallback<MouseDownEvent>(e=>{SelectRelation(relation);e.StopPropagation();});
            AddPeripheral(card,rect); _referenceControls.Add(card);
        }
        private void LocalPath(Relation r,params Vector2[] points)
        {
            var start=points[0]; var end=points[points.Length-1];
            float tangent=Math.Max(18,(end.x-start.x)*.55f);
            var a=start+Vector2.right*tangent; var b=end-Vector2.right*tangent;
            var curve=new List<Vector2>();
            for(int i=0;i<=24;i++) { float t=i/24f,u=1-t; curve.Add(u*u*u*start+3*u*u*t*a+3*u*t*t*b+t*t*t*end); }
            var path=new EmberFsmBlueprintPath(curve,r.Redirect,r.Redirect?new Color(1,.65f,.2f):new Color(.3f,.7f,1),()=>SelectRelation(r));
            path.userData=r.Key; path.tooltip=r.Description;
            _canvas.AddElement(path); _peripherals.Add(path);
        }
        private void ExitPort(Relation r,Vector2 position)
        {
            var port=new ReferenceCard { capabilities=Capabilities.Selectable,userData=r.Key,tooltip=r.Id+" · "+r.Description,name="local-exit-port" };
            port.style.backgroundColor=r.Redirect?new Color(1,.65f,.2f):new Color(.3f,.7f,1);
            port.style.borderTopLeftRadius=port.style.borderTopRightRadius=port.style.borderBottomLeftRadius=port.style.borderBottomRightRadius=5;
            port.RegisterCallback<MouseDownEvent>(e=>{SelectRelation(r);e.StopPropagation();});
            AddPeripheral(port,new Rect(position-new Vector2(5,5),new Vector2(10,10)));
        }
        private void RebuildExterior()
        {
            foreach(var item in _peripherals) _canvas.RemoveElement(item);
            _peripherals.Clear(); _referenceControls.Clear();
            foreach(var pair in _nodes)
            {
                var rect=NodeRect(pair.Value); var incoming=Sources(pair.Key); var outgoing=Exits(pair.Key);
                if(incoming.Length>0) ExteriorCaption("已声明来源",new Vector2(rect.xMin-EXTENT,rect.yMin+15));
                if(outgoing.Length>0) ExteriorCaption(Children(pair.Key).Any()?"父状态公共出口":"状态出口",new Vector2(rect.xMax+40,rect.yMin+15));
                var anchor=pair.Value.Q<VisualElement>("local-entry-anchor");
                // 锚点受 Node 内容容器的边距影响，不能用固定 y+50 猜测屏幕位置。
                // 仅取实际局部偏移，平移继续使用 inline 坐标以支持同帧拖动/保存。
                var entryOffset=anchor!=null && anchor.layout.width>0 && anchor.layout.height>0
                    ? pair.Value.WorldToLocal(anchor.worldBound.center) : new Vector2(0,50);
                var entry=rect.position+entryOffset;
                for(int i=0;i<incoming.Length;i++)
                {
                    var card=new Rect(rect.xMin-EXTENT,rect.yMin+40+i*ROW,CARD_WIDTH,56);
                    AddReference(incoming[i],true,card);
                    // 所有来源短线汇聚同一个真实状态入口；每条仍可独立选择。
                    float channel=rect.xMin-14-i*3;
                    LocalPath(incoming[i],new Vector2(card.xMax,card.center.y),new Vector2(channel,card.center.y),new Vector2(channel,entry.y),entry);
                }
                for(int i=0;i<outgoing.Length;i++)
                {
                    var point=new Vector2(rect.xMax,rect.yMin+50+i*ROW);
                    var card=new Rect(rect.xMax+40,point.y-10,CARD_WIDTH,56);
                    AddReference(outgoing[i],false,card); ExitPort(outgoing[i],point);
                    LocalPath(outgoing[i],point,new Vector2(card.xMin,point.y));
                }
            }
        }
        #endregion
    }
}
