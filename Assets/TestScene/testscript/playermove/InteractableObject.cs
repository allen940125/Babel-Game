using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 可被玩家調查的物件（取代 NpcInteractable）。
/// 玩家按 F 時，PlayerExplorationController 會在所有 InteractableObject 裡，
/// 找「玩家碰撞體表面離這個物件碰撞體表面」夠近（貼到旁邊）、而且最近的那一個，呼叫 Interact()。
///
/// 重點：要實際走到物件旁邊（空隙 ≤ Touch Margin）才算碰到，不是看遠遠的圓形範圍。
/// 物件需要有 Collider（可勾 Is Trigger，不會擋路）。範圍會自動跟著物件大小走。
/// 這個腳本請掛在「只代表這一個東西」的物件上，不要掛在包含整個場景或地板的父物件上。
/// </summary>
public class InteractableObject : MonoBehaviour
{
    /// <summary>目前場景裡所有啟用中的可調查物件（玩家腳本用它找最近的）。</summary>
    public static readonly HashSet<InteractableObject> Active = new HashSet<InteractableObject>();

    [Header("對話")]
    [Tooltip("DialogueDatabase 的 Id。填 0 代表不開對話，只觸發下面的事件。")]
    [SerializeField] private int inkId;
    [SerializeField] private UIType dialogueUI = UIType.DialogueWindowNew;

    [Header("碰到判定")]
    [Tooltip("物件的碰撞體（Collider）。留空 = 自動抓這個物件與子物件上的所有 Collider。碰到的判定就是看這些 Collider 的表面。")]
    [SerializeField] private Collider[] colliders;
    [Tooltip("玩家表面離這個物件表面多近（水平空隙，公尺）才算碰到。0 = 使用玩家身上的預設值。")]
    [SerializeField] private float touchMargin = 0f;

    [Header("提示（可選）")]
    [Tooltip("玩家靠近時顯示的提示物件，例如頭上的「F」圖示。留空就不顯示。")]
    [SerializeField] private GameObject promptObject;

    [Header("其他")]
    [Tooltip("勾選後只能調查一次（例如撿東西、一次性事件）")]
    [SerializeField] private bool oneShot;
    [Tooltip("調查的瞬間觸發（在對話開始前）。可用來給道具、改旗標等。")]
    [SerializeField] private UnityEvent onInteracted;

    private bool used;

    public bool CanInteract => !(oneShot && used);

    // 關閉「進入 Play 模式時重新載入程式」的設定時，static 資料不會自動清掉，這裡手動清
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Active.Clear();
    }

    private void Awake()
    {
        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
            Debug.LogWarning($"[{name}] InteractableObject 需要至少一個 Collider（可以勾 Is Trigger），否則無法判定有沒有碰到。", this);
        SetPromptVisible(false);
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
        SetPromptVisible(false);
    }

    /// <summary>
    /// 玩家範圍與這個物件碰撞體之間的水平空隙（公尺）。0 = 已經貼在一起或重疊。
    /// 只看水平方向（XZ），高度不影響。
    /// </summary>
    public float GapTo(Bounds player)
    {
        float best = float.MaxValue;
        foreach (var c in colliders)
        {
            if (c == null || !c.enabled) continue;
            Bounds b = c.bounds;
            float dx = Mathf.Max(0f, Mathf.Max(b.min.x - player.max.x, player.min.x - b.max.x));
            float dz = Mathf.Max(0f, Mathf.Max(b.min.z - player.max.z, player.min.z - b.max.z));
            best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dz * dz));
        }
        return best;
    }

    /// <summary>這個物件實際使用的容許距離（自己沒設定就用玩家的預設值）。</summary>
    public float GetTouchMargin(float defaultMargin)
    {
        return touchMargin > 0f ? touchMargin : defaultMargin;
    }

    public void SetPromptVisible(bool visible)
    {
        if (promptObject != null) promptObject.SetActive(visible);
    }

    /// <summary>由玩家呼叫。回傳的 UniTask 會等到對話視窗開啟完成。</summary>
    public async UniTask Interact()
    {
        used = true;
        SetPromptVisible(false);
        onInteracted?.Invoke();

        if (inkId != 0)
        {
            var dc = Dialoguecontroller.Instance;
            if (dc == null)
            {
                Debug.LogError($"[{name}] 場景裡找不到 Dialoguecontroller，無法開啟對話 {inkId}");
                return;
            }
            await dc.StartDialogue(inkId, dialogueUI);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        var cols = (colliders != null && colliders.Length > 0) ? colliders : GetComponentsInChildren<Collider>();
        float m = touchMargin > 0f ? touchMargin : 0.3f;
        foreach (var c in cols)
        {
            if (c == null) continue;
            Bounds b = c.bounds;
            b.Expand(new Vector3(m * 2f, 0f, m * 2f));
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}