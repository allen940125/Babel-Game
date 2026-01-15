using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    [Tooltip("整個序列結束後，是否銷毀此物件？")]
    public bool destroyOnFinish = true;

    // 右鍵選單：自動把子物件加到清單裡 (方便編輯)
    [ContextMenu("自動抓取子物件 Pattern")]
    public void AutoGetChildrenPatterns()
    {
        steps.Clear();
        // 抓取所有子物件的 AttackPatternBase (排除自己)
        var childPatterns = GetComponentsInChildren<AttackPatternBase>();
        foreach (var p in childPatterns)
        {
            if (p != this)
            {
                steps.Add(new PatternStep { pattern = p, delayBefore = 0.5f });
            }
        }
    }

    protected override void OnExecute(BossBase boss, float speedMultiplier, bool isAngry)
    {
        // 1. 重要：把自己註冊給 Boss
        // 這樣 Boss 就會把這個「序列發射器」當作是一個「還在場上的子彈」
        // 只要這個序列還沒銷毀，Boss 就不會進入下一波
        boss.RegisterActiveBullet(this.gameObject);

        // 2. 開始執行序列
        StartCoroutine(RunSequenceRoutine(boss, speedMultiplier, isAngry));
    }

    private IEnumerator RunSequenceRoutine(BossBase boss, float speedMultiplier, bool isAngry)
    {
        foreach (var step in steps)
        {
            if (step.pattern == null) continue;

            // 等待時間 (如果是憤怒狀態，可以加快節奏)
            float waitTime = step.delayBefore;
            if (isAngry) waitTime *= 0.8f; // 憤怒時動作快 20%

            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
            }

            // 執行這個步驟的 Pattern
            // 注意：這裡我們不傳入 isAngry 給子 Pattern，或者你可以選擇傳入
            // 通常子 Pattern 是瞬發的，所以我們直接執行它
            step.pattern.Execute(boss, speedMultiplier, isAngry);
        }

        // 序列結束
        if (destroyOnFinish)
        {
            Destroy(gameObject); // 銷毀自己 -> Boss 偵測到少了一個 ActiveBullet
        }
    }
}