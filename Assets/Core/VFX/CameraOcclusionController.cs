using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class CameraOcclusionController : MonoBehaviour
{
    [Header("目標設定")]
    public Transform target;
    public LayerMask obstacleLayer;

    // NonAlloc arrays
    private RaycastHit[] sphereHits = new RaycastHit[64];
    private RaycastHit[] rayHits = new RaycastHit[64];

    private HashSet<FadeObject> currentObstacles = new HashSet<FadeObject>();
    private HashSet<FadeObject> previousObstacles = new HashSet<FadeObject>();
    private HashSet<FadeObject> activeFaders = new HashSet<FadeObject>();
    private List<FadeObject> fadersToRemove = new List<FadeObject>(8);

    // Collider -> FadeObject 快取
    private Dictionary<Collider, FadeObject> colliderCache = new Dictionary<Collider, FadeObject>(256);

    [Header("射線參數")]
    public float sphereRadius = 0.3f;
    public float raycastStartOffset = 0.01f;

    [Header("Performance")]
    [Tooltip("0 = 不限制；>0 表示對 spherecast 的候選做額外檢查時最多檢查多少個")]
    public int maxExtraChecks = 8;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 camPos = transform.position;
        Vector3 targetPos = target.position;
        Vector3 dir = targetPos - camPos;
        float dist = dir.magnitude;
        if (dist <= Mathf.Epsilon) return;

        currentObstacles.Clear();

        // 1) RaycastNonAlloc -> 取得沿線的所有 collider（處理堆疊遮擋）
        int rcount = Physics.RaycastNonAlloc(camPos + dir.normalized * raycastStartOffset, dir.normalized, rayHits, dist - raycastStartOffset, obstacleLayer, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < rcount; i++)
        {
            var rh = rayHits[i];
            if (rh.collider == null) continue;
            FadeObject fo = GetCachedFadeObject(rh.collider);
            if (fo != null)
            {
                currentObstacles.Add(fo);
            }
        }

        // 2) SphereCastNonAlloc -> 補上 raycast 沒掃到但仍接近視線的物件（例如寬物體側邊）
        int scount = Physics.SphereCastNonAlloc(camPos, sphereRadius, dir.normalized, sphereHits, dist, obstacleLayer, QueryTriggerInteraction.Ignore);

        if (scount > 0)
        {
            // 建立候選 (collider, distance) 並依距離排序（近的先檢查）
            var candidates = new List<(Collider col, float d)>(scount);
            for (int i = 0; i < scount; i++)
            {
                var sh = sphereHits[i];
                if (sh.collider == null) continue;
                // 如果已在 rayHits 裡面（也就是已被加入 currentObstacles），可略過
                FadeObject known = GetCachedFadeObject(sh.collider);
                if (known != null && currentObstacles.Contains(known)) continue;

                candidates.Add((sh.collider, sh.distance));
            }

            if (candidates.Count > 0)
            {
                candidates.Sort((a, b) => a.d.CompareTo(b.d));

                int checks = 0;
                foreach (var c in candidates)
                {
                    if (maxExtraChecks > 0 && checks >= maxExtraChecks) break;

                    Collider col = c.col;
                    FadeObject fo = GetCachedFadeObject(col);
                    if (fo == null) { checks++; continue; }

                    // 取 collider 最接近 camera 的點，或取 ClosestPoint 到視線方向的中點
                    // 我們先試 collider.ClosestPoint(camPos) —— 若 ray 從 camera 打到那個點且第一 hit 是該 collider -> 視為遮擋
                    Vector3 cp = col.ClosestPoint(camPos);
                    Vector3 fromCam = cp - camPos;
                    float dToCp = fromCam.magnitude;
                    if (dToCp <= Mathf.Epsilon) { checks++; continue; }

                    RaycastHit firstHit;
                    bool got = Physics.Raycast(camPos + dir.normalized * raycastStartOffset, (cp - (camPos + dir.normalized * raycastStartOffset)).normalized, out firstHit, Mathf.Min(dToCp, dist - raycastStartOffset), obstacleLayer, QueryTriggerInteraction.Ignore);
                    if (got && firstHit.collider != null)
                    {
                        // 如果第一個 hit 的 FadeObject 是我們的 fo，代表 camera->cp 直線上它未被其他物件遮擋
                        FadeObject firstFo = GetCachedFadeObject(firstHit.collider);
                        if (firstFo == fo)
                        {
                            currentObstacles.Add(fo);
                        }
                    }
                    checks++;
                }
            }
        }

        // 3) 新增/移除處理（同你原本邏輯）
        foreach (var fo in currentObstacles)
        {
            if (!previousObstacles.Contains(fo))
            {
                fo.StartFade();
                activeFaders.Add(fo);
            }
        }

        foreach (var fo in previousObstacles)
        {
            if (fo != null && !currentObstacles.Contains(fo))
            {
                fo.StopFade();
                activeFaders.Add(fo);
            }
        }

        // 交換集合
        var tmp = previousObstacles;
        previousObstacles = currentObstacles;
        currentObstacles = tmp;
    }

    void Update()
    {
        if (activeFaders.Count == 0) return;

        fadersToRemove.Clear();
        float dt = Time.deltaTime;

        foreach (var fader in activeFaders)
        {
            if (fader == null)
            {
                fadersToRemove.Add(fader);
                continue;
            }
            if (!fader.DoFadeUpdate(dt))
            {
                fadersToRemove.Add(fader);
            }
        }

        foreach (var f in fadersToRemove)
        {
            activeFaders.Remove(f);
        }
    }

    private FadeObject GetCachedFadeObject(Collider col)
    {
        if (col == null) return null;
        FadeObject fo;
        if (colliderCache.TryGetValue(col, out fo)) return fo;
        fo = col.GetComponentInParent<FadeObject>();
        colliderCache[col] = fo; // 可能為 null，但可減少重複查找
        return fo;
    }

    public void ClearColliderCache() => colliderCache.Clear();
}
