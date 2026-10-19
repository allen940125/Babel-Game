using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 翻轉傾倒感測器：當物件被倒轉超過特定角度時，觸發事件 (例如：背包掉落物品)
/// </summary>
public class InvertDumpSensor3D : MonoBehaviour
{
    [Header("傾倒判定設定")]
    [Tooltip("要偵測的軸向。通常是物件的上方 (Transform.up)")]
    public Vector3 localUpAxis = Vector3.up;

    [Tooltip("傾倒的臨界值 (0 ~ 1)。\n1 代表完全倒立 (180度)\n0.7 代表傾斜約 135 度")]
    [Range(0f, 1f)]
    public float dumpThreshold = 0.7f;

    [Header("冷卻與頻率")]
    [Tooltip("觸發後要冷卻多久才能再次觸發 (秒)")]
    public float cooldownTime = 1.0f;
    [Tooltip("是否在倒轉狀態下持續觸發？(例如：一直倒著就一直掉東西)")]
    public bool continuousDump = false;

    [Header("事件觸發")]
    [Tooltip("當物件被倒轉時觸發的事件")]
    public UnityEvent OnDumpTriggered;

    private float _lastDumpTime = -999f;
    private bool _isCurrentlyUpsideDown = false;

    private void Update()
    {
        CheckInvertStatus();
    }

    private void CheckInvertStatus()
    {
        // 1. 將本地的上方軸 (通常是 Transform.up) 轉換為世界座標的方向
        Vector3 worldUpOfObject = transform.TransformDirection(localUpAxis);

        // 2. 計算它與「世界正下方 (Vector3.down)」的重合程度
        // Dot 內積結果：
        // 1.0 = 完全倒立 (指向正下方)
        // 0.0 = 平躺 (指向側邊 90 度)
        // -1.0 = 正向朝上 (指向正上方)
        float dotProduct = Vector3.Dot(worldUpOfObject, Vector3.down);

        // 3. 判斷是否超過臨界值
        bool isUpsideDown = dotProduct >= dumpThreshold;

        // 4. 處理事件觸發邏輯
        if (isUpsideDown)
        {
            if (!_isCurrentlyUpsideDown || continuousDump)
            {
                if (Time.time >= _lastDumpTime + cooldownTime)
                {
                    TriggerDump();
                }
            }
        }

        _isCurrentlyUpsideDown = isUpsideDown;
    }

    private void TriggerDump()
    {
        _lastDumpTime = Time.time;
        
        Debug.Log($"<color=orange>[傾倒感測器] {gameObject.name} 被倒過來了！觸發掉落事件！</color>");
        
        if (OnDumpTriggered != null)
        {
            OnDumpTriggered.Invoke();
        }
    }
}