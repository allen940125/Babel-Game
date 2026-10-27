using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 往下捲動的無限背景（讓玩家看起來一直往上跑）。
/// 把「底圖」物件（例如 GameScene 的 Bg）拖進 Tile，勾選 Auto Duplicate 就會自動複製出上下相接的幾份，
/// 不用手動擺。以固定速度（Constant Scroll Speed）一直往下捲。
///
/// 注意：底圖需要「上下可以無縫接」才不會看到接縫。接不起來的話勾 Mirror Alternate，
/// 每隔一份上下翻轉，接縫處會變成鏡像，看起來連續（適合有紋理的背景）。
/// 邊框、牆壁不要放進來，它們固定不動。
/// </summary>
public class ScrollingBackground2D : MonoBehaviour
{
    [Tooltip("每秒往下捲多少單位（一直捲，不看玩家按鍵）。")]
    [SerializeField] private float constantScrollSpeed = 3f;

    [Header("底圖")]
    [Tooltip("要捲動的底圖。Auto Duplicate 勾選時只需要放一個。")]
    [SerializeField] private Transform tile;
    [Tooltip("自動複製出上下相接的幾份（含原本那份，建議 3）。")]
    [SerializeField] private bool autoDuplicate = true;
    [SerializeField, Range(2, 6)] private int copies = 3;
    [Tooltip("一份底圖在世界裡的高度。0 = 自動用 SpriteRenderer 的實際高度。")]
    [SerializeField] private float tileHeightOverride = 0f;
    [Tooltip("每隔一份上下翻轉，讓接縫變成鏡像。")]
    [SerializeField] private bool mirrorAlternate = false;

    [Header("速度")]
    [Tooltip("捲動速度倍率。底圖很大時可以調小一點，製造遠景感。")]
    [SerializeField] private float speedMultiplier = 1f;

    /// <summary>目前每秒往下捲多少單位（含倍率）。破碎 UI 掩體用它跟著地板一起移動。</summary>
    public float CurrentScrollSpeed => constantScrollSpeed * speedMultiplier;

    private readonly List<Transform> tiles = new List<Transform>();
    private float height;
    private float baseY;
    private float offset;

    private void Start()
    {
        if (tile == null)
        {
            Debug.LogWarning("[ScrollingBackground2D] 尚未指定 Tile。", this);
            enabled = false;
            return;
        }

        var sr = tile.GetComponentInChildren<SpriteRenderer>();
        height = tileHeightOverride > 0f ? tileHeightOverride : (sr != null ? sr.bounds.size.y : 0f);
        if (height <= 0.01f)
        {
            Debug.LogWarning("[ScrollingBackground2D] 算不出底圖高度，請填 Tile Height Override。", this);
            enabled = false;
            return;
        }

        baseY = tile.position.y;
        tiles.Add(tile);

        if (autoDuplicate)
        {
            for (int i = 1; i < copies; i++)
            {
                var clone = Instantiate(tile.gameObject, tile.parent);
                clone.name = tile.name + "_scroll" + i;

                // 複製品會連同這個腳本一起被複製。必須立刻關掉，否則它們的 Start 會再各自複製，
                // 造成無限增生。（如果腳本就掛在 Tile 本身上，就會發生這件事。）
                foreach (var copyScript in clone.GetComponentsInChildren<ScrollingBackground2D>(true))
                    copyScript.enabled = false;
                tiles.Add(clone.transform);
            }
        }

        Layout();
    }

    private void Update()
    {
        if (tiles.Count == 0) return;

        float velocity = constantScrollSpeed;
        offset -= velocity * speedMultiplier * Time.deltaTime;
        offset = Mathf.Repeat(offset, height); // 保持在 [0, height)，永遠接得上
        Layout();
    }

    private void Layout()
    {
        // 第 i 份：baseY + offset + (i - 1) * height。offset 遞減 → 整體往下捲，
        // 最下面那份移出畫面時會被 Repeat 無縫換成上方那份。
        for (int i = 0; i < tiles.Count; i++)
        {
            var t = tiles[i];
            var p = t.position;
            p.y = baseY + offset + (i - 1) * height;
            t.position = p;

            if (mirrorAlternate)
            {
                var s = t.localScale;
                float sign = (i % 2 == 0) ? 1f : -1f;
                s.y = Mathf.Abs(s.y) * sign;
                t.localScale = s;
            }
        }
    }
}