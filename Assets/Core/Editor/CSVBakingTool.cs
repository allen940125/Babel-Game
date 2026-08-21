#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CSVBakingTool : EditorWindow
{
    // ★ 恢復為 async void，這樣才能使用 await 解除死鎖
    [MenuItem("Tools/資料管線/烘焙 CSV 至 SO")]
    public static async void BakeCSVToSO()
    {
        Debug.Log("<color=yellow>========== 開始烘焙 CSV 資料 ==========</color>");

        try
        {
            EditorUtility.DisplayProgressBar("CSV 資料烘焙中", "正在準備 ScriptableObject...", 0.1f);
            Debug.Log("[步驟 1] 正在準備 ScriptableObject...");

            string folderPath = "Assets/GameData"; 
            string assetPath = $"{folderPath}/GameDataDatabase.asset"; 
            
            GameDataDatabaseSO databaseSO = AssetDatabase.LoadAssetAtPath<GameDataDatabaseSO>(assetPath);
            if (databaseSO == null)
            {
                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                    AssetDatabase.Refresh(); 
                }
                databaseSO = ScriptableObject.CreateInstance<GameDataDatabaseSO>();
                AssetDatabase.CreateAsset(databaseSO, assetPath);
            }

            // ==========================================
            // 讀取總表
            // ==========================================
            Debug.Log("[步驟 2] 正在讀取實體 CSV 總表...");
            string masterPath = "Assets/_Game/Data/CSV/ShamanKingCSV.csv"; // 確認你的總表路徑
            TextAsset masterCSV = AssetDatabase.LoadAssetAtPath<TextAsset>(masterPath);
            
            if (masterCSV == null)
            {
                Debug.LogError($"❌ 找不到 CSV 總表！路徑: {masterPath}");
                return;
            }

            Debug.Log("[步驟 3] 進入非同步解析總表字串...");
            // ★ 恢復 await，解除死鎖
            var stringData = await Datamanager.CSVClassGenerator.GenClassArrayByCSV<Datamanager.DatasPath>(masterCSV);
            
            if (stringData == null)
            {
                Debug.LogError("❌ 總表字串解析回傳 null，請檢查解析器邏輯。");
                return;
            }

            FieldInfo[] fields = typeof(GameDataDatabaseSO).GetFields(BindingFlags.Public | BindingFlags.Instance);
            int totalTasks = Mathf.Min(fields.Length, stringData.Length);
            
            Debug.Log($"<color=cyan>[步驟 4] 總表解析完成</color>，準備處理 {totalTasks} 個子表。");

            // ==========================================
            // 開始逐一處理子表
            // ==========================================
            for (int i = 0; i < totalTasks; i++)
            {
                FieldInfo field = fields[i];
                
                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type itemType = field.FieldType.GetGenericArguments()[0];
                    string currentTableName = field.Name;
                    
                    // 根據你推論的正確路徑組合
                    string currentPath = $"Assets/_Game/Data/{stringData[i].Path}.csv"; 

                    float progress = 0.1f + (0.8f * ((float)i / totalTasks));
                    EditorUtility.DisplayProgressBar("CSV 資料烘焙中", $"正在解析: {currentTableName}", progress);
                    
                    Debug.Log($"[處理中] 準備讀取: {currentTableName} (路徑: {currentPath})");
                    
                    TextAsset tableCSV = AssetDatabase.LoadAssetAtPath<TextAsset>(currentPath);
                    if (tableCSV == null)
                    {
                        Debug.LogWarning($"⚠️ 找不到實體檔案: {currentPath}，略過此表。");
                        continue;
                    }

                    Debug.Log($"[處理中] 進入非同步解析: {currentTableName} ...");
                    // ★ 恢復 await，解除死鎖
                    var csvData = await Datamanager.CSVClassGenerator.GenClassArrayByCSV(itemType, tableCSV);
                    
                    if (csvData == null)
                    {
                        Debug.LogWarning($"⚠️ 表格解析回傳 null: {currentTableName}");
                        continue;
                    }

                    System.Collections.IList genericList = (System.Collections.IList)Activator.CreateInstance(field.FieldType);
                    foreach (var item in (System.Collections.IEnumerable)csvData)
                    {
                        genericList.Add(item);
                    }

                    field.SetValue(databaseSO, genericList); 
                    Debug.Log($"✅ 成功！ <b>{currentTableName}</b> 寫入 {genericList.Count} 筆資料。");
                }
            }

            Debug.Log("[步驟 5] 所有表格處理完畢，正在寫入硬碟...");
            EditorUtility.SetDirty(databaseSO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("<color=green>🎉 烘焙大功告成！請點開 GameDataDatabaseSO 檢查資料！</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 烘焙發生致命例外: {ex}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            Debug.Log("[系統] 已確保進度條清除。");
        }
    }
}
#endif