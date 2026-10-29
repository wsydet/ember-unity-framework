using System;
using System.Linq;
using Ember.Core.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.LaneDefense.Editor
{
    [InitializeOnLoad]
    internal static class LaneDefenseMenus
    {
        private static readonly IDisposable _menu;
        static LaneDefenseMenus()
        {
            _menu=EmberConditionalMenu.Register("Ember/LaneDefense/塔防开发工具",
                ()=>LaneDefenseExampleBuilder.CanAuthor,
                ()=>LaneDefenseWorkbench.Open(),()=>!EditorApplication.isPlayingOrWillChangePlaymode,200);
        }
    }

    public sealed class LaneDefenseWorkbench : EditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private LaneDefenseLevel _level;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private Vector2 _scroll;
        private string _identity;
        private bool _invalidated;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnEnable(){_identity=Identity();EditorApplication.projectChanged+=Repaint;}
        private void OnDisable()=>EditorApplication.projectChanged-=Repaint;
        private void OnGUI()
        {
            if(!LaneDefenseExampleBuilder.CanAuthor||Identity()!=_identity)_invalidated=true;
            if(_invalidated){EditorGUILayout.HelpBox("模板身份已变化。本窗口停止写入和试跑；所选草稿保留。请在所属模板重新打开。",MessageType.Warning);return;}
            _scroll=EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Lane Defense · Level workbench",EditorStyles.boldLabel);
            _level=(LaneDefenseLevel)EditorGUILayout.ObjectField("关卡配置",_level,typeof(LaneDefenseLevel),false);
            if(_level!=null)
            {
                var errors=_level.Validate();foreach(string error in errors)EditorGUILayout.HelpBox(error,MessageType.Error);
                if(errors.Count==0)
                {
                    var level=_level.Freeze();EditorGUILayout.LabelField($"{level.Rows} × {level.Columns}  |  {level.Waves.Count} waves  |  base {level.BaseHealth}");
                    EditorGUILayout.LabelField("BASE  ← enemies move left ←  SPAWN");
                    for(int row=0;row<level.Rows;row++)
                    {
                        EditorGUILayout.BeginHorizontal();EditorGUILayout.LabelField("Lane "+row,GUILayout.Width(65));
                        for(int col=0;col<level.Columns;col++)GUILayout.Box(level.Unbuildable.Contains(row*level.Columns+col)?"×":"·",GUILayout.Width(24),GUILayout.Height(22));
                        EditorGUILayout.EndHorizontal();
                    }
                    foreach(var wave in level.Waves){EditorGUILayout.LabelField(wave.Id+" — "+wave.Spawns.Count+" spawns");foreach(var s in wave.Spawns)EditorGUILayout.LabelField($"  {s.Time:0.00}s  lane {s.Lane}  {s.EnemyId}");}
                }
                if(GUILayout.Button("在 Inspector 编辑所选配置")){LaneDefenseExampleBuilder.RequireAccess();Selection.activeObject=_level;}
                using(new EditorGUI.DisabledScope(errors.Count>0||EditorApplication.isPlayingOrWillChangePlaymode))
                    if(GUILayout.Button("Framework 正式启动试跑所选关卡"))Launch();
            }
            EditorGUILayout.HelpBox("正式 UI 使用 EUI 开发中心。已制作资源不会自动重建；通用模板保存使用项目中心。",MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private string Identity()
        {
            var editing=EmberProjectSetup.IsEmbeddedPackage()?EmberProjectSetup.GetEditingTemplate():null;
            if(editing!=null)return editing.templateId+"/"+editing.templateVersion+"/"+editing.loadedAt;
            var deployed=EmberProjectSetup.GetActiveDeployedTemplate();return deployed==null?"":deployed.templateId+"/"+deployed.version;
        }
        private void Launch()
        {
            LaneDefenseExampleBuilder.RequireAccess();if(_invalidated||Identity()!=_identity)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            // Persist selection across Play domain reload without changing scene assets.
            SessionState.SetString("LaneDefense.WorkbenchLevel",AssetDatabase.GetAssetPath(_level));
            EditorSceneManager.OpenScene("Assets/Game/Scenes/FrameworkScene.unity");EditorApplication.isPlaying=true;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Open(){LaneDefenseExampleBuilder.RequireAccess();var w=GetWindow<LaneDefenseWorkbench>("塔防开发工具");w._identity=w.Identity();w._invalidated=false;w.minSize=new Vector2(560,420);w.Show();}
        #endregion
    }

    [CustomEditor(typeof(LaneDefenseLevel))]
    internal sealed class LaneDefenseLevelInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if(!LaneDefenseExampleBuilder.CanAuthor){EditorGUILayout.HelpBox("此配置只在正式 lane-defense 模板体系且模块启用时可编辑。",MessageType.Info);return;}
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))DrawDefaultInspector();
            foreach(string error in ((LaneDefenseLevel)target).Validate())EditorGUILayout.HelpBox(error,MessageType.Error);
        }
    }
}
