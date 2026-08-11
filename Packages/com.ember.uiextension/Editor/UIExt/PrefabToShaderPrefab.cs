////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using System.Collections;
////using System.Collections.Generic;
////using UnityEngine;
////using UnityEditor;
////using UnityEngine.UI;
////using Burner.UIExtension;
////#if UNITY_2022_1_OR_NEWER
////using UnityEditor.SceneManagement;
////#else
////using UnityEditor.Experimental.SceneManagement;
////#endif
////public class PrefabToShaderPrefabWindow : EditorWindow
////{
////    #region shader
////
////    static string shaderContent1 =
////   @"
////Shader ""{0}""
////{{
////    Properties
////    {{
////        [PerRendererData] [NoScaleOffset] _MainTex(""Sprite Texture"", 2D) = ""white"" {{}}
////        _Color (""Tint"", Color) = (1, 1, 1, 1)
////        _Visible(""Visible"", Float) = 1
////        _Gray(""Gray"", Float) = 1
////
////		_StencilComp (""Stencil Comparison"", Float) = 8
////		_Stencil (""Stencil ID"", Float) = 0
////		_StencilOp (""Stencil Operation"", Float) = 0
////		_StencilWriteMask (""Stencil Write Mask"", Float) = 255
////		_StencilReadMask (""Stencil Read Mask"", Float) = 255
////
////		_ColorMask (""Color Mask"", Float) = 15
////";
////
////    //Texture Parameter
////    static string shaderContent2 =
////        @"
////        [NoScaleOffset] _Texture_{0}(""Texture_{0}"", 2D) = ""black"" {{}}
////        _TextureColor_{0}(""TextureColor_{0}"", Color) = (1, 1, 1, 1)
////        _TextureVisible_{0}(""TextureVisible_{0}"", Float) = 1
////        _TextureGray_{0}(""TextureGray_{0}"", Float) = 1
////
////";
////
////    static string shaderContent3 =
////        @"
////    }}
////
////	SubShader
////	{{
////		Tags
////		{{
////			""Queue""=""Transparent""
////			""IgnoreProjector""=""True""
////			""RenderType""=""Transparent""
////			""PreviewType""=""Plane""
////			""CanUseSpriteAtlas""=""True""
////		}}
////
////		Stencil
////		{{
////			Ref [_Stencil]
////			Comp [_StencilComp]
////			Pass [_StencilOp]
////			ReadMask [_StencilReadMask]
////			WriteMask [_StencilWriteMask]
////		}}
////
////		Cull Off
////		Lighting Off
////		ZWrite Off
////		ZTest [unity_GUIZTestMode]
////		Fog {{ Mode Off }}
////		Blend One OneMinusSrcAlpha
////		ColorMask [_ColorMask]
////
////		Pass
////		{{
////		CGPROGRAM
////			#pragma vertex vert
////			#pragma fragment frag
////			#include ""UnityCG.cginc""
////			#include ""UnityUI.cginc""
////
////            fixed GetUvClipping(fixed2 uv)
////            {{
////                fixed outOfU = uv.x <= 0 || uv.x >= 1;
////                fixed outOfV = uv.y <= 0 || uv.y >= 1;
////                return 1 - max(outOfU, outOfV);
////            }}
////
////            half2 TransformUv(half2 uv, half4 mat)
////            {{
////                half3 xAsix = half3(mat.x + 1, 0, mat.z);
////                half3 yAsix = half3(0, mat.y + 1, mat.w);
////
////                half3 uvEx = half3(uv, 1);
////                return half2(dot(uvEx, xAsix), dot(uvEx, yAsix));
////            }}
////
////			struct appdata_t
////			{{
////				float4 vertex   : POSITION;
////				float4 color    : COLOR;
////				float2 texcoord : TEXCOORD0;
////			}};
////
////			struct v2f
////			{{
////				float4 vertex   : SV_POSITION;
////				fixed4 color    : COLOR;
////				half2 texcoord  : TEXCOORD0;
////				half2 rawTexcoord  : TEXCOORD1;
////				float4 worldPosition : TEXCOORD2;
////			}};
////
////            bool _UseClipRect;
////            float4 _ClipRect;
////            bool _UseAlphaClip;
////            float4 _UvTransform;
////            float4 _InvUvTransform;
////
////			v2f vert(appdata_t IN)
////			{{
////				v2f OUT;
////				OUT.worldPosition = IN.vertex;
////				OUT.vertex = UnityObjectToClipPos(IN.vertex);
////				OUT.texcoord = IN.texcoord;
////                OUT.rawTexcoord = TransformUv(IN.texcoord, _InvUvTransform);
////#ifdef UNITY_HALF_TEXEL_OFFSET
////				OUT.vertex.xy += (_ScreenParams.zw-1.0)*float2(-1,1);
////#endif
////				OUT.color.a = IN.color.a;
////				OUT.color.rgb = IN.color.rgb * IN.color.a;
////				return OUT;
////			}}
////
////            #pragma shader_feature _ENABLE_GRAY
////            sampler2D _MainTex;
////            fixed4 _Color;
////            float _Visible;
////            float _Gray;
////            float4 _MainTexturePos;
////";
////
////    static string shaderContent4 =
////        @"
////            #pragma shader_feature _ENABLE_GRAY{0}
////            sampler2D _Texture_{0};
////            fixed4 _TextureColor_{0};
////            float _TextureVisible_{0};
////            float _TextureGray_{0};
////            float4 _TexturePos_{0};
////            float4 _TextureUvTransform_{0};
////            float4 _TextureInvUvTransform_{0};
////";
////
////    static string shaderContent5 =
////        @"
////            fixed4 frag(v2f IN) : SV_TARGET
////            {{
////                half invAlpha = 1;
////                half3 color = half3(0, 0, 0);
////
////                {{
////                    half2 uv = TransformUv(IN.rawTexcoord, _MainTexturePos);
////                    half2 uvSample = TransformUv(uv, _UvTransform);
////                    half4 color0 = tex2D(_MainTex, uvSample) * _Color * _Visible;
////                    #ifdef _ENABLE_GRAY
////                        half grayColor0 = dot(color0.rgb, half3(0.3, 0.59, 0.11));
////                        color0.rgb = lerp(color0.rgb, half3(grayColor0, grayColor0, grayColor0), _Gray);
////                    #endif
////                    color0.a *= GetUvClipping(uv);
////                    invAlpha *= (1 - color0.a);
////                    color = lerp(color, color0.rgb, color0.a);
////                }}
////";
////
////
////    static string shaderContent6 =
////        @"
////                {{
////                    half2 uv = TransformUv(IN.rawTexcoord, _TexturePos_{0});
////                    half2 uvSample = TransformUv(uv, _TextureUvTransform_{0});
////                    half4 color{0} = tex2D(_Texture_{0}, uvSample) * _TextureColor_{0} * _TextureVisible_{0};
////                    #ifdef _ENABLE_GRAY{0}
////                        half grayColor{0} = dot(color{0}.rgb, half3(0.3, 0.59, 0.11));
////                        color{0}.rgb = lerp(color{0}.rgb, half3(grayColor{0}, grayColor{0}, grayColor{0}), _TextureGray_{0});
////                    #endif
////                    color{0}.a *= GetUvClipping(uv);
////                    invAlpha *= (1 - color{0}.a);
////                    color = lerp(color, color{0}.rgb, color{0}.a);
////                }}
////";
////
////    static string shaderContent7 =
////        @"
////                half alpha = 1 - invAlpha;
////
////                alpha *= IN.color.a;
////                color *= IN.color.rgb;
////
////                if(_UseClipRect)
////                    alpha *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
////
////                if(_UseAlphaClip)
////                    clip(alpha - 0.001);
////
////                return half4(color, alpha);
////            }}
////        ENDCG
////        }}
////    }}
////}}
////";
////    #endregion
////
////    #region mask shader
////
////    static string maskShaderContent1 =
////   @"
////Shader ""{0}""
////{{
////    Properties
////    {{
////        [PerRendererData] [NoScaleOffset] _MainTex(""Sprite Texture"", 2D) = ""white"" {{}}
////        _Color (""Tint"", Color) = (1, 1, 1, 1)
////        _Visible(""Visible"", Float) = 1
////        _Gray(""Gray"", Float) = 1
////
////		_StencilComp (""Stencil Comparison"", Float) = 8
////		_Stencil (""Stencil ID"", Float) = 0
////		_StencilOp (""Stencil Operation"", Float) = 0
////		_StencilWriteMask (""Stencil Write Mask"", Float) = 255
////		_StencilReadMask (""Stencil Read Mask"", Float) = 255
////
////		_ColorMask (""Color Mask"", Float) = 15
////";
////
////    //Texture Parameter
////    static string maskShaderContent2 =
////        @"
////        [NoScaleOffset] _Texture_{0}(""Texture_{0}"", 2D) = ""black"" {{}}
////        _TextureColor_{0}(""TextureColor_{0}"", Color) = (1, 1, 1, 1)
////        _TextureVisible_{0}(""TextureVisible_{0}"", Float) = 1
////        _TextureGray_{0}(""TextureGray_{0}"", Float) = 1
////
////";
////
////    static string maskShaderContent3 =
////        @"
////    }}
////
////	SubShader
////	{{
////		Tags
////		{{
////			""Queue""=""Transparent""
////			""IgnoreProjector""=""True""
////			""RenderType""=""Transparent""
////			""PreviewType""=""Plane""
////			""CanUseSpriteAtlas""=""True""
////		}}
////
////		Stencil
////		{{
////			Ref [_Stencil]
////			Comp [_StencilComp]
////			Pass [_StencilOp]
////			ReadMask [_StencilReadMask]
////			WriteMask [_StencilWriteMask]
////		}}
////
////		Cull Off
////		Lighting Off
////		ZWrite Off
////		ZTest [unity_GUIZTestMode]
////		Fog {{ Mode Off }}
////		Blend One OneMinusSrcAlpha
////		ColorMask [_ColorMask]
////
////		Pass
////		{{
////		CGPROGRAM
////			#pragma vertex vert
////			#pragma fragment frag
////			#include ""UnityCG.cginc""
////			#include ""UnityUI.cginc""
////
////            fixed GetUvClipping(fixed2 uv)
////            {{
////                fixed outOfU = (uv.x >= 0) * (uv.x <= 1);
////                fixed outOfV = (uv.y >= 0) * (uv.y <= 1);
////                return outOfU * outOfV;
////            }}
////
////            half2 TransformUv(half2 uv, half4 mat)
////            {{
////                half3 xAsix = half3(mat.x + 1, 0, mat.z);
////                half3 yAsix = half3(0, mat.y + 1, mat.w);
////
////                half3 uvEx = half3(uv, 1);
////                return half2(dot(uvEx, xAsix), dot(uvEx, yAsix));
////            }}
////
////            fixed CalcMask(half alpha, fixed parentMask, fixed generateMask)
////            {{
////                fixed a = alpha > 0.004;
////                return dot(float3(a, 1, a), float3(1, generateMask, -generateMask)) * parentMask;
////            }}
////
////			struct appdata_t
////			{{
////				float4 vertex   : POSITION;
////				float4 color    : COLOR;
////				float2 texcoord : TEXCOORD0;
////			}};
////
////			struct v2f
////			{{
////				float4 vertex   : SV_POSITION;
////				fixed4 color    : COLOR;
////				half2 texcoord  : TEXCOORD0;
////				half2 rawTexcoord  : TEXCOORD1;
////				float4 worldPosition : TEXCOORD2;
////			}};
////
////            bool _UseClipRect;
////            float4 _ClipRect;
////            bool _UseAlphaClip;
////            float4 _UvTransform;
////            float4 _InvUvTransform;
////
////			v2f vert(appdata_t IN)
////			{{
////				v2f OUT;
////				OUT.worldPosition = IN.vertex;
////				OUT.vertex = UnityObjectToClipPos(IN.vertex);
////				OUT.texcoord = IN.texcoord;
////                OUT.rawTexcoord = TransformUv(IN.texcoord, _InvUvTransform);
////#ifdef UNITY_HALF_TEXEL_OFFSET
////				OUT.vertex.xy += (_ScreenParams.zw-1.0)*float2(-1,1);
////#endif
////				OUT.color.a = IN.color.a;
////				OUT.color.rgb = IN.color.rgb * IN.color.a;
////				return OUT;
////			}}
////
////            #pragma shader_feature _ENABLE_GRAY
////            #pragma shader_feature _ENABLE_MASK
////            sampler2D _MainTex;
////            fixed4 _Color;
////            float _Visible;
////            float _Gray;
////            float4 _MainTexturePos;
////            float _InvGenerateMask;
////
////";
////
////    static string maskShaderContent4 =
////        @"
////            #pragma shader_feature _ENABLE_GRAY{0}
////            #pragma shader_feature _ENABLE_MASK{0}
////            sampler2D _Texture_{0};
////            fixed4 _TextureColor_{0};
////            float _TextureVisible_{0};
////            float _TextureGray_{0};
////            int _TextureParentIndex_{0};
////            float _InvTextureGenerateMask_{0};
////            float4 _TexturePos_{0};
////            float4 _TextureUvTransform_{0};
////            float4 _TextureInvUvTransform_{0};
////";
////
////    static string maskShaderContent5 =
////        @"
////            fixed4 frag(v2f IN) : SV_TARGET
////            {{
////                half invAlpha = 1;
////                half3 color = half3(0, 0, 0);
////                //1为不遮挡，0为遮挡
////                fixed maskArray[8] = {{1, 1, 1, 1, 1, 1, 1, 1}};
////
////                {{
////                    half2 uv = TransformUv(IN.rawTexcoord, _MainTexturePos);
////                    half2 uvSample = TransformUv(uv, _UvTransform);
////                    half4 color0 = tex2D(_MainTex, uvSample) * _Color * _Visible;
////                    #ifdef _ENABLE_GRAY
////                        half grayColor0 = dot(color0.rgb, half3(0.3, 0.59, 0.11));
////                        color0.rgb = lerp(color0.rgb, half3(grayColor0, grayColor0, grayColor0), _Gray);
////                    #endif
////                    color0.a *= GetUvClipping(uv);
////                    #ifdef _ENABLE_MASK
////                        maskArray[0] = CalcMask(color0.a, maskArray[0], _InvGenerateMask);
////                        color0.a *= maskArray[0];
////                    #endif
////                    invAlpha *= (1 - color0.a);
////                    color = lerp(color, color0.rgb, color0.a);
////                }}
////";
////
////
////    static string maskShaderContent6 =
////        @"
////                {{
////                    half2 uv = TransformUv(IN.rawTexcoord, _TexturePos_{0});
////                    half2 uvSample = TransformUv(uv, _TextureUvTransform_{0});
////                    half4 color{0} = tex2D(_Texture_{0}, uvSample) * _TextureColor_{0} * _TextureVisible_{0};
////                    #ifdef _ENABLE_GRAY{0}
////                        half grayColor{0} = dot(color{0}.rgb, half3(0.3, 0.59, 0.11));
////                        color{0}.rgb = lerp(color{0}.rgb, half3(grayColor{0}, grayColor{0}, grayColor{0}), _TextureGray_{0});
////                    #endif
////                    color{0}.a *= GetUvClipping(uv);
////                    #ifdef _ENABLE_MASK{0}
////                        maskArray[{0}] = CalcMask(color{0}.a, maskArray[_TextureParentIndex_{0}], _InvTextureGenerateMask_{0});
////                        color{0}.a *= maskArray[{0}];
////                    #endif
////                    invAlpha *= (1 - color{0}.a);
////                    color = lerp(color, color{0}.rgb, color{0}.a);
////                }}
////";
////
////    static string maskShaderContent7 =
////        @"
////                half alpha = 1 - invAlpha;
////
////                alpha *= IN.color.a;
////                color *= IN.color.rgb;
////
////                if(_UseClipRect)
////                    alpha *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
////
////                if(_UseAlphaClip)
////                    clip(alpha - 0.004);
////
////                return half4(color, alpha);
////            }}
////        ENDCG
////        }}
////    }}
////}}
////";
////    #endregion
////
////    enum EditorMode
////    {
////        SelectMode,
////        MergeImageMode,
////        UnpackMergeImageMode,
////    }
////
////    List<GameObject> mSrcGameObjectList = new List<GameObject>();
////    List<string> mNameList = new List<string>();
////    List<bool> mEnableGrayList = new List<bool>();
////    GameObject mDestParent;
////    static string DefaultMergedNodeName = "m_MergedImage";
////    string mMergedNodeName = DefaultMergedNodeName;
////    bool mAutoDelete = true;
////    bool mAutoResort = true;
////    Vector2 mScrollViewOffset = Vector2.zero;
////
////    List<GameObject> mNeedUnpackGameObjectList = new List<GameObject>();
////    bool mAutoDeleteMergedImageComponent = true;
////
////    EditorMode mEditorMode = EditorMode.SelectMode;
////
////    [MenuItem("Burner/Burner UI/Optimization/预生成 MergedImage Shader", false)]
////    private static void CreateAllShader()
////    {
////        for (int i = 2; i <= 8; ++i)
////        {
////            {
////                string shaderName, shaderPath;
////
////                GetShaderNameAndPath(i, true, out shaderName, out shaderPath);
////
////                string sourceCode = CreateShaderSourceCode(i, shaderName, true);
////                ProjectWindowUtil.CreateAssetWithContent(shaderPath, sourceCode);
////            }
////
////            {
////                string shaderName, shaderPath;
////
////                GetShaderNameAndPath(i, false, out shaderName, out shaderPath);
////
////                string sourceCode = CreateShaderSourceCode(i, shaderName, false);
////                ProjectWindowUtil.CreateAssetWithContent(shaderPath, sourceCode);
////            }
////        }
////    }
////
////    [MenuItem("Burner/Burner UI/Optimization/Shader 化工具", false)]
////    private static void CreateWindow()
////    {
////        GetWindow(typeof(PrefabToShaderPrefabWindow), false, "节点Shader化");
////    }
////
////    private void OnSelectModeGUI()
////    {
////        if (GUILayout.Button("Prefab Shader 化"))
////            mEditorMode = EditorMode.MergeImageMode;
////
////        if (GUILayout.Button("解开MergedPrefab"))
////            mEditorMode = EditorMode.UnpackMergeImageMode;
////    }
////
////    private void OnMergeImageGUI()
////    {
////        {
////            EditorGUILayout.BeginHorizontal();
////            if (GUILayout.Button("返回上级界面"))
////                mEditorMode = EditorMode.SelectMode;
////            GUILayout.Space(5000);
////            EditorGUILayout.EndHorizontal();
////        }
////
////        GUILayout.Label("Prefab Shader 化");
////
////        mDestParent = (GameObject)EditorGUILayout.ObjectField("根节点", mDestParent, typeof(GameObject), true);
////        mMergedNodeName = EditorGUILayout.TextField("MergedNode名称", mMergedNodeName);
////        {
////            int arraySize = EditorGUILayout.IntField("合并GameObject数量", mSrcGameObjectList.Count);
////            if (arraySize < 0)
////                arraySize = 0;
////            if (arraySize != mSrcGameObjectList.Count)
////            {
////                while (arraySize < mSrcGameObjectList.Count)
////                    mSrcGameObjectList.RemoveAt(mSrcGameObjectList.Count - 1);
////
////                while (arraySize > mSrcGameObjectList.Count)
////                    mSrcGameObjectList.Add(null);
////            }
////
////            if (arraySize != mNameList.Count)
////            {
////                while (arraySize < mNameList.Count)
////                    mNameList.RemoveAt(mNameList.Count - 1);
////
////                while (arraySize > mNameList.Count)
////                    mNameList.Add("");
////            }
////
////            if(arraySize != mEnableGrayList.Count)
////            {
////                while (arraySize < mEnableGrayList.Count)
////                    mEnableGrayList.RemoveAt(mEnableGrayList.Count - 1);
////
////                while (arraySize > mEnableGrayList.Count)
////                    mEnableGrayList.Add(false);
////            }
////
////            for (int i = 0; i != arraySize; ++i)
////            {
////                if (mNameList[i].Length == 0 && mSrcGameObjectList[i] != null)
////                    mNameList[i] = mSrcGameObjectList[i].name;
////            }
////
////            if (mMergedNodeName.Length == 0)
////            {
////                mMergedNodeName = DefaultMergedNodeName;
////            }
////
////            EditorGUI.indentLevel++;
////            GUILayout.BeginVertical();
////            mScrollViewOffset = GUILayout.BeginScrollView(mScrollViewOffset);
////            for (int i = 0; i != mSrcGameObjectList.Count; ++i)
////            {
////                EditorGUILayout.BeginHorizontal();
////                GameObject newObj = (GameObject)EditorGUILayout.ObjectField("GameObject", mSrcGameObjectList[i], typeof(GameObject), true);
////                if(mSrcGameObjectList[i] != newObj)
////                {
////                    mSrcGameObjectList[i] = newObj;
////                    if(mSrcGameObjectList[i] != null)
////                        mNameList[i] = mSrcGameObjectList[i].name;
////                }
////
////                mNameList[i] = EditorGUILayout.TextField(mNameList[i]);
////                mEnableGrayList[i] = EditorGUILayout.Toggle("启用Gray功能", mEnableGrayList[i]);
////                if (GUILayout.Button("移除"))
////                {
////                    mSrcGameObjectList.RemoveAt(i);
////                    mNameList.RemoveAt(i);
////                    mEnableGrayList.RemoveAt(i);
////                    --i;
////                }
////                EditorGUILayout.EndHorizontal();
////            }
////            GUILayout.EndScrollView();
////            GUILayout.EndVertical();
////            EditorGUI.indentLevel--;
////        }
////
////        mAutoDelete = EditorGUILayout.Toggle("自动移除无额外组件的节点", mAutoDelete);
////        mAutoResort = EditorGUILayout.Toggle("自动按照节点顺序重排GameObject列表", mAutoResort);
////
////        if (GUILayout.Button("递归包含子节点"))
////        {
////            List<GameObject> newGameObjectList = AddChildrenNode(mSrcGameObjectList);
////            RestoreNameListAndGrayList(newGameObjectList);
////        }
////
////        if (GUILayout.Button("重排GameObject"))
////        {
////            if (HasCommonParent(mSrcGameObjectList) == false)
////            {
////                EditorUtility.DisplayDialog("错误", "输入的GameObject列表不在相同场景内", "OK");
////                return;
////            }
////
////            if (HasSameGameObject(mSrcGameObjectList))
////            {
////                EditorUtility.DisplayDialog("错误", "列表中有重复的GameObject", "OK");
////                return;
////            }
////
////            List<GameObject> newGameObjectList = ResortGameObjectList(mSrcGameObjectList);
////            RestoreNameListAndGrayList(newGameObjectList);
////        }
////
////        if (GUILayout.Button("转化"))
////        {
////            if (CheckAllMergeable(mSrcGameObjectList) == false)
////            {
////                EditorUtility.DisplayDialog("错误", "GameObject列表中存在无法合并的元素", "OK");
////                return;
////            }
////
////            if (HasCommonParent(mSrcGameObjectList) == false)
////            {
////                EditorUtility.DisplayDialog("错误", "输入的GameObject列表不在相同场景内", "OK");
////                return;
////            }
////
////            if (mSrcGameObjectList.Contains(null))
////            {
////                EditorUtility.DisplayDialog("错误", "GameObject列表中存在为空的元素", "OK");
////                return;
////            }
////
////            if (mSrcGameObjectList.Count == 1)
////            {
////                EditorUtility.DisplayDialog("错误", "一个GameObject无需合并", "OK");
////                return;
////            }
////
////            if (mSrcGameObjectList.Count == 0)
////            {
////                EditorUtility.DisplayDialog("错误", "GameObject列表为空", "OK");
////                return;
////            }
////
////            if (mSrcGameObjectList.Count > 8)
////            {
////                EditorUtility.DisplayDialog("错误", "GameObject列表数量大于8", "OK");
////                return;
////            }
////
////            if (mDestParent == null)
////            {
////                EditorUtility.DisplayDialog("错误", "目标父节点为空", "OK");
////                return;
////            }
////
////            MergePrefabNode(mDestParent, mSrcGameObjectList);
////
////            if (mAutoDelete)
////            {
////                RemoveGraphicsComponent(mSrcGameObjectList);
////                RemovUselesseUnusedNode(mSrcGameObjectList);
////            }
////        }
////    }
////
////    private void OnUnpackMergeImageGUI()
////    {
////        {
////            EditorGUILayout.BeginHorizontal();
////            if (GUILayout.Button("返回上级界面"))
////                mEditorMode = EditorMode.SelectMode;
////            GUILayout.Space(5000);
////            EditorGUILayout.EndHorizontal();
////        }
////
////        GUILayout.Label("解开MergedImage");
////
////        int arraySize = EditorGUILayout.IntField("Merged Image数量", mNeedUnpackGameObjectList.Count);
////        {
////            if (arraySize < 0)
////                arraySize = 0;
////            if (arraySize != mNeedUnpackGameObjectList.Count)
////            {
////                while (arraySize < mNeedUnpackGameObjectList.Count)
////                    mNeedUnpackGameObjectList.RemoveAt(mNeedUnpackGameObjectList.Count - 1);
////
////                while (arraySize > mNeedUnpackGameObjectList.Count)
////                    mNeedUnpackGameObjectList.Add(null);
////            }
////
////            EditorGUI.indentLevel++;
////            GUILayout.BeginVertical();
////            mScrollViewOffset = GUILayout.BeginScrollView(mScrollViewOffset);
////            for (int i = 0; i != mNeedUnpackGameObjectList.Count; ++i)
////            {
////                EditorGUILayout.BeginHorizontal();
////                mNeedUnpackGameObjectList[i] = (GameObject)EditorGUILayout.ObjectField("Merged Image", mNeedUnpackGameObjectList[i], typeof(GameObject), true);
////                if (GUILayout.Button("移除"))
////                {
////                    mNeedUnpackGameObjectList.RemoveAt(i);
////                    --i;
////                }
////                EditorGUILayout.EndHorizontal();
////            }
////            GUILayout.EndScrollView();
////            GUILayout.EndVertical();
////            EditorGUI.indentLevel--;
////        }
////
////        mAutoDeleteMergedImageComponent = EditorGUILayout.Toggle("自动移除MergedImage组件，但保留相应节点", mAutoDeleteMergedImageComponent);
////
////        if (GUILayout.Button("收集全部MergedImage"))
////        {
////            mNeedUnpackGameObjectList = GetAllMergedImageInScene();
////        }
////
////        if (GUILayout.Button("解开MergedImage"))
////        {
////            if (CheckAllIsMergedImage(mNeedUnpackGameObjectList) == false)
////            {
////                EditorUtility.DisplayDialog("错误", "列表中存在不包含MergedImage的组件", "OK");
////                return;
////            }
////
////            UnpackMergedImageList(mNeedUnpackGameObjectList);
////        }
////    }
////
////    private void OnGUI()
////    {
////        switch (mEditorMode)
////        {
////            case EditorMode.SelectMode: OnSelectModeGUI(); break;
////            case EditorMode.MergeImageMode: OnMergeImageGUI(); break;
////            case EditorMode.UnpackMergeImageMode: OnUnpackMergeImageGUI(); break;
////        }
////    }
////
////    #region 公共函数
////
////    private List<GameObject> ResortGameObjectList(List<GameObject> gameObjectList)
////    {
////        if (gameObjectList.Count <= 1)
////            return gameObjectList;
////
////        GameObject commonParent = FindCommonParent(gameObjectList);
////        List<GameObject> srcGameObjectList = new List<GameObject>(gameObjectList);
////        List<GameObject> childrenList = GetAllChildren(commonParent, true);
////
////        List<GameObject> sortedGameObjectList = new List<GameObject>();
////
////        for (int i = 0; i != childrenList.Count; ++i)
////        {
////            int index = srcGameObjectList.IndexOf(childrenList[i]);
////
////            if (index != -1)
////            {
////                sortedGameObjectList.Add(srcGameObjectList[index]);
////                srcGameObjectList.RemoveAt(index);
////            }
////        }
////
////        if (srcGameObjectList.Count != 0)
////            sortedGameObjectList.AddRange(srcGameObjectList);
////
////        return sortedGameObjectList;
////    }
////
////    Rect GetNodeRect(GameObject gameObject)
////    {
////        Vector3[] pos = new Vector3[4];
////        gameObject.GetComponent<RectTransform>().GetWorldCorners(pos);
////
////        Vector3 max = Vector3.Max(pos[0], pos[1]);
////        max = Vector3.Max(max, pos[2]);
////        max = Vector3.Max(max, pos[3]);
////
////        Vector3 min = Vector3.Min(pos[0], pos[1]);
////        min = Vector3.Min(min, pos[2]);
////        min = Vector3.Min(min, pos[3]);
////
////        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
////    }
////
////    #endregion
////
////    #region 合并贴图辅助函数
////    private GameObject FindCommonParent(GameObject o1, GameObject o2)
////    {
////        if (o1 == null)
////            return null;
////
////        if (o2 == null)
////            return null;
////
////        List<GameObject> o1ParentList = new List<GameObject>();
////        List<GameObject> o2ParentList = new List<GameObject>();
////
////        GameObject o1Parent = o1;
////        GameObject o2Parent = o2;
////
////        do
////        {
////            o1ParentList.Add(o1Parent);
////            o1Parent = o1Parent.transform.parent.gameObject;
////        } while (o1Parent.transform.parent != null);
////
////        do
////        {
////            o2ParentList.Add(o2Parent);
////            o2Parent = o2Parent.transform.parent.gameObject;
////        } while (o2Parent.transform.parent != null);
////
////        for (int i = 0; i != o1ParentList.Count; ++i)
////        {
////            if (o2ParentList.Contains(o1ParentList[i]))
////            {
////                return o1ParentList[i];
////            }
////        }
////
////        return null;
////    }
////
////    private bool HasSameGameObject(List<GameObject> gameObjectList)
////    {
////        if (gameObjectList.Count <= 1)
////            return false;
////
////        List<GameObject> tempGameObjectList = new List<GameObject>(gameObjectList);
////
////        tempGameObjectList.Sort((GameObject g1, GameObject g2) => { if (g1 == null) return -1; if (g2 == null) return 1; return g1.GetInstanceID() - g2.GetInstanceID(); });
////
////        for (int i = 0; i != tempGameObjectList.Count - 1; ++i)
////        {
////            if (tempGameObjectList[i] == tempGameObjectList[i + 1] && tempGameObjectList[i] != null)
////                return true;
////        }
////
////        return false;
////    }
////
////    private bool HasCommonParent(List<GameObject> gameObjectList)
////    {
////        if (gameObjectList.Count <= 1)
////            return true;
////
////        //如果一个节点与其他的任何节点都存在公共根节点，那么所有节点一定在一棵树上
////        GameObject firstNotNull = gameObjectList[0];
////        for (int i = 1; i != gameObjectList.Count; ++i)
////        {
////            if (gameObjectList[i] != null)
////            {
////                if (firstNotNull == null)
////                    firstNotNull = gameObjectList[i];
////
////                if (FindCommonParent(firstNotNull, gameObjectList[i]) == null)
////                    return false;
////            }
////        }
////
////        return true;
////    }
////
////    private bool CheckAllMergeable(List<GameObject> gameObjectList)
////    {
////        bool allMergeable = true;
////
////        foreach (GameObject gameObject in gameObjectList)
////        {
////            allMergeable = allMergeable && IsMergeAbleNode(gameObject);
////        }
////
////        return allMergeable;
////    }
////
////    private GameObject FindCommonParent(List<GameObject> gameObjectList)
////    {
////        GameObject commonParent = null;
////
////        if (gameObjectList.Count == 0)
////            return null;
////
////        for (int i = 0; i != gameObjectList.Count; ++i)
////        {
////            if (gameObjectList[i] != null)
////            {
////                if (commonParent == null)
////                    commonParent = gameObjectList[i];
////
////                commonParent = FindCommonParent(commonParent, gameObjectList[i]);
////            }
////        }
////
////        return commonParent;
////    }
////
////    private List<GameObject> AddChildrenNode(List<GameObject> gameObjectList)
////    {
////        List<GameObject> fullGameObjectList = new List<GameObject>();
////
////        foreach (GameObject gameObject in gameObjectList)
////        {
////            List<GameObject> childrenList = GetAllChildren(gameObject, false);
////
////            fullGameObjectList.Add(gameObject);
////
////            foreach (GameObject child in childrenList)
////            {
////                if (gameObjectList.Contains(child) == false)
////                    fullGameObjectList.Add(child);
////            }
////        }
////
////        return fullGameObjectList;
////    }
////
////    private void RestoreNameListAndGrayList(List<GameObject> newGameObjectList)
////    {
////        List<string> newNameList = new List<string>();
////        List<bool> newEnableGrayList = new List<bool>();
////
////        foreach(GameObject obj in newGameObjectList)
////        {
////            if(mSrcGameObjectList.Contains(obj))
////            {
////                int index = mSrcGameObjectList.IndexOf(obj);
////                newNameList.Add(mNameList[index]);
////                newEnableGrayList.Add(mEnableGrayList[index]);
////            }
////            else
////            {
////                newNameList.Add(obj == null ? "" : obj.name);
////                newEnableGrayList.Add(false);
////            }
////        }
////
////        mSrcGameObjectList = newGameObjectList;
////        mNameList = newNameList;
////        mEnableGrayList = newEnableGrayList;
////    }
////
////    private void MergePrefabNode(GameObject _rootNode, List<GameObject> mergeGameObjectList)
////    {
////        Rect rect = CalcVisibleNodeFullyRect(mergeGameObjectList);
////
////        bool enableMask = NeedEnableMask(mergeGameObjectList);
////
////        string shaderName, shaderPath;
////
////        GetShaderNameAndPath(mergeGameObjectList.Count, enableMask, out shaderName, out shaderPath);
////
////        if (Shader.Find(shaderName) == null)
////        {
////            string sourceCode = CreateShaderSourceCode(mergeGameObjectList.Count, shaderName, enableMask);
////            ProjectWindowUtil.CreateAssetWithContent(shaderPath, sourceCode);
////        }
////
////        GameObject mergedGameobject = null;
////
////        try
////        {
////            mergedGameobject = new GameObject(mMergedNodeName);
////            RectTransform rectTransform = mergedGameobject.AddComponent<RectTransform>();
////            MergedImage mergedImage = mergedGameobject.AddComponent<MergedImage>();
////            List<bool> needEnableMaskList = new List<bool>();
////
////            if (enableMask)
////                mergedImage.SetEnableMask(enableMask);
////
////            for (int i = 0; i != mergeGameObjectList.Count; ++i)
////            {
////                Image image = mergeGameObjectList[i].GetComponent<Image>();
////                Mask mask = mergeGameObjectList[i].GetComponent<Mask>();
////                int parrentIndex = GetTextureParentIndex(mergeGameObjectList, mergeGameObjectList[i]);
////
////                bool needEnableMask = mask != null;
////                if (parrentIndex != -1 && parrentIndex != i)
////                    needEnableMask = needEnableMask || needEnableMaskList[parrentIndex];
////                needEnableMaskList.Add(needEnableMask);
////
////                mergedImage.SetSprite(image.sprite, i);
////                mergedImage.SetTextureColor(image.color, i);
////                mergedImage.SetTextureGray(false, i);
////                mergedImage.SetTextureVisible(image.gameObject.activeInHierarchy, i);
////                mergedImage.SetTexturePos(MergedImage.CalcTransform(rect, GetNodeRect(mergeGameObjectList[i])), i);
////                mergedImage.SetTextureName(mNameList[i], i);
////
////                if (image is ImageEx)
////                {
////                    ImageEx imageEx = image as ImageEx;
////                    mergedImage.SetSupportMultiLanguage(imageEx.GetSupportMultiLanguage(), i);
////
////                    if (imageEx.GetSpriteArray() == null)
////                        mergedImage.SetSpriteList(new List<Sprite>(), i);
////                    else
////                        mergedImage.SetSpriteList(new List<Sprite>(imageEx.GetSpriteArray()), i);
////
////                    if (imageEx.GetSrpiteNameArray() == null)
////                        mergedImage.SetSpriteNameList(new List<string>(), i);
////                    else
////                        mergedImage.SetSpriteNameList(new List<string>(imageEx.GetSrpiteNameArray()), i);
////                    mergedImage.SetSpriteIndex(imageEx.SpriteIndex, i);
////                }
////
////                if (image.maskable)
////                    mergedImage.SetTextureParent(parrentIndex, i);
////                else
////                    mergedImage.SetTextureParent(i, i);
////                mergedImage.SetTextureGenerateMask(mask != null, i);
////            }
////
////            mergedGameobject.transform.SetParent(_rootNode.transform);
////            rectTransform.sizeDelta = rect.size;
////            Rect newRect = GetNodeRect(mergedGameobject);
////
////            Vector2 delta = rect.position - newRect.position;
////
////            rectTransform.anchoredPosition = rectTransform.anchoredPosition + delta;
////
////            Material material = new Material(Shader.Find(shaderName));
////
////            for (int i = 0; i != mergeGameObjectList.Count; ++i)
////            {
////                Image image = mergeGameObjectList[i].GetComponent<Image>();
////
////                if(i == 0)
////                {
////                    if (needEnableMaskList[i])
////                        material.EnableKeyword("_ENABLE_MASK");
////                    else
////                        material.DisableKeyword("_ENABLE_MASK");
////
////                    if(mEnableGrayList[i])
////                        material.EnableKeyword("_ENABLE_GRAY");
////                    else
////                        material.DisableKeyword("_ENABLE_GRAY");
////                }
////                else
////                {
////                    if (needEnableMaskList[i])
////                        material.EnableKeyword(string.Format("_ENABLE_MASK{0}", i));
////                    else
////                        material.DisableKeyword(string.Format("_ENABLE_MASK{0}", i));
////
////                    if (mEnableGrayList[i])
////                        material.EnableKeyword(string.Format("_ENABLE_GRAY{0}", i));
////                    else
////                        material.DisableKeyword(string.Format("_ENABLE_GRAY{0}", i));
////                }
////
////            }
////
////            var prefabStage = PrefabStageUtility.GetPrefabStage(_rootNode);
////
////            if (prefabStage != null)
////            {
////                string path = prefabStage.assetPath;
////                AssetDatabase.AddObjectToAsset(material, path);
////            }
////
////            EditorUtility.SetDirty(_rootNode);
////            mergedImage.material = material;
////            mergedImage.raycastTarget = false;
////            mergedImage.SetAllDirty();
////
////            mergedImage.gameObject.SetActive(false);
////            mergedImage.gameObject.SetActive(true);
////        }
////        catch (System.Exception e)
////        {
////            if (mergedGameobject != null)
////                DestroyImmediate(mergedGameobject);
////
////            throw e;
////        }
////    }
////
////    private void RemoveGraphicsComponent(List<GameObject> mergeGameObjectList)
////    {
////        foreach (GameObject gameObject in mergeGameObjectList)
////        {
////            Image image = gameObject.GetComponent<Image>();
////            CanvasRenderer canvasRenderer = gameObject.GetComponent<CanvasRenderer>();
////            Mask mask = gameObject.GetComponent<Mask>();
////
////            if (image != null)
////                DestroyImmediate(image);
////
////            if (canvasRenderer != null)
////                DestroyImmediate(canvasRenderer);
////
////            if (mask != null)
////                DestroyImmediate(mask);
////        }
////    }
////
////    private void RemovUselesseUnusedNode(List<GameObject> mergeGameObjectList)
////    {
////        List<GameObject> sortedGameObjectList = ResortGameObjectList(mergeGameObjectList);
////
////        sortedGameObjectList.Reverse();
////
////        foreach (GameObject gameObject in sortedGameObjectList)
////        {
////            Component[] allComponents = gameObject.GetComponents<Component>();
////
////            if (allComponents.Length == 1 && gameObject.transform.childCount == 0)
////            {
////                DestroyImmediate(gameObject);
////            }
////        }
////
////    }
////
////    Rect CalcVisibleNodeFullyRect(List<GameObject> gameObjectList)
////    {
////        if (gameObjectList.Count == 0)
////            return Rect.zero;
////
////        Rect rect = GetNodeRect(gameObjectList[0]);
////
////        for (int i = 0; i != gameObjectList.Count; ++i)
////        {
////            Rect childRect = GetNodeRect(gameObjectList[i]);
////
////            Vector2 min = Vector2.Min(childRect.min, rect.min);
////            Vector2 max = Vector2.Max(childRect.max, rect.max);
////
////            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
////        }
////
////        return rect;
////    }
////
////    int GetTextureParentIndex(List<GameObject> gameObjectList, GameObject currentGameObject)
////    {
////        Transform parent = currentGameObject.transform.parent;
////
////        while (parent != null)
////        {
////            int index = gameObjectList.IndexOf(parent.gameObject);
////
////            if (index != -1)
////                return index;
////
////            parent = parent.transform.parent;
////        }
////
////        return gameObjectList.IndexOf(currentGameObject);
////    }
////
////    static void GetShaderNameAndPath(int imageCount, bool enableMask, out string shaderName, out string shaderPath)
////    {
////        if (enableMask)
////        {
////            shaderName = string.Format("UI/ShaderForMaskMergeImage_{0}", imageCount);
////            shaderPath = string.Format("UI-ShaderForMaskMergeImage_{0}.shader", imageCount);
////        }
////        else
////        {
////            shaderName = string.Format("UI/ShaderForMergeImage_{0}", imageCount);
////            shaderPath = string.Format("UI-ShaderForMergeImage_{0}.shader", imageCount);
////        }
////    }
////
////    static string CreateShaderSourceCode(int imageCount, string shaderName, bool enableMask)
////    {
////        if (enableMask == false)
////        {
////            //Shader头
////            string shaderSource = string.Format(shaderContent1, shaderName);
////
////            //贴图及相应参数
////            for (int i = 1; i != imageCount; ++i)
////                shaderSource += string.Format(shaderContent2, i);
////
////            //vs主体，以及部分变量声明
////            shaderSource += string.Format(shaderContent3);
////
////            //贴图及相应参数说明
////            for (int i = 1; i != imageCount; ++i)
////                shaderSource += string.Format(shaderContent4, i);
////
////            //ps前半部分
////            shaderSource += string.Format(shaderContent5);
////
////            //附加贴图操作
////            for (int i = 1; i != imageCount; ++i)
////                shaderSource += string.Format(shaderContent6, i);
////
////            shaderSource += string.Format(shaderContent7);
////
////            return shaderSource;
////        }
////        else
////        {
////            //Shader头
////            string shaderSource = string.Format(maskShaderContent1, shaderName);
////
////            //贴图及相应参数
////            for (int i = 1; i != imageCount; ++i)
////                shaderSource += string.Format(maskShaderContent2, i);
////
////            //vs主体，以及部分变量声明
////            shaderSource += string.Format(maskShaderContent3);
////
////            //贴图及相应参数说明
////            for (int i = 1; i != imageCount; ++i)
////                shaderSource += string.Format(maskShaderContent4, i);
////
////            //ps前半部分
////            shaderSource += string.Format(maskShaderContent5);
////
////            //附加贴图操作
////            for (int i = 1; i != imageCount; ++i)
////                shaderSource += string.Format(maskShaderContent6, i);
////
////            shaderSource += string.Format(maskShaderContent7);
////
////            return shaderSource;
////        }
////    }
////
////    bool NeedEnableMask(List<GameObject> gameObjectList)
////    {
////        bool needEnableMask = false;
////
////        foreach (GameObject go in gameObjectList)
////        {
////            if (go.GetComponent<Mask>() != null)
////            {
////                needEnableMask = true;
////            }
////        }
////
////        return needEnableMask;
////    }
////
////    List<GameObject> GetAllChildren(GameObject rootGameObject, bool containSelf)
////    {
////        List<GameObject> childrenList = new List<GameObject>();
////
////        if (rootGameObject == null)
////            return new List<GameObject>();
////
////        if (containSelf)
////            childrenList.Add(rootGameObject);
////
////        TraivelNode(childrenList, rootGameObject);
////
////        return childrenList;
////    }
////
////    void TraivelNode(List<GameObject> gameObjectList, GameObject gameObject)
////    {
////        if (gameObject != null)
////        {
////            for (int i = 0; i != gameObject.transform.childCount; ++i)
////            {
////                gameObjectList.Add(gameObject.transform.GetChild(i).gameObject);
////                TraivelNode(gameObjectList, gameObject.transform.GetChild(i).gameObject);
////            }
////        }
////    }
////
////    bool IsMergeAbleNode(GameObject _node)
////    {
////        if (_node.GetComponent<Image>() != null && _node.GetComponent<Image>().material == _node.GetComponent<Image>().defaultMaterial)
////            return true;
////
////        return false;
////    }
////
////    #endregion
////
////    #region 拆分MergedImage辅助函数
////
////    List<GameObject> GetAllMergedImageInScene()
////    {
////        MergedImage[] mergedImages = UnityEditor.SceneManagement.StageUtility.GetCurrentStageHandle().FindComponentsOfType<MergedImage>();
////
////        List<GameObject> mergedImageGameObjectList = new List<GameObject>();
////        foreach (MergedImage mergedImage in mergedImages)
////            mergedImageGameObjectList.Add(mergedImage.gameObject);
////
////        return ResortGameObjectList(mergedImageGameObjectList);
////    }
////
////    bool CheckAllIsMergedImage(List<GameObject> mergedImageList)
////    {
////        foreach (GameObject gameObject in mergedImageList)
////        {
////            if (gameObject == null)
////                return false;
////            if (gameObject.GetComponent<MergedImage>() == null)
////                return false;
////        }
////
////        return true;
////    }
////
////    List<int> GetAllParent(int[] parentIndexList, int nodeIndex, bool containSelf = true)
////    {
////        List<int> parentList = new List<int>();
////
////        int currentParentIndex = nodeIndex;
////
////        if (containSelf)
////            parentList.Add(nodeIndex);
////        if (parentIndexList[currentParentIndex] == currentParentIndex)
////            parentList.Add(-1);
////        else
////        {
////            do
////            {
////                currentParentIndex = parentIndexList[currentParentIndex];
////                parentList.Add(currentParentIndex);
////            } while (currentParentIndex != -1);
////        }
////
////        return parentList;
////    }
////
////    int GetMaxCommonParent(List<int> parentList1, List<int> parentList2)
////    {
////        for (int i = 0; i != parentList1.Count; ++i)
////        {
////            if (parentList2.Contains(parentList1[i]))
////                return parentList1[i];
////        }
////
////        return -1;
////    }
////
////    void UnpackMergedImageList(List<GameObject> mergedImages)
////    {
////        foreach (GameObject gameObject in mergedImages)
////        {
////            UnpackMergedImage(gameObject);
////        }
////    }
////
////    void UnpackMergedImage(GameObject mergedImageGameObject)
////    {
////        if (mergedImageGameObject == null || mergedImageGameObject.GetComponent<MergedImage>() == null)
////            return;
////
////        MergedImage mergedImage = mergedImageGameObject.GetComponent<MergedImage>();
////
////        Rect mergedImageRect = GetNodeRect(mergedImageGameObject);
////
////        List<GameObject> newChildrenList = new List<GameObject>(new GameObject[mergedImage.ImageCount]);
////
////        try
////        {
////            //parentIndexList 为-1表示其父节点为MergedNode，其中第0的Node的父节点必定为MergedNode
////            int[] parentIndexList = new int[mergedImage.ImageCount];
////            int rootChildrenCount = 0;
////            for (int i = 0; i != mergedImage.ImageCount; ++i)
////            {
////                MergedImage.ImageInfo imageInfo = mergedImage.GetImageInfo(i);
////                parentIndexList[i] = imageInfo.mTexParentIndex;
////                if (parentIndexList[i] == i)
////                    rootChildrenCount++;
////            }
////
////            parentIndexList[0] = -1;
////
////            for (int i = 0; i != mergedImage.ImageCount; ++i)
////            {
////                MergedImage.ImageInfo imageInfo = mergedImage.GetImageInfo(i);
////
////                GameObject newGameObject = new GameObject(imageInfo.mTexName);
////                newChildrenList[i] = newGameObject;
////
////                RectTransform rectTransform = newGameObject.AddComponent<RectTransform>();
////
////                if (imageInfo.mSpriteNameList.Count != 0 || imageInfo.mSpriteList.Count != 0)
////                {
////                    ImageEx imageEx = newGameObject.AddComponent<ImageEx>();
////
////                    imageEx.SetSupportMultiLanguage(imageInfo.mSupportMultiLanguage);
////                    imageEx.SetSpriteNameArray(imageInfo.mSpriteNameList.ToArray());
////                    imageEx.SetSpriteArray(imageInfo.mSpriteList.ToArray());
////                    imageEx.SpriteIndex = imageInfo.mSpriteIndex;
////                }
////                else
////                {
////                    Image imageComponent = newGameObject.AddComponent<Image>();
////                    imageComponent.sprite = imageInfo.mSprite;
////                }
////
////                Image image = newGameObject.GetComponent<Image>();
////
////                image.color = imageInfo.mTexColor;
////                newGameObject.SetActive(imageInfo.mTexVisible);
////
////                if (imageInfo.mTexParentIndex == i)
////                    image.maskable = false;
////
////                if (imageInfo.mTexGenerateMask)
////                {
////                    Mask mask = newGameObject.AddComponent<Mask>();
////                    mask.showMaskGraphic = true;
////                }
////
////                if (i == 0)
////                    newGameObject.transform.SetParent(mergedImageGameObject.transform);
////                else if (imageInfo.mTexParentIndex != i)
////                    newGameObject.transform.SetParent(newChildrenList[imageInfo.mTexParentIndex].transform);
////                else
////                {
////                    List<int> currentNodeParentList = GetAllParent(parentIndexList, i - 1);
////
////                    int maxParent = -1;
////
////                    for (int j = i + 1; j < mergedImage.ImageCount; ++j)
////                    {
////                        List<int> parentIndex = GetAllParent(parentIndexList, j);
////
////                        maxParent = Mathf.Max(maxParent, GetMaxCommonParent(currentNodeParentList, parentIndex));
////                    }
////
////                    int maxParentIndex = currentNodeParentList.IndexOf(maxParent);
////                    if (maxParentIndex != -1 && maxParentIndex != currentNodeParentList.Count - 1)
////                        currentNodeParentList.RemoveRange(maxParentIndex + 1, currentNodeParentList.Count - maxParentIndex - 1);
////
////                    int parentIndexWithMaxChildrenCount = 0;
////                    int parentIndexChildrenCount = -1;
////
////                    for (int j = 0; j != currentNodeParentList.Count; ++j)
////                    {
////                        int childrenCount = 0;
////                        if (currentNodeParentList[j] == -1)
////                            childrenCount = rootChildrenCount;
////                        else
////                            childrenCount = newChildrenList[currentNodeParentList[j]].transform.childCount;
////
////                        if (childrenCount > parentIndexChildrenCount)
////                        {
////                            parentIndexWithMaxChildrenCount = currentNodeParentList[j];
////                            parentIndexChildrenCount = childrenCount;
////                        }
////                    }
////
////                    if (parentIndexWithMaxChildrenCount == -1)
////                        newGameObject.transform.SetParent(mergedImageGameObject.transform);
////                    else
////                        newGameObject.transform.SetParent(newChildrenList[parentIndexWithMaxChildrenCount].transform);
////
////                    if (parentIndexWithMaxChildrenCount != -1)
////                        rootChildrenCount--;
////                }
////
////                Rect imageRawRect = MergedImage.CalcNodeRawRect(mergedImageRect, imageInfo.mTexPos);
////                rectTransform.sizeDelta = imageRawRect.size;
////                Rect imageTempRect = GetNodeRect(newGameObject);
////
////                Vector2 delta = imageRawRect.position - imageTempRect.position;
////                rectTransform.anchoredPosition = rectTransform.anchoredPosition + delta;
////            }
////
////            if (mAutoDeleteMergedImageComponent)
////            {
////                DestroyImmediate(mergedImage);
////                DestroyImmediate(mergedImageGameObject.GetComponent<CanvasRenderer>());
////            }
////        }
////        catch (System.Exception e)
////        {
////            for (int i = newChildrenList.Count - 1; i >= 0; --i)
////            {
////                if (newChildrenList[i] != null)
////                {
////                    DestroyImmediate(newChildrenList[i]);
////                }
////            }
////
////            throw e;
////        }
////    }
////
////    #endregion
////}
