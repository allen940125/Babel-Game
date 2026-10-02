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

        // 2. 確保載入成功且場景/管理器未被銷毀
        if (loadedPrefab != null && this != null)
        {
            // 3. 在事件指定的 3D 座標實例化物件
            Instantiate(loadedPrefab, cmd.DropPosition, Quaternion.identity);
            GameManager.Instance.MainGameEvent.Send(new ItemAddedToBagEvent() { ItemID = cmd.ItemData.BaseTemplete.Id, Quantity = -1 }); 
        }
        else
        {
            Debug.LogError($"[生成失敗] 無法從 Addressables 載入預製體：{addressableKey}");
        }
    }
}