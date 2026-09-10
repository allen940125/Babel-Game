Shader "Custom/DistortedSky"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _NoiseTex ("Distortion Noise", 2D) = "gray" {}
        _SecondTex ("Overlay Texture", 2D) = "white" {}
        _OverlayMask ("Overlay Mask", 2D) = "black" {}

        _DistortAmount ("Distort Amount", Range(0,0.5)) = 0.05
        _NoiseScale ("Noise Scale", Float) = 3.0
        _NoiseSpeedX ("Noise Speed X", Float) = 0.05
        _NoiseSpeedY ("Noise Speed Y", Float) = 0.03

        _RGBShift ("RGB Channel Shift", Range(0,0.05)) = 0.005
        _Rotation ("Sky Rotation Speed", Float) = 0.01

        _Exposure ("Exposure", Range(0,3)) = 1.0
        _Tint ("Tint Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            sampler2D _SecondTex;
            sampler2D _OverlayMask;

            float _DistortAmount;
            float _NoiseScale;
            float _NoiseSpeedX;
            float _NoiseSpeedY;
            float _RGBShift;
            float _Rotation;
            float _Exposure;
            fixed4 _Tint;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            // 把方向向量轉成球面UV
            float2 DirToUV(float3 dir)
            {
                float2 uv;
                uv.x = atan2(dir.z, dir.x) / (2 * UNITY_PI) + 0.5;
                uv.y = asin(dir.y) / UNITY_PI + 0.5;
                return uv;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dir = normalize(i.texcoord);

                // 簡易旋轉整個天空（繞Y軸）
                float angle = _Time.y * _Rotation;
                float s = sin(angle);
                float c = cos(angle);
                float3 rotatedDir;
                rotatedDir.x = dir.x * c - dir.z * s;
                rotatedDir.z = dir.x * s + dir.z * c;
                rotatedDir.y = dir.y;

                float2 uv = DirToUV(rotatedDir);

                // === 噪波扭曲UV ===
                float2 noiseUV1 = uv * _NoiseScale + float2(_Time.y * _NoiseSpeedX, _Time.y * _NoiseSpeedY);
                float2 noiseUV2 = uv * _NoiseScale * 2.1 - float2(_Time.y * _NoiseSpeedY, _Time.y * _NoiseSpeedX);

                float n1 = tex2D(_NoiseTex, noiseUV1).r;
                float n2 = tex2D(_NoiseTex, noiseUV2).g;

                float2 offset = (float2(n1, n2) - 0.5) * _DistortAmount;
                float2 distortedUV = uv + offset;

                // === RGB通道錯位（模擬訊號干擾）===
                fixed r = tex2D(_MainTex, distortedUV + float2(_RGBShift, 0)).r;
                fixed g = tex2D(_MainTex, distortedUV).g;
                fixed b = tex2D(_MainTex, distortedUV - float2(_RGBShift, 0)).b;
                fixed4 baseCol = fixed4(r, g, b, 1);

                // === 疊加第二層貼圖（用mask控制範圍）===
                float mask = tex2D(_OverlayMask, distortedUV).r;
                fixed4 overlayCol = tex2D(_SecondTex, distortedUV * 1.5 - offset * 2);
                fixed4 finalCol = lerp(baseCol, overlayCol, mask);

                finalCol.rgb *= _Exposure;
                finalCol *= _Tint;

                return finalCol;
            }
            ENDCG
        }
    }
}