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

        // 1. 透過 ResourceManager 非同步載入 GameObject (3D Prefab)
        GameObject loadedPrefab = await ResourceManager.LoadAssetAsync<GameObject>(addressableKey);

        if (loadedPrefab != null && this != null)
        {
            // 實例化
            GameObject spawnedItem = Instantiate(loadedPrefab, cmd.DropPosition, Quaternion.identity);
            
            // ★ 核心：尋找 Trigger 並注入動態資料
            if (spawnedItem.TryGetComponent<ItemPickupTrigger>(out var trigger))
            {
                // 把拖曳丟棄時的數量與ID原封不動交給 3D 物件
                trigger.InitializeData(cmd.ItemData);
            }
            else
            {
                Debug.LogWarning($"[警告] 生成的物品 {loadedPrefab.name} 缺少 ItemPickupTrigger 元件！");
            }

            // 扣除背包內的物品
            GameManager.Instance.MainGameEvent.Send(new ItemAddedToBagEvent() { ItemID = cmd.ItemData.BaseTemplete.Id, Quantity = -cmd.ItemData.quantity }); 
        }
        else
        {
            Debug.LogError($"[生成失敗] 無法從 Addressables 載入預製體：{addressableKey}");
        }
    }
}