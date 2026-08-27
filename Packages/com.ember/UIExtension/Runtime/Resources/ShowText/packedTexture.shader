Shader "Burner/UI/PackedTextureInstance"
{
    Properties
    {
        [NoScaleOffset]
        _MainTex("Main Texture", 2D) = "white" {}
    }

    Category
    {
        Tags { "Queue"="Transparent+100" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "DisableBatching"="True"}

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        Lighting Off
        ZWrite Off
        ZTest Off

        SubShader
        {
            Pass
            {
            HLSLPROGRAM
                #pragma multi_compile_instancing
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 3.0

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                sampler2D _MainTex;

                CBUFFER_START(UnityPerMaterial)
                    UNITY_INSTANCING_BUFFER_START(Props)
                    UNITY_DEFINE_INSTANCED_PROP(half4, _uv_st)
                    UNITY_DEFINE_INSTANCED_PROP(half4, _trisection_offset)
                    UNITY_DEFINE_INSTANCED_PROP(half4, _trisection_uv)
                    UNITY_DEFINE_INSTANCED_PROP(half, _trisection_enabled)
                    UNITY_INSTANCING_BUFFER_END(Props)
                CBUFFER_END

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float3 texcoord : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                v2f vert (appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    half4 uv_st = UNITY_ACCESS_INSTANCED_PROP(Props, _uv_st);

                    half trisection_enabled = UNITY_ACCESS_INSTANCED_PROP(Props, _trisection_enabled);
                    if (trisection_enabled > 0.5f) // == 1.0f
                    {
                        half4 trisection_offset = UNITY_ACCESS_INSTANCED_PROP(Props, _trisection_offset);
                        half4 trisection_uv = UNITY_ACCESS_INSTANCED_PROP(Props, _trisection_uv);
                        v.vertex.x = -0.5 + ( v.vertex.x + 0.5) * trisection_offset[v.texcoord.z];
                        v.texcoord.x *= trisection_uv[v.texcoord.z];
                    }

                    o.vertex = TransformObjectToHClip(v.vertex.xyz);
                    v.texcoord.y = 1 - v.texcoord.y;
                    o.texcoord = v.texcoord.xy * uv_st.xy + uv_st.zw;
                    o.texcoord.y = 1 - o.texcoord.y;

                    return o;
                }

                half4 frag (v2f i) : SV_Target
                {
                    return tex2D(_MainTex, i.texcoord.xy);
                }
            ENDHLSL
            }
        }
    }
}
