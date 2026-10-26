using UnityEngine;

/// <summary>
/// 2D 立繪角色在 3D 場景裡的「轉向」：
///   - 永遠正對鏡頭（只繞 Y 軸，不會躺下或歪掉）
///   - 依「相對於鏡頭的移動方向」切換圖片：
///       朝鏡頭走（畫面往下）→ 正面圖
///       遠離鏡頭走（畫面往上）→ 背面圖
///       往左 / 右走 → 側面圖（往另一邊就左右翻轉）
///   - 停下來時保持最後的朝向
///
/// 不依賴任何移動腳本：用位置變化判斷移動方向，所以探索移動（PlayerExplorationController）、
/// RunnerMover、甚至別的腳本推動角色都可以用。
///
/// 掛法：掛在「有 SpriteRenderer 的那個子物件」上。
///       Movement Root 指定真正會移動的物件（通常是 Player）；留空 = 自動用父物件，沒有父物件就用自己。
/// 注意：PlayerExplorationController 的 Face Move Direction 要保持取消勾選（那會把整個物件旋轉，2D 圖會變成歪的）。
///
/// 如果你的角色是用 Animator 切動畫而不是換圖片，把 Mode 改成 AnimatorParams，
/// 腳本會每幀設定 MoveX / MoveY / IsMoving 三個參數給 Animator（相對於鏡頭，MoveY = -1 代表朝向鏡頭走）。
/// </summary>
public class Sprite2DFacing : MonoBehaviour
{
    public enum Mode { SpriteSwap, AnimatorParams }

    [SerializeField] private Mode mode = Mode.SpriteSwap;

    [Header("對象")]
    [Tooltip("真正會移動的物件（Player）。留空 = 父物件，沒有就用自己。")]
    [SerializeField] private Transform movementRoot;
    [Tooltip("指定哪個鏡頭決定方向。留空 = Camera.main。")]
    [SerializeField] private Camera cam;

    [Header("位置跟隨（圖片物件不是 Player 的子物件時才需要）")]
    [Tooltip("勾選後，每幀把這個物件移到 Movement Root 的位置 + Follow Offset。\n"
           + "用在：Player 的縮放不是 1（會把圖壓扁），所以把圖片物件放在場景根目錄、不當子物件的情況。\n"
           + "此時 Movement Root 一定要手動指定成 Player。")]
    [SerializeField] private bool followMovementRoot = false;
    [SerializeField] private Vector3 followOffset = Vector3.zero;

    [Header("正對鏡頭")]
    [SerializeField] private bool billboard = true;
    [Tooltip("勾選 = 只繞 Y 軸轉向鏡頭（推薦）；取消 = 完全和鏡頭平行（鏡頭俯視時圖片會跟著傾斜）。")]
    [SerializeField] private bool yAxisOnly = true;

    [Header("Sprite Swap 模式：圖片")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("朝向鏡頭（往畫面下方走）。")]
    [SerializeField] private Sprite front;
    [Tooltip("背對鏡頭（往畫面上方走）。留空 = 用正面圖。")]
    [SerializeField] private Sprite back;
    [Tooltip("側面。留空 = 用正面圖。")]
    [SerializeField] private Sprite side;
    [Tooltip("側面圖原本是朝『右』畫的就勾選；朝左畫的就取消。")]
    [SerializeField] private bool sideSpriteFacesRight = true;
    [Tooltip("只有正面圖時，往左右走要不要把正面圖左右翻轉。")]
    [SerializeField] private bool flipFrontWhenOnlyFront = false;

    [Header("Animator Params 模式")]
    [SerializeField] private Animator animator;
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string isMovingParam = "IsMoving";
    [Tooltip("勾選後，往側面走時會自動左右翻轉 SpriteRenderer（側面動畫只需要做一個方向）。")]
    [SerializeField] private bool flipSpriteInAnimatorMode = true;

    [Header("判斷")]
    [Tooltip("移動速度低於這個值（公尺 / 秒）視為停下，保持最後的朝向。")]
    [SerializeField] private float minSpeed = 0.1f;
    [Tooltip("斜著走時，前後與左右的比較偏向哪邊。>1 偏向前後（斜走時較容易顯示正 / 背面）。")]
    [SerializeField] private float verticalBias = 1f;

    public enum Facing { Front, Back, Side }
    public Facing CurrentFacing { get; private set; } = Facing.Front;
    /// <summary>目前是否往畫面右邊（側面時用）。</summary>
    public bool FacingRight { get; private set; } = true;

    private Vector3 lastPos;
    private bool hasLast;

    private void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponentInParent<Animator>();
    }

    private void Awake()
    {
        if (movementRoot == null) movementRoot = (!followMovementRoot && transform.parent != null) ? transform.parent : transform;
        if (followMovementRoot && movementRoot == transform)
            Debug.LogWarning("[Sprite2DFacing] 勾了 Follow Movement Root，但 Movement Root 沒有指定成 Player。", this);
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponentInParent<Animator>();
    }

    private void Start()
    {
        lastPos = movementRoot.position;
        hasLast = true;
        Apply(Facing.Front, true);
    }

    private void LateUpdate()
    {
        var c = ResolveCamera();
        if (c == null) return;

        if (followMovementRoot && movementRoot != null)
            transform.position = movementRoot.position + followOffset;

        // 1. 正對鏡頭
        if (billboard)
        {
            if (yAxisOnly)
            {
                Vector3 f = Vector3.ProjectOnPlane(c.transform.forward, Vector3.up);
                if (f.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);
            }
            else
            {
                transform.rotation = c.transform.rotation;
            }
        }

        // 2. 算移動方向（相對於鏡頭）
        Vector3 pos = movementRoot.position;
        Vector3 delta = hasLast ? pos - lastPos : Vector3.zero;
        lastPos = pos;
        hasLast = true;

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 vel = Vector3.ProjectOnPlane(delta / dt, Vector3.up);
        bool moving = vel.magnitude > minSpeed;

        Vector3 camFwd = Vector3.ProjectOnPlane(c.transform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(c.transform.right, Vector3.up).normalized;
        float x = Vector3.Dot(vel, camRight);   // + = 往畫面右
        float y = Vector3.Dot(vel, camFwd);     // + = 遠離鏡頭（往畫面上），- = 朝向鏡頭

        if (mode == Mode.AnimatorParams)
        {
            if (animator != null)
            {
                float mag = Mathf.Max(vel.magnitude, 0.0001f);
                if (!string.IsNullOrEmpty(moveXParam) && moving) animator.SetFloat(moveXParam, x / mag);
                if (!string.IsNullOrEmpty(moveYParam) && moving) animator.SetFloat(moveYParam, y / mag);
                if (!string.IsNullOrEmpty(isMovingParam)) animator.SetBool(isMovingParam, moving);
            }

            if (flipSpriteInAnimatorMode && spriteRenderer != null && moving)
            {
                bool sideMove = Mathf.Abs(y) * verticalBias < Mathf.Abs(x);
                if (sideMove)
                {
                    FacingRight = x > 0f;
                    spriteRenderer.flipX = sideSpriteFacesRight ? !FacingRight : FacingRight;
                }
                else
                {
                    spriteRenderer.flipX = false;
                }
            }
            return;
        }

        if (!moving) return; // 停下 → 保持最後朝向

        Facing next;
        if (Mathf.Abs(y) * verticalBias >= Mathf.Abs(x)) next = y < 0f ? Facing.Front : Facing.Back;
        else next = Facing.Side;

        if (Mathf.Abs(x) > 0.01f) FacingRight = x > 0f;
        Apply(next, false);
    }

    private void Apply(Facing facing, bool force)
    {
        if (spriteRenderer == null) return;
        if (!force && facing == CurrentFacing && !(facing == Facing.Side)) return;

        CurrentFacing = facing;

        switch (facing)
        {
            case Facing.Front:
                if (front != null) spriteRenderer.sprite = front;
                spriteRenderer.flipX = false;
                break;

            case Facing.Back:
                spriteRenderer.sprite = back != null ? back : front;
                spriteRenderer.flipX = false;
                break;

            case Facing.Side:
                if (side != null)
                {
                    spriteRenderer.sprite = side;
                    spriteRenderer.flipX = sideSpriteFacesRight ? !FacingRight : FacingRight;
                }
                else
                {
                    spriteRenderer.sprite = front;
                    spriteRenderer.flipX = flipFrontWhenOnlyFront && !FacingRight;
                }
                break;
        }
    }

    private Camera ResolveCamera()
    {
        if (cam != null) return cam;
        return Camera.main;
    }
}