#ifndef ABYSS_PASS_FORWARD_INCLUDED
#define ABYSS_PASS_FORWARD_INCLUDED

#include "Core/AbyssCore.hlsl"
#include "Core/AbyssSurfaceSetup.hlsl"
#include "Core/AbyssPass_Utilities.hlsl"

#include "Effects/Effect_Fresnel.hlsl"
#include "Effects/Effect_Matcap.hlsl"
//#include "Effects/Effect_AnisotropicHighlight.hlsl"
#include "Effects/Effect_Wetness.hlsl"
#include "Lighting/Effect_Lighting.hlsl"

// =======================================================
// 【解耦設計】：只在開啟功能時才引入套件核心
// =======================================================
#if defined(_USE_CHAR_SHADOW)
    #include "Packages/com.unity.tooncharactershadow/Shaders/DeclareCharacterShadowTexture.hlsl"
#endif

Varyings vert_forward(Attributes input)
{
    Varyings output = (Varyings)0;;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionOS = input.positionOS.xyz;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), sign);
    
    output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
    output.screenPos = ComputeScreenPos(output.positionHCS);
    
    return output;
}

half4 frag_forward(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    AbyssSurfaceData surface;
    InitializeSurfaceData(input, surface);

    // 透明度裁切 (Opaque / Cutout / Dither 模式)
    // 即使在 Transparent 模式下，我們也保留基礎的 Clip 剔除極低 Alpha 的雜訊
    #if defined(_TRANSPARENCY_MODE_TRANSPARENT)
    clip(surface.alpha - 0.01); // 剃除接近完全透明的像素，減少 Overdraw
    #else
    DoTransparencyClip(surface.alpha, input.screenPos.xy / input.screenPos.w);
    #endif

    #if defined(_WEATHER_WETNESS_ON)
        ApplyWeatherWetness(surface, _LocalWetness, input.uv);
    #endif

    // 在 frag_forward 中找到計算 shadowCoord 的位置：
    // 取得指向主光源的方向向量
    float3 lightDirWS = _MainLightPosition.xyz;
    // 建立迎向光源推移的偏移座標，消除 CSM 陰影雜斑
    float3 biasedSamplePosWS = surface.positionWS + (lightDirWS * _CSMSampleBias);
    
    float4 shadowCoord = TransformWorldToShadowCoord(biasedSamplePosWS);
    Light mainLight = GetMainLight(shadowCoord);

    // 【新增備份】：把最原始、沒被污染的 URP 原生 CSM 存起來！
    half pureCSMShadow = mainLight.shadowAttenuation;

    // =======================================================
    // 【解耦設計】：角色局部陰影合併 (Per-Object Shadow)
    // =======================================================
    // =======================================================
    // 【解耦設計】：角色局部陰影合併 (Per-Object Shadow)
    // =======================================================
    half localCharShadow = 1.0; 

    #if defined(_USE_CHAR_SHADOW)
    // 1. 先取得套件算出來的原始數值
    half rawCharShadow = SampleCharacterAndTransparentShadow(surface.positionWS, surface.alpha);

    // 2. 智能反轉機制：
    // 如果是透視相機 (_CharShadowIsOrtho == 0)，套用 1.0 - rawCharShadow
    // 如果是正交相機 (_CharShadowIsOrtho == 1)，套用 rawCharShadow
    // (備註：如果你發現正反對調了，就把 lerp 裡面前面兩個參數的位置互換即可！)
    localCharShadow = lerp(1.0 - rawCharShadow, rawCharShadow, _CharShadowIsOrtho);

    // 3. 將 URP 全域陰影與局部陰影合併
    mainLight.shadowAttenuation = min(mainLight.shadowAttenuation, localCharShadow);
    #endif

    // AO 融合系統
    float2 screenUV = input.screenPos.xy / input.screenPos.w;
    half ssao = 1.0;
    #if defined(_SCREEN_SPACE_OCCLUSION)
        AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(screenUV);
        ssao = aoFactor.indirectAmbientOcclusion; 
    #endif
    half bakedAO = lerp(1.0, surface.occlusion, _OcclusionStrength);
    half finalAO = min(ssao, bakedAO);

    half3 finalColor = surface.albedo;
    
    // 3. 呼叫光照大腦 (取得完整 Diffuse + Specular + Rim + Env)
    #if defined(_USE_LIGHTING)
        finalColor = ComputeFinalLighting(surface, mainLight, finalAO);
    #endif

    // 效果疊加
    // ApplyFresnel(surface);
    // ApplyMatCap(surface);
    // ApplyAnisotropicHighlight(surface, mainLight);

    finalColor += surface.emission;

    // =======================================================
    // 全域除錯模式攔截器 (Global Debug View)
    // =======================================================
    // 1. 安全轉型：加上 0.1 確保浮點數轉整數時不會因為精度問題向下取整
        int debugMode = (int)(_GlobalDebugViewMode + 0.1);

    UNITY_BRANCH
    if (debugMode != 0)
    {
        switch (debugMode)
        {
            // ==========================================
            // 【1xx】最重要：光照與陰影核心
            // ==========================================
            case 101: // [1xx 關鍵] 原生 URP 全域陰影
                finalColor = pureCSMShadow.xxx;
                break;

            case 102: // [1xx 關鍵] CSM + 角色局部陰影合併後
                finalColor = mainLight.shadowAttenuation.xxx;
                break;

            case 103: // [1xx 關鍵] 角色局部陰影
                finalColor = localCharShadow.xxx;
                break;

            case 104: // [1xx 關鍵] 最終光照簡化版 (GI + 主光 + 陰影)
                half3 dbgGi = GetIndirectDiffuse(surface.positionWS, surface.normalWS, surface.viewDirWS);
                half dbgHalfLambert = dot(surface.normalWS, mainLight.direction) * 0.5 + 0.5;
                half3 dbgDirect = mainLight.color * dbgHalfLambert * mainLight.shadowAttenuation;
                finalColor = dbgGi + dbgDirect;
                break;

            case 105: // [1xx 關鍵] 間接光 (GI / SH / Light Probe)
                finalColor = GetIndirectDiffuse(surface.positionWS, surface.normalWS, surface.viewDirWS);
                break;

            case 106: // [1xx 關鍵] 原始環境反射
                half3 dbgReflectDir = reflect(-surface.viewDirWS, surface.normalWS);
                half dbgPerceptualRoughness = 1.0 - surface.smoothness;
                finalColor = GlossyEnvironmentReflection(dbgReflectDir, surface.positionWS, dbgPerceptualRoughness, 1.0h);
                break;

            case 107: // [1xx 關鍵] 反彈光
                float dbgBounceLambert = saturate(dot(surface.normalWS, -mainLight.direction)) * 0.5 + 0.5;
                finalColor = dbgBounceLambert * _BounceColor.rgb * _BounceIntensity;
                break;

            case 108: // [1xx 關鍵] 暗部過渡帶
                half dbgST_NdotL = dot(surface.normalWS, mainLight.direction);
                half dbgST_HalfLambert = dbgST_NdotL * 0.5 + 0.5;
                half dbgST_MainLightPower = saturate(max(mainLight.color.r, max(mainLight.color.g, mainLight.color.b)));
                half dbgST_LightWeight = saturate(dbgST_MainLightPower * 10.0);
                half dbgST_MathBand = smoothstep(_BandThreshold - _BandSmoothness, _BandThreshold + _BandSmoothness, dbgST_HalfLambert);
                half dbgST_FinalBand = dbgST_MathBand * mainLight.shadowAttenuation * dbgST_LightWeight;
                finalColor = ComputeShadowTerminator(surface, mainLight, dbgST_HalfLambert, dbgST_FinalBand);
                break;

            case 109: // [1xx 關鍵] 卡通分級 (未乘陰影)
                half dbgLB_NdotL = dot(surface.normalWS, mainLight.direction);
                half dbgLB_HalfLambert = dbgLB_NdotL * 0.5 + 0.5;
                half dbgLB_Band = smoothstep(_BandThreshold - _BandSmoothness, _BandThreshold + _BandSmoothness, dbgLB_HalfLambert);
                finalColor = dbgLB_Band.xxx;
                break;

            case 110: // [1xx 關鍵] 最終 AO (合併 SSAO)
                finalColor = finalAO.xxx;
                break;

            // ==========================================
            // 【2xx】重要：基礎輸入
            // ==========================================
            case 201: // [2xx 重要] Albedo
                finalColor = surface.albedo;
                break;

            case 202: // [2xx 重要] Normal
                finalColor = surface.normalWS * 0.5 + 0.5;
                break;

            case 203: // [2xx 重要] Alpha
                finalColor = surface.alpha.xxx;
                break;

            case 204: // [2xx 重要] Fresnel
                half dbgFresnel = pow(1.0 - saturate(dot(surface.normalWS, surface.viewDirWS)), 5.0);
                finalColor = dbgFresnel.xxx;
                break;

            case 205: // [2xx 重要] NdotL (原始 Lambert)
                half dbgNdotL = saturate(dot(surface.normalWS, mainLight.direction));
                finalColor = dbgNdotL.xxx;
                break;

            case 206: // [2xx 重要] HalfLambert (0~1)
                half dbgHL = dot(surface.normalWS, mainLight.direction) * 0.5 + 0.5;
                finalColor = dbgHL.xxx;
                break;

            case 207: // [2xx 重要] NdotH (高光核心)
                half3 dbgHD = SafeNormalize(mainLight.direction + surface.viewDirWS);
                half dbgNdotH = saturate(dot(surface.normalWS, dbgHD));
                finalColor = dbgNdotH.xxx;
                break;

            case 208: // [2xx 重要] Ramp 取樣結果
                half dbgRamp_NdotL = dot(surface.normalWS, mainLight.direction);
                half dbgRamp_HalfLambert = dbgRamp_NdotL * 0.5 + 0.5;
                half dbgRamp_Band = smoothstep(_BandThreshold - _BandSmoothness, _BandThreshold + _BandSmoothness, dbgRamp_HalfLambert);
                finalColor = SAMPLE_TEXTURE2D(_RampMap, sampler_BaseMap, float2(dbgRamp_Band, 0.5)).rgb;
                break;

            case 209: // [2xx 重要] 全域陰影色偏
                finalColor = _GlobalShadowColorBias.rgb;
                break;

            case 210: // [2xx 重要] 全域環境光
                finalColor = _GlobalAmbientColor.rgb * _GlobalAmbientIntensity;
                break;

            case 211: // [2xx 重要] 全域天氣 (強度 + 方向)
                finalColor = half3(_GlobalRainIntensity, 0, 0) + _GlobalRainDirection * 0.5 + 0.5;
                break;

            // ==========================================
            // 【3xx】中等：光照參數與中間值
            // ==========================================
            case 301: // [3xx 中等] Highlight
                half3 dbgSpecHD = SafeNormalize(mainLight.direction + surface.viewDirWS);
                half dbgSpecNdotH = max(0.0, dot(surface.normalWS, dbgSpecHD));
                half dbgSpec = smoothstep(_SpecularStep - _SpecularFeather, _SpecularStep + _SpecularFeather, dbgSpecNdotH);
                finalColor = _SpecularColor.rgb * dbgSpec * _SpecularIntensity;
                break;

            case 302: // [3xx 中等] RimLight (內 + 外)
                half3 dbgInnerRim, dbgFinalRim;
                CalculateAnimeRimLight(surface, mainLight, 1.0, dbgInnerRim, dbgFinalRim);
                finalColor = dbgInnerRim + dbgFinalRim;
                break;

            case 303: // [3xx 中等] 烘焙 AO
                finalColor = surface.occlusion.xxx;
                break;

            case 304: // [3xx 中等] View Direction
                finalColor = surface.viewDirWS * 0.5 + 0.5;
                break;

            case 305: // [3xx 中等] Tangent WS
                finalColor = surface.tangentWS.xyz * 0.5 + 0.5;
                break;

            case 306: // [3xx 中等] Bitangent WS
                half3 dbgBitangent = cross(surface.normalWS, surface.tangentWS.xyz) * surface.tangentWS.w;
                finalColor = dbgBitangent * 0.5 + 0.5;
                break;

            case 307: // [3xx 中等] Screen UV
                finalColor = half3(screenUV, 0);
                break;

            case 308: // [3xx 中等] World Position (低頻)
                finalColor = frac(surface.positionWS * 0.1);
                break;

            case 309: // [3xx 中等] Object Position
                finalColor = frac(surface.positionOS * 0.5 + 0.5);
                break;

            case 310: // [3xx 中等] Tiled UV
                finalColor = half3(input.uv, 0);
                break;

            case 311: // [3xx 中等] Camera Distance
                float dbgCamDist = distance(surface.positionWS, GetCameraPositionWS());
                finalColor = (dbgCamDist * 0.1).xxx;
                break;

            case 312: // [3xx 中等] Specular Raw (未乘顏色與強度)
                half3 dbgSpecRawHD = SafeNormalize(mainLight.direction + surface.viewDirWS);
                half dbgSpecRawNdotH = max(0.0, dot(surface.normalWS, dbgSpecRawHD));
                half dbgSpecRaw = smoothstep(_SpecularStep - _SpecularFeather, _SpecularStep + _SpecularFeather, dbgSpecRawNdotH);
                finalColor = dbgSpecRaw.xxx;
                break;

            case 313: // [3xx 中等] Aniso Raw
                finalColor = _AnisoColor.rgb * _AnisoIntensity;
                break;

            // ==========================================
            // 【4xx】次要：純 PBR 通道
            // ==========================================
            case 401: // [4xx 次要] Metallic
                finalColor = surface.metallic.xxx;
                break;

            case 402: // [4xx 次要] Smoothness
                finalColor = surface.smoothness.xxx;
                break;

            case 403: // [4xx 次要] Emission
                finalColor = surface.emission;
                break;

            case 404: // [4xx 次要] Main Light Color
                finalColor = mainLight.color;
                break;

            case 405: // [4xx 次要] Main Light Direction
                finalColor = mainLight.direction * 0.5 + 0.5;
                break;

            case 406: // [4xx 次要] Main Light Distance Attenuation
                finalColor = mainLight.distanceAttenuation.xxx;
                break;
        }
    }

    return half4(finalColor, surface.alpha);
}
#endif