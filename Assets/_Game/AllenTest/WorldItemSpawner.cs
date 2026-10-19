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
    // 觸發 A：背包傾倒 (抽籤 -> 轉發給實體生成器)
    // ==========================================
    private void OnInventoryDumpTriggered(InventoryDumpTriggeredEvent evt)
    {
        // 1. 呼叫大腦隨機找一個有數量的物品
        InventoryItemRuntimeData selectedItem = InventoryManager.Instance.GetRandomNonEmptyItem();

        if (selectedItem == null)
        {
            Debug.Log("<color=gray>[傾倒] 背包是空的，掉不出東西！</color>");
            return;
        }

        // 2. 既然抽到了，我們就「假裝」這是一個普通的丟棄行為
        // 直接呼叫底下的非同步生成方法！
        SpawnRealItemAsync(selectedItem, evt.dumpPosition, isFromDump: true);
    }

    // ==========================================
    // 觸發 B：滑鼠拖曳丟棄
    // ==========================================
    private void OnItemDroppedInWorld(ItemDroppedInWorldEvent cmd)
    {
        // 滑鼠丟出來的，當然也是直接呼叫非同步生成方法
        SpawnRealItemAsync(cmd.ItemData, cmd.DropPosition, isFromDump: false);
    }

    // ==========================================
    // 核心共用邏輯：非同步載入真實 3D 模型並生成
    // ==========================================
    private async void SpawnRealItemAsync(InventoryItemRuntimeData sourceItemData, Vector3 spawnPos, bool isFromDump)
    {
        string addressableKey = sourceItemData.BaseTemplete.PrefabPath;
    
        if (string.IsNullOrEmpty(addressableKey))
        {
            Debug.LogError($"[生成失敗] 物品 {sourceItemData.BaseTemplete.Name} 沒有設定 PrefabPath。");
            return;
        }

        // 1. 非同步載入真實的 3D 模型 (例如蘋果、劍、盾牌)
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

            // 4. 根據來源，決定要不要給物理推力 (這是背包傾倒特有的！)
            if (isFromDump && spawnedItem.TryGetComponent<Rigidbody>(out var rb))
            {
                // 如果是背包倒出來的，給他一個四散噴射的物理力
                Vector3 randomTorque = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
                rb.AddTorque(randomTorque, ForceMode.Impulse);
                rb.AddForce(Vector3.down * 4f + new Vector3(Random.Range(-1.5f, 1.5f), 0, 0), ForceMode.Impulse);
            }

            // 5. 正式從背包中扣除 1 個
            InventoryManager.Instance.RemoveItem(sourceItemData.itemId, 1);
            
            // 6. 發送 UI 刷新事件
            GameManager.Instance.MainGameEvent.Send(new PlayerBagRefreshedEvent() { ItemControllerType = ItemControllerType.All });
        }
    }
}