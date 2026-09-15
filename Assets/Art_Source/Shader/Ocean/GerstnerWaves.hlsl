void GerstnerWaves_float(float3 WorldPos, float Time, float Amplitude, float Frequency, float Speed,
    out float3 OffsetWS, out float3 NormalWS)
{
    float3 waveOffset = float3(0, 0, 0);
    float3 waveNormal = float3(0, 1, 0);

    float2 dirs[3] = { float2(1,0), float2(0.6,0.8), float2(-0.7,0.7) };
    float steep[3]  = { 0.35, 0.25, 0.18 };
    float wlen[3]   = { 1.0, 0.55, 0.32 };

    [unroll]
    for (int i = 0; i < 3; i++)
    {
        float k = 6.2831853 / (wlen[i] / Frequency);
        float c = sqrt(9.8 / k) * Speed;
        float2 d = normalize(dirs[i]);
        float f = k * (dot(d, WorldPos.xz) - c * Time);
        float a = Amplitude * steep[i] / k;

        waveOffset.x += d.x * a * cos(f);
        waveOffset.z += d.y * a * cos(f);
        waveOffset.y += a * sin(f);

        waveNormal.x -= d.x * steep[i] * sin(f);
        waveNormal.z -= d.y * steep[i] * sin(f);
    }

    OffsetWS = waveOffset;
    NormalWS = normalize(waveNormal);
}