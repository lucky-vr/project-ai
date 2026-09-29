Shader "VillageDraft/WaterSurface"
{
    Properties
    {
        _DeepColor ("Deep Water", Color) = (0.045, 0.24, 0.31, 1)
        _ShallowColor ("Shallow Water", Color) = (0.18, 0.45, 0.50, 1)
        _RippleColor ("Ripple Highlight", Color) = (0.45, 0.66, 0.65, 1)
        _RippleScale ("Ripple Scale", Float) = 1.4
        _RippleSpeed ("Ripple Speed", Float) = 0.58
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                half4 _RippleColor;
                float _RippleScale;
                float _RippleSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 p = input.positionWS.xz * _RippleScale;
                float t = _Time.y * _RippleSpeed;
                float bend = sin(p.y * 0.37 + t * 0.42) * 0.52;
                half ripple = saturate(0.50h + sin(p.x * 0.57 + p.y * 0.24 - t + bend) * 0.29h
                    + sin(p.y * 0.81 - p.x * 0.19 + t * 0.65) * 0.18h);
                half3 normal = normalize(input.normalWS);
                half3 view = normalize(_WorldSpaceCameraPos.xyz - input.positionWS);
                half grazing = pow(1.0h - saturate(dot(normal, view)), 2.0h);
                half3 water = lerp(_DeepColor.rgb, _ShallowColor.rgb, ripple * 0.45h + 0.22h);
                half crest = smoothstep(0.76h, 0.95h, ripple) * 0.16h;
                water = lerp(water, _RippleColor.rgb, crest + grazing * 0.10h);
                return half4(water, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
