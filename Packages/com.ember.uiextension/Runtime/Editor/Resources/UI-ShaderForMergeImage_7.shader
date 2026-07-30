
Shader "UI/ShaderForMergeImage_7"
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


        [NoScaleOffset] _Texture_4("Texture_4", 2D) = "black" {}
        _TextureColor_4("TextureColor_4", Color) = (1, 1, 1, 1)
        _TextureVisible_4("TextureVisible_4", Float) = 1
        _TextureGray_4("TextureGray_4", Float) = 1


        [NoScaleOffset] _Texture_5("Texture_5", 2D) = "black" {}
        _TextureColor_5("TextureColor_5", Color) = (1, 1, 1, 1)
        _TextureVisible_5("TextureVisible_5", Float) = 1
        _TextureGray_5("TextureGray_5", Float) = 1


        [NoScaleOffset] _Texture_6("Texture_6", 2D) = "black" {}
        _TextureColor_6("TextureColor_6", Color) = (1, 1, 1, 1)
        _TextureVisible_6("TextureVisible_6", Float) = 1
        _TextureGray_6("TextureGray_6", Float) = 1


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
                fixed outOfU = uv.x <= 0 || uv.x >= 1;
                fixed outOfV = uv.y <= 0 || uv.y >= 1;
                return 1 - max(outOfU, outOfV);
            }

            half2 TransformUv(half2 uv, half4 mat)
            {
                half3 xAsix = half3(mat.x + 1, 0, mat.z);
                half3 yAsix = half3(0, mat.y + 1, mat.w);

                half3 uvEx = half3(uv, 1);
                return half2(dot(uvEx, xAsix), dot(uvEx, yAsix));
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
            sampler2D _MainTex;
            fixed4 _Color;
            float _Visible;
            float _Gray;
            float4 _MainTexturePos;

            #pragma shader_feature _ENABLE_GRAY1
            sampler2D _Texture_1;
            fixed4 _TextureColor_1;
            float _TextureVisible_1;
            float _TextureGray_1;
            float4 _TexturePos_1;
            float4 _TextureUvTransform_1;
            float4 _TextureInvUvTransform_1;

            #pragma shader_feature _ENABLE_GRAY2
            sampler2D _Texture_2;
            fixed4 _TextureColor_2;
            float _TextureVisible_2;
            float _TextureGray_2;
            float4 _TexturePos_2;
            float4 _TextureUvTransform_2;
            float4 _TextureInvUvTransform_2;

            #pragma shader_feature _ENABLE_GRAY3
            sampler2D _Texture_3;
            fixed4 _TextureColor_3;
            float _TextureVisible_3;
            float _TextureGray_3;
            float4 _TexturePos_3;
            float4 _TextureUvTransform_3;
            float4 _TextureInvUvTransform_3;

            #pragma shader_feature _ENABLE_GRAY4
            sampler2D _Texture_4;
            fixed4 _TextureColor_4;
            float _TextureVisible_4;
            float _TextureGray_4;
            float4 _TexturePos_4;
            float4 _TextureUvTransform_4;
            float4 _TextureInvUvTransform_4;

            #pragma shader_feature _ENABLE_GRAY5
            sampler2D _Texture_5;
            fixed4 _TextureColor_5;
            float _TextureVisible_5;
            float _TextureGray_5;
            float4 _TexturePos_5;
            float4 _TextureUvTransform_5;
            float4 _TextureInvUvTransform_5;

            #pragma shader_feature _ENABLE_GRAY6
            sampler2D _Texture_6;
            fixed4 _TextureColor_6;
            float _TextureVisible_6;
            float _TextureGray_6;
            float4 _TexturePos_6;
            float4 _TextureUvTransform_6;
            float4 _TextureInvUvTransform_6;

            fixed4 frag(v2f IN) : SV_TARGET
            {
                half invAlpha = 1;
                half3 color = half3(0, 0, 0);

                {
                    half2 uv = TransformUv(IN.rawTexcoord, _MainTexturePos);
                    half2 uvSample = TransformUv(uv, _UvTransform);
                    half4 color0 = tex2D(_MainTex, uvSample) * _Color * _Visible;
                    #ifdef _ENABLE_GRAY
                        half grayColor0 = dot(color0.rgb, half3(0.3, 0.59, 0.11));
                        color0.rgb = lerp(color0.rgb, half3(grayColor0, grayColor0, grayColor0), _Gray);
                    #endif
                    color0.a *= GetUvClipping(uv);
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
                    invAlpha *= (1 - color3.a);
                    color = lerp(color, color3.rgb, color3.a);
                }

                {
                    half2 uv = TransformUv(IN.rawTexcoord, _TexturePos_4);
                    half2 uvSample = TransformUv(uv, _TextureUvTransform_4);
                    half4 color4 = tex2D(_Texture_4, uvSample) * _TextureColor_4 * _TextureVisible_4;
                    #ifdef _ENABLE_GRAY4
                        half grayColor4 = dot(color4.rgb, half3(0.3, 0.59, 0.11));
                        color4.rgb = lerp(color4.rgb, half3(grayColor4, grayColor4, grayColor4), _TextureGray_4);
                    #endif
                    color4.a *= GetUvClipping(uv);
                    invAlpha *= (1 - color4.a);
                    color = lerp(color, color4.rgb, color4.a);
                }

                {
                    half2 uv = TransformUv(IN.rawTexcoord, _TexturePos_5);
                    half2 uvSample = TransformUv(uv, _TextureUvTransform_5);
                    half4 color5 = tex2D(_Texture_5, uvSample) * _TextureColor_5 * _TextureVisible_5;
                    #ifdef _ENABLE_GRAY5
                        half grayColor5 = dot(color5.rgb, half3(0.3, 0.59, 0.11));
                        color5.rgb = lerp(color5.rgb, half3(grayColor5, grayColor5, grayColor5), _TextureGray_5);
                    #endif
                    color5.a *= GetUvClipping(uv);
                    invAlpha *= (1 - color5.a);
                    color = lerp(color, color5.rgb, color5.a);
                }

                {
                    half2 uv = TransformUv(IN.rawTexcoord, _TexturePos_6);
                    half2 uvSample = TransformUv(uv, _TextureUvTransform_6);
                    half4 color6 = tex2D(_Texture_6, uvSample) * _TextureColor_6 * _TextureVisible_6;
                    #ifdef _ENABLE_GRAY6
                        half grayColor6 = dot(color6.rgb, half3(0.3, 0.59, 0.11));
                        color6.rgb = lerp(color6.rgb, half3(grayColor6, grayColor6, grayColor6), _TextureGray_6);
                    #endif
                    color6.a *= GetUvClipping(uv);
                    invAlpha *= (1 - color6.a);
                    color = lerp(color, color6.rgb, color6.a);
                }

                half alpha = 1 - invAlpha;

                alpha *= IN.color.a;
                color *= IN.color.rgb;

                if(_UseClipRect)
                    alpha *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                if(_UseAlphaClip)
                    clip(alpha - 0.001);
                
                return half4(color, alpha);
            }
        ENDCG
        }
    }
}
