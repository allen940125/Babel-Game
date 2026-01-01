using UnityEngine;


namespace GameFramework.Actors
{
    [System.Serializable]
    public class AttackActionData
    {
        [Header("動畫設定")]
        public string AnimationName; // 動畫名稱 (或 AnimationClip)
        public float CrossFadeDuration = 0.1f;
    
        [Header("時間設定 (秒)")]
        // 雖然動畫有長度，但我們通常會手動定義邏輯時間，手感比較好調
        public float DamageActiveTime; // 第幾秒開啟 Hitbox (Windup 結束)
        public float DamageEndTime;    // 第幾秒關閉 Hitbox (Swing 結束)
        public float RecoveryTime;     // 整個動作結束的時間
    
        [Header("數值設定")]
        public float DamageMultiplier = 1.0f; // 這一下的傷害倍率
        public float MovementMultiplier = 0f; // 這一下能不能移動
        public float RotationSpeedMultiplier = 0.3f;
        
        public Vector3 RootMotionForce;       // 額外的位移衝量
    }
}