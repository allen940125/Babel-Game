using UnityEngine;

public class SystemSetting : MonoBehaviour
{
    void Awake() // 在遊戲啟動瞬間執行
    {
        // 1. 關閉 VSync (垂直同步)
        // 手機上如果開 VSync，會被強制鎖在螢幕刷新率的倍數 (30/60)
        // 建議先關掉，讓 targetFrameRate 生效
        QualitySettings.vSyncCount = 0;

        // 2. 設定目標 FPS
        // 設為 60 是 ARPG 的標準
        // 設為 -1 代表「不限制」(讓手機全力跑，能跑165就跑165)
        // 但建議設 60 比較穩，設太高容易過熱
        Application.targetFrameRate = 60; 
        
        // 如果你想榨乾 ROG 的效能，可以試試看 120 (前提是手機螢幕有開120Hz)
        // Application.targetFrameRate = 120;
    }
}