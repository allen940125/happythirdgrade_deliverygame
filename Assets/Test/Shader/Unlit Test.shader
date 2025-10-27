Shader "Custom/LODExample"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        // 高品質版本（有簡單的 directional "fake" lighting）
        LOD 600
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "HighQuality"
            HLSLPROGRAM
            #pragma vertex VertHQ
            #pragma fragment FragHQ
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings VertHQ(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);

                // 計算世界空間法線（將 Object->World 矩陣前三行乘以法線）
                float3x3 objToWorld3 = (float3x3)unity_ObjectToWorld;
                OUT.normalWS = normalize(mul(objToWorld3, IN.normalOS));

                return OUT;
            }

            half4 FragHQ(Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // 假 directional light（固定方向，可替換）
                float3 lightDir = normalize(float3(0.5, 0.8, 0.3));
                float ndotl = saturate(dot(IN.normalWS, lightDir));

                // 合成：環境 + 主要光源
                float3 color = albedo.rgb * (0.2 + 0.8 * ndotl);

                return half4(color, albedo.a);
            }
            ENDHLSL
        }
    }

    SubShader
    {
        // 中等品質版本（用 UV 做簡易明暗/高度感）
        LOD 300
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "MidQuality"
            HLSLPROGRAM
            #pragma vertex VertMid
            #pragma fragment FragMid
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings VertMid(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 FragMid(Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // 以 UV.y 作為 "簡易陰影"（只是示範）
                float shade = lerp(0.9, 0.5, IN.uv.y);
                float3 color = albedo.rgb * shade;

                return half4(color, albedo.a);
            }
            ENDHLSL
        }
    }

    SubShader
    {
        // 低品質版本（Unlit）
        LOD 100
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "LowQuality"
            HLSLPROGRAM
            #pragma vertex VertLow
            #pragma fragment FragLow
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings VertLow(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 FragLow(Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                // 完全 unlit
                return albedo;
            }
            ENDHLSL
        }
    }

    // 一般情況下加一個 FallBack（可選）
    Fallback Off
}
