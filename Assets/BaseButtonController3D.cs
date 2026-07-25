using Gamemanager;
using UnityEngine;

[RequireComponent(typeof(DraggableBehavior3D))]
[RequireComponent(typeof(RotatableBehavior3D))]
[RequireComponent(typeof(GlowBehavior3D))]
public abstract class BaseButtonController3D : MonoBehaviour, IPointerClickHandler
{
    [Header("基底設定")]
    [SerializeField] protected string activeTag = "Untagged";
    [SerializeField] protected string lockedTag = "PlayerButton";

    protected DraggableBehavior3D _dragBehavior;
    protected RotatableBehavior3D _rotateBehavior;
    protected GlowBehavior3D _glowBehavior;

    protected virtual void Awake()
    {
        _dragBehavior = GetComponent<DraggableBehavior3D>();
        _rotateBehavior = GetComponent<RotatableBehavior3D>();
        _glowBehavior = GetComponent<GlowBehavior3D>();
    }

    protected virtual void OnEnable()
    {
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, OnBossEnterIdlePhase);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterVulnerablePhaseEvent, OnBossEnterVulnerablePhase);
        }
    }

    protected virtual void OnDisable()
    {
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterIdlePhaseEvent>(OnBossEnterIdlePhase);
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterVulnerablePhaseEvent>(OnBossEnterVulnerablePhase);
        }
    }

    private void OnBossEnterIdlePhase(BossEnterIdlePhaseEvent evt)
    {
        SetInteractableState(true);
    }

    private void OnBossEnterVulnerablePhase(BossEnterVulnerablePhaseEvent evt)
    {
        SetInteractableState(false);
    }

    protected void SetInteractableState(bool state)
    {
        if (_dragBehavior != null) _dragBehavior.SetDraggable(state);
        if (_rotateBehavior != null) _rotateBehavior.enabled = state;
        if (_glowBehavior != null) _glowBehavior.SetGlow(state);
        this.tag = state ? activeTag : lockedTag;
    }

    // 實作介面：進行共同狀態攔截後，轉交給抽象方法
    public void OnClick()
    {
        Debug.Log($"[按鈕觸發] {gameObject.name} 被點擊");
        SetInteractableState(false); // 點擊後預設鎖定，防止重複連點
        
        // 呼叫具體的子類別業務邏輯
        OnButtonClicked();
    }

    /// <summary>
    /// 抽象方法：強迫所有繼承此類別的按鈕，必須具體實作自己的點擊行為
    /// </summary>
    protected abstract void OnButtonClicked();
}