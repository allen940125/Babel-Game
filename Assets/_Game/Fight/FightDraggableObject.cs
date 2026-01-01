using Gamemanager; // 引用你的事件系統
using UnityEngine;

public class FightDraggableObject : DraggableObject // 繼承 DraggableObject
{
    // --- 1. 事件註冊 (只在子類別處理) ---
    
    private void OnEnable()
    {
        // 訂閱事件：當 Boss 進入 Idle (靜止) 狀態時 -> 解鎖拖曳
        GameManager.Instance.MainGameEvent.SetSubscribe(GameManager.Instance.MainGameEvent.OnBossEnterIdlePhaseEvent, OnBossEnterIdlePhase);
    }

    private void OnDisable()
    {
        // 記得檢查 Null 防止報錯
        if (GameManager.Instance != null && GameManager.Instance.MainGameEvent != null)
        {
            GameManager.Instance.MainGameEvent.Unsubscribe<BossEnterIdlePhaseEvent>(OnBossEnterIdlePhase);
        }
    }
    
    // --- 2. 事件處理 ---

    private void OnBossEnterIdlePhase(BossEnterIdlePhaseEvent evt)
    {
        Debug.Log("收到 Boss Idle 事件：解除鎖定，玩家可調整位置。");
    }

    // --- 3. 覆寫父類別的行為 ---

    // 只有在「判定為點擊」時，才發送戰鬥訊號
    protected override void OnClicked()
    {
        Debug.Log("玩家原地確認，戰鬥開始！");
        TriggerFightLock();
    }

    // 當玩家拖曳移動後放開，我們什麼都不做 (不觸發戰鬥)，讓玩家繼續調整
    protected override void OnRepositioned()
    {
        Debug.Log("玩家調整了位置 (未觸發戰鬥)");
        // 你可以在這裡播一個「放置音效」
    }
    
    // 我們只需要覆寫 "拖曳結束 (OnDragEnd)" 的時候要做的事
    protected override void OnDragEnd()
    {
        base.OnDragEnd(); // 先執行父類別原本的邏輯 (把 _isDragging 設為 false)
    }

    private void TriggerFightLock()
    {
        // 1. 發送戰鬥開始事件
        Debug.Log("位置確認！鎖定拖曳，發送戰鬥訊號。");
        GameManager.Instance.MainGameEvent.Send(new FightButtonClickEvent());
    }
}