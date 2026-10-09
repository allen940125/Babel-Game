Shader "Custom/Local_ColorFill_Vector_URP"
{
    Properties
    {
        [Header(Color Settings)]
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _FillColor ("Fill Color", Color) = (1,0,0,1)
        
        [Header(Fill Control)]
        _FillAmount ("Fill Amount", Range(0, 1)) = 0.0
        
        [Header(Fill Direction and Bounds)]
        // 使用 Vector 定義方向。X軸填(1,0,0)，Y軸填(0,1,0)，反向就填負值(-1,0,0)
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
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
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 確保輸入的方向為單位向量
                float3 dir = normalize(_FillDirection.xyz);

                // 核心數學：使用 Dot 將 3D 座標投影到該方向向量上
                // 這會得出該點在 _FillDirection 軸向上的純量值
                float projectedVal = dot(input.positionOS, dir);

                // 正規化映射
                float normalizedVal = saturate((projectedVal - _MinBound) / (_MaxBound - _MinBound));

                // 遮罩判定與插值
                float fillMask = step(normalizedVal, _FillAmount);
                return lerp(_BaseColor, _FillColor, fillMask);
            }
            ENDHLSL
        }
    }
}