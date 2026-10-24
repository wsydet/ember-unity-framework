Shader "Game/Player/Whitebox Grid"
{
    Properties
    {
        _Background ("Background", Color) = (0.025, 0.045, 0.065, 1)
        _MinorColor ("One-unit lines", Color) = (0.085, 0.135, 0.165, 1)
        _MajorColor ("Ten-unit lines", Color) = (0.17, 0.29, 0.34, 1)
        _AxisColor ("World origin axes", Color) = (0.21, 0.43, 0.48, 1)
        _TextColor ("World coordinate labels", Color) = (0.34, 0.52, 0.57, 1)
        _LabelHeight ("Label height (world units)", Range(0.15, 0.8)) = 0.32
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Background" }
        Pass
        {
            Name "WhiteboxGrid"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Background, _MinorColor, _MajorColor, _AxisColor, _TextColor;
                float _LabelHeight;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 world : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(world);
                output.world = world.xy;
                return output;
            }

            // Screen-space line widths: world coordinates remain fixed while the camera moves.
            float GridLine(float2 world, float spacing, float2 pixel, float width)
            {
                float2 distance = abs(frac(world / spacing + 0.5) - 0.5) * spacing / pixel;
                return 1.0 - smoothstep(width * 0.5 - 0.5, width * 0.5 + 0.5, min(distance.x, distance.y));
            }

            // Tiny 3x5 technical numerals. No font texture, text objects, or per-label draw calls.
            uint Glyph(int symbol)
            {
                if (symbol == 0) return 31599u;
                if (symbol == 1) return 11415u;
                if (symbol == 2) return 29671u;
                if (symbol == 3) return 29647u;
                if (symbol == 4) return 23497u;
                if (symbol == 5) return 31183u;
                if (symbol == 6) return 31215u;
                if (symbol == 7) return 29257u;
                if (symbol == 8) return 31727u;
                if (symbol == 9) return 31695u;
                if (symbol == 10) return 448u;   // minus
                if (symbol == 11) return 23213u; // X
                if (symbol == 12) return 23186u; // Y
                return 0u;
            }

            float Coordinate(float2 uv, float value, int axis, float2 pixel)
            {
                // Seven cells: axis, space, sign, four digits. Leading zeroes are suppressed.
                if (uv.x < 0 || uv.x >= 5.6 || uv.y < 0 || uv.y >= 1) return 0;
                int column = (int)floor(uv.x / 0.8);
                int absolute = (int)round(abs(value));
                int symbol = -1;
                if (column == 0) symbol = axis;
                if (column == 2 && value < -0.5) symbol = 10;
                int divisor = column == 3 ? 1000 : column == 4 ? 100 : column == 5 ? 10 : 1;
                if (column >= 3 && (absolute >= divisor || column == 6)) symbol = (absolute / divisor) % 10;
                float2 cell = float2(frac(uv.x / 0.8) * 4, (1 - uv.y) * 5);
                int2 bit = (int2)floor(cell);
                if (bit.x > 2 || bit.y > 4) return 0;
                uint mask = Glyph(symbol);
                float filled = (float)((mask >> (uint)((4 - bit.y) * 3 + (2 - bit.x))) & 1u);
                float2 edge = min(frac(cell), 1 - frac(cell));
                float2 aa = max(pixel * float2(5, 5), 0.001);
                return filled * saturate(min(edge.x / aa.x, edge.y / aa.y));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 world = input.world;
                float2 pixel = max(fwidth(world), 0.00001);
                float footprint = max(pixel.x, pixel.y);
                float minor = GridLine(world, 1, pixel, 1) * saturate((1 / footprint - 3) / 5);
                float major = GridLine(world, 10, pixel, 1.7);
                float axis = 1 - smoothstep(0.5, 2.0, min(abs(world.x) / pixel.x, abs(world.y) / pixel.y));
                half3 color = lerp(_Background.rgb, _MinorColor.rgb, minor);
                color = lerp(color, _MajorColor.rgb, major);
                color = lerp(color, _AxisColor.rgb, axis);

                float2 origin = floor(world / 10) * 10;
                float2 local = (world - origin - float2(0.22, 0.22)) / _LabelHeight;
                float textFade = saturate((_LabelHeight / footprint - 6) / 6);
                float text = 0;
                [branch] if (textFade > 0 && local.x < 5.6 && local.y < 2.4)
                {
                    text = max(Coordinate(local - float2(0, 1.4), origin.x, 11, pixel / _LabelHeight),
                               Coordinate(local, origin.y, 12, pixel / _LabelHeight));
                }
                return half4(lerp(color, _TextColor.rgb, text * textFade), 1);
            }
            ENDHLSL
        }
    }
}
