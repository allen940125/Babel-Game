using UnityEngine;
using Gamemanager;

// ★ 這個腳本只掛在「玩家」的 Prefab 上！哥布林不要掛！
[RequireComponent(typeof(EntityCore))]
public class PlayerRegistration : MonoBehaviour
{
    private void Start()
    {
        // 1. 拿取自己的大腦資料
        var core = GetComponent<EntityCore>();
        if (core == null || core.RuntimeData == null)
        {
            Debug.LogError($"[致命錯誤] 玩家缺少 EntityCore，無法向系統報到！");
            return;
        }

        // 2. 主動向中繼站報到 (假設你的 Mediator 裡面有寫這個方法)
        GameManager.Instance.MainGameMediator.RegisterCurrentPlayer(core.RuntimeData);
        
        Debug.Log("<color=green>[系統] 玩家實體已成功向全域中繼站報到！</color>");
    }

    private void OnDestroy()
    {
        // 3. 玩家死亡或場景切換時，註銷資料，防止記憶體洩漏與報錯
        if (GameManager.Instance != null && GameManager.Instance.MainGameMediator != null)
        {
            GameManager.Instance.MainGameMediator.UnregisterCurrentPlayer();
        }
    }
}