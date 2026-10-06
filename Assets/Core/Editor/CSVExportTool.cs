#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public class CSVExportTool : EditorWindow
{
    [MenuItem("Tools/資料管線/匯出 SO 至 CSV (備份用)")]
    public static void ExportSOToCSV()
    {
        Debug.Log("<color=cyan>========== 開始匯出 SO 資料至 CSV ==========</color>");

        string soPath = "Assets/GameData/GameDataDatabase.asset";
        GameDataDatabaseSO databaseSO = AssetDatabase.LoadAssetAtPath<GameDataDatabaseSO>(soPath);

        if (databaseSO == null)
        {
            Debug.LogError($"❌ 找不到 GameDataDatabaseSO！路徑: {soPath}");
            return;
        }

        // 為了安全，我們匯出到獨立資料夾，避免直接覆蓋企劃的原始表
        string exportDirectory = "Assets/ExportedCSV";
        if (!Directory.Exists(exportDirectory))
        {
            Directory.CreateDirectory(exportDirectory);
        }

        try
        {
            // 抓取 GameDataDatabaseSO 裡面所有的欄位 (如 UIDatabase, ItemDatabase...)
            FieldInfo[] fields = typeof(GameDataDatabaseSO).GetFields(BindingFlags.Public | BindingFlags.Instance);

            int exportedCount = 0;

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                // 確認該欄位是 List<>
                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                {
                    IList genericList = (IList)field.GetValue(databaseSO);
                    if (genericList == null || genericList.Count == 0)
                    {
                        Debug.LogWarning($"⚠️ 表格 {field.Name} 為空，跳過匯出。");
                        continue;
                    }

                    // 取得 List 裡面的元素型別 (例如 ItemDataBaseTemplete)
                    Type itemType = field.FieldType.GetGenericArguments()[0];
                    string csvContent = GenerateCSVString(itemType, genericList);

                    // 寫入實體檔案
                    string filePath = $"{exportDirectory}/{field.Name}.csv";
                    File.WriteAllText(filePath, csvContent, Encoding.UTF8);
                    
                    Debug.Log($"✅ 成功匯出: {field.Name}.csv ({genericList.Count} 筆資料)");
                    exportedCount++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"<color=green>🎉 匯出完畢！共匯出 {exportedCount} 個表格，請至 {exportDirectory} 查看。</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 匯出過程發生錯誤: {ex}");
        }
    }

    /// <summary>
    /// 透過反射將 List 資料轉為 CSV 格式字串
    /// </summary>
    private static string GenerateCSVString(Type dataType, IList dataList)
    {
        StringBuilder sb = new StringBuilder();

        // 抓取物件的所有公開欄位與屬性 (支援 SerializeField 與 public field/property)
        // 注意：這裡假設你的 Templete 使用了 [field: SerializeField] public ... { get; set; } 的寫法
        PropertyInfo[] properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        FieldInfo[] fields = dataType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // 1. 產生 Header (標題列)
        var allNames = properties.Select(p => p.Name).Concat(fields.Select(f => f.Name)).ToList();
        sb.AppendLine(string.Join(",", allNames));

        // 2. 產生資料列
        foreach (var item in dataList)
        {
            var values = new System.Collections.Generic.List<string>();

            // 讀取 Properties (例如 Id, Name, PrefabPath)
            foreach (var prop in properties)
            {
                object val = prop.GetValue(item);
                values.Add(FormatForCSV(val));
            }

            // 讀取 Fields (如果有傳統 public 變數)
            foreach (var field in fields)
            {
                object val = field.GetValue(item);
                values.Add(FormatForCSV(val));
            }

            sb.AppendLine(string.Join(",", values));
        }

        return sb.ToString();
    }

    /// <summary>
    /// 處理 CSV 字串格式 (防呆：如果字串裡面有逗號，必須用雙引號包起來)
    /// </summary>
    private static string FormatForCSV(object value)
    {
        if (value == null) return "";

        string str = value.ToString();

        // 如果字串本身包含逗號、雙引號或換行，必須遵守 CSV 規範用雙引號包覆，並將內部雙引號跳脫
        if (str.Contains(",") || str.Contains("\"") || str.Contains("\n") || str.Contains("\r"))
        {
            str = str.Replace("\"", "\"\""); // 雙引號跳脫
            str = $"\"{str}\""; // 外層包覆
        }

        return str;
    }
}
#endif