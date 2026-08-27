Shader "Burner/UI/ShowTextInstance"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        [NoScaleOffset]
        _MainTex ("Main Texture", 2D) = "white" {}
        [NoScaleOffset]
        _TextTex ("Text Texture", 2D) = "black" {}
        [NoScaleOffset]
        _TextAniTex ("Text Ani Texture", 2D) = "black" {}
        _textArg ("_textArg", Vector) = (0,0,0,0)
        _textAniArg ("_textArg", Vector) = (0,0,0,0)
    }

    Category
    {
        Tags { "Queue"="Transparent+100" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }

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
                sampler2D _TextTex;
                sampler2D _TextAniTex;

                CBUFFER_START(UnityPerMaterial)
                    half4   _TintColor;
                    float4 _textArg;//x frame y unuse z uvStep w uvLineCharCount
                    float4 _textAniArg;//x (1/aniTexWidth) y (1/aniTexHeight)
                    
                    UNITY_INSTANCING_BUFFER_START(Props)
                        UNITY_DEFINE_INSTANCED_PROP(float4x4, _width)
                        UNITY_DEFINE_INSTANCED_PROP(float4x4, _pos)
                        UNITY_DEFINE_INSTANCED_PROP(float4x4, _code)
                        UNITY_DEFINE_INSTANCED_PROP(float4, _beginTime)//x time y beginFrame z endFrame
                    UNITY_INSTANCING_BUFFER_END(Props)
                CBUFFER_END

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                    float4 texcoord : TEXCOORD0;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                v2f vert(appdata_t v)
                {
                    v2f o = (v2f)0;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    float4x4 w = UNITY_ACCESS_INSTANCED_PROP(Props, _width);
                    float4x4 p = UNITY_ACCESS_INSTANCED_PROP(Props, _pos);
                    float4x4 c = UNITY_ACCESS_INSTANCED_PROP(Props, _code);
                    float4 timeValue = UNITY_ACCESS_INSTANCED_PROP(Props, _beginTime);

                    float time = _Time.y - timeValue.x;
                    int rx = ((int)v.texcoord.x) / 4;
                    int ry = ((int)v.texcoord.x) % 4;
                    int code = c[ry][rx];

                    //计算uv
                    float cx = code % _textArg.w;
                    float cy = code / _textArg.w;
                    float4 textUV = tex2Dlod(_TextTex, float4((int)v.texcoord.y / 2 + cx * 2, cy, 0, 0)  * _textArg.z);
                    float2 uv = lerp(textUV.xy, textUV.zw, (int)v.texcoord.y % 2);

                    //计算位置
                    int frame = time * _textArg.x;
                    float dt = min(time - frame / _textArg.x, 0);
                    frame += timeValue.y;
                    float vis = step(frame, timeValue.z);//当前帧的可见性

                    int anix = v.texcoord.x * 4 + v.texcoord.y;
                    int row = frame / 16;
                    int col = frame % 16;
                    int newRow = (frame + 1) / 16;
                    int newCol = (frame + 1) % 16;
                    float4 p0 = tex2Dlod(_TextAniTex, float4(anix + col * 64, row, 0, 0) * _textAniArg);
                    float4 p1 = tex2Dlod(_TextAniTex, float4(anix + newCol * 64, newRow, 0, 0) * _textAniArg);
                    float4 tp = (p0 + (p1 - p0) * dt);
                    v.vertex.xyz = tp.xyz;
                    v.vertex.x = v.vertex.x * w[ry][rx] + p[ry][rx];

                    o.vertex = TransformObjectToHClip(v.vertex.xyz) * vis;
                    o.texcoord.xy = uv * float2(1, -1); // -1: because y position is opposite to texture packer
                    o.texcoord.z = tp.a;

                    return o;
                }

                half4 frag (v2f i) : SV_Target
                {
                    half4 col = _TintColor * tex2D(_MainTex, i.texcoord.xy);
                    col.a *= i.texcoord.z;
                    return col;
                }
            ENDHLSL
            }
        }
    }
}
