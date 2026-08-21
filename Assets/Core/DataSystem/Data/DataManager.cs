using System;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Datamanager;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Datamanager
{
    public class DataManager
    {
        public event System.Action OnDataLoaded;
        
        public GameDataDatabaseSO Database { get; private set; }

        public RealTimePlayerData realTimePlayerData = new RealTimePlayerData();
        
        public async Task InitDataMananger()
        {
            Database = await AddressableSearcher.GetAddressableAssetAsync<GameDataDatabaseSO>("GameDataDatabase");
            
            if (Database == null) Debug.LogError("找不到資料庫 SO！");
            
            OnDataLoaded?.Invoke();
        }
        
        // ==========================================
        // ★ 核心修復：維持泛型 API，但在內部進行型別路由
        // 加上 where T : class, IWithIdData 約束，確保進來的 T 都有 Id 屬性
        // ==========================================
        public T GetDataByID<T>(int id) where T : class, IWithIdData
        {
            if (Database == null) return null;

            Type type = typeof(T);

            // 根據傳入的型別 T，把任務派發給對應的具體 List
            if (type == typeof(ItemDataBaseTemplete))
            {
                return Database.ItemDatabase.Find(x => x.Id == id) as T;
            }
            else if (type == typeof(UIDataBaseTemplete))
            {
                return Database.UIDatabase.Find(x => x.Id == id) as T;
            }
            else if (type == typeof(StoreDataBaseTemplete))
            {
                return Database.StoreDatabase.Find(x => x.Id == id) as T;
            }
            // ... 未來每新增一個表格，就在這裡加一行 else if ...

            Debug.LogError($"[DataManager] 查無此型別的資料庫路由: {type}");
            return null;
        }

        // ==========================================
        // 同理修復 GetDataByName
        // ==========================================
        public T GetDataByName<T>(string name) where T : class, IWithNameData
        {
            if (Database == null) return null;

            Type type = typeof(T);

            if (type == typeof(ItemDataBaseTemplete))
            {
                return Database.ItemDatabase.Find(x => x.Name == name) as T;
            }
            else if (type == typeof(UIDataBaseTemplete))
            {
                return Database.UIDatabase.Find(x => x.Name == name) as T;
            }
            else if (type == typeof(StoreDataBaseTemplete))
            {
                return Database.StoreDatabase.Find(x => x.Name == name) as T;
            }

            Debug.LogError($"[DataManager] 查無此型別的資料庫路由: {type}");
            return null;
        }
    }
}

