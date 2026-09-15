Shader "Custom/LocalX_ColorFill_URP"
{
    Properties
    {
        [Header(Color Settings)]
        _BaseColor ("Base Color (Original)", Color) = (1,1,1,1)
        _FillColor ("Fill Color (New)", Color) = (1,0,0,1)
        
        [Header(Fill Control)]
        _FillAmount ("Fill Amount", Range(0, 1)) = 0.0
        
        [Header(Model Bounds)]
        //[Tooltip(模型的本地 X 軸最小值 左邊界)]
        _MinX ("Min X (Local Bounds)", Float) = -0.5
        //[Tooltip(模型的本地 X 軸最大值 右邊界)]
        _MaxX ("Max X (Local Bounds)", Float) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0; // 傳遞本機座標至片段著色器
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _FillColor;
                float _FillAmount;
                float _MinX;
                float _MaxX;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                // 將頂點從 Object Space 轉換至 Clip Space
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // 保留 Object Space 座標供後續判定
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. 正規化 X 座標 (將 _MinX ~ _MaxX 的範圍映射到 0 ~ 1)
                // 若 X = _MinX，結果為 0 (最左側)
                // 若 X = _MaxX，結果為 1 (最右側)
                float normalizedX = (input.positionOS.x - _MinX) / (_MaxX - _MinX);

                // 避免超出邊界導致的負數或大於 1 的異常值
                normalizedX = saturate(normalizedX);

                // 2. 遮罩判定
                // 當 normalizedX <= _FillAmount 時，step 回傳 1 (填滿)
                // 當 normalizedX > _FillAmount 時，step 回傳 0 (維持原色)
                float fillMask = step(normalizedX, _FillAmount);

                // 3. 顏色線性插值
                return lerp(_BaseColor, _FillColor, fillMask);
            }
            ENDHLSL
        }
    }
}