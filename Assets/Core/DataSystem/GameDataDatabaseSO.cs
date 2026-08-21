using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameDataDatabase", menuName = "Game Data/Database")]
public class GameDataDatabaseSO : ScriptableObject
{
    [Header("UI 介面資料庫")]
    public List<UIDataBaseTemplete> UIDatabase = new List<UIDataBaseTemplete>();

    [Header("道具資料庫")]
    public List<ItemDataBaseTemplete> ItemDatabase = new List<ItemDataBaseTemplete>();

    [Header("商店資料庫")]
    public List<StoreDataBaseTemplete> StoreDatabase = new List<StoreDataBaseTemplete>();
    
    // 未來有新的資料庫，就直接在這裡加 public List<T> ...
}