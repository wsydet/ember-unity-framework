using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ember.UIExtension.Editor
{
    internal sealed class EmberBuiPanel
    {
        private string _requestPath = "", _result = "";
        private EmberBuiPlan _plan;
        private Vector2 _scroll;
        internal void Draw()
        {
            EditorGUILayout.HelpBox("选择项目制作请求。分析与隔离预览不保存正式资产；应用前重新校验活动模板、输入、GUID 和计划。",MessageType.Info);
            _requestPath=EditorGUILayout.TextField("制作请求 JSON",_requestPath);
            if(GUILayout.Button("选择请求"))
            {
                var path=EditorUtility.OpenFilePanel("BUI 制作请求",Path.GetFullPath("."),"json");
                if(!string.IsNullOrEmpty(path)) { _requestPath=Path.GetRelativePath(Path.GetFullPath("."),path).Replace('\\','/');_plan=null; }
            }
            if(GUILayout.Button("只读分析")) { _plan=EmberBuiService.Analyze(_requestPath);_result=JsonUtility.ToJson(_plan,true); }
            using(new EditorGUI.DisabledScope(_plan==null||!_plan.valid))
            {
                if(GUILayout.Button("隔离构建预览并清理"))Run(()=>EmberBuiService.Preview(_plan));
                if(GUILayout.Button("应用当前计划"))Run(()=>JsonUtility.ToJson(EmberBuiService.Apply(_plan),true));
            }
            if(GUILayout.Button("续接已保存资产的代码生成"))Run(()=>JsonUtility.ToJson(EmberBuiService.ResumeGeneration(_requestPath),true));
            _scroll=EditorGUILayout.BeginScrollView(_scroll);EditorGUILayout.TextArea(_result,GUILayout.ExpandHeight(true));EditorGUILayout.EndScrollView();
        }
        private void Run(Func<string> action) { try { _result=action(); } catch(Exception e) { _result=e.ToString(); } }
    }
}
