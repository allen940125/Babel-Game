using Cysharp.Threading.Tasks;
using Game.Input;
using Game.UI;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 通用的玩家探索控制（任何探索場景都能掛）：
///   WASD / 方向鍵：移動（X-Z 平面，預設依攝影機方向）
///   F：調查附近最近的 InteractableObject（會開對話框）
///   E：開 / 關背包（走同事的 UIManager.BagMenu）
///   ESC：關閉背包 > 開 / 關選單（PauseMenuPanel）
///
/// 對話進行中、背包開啟中、選單開啟中、被外部鎖定時，玩家不能移動也不能再觸發其他操作。
/// 注意：不要跟同事的 PlayerAdventureController 掛在同一個物件上，兩者都會寫 Rigidbody 速度。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerExplorationController : MonoBehaviour
{
    [Header("移動（WASD / 方向鍵）")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("勾選：移動方向依鏡頭決定（固定鏡頭請勾選）；取消：固定用世界座標（W = +Z、D = +X）")]
    [SerializeField] private bool cameraRelative = true;
    [Tooltip("勾選：W = 往靠近鏡頭的方向、S = 遠離鏡頭（A / D 一律是鏡頭的左 / 右）。\n取消：W = 遠離鏡頭（一般第三人稱的習慣）。")]
    [SerializeField] private bool wTowardCamera = true;
    [Tooltip("指定用哪個鏡頭決定方向。留空就用 Tag 是 MainCamera 的那台。")]
    [SerializeField] private Transform cameraOverride;
    [Tooltip("角色是否轉向移動方向（攝影機是玩家子物件時請取消勾選，否則畫面會跟著轉）")]
    [SerializeField] private bool faceMoveDirection = false;
    [SerializeField] private float turnSpeed = 720f;

    [Header("調查（F）")]
    [SerializeField] private Key interactKey = Key.F;
    [Tooltip("預設的『碰到』容許距離（公尺）：玩家碰撞體表面到物件碰撞體表面的水平空隙，小於等於這個值才算碰到。0.2~0.4 = 貼著物件才能調查。個別物件可以在自己身上覆寫。")]
    [SerializeField] private float touchMargin = 0.3f;
    [Tooltip("勾選後，目前可調查的物件換人時會在 Console 印出物件名稱，用來找出『為什麼這裡也能調查』。")]
    [SerializeField] private bool debugLogTarget = true;

    [Header("背包（E）")]
    [SerializeField] private Key bagKey = Key.E;
    [Tooltip("取消勾選 = 這支腳本不處理背包鍵。如果同事的輸入系統已經用同一個鍵開背包，請取消勾選，避免重複處理。")]
    [SerializeField] private bool handleBagKey = true;
    [Tooltip("背包關閉後，把同事的輸入系統切回 Player 模式。\n背包開啟時他的 UI 會把輸入切到 UI 模式，但關閉時沒有切回來，會導致他綁在 E 上的『開背包』按鍵失效。")]
    [SerializeField] private bool restorePlayerInputOnBagClose = true;

    [Header("選單（ESC）")]
    [Tooltip("ESC 選單面板（掛 PauseMenuPanel 的物件）。留空就不會有選單。")]
    [SerializeField] private PauseMenuPanel pauseMenu;
    [SerializeField] private Key menuKey = Key.Escape;
    [Tooltip("取消勾選 = 這支腳本不處理 ESC。")]
    [SerializeField] private bool handleMenuKey = true;

    [Header("動畫（可選）")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkBoolParam = "IsWalking";

    private Rigidbody rb;
    private Camera cam;
    private bool warnedNoCamera;

    private InteractableObject currentTarget;
    private Vector3 moveDirection;

    private bool externalLock;
    private bool startingInteraction;
    private bool wasDialoguePlaying;
    private bool bagWasOpen;

    /// <summary>被外部鎖住（例如過場演出）。對話、背包、選單造成的鎖定不算在內。</summary>
    public bool IsLockedExternally => externalLock;

    /// <summary>切場景演出、過場動畫時可呼叫，鎖住所有操作並讓角色停下。</summary>
    public void SetControlLocked(bool locked)
    {
        externalLock = locked;
        if (locked) StopHorizontalMotion();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void OnDisable()
    {
        SetCurrentTarget(null);
        StopHorizontalMotion();
        SetWalkAnim(false);
    }

    private void Update()
    {
        var kb = Keyboard.current;

        bool dialoguePlaying = IsDialoguePlaying() || ShowcaseOverlay.IsShowing; // 大立繪展示中也視為鎖定（不能移動 / 調查 / 開選單）
        bool dialogueJustEnded = wasDialoguePlaying && !dialoguePlaying;
        wasDialoguePlaying = dialoguePlaying;

        bool bagOpen = IsBagOpen();
        bool menuOpen = pauseMenu != null && pauseMenu.IsOpen;

        // 1. ESC：優先順序 = 關選單 > 關背包 > 開選單
        if (kb != null && handleMenuKey && kb[menuKey].wasPressedThisFrame)
        {
            if (menuOpen)
            {
                pauseMenu.Close();
                menuOpen = false;
            }
            else if (bagOpen || bagWasOpen)
            {
                // 這一下 ESC 是拿來關背包的，不能同時又把選單打開。
                // 用 bagWasOpen 一併判斷：萬一同事的 UIManager 搶先在這一幀把背包關掉了，也不會誤開選單。
                if (bagOpen)
                {
                    CloseBag();
                    bagOpen = IsBagOpen();
                }
            }
            else if (pauseMenu != null && !externalLock && !dialoguePlaying && !startingInteraction)
            {
                pauseMenu.Open();
                menuOpen = true;
            }
        }

        // 2. E：對話中、選單開著、被外部鎖定時不能開；背包本身開著時可以按 E 關閉
        if (kb != null && handleBagKey && !externalLock && !dialoguePlaying
            && !startingInteraction && !menuOpen && kb[bagKey].wasPressedThisFrame)
        {
            ToggleBag();
            bagOpen = IsBagOpen();
        }

        // 背包從開著變成關閉（不管是我們關的、ESC 關的，還是背包自己的按鈕關的）
        if (bagWasOpen && !bagOpen && restorePlayerInputOnBagClose)
            RestorePlayerInput();

        bagWasOpen = bagOpen;

        bool locked = externalLock || dialoguePlaying || startingInteraction || bagOpen || menuOpen;

        // 3. 移動
        if (locked || kb == null)
        {
            moveDirection = Vector3.zero;
        }
        else
        {
            moveDirection = ReadMoveDirection(kb);
        }
        SetWalkAnim(moveDirection.sqrMagnitude > 0.01f);

        // 4. 調查（F）
        if (locked || kb == null)
        {
            SetCurrentTarget(null);
            return;
        }

        SetCurrentTarget(FindNearestInteractable());

        // 對話視窗用 F 當「下一句」。結束對話的那一下 F 不能再當成新的調查，
        // 否則會結束後立刻又重新開始同一段對話。
        if (dialogueJustEnded) return;

        if (currentTarget != null && kb[interactKey].wasPressedThisFrame)
        {
            var target = currentTarget;
            SetCurrentTarget(null);
            InteractAsync(target).Forget();
        }
    }

    private void FixedUpdate()
    {
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = rb.linearVelocity.y; // 保留重力
        rb.linearVelocity = velocity;

        if (faceMoveDirection && moveDirection.sqrMagnitude > 0.01f)
        {
            var targetRot = Quaternion.LookRotation(moveDirection, Vector3.up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
        }
    }

    // ───────────── 移動 ─────────────

    private Vector3 ReadMoveDirection(Keyboard kb)
    {
        float h = 0f, v = 0f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;

        if (h == 0f && v == 0f) return Vector3.zero;

        // 預設用世界座標（找不到鏡頭或沒勾 Camera Relative 時）
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraRelative)
        {
            Transform camTransform = ResolveCameraTransform();
            if (camTransform != null)
            {
                // 鏡頭的「前方」壓平到地面上；鏡頭幾乎垂直往下看時，改用鏡頭的「上方」當前方
                forward = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up);
                if (forward.sqrMagnitude < 0.0001f)
                    forward = Vector3.ProjectOnPlane(camTransform.up, Vector3.up);
                forward.Normalize();

                right = Vector3.ProjectOnPlane(camTransform.right, Vector3.up).normalized;

                // W = 靠近鏡頭（鏡頭前方的反方向）
                if (wTowardCamera) forward = -forward;
            }
            else if (!warnedNoCamera)
            {
                warnedNoCamera = true;
                Debug.LogWarning("[PlayerExplorationController] 找不到鏡頭（沒有 Tag 為 MainCamera 的攝影機，也沒有指定 Camera Override），" +
                                 "改用世界座標移動，方向可能跟畫面對不上。");
            }
        }

        Vector3 dir = forward * v + right * h;
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        return dir;
    }

    private Transform ResolveCameraTransform()
    {
        if (cameraOverride != null) return cameraOverride;
        if (cam == null) cam = Camera.main;
        return cam != null ? cam.transform : null;
    }

    private void StopHorizontalMotion()
    {
        moveDirection = Vector3.zero;
        if (rb != null)
        {
            var vel = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, vel.y, 0f);
        }
    }

    private void SetWalkAnim(bool walking)
    {
        if (animator != null && !string.IsNullOrEmpty(walkBoolParam))
            animator.SetBool(walkBoolParam, walking);
    }

    // ───────────── 調查 ─────────────

    private InteractableObject FindNearestInteractable()
    {
        InteractableObject best = null;
        float bestDistance = float.MaxValue;
        Bounds myBounds = GetMyBounds();

        foreach (var candidate in InteractableObject.Active)
        {
            if (candidate == null || !candidate.CanInteract) continue;

            // 玩家碰撞體表面 ↔ 物件碰撞體表面 的水平空隙（0 = 已經貼在一起）
            float distance = candidate.GapTo(myBounds);
            if (distance > candidate.GetTouchMargin(touchMargin)) continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }
        return best;
    }

    private Collider[] myColliders;

    /// <summary>玩家自己所有 Collider 合起來的範圍（用來算「有沒有碰到物件」）。</summary>
    private Bounds GetMyBounds()
    {
        if (myColliders == null || myColliders.Length == 0)
            myColliders = GetComponentsInChildren<Collider>();

        bool has = false;
        Bounds b = new Bounds(transform.position, Vector3.zero);
        foreach (var c in myColliders)
        {
            if (c == null || !c.enabled || c.isTrigger) continue;
            if (!has) { b = c.bounds; has = true; }
            else b.Encapsulate(c.bounds);
        }
        return b;
    }

    private void SetCurrentTarget(InteractableObject target)
    {
        if (currentTarget == target) return;
        if (currentTarget != null) currentTarget.SetPromptVisible(false);
        currentTarget = target;

        if (debugLogTarget && target != null)
            Debug.Log($"[PlayerExploration] 可調查：{target.name}", target);
        if (currentTarget != null) currentTarget.SetPromptVisible(true);
    }

    private async UniTaskVoid InteractAsync(InteractableObject target)
    {
        // 對話是非同步載入的，這段空窗期也要擋住，避免連按 F 重複開啟
        startingInteraction = true;
        try
        {
            await target.Interact();
        }
        finally
        {
            startingInteraction = false;
        }
    }

    // ───────────── 對話 / 背包 ─────────────

    private static bool IsDialoguePlaying()
    {
        var dc = Dialoguecontroller.Instance;
        return dc != null && dc.DialogueIsPlaying;
    }

    private static UIManager GetUIManager()
    {
        var gm = GameManager.Instance;
        return gm != null ? gm.UIManager : null;
    }

    private static bool IsBagOpen()
    {
        var ui = GetUIManager();
        return ui != null && ui.IsPanelOpen(UIType.BagMenu);
    }

    private static void ToggleBag()
    {
        var ui = GetUIManager();
        if (ui == null) return;

        if (ui.IsPanelOpen(UIType.BagMenu))
            ui.ClosePanel(UIType.BagMenu);
        else
            ui.OpenPanel<BasePanel>(UIType.BagMenu).Forget();
    }

    private static void RestorePlayerInput()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.InputManagers != null)
            gm.InputManagers.SetInputActive(InputType.Player);
    }

    private static void CloseBag()
    {
        var ui = GetUIManager();
        if (ui != null && ui.IsPanelOpen(UIType.BagMenu))
            ui.ClosePanel(UIType.BagMenu);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        var cols = GetComponentsInChildren<Collider>();
        if (cols.Length == 0) return;
        Bounds b = cols[0].bounds;
        foreach (var c in cols) if (!c.isTrigger) b.Encapsulate(c.bounds);
        b.Expand(new Vector3(touchMargin * 2f, 0f, touchMargin * 2f));
        Gizmos.DrawWireCube(b.center, b.size);
    }
}