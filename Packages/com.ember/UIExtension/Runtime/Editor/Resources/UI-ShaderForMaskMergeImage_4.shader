
Shader "UI/ShaderForMaskMergeImage_4"
{
    Properties
    {
        [PerRendererData] [NoScaleOffset] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Visible("Visible", Float) = 1
        _Gray("Gray", Float) = 1

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
 
		_ColorMask ("Color Mask", Float) = 15

        [NoScaleOffset] _Texture_1("Texture_1", 2D) = "black" {}
        _TextureColor_1("TextureColor_1", Color) = (1, 1, 1, 1)
        _TextureVisible_1("TextureVisible_1", Float) = 1
        _TextureGray_1("TextureGray_1", Float) = 1


        [NoScaleOffset] _Texture_2("Texture_2", 2D) = "black" {}
        _TextureColor_2("TextureColor_2", Color) = (1, 1, 1, 1)
        _TextureVisible_2("TextureVisible_2", Float) = 1
        _TextureGray_2("TextureGray_2", Float) = 1


        [NoScaleOffset] _Texture_3("Texture_3", 2D) = "black" {}
        _TextureColor_3("TextureColor_3", Color) = (1, 1, 1, 1)
        _TextureVisible_3("TextureVisible_3", Float) = 1
        _TextureGray_3("TextureGray_3", Float) = 1


    }
 
	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
		
		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}
 
		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Fog { Mode Off }
		Blend One OneMinusSrcAlpha
		ColorMask [_ColorMask]
 
		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

            fixed GetUvClipping(fixed2 uv)
            {
                fixed outOfU = (uv.x >= 0) * (uv.x <= 1);
                fixed outOfV = (uv.y >= 0) * (uv.y <= 1);
                return outOfU * outOfV;
            }

            half2 TransformUv(half2 uv, half4 mat)
            {
                half3 xAsix = half3(mat.x + 1, 0, mat.z);
                half3 yAsix = half3(0, mat.y + 1, mat.w);

                half3 uvEx = half3(uv, 1);
                return half2(dot(uvEx, xAsix), dot(uvEx, yAsix));
            }

            fixed CalcMask(half alpha, fixed parentMask, fixed generateMask)
            {
                fixed a = alpha > 0.004;
                return dot(float3(a, 1, a), float3(1, generateMask, -generateMask)) * parentMask;
            }

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};
 
			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
				half2 rawTexcoord  : TEXCOORD1;
				float4 worldPosition : TEXCOORD2;
			};
            
            bool _UseClipRect;
            float4 _ClipRect;
            bool _UseAlphaClip;
            float4 _UvTransform;
            float4 _InvUvTransform;
            
			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.worldPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
                OUT.rawTexcoord = TransformUv(IN.texcoord, _InvUvTransform);
#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw-1.0)*float2(-1,1);
#endif
				OUT.color.a = IN.color.a;
				OUT.color.rgb = IN.color.rgb * IN.color.a;
				return OUT;
			}

            #pragma shader_feature _ENABLE_GRAY
            #pragma shader_feature _ENABLE_MASK
            sampler2D _MainTex;
            fixed4 _Color;
            float _Visible;
            float _Gray;
            float4 _MainTexturePos;
            float _InvGenerateMask;
            

            #pragma shader_feature _ENABLE_GRAY1
            #pragma shader_feature _ENABLE_MASK1
            sampler2D _Texture_1;
            fixed4 _TextureColor_1;
            float _TextureVisible_1;
            float _TextureGray_1;
            int _TextureParentIndex_1;
            float _InvTextureGenerateMask_1;
            float4 _TexturePos_1;
            float4 _TextureUvTransform_1;
            float4 _TextureInvUvTransform_1;

            #pragma shader_feature _ENABLE_GRAY2
            #pragma shader_feature _ENABLE_MASK2
            sampler2D _Texture_2;
            fixed4 _TextureColor_2;
            float _TextureVisible_2;
            float _TextureGray_2;
            int _TextureParentIndex_2;
            float _InvTextureGenerateMask_2;
            float4 _TexturePos_2;
            float4 _TextureUvTransform_2;
            float4 _TextureInvUvTransform_2;

            #pragma shader_feature _ENABLE_GRAY3
            #pragma shader_feature _ENABLE_MASK3
            sampler2D _Texture_3;
            fixed4 _TextureColor_3;
            float _TextureVisible_3;
            float _TextureGray_3;
            int _TextureParentIndex_3;
            float _InvTextureGenerateMask_3;
            float4 _TexturePos_3;
            float4 _TextureUvTransform_3;
            float4 _TextureInvUvTransform_3;

            fixed4 frag(v2f IN) : SV_TARGET
            {
                half invAlpha = 1;
                half3 color = half3(0, 0, 0);
                //1为不遮挡，0为遮挡
                fixed maskArray[8] = {1, 1, 1, 1, 1, 1, 1, 1};

                {
                    half2 uv = TransformUv(IN.rawTexcoord, _MainTexturePos);
                    half2 uvSample = TransformUv(uv, _UvTransform);
                    half4 color0 = tex2D(_MainTex, uvSample) * _Color * _Visible;
                    #ifdef _ENABLE_GRAY
                        half grayColor0 = dot(color0.rgb, half3(0.3, 0.59, 0.11));
                        color0.rgb = lerp(color0.rgb, half3(grayColor0, grayColor0, grayColor0), _Gray);
                    #endif
                    color0.a *= GetUvClipping(uv);
                    #ifdef _ENABLE_MASK
                        maskArray[0] = CalcMask(color0.a, maskArray[0], _InvGenerateMask);
                        color0.a *= maskArray[0];
                    #endif
                    invAlpha *= (1 - color0.a);
                    color = lerp(color, color0.rgb, color0.a);
                }

                {
                    half2 uv = TransformUv(IN.rawTexcoord, _TexturePos_1);
                    half2 uvSample = TransformUv(uv, _TextureUvTransform_1);
                    half4 color1 = tex2D(_Texture_1, uvSample) * _TextureColor_1 * _TextureVisible_1;
                    #ifdef _ENABLE_GRAY1
                        half grayColor1 = dot(color1.rgb, half3(0.3, 0.59, 0.11));
                        color1.rgb = lerp(color1.rgb, half3(grayColor1, grayColor1, grayColor1), _TextureGray_1);
                    #endif
                    color1.a *= GetUvClipping(uv);
                    #ifdef _ENABLE_MASK1
                        maskArray[1] = CalcMask(color1.a, maskArray[_TextureParentIndex_1], _InvTextureGenerateMask_1);
                        color1.a *= maskArray[1];
                    #endif
                    invAlpha *= (1 - color1.a);
                    color = lerp(color, color1.rgb, color1.a);
                }

                {
                    half2 uv = TransformUv(IN.rawTexcoord, _TexturePos_2);
                    half2 uvSample = TransformUv(uv, _TextureUvTransform_2);
                    half4 color2 = tex2D(_Texture_2, uvSample) * _TextureColor_2 * _TextureVisible_2;
                    #ifdef _ENABLE_GRAY2
                        half grayColor2 = dot(color2.rgb, half3(0.3, 0.59, 0.11));
                        color2.rgb = lerp(color2.rgb, half3(grayColor2, grayColor2, grayColor2), _TextureGray_2);
                    #endif
                    color2.a *= GetUvClipping(uv);
                    #ifdef _ENABLE_MASK2
                        maskArray[2] = CalcMask(color2.a, maskArray[_TextureParentIndex_2], _InvTextureGenerateMask_2);
                        color2.a *= maskArray[2];
                    #endif
                    invAlpha *= (1 - color2.a);
                    color = lerp(color, color2.rgb, color2.a);
                }

                {
                    half2 uv = TransformUv(IN.rawTexcoord, _TexturePos_3);
                    half2 uvSample = TransformUv(uv, _TextureUvTransform_3);
                    half4 color3 = tex2D(_Texture_3, uvSample) * _TextureColor_3 * _TextureVisible_3;
                    #ifdef _ENABLE_GRAY3
                        half grayColor3 = dot(color3.rgb, half3(0.3, 0.59, 0.11));
                        color3.rgb = lerp(color3.rgb, half3(grayColor3, grayColor3, grayColor3), _TextureGray_3);
                    #endif
                    color3.a *= GetUvClipping(uv);
                    #ifdef _ENABLE_MASK3
                        maskArray[3] = CalcMask(color3.a, maskArray[_TextureParentIndex_3], _InvTextureGenerateMask_3);
                        color3.a *= maskArray[3];
                    #endif
                    invAlpha *= (1 - color3.a);
                    color = lerp(color, color3.rgb, color3.a);
                }

                half alpha = 1 - invAlpha;

                alpha *= IN.color.a;
                color *= IN.color.rgb;

                if(_UseClipRect)
                    alpha *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                if(_UseAlphaClip)
                    clip(alpha - 0.004);
                
                return half4(color, alpha);
            }
        ENDCG
        }
    }
}
