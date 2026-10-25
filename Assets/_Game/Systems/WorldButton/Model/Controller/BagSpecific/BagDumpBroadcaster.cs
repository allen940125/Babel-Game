using Gamemanager;
using UnityEngine;

public class BagDumpBroadcaster : MonoBehaviour
{
    public Transform dropOrigin;

    // ★ 變更：接收感測器傳來的向量
    public void BroadcastDump(Vector3 ejectVelocity)
    {
        Vector3 pos = dropOrigin != null ? dropOrigin.position : transform.position;
        
        GameManager.Instance.MainGameEvent.Send(new InventoryDumpTriggeredEvent() 
        { 
            dumpPosition = pos,
            dumpVelocity = ejectVelocity // ★ 新增：把速度塞進事件裡
        });
    }
}