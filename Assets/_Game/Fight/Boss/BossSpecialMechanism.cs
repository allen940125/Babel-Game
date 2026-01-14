using UnityEngine;

public class BossSpecialMechanism : MonoBehaviour
{
    [Header("判定設定")]
    [Tooltip("誰撞到我才算數？(例如 Player)")]
    public string targetTag = "Player";
    
    [Tooltip("視覺物件 (要隱藏/顯示的東西，可以是 SpriteRenderer 或是子物件)")]
    public GameObject visualObject;

    // 公開屬性：讓 Boss 知道我現在是不是「被隱藏/被壓制」的狀態
    // 如果 visualObject 是關閉的，代表我被壓制了 (IsCleared = true)
    public bool IsCleared => !visualObject.activeSelf;

    private void Awake()
    {
        // 如果沒手動拉，就抓自己的 SpriteRenderer
        if (visualObject == null)
        {
            // 嘗試抓子物件或自己的 SpriteRenderer
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr) visualObject = sr.gameObject;
        }
        
        // 確保 Collider 是 Trigger
        GetComponentInChildren<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 檢查是不是目標 (Player) 進入範圍
        // 你也可以改用 Layer: if (((1 << other.gameObject.layer) & targetLayer) != 0)
        if (other.CompareTag(targetTag))
        {
            Debug.Log("玩家進入判定區，隱藏機關！");
            visualObject.SetActive(false); // 隱藏
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 玩家離開範圍 -> 重新顯示
        if (other.CompareTag(targetTag))
        {
            Debug.Log("玩家離開判定區，機關恢復！");
            visualObject.SetActive(true); // 顯示
        }
    }
}