using Cysharp.Threading.Tasks;
using Game.UI;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 玩家在場景裡「累計走了一段距離」之後，自動觸發一次對話（和／或事件）。
/// 用於「讓玩家移動幾步後再跳劇情」。
///
/// 用法：場景放一個空物件掛這支腳本，Ink Id 填 DialogueDatabase 的 Id，Walk Distance 填公尺數。
/// 只算水平方向、只算玩家自己走的距離（對話、背包、選單期間不會累計，因為那時候玩家本來就動不了）。
/// 之前的過場對話還在播時不會開始計算，等它結束才開始。
/// </summary>
public class WalkDistanceTrigger : MonoBehaviour
{
    [Header("觸發條件")]
    [Tooltip("玩家。留空 = 自動找場景裡的 PlayerExplorationController。")]
    [SerializeField] private Transform player;
    [SerializeField] private float walkDistance = 6f;
    [Tooltip("單幀位移超過這個值視為瞬間移動（傳送），不計入距離。")]
    [SerializeField] private float ignoreJumpOver = 3f;

    [Header("觸發內容")]
    [Tooltip("DialogueDatabase 的 Id。0 = 不開對話，只觸發下面的事件。")]
    [SerializeField] private int inkId;
    [SerializeField] private UIType dialogueUI = UIType.DialogueWindowNew;
    [Tooltip("對話結束（或沒有對話）之後觸發。可用來啟用可調查物件、開啟新手教學等。")]
    [SerializeField] private UnityEvent onFinished;

    public float WalkedDistance { get; private set; }
    public bool Triggered { get; private set; }

    private Vector3 lastPosition;
    private bool hasLast;

    private void Start()
    {
        if (player == null)
        {
            var controller = FindFirstObjectByType<PlayerExplorationController>();
            if (controller != null) player = controller.transform;
        }
        if (player == null)
            Debug.LogWarning("[WalkDistanceTrigger] 找不到玩家，請手動指定 Player。", this);
    }

    private void Update()
    {
        if (Triggered || player == null) return;

        // 其他對話 / 展示進行中不計算，並重設基準點，避免結束後算到一大段位移
        var dc = Dialoguecontroller.Instance;
        if ((dc != null && dc.DialogueIsPlaying) || ShowcaseOverlay.IsShowing)
        {
            hasLast = false;
            return;
        }

        Vector3 pos = player.position;
        if (hasLast)
        {
            Vector3 delta = pos - lastPosition;
            delta.y = 0f;
            float d = delta.magnitude;
            if (d <= ignoreJumpOver) WalkedDistance += d;
        }
        lastPosition = pos;
        hasLast = true;

        if (WalkedDistance >= walkDistance)
        {
            Triggered = true;
            Run().Forget();
        }
    }

    private async UniTaskVoid Run()
    {
        if (inkId != 0)
        {
            var dc = Dialoguecontroller.Instance;
            if (dc == null)
            {
                Debug.LogError("[WalkDistanceTrigger] 場景裡找不到 Dialoguecontroller。", this);
            }
            else
            {
                dc.StartDialogue(inkId, dialogueUI).Forget();
                // 等對話真的開始（最多等 10 秒，避免載入失敗時卡住），再等它結束
                float waited = 0f;
                while (!dc.DialogueIsPlaying && waited < 10f)
                {
                    waited += Time.deltaTime;
                    await UniTask.Yield();
                }
                await UniTask.WaitUntil(() => !dc.DialogueIsPlaying);
            }
        }
        onFinished?.Invoke();
    }
}
