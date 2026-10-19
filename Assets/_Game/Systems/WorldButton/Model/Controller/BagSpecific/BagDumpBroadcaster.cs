using Gamemanager;
using UnityEngine;

/// <summary>
/// 專門給背包用的中介：將 UnityEvent 轉換為全域的背包傾倒事件
/// </summary>
public class BagDumpBroadcaster : MonoBehaviour
{
    [Tooltip("掉落物要在哪裡生成？(例如綁定在背包 3D 模型的開口處)")]
    public Transform dropOrigin;

    // 給 InvertDumpSensor3D 的 UnityEvent 呼叫的方法
    public void BroadcastDump()
    {
        Vector3 pos = dropOrigin != null ? dropOrigin.position : transform.position;
        
        // 翻譯成全域事件發送出去
        GameManager.Instance.MainGameEvent.Send(new InventoryDumpTriggeredEvent() 
        { 
            dumpPosition = pos 
        });
    }
}