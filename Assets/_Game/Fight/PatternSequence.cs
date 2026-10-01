using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 複合型發射器：依序執行多個 AttackPatternBase。
/// </summary>
public class PatternSequence : AttackPatternBase
{
    [System.Serializable]
    public struct PatternStep
    {
        [Tooltip("要執行的攻擊模式 (可以是子物件，也可以是外部 Prefab)")]
        public AttackPatternBase pattern;
        
        [Tooltip("執行這個步驟前要等待的秒數")]
        public float delayBefore;
    }

    [Header("序列設定")]
    [Tooltip("攻擊步驟清單")]
    public List<PatternStep> steps = new List<PatternStep>();

    // --- 編輯器輔助功能 ---
    [ContextMenu("自動抓取子物件 Pattern")]
    public void AutoGetChildrenPatterns()
    {
        steps.Clear();
        var childPatterns = GetComponentsInChildren<AttackPatternBase>(true);
        foreach (var p in childPatterns)
        {
            if (p != this) // 排除自己，避免無限迴圈
            {
                steps.Add(new PatternStep { pattern = p, delayBefore = 0.5f });
            }
        }
        Debug.Log($"已抓取 {steps.Count} 個 Pattern 步驟！");
    }

    protected override void OnExecute(BossStateMachine boss, float speedMultiplier, bool isAngry)
    {
        transform.SetParent(null);

        if (boss != null)
        {
            boss.RegisterActiveBullet(this.gameObject);
        }

        StartCoroutine(RunSequenceRoutine(boss, speedMultiplier, isAngry));
    }

    private IEnumerator RunSequenceRoutine(BossStateMachine boss, float speedMultiplier, bool isAngry)
    {
        foreach (var step in steps)
        {
            if (step.pattern == null) continue;

            float waitTime = step.delayBefore;
            if (isAngry) waitTime *= 0.8f; 

            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
            }

            step.pattern.Execute(boss, speedMultiplier, isAngry);
        }

        FinishPattern();
    }

    // ==========================================
    // ★ Debug 測試：測試整個序列發射
    // ==========================================
    [ContextMenu("👉 測試發射序列 (Debug Test)")]
    public void DebugTest()
    {
        if (!Application.isPlaying) { Debug.LogError("⛔ 請先按 Play！"); return; }
        Execute(null, 1f, false);
    }

    // ==========================================
    // ★ 批次預覽管理：讓 Sequence 可以一次控制所有子 Pattern
    // ==========================================
    [ContextMenu("👁️ 生成全部步驟預覽 (All Previews)")]
    public void GenerateAllPreviews()
    {
        // 先幫自己畫預覽 (球體標記)
        GenerateEditorPreview();

        // 叫 List 裡面的所有 Pattern 也畫出他們的預覽
        foreach (var step in steps)
        {
            if (step.pattern != null)
            {
                step.pattern.GenerateEditorPreview();
            }
        }
    }

    [ContextMenu("❌ 清除全部步驟預覽")]
    public void ClearAllPreviews()
    {
        // 清除自己的
        ClearEditorPreview();

        // 清除 List 裡面所有 Pattern 的
        foreach (var step in steps)
        {
            if (step.pattern != null)
            {
                step.pattern.ClearEditorPreview();
            }
        }
    }

    protected override void OnGeneratePreview(Transform previewContainer)
    {
        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step.pattern == null) continue;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"Step {i} → {step.pattern.name} (delay {step.delayBefore}s)";
            marker.transform.SetParent(previewContainer);
            marker.transform.position = transform.position;
            marker.transform.localScale = Vector3.one * 0.4f;
            DestroyImmediate(marker.GetComponent<Collider>());
        }

        Debug.Log($"<color=cyan>[PatternSequence]</color> 共 {steps.Count} 個步驟，已呼叫全體預覽。");
    }
}