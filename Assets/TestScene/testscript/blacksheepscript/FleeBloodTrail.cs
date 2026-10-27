using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 重傷的血跡：玩家移動時，在他走過的地方留下血跡。血跡會跟著地板一起往下捲，捲出畫面就消失。
/// 站著不動時可以選擇偶爾滴一滴血（Idle Drip Interval，0 = 不滴）。
///
/// 用法：掛在任何物件上（例如 FleeSystem）。Background 拖掛了 ScrollingBackground2D 的 Bg。
/// Blood Sprites 放你的血跡圖；留空的話會自動用一個暗紅色的柔邊圓點代替，可以先拿來測試位置和節奏。
/// </summary>
public class FleeBloodTrail : MonoBehaviour
{
    [Header("對象")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("血跡跟著這個背景的捲動速度往下移動。")]
    [SerializeField] private ScrollingBackground2D background;
    [SerializeField] private Camera targetCamera;

    [Header("血跡外觀")]
    [Tooltip("血跡圖。每次隨機挑一張。留空 = 自動用暗紅色圓點。")]
    [SerializeField] private List<Sprite> bloodSprites = new List<Sprite>();
    [SerializeField] private Color bloodColor = new Color(0.45f, 0.02f, 0.02f, 0.85f);
    [Tooltip("每滴血的大小範圍（隨機）。")]
    [SerializeField] private Vector2 scaleRange = new Vector2(0.25f, 0.5f);
    [SerializeField] private bool randomRotation = true;
    [Tooltip("位置隨機偏移多少，讓血跡不要排得太整齊。")]
    [SerializeField] private float jitter = 0.12f;
    [Tooltip("血跡生成點相對玩家中心的位置（通常在腳邊，Y 填負的）。")]
    [SerializeField] private Vector2 footOffset = new Vector2(0f, -0.25f);

    [Header("生成節奏")]
    [Tooltip("玩家每移動這麼遠就留一滴血。越小血跡越連續。")]
    [SerializeField] private float spacing = 0.5f;
    [Tooltip("站著不動時，每隔幾秒滴一滴血。0 = 不滴。")]
    [SerializeField] private float idleDripInterval = 1.5f;
    [Tooltip("站著滴的血相對大小倍率。")]
    [SerializeField] private float idleDripScale = 0.6f;
    [Tooltip("血跡存在幾秒後淡出消失。0 = 不淡出，捲出畫面才消失。")]
    [SerializeField] private float lifetimeSeconds = 0f;
    [SerializeField] private int maxStains = 150;

    [Header("排序")]
    [Tooltip("血跡的 Order in Layer = 玩家圖的 Order + 這個數字。負數 = 在玩家後面。如果血跡被地板蓋住，改成 0 或正數。")]
    [SerializeField] private int sortingOrderOffset = -1;

    private class Stain
    {
        public Transform t;
        public SpriteRenderer sr;
        public float age;
        public float baseAlpha;
    }

    private readonly List<Stain> stains = new List<Stain>();
    private Transform container;
    private Transform playerRoot;
    private SpriteRenderer playerSprite;
    private Vector3 lastPos;
    private bool hasLast;
    private float idleTimer;
    private Sprite fallbackSprite;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        container = new GameObject("BloodTrail").transform;
    }

    private void OnDestroy()
    {
        if (container != null) Destroy(container.gameObject);
    }

    private void Update()
    {
        if (!FindPlayer()) return;

        Vector3 pos = playerRoot.position;
        pos.z = 0f;
        if (!hasLast) { lastPos = pos; hasLast = true; }

        // 依移動距離留血跡（沿著移動路徑補齊，快速移動也不會斷）
        Vector3 delta = pos - lastPos;
        float dist = delta.magnitude;
        if (dist >= spacing && spacing > 0.01f)
        {
            int n = Mathf.Min(12, Mathf.FloorToInt(dist / spacing));
            for (int i = 1; i <= n; i++)
            {
                Vector3 p = Vector3.Lerp(lastPos, pos, i * spacing / dist);
                Spawn(p, 1f);
            }
            lastPos = Vector3.Lerp(lastPos, pos, n * spacing / dist);
            idleTimer = 0f;
        }
        else
        {
            // 站著不動：偶爾滴一滴
            if (idleDripInterval > 0f)
            {
                idleTimer += Time.deltaTime;
                if (idleTimer >= idleDripInterval)
                {
                    idleTimer = 0f;
                    Spawn(pos, idleDripScale);
                }
            }
        }

        UpdateStains();
    }

    private bool FindPlayer()
    {
        if (playerRoot != null) return true;

        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go == null) return false;

        var rb = go.GetComponentInParent<Rigidbody>();
        playerRoot = rb != null ? rb.transform : go.transform.root;
        playerSprite = playerRoot.GetComponentInChildren<SpriteRenderer>();
        return true;
    }

    private void Spawn(Vector3 center, float sizeMultiplier)
    {
        Vector3 p = center + new Vector3(footOffset.x, footOffset.y, 0f);
        p += new Vector3(Random.Range(-jitter, jitter), Random.Range(-jitter, jitter), 0f);

        var go = new GameObject("Blood");
        go.transform.SetParent(container, true);
        go.transform.position = p;
        go.transform.rotation = randomRotation ? Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)) : Quaternion.identity;
        float s = Random.Range(scaleRange.x, scaleRange.y) * sizeMultiplier;
        go.transform.localScale = new Vector3(s, s, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PickSprite();
        sr.color = bloodColor;
        if (playerSprite != null)
        {
            sr.sortingLayerID = playerSprite.sortingLayerID;
            sr.sortingOrder = playerSprite.sortingOrder + sortingOrderOffset;
        }

        stains.Add(new Stain { t = go.transform, sr = sr, age = 0f, baseAlpha = bloodColor.a });

        // 超過上限就移除最舊的
        while (stains.Count > maxStains)
        {
            if (stains[0].t != null) Destroy(stains[0].t.gameObject);
            stains.RemoveAt(0);
        }
    }

    private void UpdateStains()
    {
        float speed = background != null ? background.CurrentScrollSpeed : 0f;

        for (int i = stains.Count - 1; i >= 0; i--)
        {
            var st = stains[i];
            if (st.t == null) { stains.RemoveAt(i); continue; }

            st.age += Time.deltaTime;
            st.t.position += Vector3.down * (speed * Time.deltaTime);

            if (lifetimeSeconds > 0f)
            {
                float a = st.baseAlpha * Mathf.Clamp01(1f - st.age / lifetimeSeconds);
                var c = st.sr.color; c.a = a; st.sr.color = c;
                if (st.age >= lifetimeSeconds) { Destroy(st.t.gameObject); stains.RemoveAt(i); continue; }
            }

            if (targetCamera != null && targetCamera.WorldToViewportPoint(st.t.position).y < -0.2f)
            {
                Destroy(st.t.gameObject);
                stains.RemoveAt(i);
            }
        }
    }

    private Sprite PickSprite()
    {
        if (bloodSprites != null && bloodSprites.Count > 0)
        {
            var s = bloodSprites[Random.Range(0, bloodSprites.Count)];
            if (s != null) return s;
        }
        if (fallbackSprite == null) fallbackSprite = MakeSoftCircle();
        return fallbackSprite;
    }

    /// <summary>沒指定血跡圖時用的暗紅色柔邊圓點（白色，顏色由 Blood Color 上色）。</summary>
    private static Sprite MakeSoftCircle()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a); // 柔邊
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
