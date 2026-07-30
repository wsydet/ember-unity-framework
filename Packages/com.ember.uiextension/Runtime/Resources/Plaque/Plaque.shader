Shader "Burner/Plaque"
{
	Properties
	{
		[NoScaleOffset]
		_MainTex ("Base (RGB)", 2D) = "white" {}
		[NoScaleOffset]
		_BGTex ("Background ", 2D) = "white" {}
		_Color ("Tint", Color) = (1, 1, 1, 1)
	}

	SubShader
	{
		Tags
		{
			"Queue" 			= "Transparent"
			"IgnoreProjector" 	= "True"
			"RenderType" 		= "Transparent"
		}

        Lighting Off Cull Off ZTest Always ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
		
		Pass
		{
		HLSLPROGRAM
			#pragma target 3.0
			#pragma multi_compile_instancing
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			sampler2D _MainTex;

			sampler2D _BGTex;


			    CBUFFER_START(UnityPerMaterial)
                    UNITY_INSTANCING_BUFFER_START(Props)
                    UNITY_DEFINE_INSTANCED_PROP(float, _Flag)
                    UNITY_DEFINE_INSTANCED_PROP(float4, _PositionXY)
                    UNITY_DEFINE_INSTANCED_PROP(float, _PositionZ)
                    UNITY_DEFINE_INSTANCED_PROP(float4, _TopUV)
                    UNITY_DEFINE_INSTANCED_PROP(float4, _BottomUV)
					UNITY_DEFINE_INSTANCED_PROP(float4, _Colors)
                    UNITY_INSTANCING_BUFFER_END(Props)
                CBUFFER_END

			struct appdata_base
			{
			    float4 vertex : POSITION;
			    float3 normal : NORMAL;
			    float4 texcoord : TEXCOORD0;

			    UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				half2 uv 	: TEXCOORD0;
				float4 pos	: SV_POSITION;
				half4 color	: COLOR;

			    UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			v2f vert(appdata_base v)
			{
				v2f o = (v2f)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				half flag = UNITY_ACCESS_INSTANCED_PROP(Props, _Flag);

				float4 posH = UNITY_ACCESS_INSTANCED_PROP(Props, _PositionXY);
				float4 topUV = UNITY_ACCESS_INSTANCED_PROP(Props, _TopUV);

				float4 vertex = v.vertex;
				vertex.xy = posH.xy + v.vertex.xy * posH.zw;
				vertex.z = UNITY_ACCESS_INSTANCED_PROP(Props, _PositionZ);

				if (flag < 0.5) // flag = 0, it's text
				{
					float4 bottomUV = UNITY_ACCESS_INSTANCED_PROP(Props, _BottomUV);

					float4 uv = lerp(bottomUV, topUV, v.texcoord.y);
					o.uv = lerp(uv.xy, uv.zw, v.texcoord.x);
				}
				else // flag = 1, it's background
				{
					o.uv = v.texcoord.xy;
					o.uv.y = 1.0 - o.uv.y;
					o.uv = topUV.zw + o.uv.xy * topUV.xy;
					o.uv.y = 1.0 - o.uv.y;
				}

				o.pos = TransformObjectToHClip(vertex);
				o.color = UNITY_ACCESS_INSTANCED_PROP(Props, _Colors);
				
				return o;
			}

			half4 frag (v2f i) : SV_TARGET
			{
				half4 color = 0;

				UNITY_SETUP_INSTANCE_ID(i);
				half flag = UNITY_ACCESS_INSTANCED_PROP(Props, _Flag);
				if (flag < 0.5) // flag = 0, it's text
				{
					color = i.color;
					color.a *= tex2D(_MainTex, i.uv).a;
				}
				else // flag = 1, it's background
				{
					color = tex2D(_BGTex, i.uv);
				}
				
				return color;
			}
		ENDHLSL
		}
	}
}