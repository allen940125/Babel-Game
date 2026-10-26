using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// 逃跑時間軸：照順序執行 劇情文字 / 等待 / 攻擊（可搭配躲避 QTE）/ 結局最後一擊。
///
/// 攻擊步驟的流程：
///   1. 在指定位置生成攻擊（發射器自己的預警會先出現）。
///   2. 同時跳出 QTE。按對 → 立刻躲進附近的破碎 UI（無敵）；按錯或超時 → 不躲，攻擊照常打。
///   3. 躲藏結束後，再等 After Seconds 進入下一步。
///
/// 場景需求：FleeAttackDirector、FleeCoverSpawner、FleeDodge、QTEController、FleeTextSequence。
/// </summary>
public class FleeSequence : MonoBehaviour
{
    public enum StepType { Text, Wait, Attack, Finale }

    [Serializable]
    public class Step
    {
        public string note;
        public StepType type = StepType.Text;

        [Header("Text")]
        [TextArea] public string text;
        public float charInterval = 0.07f;
        public float holdSeconds = 2f;

        [Header("Wait")]
        public float waitSeconds = 2f;

        [Header("Attack / Finale")]
        [Tooltip("要發射的攻擊 prefab（GeneralAttackPattern / PatternSequence 等）。")]
        public AttackPatternBase attack;
        public FleeAttackDirector.SpawnMode spawnMode = FleeAttackDirector.SpawnMode.BottomRandomX;

        [Header("Attack 專用：躲避 QTE")]
        [Tooltip("勾選：這個攻擊要玩家按 QTE 躲進破碎 UI。Finale 不使用。")]
        public bool dodgeQTE = true;
        public Key qteKey = Key.E;
        [Tooltip("QTE 時限（秒）。0 = 預設 1.5 秒。建議設成跟攻擊預警時間差不多。")]
        public float qteSeconds = 0f;
        [Tooltip("成功後躲多久（要蓋過預警剩餘時間 + 子彈飛行時間）。")]
        public float hideSeconds = 3f;
        [Tooltip("這個步驟結束後再等幾秒。")]
        public float afterSeconds = 1f;
    }

    [SerializeField] private FleeAttackDirector director;
    [SerializeField] private FleeCoverSpawner coverSpawner;
    [SerializeField] private FleeDodge dodge;
    [SerializeField] private QTEController qte;
    [SerializeField] private FleeTextSequence text;
    [SerializeField] private FleeSceneController controller;

    [Header("躲避設定")]
    [Tooltip("QTE 成功時，離玩家這個距離內有掩體就躲進去；沒有就在玩家旁邊生一個。")]
    [SerializeField] private float maxCoverDistance = 4f;
    [Tooltip("附近沒有掩體時，新生成的掩體離玩家多遠（左右隨機一側）。")]
    [SerializeField] private float emergencyCoverOffset = 1.5f;

    [Header("時間軸")]
    [SerializeField] private Step[] steps;

    [Header("事件")]
    [SerializeField] private UnityEvent onDodgeSuccess;
    [SerializeField] private UnityEvent onDodgeFail;

    private int version;

    public void Play()
    {
        Run(++version).Forget();
    }

    public void Stop()
    {
        version++;
    }

    private async UniTaskVoid Run(int v)
    {
        var token = this.GetCancellationTokenOnDestroy();

        foreach (var step in steps)
        {
            if (v != version) return;

            switch (step.type)
            {
                case StepType.Text:
                    if (text != null) await text.Show(step.text, step.charInterval, step.holdSeconds);
                    break;

                case StepType.Wait:
                    await UniTask.Delay(TimeSpan.FromSeconds(step.waitSeconds), cancellationToken: token);
                    break;

                case StepType.Attack:
                    await RunAttack(step, token);
                    break;

                case StepType.Finale:
                    director?.SpawnAttack(step.attack, step.spawnMode);
                    controller?.ForceDefeatNow();
                    return; // 結局之後交給 FleeSceneController
            }
        }
    }

    private async UniTask RunAttack(Step step, CancellationToken token)
    {
        var attack = director != null ? director.SpawnAttack(step.attack, step.spawnMode) : null;
        float hideTime = step.hideSeconds;

        if (step.dodgeQTE && qte != null && attack != null)
        {
            float limit = step.qteSeconds > 0f ? step.qteSeconds : 1.5f;

            var resolved = new UniTaskCompletionSource<bool>();
            void OnResolved(bool ok) => resolved.TrySetResult(ok);

            qte.OnQTEResolved += OnResolved;
            var qteTask = qte.RunQTE(step.qteKey, limit);

            bool success = await resolved.Task.AttachExternalCancellation(token);
            qte.OnQTEResolved -= OnResolved;

            UniTask hideTask = UniTask.CompletedTask;
            if (success)
            {
                onDodgeSuccess?.Invoke();
                hideTask = Hide(hideTime);
            }
            else
            {
                onDodgeFail?.Invoke();
            }

            await qteTask;   // 等 QTE 的結果畫面收起來
            await hideTask;  // 等躲藏結束
        }
        else
        {
            // 沒有 QTE：直接等這個攻擊的預警 + 一小段時間
            await UniTask.Delay(TimeSpan.FromSeconds(hideTime), cancellationToken: token);
        }

        if (step.afterSeconds > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(step.afterSeconds), cancellationToken: token);
    }

    private async UniTask Hide(float seconds)
    {
        var cover = FindCover();
        if (cover == null || dodge == null) return;
        await dodge.HideIn(cover, seconds);
    }

    private FleeCover FindCover()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo == null) return null;
        Vector3 p = playerGo.transform.position;

        FleeCover best = null;
        float bestDist = maxCoverDistance;
        foreach (var c in FleeCover.All)
        {
            if (c == null || c.IsOccupied) continue;
            float d = Vector2.Distance(c.HidePosition, p);
            if (d <= bestDist) { best = c; bestDist = d; }
        }
        if (best != null) return best;

        // 附近沒有：在玩家旁邊生成一個
        if (coverSpawner == null) return null;
        float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        return coverSpawner.SpawnAt(p + new Vector3(side * emergencyCoverOffset, 0f, 0f));
    }
}
