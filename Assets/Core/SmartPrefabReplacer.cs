using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SmartPrefabReplacer : MonoBehaviour
{
    [Header("搜尋與過濾設定")]
    [Tooltip("勾選後，會搜尋所有深層的子物件。")]
    public bool includeAllChildren = true; 

    [Tooltip("【強烈建議勾選】只替換身上有 MeshFilter 的物件。")]
    public bool replaceOnlyMeshes = true;

    public enum MatchType
    {
        Contains, // 寬鬆模式
        Exact     // 嚴格模式 (推薦)
    }

    [System.Serializable]
    public class PrefabOption
    {
        public GameObject prefab;
        [Range(0f, 100f)] public float weight = 10f; 
    }

    [System.Serializable]
    public class ReplacementRule
    {
        public string nameFilter;
        public MatchType matchType = MatchType.Exact; 
        
        [Tooltip("【強烈推薦勾選】\n勾選：新物件會被改名為 'Name Filter' (例如 'Wall')，這讓你下次還可以再次替換它 (無限重刷)。\n不勾選：新物件會使用 Prefab 原名 (例如 'Wall_Broken_01')。")]
        public bool renameToRule = true; // V5 新功能

        public List<PrefabOption> prefabOptions;
    }

    [Header("替換規則清單")]
    public List<ReplacementRule> replacementRules;

#if UNITY_EDITOR
    public void ExecuteReplacement()
    {
        if (replacementRules == null || replacementRules.Count == 0)
        {
            Debug.LogWarning("請先設定至少一個替換規則！");
            return;
        }

        List<Transform> candidates = new List<Transform>();
        
        if (includeAllChildren)
        {
            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms) if (t != this.transform) candidates.Add(t);
        }
        else
        {
            foreach (Transform child in transform) candidates.Add(child);
        }

        int replaceCount = 0;
        int skippedCount = 0;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Replace Prefabs V5");
        var undoGroup = Undo.GetCurrentGroup();

        foreach (Transform target in candidates)
        {
            if (target == null) continue;

            if (replaceOnlyMeshes)
            {
                bool hasMesh = target.GetComponent<MeshFilter>() != null || target.GetComponent<SkinnedMeshRenderer>() != null;
                if (!hasMesh) { skippedCount++; continue; }
            }

            string cleanName = GetCleanName(target.name);
            ReplacementRule targetRule = FindMatchingRule(target.name, cleanName);

            if (targetRule != null && targetRule.prefabOptions.Count > 0)
            {
                GameObject selectedPrefab = GetWeightedRandomPrefab(targetRule.prefabOptions);

                if (selectedPrefab != null)
                {
                    GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                    Undo.RegisterCreatedObjectUndo(newObj, "Create New Prefab");

                    newObj.transform.SetParent(target.parent); 
                    newObj.transform.localPosition = target.localPosition;
                    newObj.transform.localRotation = target.localRotation;
                    newObj.transform.localScale = target.localScale;

                    // --- V5 核心：決定新名字 ---
                    if (targetRule.renameToRule)
                    {
                        // 強制改回規則名字 (例如 "Wall")
                        // Unity 會自動處理重複，變成 "Wall (1)"
                        newObj.name = targetRule.nameFilter; 
                    }
                    else
                    {
                        // 使用 Prefab 原名 (例如 "Wall_Broken_Variant")
                        newObj.name = selectedPrefab.name;
                    }

                    Undo.DestroyObjectImmediate(target.gameObject);
                    replaceCount++;
                }
            }
        }
        
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"<color=green><b>V5 替換完成！</b></color> 成功: {replaceCount} | 跳過: {skippedCount}");
    }

    private string GetCleanName(string originalName)
    {
        // 移除 Unity 自動生成的 " (1)" 編號
        return Regex.Replace(originalName, @"\s\(\d+\)$", "");
    }

    private ReplacementRule FindMatchingRule(string rawName, string cleanName)
    {
        foreach (var rule in replacementRules)
        {
            if (string.IsNullOrEmpty(rule.nameFilter)) continue;

            if (rule.matchType == MatchType.Exact)
            {
                if (cleanName.Equals(rule.nameFilter)) return rule;
            }
            else 
            {
                if (rawName.Contains(rule.nameFilter)) return rule;
            }
        }
        return null;
    }

    private GameObject GetWeightedRandomPrefab(List<PrefabOption> options)
    {
        float totalWeight = 0f;
        foreach (var option in options) totalWeight += option.weight;

        float randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0f;

        foreach (var option in options)
        {
            currentWeight += option.weight;
            if (randomValue <= currentWeight) return option.prefab;
        }
        return options[0].prefab;
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(SmartPrefabReplacer))]
public class SmartPrefabReplacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        SmartPrefabReplacer script = (SmartPrefabReplacer)target;

        GUILayout.Space(20);
        GUI.backgroundColor = new Color(1f, 0.9f, 0.4f); // 金色按鈕
        if (GUILayout.Button("執行替換 (支援無限重刷)", GUILayout.Height(40)))
        {
            script.ExecuteReplacement();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.Space(10);
        EditorGUILayout.HelpBox("V5 更新提示：\n規則中新增 'Rename to Rule' 選項。\n✅ 勾選後：新物件名字會保持為規則名稱（例如 'Wall'）。這讓你可以反覆點擊按鈕來重新隨機 (Re-roll)。\n❌ 不勾選：新物件會使用 Prefab 原始名稱。", MessageType.Info);
    }
}
#endif