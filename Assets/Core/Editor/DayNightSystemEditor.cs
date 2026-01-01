// Assets/Editor/DayNightSystemEditor.cs
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DayNightSystem))]
public class DayNightSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DayNightSystem script = (DayNightSystem)target;

        GUILayout.Space(10);
        if (GUILayout.Button("✨ Apply Beautiful Day/Night Preset", GUILayout.Height(30)))
        {
            ApplyBeautifulPreset(script);
            EditorUtility.SetDirty(script);
            Debug.Log("Applied 'Beautiful Day/Night' preset!");
        }
    }

    void ApplyBeautifulPreset(DayNightSystem script)
    {
        // 1. Night Weight Curve (用於 APV 混合)
        script.nightWeightCurve = new AnimationCurve();
        script.nightWeightCurve.AddKey(new Keyframe(0f, 1f, 0f, 0f, (float)WeightedMode.In, (float)WeightedMode.In));
        script.nightWeightCurve.AddKey(new Keyframe(5f, 0.8f, 0f, -1f));
        script.nightWeightCurve.AddKey(new Keyframe(8f, 0.1f, -1f, 0f));
        script.nightWeightCurve.AddKey(new Keyframe(12f, 0f, 0f, 0f));
        script.nightWeightCurve.AddKey(new Keyframe(16f, 0.1f, 0f, 1f));
        script.nightWeightCurve.AddKey(new Keyframe(19f, 0.8f, 1f, 0f));
        script.nightWeightCurve.AddKey(new Keyframe(24f, 1f, 0f, 0f));
        script.nightWeightCurve.preWrapMode = WrapMode.ClampForever;
        script.nightWeightCurve.postWrapMode = WrapMode.ClampForever;

        // 2. Sun Intensity Curve (峰值 1.5，符合你的要求)
        script.sunIntensity = new AnimationCurve();
        script.sunIntensity.AddKey(new Keyframe(0f, 0.05f));
        script.sunIntensity.AddKey(new Keyframe(5f, 0.12f));
        script.sunIntensity.AddKey(new Keyframe(6.5f, 0.45f));
        script.sunIntensity.AddKey(new Keyframe(8f, 0.95f));
        script.sunIntensity.AddKey(new Keyframe(12f, 1.45f));
        script.sunIntensity.AddKey(new Keyframe(12.5f, 1.5f)); // 峰值
        script.sunIntensity.AddKey(new Keyframe(13f, 1.45f));
        script.sunIntensity.AddKey(new Keyframe(15f, 1.28f));
        script.sunIntensity.AddKey(new Keyframe(18f, 0.15f));
        script.sunIntensity.AddKey(new Keyframe(20f, 0.08f));
        script.sunIntensity.AddKey(new Keyframe(24f, 0.05f));
        script.sunIntensity.preWrapMode = WrapMode.ClampForever;
        script.sunIntensity.postWrapMode = WrapMode.ClampForever;

        // 3. Sun Color Gradient (日出→日落)
        var sunColorKeys = new GradientColorKey[]
        {
            new GradientColorKey(HexToColor("#0a0c1d"), 0.00f), // 深夜
            new GradientColorKey(HexToColor("#14182a"), 0.22f), // 月光黎明
            new GradientColorKey(HexToColor("#e08b55"), 0.30f), // 日出
            new GradientColorKey(HexToColor("#f9d549"), 0.40f), // 上午金
            new GradientColorKey(Color.white,          0.50f), // 正午白
            new GradientColorKey(HexToColor("#f8d96c"), 0.60f), // 下午
            new GradientColorKey(HexToColor("#ff6b4a"), 0.72f), // 日落
            new GradientColorKey(HexToColor("#0a0c1d"), 1.00f)  // 回夜
        };
        var sunAlphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1f, 0.0f),
            new GradientAlphaKey(1f, 1.0f)
        };
        script.sunColor = new Gradient();
        script.sunColor.SetKeys(sunColorKeys, sunAlphaKeys);

        // 4. Fog Color Gradient (更柔和的環境色)
        var fogColorKeys = new GradientColorKey[]
        {
            new GradientColorKey(HexToColor("#0c1022"), 0.00f),
            new GradientColorKey(HexToColor("#1a1d33"), 0.20f),
            new GradientColorKey(HexToColor("#d87a3e"), 0.28f),
            new GradientColorKey(HexToColor("#f0e6d2"), 0.50f),
            new GradientColorKey(HexToColor("#f0e6d2"), 0.60f),
            new GradientColorKey(HexToColor("#d87a3e"), 0.72f),
            new GradientColorKey(HexToColor("#1a1d33"), 0.85f),
            new GradientColorKey(HexToColor("#0c1022"), 1.00f)
        };
        script.fogColor = new Gradient();
        script.fogColor.SetKeys(fogColorKeys, sunAlphaKeys);

        // 5. 預設時間與速度
        script.timeOfDay = 12f;
        script.timeSpeed = 1f; // 約 72 秒一圈 (適合展示)
    }

    Color HexToColor(string hex)
    {
        if (hex.StartsWith("#")) hex = hex.Substring(1);
        if (hex.Length != 6) return Color.black;
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color32(r, g, b, 255);
    }
}