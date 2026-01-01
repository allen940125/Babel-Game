using UnityEngine;
using UnityEngine.Rendering; // 引用 APV 系統

[ExecuteAlways] // 讓你在編輯模式下拖拉時間也能看到效果 (不用按 Play)
public class DayNightSystem : MonoBehaviour
{
    [Header("--- 時間設定 ---")]
    [Range(0f, 24f)] 
    public float timeOfDay = 12f; // 當前時間 (0~24小時)
    public float timeSpeed = 1f;  // 時間流逝速度 (1 = 真實時間, 60 = 遊戲1分鐘等於現實1秒)
    public bool pauseTime = false;

    [Header("--- APV 設定 ---")]
    public string dayScenarioName = "Day";   // 你烘焙時取的名字
    public string nightScenarioName = "Night"; // 你烘焙時取的名字
    
    // 這條曲線決定 "黑夜" 的權重。 
    // X軸是時間(0~24)，Y軸是混合比例(0=全白天, 1=全黑夜)
    public AnimationCurve nightWeightCurve; 

    [Header("--- 太陽與環境設定 ---")]
    public Light sunLight;
    public Gradient sunColor;       // 太陽顏色隨時間變化
    public AnimationCurve sunIntensity; // 太陽強度隨時間變化
    public Gradient fogColor;       // 霧氣顏色隨時間變化

    // 內部變數
    private ProbeReferenceVolume probeVolume;

    void Start()
    {
        // 抓取 APV 的實例
        probeVolume = ProbeReferenceVolume.instance;
        
        // 初始化曲線 (如果使用者沒設定的話，給一個預設的 V 字型)
        if (nightWeightCurve.length == 0)
        {
            // 預設邏輯：半夜(0點)權重1，中午(12點)權重0，晚上(24點)權重1
            nightWeightCurve.AddKey(0f, 1f);  // 00:00 是黑夜
            nightWeightCurve.AddKey(6f, 0.5f); // 06:00 過渡
            nightWeightCurve.AddKey(12f, 0f); // 12:00 是純白天
            nightWeightCurve.AddKey(18f, 0.5f);
            nightWeightCurve.AddKey(24f, 1f); // 24:00 又是黑夜
        }
    }

    void Update()
    {
        // 1. 時間流逝邏輯 (只在遊戲執行時跑)
        if (Application.isPlaying && !pauseTime)
        {
            timeOfDay += Time.deltaTime * timeSpeed;
            if (timeOfDay >= 24f) timeOfDay = 0f; // 循環新的一天
        }

        // 2. 更新 APV 混合
        UpdateAPVBlending();

        // 3. 更新太陽與環境
        UpdateSunAndEnvironment();
    }

    void UpdateAPVBlending()
    {
        if (probeVolume == null) probeVolume = ProbeReferenceVolume.instance;
        if (probeVolume == null) return;

        // 計算當前的 "黑夜權重" (0=白天, 1=黑夜)
        float blendFactor = nightWeightCurve.Evaluate(timeOfDay);

        // 邏輯核心：
        // 我們把 "Day" 當作基底 (Base)，然後把 "Night" 疊加 (Blend) 上去。
        // 當 blendFactor 為 0 時，完全顯示 Day。
        // 當 blendFactor 為 1 時，完全顯示 Night。
        
        probeVolume.lightingScenario = dayScenarioName; // 設定基底
        probeVolume.BlendLightingScenario(nightScenarioName, blendFactor); // 混合目標
    }

    void UpdateSunAndEnvironment()
    {
        if (sunLight == null) return;

        // 歸一化時間 (0~1) 用於採樣 Gradient
        float timePercent = timeOfDay / 24f;

        // A. 太陽旋轉 (0點在底下, 6點日出, 12點頭頂, 18點日落)
        // 簡單算法：(時間/24) * 360度 - 90度偏移
        float sunAngle = (timePercent * 360f) - 90f; 
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f); // Y軸170是為了有點斜度，影子比較好看

        // B. 太陽顏色與強度
        sunLight.color = sunColor.Evaluate(timePercent);
        sunLight.intensity = sunIntensity.Evaluate(timeOfDay);

        // C. 霧氣控制
        if (RenderSettings.fog)
        {
            RenderSettings.fogColor = fogColor.Evaluate(timePercent);
        }
    }
}