using UnityEngine;
using Gamemanager;

// ★ 1. 移除 abstract！它現在是一個合格的、可以直接拖曳掛載到 GameObject 上的實體類別！
// ★ 2. 為了名稱清晰，建議你可以把這份檔案就叫做 CommandButton3D.cs
public class BaseButtonController3D : InteractiveMetaEntity3D, IPointerClickHandler
{
    [Header("★ 綁定的戰鬥指令")]
    [Tooltip("請拖入你要這個按鈕執行的 GameCommandSO (例如：開戰、對話、使用道具)")]
    [SerializeField] protected GameCommandSO commandToExecute;

    [Header("戰鬥鎖定設定")]
    [Tooltip("如果為 true，Boss 攻擊時此按鈕不會被鎖定 (可繼續拖曳或旋轉)")]
    [SerializeField] protected bool ignorePhaseLock = false;

    protected override void Awake()
    {
        base.Awake(); // 呼叫 InteractiveMetaEntity3D 的 Awake，自動處理 3D 物理厚度與開關！

        // 註冊戰鬥階段事件，用於自動鎖定/解鎖按鈕
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterAttackingPhaseEvent, OnBossAttacking);
            GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterVulnerablePhaseEvent, OnBossVulnerable);
        }
    }

    protected virtual void OnDestroy()
    {
        if (GameManager.Instance?.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterAttackingPhaseEvent>(OnBossAttacking);
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterVulnerablePhaseEvent>(OnBossVulnerable);
        }
    }

    // ==========================================
    // 實作點擊行為 (由 InteractionController3D 射線觸發)
    // ==========================================
    public virtual void OnClick()
    {
        if (commandToExecute != null)
        {
            Debug.Log($"[3D按鈕觸發] 執行綁定指令: {commandToExecute.commandName}");
            commandToExecute.Execute();
        }
        else
        {
            Debug.LogWarning($"[警告] 3D 按鈕 {gameObject.name} 尚未綁定任何 GameCommandSO 指令！");
        }
    }

    // ==========================================
    // Boss 階段自動鎖定控制
    // ==========================================
    private void OnBossAttacking(BossEnterAttackingPhaseEvent evt)
    {
        if (!ignorePhaseLock) SetInteractable(false, false);
    }

    private void OnBossVulnerable(BossEnterVulnerablePhaseEvent evt)
    {
        if (!ignorePhaseLock) SetInteractable(true, true);
    }
}