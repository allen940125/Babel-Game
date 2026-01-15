using UnityEngine;

public abstract class AttackPatternBase : MonoBehaviour
{
    // 讓 Boss 呼叫的方法
    public void Execute(BossBase boss, float speedMultiplier, bool isAngry)
    {
        OnExecute(boss, speedMultiplier, isAngry);
        
        // --- 修改：移除原本這裡的 Destroy(gameObject, 0.1f); ---
        // 讓子類別自己決定什麼時候銷毀
        // 普通的發射器射完就可以 Destroy，但序列發射器需要存活一段時間
    }

    protected abstract void OnExecute(BossBase boss, float speedMultiplier, bool isAngry);
}