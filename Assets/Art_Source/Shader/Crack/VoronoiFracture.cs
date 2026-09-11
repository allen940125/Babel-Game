using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using EzySlice;

/// <summary>
/// UnityEvent&lt;int&gt; 需要包成具體的 class 才能在 Inspector 上正常顯示、
/// 讓其他人拖拽物件/方法進來（直接用 UnityEvent&lt;int&gt; 只能在程式碼裡掛，
/// Inspector 面板不會顯示參數欄位）。
/// </summary>
[System.Serializable]
public class FractureStageEvent : UnityEvent<int> { }

/// <summary>
/// 單一個「釋放階段」的設定。在 Inspector 上是一個 List，可以自由增加、
/// 刪除、拖曳排序，不需要改程式碼就能調整要分幾階段崩塌。
/// </summary>
[System.Serializable]
public class FractureReleaseStage
{
    [Tooltip("純粹方便在 Inspector 上辨識用的名稱，不影響邏輯")]
    public string label = "Stage";

    [Tooltip("這個階段要從「目前還沒掉落的碎片」中隨機釋放多少比例（0~1）。" +
             "最後一個階段無論設多少，都會強制釋放剩餘的所有碎片，" +
             "確保不會有碎片永遠卡在半空中掉不下來。")]
    [Range(0f, 1f)] public float releaseRatio = 1f;

    [Tooltip("這個階段的爆炸力，相對 explosionForce 的倍率")]
    [Range(0f, 2f)] public float forceMultiplier = 1f;
}

/// <summary>
/// 自寫 Voronoi 破碎：
/// 原理是「半空間切割法」，數學上等價於真正的 Voronoi 圖，
/// 不需要做完整的 3D Delaunay 三角化。
///
/// 做法：
/// 1. 在物體 bounding box 內隨機灑 N 個種子點。
/// 2. 對每個種子點 i，依序用它跟其他每個種子點 j 的垂直平分面切割 mesh，
///    每刀都只保留「離種子 i 較近」的那一半。
/// 3. 切完所有平分面後剩下的形狀，就是種子 i 的 Voronoi cell 跟原始 mesh 的交集，
///    也就是一塊碎片。
///
/// 分階段破碎（階段數量可自行增減，見 releaseStages）：
/// 第 1 次呼叫 TriggerNextStage()：算出所有碎片，撐開一點縫隙（視覺上「裂開」），
///   但碎片保持 Kinematic 不掉落。
/// 之後每呼叫一次 TriggerNextStage()，就依序執行 releaseStages 裡的下一個階段，
///   釋放一部分碎片開始掉落。releaseStages 是一個 List，可以在 Inspector 上
///   自由增加/刪除/調整每個階段的釋放比例與力道，不需要改程式碼。
/// 若要直接跳到最終崩塌，呼叫 JumpToStage(TotalStageCount) 即可
///   （內部會依序自動補跑前面所有階段）。
/// </summary>
public class VoronoiFracture : MonoBehaviour
{
    [Header("Voronoi 設定")]
    [Range(2, 40)] public int seedCount = 12;
    [Tooltip("種子點分佈範圍，1 = 完全填滿物體 bounding box")]
    [Range(0.1f, 1f)] public float seedSpread = 0.9f;
    [Tooltip("切開後斷面要顯示的材質（沒有的話用原本材質）")]
    public Material insideMaterial;

    [Header("物理設定")]
    public float explosionForce = 300f;
    public float explosionRadius = 3f;
    public float pieceLifetime = 8f;

    [Header("分階段破碎設定")]
    [Tooltip("第一次觸發（裂開）時，碎片彼此撐開的縫隙大小")]
    [Range(0f, 0.2f)] public float crackGap = 0.03f;

    [Tooltip("裂開之後的釋放階段。可自由增加/刪除/拖曳排序，數量沒有上限，" +
             "至少需要 1 個。每個階段會從「目前還沒掉落的碎片」隨機釋放一定比例，" +
             "最後一個階段一定會清空剩餘全部碎片。")]
    public List<FractureReleaseStage> releaseStages = new List<FractureReleaseStage>
    {
        new FractureReleaseStage { label = "部分掉落", releaseRatio = 0.5f, forceMultiplier = 0.5f },
        new FractureReleaseStage { label = "全部崩塌", releaseRatio = 1f,  forceMultiplier = 1f },
    };

    /// <summary>
    /// 目前破碎進度：
    /// 0 = 完整未破碎
    /// 1 = 已裂開，尚未執行任何釋放階段
    /// 2, 3, ... = 已完成第 (CurrentStage - 1) 個釋放階段
    /// TotalStageCount = 全部釋放階段都跑完，物體徹底崩塌（終點）
    /// </summary>
    public int CurrentStage { get; private set; } = 0;

    /// <summary>總階段數 = 裂開(1) + releaseStages 的數量，也是 CurrentStage 的終點值</summary>
    public int TotalStageCount => 1 + (releaseStages?.Count ?? 0);

    [Header("事件（給其他腳本/設計師掛勾子用，不用碰內部邏輯）")]
    [Tooltip("每次階段推進時觸發，參數是新的 CurrentStage 值。" +
             "可在 Inspector 上直接拖音效、動畫、UI 等進來，不需要寫程式。")]
    public FractureStageEvent OnStageChanged;

    private List<GameObject> fragments;
    private List<int> fragmentReleaseStep; // 每塊碎片預先分配好要在 releaseStages 的第幾個索引被釋放
    private int releaseStepsDone = 0;      // 已經完成的釋放階段數量

    // ------------------------------------------------------------------
    // 對外的簡單入口：每呼叫一次就往下一個階段推進一次，適合直接綁在
    // 輸入事件、UI 按鈕、劇情事件等任何觸發來源上，不用自己管理狀態機。
    // 這是「觸發端」唯一需要知道的方法——負責觸發的人不需要理解
    // Voronoi 切割、碎片管理或 releaseStages 的細節。
    // ------------------------------------------------------------------
    public void TriggerNextStage()
    {
        if (releaseStages == null || releaseStages.Count == 0)
        {
            Debug.LogError("VoronoiFracture: releaseStages 至少需要設定 1 個階段");
            return;
        }

        if (CurrentStage == 0)
        {
            StageCrack();
        }
        else if (releaseStepsDone < releaseStages.Count)
        {
            StageRelease(releaseStepsDone);
            releaseStepsDone++;
            CurrentStage++;
        }
        else
        {
            // 已經全部崩塌，不用再做事，也不用再觸發事件
            return;
        }

        OnStageChanged?.Invoke(CurrentStage);
    }

    /// <summary>
    /// 直接跳到指定階段，中間跳過的階段會依序自動補跑，
    /// 讓觸發端不用連續呼叫 TriggerNextStage() 好幾次。
    /// 例如：UI 上一個「全部炸開」按鈕可以直接呼叫 JumpToStage(TotalStageCount)。
    /// 若目前階段已經 >= targetStage，則不做任何事。
    /// </summary>
    public void JumpToStage(int targetStage)
    {
        targetStage = Mathf.Clamp(targetStage, 1, TotalStageCount);
        while (CurrentStage < targetStage)
        {
            TriggerNextStage();
        }
    }

    // ------------------------------------------------------------------
    // 裂開：算出所有碎片，撐開一點縫隙，先不掉落，
    // 並依 releaseStages 的比例，預先幫每塊碎片分配要在哪個階段被釋放
    // ------------------------------------------------------------------
    private void StageCrack()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogError("VoronoiFracture 需要物件上有 Renderer");
            return;
        }

        Vector3 originCenter = rend.bounds.center;

        fragments = GenerateFragments(rend);
        if (fragments.Count == 0)
        {
            Debug.LogWarning("VoronoiFracture：沒有產生任何碎片");
            CurrentStage = TotalStageCount;
            return;
        }

        fragmentReleaseStep = new List<int>(fragments.Count);

        foreach (var frag in fragments)
        {
            var mf = frag.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                fragmentReleaseStep.Add(releaseStages.Count - 1); // 丟到最後一個階段一起收尾
                continue;
            }

            var mc = frag.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = true;

            var rb = frag.AddComponent<Rigidbody>();
            rb.isKinematic = true; // 先固定住，這階段只做視覺裂開，不掉落

            // 沿著「碎片中心到物體中心」的方向，把碎片往外推一點點，露出縫隙
            var fragRenderer = frag.GetComponent<Renderer>();
            Vector3 fragCenter = fragRenderer != null ? fragRenderer.bounds.center : frag.transform.position;
            Vector3 dir = fragCenter - originCenter;
            if (dir.sqrMagnitude < 0.0001f) dir = Random.onUnitSphere;
            frag.transform.position += dir.normalized * crackGap;

            fragmentReleaseStep.Add(PickReleaseStepForFragment());
        }

        HideOriginal(rend); // 隱藏完整原始物件的視覺/碰撞，但保持 GameObject 啟用，
                            // 這樣掛在同一個物件上的本腳本與觸發腳本才能繼續收到 Update()
        CurrentStage = 1;
    }

    // ------------------------------------------------------------------
    // 依序檢查 releaseStages[0..N-2] 的 releaseRatio，決定這塊碎片要在哪個
    // 階段被釋放；如果都沒中，就丟到最後一個階段（保證一定會被釋放掉）。
    // stages.Count == 1 時，直接回傳唯一的索引 0。
    // ------------------------------------------------------------------
    private int PickReleaseStepForFragment()
    {
        for (int s = 0; s < releaseStages.Count - 1; s++)
        {
            if (Random.value < releaseStages[s].releaseRatio)
            {
                return s;
            }
        }
        return releaseStages.Count - 1;
    }

    // ------------------------------------------------------------------
    // 隱藏原始完整物件：只關掉會被看到/碰到的元件，
    // 「絕對不能」SetActive(false) 整個 GameObject，
    // 否則掛在同一物件上的本腳本、觸發腳本都會停止收到 Update()，
    // 導致後續階段永遠無法被觸發。
    // ------------------------------------------------------------------
    private void HideOriginal(Renderer rend)
    {
        rend.enabled = false;

        var colliders = GetComponents<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }
    }

    // ------------------------------------------------------------------
    // 執行 releaseStages[stepIndex] 這個釋放階段：
    // 釋放所有「被預先分配到這個索引」的碎片；
    // 如果這是最後一個階段，額外把所有還沒被釋放（仍是 Kinematic）的碎片
    // 一併釋放掉，確保不會有碎片永遠卡住。
    // ------------------------------------------------------------------
    private void StageRelease(int stepIndex)
    {
        if (fragments == null) return; // 正常流程下不會發生，安全起見擋一下

        var stageConfig = releaseStages[stepIndex];
        bool isLastStep = stepIndex == releaseStages.Count - 1;
        Vector3 explosionCenter = transform.position;

        for (int i = 0; i < fragments.Count; i++)
        {
            if (fragments[i] == null) continue;

            var rb = fragments[i].GetComponent<Rigidbody>();
            if (rb == null || !rb.isKinematic) continue; // 已經釋放過了

            bool shouldRelease = isLastStep || fragmentReleaseStep[i] == stepIndex;
            if (shouldRelease)
            {
                ReleaseFragment(fragments[i], explosionCenter, explosionForce * stageConfig.forceMultiplier);
            }
        }
    }

    private void ReleaseFragment(GameObject frag, Vector3 explosionCenter, float force)
    {
        var rb = frag.GetComponent<Rigidbody>();
        if (rb == null) rb = frag.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.AddExplosionForce(force, explosionCenter, explosionRadius);
        Destroy(frag, pieceLifetime);
    }

    // ------------------------------------------------------------------
    // 半空間切割法算出所有碎片（不含物理）
    // ------------------------------------------------------------------
    private List<GameObject> GenerateFragments(Renderer rend)
    {
        Bounds bounds = rend.bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents * seedSpread;

        // 1. 隨機灑種子點
        List<Vector3> seeds = new List<Vector3>();
        for (int i = 0; i < seedCount; i++)
        {
            Vector3 p = center + new Vector3(
                Random.Range(-extents.x, extents.x),
                Random.Range(-extents.y, extents.y),
                Random.Range(-extents.z, extents.z));
            seeds.Add(p);
        }

        List<GameObject> result = new List<GameObject>();

        // 2. 每個種子各自算出自己的 Voronoi cell
        for (int i = 0; i < seeds.Count; i++)
        {
            GameObject piece = gameObject;
            bool pieceIsIntermediate = false; 
            bool valid = true;

            for (int j = 0; j < seeds.Count; j++)
            {
                if (i == j) continue;

                Vector3 midPoint = (seeds[i] + seeds[j]) * 0.5f;
                Vector3 planeNormal = (seeds[j] - seeds[i]).normalized;

                SlicedHull hull = piece.Slice(midPoint, planeNormal, insideMaterial);

                if (hull == null)
                {
                    // 用種子 i 本身判斷該側是否為「保留側」。
                    float side = Vector3.Dot(seeds[i] - midPoint, planeNormal);
                    if (side > 0f)
                    {
                        // 種子 i 落在被排除的那一側 → 這個 cell 被其他種子完全蓋過，不存在
                        valid = false;
                        break;
                    }
                    // 否則 piece 本來就整塊在保留側，維持不變，繼續下一刀
                    continue;
                }

                GameObject upper = hull.CreateUpperHull(piece, insideMaterial);
                GameObject lower = hull.CreateLowerHull(piece, insideMaterial);

                // 用種子 i 判斷該保留 upper 還是 lower
                float upperSide = Vector3.Dot(seeds[i] - midPoint, planeNormal);
                GameObject keep = upperSide > 0f ? upper : lower;
                GameObject discard = upperSide > 0f ? lower : upper;

                if (discard != null) Destroy(discard);
                if (pieceIsIntermediate && piece != null) Destroy(piece);

                piece = keep;
                pieceIsIntermediate = true;

                if (piece == null) { valid = false; break; }
            }

            if (valid && piece != null && piece != gameObject)
            {
                result.Add(piece);
            }
            else if (pieceIsIntermediate && piece != null && piece != gameObject)
            {
                Destroy(piece); // 中途判定無效，清掉殘留的中間物件
            }
        }

        return result;
    }
}