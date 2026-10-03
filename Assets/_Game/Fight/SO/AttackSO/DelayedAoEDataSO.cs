using UnityEngine;

// ==========================================
// 3. 延遲範圍攻擊專屬資料 (靜態定點，依賴動畫或計時引爆)
// ==========================================
[CreateAssetMenu(fileName = "New Delayed AoE Data", menuName = "Combat/Delayed AoE Data")]
public class DelayedAoEDataSO : AttackDataSO
{
    [Header("★ 命中震動回饋")]
    public bool enableCameraShake = false;
    public float shakeIntensity = 0.5f;
    public float shakeDuration = 0.2f;
}
