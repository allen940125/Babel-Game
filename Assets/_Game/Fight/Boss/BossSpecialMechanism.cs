using UnityEngine;

// ★ 徹底移除了 2D 語法！全面支援 3D Collider 與 O(1) SO 減秒！
[RequireComponent(typeof(Collider))]
public class BossSpecialMechanism : MonoBehaviour
{
    [Header("道具與減秒設定")]
    [Tooltip("誰撞到我才算數？")]
    public string targetTag = "PlayerButton";
    [Tooltip("被碰到時，要扣除 Boss 幾秒攻擊時間？")]
    public float timeReduction = 3.0f;
    
    [Tooltip("視覺物件 (被吃掉後隱藏)")]
    public GameObject visualObject;

    // ★ 綁定 Boss 資料庫以執行減秒
    protected EntityRuntime TargetBoss;
    public bool IsCleared => visualObject != null && !visualObject.activeSelf;

    protected virtual void Awake()
    {
        if (visualObject == null)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr) visualObject = sr.gameObject;
        }
        
        // ★ 強行改為 3D Trigger，杜絕實體碰撞阻礙
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    // 由生成端 (BossCleaner) 傳入 SO 參照
    public void InitializeMechanism(EntityRuntime boss)
    {
        TargetBoss = boss;
    }

    public virtual void ResetMechanism()
    {
        if (visualObject) visualObject.SetActive(true);
    }

    // ★ 嚴格修正：必須是 OnTriggerEnter (3D)！
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            TriggerThisMechanism();
        }
    }

    public void ManualTrigger(GameObject obj)
    {
        if (obj.CompareTag(targetTag))
        {
            TriggerThisMechanism();
        }
    }

    protected void TriggerThisMechanism()
    {
        if (visualObject != null && !visualObject.activeSelf) return; // 避免重複吃
        
        if (visualObject != null) visualObject.SetActive(false);
        
        // ★ 核心變更：不要直接呼叫 SO，而是向 SO 索取「計時器特徵 (TimerTrait)」
        if (TargetBoss != null)
        {
            TimerTrait timerTrait = TargetBoss.GetTrait<TimerTrait>();
            
            if (timerTrait != null)
            {
                // 確定有拿到特徵，才執行扣秒
                timerTrait.ReduceTimer(timeReduction);
                Debug.Log($"<color=green>[道具生效] 成功吃掉機關！Boss 攻擊時間減少 {timeReduction} 秒！</color>");
            }
            else
            {
                // 防呆：如果企劃忘記在 Boss 的 SO 裡加上 TimerTrait，這裡會精準報錯
                Debug.LogWarning($"[{gameObject.name}] 目標 Boss 的 SO 並未掛載 TimerTrait，無法執行時間扣除！請檢查 Inspector！");
            }
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 尚未綁定 EntityRuntime，無法執行時間扣除！");
        }

        OnMechanismTriggered(); 
    }

    protected virtual void OnMechanismTriggered() { }
}