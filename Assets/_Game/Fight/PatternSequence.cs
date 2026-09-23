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

    // ❌ 移除重複宣告：基底類別 AttackPatternBase 已經有 public bool destroyOnFinish;

    // --- 編輯器輔助功能 ---
    [ContextMenu("自動抓取子物件 Pattern")]
    public void AutoGetChildrenPatterns()
    {
        steps.Clear();
        // ★ 修正：includeInactive = true，才抓得到未啟用的子物件
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
        // ★ 修正：脫離 Boss 的子物件層級，避免 Boss 移動時把整個序列拖著跑
        transform.SetParent(null);

        // 1. 欺騙狀態機：將自己註冊為 ActiveBullet。
        //    只要這個 Sequence 還沒跑完並銷毀，Boss 的 WaitingForBullets 狀態就不會結束。
        if (boss != null)
        {
            boss.RegisterActiveBullet(this.gameObject);
        }

        // 2. 啟動非同步的時間軸序列
        StartCoroutine(RunSequenceRoutine(boss, speedMultiplier, isAngry));
    }

    private IEnumerator RunSequenceRoutine(BossStateMachine boss, float speedMultiplier, bool isAngry)
    {
        foreach (var step in steps)
        {
            if (step.pattern == null) continue;

            // 若 Boss 處於憤怒狀態，縮減等待時間，加快攻擊節奏
            float waitTime = step.delayBefore;
            if (isAngry) waitTime *= 0.8f; 

            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
            }

            // 觸發該步驟的 Pattern 執行其獨立邏輯
            step.pattern.Execute(boss, speedMultiplier, isAngry);
        }

        // ★ 修正：改用基底類別的統一結束方法
        FinishPattern();
    }

    // ==========================================
    // ★ 實作父類別的抽象方法：編輯器預覽
    // ==========================================
    protected override void OnGeneratePreview(Transform previewContainer)
    {
        // PatternSequence 本身不發射子彈，
        // 這裡只為每個步驟畫一個「編號球」，讓你知道這個序列有幾段。
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

        Debug.Log($"<color=cyan>[PatternSequence]</color> 共 {steps.Count} 個步驟。子 Pattern 的預覽請分別對它們按 Context Menu 產生。");
    }
}