Shader "Ember/UI/Backdrop"
{
    Properties
    {
        [PerRendererData] _MainTex("Backdrop", 2D) = "white" {}
        _HasBackdrop("Has Backdrop", Float) = 0
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float4 uv:TEXCOORD0; float4 shape:TEXCOORD1; float4 data:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float4 uv:TEXCOORD0; float4 shape:TEXCOORD1; float4 screen:TEXCOORD2; float4 mask:TEXCOORD3; float4 optics:TEXCOORD4; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            float _HasBackdrop;
            float4 _ClipRect;
            float _UIMaskSoftnessX, _UIMaskSoftnessY;
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex=UnityObjectToClipPos(v.vertex); o.screen=ComputeScreenPos(o.vertex);
                o.color=v.color; o.uv=v.uv; o.shape=v.shape; o.optics=v.data;
                float2 pixelSize=o.vertex.w / abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 cr=clamp(_ClipRect,-2e10,2e10);
                o.mask=float4(v.vertex.xy*2-cr.xy-cr.zw, .25/(.25*float2(_UIMaskSoftnessX,_UIMaskSoftnessY)+abs(pixelSize)));
                return o;
            }
            half4 frag(v2f i):SV_Target
            {
                float2 halfSize=i.uv.zw*.5;
                float radius=(i.optics.w>.5 && i.uv.y>.5)?0:i.shape.x;
                float2 q=abs((i.uv.xy-.5)*i.uv.zw)-halfSize+radius;
                float d=length(max(q,0))+min(max(q.x,q.y),0)-radius;
                float aa=max(fwidth(d),.001);
                float coverage=saturate(.5-d/aa);
                float edge=1-saturate((-d-max(i.shape.z,aa))/aa);
                float2 uv=i.screen.xy/i.screen.w;
                half3 bg=tex2D(_MainTex,uv).rgb;
                // A broad frosted wash and a soft inner bevel give glass volume at every size.
                half luminance=dot(bg,half3(.2126,.7152,.0722));
                bg=lerp(luminance.xxx,bg,1.12);
                half3 surface=lerp(bg,i.color.rgb,i.shape.y);
                surface=lerp(i.color.rgb,surface,i.optics.x);
                float light=saturate(.5+.32*i.uv.y-.22*i.uv.x);
                surface=lerp(surface,half3(.78,.9,1),i.optics.z*(.6+.4*light));
                float bevel=exp2(min(d,0)/max(2,min(i.uv.z,i.uv.w)*.07));
                float sheen=pow(saturate(1-i.uv.y),6)*.08+pow(saturate(i.uv.y),3)*.22;
                surface+=i.optics.y*(bevel*(.12+.3*light)+sheen)*half3(.7,.86,1);
                surface+=edge*i.shape.w*(.35+.65*light);
                half alpha=coverage*i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                half2 m=saturate((_ClipRect.zw-_ClipRect.xy-abs(i.mask.xy))*i.mask.zw); alpha*=m.x*m.y;
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha-.001);
                #endif
                return half4(surface,alpha);
            }
            ENDHLSL
        }
    }
}
