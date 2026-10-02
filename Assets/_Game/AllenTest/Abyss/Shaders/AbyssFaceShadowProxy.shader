Shader "Abyss/Utility/FaceShadowProxy"
{
    Properties
    {
        [Main(Main, _, off)] _group_Main ("Shadow Proxy Settings", Float) = 0
        [Sub(Main)] [HDR] _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        
        [Header(Mode 1  Depth Clip (Hair Shadow))]
        [Sub(Main)] _DepthClip ("Depth Clip Distance", Range(0, 1)) = 0.5
        
        [Header(Mode 2  Vertex Offset (Eye Shadow))]
        [Toggle(_USE_EYE_SHADOW)] _UseEyeShadow ("Enable Eye Shadow Mode", Float) = 0
        [Sub(Main)] [NoScaleOffset] _ShadowMask ("Eye Shadow Mask", 2D) = "white" {}
        [Sub(Main)] _EyeShadowOffset ("Camera Push Offset", Range(0, 0.5)) = 0.05
    }
    SubShader
    {
        // 定義在 Transparent 階段，但比一般半透明物件早一點渲染 (2999)
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-1" }
        LOD 100

        Pass
        {
            Name "ShadowProxy"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local _USE_EYE_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenUV : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            TEXTURE2D(_ShadowMask);
            SAMPLER(sampler_ShadowMask);

            // 這是獨立的 Transparent 物件，擁有自己專屬的 CBUFFER 是正確的
            CBUFFER_START(UnityPerMaterial)
                half4 _ShadowColor;
                float _DepthClip;
                float _EyeShadowOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                #if defined(_USE_EYE_SHADOW)
                    // 眼窩陰影模式：將頂點往攝影機方向推近，避免與臉部模型產生 Z-Fighting
                    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                    float3 offsetDir = normalize(GetCameraPositionWS() - positionWS);
                    float3 offsetPositionWS = positionWS + offsetDir * _EyeShadowOffset;
                    output.positionCS = TransformWorldToHClip(offsetPositionWS);
                    output.uv = input.uv;
                #else
                    // 瀏海陰影模式：正常投影
                    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                    output.uv = input.uv;
                #endif
                
                output.screenUV = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                #if defined(_USE_EYE_SHADOW)
                    // 眼窩陰影直接讀取 Mask，並乘上透明度
                    half mask = SAMPLE_TEXTURE2D(_ShadowMask, sampler_ShadowMask, input.uv).r;
                    half finalAlpha = saturate(mask * _ShadowColor.a);
                    return half4(_ShadowColor.rgb, finalAlpha);
                #else
                    // 瀏海陰影：深度比較剔除法 (Depth Testing)
                    float2 screenUV = input.screenUV.xy / input.screenUV.w;
                    
                    // 讀取螢幕深度緩衝區
                    float rawDepth = SampleSceneDepth(screenUV);
                    float eyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                    // 計算當前片元的螢幕深度
                    float4 clipPos = ComputeClipSpacePosition(screenUV, input.positionCS.z) * input.positionCS.w;
                    float4 screenPos = ComputeScreenPos(clipPos);
                    
                    // 核心邏輯：如果這個代理面片與臉部(背景)的深度差距大於 _DepthClip，則剔除不畫
                    float alpha = step((eyeDepth - screenPos.w), _DepthClip) * _ShadowColor.a;
                    
                    clip(alpha - 0.001);
                    return half4(_ShadowColor.rgb, alpha);
                #endif
            }
            ENDHLSL
        }
    }
    // 獨立的 Utility Shader，若需要可掛載 LWGUI
    CustomEditor "LWGUI.LWGUI"
}