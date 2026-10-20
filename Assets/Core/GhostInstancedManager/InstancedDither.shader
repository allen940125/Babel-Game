Shader "Custom/InstancedDither"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1) // 接收 Manager 傳來的陣列顏色
    }
    
    SubShader
    {
        // ★ 核心優化：標記為 Opaque (不透明)，交給 Geometry 佇列渲染，零 Overdraw！
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            // 強制關閉陰影，省下一個 SetPass Call
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // ★ 啟動 GPU 實例化黑魔法
            #pragma multi_compile_instancing 

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // 宣告需要實例化 ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;  // 記錄螢幕座標用來畫棋盤格
                UNITY_VERTEX_INPUT_INSTANCE_ID // 傳遞給 Fragment Shader
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // ★ 接收 C# 總管傳進來的變數陣列
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // 處理模型頂點與攝影機的關係
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                
                // 取得螢幕座標
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // 1. 讀取材質原本的圖片
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // 2. 讀取 C# 陣列裡屬於這個殘影的透明度與顏色
                float4 instanceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor);
                float4 finalColor = texColor * instanceColor;

                // 3. PNG 去背防呆：如果圖片原本就是透明的(Alpha極低)，直接挖掉不畫
                clip(texColor.a - 0.05);

                // ==========================================
                // ★ Dither (像素抖動) 核心邏輯
                // ==========================================
                
                // 把座標轉換成真實的螢幕像素位置
                float2 screenPixel = (input.screenPos.xy / input.screenPos.w) * _ScreenParams.xy;
                
                // 找出這個像素在 4x4 棋盤格裡的哪一格
                uint x = (uint)screenPixel.x % 4;
                uint y = (uint)screenPixel.y % 4;
                
                // 經典的 Bayer 矩陣 (定義了 16 個格子的消失順序)
                float ditherMatrix[16] = {
                     1.0/17.0,  9.0/17.0,  3.0/17.0, 11.0/17.0,
                    13.0/17.0,  5.0/17.0, 15.0/17.0,  7.0/17.0,
                     4.0/17.0, 12.0/17.0,  2.0/17.0, 10.0/17.0,
                    16.0/17.0,  8.0/17.0, 14.0/17.0,  6.0/17.0
                };
                
                float ditherThreshold = ditherMatrix[x + y * 4];

                // 如果這個殘影的透明度 (finalColor.a) 小於這個格子的門檻，就挖空！
                clip(finalColor.a - ditherThreshold);

                // 成功活下來的像素，一律以 100% 不透明畫上去
                return float4(finalColor.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}