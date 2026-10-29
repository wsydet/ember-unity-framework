using System;
using Ember.Core.Editor;
using System.Linq;
using Game.Entity;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    [InitializeOnLoad]
    public sealed class PlayerDevelopmentWindow:EditorWindow
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerScheme _scheme;
        [SerializeField] private string _ownerTemplate;
        [SerializeField] private string _extensionScope;
        [SerializeField] private string _copyFolder="Assets/GameResource/Resources/Config/Player/Custom";
        [SerializeField] private float _angle;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private string _message;
        internal bool HasAccess=>PlayerEditorAccess.Available&&_ownerTemplate==EmberFsmBlueprintAccess.CurrentTemplateId
            &&EmberExtensionProject.AllowsScope(EmberExtensionProject.FsmBlueprint,_extensionScope);
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        static PlayerDevelopmentWindow()
        {
            EmberConditionalMenu.Register("Ember/Player/玩家开发工具",()=>PlayerEditorAccess.Available,Open);
            EmberFsmBlueprintParameters.Register("topdown.player.parameters",DrawBlueprint,ownerTemplateId:"topdown-2d",enabled:()=>PlayerEditorAccess.ModuleEnabled);
            EmberExtensionDocuments.RegisterModuleScope("topdown.player","topdown-2d",()=>PlayerEditorAccess.ModuleEnabled);
        }
        private void OnEnable(){titleContent=new GUIContent("俯视玩家");if(string.IsNullOrEmpty(_ownerTemplate))_ownerTemplate=EmberFsmBlueprintAccess.CurrentTemplateId;
            if(string.IsNullOrEmpty(_extensionScope))_extensionScope=EmberExtensionProject.CaptureScope(EmberExtensionProject.FsmBlueprint);}
        private void OnGUI()
        {
            if(!HasAccess)
            {EditorGUILayout.HelpBox("原模板、扩展版本或 Player 模块已失效；保留所选资源和草稿，重新从当前模板入口打开。",MessageType.Warning);return;}
            _scheme=(PlayerScheme)EditorGUILayout.ObjectField("角色方案",_scheme,typeof(PlayerScheme),false);
            if(_scheme==null)return;
            EditorGUILayout.HelpBox("运行会话捕获独立副本；编辑源方案只影响之后的新会话。Sprite 美术显式共享。",MessageType.Info);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EmberFsmBlueprintParameters.DrawProperties(_scheme,"_id","_speed","_acceleration","_braking","_aimMode","_aimDeadzone","_defaultFacing","_obstacles","_skin","_combatEnabled","_combat","_powers","_weapons","_animations","_allowPrimitivePresentation");
                _copyFolder=EditorGUILayout.TextField("新方案目录",_copyFolder);
                if(GUILayout.Button("复制为独立方案"))Run(()=>{_scheme=PlayerSchemeWorkflow.Copy(_scheme,_copyFolder);_message="已复制独立方案、图、定义和能力配置。";});
                if(GUILayout.Button("校验方案"))Run(()=>{using var frozen=_scheme.Capture();_message=$"校验通过：{frozen.Id} · {frozen.Graph.Nodes.Count} 节点";});
            }
            if(GUILayout.Button("打开状态机蓝图"))EmberFsmBlueprintWindow.Open(_scheme.Graph).ParameterPreset=_scheme;
            foreach(var weapon in _scheme.Weapons)
                if(weapon!=null&&GUILayout.Button("武器蓝图 · "+weapon.DisplayName))EmberFsmBlueprintWindow.Open(weapon.Graph).ParameterPreset=weapon;
            _angle=EditorGUILayout.Slider("本地攻击方向",_angle,0,360);Preview();
            if(!string.IsNullOrEmpty(_message))EditorGUILayout.HelpBox(_message,MessageType.Info);
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Run(Action action){try{if(!HasAccess)throw new InvalidOperationException("窗口版本或启用条件已变化，草稿保留。");PlayerEditorAccess.RequireWrite();action();}catch(Exception e){_message=e.Message;}}
        private void Preview()
        {
            var rect=GUILayoutUtility.GetRect(220,220);var center=rect.center;const float scale=70;
            try
            {
                using var frozen=_scheme.Capture();var direction=new Vector2(Mathf.Cos(_angle*Mathf.Deg2Rad),Mathf.Sin(_angle*Mathf.Deg2Rad));
                if(frozen.Weapons.Count==0){GUI.Label(rect,"当前方案未装配武器。");return;}
                var spec=frozen.Weapons[0];var size=spec.Mode==Weapons.WeaponHitMode.Ray?new Vector2(spec.Range,.05f):spec.Size;
                var offset=EntityPlaneFrame.Transform(spec.Mode==Weapons.WeaponHitMode.Ray?new Vector2(spec.Range*.5f,0):spec.Offset,direction);
                var points=new Vector3[5];var signs=new[]{new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,1),new Vector2(1,-1),new Vector2(-1,-1)};
                for(var i=0;i<points.Length;i++){var v=offset+EntityPlaneFrame.Transform(Vector2.Scale(signs[i],size*.5f),direction);points[i]=center+new Vector2(v.x,-v.y)*scale;}
                Handles.BeginGUI();Handles.color=new Color(.3f,1,.9f);Handles.DrawAAPolyLine(3,points);Handles.color=Color.yellow;
                Handles.DrawAAPolyLine(3,center,center+new Vector2(direction.x,-direction.y)*scale);Handles.EndGUI();
                GUI.Label(new Rect(rect.x,rect.y,rect.width,25),"运行查询、刀光与此预览共用 EntityPlaneFrame");
            }
            catch(Exception e){GUI.Label(rect,e.Message);}
        }
        private static void DrawBlueprint(EmberFsmBlueprintWindow window)
        {
            var weapon=window.ParameterPreset as Weapons.WeaponDefinition;
            if(weapon!=null&&weapon.Graph==window.ParameterGraph)
            {using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))EmberFsmBlueprintParameters.DrawProperties(weapon,"_id","_displayName","_mode","_windup","_active","_recovery","_lockMovement","_lockFacing","_offset","_size","_range","_damage","_stun","_braking","_knockback","_powers");return;}
            var scheme=window.ParameterPreset as PlayerScheme;
            if(scheme==null||scheme.Graph!=window.ParameterGraph)return;
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                EmberFsmBlueprintParameters.DrawProperties(scheme,"_speed","_acceleration","_braking","_combat","_animations");
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Open()
        {
            if(!PlayerEditorAccess.Available)throw new InvalidOperationException("当前模板或模块不支持俯视玩家工具。");
            var window=Resources.FindObjectsOfTypeAll<PlayerDevelopmentWindow>().FirstOrDefault(w=>w.HasAccess)??CreateInstance<PlayerDevelopmentWindow>();
            if(window._scheme==null)window._scheme=Resources.Load<PlayerScheme>("Config/Player/PlayerScheme");window.Show();
        }
        #endregion
    }
}
