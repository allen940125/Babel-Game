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
        // ★ 1. 新增 ReplaceBase (替換底色，保留光影) 模式
        [KeywordEnum(Overwrite, Multiply, Add, AlphaBlend, ReplaceBase)] _FillMode("Fill Blend Mode", Float) = 0
        
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
            
            // ★ 2. 註冊新的 Keyword
            #pragma shader_feature_local _FILLMODE_OVERWRITE _FILLMODE_MULTIPLY _FILLMODE_ADD _FILLMODE_ALPHABLEND _FILLMODE_REPLACEBASE
            
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
                // ★ 1. 【拆解動作】：只取樣最原始的貼圖 (不乘上任何顏色)
                half4 rawTexture = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                // 2. 提前計算 Fill Mask
                float3 dir = normalize(_FillDirection.xyz);
                float projectedVal = dot(input.positionOS, dir);
                float normalizedVal = saturate((projectedVal - _MinBound) / (_MaxBound - _MinBound));
                float fillMask = step(normalizedVal, _FillAmount);

                // ==========================================
                // ★ 3. 核心邏輯：決定「底色」到底是誰？
                // ==========================================
                half3 activeBaseColor = _BaseColor.rgb; // 預設使用原本的 BaseColor

                #if defined(_FILLMODE_REPLACEBASE)
                    // 如果開啟了 ReplaceBase，在這個區域內，BaseColor 的身分直接被 _FillColor 搶走！
                    activeBaseColor = lerp(_BaseColor.rgb, _FillColor.rgb, fillMask);
                #endif

                // ★ 4. 結算最終的 Albedo (原始貼圖 * 當下掌權的底色)
                half4 albedo = half4(rawTexture.rgb * activeBaseColor, rawTexture.a * _BaseColor.a);


                // 5. Normal Mapping (法線運算保持不變)
                half4 normalSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);
                float3 normalTS = UnpackNormalScale(normalSample, _BumpScale);
                float3x3 tbn = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 normalWS = normalize(mul(normalTS, tbn));

                // 6. NPR 賽璐璐陰影計算
                Light mainLight = GetMainLight();
                float NdotL = dot(normalWS, mainLight.direction);
                float lightIntensity = smoothstep(_ShadowThreshold - 0.01, _ShadowThreshold + 0.01, NdotL);
                
                // 陰影顏色與目前的 albedo 完美融合
                half3 shadowAreaColor = albedo.rgb * _ShadowColor.rgb; 
                half3 litColor = lerp(shadowAreaColor, albedo.rgb * mainLight.color, lightIntensity);
                
                // 7. 後期混合 (處理 ReplaceBase 以外的其他模式)
                half3 finalColor = litColor;

                #if defined(_FILLMODE_MULTIPLY)
                    finalColor = lerp(litColor, litColor * _FillColor.rgb, fillMask);
                #elif defined(_FILLMODE_ADD)
                    finalColor = lerp(litColor, litColor + _FillColor.rgb, fillMask);
                #elif defined(_FILLMODE_ALPHABLEND)
                    half3 blendColor = lerp(litColor, _FillColor.rgb, _FillColor.a);
                    finalColor = lerp(litColor, blendColor, fillMask);
                #elif defined(_FILLMODE_OVERWRITE)
                    finalColor = lerp(litColor, _FillColor.rgb, fillMask);
                #endif
                
                // 注意：_FILLMODE_REPLACEBASE 在步驟 3 就已經處理完了，
                // 所以它來到這裡時，litColor 已經是完美的狀態，直接通過。

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
}