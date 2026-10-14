using UnityEngine;

public class LaserTripwire : MonoBehaviour
{
    [Header("除錯設定")]
    public bool enableDebug = true;

    private BombMiniGameManager _manager;

    private void Start()
    {
        _manager = GetComponentInParent<BombMiniGameManager>();
        
        if (enableDebug && _manager == null)
        {
            Debug.LogError($"[絆線致命錯誤] {gameObject.name} 無法在父節點找到 BombMiniGameManager！");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 確認物理引擎是否有成功偵測到交疊
        if (enableDebug) 
        {
            Debug.Log($"<color=cyan>[絆線物理觸發] 偵測到物件進入: {other.gameObject.name} (Layer: {LayerMask.LayerToName(other.gameObject.layer)})</color>");
        }

        // 2. 向上尋找標記腳本
        TripwireDetonator detonator = other.GetComponentInParent<TripwireDetonator>();
        
        if (detonator != null)
        {
            if (enableDebug) 
            {
                Debug.Log($"<color=green>[絆線驗證成功] {other.gameObject.name} 身上或父節點具有 TripwireDetonator 標記，準備發送引爆信號。</color>");
            }

            // 3. 呼叫總管
            if (_manager != null)
            {
                _manager.ReportTripwireBreached();
            }
            else
            {
                Debug.LogError($"[絆線邏輯斷裂] 驗證成功，但總管參考遺失，無法引爆！");
            }

            gameObject.SetActive(false); 
        }
        else
        {
            if (enableDebug) 
            {
                Debug.LogWarning($"<color=orange>[絆線驗證失敗] {other.gameObject.name} 缺乏 TripwireDetonator 標記，已過濾此碰撞。</color>");
            }
        }
    }
}