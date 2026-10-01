using UnityEngine;

// ★ 這個腳本只掛在「玩家」的 Prefab 上！哥布林不要掛！
[RequireComponent(typeof(EntityCore))]
public class PlayerRegistration : MonoBehaviour, IEntityRuntimeDependent
{
    public void OnRuntimeDataChanged(EntityRuntime newData)
    {
        if (newData == null)
        {
            Debug.LogError($"[致命錯誤] 玩家缺少 EntityRuntime，無法向系統報到！");
            return;
        }
        GameManager.Instance.MainGameMediator.RegisterCurrentPlayer(newData);
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