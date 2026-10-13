Shader "Custom/Local_ColorFill_NPR_URP"
{
    Properties
    {
        [Header(Textures)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0

        [Header(NPR Lighting)]
        _ShadowColor ("Shadow Color", Color) = (0.5, 0.5, 0.5, 1)
        _ShadowThreshold ("Shadow Threshold", Range(-1, 1)) = 0.0
        _ShadowSmoothness ("Shadow Smoothness", Range(0, 0.5)) = 0.05
        
        [Header(Fill Control)]
        // 提供材質面板下拉選單
        [KeywordEnum(Overwrite, Multiply, Add, AlphaBlend)] _FillMode("Fill Blend Mode", Float) = 0
        
        _FillColor ("Fill Color (A for AlphaBlend Mode)", Color) = (1,0,0,1)
        _FillAmount ("Fill Amount", Range(0, 1)) = 0.0
        _FillDirection ("Fill Direction (Local)", Vector) = (1, 0, 0, 0) 
        _MinBound ("Min Bound (Along Direction)", Float) = -0.5
        _MaxBound ("Max Bound (Along Direction)", Float) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // 定義 Local Shader Features (避免占用全域 Keyword 數量)
            #pragma shader_feature_local _FILLMODE_OVERWRITE _FILLMODE_MULTIPLY _FILLMODE_ADD _FILLMODE_ALPHABLEND
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes 
            { 
                float4 positionOS : POSITION; 
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings 
            { 
                float4 positionCS : SV_POSITION; 
                float3 positionOS : TEXCOORD0; 
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowColor;
                half _ShadowThreshold;
                half _ShadowSmoothness;
                half _BumpScale;
                
                half4 _FillColor;
                float _FillAmount;
                float4 _FillDirection;
                float _MinBound;
                float _MaxBound;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. BaseMap & Color
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // 2. Normal Mapping
                half4 normalSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);
                float3 normalTS = UnpackNormalScale(normalSample, _BumpScale);
                float3x3 tbn = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 normalWS = normalize(mul(normalTS, tbn));

                // // 3. NPR Cel-Shading Lighting
                // Light mainLight = GetMainLight();
                // float NdotL = dot(normalWS, mainLight.direction);
                // float lightIntensity = smoothstep(_ShadowThreshold - _ShadowSmoothness, _ShadowThreshold + _ShadowSmoothness, NdotL);
                // half3 litColor = lerp(albedo.rgb * _ShadowColor.rgb, albedo.rgb * mainLight.color, lightIntensity);

                // 3. NPR Cel-Shading Lighting
                Light mainLight = GetMainLight();
                float NdotL = dot(normalWS, mainLight.direction);

                // 二階陰影切分
                float lightIntensity = smoothstep(_ShadowThreshold - 0.01, _ShadowThreshold + 0.01, NdotL);

                // 【關鍵修改】：完全放棄 BaseMap 的亮度，直接使用 _ShadowColor 作為陰影
                half3 shadowAreaColor = _ShadowColor.rgb; 

                // 受光區保留 BaseMap 原色，陰影區強制變成 _ShadowColor (設為白色就是純白)
                half3 litColor = lerp(shadowAreaColor, albedo.rgb * mainLight.color, lightIntensity);
                
                // 4. Fill Mask Calculation
                float3 dir = normalize(_FillDirection.xyz);
                float projectedVal = dot(input.positionOS, dir);
                float normalizedVal = saturate((projectedVal - _MinBound) / (_MaxBound - _MinBound));
                float fillMask = step(normalizedVal, _FillAmount);

                // 5. Fill Color Blending (依據 Shader Feature 在編譯期決定路徑)
                half3 finalColor = litColor;

                #if defined(_FILLMODE_MULTIPLY)
                    // 正片疊底：適合做燒焦、毒液侵蝕、血跡疊加，保留底圖細節與明暗
                    finalColor = lerp(litColor, litColor * _FillColor.rgb, fillMask);
                #elif defined(_FILLMODE_ADD)
                    // 線性發光：適合做能量充能、過載發光、護盾效果
                    finalColor = lerp(litColor, litColor + _FillColor.rgb, fillMask);
                #elif defined(_FILLMODE_ALPHABLEND)
                    // 透明度混合：使用 _FillColor 的 Alpha 通道決定疊加濃度
                    half3 blendColor = lerp(litColor, _FillColor.rgb, _FillColor.a);
                    finalColor = lerp(litColor, blendColor, fillMask);
                #else
                    // 預設覆蓋 (Overwrite)：完全無視底圖光影，直接取代為純色
                    finalColor = lerp(litColor, _FillColor.rgb, fillMask);
                #endif

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
}