using UnityEngine;

public abstract class BossSpecialMechanism : MonoBehaviour
{
    [Header("共用設定")]
    [Tooltip("誰撞到我才算數？")]
    public string targetTag = "PlayerButton";
    
    [Tooltip("視覺物件 (要隱藏/顯示的東西)")]
    public GameObject visualObject;

    public bool IsCleared => visualObject != null && !visualObject.activeSelf;

    protected virtual void Awake()
    {
        if (visualObject == null)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr) visualObject = sr.gameObject;
        }
        
        Collider2D col = GetComponentInChildren<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public virtual void ResetMechanism()
    {
        if(visualObject) visualObject.SetActive(true);
    }

    // --- ★ 修改 1：原本的 OnTriggerEnter2D ---
    // 這是給「子彈」或是「非拖曳物體」用的
    protected virtual void OnTriggerStay2D(Collider2D other)
    {
        // 如果撞到的是目標 Tag，就觸發
        if (other.CompareTag(targetTag))
        {
            TriggerThisMechanism();
        }
    }

    // --- ★ 修改 2：提供一個公開方法讓外部(拖曳物體)手動觸發 ---
    public void ManualTrigger(GameObject obj)
    {
        // 雙重確認：傳進來的物件 Tag 是對的才執行
        if (obj.CompareTag(targetTag))
        {
            TriggerThisMechanism();
        }
    }

    // --- ★ 修改 3：把核心邏輯抽出來 ---
    protected void TriggerThisMechanism()
    {
        // 執行關閉邏輯
        if (visualObject != null) visualObject.SetActive(false);
        
        // 如果這機關有 LineRenderer (Celeste版)，這裡最好也廣播或是由子類處理
        // 為了通用性，你可以把具體關閉邏輯保持在原本位置，或是讓子類別 override TriggerThisMechanism
        OnMechanismTriggered(); 
    }

    // 讓子類別 (如 BossCelesteMechanism) 去實作額外的關閉邏輯 (如關閉線條)
    protected virtual void OnMechanismTriggered() { }
}