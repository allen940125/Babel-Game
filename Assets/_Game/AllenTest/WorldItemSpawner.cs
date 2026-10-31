using Gamemanager;
using UnityEngine;
using System.Threading.Tasks;

public class WorldItemSpawner : MonoBehaviour
{
    private void Awake()
    {
        // 1. 訂閱：玩家手動拖曳丟棄
        GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnItemDroppedInWorldEvent, OnItemDroppedInWorld);
        
        // 2. 訂閱：背包傾倒事件
        GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnInventoryDumpTriggeredEvent, OnInventoryDumpTriggered);
    }

    private void OnDestroy()
    {
        GameManager.Instance.MainGameEvent.Unsubscribe<ItemDroppedInWorldEvent>(OnItemDroppedInWorld);
        GameManager.Instance.MainGameEvent.Unsubscribe<InventoryDumpTriggeredEvent>(OnInventoryDumpTriggered);
    }

    // ==========================================
    // 觸發 A：背包傾倒 (接收事件中的 dumpVelocity)
    // ==========================================
    private void OnInventoryDumpTriggered(InventoryDumpTriggeredEvent evt)
    {
        InventoryItemRuntimeData selectedItem = InventoryManager.Instance.GetRandomNonEmptyItem();

        if (selectedItem == null)
        {
            Debug.Log("<color=gray>[傾倒] 背包是空的，掉不出東西！</color>");
            return;
        }

        // ★ 修改：把 evt.dumpVelocity 傳遞給生成方法
        SpawnRealItemAsync(selectedItem, evt.dumpPosition, true, evt.dumpVelocity);
    }

    // ==========================================
    // 觸發 B：滑鼠拖曳丟棄
    // ==========================================
    private void OnItemDroppedInWorld(ItemDroppedInWorldEvent cmd)
    {
        // ★ 修改：滑鼠靜態丟棄沒有動能，傳入 Vector3.zero
        SpawnRealItemAsync(cmd.ItemData, cmd.DropPosition, false, Vector3.zero);

        // ★ 新增：這是玩家在戰鬥中的實際動作，消耗一個回合，通知 Boss 攻擊
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Send(new PlayerActionCompletedEvent 
            { 
                //TriggerSource = $"UI 拖曳丟棄 ({cmd.ItemData.BaseTemplete.Name})" 
            });
        }
    }

    // ==========================================
    // 核心共用邏輯：非同步載入真實 3D 模型並生成
    // ==========================================
    // ★ 修改：新增參數 ejectVelocity
    private async void SpawnRealItemAsync(InventoryItemRuntimeData sourceItemData, Vector3 spawnPos, bool isFromDump, Vector3 ejectVelocity)
    {
        string addressableKey = sourceItemData.BaseTemplete.PrefabPath;
    
        if (string.IsNullOrEmpty(addressableKey))
        {
            Debug.LogError($"[生成失敗] 物品 {sourceItemData.BaseTemplete.Name} 沒有設定 PrefabPath。");
            return;
        }

        // 1. 非同步載入真實的 3D 模型
        GameObject loadedPrefab = await ResourceManager.LoadAssetAsync<GameObject>(addressableKey);

        if (loadedPrefab != null && this != null)
        {
            // 2. 生成真實物件
            GameObject spawnedItem = Instantiate(loadedPrefab, spawnPos, Quaternion.identity);
        
            // 3. 綁定獨立的快照資料
            if (spawnedItem.TryGetComponent<ItemPickupTrigger>(out var trigger))
            {
                InventoryItemRuntimeData dropData = new InventoryItemRuntimeData
                {
                    itemId = sourceItemData.itemId,
                    quantity = 1 
                };
                trigger.InitializeData(dropData);
            }
            else
            {
                Debug.LogWarning($"[警告] 生成的物品 {loadedPrefab.name} 缺少 ItemPickupTrigger 元件！");
            }

            // 在 WorldItemSpawner.cs 的 SpawnRealItemAsync 裡面：

            // 在 WorldItemSpawner.cs 的 SpawnRealItemAsync 裡面：

            // 4. 根據來源給予推力
            if (isFromDump)
            {
                // ★ 檢查這是不是掛了中介控制器的特殊武器
                if (spawnedItem.TryGetComponent<PlayerThrowableController>(out var controller))
                {
                    // =====================================
                    // 情境 A：這是你的客製化投擲子彈！
                    // =====================================
                    Vector3 finalVelocity = ejectVelocity * 0.5f + new Vector3(Random.Range(-2f, 2f), Random.Range(2f, 5f), 0f);
                    finalVelocity.z = 0f;

                    // 我們不直接找子彈，我們叫 Controller 幫我們處理所有初始化的鳥事
                    controller.Throw(finalVelocity.normalized, finalVelocity.magnitude);
                }
                else if (spawnedItem.TryGetComponent<Rigidbody>(out var rb))
                {
                    // =====================================
                    // 情境 B：這是一般物品 (蘋果、木頭)，用 Unity 內建物理
                    // =====================================
                    rb.isKinematic = false;
                    rb.useGravity = true;

                    Vector3 finalForce = ejectVelocity * 0.5f + new Vector3(Random.Range(-2f, 2f), Random.Range(2f, 5f), 0f);
                    finalForce.z = 0f;
                    rb.AddForce(finalForce, ForceMode.Impulse);
                    rb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.Impulse);
                }
            }

            // 5. 正式從背包中扣除 1 個
            InventoryManager.Instance.RemoveItem(sourceItemData.itemId, 1);
            
            // 6. 發送 UI 刷新事件
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = ItemControllerType.All });
        }
    }
}