Shader "Hidden/Ember/BackdropBlur"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        float _Radius;
        half4 Blur(Varyings input, float2 axis)
        {
            float2 uv=input.texcoord;
            float2 stepUV=axis*_BlitTexture_TexelSize.xy*_Radius;
            half4 c=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv)*.2041637;
            c+=(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+stepUV)+SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv-stepUV))*.1801738;
            c+=(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+stepUV*2)+SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv-stepUV*2))*.1238315;
            c+=(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+stepUV*3)+SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv-stepUV*3))*.0662822;
            c+=(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+stepUV*4)+SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv-stepUV*4))*.02763055;
            return c;
        }
        half4 Horizontal(Varyings input):SV_Target { return Blur(input,float2(1,0)); }
        half4 Vertical(Varyings input):SV_Target { return Blur(input,float2(0,1)); }
        ENDHLSL
        Pass { HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Horizontal
            ENDHLSL }
        Pass { HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Vertical
            ENDHLSL }
    }
}
