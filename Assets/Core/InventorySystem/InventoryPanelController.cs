using System;
using System.Collections.Generic; // 必須引用
using System.Linq;                // 必須引用 (用於 OrderBy)
using Game.UI;
using Gamemanager;
using UnityEngine;

public class InventoryPanelController
{
    [Header("背包UI位置")]
    private GameObject _uiPanel;
    private GameObject _slotGrid;
    private GameObject _emptySlot;
    
    public InventoryPanelController()
    {
        SubscribeEvents();
    }

    public void Dispose()
    {
        UnsubscribeEvents();
        GC.SuppressFinalize(this);
    }
    
    #region 初始化
    
    private void SubscribeEvents()
    {
        GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnPlayerBagRefreshedEvent, OnPlayerBagRefreshedEvent);
    }

    private void UnsubscribeEvents()
    {
        GameManager.Instance.MainGameEvent.Unsubscribe<PlayerBagRefreshedEvent>();
    }

    #endregion

    private void OnPlayerBagRefreshedEvent(PlayerBagRefreshedEvent cmd)
    {
        RefreshPlayerBagItem(cmd.ItemControllerType);
    }
    
    #region 背包更新

    public void SetBagInfo(GameObject uiPanel, GameObject grid, GameObject emptySlot)
    {
        this._uiPanel = uiPanel;
        _slotGrid = grid;
        _emptySlot = emptySlot;
    }
    
    private void RefreshPlayerBagItem(ItemControllerType itemControllerType)
    {
        InventoryManager.Instance.ClearChildObjects(_slotGrid.transform);
        InventoryManager.Instance.curCategoryTypeName = itemControllerType;

        // 核心修正：將動態型別 (dynamic) 替換為真實的資料型別 InventoryItemRuntimeData
        var itemsToDisplay = new List<InventoryItemRuntimeData>(); 

        if (itemControllerType == ItemControllerType.All)
        {
            // 指令為 All：無視分類，將所有 categoryGroups 內的 items 合併至單一清單
            foreach (var group in SaveManager.Instance.CurrentSaveData.InventoryData.categoryGroups)
            {
                if (group.items != null)
                {
                    itemsToDisplay.AddRange(group.items);
                }
            }
        
            // 將所有物品按照 itemId 排序
            itemsToDisplay = itemsToDisplay.OrderBy(item => item.itemId).ToList();
        }
        else
        {
            // 指令為具體分類：尋找對應的分類群組
            var category = SaveManager.Instance.CurrentSaveData.InventoryData.categoryGroups
                .Find(c => c.categoryName == itemControllerType);

            if (category != null)
            {
                itemsToDisplay.AddRange(category.items);
            }
            else
            {
                Debug.LogWarning("沒有找到符合的背包分類: " + itemControllerType);
                return;
            }
        }

        Debug.Log(itemsToDisplay.Count + " 生成格子");
    
        // 依據整併後的 itemsToDisplay 數量生成格子
        for (int i = 0; i < itemsToDisplay.Count; i++)
        {
            GameObject curGameObject = GameManager.Instance.InstantiateFromManager(_emptySlot);
            curGameObject.transform.SetParent(_slotGrid.transform);
        
            // 傳遞正確的型別 InventoryItemRuntimeData 給 Initialize
            curGameObject.GetComponent<SlotItem>().Initialize(itemsToDisplay[i]);
        }
    }
    #endregion
}