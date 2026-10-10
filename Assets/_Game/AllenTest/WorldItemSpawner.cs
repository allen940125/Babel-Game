using Gamemanager;
using UnityEngine;
using System.Threading.Tasks;

public class WorldItemSpawner : MonoBehaviour
{
    private void Awake()
    {
        GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnItemDroppedInWorldEvent, OnItemDroppedInWorld);
    }

    private void OnDestroy()
    {
        GameManager.Instance.MainGameEvent.Unsubscribe<ItemDroppedInWorldEvent>(OnItemDroppedInWorld);
    }

    // ★ 加上 async 關鍵字以支援非同步等待
    private async void OnItemDroppedInWorld(ItemDroppedInWorldEvent cmd)
    {
        string addressableKey = cmd.ItemData.BaseTemplete.PrefabPath;
    
        if (string.IsNullOrEmpty(addressableKey))
        {
            Debug.LogError($"[生成失敗] 物品 {cmd.ItemData.BaseTemplete.Name} 沒有設定 PrefabPath。");
            return;
        }

        GameObject loadedPrefab = await ResourceManager.LoadAssetAsync<GameObject>(addressableKey);

        if (loadedPrefab != null && this != null)
        {
            GameObject spawnedItem = Instantiate(loadedPrefab, cmd.DropPosition, Quaternion.identity);
        
            if (spawnedItem.TryGetComponent<ItemPickupTrigger>(out var trigger))
            {
                // ★ 修正二：實例化全新的資料物件給地上的掉落物，切斷與背包資料的參考連結
                InventoryItemRuntimeData dropData = new InventoryItemRuntimeData
                {
                    itemId = cmd.ItemData.itemId,
                    quantity = 1 // 每次掉落 1 個
                };
                trigger.InitializeData(dropData);
            }
            else
            {
                Debug.LogWarning($"[警告] 生成的物品 {loadedPrefab.name} 缺少 ItemPickupTrigger 元件！");
            }

            // ★ 修正一：每次丟棄只扣除 1 個，而不是扣除該物品的所有數量
            GameManager.Instance.MainGameEvent.Send(new ItemAddedToBagEvent() 
            { 
                ItemID = cmd.ItemData.BaseTemplete.Id, 
                Quantity = -1 
            }); 
        }
    }
}