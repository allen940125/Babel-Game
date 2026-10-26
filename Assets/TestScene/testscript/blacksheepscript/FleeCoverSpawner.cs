using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 破碎 UI 掩體生成器：隨機間隔從畫面上方生成掩體，跟著地板往下捲。
/// 另外提供 SpawnAt()，讓 QTE 成功時附近沒有掩體也能立刻生出一個讓玩家躲。
/// </summary>
public class FleeCoverSpawner : MonoBehaviour
{
    [SerializeField] private List<FleeCover> coverPrefabs = new List<FleeCover>();
    [Tooltip("掩體跟著這個背景的捲動速度往下移動。")]
    [SerializeField] private ScrollingBackground2D background;
    [Tooltip("掩體捲動速度的倍率。若背景與玩家的景深不同看起來速度不一致，用這個微調。")]
    [SerializeField] private float coverSpeedMultiplier = 1f;

    [Header("自動生成")]
    [SerializeField] private bool autoSpawn = true;
    [SerializeField] private Vector2 intervalRange = new Vector2(3f, 6f);
    [Tooltip("X 位置範圍（畫面比例 0~1）。")]
    [SerializeField] private Vector2 xRange = new Vector2(0.15f, 0.85f);
    [Tooltip("生成的高度（畫面比例）。大於 1 = 畫面上方之外。")]
    [SerializeField] private float topViewportY = 1.15f;

    [Tooltip("掩體生成時的 Z 位置。0 = 跟玩家同一個平面。想讓掩體在玩家前面（鏡頭那一側）可以微調，例如 -0.3。")]
    [SerializeField] private float coverZ = 0f;

    [SerializeField] private Camera targetCamera;

    public Camera TargetCamera => targetCamera;

    private int loopVersion;
    private bool running;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
    }

    public void StartLoop()
    {
        if (running || !autoSpawn) return;
        running = true;
        Loop(++loopVersion).Forget();
    }

    public void StopLoop()
    {
        running = false;
        loopVersion++;
    }

    /// <summary>在指定世界座標生成一個隨機掩體並回傳。</summary>
    public FleeCover SpawnAt(Vector3 worldPosition)
    {
        var prefab = PickPrefab();
        if (prefab == null) return null;

        worldPosition.z = coverZ;
        var cover = Instantiate(prefab, worldPosition, Quaternion.identity);
        cover.Setup(background, coverSpeedMultiplier, targetCamera);
        return cover;
    }

    /// <summary>在畫面上方、指定的水平位置（0~1）生成一個掩體。時間軸用它做「有規律」的出現。</summary>
    public FleeCover SpawnAtViewport(float viewportX)
    {
        return SpawnAt(FleeView.ViewportToPlane(targetCamera, viewportX, topViewportY));
    }

    private async UniTaskVoid Loop(int version)
    {
        var token = this.GetCancellationTokenOnDestroy();

        while (version == loopVersion)
        {
            float wait = UnityEngine.Random.Range(Mathf.Min(intervalRange.x, intervalRange.y), Mathf.Max(intervalRange.x, intervalRange.y));
            await UniTask.Delay(TimeSpan.FromSeconds(wait), cancellationToken: token);
            if (version != loopVersion) return;

            float vx = UnityEngine.Random.Range(xRange.x, xRange.y);
            SpawnAt(FleeView.ViewportToPlane(targetCamera, vx, topViewportY));
        }
    }

    private FleeCover PickPrefab()
    {
        if (coverPrefabs == null || coverPrefabs.Count == 0)
        {
            Debug.LogWarning("[FleeCoverSpawner] Cover Prefabs 是空的。", this);
            return null;
        }
        return coverPrefabs[UnityEngine.Random.Range(0, coverPrefabs.Count)];
    }
}