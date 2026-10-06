using UnityEngine;
using System.Collections.Generic;

public class ItemPickupTrigger : MonoBehaviour
{
    [Header("觸發條件")]
    public List<string> triggerTags = new List<string> { "Player", "Boss" }; // 記得把 Boss 的 Tag 加上去

    [Header("撿起時觸發的命令 (可放入多個)")]
    public GameCommandSO[] onPickupCommands;

    [Header("撿起後是否摧毀此物件")]
    public bool destroyOnPickup = true;

    private InventoryItemRuntimeData _runtimeData;
    
    // ★ 安全鎖：防止同一個物品被物理和 Trigger 同時判定，導致撿了兩次
    private bool _isConsumed = false;

    public void InitializeData(InventoryItemRuntimeData data)
    {
        _runtimeData = data;
    }

    // ==========================================
    // 判定 1：物理碰撞 (給帶有實體 Collider 的玩家)
    // ==========================================
    private void OnCollisionEnter(Collision other)
    {
        TryTrigger(other.gameObject, "OnCollisionEnter (實體碰撞)");
    }

    // ==========================================
    // 判定 2：感應觸發 (給只有 Trigger 的 Boss，或進到感應圈的玩家)
    // ==========================================
    private void OnTriggerEnter(Collider other)
    {
        TryTrigger(other.gameObject, "OnTriggerEnter (觸發區域)");
    }

    // ==========================================
    // 統一的過濾邏輯
    // ==========================================
    private void TryTrigger(GameObject hitObject, string triggerMode)
    {
        if (_isConsumed) return;

        bool isTagMatched = triggerTags.Contains(hitObject.tag) || triggerTags.Contains(hitObject.transform.root.tag);

        if (isTagMatched)
        {
            Debug.Log($"<color=#4EC9B0>[ItemPickupTrigger]</color> 物件: <b>{gameObject.name}</b> 經由 <b>{triggerMode}</b> 觸發！" +
                      $"\n- 碰撞物件: <color=yellow>{hitObject.name}</color> (Tag: {hitObject.tag})" +
                      $"\n- 根物件 (Root): {hitObject.transform.root.name} (Tag: {hitObject.transform.root.tag})");

            ExecuteCommands(hitObject);
        }
        else
        {
            // 非目標物碰撞時的除錯（如地板、牆壁）
            // Debug.Log($"[ItemPickupTrigger] 忽略未配對 Tag: {hitObject.name} ({hitObject.tag})");
        }
    }

    private void ExecuteCommands(GameObject triggerEntity)
    {
        _isConsumed = true; // 上鎖，確保只會執行一次

        foreach (var cmd in onPickupCommands)
        {
            if (cmd != null)
            {
                cmd.Execute(triggerEntity, _runtimeData);
            }
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }
}