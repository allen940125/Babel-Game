using UnityEngine;

// ★ 徹底消滅 2D 語法與來回進出刷秒數的邏輯漏洞！
// 繼承自 BossSpecialMechanism，自動具備：3D Collider 觸發 + 扣除 SO 攻擊秒數 + 消耗隱藏
public class BossCleanerMechanism : BossSpecialMechanism
{
    [Header("Cleaner 道具專屬擴充")]
    [Tooltip("被吃掉時是否要播放特定的視覺特效或音效？")]
    [SerializeField] private GameObject consumeEffectPrefab;

    // ★ 1. 徹底刪除原本錯誤的 OnTriggerEnter2D 與 OnTriggerExit2D！
    // 3D 碰撞觸發與 SO 扣減秒數，父類別的 OnTriggerEnter 已經全權處理完畢！

    // ★ 2. 實作父類別留下的擴充介面：當此道具成功被玩家吃掉且扣除時間後，這裡會被自動呼叫
    protected override void OnMechanismTriggered()
    {
        Debug.Log($"<color=cyan>[Cleaner道具] {gameObject.name} 被成功吸收！</color>");

        // 如果你有放吃掉時的粒子特效或音效，在這裡生成
        if (consumeEffectPrefab != null)
        {
            Instantiate(consumeEffectPrefab, transform.position, Quaternion.identity);
        }

        // 如果這個道具被吃掉時有其他 Cleaner 專屬的額外效果（例如同時補玩家 10 點體力），可以寫在這裡：
        // PlayerRuntimeSO.Instance.RegenStamina(10f);
    }
}